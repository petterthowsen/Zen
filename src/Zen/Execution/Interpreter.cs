using System.Data;
using System.Text;
using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST;
using Zen.Parsing.AST.Expressions;
using Zen.Parsing.AST.Statements;
using Zen.Typing;

namespace Zen.Execution;

public class Interpreter : IGenericVisitor<IEvaluationResult> {

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
    
    public static bool IsArithmeticAssignmentOperator(TokenType type) {
        return type == TokenType.PlusAssign || type == TokenType.MinusAssign || type == TokenType.StarAssign || type == TokenType.SlashAssign;
    }

    public static bool IsComparisonOperator(TokenType type) {
        return type == TokenType.Equal || type == TokenType.NotEqual || type == TokenType.LessThan || type == TokenType.LessThanOrEqual || type == TokenType.GreaterThan || type == TokenType.GreaterThanOrEqual;
    }

    public static ZenType DetermineResultType(TokenType op, ZenType left, ZenType right) {
        if ((IsArithmeticOperator(op) || IsArithmeticAssignmentOperator(op)) && left.IsNumeric && right.IsNumeric) {
            return GetNumericPromotionType(left, right);
        }else if (left == ZenType.String || right == ZenType.String) {
            return ZenType.String;
        }

        throw Error($"Invalid operation {op} between types {left} and {right}", null, ErrorType.TypeError);

        // return op switch {
        //     IsArithmeticOperator() when left.IsNumeric && right.IsNumeric 
        //         => GetNumericPromotionType(left, right),
        //     TokenType.Plus or TokenType.PlusAssign when left == ZenType.String || right == ZenType.String 
        //         => ZenType.String,
        //     TokenType.Minus or TokenType.MinusAssign or TokenType.Star or TokenType.Slash or TokenType.StarAssign when left.IsNumeric && right.IsNumeric 
        //         => GetNumericPromotionType(left, right),
        //     _ => throw Error($"Invalid operation {op} between types {left} and {right}", null, ErrorType.TypeError)
        // };
    }

    private static ZenType GetNumericPromotionType(ZenType a, ZenType b) {
        if (a == ZenType.Float64 || b == ZenType.Float64) return ZenType.Float64;
        if (a == ZenType.Float || b == ZenType.Float) return ZenType.Float;
        if (a == ZenType.Integer64 || b == ZenType.Integer64) return ZenType.Integer64;
        return ZenType.Integer;
    }

    public static bool IsEqual(dynamic? a, dynamic? b) {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;

        return a.Equals(b);
    }

    public void Interpret(Node node) {
        node.Accept(this);
    }

    private IEvaluationResult Evaluate(Expr expr) {
        return expr.Accept(this) !;
    }

    public IEvaluationResult Visit(ProgramNode programNode)
    {
        foreach (var statement in programNode.Statements) {
            statement.Accept(this);
        }

        return VoidResult.Instance;
    }

    public IEvaluationResult Visit(Block block) {
        foreach (var statement in block.Statements) {
            statement.Accept(this);
        }

        return VoidResult.Instance;
    }

