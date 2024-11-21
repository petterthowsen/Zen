using System.Data;
using System.Text;
using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST;
using Zen.Parsing.AST.Expressions;
using Zen.Parsing.AST.Statements;
using Zen.Typing;
using Zen.Execution.EvaluationResult;

namespace Zen.Execution;

public class Interpreter : IGenericVisitor<IEvaluationResult>
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

        // register builtins
        RegisterBuiltins(new Builtins.Core.Typing());
        RegisterBuiltins(new Builtins.Core.Time());
    }

    /// <summary>
    /// Registers builtins from the provided builtins provider.
    /// </summary>
    /// <param name="builtinsProvider"></param>
    public void RegisterBuiltins(IBuiltinsProvider builtinsProvider) {
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

    private IEvaluationResult Evaluate(Expr expr)
    {
        return expr.Accept(this)!;
    }

    public IEvaluationResult Visit(ProgramNode programNode)
    {
        foreach (var statement in programNode.Statements)
        {
            statement.Accept(this);
        }

        return VoidResult.Instance;
    }

    public IEvaluationResult Visit(Block block)
    {
        return ExecuteBlock(block, new Environment(environment));
    }

    public IEvaluationResult ExecuteBlock(Block block, Environment environment)
    {
        Environment previousEnvironment = this.environment;
        this.environment = environment;

        try
        {
            foreach (var statement in block.Statements)
            {
                statement.Accept(this);
            }
        }
        finally
        {
            this.environment = previousEnvironment;
        }

        return VoidResult.Instance;
    }

    public IEvaluationResult Visit(IfStmt ifStmt)
    {
        if (Evaluate(ifStmt.Condition).IsTruthy())
        {
            ifStmt.Then.Accept(this);
        }
        else if (ifStmt.ElseIfs != null)
        {
            foreach (var elseIf in ifStmt.ElseIfs)
            {
                if (Evaluate(elseIf.Condition).IsTruthy())
                {
                    elseIf.Then.Accept(this);
                    break;
                }
            }
        }
        else if (ifStmt.Else != null)
        {
            ifStmt.Else.Accept(this);
        }

        return VoidResult.Instance;
    }

    public IEvaluationResult Visit(Binary binary)
    {
        IEvaluationResult leftRes = Evaluate(binary.Left);
        IEvaluationResult rightRes = Evaluate(binary.Right);

        ZenValue left = leftRes.Value;
        ZenValue right = rightRes.Value;

        if (IsArithmeticOperator(binary.Operator.Type))
        {
            // For now, we only support numbers (int, long, float, double)
            if (!left.IsNumber())
            {
                throw Error($"Cannot use operator `{binary.Operator.Type}` on non-numeric value `{left}`", binary.Location);
            }
            else if (!right.IsNumber())
            {
                throw Error($"Cannot use operator `{binary.Operator.Type}` on non-numeric value `{right}`", binary.Location);
            }

            // Check operation validity and get result type
            ZenType resultType = DetermineResultType(binary.Operator.Type, left.Type, right.Type);

            if (left.Type != resultType)
            {
                left = TypeConverter.Convert(left, resultType);
            }
            if (right.Type != resultType)
            {
                right = TypeConverter.Convert(right, resultType);
            }

            // Perform operation
            ZenValue result = PerformArithmetic(binary.Operator.Type, resultType, left.Underlying, right.Underlying);

            return (ValueResult)result;
        }
        else if (IsComparisonOperator(binary.Operator.Type))
        {
            return (ValueResult)PerformComparison(left, right, binary.Operator);
        }
        else
        {
            throw Error($"Unsupported binary operator {binary.Operator}", binary.Location);
        }
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
            TokenType.NotEqual => ! IsEqual(left, right),
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
            if ( ! eval.IsTruthy())
            {
                return (ValueResult)ZenValue.True;
            }
            else
            {
                return (ValueResult)ZenValue.False;
            }
        }
        else if (unary.IsMinus())
        {
            ZenValue zenValue = ZenValue.Null;
            if (eval is VariableResult variableResult)
            {
                zenValue = variableResult.Value;
            }
            else if (eval is ValueResult valueResult)
            {
                zenValue = valueResult.Value;
            }

            // Check if the value is a number
            if (!zenValue.IsNumber())
            {
                throw Error("Cannot negate a non-number. Did you mean to use 'not'?");
            }

            // Negate the numeric value
            if (zenValue.Type == ZenType.Integer)
            {
                return (ValueResult)new ZenValue(ZenType.Integer, -zenValue.Underlying);
            }
            else if (zenValue.Type == ZenType.Float)
            {
                return (ValueResult)new ZenValue(ZenType.Float, -zenValue.Underlying);
            }
            else if (zenValue.Type == ZenType.Integer64)
            {
                return (ValueResult)new ZenValue(ZenType.Integer64, -zenValue.Underlying);
            }
            else if (zenValue.Type == ZenType.Float64)
            {
                return (ValueResult)new ZenValue(ZenType.Float64, -zenValue.Underlying);
            }
        }

        throw new Exception("Implementation Error: Unknown unary operator.");
    }


    public IEvaluationResult Visit(Literal literal)
    {
        return (ValueResult)literal.Value;
    }

    public IEvaluationResult Visit(Identifier identifier)
    {
        string name = identifier.Name;

        return LookUpVariable(name, identifier);
    }

    private IEvaluationResult LookUpVariable(string name, Expr expr)
    {
        if (Locals.TryGetValue(expr, out int distance))
        {
            Variable value = environment.GetAt(distance, name);
            return (VariableResult)value;
        }
        else
        {
            // Global variable
            if ( ! globalEnvironment.Exists(name))
            {
                throw Error($"Undefined variable '{name}'",
                    expr.Location, ErrorType.UndefinedVariable);
            }

            return (VariableResult) globalEnvironment.GetVariable(name);
        }
    }

    public IEvaluationResult Visit(PrintStmt printStmt)
    {
        IEvaluationResult expResult = Evaluate(printStmt.Expression);

        ZenValue value = expResult.Value; // might be a from a variable - might not.

        // todo: might need to handle some types differently

        if (GlobalOutputBufferingEnabled)
        {
            GlobalOutputBuffer.Append(value.Stringify());
        }
        else
        {
            Console.WriteLine(value.Stringify());
        }

        return (ValueResult)ZenValue.Void;
    }

    public IEvaluationResult Visit(ExpressionStmt expressionStmt)
    {
        return Evaluate(expressionStmt.Expression);
    }

    public IEvaluationResult Visit(VarStmt varStmt)
    {
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
        if (varStmt.TypeHint == null && varStmt.Initializer == null)
        {
            throw Error($"Missing type hint for variable '{name}'", varStmt.Identifier.Location, ErrorType.SyntaxError);
        }

        if (varStmt.TypeHint != null)
        {
            // type = varStmt.TypeHint.GetBaseType();
            type = Evaluate(varStmt.TypeHint).Type;
            nullable = varStmt.TypeHint.Nullable;
        }

        // assign?
        if (varStmt.Initializer != null)
        {
            IEvaluationResult value = Evaluate(varStmt.Initializer);

            // infer type?
            if (varStmt.TypeHint == null)
            {
                type = value.Type;
            }

            environment.Define(constant, name, type!, nullable);
            environment.Assign(name, value.Value);
        }
        else
        {
            // declare only
            environment.Define(constant, name, type!, nullable);
        }

        return (ValueResult)ZenValue.Void;
    }

    public IEvaluationResult Visit(Assignment assignment)
    {
        // evaluate the expression on the right hand side
        IEvaluationResult right = Evaluate(assignment.Expression);
        Variable leftVariable;

        try 
        {
            // Use resolved scope information if available
            if (Locals.TryGetValue(assignment, out int distance))
            {
                leftVariable = environment.GetAt(distance, assignment.Identifier.Name);
            }
            else
            {
                // Global variable
                leftVariable = globalEnvironment.GetVariable(assignment.Identifier.Name);
            }
        }
        catch
        {
            throw Error($"Undefined variable '{assignment.Identifier.Name}'",
                assignment.Identifier.Location, ErrorType.UndefinedVariable);
        }

        if (leftVariable is null)
        {
            throw Error($"Cannot assign to non-variable '{assignment.Identifier.Name}'",
                assignment.Identifier.Location, ErrorType.RuntimeError);
        }

        if (leftVariable.Constant)
        {
            throw Error($"Cannot assign to constant '{assignment.Identifier.Name}'",
                assignment.Identifier.Location, ErrorType.RuntimeError);
        }

        // perform the assignment operation
        ZenValue newValue = PerformAssignment(assignment.Operator, (ZenValue) leftVariable.Value!, right.Value);

        // update the variable
        leftVariable.Assign(newValue);

        // return the variable
        return (VariableResult) leftVariable;
    }

    public IEvaluationResult Visit(Get get) {
        IEvaluationResult result = Evaluate(get.Expression);

        if (result.Type == ZenType.Object)
        {
            ZenObject instance = (ZenObject)result.Value.Underlying!;
            
            // is it a method?
            ZenMethod? method;
            instance.HasMethodHierarchically(get.Identifier.Value, out method);

            if (method != null)
            {
                return (ValueResult) method.Bind(instance);
            }

            if ( ! instance.HasProperty(get.Identifier.Value)) {
                throw Error($"Undefined property '{get.Identifier.Value}' on object of type '{result.Type}'", get.Identifier.Location, ErrorType.UndefinedProperty);
            }

            return (ValueResult) instance.GetProperty(get.Identifier.Value);
        }
        else
        {
            throw Error($"Cannot get property of type '{result.Type}'", get.Identifier.Location, ErrorType.TypeError);
        }
    }

    public IEvaluationResult Visit(Set set) {
        // evaluate the object expression
        IEvaluationResult objectExpression = Evaluate(set.ObjectExpression);
        string propertyName = set.Identifier.Value;

        // get the object
        ZenValue objectValue = objectExpression.Value;

        if (objectValue.Type == ZenType.Object)
        {
            // get the object instance
            ZenObject instance = (ZenObject)objectValue.Underlying!;

            if ( ! instance.HasProperty(propertyName)) {
                throw Error($"Undefined property '{propertyName}' on object of type '{objectValue.Type}'", set.Identifier.Location, ErrorType.UndefinedProperty);
            }

            // evaluate the value expression
            IEvaluationResult valueExpression = Evaluate(set.ValueExpression);

            // perform assignment
            ZenValue newValue = PerformAssignment(set.Operator, instance.GetProperty(propertyName), valueExpression.Value);

            // set the property
            instance.SetProperty(propertyName, newValue);

            return (ValueResult)newValue;
        }
        else
        {
            throw Error($"Cannot set property of non-object type '{objectValue.Type}'", set.Identifier.Location, ErrorType.TypeError);
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


    public IEvaluationResult Visit(TypeHint typeHint)
    {
        // For now, we just return the base type
        return (TypeResult)typeHint.GetBaseZenType();
    }

    public IEvaluationResult Visit(Logical logical)
    {
        IEvaluationResult left = Evaluate(logical.Left);

        if (logical.Token.Value == "or")
        {
            if (left.IsTruthy()) return left;
        }
        else
        {
            if (!left.IsTruthy()) return left;
        }

        return Evaluate(logical.Right);
    }

    public IEvaluationResult Visit(WhileStmt whileStmt)
    {
        IEvaluationResult conditionResult = Evaluate(whileStmt.Condition);

        while (conditionResult.IsTruthy())
        {
            Visit(whileStmt.Body);
            conditionResult = Evaluate(whileStmt.Condition);
        }

        return (ValueResult)ZenValue.Void;
    }

    public IEvaluationResult Visit(ForStmt forStmt)
    {
        Environment previousEnvironment = environment;
        environment = new Environment(previousEnvironment);

        try
        {
            Token loopIdentifier = forStmt.LoopIdentifier;
            ValueResult loopValue = (ValueResult)Evaluate(forStmt.Initializer);

            environment.Define(false, loopIdentifier.Value, loopValue.Type, false);
            environment.Assign(loopIdentifier.Value, loopValue.Value);

            Expr condition = forStmt.Condition;
            Expr incrementor = forStmt.Incrementor;

            while (Evaluate(condition).Value.IsTruthy())
            {
                // execute body
                foreach (Stmt statement in forStmt.Body.Statements) {
                    statement.Accept(this);
                }

                // increment loop variable
                Evaluate(incrementor);
            }
        }
        finally
        {
            environment = previousEnvironment;
        }

        return (ValueResult)ZenValue.Void;
    }

    public IEvaluationResult Visit(ForInStmt forInStmt)
    {
        throw new NotImplementedException();
    }

    public IEvaluationResult Visit(ReturnStmt returnStmt)
    {
        IEvaluationResult result;

        if (returnStmt.Expression != null)
        {
            result = Evaluate(returnStmt.Expression);
        }
        else
        {
            result = (ValueResult)ZenValue.Void;
        }

        throw new ReturnException(result, returnStmt.Location);
    }

    public IEvaluationResult Visit(This dis)
    {
        return LookUpVariable("this", dis);
    }

    public IEvaluationResult Visit(Call call)
    {
        IEvaluationResult callee = Evaluate(call.Callee);

        if (callee.IsCallable())
        {
            ZenFunction function = (ZenFunction)callee.Value.Underlying!;

            // check number of arguments is at least equal to the number of non-nullable parameters
            if (call.Arguments.Length < function.Parameters.Count(p => !p.Nullable))
            {
                throw Error($"Not enough arguments for function", null, ErrorType.RuntimeError);
            }

            // check number of arguments is at most equal to the number of parameters
            if (call.Arguments.Length > function.Parameters.Count)
            {
                throw Error($"Too many arguments for function", null, ErrorType.RuntimeError);
            }

            // evaluate the arguments
            ZenValue[] argumentValues = new ZenValue[call.Arguments.Length];

            for (int i = 0; i < call.Arguments.Length; i++)
            {
                argumentValues[i] = Evaluate(call.Arguments[i]).Value;
            }

            // check that the types of the arguments are compatible with the types of the parameters
            for (int i = 0; i < call.Arguments.Length; i++)
            {
                ZenFunction.Parameter parameter = function.Parameters[i];
                ZenValue argument = argumentValues[i];

                if (!TypeChecker.IsCompatible(argument.Type, parameter.Type))
                {
                    throw Error($"Cannot pass argument of type '{argument.Type}' to parameter of type '{parameter.Type}'", call.Arguments[i].Location, ErrorType.TypeError);
                }
            }

            if (function is ZenHostFunction)
            {
                return (ValueResult)function.Call(this, argumentValues);
            }
            else if (function is ZenUserFunction userFunc)
            {
                return CallUserFunction(userFunc, argumentValues);
            }
            else if (function is BoundMethod boundMethod) {
                return CallUserFunction(boundMethod, argumentValues);
            }
            else
            {
                throw Error($"Cannot call unknown function type '{function.GetType()}'", call.Location, ErrorType.RuntimeError);
            }
        }
        else
        {
            throw Error($"Cannot call non-callable of type '{callee.Type}'", call.Location, ErrorType.RuntimeError);
        }
    }

    public IEvaluationResult CallUserFunction(BoundMethod bound, ZenValue[] arguments) {
        if (bound.Method is ZenUserMethod userMethod) {
            return CallUserFunction(bound.Closure, userMethod.Block, bound.Parameters, bound.ReturnType, arguments);
        }
        else if (bound.Method is ZenHostMethod hostMethod) {
            return (ValueResult) hostMethod.Call(this, bound.Instance, arguments);
        }

        return VoidResult.Instance;
    }

    public IEvaluationResult CallUserFunction(ZenUserFunction function, ZenValue[] arguments)
    {
        return CallUserFunction(function.Closure, function.Block, function.Parameters, function.ReturnType, arguments);
    }

    public IEvaluationResult CallUserFunction(Environment? closure, Block block, List<ZenFunction.Parameter> parameters, ZenType returnType, ZenValue[] arguments)
    {
        Environment previousEnvironment = environment;

        // Create outer environment for parameters
        Environment paramEnv = new Environment(closure);
        for (int i = 0; i < parameters.Count; i++)
        {
            paramEnv.Define(false, parameters[i].Name, parameters[i].Type, parameters[i].Nullable);
            paramEnv.Assign(parameters[i].Name, arguments[i]);
        }

        // Create inner environment for method body, which will contain 'this'
        environment = new Environment(paramEnv);

        try
        {
            foreach (var statement in block.Statements)
            {
                statement.Accept(this);
            }
        }
        catch (ReturnException returnException)
        {
            // type check return value
            if (!TypeChecker.IsCompatible(returnException.Result.Type, returnType))
            {
                throw Error($"Cannot return value of type '{returnException.Result.Type}' from function of type '{returnType}'", returnException.Location, ErrorType.TypeError);
            }
            return returnException.Result;
        }
        finally
        {
            environment = previousEnvironment;
        }

        return (ValueResult)ZenValue.Void;
    }

    public IEvaluationResult Visit(FuncStmt funcStmt)
    {
        // function parameters
        List<ZenFunction.Parameter> parameters = [];

        for (int i = 0; i < funcStmt.Parameters.Length; i++)
        {
            FunctionParameterResult funcParamResult = (FunctionParameterResult) funcStmt.Parameters[i].Accept(this);
            parameters.Add(funcParamResult.Parameter);
        }

        RegisterFunction(funcStmt.Identifier.Value, funcStmt.ReturnType.GetZenType(), parameters, funcStmt.Block, environment);

        return (ValueResult)ZenValue.Void;
    }

    public IEvaluationResult Visit(FuncParameter funcParameter)
    {
        // parameter name
        string name = funcParameter.Identifier.Value;

        // parameter type
        ZenType type = ZenType.Null;
        bool nullable = true;

        if (funcParameter.TypeHint != null)
        {
            type = funcParameter.TypeHint.GetZenType();
            nullable = funcParameter.TypeHint.Nullable;
        }


        ZenFunction.Parameter parameter = new(name, type, nullable);

        return (FunctionParameterResult)parameter;
    }

    public IEvaluationResult Visit(ClassStmt classStmt)
    {
        environment.Define(true, classStmt.Identifier.Value, ZenType.Class, false);

        // create the Properties
        List<ZenClass.Property> properties = [];

        foreach (var property in classStmt.Properties)
        {
            ZenType type = ZenType.Any;
            ZenValue defaultValue = ZenValue.Null;

            if (property.Initializer != null)
            {
                IEvaluationResult defaultValueResult = Evaluate(property.Initializer);
                defaultValue = defaultValueResult.Value;
                type = defaultValue.Type;

                if (property.TypeHint != null)
                {
                    type = property.TypeHint.GetZenType();

                    if ( ! TypeChecker.IsCompatible(defaultValue.Type, type))
                    {
                        throw Error($"Cannot assign value of type '{defaultValue.Type}' to property of type '{type}'", property.TypeHint.Location, ErrorType.TypeError);
                    }
                }
            }else {
                if (property.TypeHint == null) {
                    throw Error($"Property '{property.Identifier.Value}' must have a type hint", property.Identifier.Location, ErrorType.SyntaxError);
                }

                type = property.TypeHint.GetZenType();
                defaultValue = new ZenValue(type);
            }

            ZenClass.Visibility visibility = ZenClass.Visibility.Public;
            
            // check modifiers
            foreach (Token modifier in property.Modifiers)
            {
                if (modifier.Value == "public")
                {
                    visibility = ZenClass.Visibility.Public;
                }
                else if (modifier.Value == "private")
                {
                    visibility = ZenClass.Visibility.Private;
                }
                else if (modifier.Value == "protected")
                {
                    visibility = ZenClass.Visibility.Protected;
                }
            }

            properties.Add(new ZenClass.Property(property.Identifier.Value, type, defaultValue, visibility));
        }

        // create the methods
        List<ZenMethod> methods = [];

        foreach (MethodStmt methodStmt in classStmt.Methods)
        {
            string name = methodStmt.Identifier.Value;
            ZenClass.Visibility visibility = ZenClass.Visibility.Public;
            ZenType returnType = ZenType.Void;

            // method parameters
            List<ZenFunction.Parameter> parameters = [];

            for (int i = 0; i < methodStmt.Parameters.Length; i++)
            {
                FunctionParameterResult funcParamResult = (FunctionParameterResult) methodStmt.Parameters[i].Accept(this);
                parameters.Add(funcParamResult.Parameter);
            }

            ZenUserMethod method = new(name, visibility, returnType, parameters, methodStmt.Block, environment);
            methods.Add(method);
        }

        ZenClass clazz = new ZenClass(classStmt.Identifier.Value, methods, properties);

        environment.Assign(classStmt.Identifier.Value, new ZenValue(ZenType.Class, clazz));

        return (ValueResult)ZenValue.Void;
    }

    public IEvaluationResult Visit(PropertyStmt propertyStmt)
    {
        // this isn't used.
        return (ValueResult)ZenValue.Void;
    }

    public IEvaluationResult Visit(MethodStmt methodStmt)
    {
        // this isn't used.
        return (ValueResult)ZenValue.Void;
    }

    public IEvaluationResult Visit(Instantiation instantiation)
    {
        Call call = instantiation.Call;
        IEvaluationResult clazzResult = Evaluate(call.Callee);

        // make sure it's a Class type
        if (clazzResult.Type != ZenType.Class)
        {
            throw Error($"Cannot instantiate non-class type '{clazzResult.Type}'", instantiation.Location, ErrorType.TypeError);
        }

        // get the underlying ZenClass
        ZenValue value = clazzResult.Value;
        ZenClass clazz = (ZenClass) value.Underlying!;

        //TODO: gather arguments
        List<ZenValue> arguments = [];
        foreach (var argument in call.Arguments)
        {
            arguments.Add(Evaluate(argument).Value);
        }

        // create new instance
        ZenObject instance = clazz.CreateInstance(this, [.. arguments]);

        // wrap it in a ZenValue
        ZenValue result = new ZenValue(ZenType.Object, instance);

        // return as ValueResult (IEvaluationResult)
        return (ValueResult) result;
    }
}