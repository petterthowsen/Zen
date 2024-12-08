using System.Text;
using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST;
using Zen.Typing;
using Zen.Execution.EvaluationResult;
using Zen.Execution.Import;
using System.Reflection.Metadata.Ecma335;

namespace Zen.Execution;

public partial class Interpreter : IGenericVisitorAsync<Task<IEvaluationResult>>
{
    public static RuntimeError Error(string message, SourceLocation? location = null, ErrorType errorType = ErrorType.RuntimeError, Exception? innerException = null)
    {
        return new RuntimeError(message, errorType, location, innerException);
    }

    public static Interpreter Instance;

    /// <summary>
    /// The top level environment / global scope
    /// </summary>
    public Environment globalEnvironment = new(null, "global");

    // The current environment
    public Environment environment;

    /// <summary>
    /// Maps expressions to a distance
    /// </summary>
    public Dictionary<Node, int> Locals = [];

    // Global Output Buffering - useful for testing
    public bool GlobalOutputBufferingEnabled = false;
    public readonly StringBuilder GlobalOutputBuffer = new();
    
    // OutputHandler func takes a string and returns void
    public Action<string>? OutputHandler;

    // Synchronization context for managing async operations
    public readonly ZenSynchronizationContext SyncContext;

    public Importer Importer;

    public Node? CurrentNode {get; protected set;}

    public Interpreter(ZenSynchronizationContext syncContext)
    {
        environment = globalEnvironment;
        SyncContext = syncContext;
        Instance = this;
    }

    public async Task<IEvaluationResult> Interpret(Node node)
    {
        return await node.AcceptAsync(this);
    }

    /// <summary>
    /// Interpret a program as a main script and run the event loop.
    /// </summary>
    /// <param name="programNode"></param>
    /// <returns></returns>
    public async Task<IEvaluationResult> Execute(ProgramNode programNode)
    {
        await VisitAsync(programNode);

        SyncContext.RunOnCurrentThread();

        return VoidResult.Instance;
    }


    /// <summary>
    /// Create and register a regular host function as a global constant.
    /// </summary>
    public ZenFunction RegisterHostFunction(string name, ZenType returnType, List<ZenFunction.Argument> arguments, Func<ZenValue[], ZenValue> func, bool variadic = false)
    {
        var zenFunc = ZenFunction.NewHostFunction(returnType, arguments, func, variadic);
        zenFunc.Name = name;
        RegisterFunction(name, zenFunc, globalEnvironment);
        return zenFunc;
    }

    /// <summary>
    /// Create and register an async host function as a global constant.
    /// </summary>
    public ZenFunction RegisterHostFunction(string name, ZenType returnType, List<ZenFunction.Argument> arguments, Func<ZenValue[], Task<ZenValue>> func, bool variadic = false) {
        var zenFunc = ZenFunction.NewAsyncHostFunction(returnType, arguments, func, variadic);
        zenFunc.Name = name;
        RegisterFunction(name, zenFunc, globalEnvironment);
        return zenFunc;
    }

    // // the old function
    // public ZenFunction RegisterAsyncHostFunction(string name, ZenType returnType, List<ZenFunction.Argument> parameters, Func<ZenValue[], Task<ZenValue>> func, bool variadic = false)
    // {   
    //     // should we wrap the function in a Task or handle that when calling instead?
    //     // wrap the async function
    //     Func<ZenValue[], Task<ZenValue>> wrapper = async (args) =>
    //     {
    //         // Create a TaskCompletionSource
    //         var tcs = new TaskCompletionSource<ZenValue>();
            
    //         // Post the async work to our sync context
    //         SyncContext.Post(async _ =>
    //         {
    //             try
    //             {
    //                 var result = await func(args);
    //                 tcs.SetResult(result);
    //             }
    //             catch (Exception ex)
    //             {
    //                 tcs.SetException(ex);
    //             }
    //         }, null);

    //         // Return a ZenValue that wraps the Task
    //         return new ZenValue(ZenType.Task, tcs.Task);
    //     };

    //     ZenFunction zenFunc = ZenFunction.NewAsyncHostFunction(returnType, parameters, wrapper, variadic);
    //     RegisterFunction(name, zenFunc);
    //     return zenFunc;
    // }

    /// <summary>
    /// Create and register a user function as a global constant.
    /// </summary>
    public ZenFunction RegisterUserFunction(string name, ZenType returnType, List<ZenFunction.Argument> parameters, Block block, Environment? closure = null, bool async = false)
    {
        ZenFunction userFunction = ZenFunction.NewUserFunction(returnType, parameters, block, closure ?? globalEnvironment, async);
        userFunction.Name = name;
        RegisterFunction(name, userFunction);
        return userFunction;
    }

    /// <summary>
    ///     Register the given ZenFunction as a constant in the given environment, defaulting to the current environment.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="func"></param>
    public void RegisterFunction(string name, ZenFunction func, Environment? env = null)
    {
        env ??= environment;

        env.Define(true, name, ZenType.Function, false);
        env.Assign(name, new ZenValue(ZenType.Function, func));
    }

    public void Resolve(Node expr, int depth = 0)
    {
        Logger.Instance.Debug($"Resolving {expr} at depth {depth}");
        Locals.Add(expr, depth);
    }

    public void ClearLocals()
    {
        Locals.Clear();
    }

