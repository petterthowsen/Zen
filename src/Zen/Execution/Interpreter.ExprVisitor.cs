// Interpreter.ExpressionVisitor.cs
using Zen.Execution.EvaluationResult;
using Zen.Parsing.AST;
using Zen.Parsing.AST.Expressions;
using Zen.Typing;

namespace Zen.Execution;

public partial class Interpreter
{
    private IEvaluationResult Evaluate(Expr expr)
    {
        return expr.Accept(this)!;
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
                throw Error($"Undefined property '{get.Identifier.Value}' on object of type '{result.Type}'", get.Identifier.Location, Common.ErrorType.UndefinedProperty);
            }

            return (ValueResult) instance.GetProperty(get.Identifier.Value);
        }
        else
        {
            throw Error($"Cannot get property of type '{result.Type}'", get.Identifier.Location, Common.ErrorType.TypeError);
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
                throw Error($"Undefined property '{propertyName}' on object of type '{objectValue.Type}'", set.Identifier.Location, Common.ErrorType.UndefinedProperty);
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
            throw Error($"Cannot set property of non-object type '{objectValue.Type}'", set.Identifier.Location, Common.ErrorType.TypeError);
        }
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

    public IEvaluationResult Visit(This dis)
    {
        return LookUpVariable("this", dis);
    }

    public IEvaluationResult Visit(Await await)
    {
        // Evaluate the expression being awaited
        IEvaluationResult result = Evaluate(await.Expression);
        
        // Get the value
        ZenValue value = result.Value;
        
        // If it's not a promise, throw an error
        if (value.Underlying is not ZenPromise promise)
        {
            throw Error($"Cannot await non-promise value of type '{value.Type}'", 
                await.Location, Common.ErrorType.TypeError);
        }

        // Wait for the promise to complete and get its result
        try 
        {
            ZenValue promiseResult = promise.AsTask().GetAwaiter().GetResult();
            return (ValueResult)promiseResult;
        }
        catch (Exception ex)
        {
            throw Error($"Promise rejected with error: {ex.Message}", 
                await.Location, Common.ErrorType.RuntimeError);
        }
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
                throw Error($"Not enough arguments for function", null, Common.ErrorType.RuntimeError);
            }

            // check number of arguments is at most equal to the number of parameters
            if (call.Arguments.Length > function.Parameters.Count)
    {
        throw Error($"Too many arguments for function", null, Common.ErrorType.RuntimeError);
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
                    throw Error($"Cannot pass argument of type '{argument.Type}' to parameter of type '{parameter.Type}'", call.Arguments[i].Location, Common.ErrorType.TypeError);
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
                throw Error($"Cannot call unknown function type '{function.GetType()}'", call.Location, Common.ErrorType.RuntimeError);
            }
        }
        else
        {
            throw Error($"Cannot call non-callable of type '{callee.Type}'", call.Location, Common.ErrorType.RuntimeError);
        }
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

    public IEvaluationResult Visit(Instantiation instantiation)
    {
        Call call = instantiation.Call;
        IEvaluationResult clazzResult = Evaluate(call.Callee);

        // make sure it's a Class type
        if (clazzResult.Type != ZenType.Class)
        {
            throw Error($"Cannot instantiate non-class type '{clazzResult.Type}'", instantiation.Location, Common.ErrorType.TypeError);
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

    public IEvaluationResult Visit(TypeHint typeHint)
    {
        // check if the base type is a primitive type under ZenType
        if (typeHint.IsPrimitive()) {
            return (TypeResult) typeHint.GetBaseZenType();
        }else {
            // TypeHint refers to a class?
            VariableResult variable = LookUpVariable(typeHint.Name, typeHint);

            if (variable.Type == ZenType.Class) {
                ZenClass clazz = (ZenClass) variable.Value.Underlying!;

                return (TypeResult) clazz.Type;
    }
    }

        return (TypeResult) typeHint.GetBaseZenType();
    }

    public IEvaluationResult Visit(TypeCheck typeCheck)
    {
        IEvaluationResult exprResult = Evaluate(typeCheck.Expression);
        ZenType sourceType;
        ZenClass? sourceClass = null;

        // if it's a variable, we need to get the type of the current value - not the type of the variable
        if (exprResult is VariableResult variableResult) {
            // if it's an object, get the class
            sourceType = variableResult.Value.Type;
            if (sourceType == ZenType.Object) {
                ZenObject sourceObject = (ZenObject)variableResult.Value.Underlying!;
                sourceClass = sourceObject.Class;
            }
        }else {
            sourceType = exprResult.Type;
        }

        // If the expression evaluates to a type, it's a type-to-type comparison which we don't allow
        if (sourceType == ZenType.Type)
        {
            throw Error($"Invalid type check: 'is' operator cannot be used for type-to-type comparisons. Use '==' instead.", 
                typeCheck.Token.Location, Common.ErrorType.TypeError);
        }

        
        // If the expression evaluates to a class, it's a class comparison
        if (sourceClass != null) {
            // get target class
            TypeResult targetTypeResult = (TypeResult) Evaluate(typeCheck.Type);
            
            if ( ! targetTypeResult.IsClass()) {
                throw Error($"Invalid type check. Comparison to non-class type '{targetTypeResult.Type}'", 
                    typeCheck.Token.Location, Common.ErrorType.TypeError);
            }

            ZenTypeClass targetClassType = (ZenTypeClass) targetTypeResult.Type;
            ZenClass targetClass = targetClassType.Clazz;

            bool isCompatible = sourceClass.IsAssignableFrom(targetClass);
            if (isCompatible) {
                return (ValueResult) ZenValue.True;
            }else {
                return (ValueResult) ZenValue.False;
            }
        }else {
            ZenType targetType = typeCheck.Type.GetZenType();
            // we're comparing a value to a type
            bool isCompatible = targetType.IsAssignableFrom(sourceType);
            if (isCompatible) {
                return (ValueResult) ZenValue.True;
            }else {
                return (ValueResult) ZenValue.False;
            }
        }
    }
        
    public IEvaluationResult Visit(TypeCast typeCast)
    {
        IEvaluationResult exprResult = Evaluate(typeCast.Expression);
        ZenType targetType = typeCast.Type.GetZenType();

        try
        {
            ZenValue convertedValue = TypeConverter.Convert(exprResult.Value, targetType);
        return (ValueResult)convertedValue;
        }
        catch (Exception)
        {
            throw Error($"Cannot cast value of type '{exprResult.Type}' to type '{targetType}'", 
                typeCast.Token.Location, Common.ErrorType.TypeError);
        }
    }
}