    public IEvaluationResult Visit(IfStmt ifStmt)
    {
        if (Evaluate(ifStmt.Condition).IsTruthy()) {
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
        
        return VoidResult.Instance;
    }

    public IEvaluationResult Visit(Binary binary)
    {
        IEvaluationResult leftRes = Evaluate(binary.Left);
        IEvaluationResult rightRes = Evaluate(binary.Right);

        ZenValue left = leftRes.Value;;
        ZenValue right = rightRes.Value;
        
        if (IsArithmeticOperator(binary.Operator.Type)) {
            // For now, we only support numbers (int, long, float, double)
            if ( ! left.IsNumber()) {
                throw Error($"Cannot use operator `{binary.Operator.Type}` on non-numeric value `{left}`", binary.Location);
            } else if ( ! right.IsNumber()) {
                throw Error($"Cannot use operator `{binary.Operator.Type}` on non-numeric value `{right}`", binary.Location);
            }

            // Check operation validity and get result type
            ZenType resultType = DetermineResultType(binary.Operator.Type, left.Type, right.Type);

            if (left.Type != resultType) {
                left = TypeConverter.Convert(left, resultType);
            }
            if (right.Type != resultType) {
                right = TypeConverter.Convert(right, resultType);
            }

            // Perform operation
            ZenValue result = PerformArithmetic(binary.Operator.Type, resultType, left.Underlying, right.Underlying);
            
            return (ValueResult) result;
        }else if (IsComparisonOperator(binary.Operator.Type)) {
            return (ValueResult) PerformComparison(left, right, binary.Operator);
        } else {
            throw Error($"Unsupported binary operator {binary.Operator}", binary.Location);
        }
    }

    private static ZenValue PerformArithmetic(TokenType tokenType, ZenType type, dynamic leftNumber, dynamic rightNumber) {
        // Perform the operation
        switch (tokenType) {
            case TokenType.Plus or TokenType.PlusAssign:
                return new ZenValue(type, leftNumber + rightNumber);
            case TokenType.MinusAssign or TokenType.Minus:
                return new ZenValue(type, leftNumber - rightNumber);
            case TokenType.StarAssign or TokenType.Star:
                return new ZenValue(type, leftNumber * rightNumber);
            case TokenType.SlashAssign or TokenType.Slash:
                if (rightNumber == 0) {
                    throw Error($"Cannot divide `{leftNumber}` by zero");
                }
                return new ZenValue(type, leftNumber / rightNumber);
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
                return new ZenValue(type, leftNumber % rightNumber);
        }

        return ZenValue.Null;
    }

    private static ZenValue PerformComparison(ZenValue left, ZenValue right, Token operatorToken)
    {
        bool result = operatorToken.Type switch {
            TokenType.Equal => IsEqual(left, right),
            TokenType.NotEqual => ! IsEqual(left, right),
            TokenType.LessThan => left.Underlying < right.Underlying,
            TokenType.LessThanOrEqual => left.Underlying <= right.Underlying,
            TokenType.GreaterThan => left.Underlying > right.Underlying,
            TokenType.GreaterThanOrEqual => left.Underlying >= right.Underlying,
            _ => throw Error($"Unsupported operator {operatorToken.Type}", operatorToken.Location)
        };

        if (result) {
            return ZenValue.True;
        } else {
            return ZenValue.False;
        }
    }

    public IEvaluationResult Visit(Grouping grouping)
    {
        return Evaluate(grouping.Expression);
    }

    public IEvaluationResult Visit(Unary unary)
    {
        IEvaluationResult eval = Evaluate(unary.Right);

        if (unary.IsNot())
        {
            // Negate the truthiness of the value
            var result = !IsTruthy(eval);
            if (result) {
                return (ValueResult) ZenValue.True;
            }else {
                return (ValueResult) ZenValue.False;
            }
        }
        else if (unary.IsMinus())
        {
            ZenValue zenValue = ZenValue.Null;
            if (eval is VariableResult variableResult) {
                zenValue = variableResult.Value;
            }else if (eval is ValueResult valueResult) {
                zenValue = valueResult.Value;
            }

            // Check if the value is a number
            if ( ! zenValue.IsNumber())
            {
                throw Error("Cannot negate a non-number. Did you mean to use 'not'?");
            }

            // Negate the numeric value
            if (zenValue.Type == ZenType.Integer) {
                return (ValueResult) new ZenValue(ZenType.Integer, -zenValue.Underlying);
            }else if (zenValue.Type == ZenType.Float) {
                return (ValueResult) new ZenValue(ZenType.Float, -zenValue.Underlying);
            } else if (zenValue.Type == ZenType.Integer64) {
                return (ValueResult) new ZenValue(ZenType.Integer64, -zenValue.Underlying);
            } else if (zenValue.Type == ZenType.Float64) {
                return (ValueResult) new ZenValue(ZenType.Float64, -zenValue.Underlying);
            }
        }

        throw new Exception("Implementation Error: Unknown unary operator.");
    }


    public IEvaluationResult Visit(Literal literal)
    {
        return (ValueResult) literal.Value;
    }

    public IEvaluationResult Visit(Identifier identifier)
    {
        string name = identifier.Name;

        if ( ! environment.Exists(name))
        {
            throw Error($"Undefined variable '{name}'", identifier.Location, ErrorType.UndefinedVariable);
        }

        return (VariableResult) environment.GetVariable(identifier.Name);
    }

    public IEvaluationResult Visit(PrintStmt printStmt) {
        IEvaluationResult expResult = Evaluate(printStmt.Expression);

        ZenValue value = expResult.Value; // might be a from a variable - might not.

        // todo: might need to handle some types differently

        if (GlobalOutputBufferingEnabled) {
            GlobalOutputBuffer.Append(value.Stringify());
        }else {
            Console.WriteLine(value.Stringify());
        }

        return (ValueResult) ZenValue.Void;
    }

    public IEvaluationResult Visit(ExpressionStmt expressionStmt)
    {
        return Evaluate(expressionStmt.Expression);
    }

    // DRAFT, not sure if it works
    public IEvaluationResult Visit(WhileStmt whileStmt)
    {
        IEvaluationResult conditionResult = Evaluate(whileStmt.Condition);

        while (conditionResult.IsTruthy())
        {
            Visit(whileStmt.Body);
            conditionResult = Evaluate(whileStmt.Condition);
        }

        return (ValueResult) ZenValue.Void;
    }

    public IEvaluationResult Visit(VarStmt varStmt)
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
            type = Evaluate(varStmt.TypeHint).Type;
            nullable = varStmt.TypeHint.Nullable;
        }

