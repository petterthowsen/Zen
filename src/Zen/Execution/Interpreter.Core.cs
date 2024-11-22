// Interpreter.Core.cs
using System.Text;
using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST;
using Zen.Typing;
using Zen.Execution.EvaluationResult;

namespace Zen.Execution;

public partial class Interpreter : IGenericVisitor<IEvaluationResult>
{
    public static RuntimeError Error(string message, SourceLocation? location = null, ErrorType errorType = ErrorType.RuntimeError)
    {
        return new RuntimeError(message, errorType, location);
    }

    /// <summary>
    /// The top level environment / global scope
    /// </summary>
    public Environment globalEnvironment = new();

    // The current environment
    public Environment environment;

    /// <summary>
    /// Maps expressions to a distance from global scope (I think?)
    /// </summary>
    protected Dictionary<Expr, int> Locals = [];

    // Global Output Buffering - useful for testing
    public bool GlobalOutputBufferingEnabled = false;
    public readonly StringBuilder GlobalOutputBuffer = new();

    public Interpreter()
    {
        environment = globalEnvironment;
        RegisterBuiltins(new Builtins.Core.Typing());
        RegisterBuiltins(new Builtins.Core.Time());
    }

    /// <summary>
    /// Registers builtins from the provided builtins provider.
    /// </summary>
    /// <param name="builtinsProvider"></param>
    public void RegisterBuiltins(IBuiltinsProvider builtinsProvider)
    {
        builtinsProvider.RegisterBuiltins(this);
    }

    public void RegisterHostFunction(string name, ZenType returnType, List<ZenFunction.Parameter> parameters, Func<ZenValue[], ZenValue> func)
    {
        var hostFunc = new ZenHostFunction(returnType, parameters, func, globalEnvironment);
        globalEnvironment.Define(true, name, ZenType.Function, false);
        globalEnvironment.Assign(name, new ZenValue(ZenType.Function, hostFunc));
    }

    // TODO: need to store named map of parameters and their type and default value
    public void RegisterFunction(string name, ZenType returnType, List<ZenFunction.Parameter> parameters, Block block, Environment? closure = null)
    {
        var userFunc = new ZenUserFunction(returnType, parameters, block, closure ?? globalEnvironment);
        globalEnvironment.Define(true, name, ZenType.Function, false);
        globalEnvironment.Assign(name, new ZenValue(ZenType.Function, userFunc));
    }

    public void Resolve(Expr expr, int depth = 0)
    {
        Locals.Add(expr, depth);
    }

    private VariableResult LookUpVariable(string name, Expr expr)
    {
        if (Locals.TryGetValue(expr, out int distance))
        {
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
        else if (left == ZenType.String || right == ZenType.String)
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
        return ZenType.Integer;
    }

    private static ZenValue PerformArithmetic(TokenType tokenType, ZenType type, dynamic leftNumber, dynamic rightNumber)
    {
        // Perform the operation
        switch (tokenType)
        {
            case TokenType.Plus or TokenType.PlusAssign:
                return new ZenValue(type, leftNumber + rightNumber);
            case TokenType.MinusAssign or TokenType.Minus:
                return new ZenValue(type, leftNumber - rightNumber);
            case TokenType.StarAssign or TokenType.Star:
                return new ZenValue(type, leftNumber * rightNumber);
            case TokenType.SlashAssign or TokenType.Slash:
                if (rightNumber == 0)
                {
                    throw Error($"Cannot divide `{leftNumber}` by zero");
                }
                return new ZenValue(type, leftNumber / rightNumber);
            case TokenType.Percent:
                // Ensure both operands are integers for modulus
                if (!(leftNumber is long || leftNumber is int))
                {
                    throw Error($"Modulus operator requires both operands to be integers");
                }
                if (!(rightNumber is long || rightNumber is int))
                {
                    throw Error($"Modulus operator requires both operands to be integers");
                }
                // Check for division by zero
                if (rightNumber == 0)
                {
                    throw Error($"Cannot compute modulus with divisor zero");
                }
                return new ZenValue(type, leftNumber % rightNumber);
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
}