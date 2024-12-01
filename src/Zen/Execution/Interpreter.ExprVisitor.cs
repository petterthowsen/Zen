using System.Linq.Expressions;
using Zen.Common;
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

        if (result.Value.Underlying is ZenObject instance)
        {
            // is it a method?
            ZenMethod? method;
            instance.HasMethodHierarchically(get.Identifier.Value, out method);

            if (method != null)
            {
                return (ValueResult) method.Bind(instance);
            }

            if (!instance.HasProperty(get.Identifier.Value)) {
                throw Error($"Undefined property '{get.Identifier.Value}' on object of type '{instance.Type}'", get.Identifier.Location, Common.ErrorType.UndefinedProperty);
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

        if (objectValue.Underlying is ZenObject instance)
        {
            if (!instance.HasProperty(propertyName)) {
                throw Error($"Undefined property '{propertyName}' on object of type '{instance.Type}'", set.Identifier.Location, Common.ErrorType.UndefinedProperty);
            }

            // evaluate the value expression
            IEvaluationResult valueExpression = Evaluate(set.ValueExpression);

            // Get the property's type and value's type for comparison
            ZenValue propertyValue = instance.GetProperty(propertyName);
            
            // Check type compatibility including type parameters
            if (!TypeChecker.IsCompatible(valueExpression.Value.Type, propertyValue.Type))
            {
                throw Error($"Cannot assign value of type '{valueExpression.Value.Type}' to target of type '{propertyValue.Type}'", 
                    set.Identifier.Location, Common.ErrorType.TypeError);
            }

            // perform assignment
            ZenValue newValue = PerformAssignment(set.Operator, propertyValue, valueExpression.Value);

            // set the property
            instance.SetProperty(propertyName, newValue);

            return (ValueResult)newValue;
        }
        else
        {
            throw Error($"Cannot set property of non-object type '{objectValue.Type}'", set.Identifier.Location, Common.ErrorType.TypeError);
        }
    }

  public IEvaluationResult Visit(BracketGet bracketGet)
    {
        IEvaluationResult target = Evaluate(bracketGet.Target);
        IEvaluationResult element = Evaluate(bracketGet.Element);

        if (target.Value.Underlying is not ZenObject instance)
        {
            throw Error($"Cannot use bracket access on non-object type '{target.Type}'", bracketGet.Location);
        }

        // Call the get method
        ZenMethod? method;
        instance.HasMethodHierarchically("get", out method);

        if (method == null)
        {
            throw Error($"Object does not support bracket access (missing 'get' method)", bracketGet.Location);
        }

        return (ValueResult)instance.Call(this, method, [new ZenValue(instance.Type, instance), element.Value]);
    }

    public IEvaluationResult Visit(BracketSet bracketSet)
    {
        IEvaluationResult target = Evaluate(bracketSet.Target);
        IEvaluationResult element = Evaluate(bracketSet.Element);
        IEvaluationResult value = Evaluate(bracketSet.ValueExpression);

        if (target.Value.Underlying is not ZenObject instance)
        {
            throw Error($"Cannot use bracket access on non-object type '{target.Type}'", bracketSet.Location);
        }

        // Call the set method
        ZenMethod? method;
        instance.HasMethodHierarchically("set", out method);

        if (method == null)
        {
            throw Error($"Object does not support bracket assignment (missing 'set' method)", bracketSet.Location);
        }

        return (ValueResult)instance.Call(this, method, [target.Value, element.Value, value.Value]);
    }

    public IEvaluationResult Visit(Parameter parameter)
    {
        // For type parameters, just evaluate the type
        if (parameter.IsTypeParameter)
        {
            return Evaluate(parameter.Type);
        }

        // For value constraints, evaluate both type and default value if present
        IEvaluationResult type = Evaluate(parameter.Type);
        
        if (parameter.DefaultValue != null)
        {
            IEvaluationResult defaultValue = Evaluate(parameter.DefaultValue);
            
            // Verify default value matches the type
            if (!TypeChecker.IsCompatible(defaultValue.Type, type.Type))
            {
                throw Error($"Default value of type '{defaultValue.Type}' is not compatible with parameter type '{type.Type}'", 
                    parameter.Location);
            }
        }

        return type;
    }

    // public IEvaluationResult Visit(BracketSet bracketSet)

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
            // Create a new promise that will be resolved when the task completes
            var waitPromise = new ZenPromise(environment, promise.ResultType);
            var completed = false;
            var promiseResult = ZenValue.Void;

            // Add callbacks to handle resolution/rejection
            promise.Then(() => 
            {
                promiseResult = promise.AsTask().GetAwaiter().GetResult();
                completed = true;
            });

            promise.Catch(() => 
            {
                completed = true;
            });

            // Wait for the promise to complete
            while (!completed)
            {
                Thread.Sleep(1);
            }

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
            if (call.Arguments.Length < function.Arguments.Count(p => !p.Nullable))
            {
                throw Error($"Not enough arguments for function", null, Common.ErrorType.RuntimeError);
            }

            // check number of arguments is at most equal to the number of parameters
            if (call.Arguments.Length > function.Arguments.Count)
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
                ZenFunction.Argument parameter = function.Arguments[i];
                ZenValue argument = argumentValues[i];

                if (!TypeChecker.IsCompatible(argument.Type, parameter.Type))
                {
                    throw Error($"Cannot pass argument of type '{argument.Type}' to parameter of type '{parameter.Type}'", call.Arguments[i].Location, Common.ErrorType.TypeError);
                }
            }

            return CallFunction(function, argumentValues);
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


        ZenFunction.Argument parameter = new(name, type, nullable);

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

        Logger.Instance.Debug($"Class parameters: {string.Join(", ", clazz.Parameters.Select(p => $"{p.Name} (Type: {p.Type})"))}");
        Logger.Instance.Debug($"Instantiation parameters: {string.Join(", ", instantiation.Parameters.Select(p => p.ToString()))}");

        // Evaluate constructor arguments
        List<ZenValue> arguments = [];
        foreach (var argument in call.Arguments)
        {
            arguments.Add(Evaluate(argument).Value);
        }

        // Evaluate parameters and build parameter values dictionary
        Dictionary<string, ZenValue> paramValues = [];
        
        // If no parameters provided but class has type parameters,
        // use 'any' as default type parameter
        if (instantiation.Parameters.Count == 0 && clazz.Parameters.Count > 0)
        {
            Logger.Instance.Debug("No parameters needed.");
        }
        else
        {
            Logger.Instance.Debug($"Processing {instantiation.Parameters.Count} parameters");

            int paramIndex = 0;
            foreach (ZenClass.Parameter param in clazz.Parameters)
            {
                if (paramIndex >= instantiation.Parameters.Count) {
                    // provides default value ?
                    if (param.DefaultValue.IsNull() == false) {
                        ZenValue val = (ZenValue) param.DefaultValue;
                        paramValues[param.Name] = val;
                    }else {
                        throw Error($"{clazz.Name} expects a '{param.Name}' parameter.!", 
                            instantiation.Location);
                    }
                }else {
                    Expr valueExpr = instantiation.Parameters[paramIndex];
                    IEvaluationResult paramResult = Evaluate(valueExpr);
                    
                    // For type parameters (like T)
                    if (param.IsTypeParameter) {
                        // If it's a TypeResult
                        if (paramResult is TypeResult typeResult) {
                            paramValues[param.Name] = typeResult.Value;
                        }
                        // If it's a variable that holds a type (like 'string')
                        else if (paramResult is VariableResult varResult && varResult.Value.Type == ZenType.Type) {
                            paramValues[param.Name] = varResult.Value;
                        }
                        // If it's a class type
                        else if (paramResult.Type == ZenType.Class) {
                            ZenClass resultingClass = paramResult.Value.Underlying!;
                            var baseType = new ZenTypeClass(resultingClass, resultingClass.Name);
                            paramValues[param.Name] = new ZenValue(ZenType.Type, baseType);
                        }
                        else {
                            throw Error($"{clazz.Name} expects parameter '{param.Name}' to be a Type. You passed a {paramResult.Type}!", 
                                valueExpr.Location, ErrorType.TypeError);
                        }
                    }
                    // For value parameters (like SIZE:int)
                    else {
                        ZenValue val = paramResult.Value;
                        
                        // type check the value
                        if (!param.Type.IsAssignableFrom(val.Type)) {
                            throw Error($"{clazz.Name} expects parameter '{param.Name}' to be a {param.Type}. You passed a '{val.Type}'!",
                                valueExpr.Location, ErrorType.TypeError);
                        }
    
                        paramValues[param.Name] = val;
                    }
                }
                paramIndex++;
            }

            // Check if we have all required parameters
            foreach (ZenClass.Parameter param in clazz.Parameters)
            {
                if (!paramValues.ContainsKey(param.Name))
                {
                    Logger.Instance.Debug($"Missing parameter {param.Name}");
                    if (param.DefaultValue.IsNull() == false)
                    {
                        paramValues[param.Name] = param.DefaultValue;
                        Logger.Instance.Debug($"Using default value for {param.Name}: {param.DefaultValue}");
                    }
                    else
                    {
                        throw Error($"No value provided for parameter '{param.Name}' and it has no default value", 
                            instantiation.Location);
                    }
                }
            }
        }

        Logger.Instance.Debug($"Final parameter values: {string.Join(", ", paramValues.Select(kv => $"{kv.Key}: {kv.Value}"))}");

        // create new instance with both constructor args and parameter values
        ZenObject instance = clazz.CreateInstance(this, [.. arguments], paramValues);

        // wrap it in a ZenValue with the instance's specific type
        ZenValue result = new ZenValue(instance.Type, instance);

        // return as ValueResult (IEvaluationResult)
        return (ValueResult) result;
    }

    public IEvaluationResult Visit(TypeHint typeHint)
    {
        // For primitive types like 'string', return a TypeResult with the base type
        // don't need to do this, as all primtiives are global variables, see below...
        // if (typeHint.IsPrimitive()) {
        //     var baseType = typeHint.GetBaseZenType();
        //     return new TypeResult(baseType);
        // }

        // Look up if this resolves to a known type.
        // For class types, look up the class and return its type
        VariableResult variable = LookUpVariable(typeHint.Name, typeHint);
        if (variable.Type == ZenType.Class) {
            ZenClass clazz = (ZenClass)variable.Value.Underlying!;
            ZenType type = clazz.Type;
            if (typeHint.Nullable)
            {
                type = type.MakeNullable();
            }
            return new TypeResult(type, typeHint.Nullable);
        }else {
            return new TypeResult(variable.Value, typeHint.Nullable); // variable.Type == ZenType.Type while variable.value is ZenType.String|Int etc
        }
    }

    public IEvaluationResult Visit(TypeCheck typeCheck)
    {
        IEvaluationResult exprResult = Evaluate(typeCheck.Expression);
        ZenType sourceType = exprResult.Type;
        ZenClass? sourceClass = null;

        // if it's an object, get the class
        if (sourceType == ZenType.Object) {
            ZenObject sourceObject = (ZenObject)exprResult.Value.Underlying!;
            sourceClass = sourceObject.Class;
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
            // get target
            TypeResult targetTypeResult = (TypeResult) Evaluate(typeCheck.Type);
            ZenType targetType = targetTypeResult.Type;
            
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
        TypeResult targetTypeResult = (TypeResult) Evaluate(typeCast.Type);
        ZenType targetType = targetTypeResult.Type;

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
