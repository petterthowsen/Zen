using System.Data;
using System.Text;
using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST;
using Zen.Parsing.AST.Expressions;
using Zen.Parsing.AST.Statements;
using Zen.Typing;

namespace Zen.Execution;

public class Interpreter : IGenericVisitor<object?> {

    public static RuntimeError Error(string message, SourceLocation? location = null, ErrorType errorType = ErrorType.RuntimeError)
	{
		return new RuntimeError(message, errorType, location);
	}

    public Environment environment = new();

    // Global Output Buffering - useful for testing
    public bool GlobalOutputBufferingEnabled = false;
    public readonly StringBuilder GlobalOutputBuffer = new();

    /// <summary>
    /// Determines if a given object is truthy.
    /// </summary>
    public static bool IsTruthy(dynamic? value) {
        if (value is Variable variable) return variable.IsTruthy();
        if (value is ZenValue) return value.IsTruthy();
        
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

    private dynamic Evaluate(Expr expr) {
        return expr.Accept(this) !;
    }

    public dynamic? Visit(ProgramNode programNode)
    {
        foreach (var statement in programNode.Statements) {
            statement.Accept(this);
        }

        return ZenValue.Void;
    }

    public dynamic? Visit(Block block) {
        foreach (var statement in block.Statements) {
            statement.Accept(this);
        }

        return ZenValue.Void;
    }

    public dynamic? Visit(IfStmt ifStmt)
    {
        if (IsTruthy(Evaluate(ifStmt.Condition))) {
            ifStmt.Then.Accept(this);
        } else if (ifStmt.ElseIfs != null) {
            foreach (var elseIf in ifStmt.ElseIfs) {
                if (IsTruthy(Evaluate(elseIf.Condition))) {
                    elseIf.Then.Accept(this);
                    break;
                }
            }
        } else if (ifStmt.Else != null) {
            ifStmt.Else.Accept(this);
        }
        
        return ZenValue.Void;
    }

    public dynamic? Visit(Binary binary)
    {
        dynamic? left = Evaluate(binary.Left);
        dynamic? right = Evaluate(binary.Right);

        if (left is null || right is null) {
            throw Error($"Cannot use operator `{binary.Operator.Type}` on null value", binary.Location);
        }
        
        if (left is Variable leftVariable) {
            left = leftVariable.Value;
        }

        if (right is Variable rightVariable) {
            right = rightVariable.Value;
        }
        
        if (IsArithmeticOperator(binary.Operator.Type)) {
            // For now, we only support numbers (int, long, float, double)
            if ( ! IsNumber(left)) {
                throw Error($"Cannot use operator `{binary.Operator.Type}` on non-numeric value `{left}`", binary.Location);
            } else if ( ! IsNumber(right)) {
                throw Error($"Cannot use operator `{binary.Operator.Type}` on non-numeric value `{right}`", binary.Location);
            }
            
            var leftNumber = left!.Underlying;
            var rightNumber = right!.Underlying;

            return PerformArithmetic(binary.Operator.Type, leftNumber, rightNumber);
        }else if (IsComparisonOperator(binary.Operator.Type)) {
            return PerformComparison(left, right, binary.Operator);
        } else {
            throw Error($"Unsupported binary operator {binary.Operator}", binary.Location);
        }
    }

    private static ZenValue PerformArithmetic(TokenType tokenType, dynamic leftNumber, dynamic rightNumber) {
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
            } else if (leftNumber is float) {
                rightNumber = (float)rightNumber;
            } else if (leftNumber is double) {
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
        switch (tokenType) {
            case TokenType.PlusAssign:
                return new ZenValue(ZenType.Integer64, leftNumber + rightNumber);
            case TokenType.MinusAssign:
                return new ZenValue(ZenType.Integer64, leftNumber - rightNumber);
            case TokenType.StarAssign:
                return new ZenValue(ZenType.Integer64, leftNumber * rightNumber);
            case TokenType.SlashAssign:
                if (rightNumber == 0) {
                    throw Error($"Cannot divide `{leftNumber}` by zero");
                }
                return new ZenValue(ZenType.Integer64, leftNumber / rightNumber);
            case TokenType.Percent:
                // Ensure both operands are integers for modulus
                if (!(leftNumber is long || leftNumber is int)) {
                    throw Error($"Modulus operator requires both operands to be integers");
                }
                if (!(rightNumber is long || rightNumber is int)) {
                    throw Error($"Modulus operator requires both operands to be integers");
                }
                // Check for division by zero
                if (rightNumber == 0) {
                    throw Error($"Cannot compute modulus with divisor zero");
                }
                return new ZenValue(ZenType.Integer64, leftNumber % rightNumber);
        }

        return ZenValue.Null;
    }

    private static ZenValue PerformComparison(object left, object right, Token operatorToken)
    {
        bool result = operatorToken.Type switch {
            TokenType.Equal => IsEqual(left, right),
            TokenType.NotEqual => ! IsEqual(left, right),
            TokenType.LessThan => (dynamic)left < (dynamic)right,
            TokenType.LessThanOrEqual => (dynamic)left <= (dynamic)right,
            TokenType.GreaterThan => (dynamic)left > (dynamic)right,
            TokenType.GreaterThanOrEqual => (dynamic)left >= (dynamic)right,
            _ => throw Error($"Unsupported operator {operatorToken.Type}", operatorToken.Location)
        };

        if (result) {
            return ZenValue.True;
        } else {
            return ZenValue.False;
        }
    }

    public dynamic? Visit(Grouping grouping)
    {
        return Evaluate(grouping.Expression);
    }

    public dynamic? Visit(Unary unary)
    {
        dynamic? eval = Evaluate(unary.Right);

        if (unary.IsNot())
        {
            // Negate the truthiness of the value
            var result = !IsTruthy(eval);
            if (result) {
                return ZenValue.True;
            }else {
                return ZenValue.False;
            }
        }
        else if (unary.IsMinus())
        {
            ZenValue zenValue = ZenValue.Null;
            if (eval is Variable variable) {
                zenValue = variable.GetZenValue();
            }else if (eval is ZenValue value) {
                zenValue = value;
            }

            // Check if the value is a number
            if ( ! zenValue.IsNumber())
            {
                throw Error("Cannot negate a non-number. Did you mean to use 'not'?");
            }

            // Negate the numeric value
            if (zenValue.Type == ZenType.Integer) {
                return new ZenValue(ZenType.Integer, -zenValue.Underlying);
            }else if (zenValue.Type == ZenType.Float) {
                return new ZenValue(ZenType.Float, -zenValue.Underlying);
            } else if (zenValue.Type == ZenType.Integer64) {
                return new ZenValue(ZenType.Integer64, -zenValue.Underlying);
            } else if (zenValue.Type == ZenType.Float64) {
                return new ZenValue(ZenType.Float64, -zenValue.Underlying);
            }
        }

        throw new Exception("Implementation Error: Unknown unary operator.");
    }


    public dynamic? Visit(Literal literal)
    {
        return literal.Value;
    }

    public dynamic? Visit(Identifier identifier)
    {
        string name = identifier.Name;

        if ( ! environment.Exists(name))
        {
            throw Error($"Undefined variable '{name}'", identifier.Location, ErrorType.UndefinedVariable);
        }

        return environment.GetVariable(identifier.Name);
    }

    public dynamic? Visit(PrintStmt printStmt) {
        dynamic? value = Evaluate(printStmt.Expression);

        // if a variable reference, get the value
        if (value is Variable variable) {
            value = variable.Value;
        }

        // get the underlying value
        if (value is ZenValue) {
            value = value.Underlying;
        }

        // todo: might need to handle some types differently

        if (GlobalOutputBufferingEnabled) {
            GlobalOutputBuffer.Append(value);
        }else {
            Console.WriteLine(value);
        }

        return ZenValue.Void;
    }

    public dynamic Visit(ExpressionStmt expressionStmt)
    {
        return Evaluate(expressionStmt.Expression);
    }

    // DRAFT, not sure if it works
    public object? Visit(WhileStmt whileStmt)
    {
        dynamic? condition = Evaluate(whileStmt.Condition);

        while (IsTruthy(condition))
        {
            Visit(whileStmt.Body);
            condition = Evaluate(whileStmt.Condition);
        }

        return ZenValue.Void;
    }

    public object? Visit(VarStmt varStmt)
    {
        // TODO: Check for duplicate variable names
        // TODO: Check for invalid variable names
        // TODO: add a Variable class to store the name and value which could be a reference type or value type

        // first, we get the name of the variable
        // var myArr : Array<int, 10> = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        string name = varStmt.Identifier.Value;
        ZenType type = ZenType.Null;
        bool nullable = false;
        bool constant = varStmt.Constant;

        // Check if the variable is already defined
        if (environment.Exists(name))
        {
            throw Error($"Variable '{name}' is already defined", varStmt.Identifier.Location, ErrorType.RedefinitionError);
        }

        // check for missing typehint without initializer
        if (varStmt.TypeHint == null && varStmt.Initializer == null) {
            throw Error($"Missing type hint for variable '{name}'", varStmt.Identifier.Location, ErrorType.SyntaxError);
        }

        if (varStmt.TypeHint != null) {
            // type = varStmt.TypeHint.GetBaseType();
            type = Evaluate(varStmt.TypeHint);
            nullable = varStmt.TypeHint.Nullable;
        }

        // assign?
        if (varStmt.Initializer != null) {
            dynamic? value = Evaluate(varStmt.Initializer);

            if (value is ZenValue == false) {
                throw Error($"Implementation Error?: Trying to assign non-ZenValue to variable '{name}'", varStmt.Identifier.Location, ErrorType.RuntimeError);
            }

            // infer type?
            if (varStmt.TypeHint == null) {
                type = value.Type;
            }

            environment.Define(constant, name, type!, nullable);
            environment.Assign(name, value);
        }else {
            // declare only
            environment.Define(constant, name, type!, nullable);
        }

        return ZenValue.Void;
    }


    public dynamic? Visit(Assignment assignment) {
        dynamic? left = Evaluate(assignment.Identifier) ! ;

        if (left is not Variable) {
            throw Error($"Cannot assign to non-variable '{assignment.Identifier.Name}'", assignment.Identifier.Location, ErrorType.RuntimeError);
        }

        Variable leftVariable = (Variable) left;

        if (leftVariable.Constant) {
            throw Error($"Cannot assign to constant '{assignment.Identifier.Name}'", assignment.Identifier.Location, ErrorType.RuntimeError);
        }

        dynamic? right = Evaluate(assignment.Expression);
        dynamic? rightValue;

        // if a variable reference, get the value
        if (right is Variable rightVariable) {
            rightValue = rightVariable.Value;
        }else {
            rightValue = right;
        }
        
        if (assignment.Operator.Type == TokenType.Assign) {
            PerformAssignment(assignment.Operator, leftVariable, rightValue);
        }else {
            PerformAssignment(assignment.Operator, leftVariable, rightValue);
        }

        // // is the type of the value the same as the type of the variable?
        // if (rightValue.Type != leftVariable.Type) {
        //     throw Error($"Cannot assign value of type '{rightValue.Type}' to variable of type '{leftVariable.Type}'", assignment.Identifier.Location, ErrorType.TypeError);
        // }

        // environment.Assign(assignment.Identifier.Name, rightValue);

        return ZenValue.Void;
    }

    private static void PerformAssignment(Token op, Variable target, ZenValue right) {
        ZenValue left = (ZenValue) target.GetZenValue()!;
        ZenValue result;

        if (op.Type == TokenType.Assign) {
            result = right;
        }else {

            if ( ! IsNumber(left)) {
                throw Error($"Cannot use operator `{op.Type}` on non-numeric value `{left}`", op.Location);
            } else if ( ! IsNumber(right)) {
                throw Error($"Cannot use operator `{op.Type}` on non-numeric value `{right}`", op.Location);
            }

            result = PerformArithmetic(op.Type, left.Underlying, right.Underlying);
        }

        target.Assign(result);
    }

    public dynamic? Visit(ForStmt forStmt)
    {   
        throw new NotImplementedException();
    }

    public dynamic? Visit(ForInStmt forInStmt)
    {
        throw new NotImplementedException();
    }

    public dynamic? Visit(TypeHint typeHint)
    {
        // For now, we just return the base type
        return typeHint.GetBaseZenType();
    }
}