using System.Data;
using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST;
using Zen.Parsing.AST.Expressions;
using Zen.Parsing.AST.Statements;
using Zen.Typing;

namespace Zen.Execution;

public class Interpreter : IGenericVisitor<object?> {

    protected static RuntimeError Error(string message, SourceLocation? location = null, ErrorType errorType = ErrorType.RuntimeError)
	{
		return new RuntimeError(message, errorType, location);
	}

    public Environment environment = new();

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
    public static bool IsTruthy(dynamic? value) {
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

    public static bool IsNumber(dynamic? value) {
        if (value is null) return false;
        if (value is ZenValue) return value.IsNumber();
        return false;
    }

    public static bool IsArithmeticOperator(TokenType type) {
        return type == TokenType.Plus || type == TokenType.Minus || type == TokenType.Star || type == TokenType.Slash;
    }

    public static bool IsComparisonOperator(TokenType type) {
        return type == TokenType.Equal || type == TokenType.NotEqual || type == TokenType.LessThan || type == TokenType.LessThanOrEqual || type == TokenType.GreaterThan || type == TokenType.GreaterThanOrEqual;
    }

    public static bool IsEqual(dynamic? a, dynamic? b) {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;

        return a.Equals(b);
    }

    public void Interpret(Node node) {
        node.Accept(this);
    }

    private dynamic? Evaluate(Expr expr) {
        return expr.Accept(this);
    }

    public dynamic? Visit(ProgramNode programNode)
    {
        foreach (var statement in programNode.Statements) {
            statement.Accept(this);
        }

        return null;
    }

    public dynamic? Visit(Block block) {
        foreach (var statement in block.Body) {
            statement.Accept(this);
        }

        return null;
    }

    public dynamic? Visit(IfStmt ifStmt)
    {
        throw new NotImplementedException();
    }

    public dynamic? Visit(Binary binary)
    {
        dynamic? left = Evaluate(binary.Left);
        dynamic? right = Evaluate(binary.Right);

        if (IsArithmeticOperator(binary.Operator.Type)) {
            // For now, we only support numbers (int, long, float, double)
            if ( ! IsNumber(left)) {
                throw Error($"Cannot use operator `{binary.Operator.Type}` on non-numeric value `{left}`", binary.Location);
            } else if ( ! IsNumber(right)) {
                throw Error($"Cannot use operator `{binary.Operator.Type}` on non-numeric value `{right}`", binary.Location);
            }
            
            var leftNumber = left!.Underlying;
            var rightNumber = right!.Underlying;

            // Promote the operands as necessary
            if (leftNumber is int) {
                if (rightNumber is long) {
                    leftNumber = (long)leftNumber;
                } else if (rightNumber is float) {
                    leftNumber = (float)leftNumber;
                } else if (rightNumber is double) {
                    leftNumber = (double)leftNumber;
                }
            } else if (leftNumber is long) {
                if (rightNumber is float) {
                    leftNumber = (float)leftNumber;
                } else if (rightNumber is double) {
                    double doubleLeft = (long)leftNumber;
                    leftNumber = doubleLeft;
                }
            } else if (leftNumber is float) {
                if (rightNumber is double) {
                    leftNumber = (double)leftNumber;
                }
            }

            if (rightNumber is int) {
                if (leftNumber is long) {
                    rightNumber = (long)rightNumber;
                } else if (left is float) {
                    rightNumber = (float)rightNumber;
                } else if (left is double) {
                    rightNumber = (double)rightNumber;
                }
            } else if (rightNumber is long) {
                if (leftNumber is float) {
                    rightNumber = (float)rightNumber;
                } else if (leftNumber is double) {
                    rightNumber = (double)rightNumber;
                }
            } else if (rightNumber is float) {
                if (leftNumber is double) {
                    rightNumber = (double)rightNumber;
                }
            }
            
            // Perform the operation
            switch (binary.Operator.Type) {
                case TokenType.Plus:
                    return new ZenValue(ZenType.Integer64, leftNumber + rightNumber);
                case TokenType.Minus:
                    return new ZenValue(ZenType.Integer64, leftNumber - rightNumber);
                case TokenType.Star:
                    return new ZenValue(ZenType.Integer64, leftNumber * rightNumber);
                case TokenType.Slash:
                    if (rightNumber == 0) {
                        throw Error($"Cannot divide `{leftNumber}` by zero", binary.Location);
                    }
                    return new ZenValue(ZenType.Integer64, leftNumber / rightNumber);
                case TokenType.Percent:
                    // Ensure both operands are integers for modulus
                    if (!(leftNumber is long || leftNumber is int)) {
                        throw Error($"Modulus operator requires both operands to be integers", binary.Location);
                    }
                    if (!(rightNumber is long || rightNumber is int)) {
                        throw Error($"Modulus operator requires both operands to be integers", binary.Location);
                    }
                    // Check for division by zero
                    if (rightNumber == 0) {
                        throw Error($"Cannot compute modulus with divisor zero", binary.Location);
                    }
                    return new ZenValue(ZenType.Integer64, leftNumber % rightNumber);
            }

            return null;
        }else if (IsComparisonOperator(binary.Operator.Type)) {
            return PerformComparison(left, right, binary.Operator);
        } else {
            throw Error($"Unsupported binary operator {binary.Operator}", binary.Location);
        }
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

    public dynamic? Visit(Grouping grouping)
    {
        return Evaluate(grouping.Expression);
    }

    public dynamic? Visit(Unary unary)
    {
        dynamic? value = Evaluate(unary.Right);

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


    public dynamic? Visit(Literal literal)
    {
        return literal.Value;
    }

    public dynamic? Visit(PrintStmt printStmt) {
        dynamic? value = Evaluate(printStmt.Expression);

        // print the value
        if (value is ZenValue) {
            value = value.Underlying;
        }

        Console.WriteLine(value);

        return null;
    }

    public dynamic? Visit(ExpressionStmt expressionStmt)
    {
        return Evaluate(expressionStmt.Expression);
    }
}