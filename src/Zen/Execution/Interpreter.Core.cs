using System.Text;
using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST;
using Zen.Typing;
using Zen.Execution.EvaluationResult;
using Zen.Execution.Import;

namespace Zen.Execution;

public partial class Interpreter : IGenericVisitor<IEvaluationResult>
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

    // Event loop for managing async operations
    public readonly EventLoop EventLoop;

    public Importer Importer;

    public Node? CurrentNode {get; protected set;}

    public Interpreter(EventLoop eventLoop)
    {
        environment = globalEnvironment;
        EventLoop = eventLoop;
        Instance = this;
    }

    public ZenHostFunction RegisterHostFunction(string name, ZenType returnType, List<ZenFunction.Argument> parameters, Func<ZenValue[], ZenValue> func, bool variadic = false)
    {
        var hostFunc = new ZenHostFunction(false, returnType, parameters, func, globalEnvironment);
        hostFunc.Variadic = variadic;
        globalEnvironment.Define(true, name, ZenType.Function, false);
        globalEnvironment.Assign(name, new ZenValue(ZenType.Function, hostFunc));
        return hostFunc;
    }

    public ZenHostFunction RegisterAsyncHostFunction(string name, ZenType returnType, List<ZenFunction.Argument> parameters, Func<ZenValue[], Task<ZenValue>> func, bool variadic = false)
    {
        var hostFunc = new ZenHostFunction(true, returnType, parameters, args =>
        {
            var promise = new ZenPromise(environment, returnType);
            EventLoop.EnqueueTask(async () =>
            {
                try
                {
                    var result = await func(args);
                    promise.Resolve(result);
                }
                catch (Exception ex)
                {
                    promise.Reject(new ZenValue(ZenType.String, ex.Message));
                }
            });
            return new ZenValue(ZenType.Promise, promise);
        }, globalEnvironment);

        hostFunc.Variadic = variadic;

        globalEnvironment.Define(true, name, ZenType.Function, false);
        globalEnvironment.Assign(name, new ZenValue(ZenType.Function, hostFunc));

        return hostFunc;
    }

    public void RegisterFunction(bool async, string name, ZenType returnType, List<ZenFunction.Argument> parameters, Block block, Environment? closure = null)
    {
        var userFunc = new ZenUserFunction(async, returnType, parameters, block, closure ?? globalEnvironment);
        RegisterFunction(name, userFunc);
    }

    public void RegisterFunction(string name, ZenUserFunction func)
    {
        environment.Define(true, name, ZenType.Function, false);
        environment.Assign(name, new ZenValue(ZenType.Function, func));
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

    private static ZenValue PerformAssignment(Token op, ZenType leftType, ZenValue right)
    {
        return PerformAssignment(op, new ZenValue(leftType, null), right);
    }

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

    public void Interpret(Node node)
    {
        node.Accept(this);
    }

    public void Interpret(ProgramNode node, bool awaitEvents = true)
    {
        Instance = this;
        Visit(node, awaitEvents);
    }

    public void Shutdown()
    {
        EventLoop.Stop();
    }
}