    private VariableResult LookUpVariable(string name, Expr expr)
    {
        if (Locals.TryGetValue(expr, out int distance))
        {
            Logger.Instance.Debug($"Found variable {name} at distance {distance}");
            Variable value = environment.GetAt(distance, name);
            return (VariableResult)value;
        }
        else
        {
            // Global variable
            if (!globalEnvironment.Exists(name))
            {
                throw Error($"Undefined variable '{name}'",
                    expr.Location, ErrorType.UndefinedVariable);
            }

            return (VariableResult)globalEnvironment.GetVariable(name);
        }
    }

    public static bool IsArithmeticOperator(TokenType type)
    {
        return type == TokenType.Plus || type == TokenType.Minus || type == TokenType.Star || type == TokenType.Slash;
    }

    public static bool IsArithmeticAssignmentOperator(TokenType type)
    {
        return type == TokenType.PlusAssign || type == TokenType.MinusAssign || type == TokenType.StarAssign || type == TokenType.SlashAssign;
    }

    public static bool IsComparisonOperator(TokenType type)
    {
        return type == TokenType.Equal || type == TokenType.NotEqual || type == TokenType.LessThan || type == TokenType.LessThanOrEqual || type == TokenType.GreaterThan || type == TokenType.GreaterThanOrEqual;
    }

    public static ZenType DetermineResultType(TokenType op, ZenType left, ZenType right)
    {
        if ((IsArithmeticOperator(op) || IsArithmeticAssignmentOperator(op)) && left.IsNumeric && right.IsNumeric)
        {
            return GetNumericPromotionType(left, right);
        }
        else if ((op == TokenType.Plus) && left == ZenType.String && (right == ZenType.String || right.IsNumeric))
        {
            return ZenType.String;
        }

        throw Error($"Invalid operation {op} between types {left} and {right}", null, ErrorType.TypeError);
    }

    private static ZenType GetNumericPromotionType(ZenType a, ZenType b)
    {
        if (a == ZenType.Float64 || b == ZenType.Float64) return ZenType.Float64;
        if (a == ZenType.Float || b == ZenType.Float) return ZenType.Float;
        if (a == ZenType.Integer64 || b == ZenType.Integer64) return ZenType.Integer64;
        if (a == ZenType.Integer || b == ZenType.Integer) return ZenType.Integer;
        return ZenType.Void;
    }

    private static ZenValue PerformArithmetic(TokenType tokenType, ZenType type, dynamic left, dynamic right)
    {
        // Perform the operation
        switch (tokenType)
        {
            case TokenType.Plus or TokenType.PlusAssign:
                return new ZenValue(type, left + right);
            case TokenType.MinusAssign or TokenType.Minus:
                return new ZenValue(type, left - right);
            case TokenType.StarAssign or TokenType.Star:
                return new ZenValue(type, left * right);
            case TokenType.SlashAssign or TokenType.Slash:
                if (right == 0)
                {
                    throw Error($"Cannot divide `{left}` by zero");
                }
                return new ZenValue(type, left / right);
            case TokenType.Percent:
                // Ensure both operands are integers for modulus
                if (!(left is long || left is int))
                {
                    throw Error($"Modulus operator requires both operands to be integers");
                }
                if (!(right is long || right is int))
                {
                    throw Error($"Modulus operator requires both operands to be integers");
                }
                // Check for division by zero
                if (right == 0)
                {
                    throw Error($"Cannot compute modulus with divisor zero");
                }
                return new ZenValue(type, left % right);
        }

        return ZenValue.Null;
    }

    private static ZenValue PerformComparison(ZenValue left, ZenValue right, Token operatorToken)
    {
        bool result = operatorToken.Type switch
        {
            TokenType.Equal => IsEqual(left, right),
            TokenType.NotEqual => !IsEqual(left, right),
            TokenType.LessThan => left.Underlying < right.Underlying,
            TokenType.LessThanOrEqual => left.Underlying <= right.Underlying,
            TokenType.GreaterThan => left.Underlying > right.Underlying,
            TokenType.GreaterThanOrEqual => left.Underlying >= right.Underlying,
            _ => throw Error($"Unsupported operator {operatorToken.Type}", operatorToken.Location)
        };

        if (result)
        {
            return ZenValue.True;
        }
        else
        {
            return ZenValue.False;
        }
    }

    // private static ZenValue PerformAssignment(Token op, ZenType leftType, ZenValue right)
    // {
    //     return PerformAssignment(op, new ZenValue(leftType, null), right);
    // }

    private static ZenValue PerformAssignment(Token op, ZenValue left, ZenValue right)
    {
        ZenValue result;

        if (op.Type == TokenType.Assign)
        {
            // type check
            if (!TypeChecker.IsCompatible(left.Type, right.Type))
            {
                throw Error($"Cannot assign value of type '{right.Type}' to target of type '{left.Type}'", op.Location, ErrorType.TypeError);
            }
            result = right;
        }
        else
        {
            if (!left.IsNumber())
            {
                throw Error($"Cannot use operator `{op.Type}` on non-numeric value `{left}`", op.Location);
            }
            else if (!right.IsNumber())
            {
                throw Error($"Cannot use operator `{op.Type}` on non-numeric value `{right}`", op.Location);
            }

            if (!TypeChecker.IsCompatible(source: right.Type, target: left.Type))
            {
                throw Error($"Cannot use operator `{op.Type}` on values of different types `{left.Type}` and `{right.Type}`", op.Location, ErrorType.TypeError);
            }

            ZenType returnType = DetermineResultType(op.Type, left.Type, right.Type);

            result = PerformArithmetic(op.Type, returnType, left.Underlying, right.Underlying);
        }

        return result;
    }

    public static bool IsEqual(dynamic? a, dynamic? b)
    {
        if (a is null && b is null) return true;
        if (a is null) return false;

        return a.Equals(b);
    }
}
