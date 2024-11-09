using System.Data;
using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST;
using Zen.Parsing.AST.Expressions;
using Zen.Parsing.AST.Statements;

namespace Zen.Execution;

public class Interpreter : IGenericVisitor<object?> {

    protected static RuntimeError Error(string message, SourceLocation? location = null, ErrorType errorType = ErrorType.RuntimeError)
	{
		return new RuntimeError(message, errorType, location);
	}

    /// <summary>
    /// Determines if a given object is truthy.
    /// </summary>
    /// <remarks>
    /// The following values are considered truthy:
    /// <list type="bullet">
    ///     <item>true</item>
    ///     <item>non-zero integers</item>
    ///     <item>non-zero floats</item>
    ///     <item>non-zero longs</item>
    ///     <item>non-zero doubles</item>
    /// </list>
    /// </remarks>
    public static bool IsTruthy(object? value) {
        return value switch {
            null => false,
            bool b => b,
            int i => i != 0,
            float f => f != 0f,
            long l => l != 0L,
            double d => d != 0d,
            _ => true
        };
    }

    public static bool IsNumber(object? value) {
        return value is int || value is float || value is long || value is double;
    }

    public static bool IsArithmeticOperator(TokenType type) {
        return type == TokenType.Plus || type == TokenType.Minus || type == TokenType.Star || type == TokenType.Slash;
    }

    public static bool IsComparisonOperator(TokenType type) {
        return type == TokenType.Equal || type == TokenType.NotEqual || type == TokenType.LessThan || type == TokenType.LessThanOrEqual || type == TokenType.GreaterThan || type == TokenType.GreaterThanOrEqual;
    }

    public static bool IsEqual(object? a, object? b) {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;

        return a.Equals(b);
    }

    private object? Evaluate(Expr expr) {
        return expr.Accept(this);
    }

    public object? Visit(ProgramNode programNode)
    {
        return "";
    }

    public object? Visit(IfStmt ifStmt)
    {
        throw new NotImplementedException();
    }

    public object? Visit(Binary binary)
    {
        object? left = Evaluate(binary.Left);
        object? right = Evaluate(binary.Right);

        if (IsArithmeticOperator(binary.Operator.Type)) {
            // For now, we only support numbers (int, long, float, double)
            if ( ! IsNumber(left)) {
                throw Error($"Cannot use operator `{binary.Operator.Type}` on non-numeric value `{left}`", binary.Location);
            } else if ( ! IsNumber(right)) {
                throw Error($"Cannot use operator `{binary.Operator.Type}` on non-numeric value `{right}`", binary.Location);
            }

            // Promote the operands as necessary
            if (left is int) {
                if (right is long) {
                    left = (long)left;
                } else if (right is float) {
                    left = (float)left;
                } else if (right is double) {
                    left = (double)left;
                }
            } else if (left is long) {
                if (right is float) {
                    left = (float)left;
                } else if (right is double) {
                    left = (double)left;
                }
            } else if (left is float) {
                if (right is double) {
                    left = (double)left;
                }
            }

            if (right is int) {
                if (left is long) {
                    right = (long)right;
                } else if (left is float) {
                    right = (float)right;
                } else if (left is double) {
                    right = (double)right;
                }
            } else if (right is long) {
                if (left is float) {
                    right = (float)right;
                } else if (left is double) {
                    right = (double)right;
                }
            } else if (right is float) {
                if (left is double) {
                    right = (double)right;
                }
            }

            // Handle arithmetic and comparison operations
            return binary.Operator.Type switch
            {
                TokenType.Plus => PerformArithmetic(left, right, (x, y) => x + y),
                TokenType.Minus => PerformArithmetic(left, right, (x, y) => x - y),
                TokenType.Star => PerformArithmetic(left, right, (x, y) => x * y),
                TokenType.Slash => PerformArithmetic(left, right, (x, y) => x / y),
                TokenType.Percent => PerformArithmetic(left, right, (x, y) => x % y),
                _ => throw Error($"Unsupported operator {binary.Operator}", binary.Location)
            };
        }else if (IsComparisonOperator(binary.Operator.Type)) {
            return PerformComparison(left, right, binary.Operator);
        } else {
            throw Error($"Unsupported binary operator {binary.Operator}", binary.Location);
        }
    }

    private static dynamic PerformArithmetic(object left, object right, Func<dynamic, dynamic, dynamic> operation)
    {
        return operation(left, right);
    }

    private static bool PerformComparison(object left, object right, Token operatorToken)
    {
        return operatorToken.Type switch {
            TokenType.Equal => IsEqual(left, right),
            TokenType.NotEqual => !IsEqual(left, right),
            TokenType.LessThan => (dynamic)left < (dynamic)right,
            TokenType.LessThanOrEqual => (dynamic)left <= (dynamic)right,
            TokenType.GreaterThan => (dynamic)left > (dynamic)right,
            TokenType.GreaterThanOrEqual => (dynamic)left >= (dynamic)right,
            _ => throw Error($"Unsupported operator {operatorToken.Type}", operatorToken.Location)
        };
    }

    public object? Visit(Grouping grouping)
    {
        return Evaluate(grouping.Expression);
    }

    public object? Visit(Unary unary)
    {
        object? value = Evaluate(unary.Right);

        if (unary.IsNot())
        {
            // Negate the truthiness of the value
            return !IsTruthy(value);
        }
        else if (unary.IsMinus())
        {
            // Check if the value is a number
            if (!IsNumber(value))
            {
                throw new Exception("Cannot negate a non-number. Did you mean to use 'not'?");
            }

            // Negate the numeric value
            return value switch
            {
                int i => -i,
                float f => -f,
                long l => -l,
                double d => -d,
                _ => throw new Exception("Cannot negate unsupported numeric type.")
            };
        }

        throw new Exception("Implementation Error: Unknown unary operator.");
    }


    public object? Visit(Literal literal)
    {
        return literal.Value;
    }
}