        // assign?
        if (varStmt.Initializer != null) {
            IEvaluationResult value = Evaluate(varStmt.Initializer);

            // infer type?
            if (varStmt.TypeHint == null) {
                type = value.Type;
            }

            environment.Define(constant, name, type!, nullable);
            environment.Assign(name, value.Value);
        }else {
            // declare only
            environment.Define(constant, name, type!, nullable);
        }

        return (ValueResult) ZenValue.Void;
    }


    public IEvaluationResult Visit(Assignment assignment) {
        VariableResult left = (VariableResult) Evaluate(assignment.Identifier) ! ;

        if (left.Variable is null) {
            throw Error($"Cannot assign to non-variable '{assignment.Identifier.Name}'", assignment.Identifier.Location, ErrorType.RuntimeError);
        }

        Variable leftVariable = left.Variable;

        if (leftVariable.Constant) {
            throw Error($"Cannot assign to constant '{assignment.Identifier.Name}'", assignment.Identifier.Location, ErrorType.RuntimeError);
        }

        IEvaluationResult right =  Evaluate(assignment.Expression);

        // perform the assignment operation
        PerformAssignment(assignment.Operator, leftVariable, right.Value);

        return (ValueResult) ZenValue.Void;
    }

    private static void PerformAssignment(Token op, Variable target, ZenValue right) {
        ZenValue left = target.GetZenValue()!;
        ZenValue result;

        if (op.Type == TokenType.Assign) {
            // type check
            if ( ! TypeChecker.IsCompatible(left.Type, right.Type)) {
                throw Error($"Cannot assign value of type '{right.Type}' to variable of type '{left.Type}'", op.Location, ErrorType.TypeError);
            }
            result = right;
        }else {
            if ( ! left.IsNumber()) {
                throw Error($"Cannot use operator `{op.Type}` on non-numeric value `{left}`", op.Location);
            } else if ( ! right.IsNumber()) {
                throw Error($"Cannot use operator `{op.Type}` on non-numeric value `{right}`", op.Location);
            }

            if ( ! TypeChecker.IsCompatible(source: right.Type, target: left.Type)) {
                throw Error($"Cannot use operator `{op.Type}` on values of different types `{left.Type}` and `{right.Type}`", op.Location, ErrorType.TypeError);
            }

            ZenType returnType = DetermineResultType(op.Type, left.Type, right.Type);

            result = PerformArithmetic(op.Type, returnType, left.Underlying, right.Underlying);
        }

        target.Assign(result);
    }

    public IEvaluationResult Visit(ForStmt forStmt)
    {   
        Environment previousEnvironment = environment;
        environment = new Environment(previousEnvironment);

        try {
            Token loopIdentifier = forStmt.LoopIdentifier;
            ValueResult loopValue = (ValueResult) Evaluate(forStmt.Initializer);

            environment.Define(false, loopIdentifier.Value, loopValue.Type, false);
            environment.Assign(loopIdentifier.Value, loopValue.Value);

            Expr condition = forStmt.Condition;
            Expr incrementor = forStmt.Incrementor;

            while (Evaluate(condition).Value.IsTruthy()) {
                // execute body
                forStmt.Body.Accept(this);

                // increment loop variable
                Evaluate(incrementor);
            }
        } finally {
            environment = previousEnvironment;
        }
        
        return (ValueResult) ZenValue.Void;
    }

    public IEvaluationResult Visit(ForInStmt forInStmt)
    {
        throw new NotImplementedException();
    }

    public IEvaluationResult Visit(TypeHint typeHint)
    {
        // For now, we just return the base type
        return (TypeResult) typeHint.GetBaseZenType();
    }
}