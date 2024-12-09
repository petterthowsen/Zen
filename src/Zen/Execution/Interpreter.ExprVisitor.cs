using Zen.Common;
using Zen.Execution.EvaluationResult;
using Zen.Parsing.AST;
using Zen.Parsing.AST.Expressions;
using Zen.Typing;

namespace Zen.Execution;

public partial class Interpreter
{
    private async Task<IEvaluationResult> Evaluate(Expr expr)
    {
        return await expr.AcceptAsync(this)!;
    }

    public async Task<IEvaluationResult> VisitAsync(Binary binary)
    {
        CurrentNode = binary;

        IEvaluationResult leftRes = await Evaluate(binary.Left);
        IEvaluationResult rightRes = await Evaluate(binary.Right);

        ZenValue left = leftRes.Value;
        ZenValue right = rightRes.Value;

        if (IsArithmeticOperator(binary.Operator.Type))
        {
            // For now, we only support numbers (int, long, float, double)
            // if (!left.IsNumber())
            // {
            //     throw Error($"Cannot use operator `{binary.Operator.Type}` on non-numeric value `{left}`", binary.Location);
            // }
            // else if (!right.IsNumber())
            // {
            //     throw Error($"Cannot use operator `{binary.Operator.Type}` on non-numeric value `{right}`", binary.Location);
            // }

            // Check operation validity and get result type
            // this throws an error if the operation is invalid
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

public async Task<IEvaluationResult> VisitAsync(Grouping grouping)
    {
        CurrentNode = grouping;

        return await Evaluate(grouping.Expression);
    }

    public async Task<IEvaluationResult> VisitAsync(Unary unary)
    {
        CurrentNode = unary;

        IEvaluationResult eval = await Evaluate(unary.Right);

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


    public async Task<IEvaluationResult> VisitAsync(Literal literal)
    {
        CurrentNode = literal;

        return (ValueResult)literal.Value;
    }

    public async Task<IEvaluationResult> VisitAsync(Identifier identifier)
    {
        CurrentNode = identifier;

        string name = identifier.Name;

        return LookUpVariable(name, identifier);
    }

    public async Task<IEvaluationResult> VisitAsync(Get get)
    {
        CurrentNode = get;

        // evaluate the object expression
        IEvaluationResult result = await Evaluate(get.Expression);

        if (result.Value.Underlying is ZenObject instance)
        {
            // is it a method?
            ZenFunction? method = instance.GetMethodHierarchically(get.Identifier.Value);

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

    public async Task<IEvaluationResult> VisitAsync(Set set) {
        CurrentNode = set;

        // evaluate the object expression
        IEvaluationResult objectExpression = await Evaluate(set.ObjectExpression);
        string propertyName = set.Identifier.Value;

        // get the object
        ZenValue objectValue = objectExpression.Value;

        if (objectValue.Underlying is ZenObject instance)
        {
            if (!instance.HasProperty(propertyName)) {
                throw Error($"Undefined property '{propertyName}' on object of type '{instance.Type}'", set.Identifier.Location, Common.ErrorType.UndefinedProperty);
            }

            // evaluate the value expression
            IEvaluationResult valueExpression = await Evaluate(set.ValueExpression);

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

    public async Task<IEvaluationResult> VisitAsync(BracketGet bracketGet)
    {
        CurrentNode = bracketGet;

        IEvaluationResult target = await Evaluate(bracketGet.Target);
        IEvaluationResult element = await Evaluate(bracketGet.Element);

        if (target.Value.Underlying is not ZenObject instance)
        {
            throw Error($"Cannot use bracket access on non-object type '{target.Type}'", bracketGet.Location);
        }

        // Call the get method
        ZenFunction? method = instance.GetMethodHierarchically("_BracketGet");

        if (method == null)
        {
            throw Error($"Object of type '{instance.Type}' does not support BracketGet (missing '_BracketGet' method)", bracketGet.Location);
        }

        BoundMethod bound = method.Bind(instance);

        return (ValueResult) await CallFunction(bound, [element.Value]);
    }

    public async Task<IEvaluationResult> VisitAsync(BracketSet bracketSet)
    {
        CurrentNode = bracketSet;

        IEvaluationResult target = await Evaluate(bracketSet.Target);
        IEvaluationResult element = await Evaluate(bracketSet.Element);
        IEvaluationResult value = await Evaluate(bracketSet.ValueExpression);

        if (target.Value.Underlying is not ZenObject instance)
        {
            throw Error($"Cannot use bracket access on non-object type '{target.Type}'", bracketSet.Location);
        }

        // Call the set method
        ZenFunction? method = instance.GetMethodHierarchically("_BracketSet");

        if (method == null)
        {
            throw Error($"Object of type '{instance.Type}' does not support BracketSet (missing '_BracketSet' method)", bracketSet.Location);
        }

        BoundMethod bound = method.Bind(instance);

        return (ValueResult) await CallFunction(bound, [element.Value, value.Value]);
    }

    public async Task<IEvaluationResult> VisitAsync(ParameterDeclaration parameter)
    {
        CurrentNode = parameter;

        // For type parameters, we just return ZenType.Type
        if (parameter.IsTypeParameter)
        {
            return (TypeResult) ZenType.Type;
        }

        // For non-type parameters (constraints), evaluate the type
        IEvaluationResult type = await Evaluate(parameter.Type);
        
        // default value?
        if (parameter.DefaultValue != null)
        {
            IEvaluationResult defaultValue = await Evaluate(parameter.DefaultValue);
            
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

    public async Task<IEvaluationResult> VisitAsync(Logical logical)
    {
        CurrentNode = logical;

        IEvaluationResult left = await Evaluate(logical.Left);

        if (logical.Token.Value == "or")
        {
            if (left.IsTruthy()) return left;
        }
        else
        {
            if (!left.IsTruthy()) return left;
        }

        return await Evaluate(logical.Right);
    }

    public async Task<IEvaluationResult> VisitAsync(This dis)
    {
        CurrentNode = dis;

        return LookUpVariable("this", dis);
    }

    public async Task<IEvaluationResult> VisitAsync(Await awaitNode)
    {
        CurrentNode = awaitNode;

        Logger.Instance.Debug($"AWAIT {awaitNode} with expression: {awaitNode.Expression}...");

        ZenValue value = (await Evaluate(awaitNode.Expression)).Value;
        Logger.Instance.Debug($"value: {value}");

        // we can only await ZenType.Task objects
        if (!value.Type.IsTask)
            throw Error($"Cannot await non-task value of type {value.Type}", awaitNode.Location);

        // get the task
        Task<ZenValue> task = value.Underlying!;
        Logger.Instance.Debug($"awaiting task: {task}");
        try {
            ZenValue result = await task;

            Logger.Instance.Debug($"Awaiting task complete with result: {result}");

            return (ValueResult) result; // Return the final result, not another Task
        } catch (Exception ex) {
            // this works, we do get the narrowing conversion exception
            // and throw it
            // If it's our RuntimeError, rethrow it directly
            if (ex is RuntimeError runtimeError)
            {
                throw runtimeError;
            }
            // If it's an AggregateException (which wraps task exceptions), unwrap it
            else if (ex is AggregateException aggEx && aggEx.InnerExceptions.Count == 1)
            {
                if (aggEx.InnerException is RuntimeError innerRuntimeError)
                {
                    throw innerRuntimeError;
                }
            }
            // Otherwise wrap it in a RuntimeError
            throw Error($"Task failed: {ex.Message}", awaitNode.Location, ErrorType.RuntimeError, ex);
        }
    }

    public async Task<IEvaluationResult> EvaluateGetMethod(Get get, ZenValue[] argValues)
    {
        CurrentNode = get;

        IEvaluationResult result = await Evaluate(get.Expression);
        ZenFunction? method;

        // object?
        if (result.Value.Underlying is ZenObject instance) // this may be a regular ZenObject or a ZenObjectProxy which proxies for a .NET object
        {
            // is it a method?
            method = instance.GetMethodHierarchically(get.Identifier.Value, argValues);
            if (method != null) {
                return (ValueResult) method.Bind(instance); // implicitly creates a ZenValue of type ZenType.BoundMethod
            }
        }
        // string?
        else if (result.Type == ZenType.String) {
            var argsWithObj = argValues.ToList();
            argsWithObj.Insert(0, result.Value);
            var args = argsWithObj.ToArray();

            method = Builtins.Core.String.ZenString.GetOwnMethod(get.Identifier.Value, args);
            
            if (method != null && method.IsStatic) {
                return new PrimitiveMethodResult(result.Value, method, args);
            }
        }
        
        throw Error($"Cannot find method '{get.Identifier.Value}' on '{result.Type}' with argument types '{string.Join<ZenValue>(", ", argValues)}'", get.Identifier.Location, Common.ErrorType.TypeError);
    }


    public async Task<IEvaluationResult> VisitAsync(Call call)
    {
        CurrentNode = call;

        IEvaluationResult callee;
        var argTasks = call.Arguments.Select(async e => (await Evaluate(e)).Value);
        ZenValue[] argValues = await Task.WhenAll(argTasks);

        // is the callee a Get Expression?
        if (call.Callee is Get get)
        {
            // resolve the get expression
            callee = await EvaluateGetMethod(get, argValues);
        }else {
            // evaluate the callee
            callee = await Evaluate(call.Callee);
        }
        
        if (callee.IsCallable() == false) {
            throw Error($"Cannot call non-callable of type '{callee.Type}'", call.Location, Common.ErrorType.RuntimeError);
        }

        if (callee is PrimitiveMethodResult pmr)
        {
            ZenFunction method = pmr.Method;
            ZenValue[] args = pmr.Arguments;

            // check number of arguments is at most equal to the number of parameters
            if (method.Variadic == false && args.Length > method.Arguments.Count)
            {
                throw Error($"Too many arguments for function {method}", call.Location, Common.ErrorType.RuntimeError);
            }

            return await CallFunction(method, args);
        }
        else if (callee.Value.Underlying is ZenFunction function) {
            // check number of arguments is at least equal to the number of non-nullable parameters
            if (argValues.Length < function.Arguments.Count(p => !p.Nullable))
            {
                throw Error($"Not enough arguments for function", null, Common.ErrorType.RuntimeError);
            }

            // check number of arguments is at most equal to the number of parameters
            if (function.Variadic == false && argValues.Length > function.Arguments.Count)
            {
                throw Error($"Too many arguments for function {function}", call.Location, Common.ErrorType.RuntimeError);
            }

            return await CallFunction(function, argValues);
        }
        else if (callee.Value.Underlying is BoundMethod) {
            BoundMethod boundMethod = (BoundMethod)callee.Value.Underlying;
            return await CallFunction(boundMethod, argValues);
        }
        else
        {
            throw Error($"Cannot call non-callable of type '{callee.Type}'", call.Location, Common.ErrorType.RuntimeError);
        }
    }

    public async Task<IEvaluationResult> VisitAsync(FuncParameter funcParameter)
    {
        CurrentNode = funcParameter;

        // parameter name
        string name = funcParameter.Identifier.Value;

        // parameter type
        ZenType type = ZenType.Any;
        bool nullable = true;

        // type hint?
        if (funcParameter.TypeHint != null)
        {
            type = (await Evaluate(funcParameter.TypeHint)).Type;
            nullable = funcParameter.TypeHint.Nullable;
        }

        ZenFunction.Argument parameter = new(name, type, nullable);

        return (FunctionParameterResult)parameter;
    }

    public async Task<IEvaluationResult> VisitAsync(Instantiation instantiation)
    {
        CurrentNode = instantiation;

        Call call = instantiation.Call;
        IEvaluationResult clazzResult = await Evaluate(call.Callee);

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
            arguments.Add((await Evaluate(argument)).Value);
        }

        // Evaluate generic<parameters> and build parameter values dictionary
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
            // loop through class parameters
            foreach (IZenClass.Parameter param in clazz.Parameters)
            {
                // no more parameters?
                if (paramIndex >= instantiation.Parameters.Count) {
                    // provides default value ?
                    if (param.DefaultValue.IsNull()) {
                        throw Error($"{clazz.Name} expects a '{param.Name}' parameter.!", 
                            instantiation.Location);
                    }

                    ZenValue val = param.DefaultValue;
                    paramValues[param.Name] = val;
                }else {
                    // evaluate the parameter
                    Expr valueExpr = instantiation.Parameters[paramIndex];
                    IEvaluationResult paramResult = await Evaluate(valueExpr);
                    
                    // For type parameters (like "T" or "T:Type")
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
                        else if (paramResult.Type.IsClass) {
                            IZenClass resultingClass = paramResult.Value.Underlying!;
                            paramValues[param.Name] = new ZenValue(ZenType.Type, ZenType.FromClass(resultingClass));
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
            foreach (IZenClass.Parameter param in clazz.Parameters)
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

    public ZenType ResolveTypeHint(TypeHint typeHint) {
        // if the typeHint is "T", just return a generic type
        if (typeHint.IsGeneric) {
            // todo: handle nullable
            return ZenType.GenericParameter(typeHint.Name);
        }

        // For concrete types, we need to resolve the type by looking up the variable
        // look up the variable
        // The resolver has already resolved this.
        // The variable is a ZenValue but its type can resolve to a ZenType.Type, ZenType.Class, ZenType.Interface etc.
        // We need to handle these cases.
        ZenValue val = LookUpVariable(typeHint.Name, typeHint).Value;
        ZenType type;

        // in the case of primitives like 'string', 'int' etc. These are stored as a ZenValue with type 'Type' (ZenType.Type):
        // in this case, we simply get the underlying ZenType (ZenType.String, or other custom types.)
        if (val.Type == ZenType.Type) {
            type = val.Underlying!;
        }
        else if (val.Type.IsPrimitive) {
            type = val.Type;
        }
        else if (val.Type == ZenType.Class || val.Type == ZenType.Interface) {
            IZenClass clazz = val.Underlying!;
            type = ZenType.FromClass(clazz);
        }
        else if (val.Type == ZenType.Object) {
            throw Error($"'{typeHint.Name}' is an object, not a class, interface or primitive type.",
                typeHint.Location);
        }else {
            throw Error($"Type '{typeHint.Name}' is a {val.Type}! Should be a class, interface or primitive type.", 
                typeHint.Location);
        }

        // resolve the parameters
        if (typeHint.Parameters.Length > 0) {

            // check if the resolved type has the same number of parameters
            if (type.Parameters.Length != typeHint.Parameters.Length) {
                throw Error($"Type '{type.Name}' expects {type.Parameters.Length} parameters, not {typeHint.Parameters.Length}",
                    typeHint.Location);
            }

            // resolve the parameters
            for (int i = 0; i < typeHint.Parameters.Length; i++) {
                TypeHint param = typeHint.Parameters[i];
                type.Parameters[i] = ResolveTypeHint(param);
            }
        }

        return type;        
    }
    
    public async Task<IEvaluationResult> VisitAsync(TypeHint typeHint)
    {
        CurrentNode = typeHint;

        return (TypeResult) ResolveTypeHint(typeHint);
    }

    public async Task<IEvaluationResult> VisitAsync(TypeCheck typeCheck)
    {
        CurrentNode = typeCheck;

        IEvaluationResult exprResult = await Evaluate(typeCheck.Expression);
        ZenType sourceType = exprResult.Type;

        // If the expression evaluates to a type, it's a type-to-type comparison which we don't allow
        if (sourceType == ZenType.Type)
        {
            throw Error($"Invalid type check: 'is' operator cannot be used for type-to-type comparisons. Use '==' instead.", 
                typeCheck.Token.Location, Common.ErrorType.TypeError);
        }
        
        // Resolve the target type from the type hint
        TypeResult targetTypeResult = (TypeResult) await Evaluate(typeCheck.Type);
        ZenType targetType = targetTypeResult.Type;

        // Now perform the assignability check in the correct direction:
        return (ValueResult) targetType.IsAssignableFrom(sourceType);
    }
        
    public async Task<IEvaluationResult> VisitAsync(TypeCast typeCast)
    {
        CurrentNode = typeCast;

        IEvaluationResult exprResult = await Evaluate(typeCast.Expression);
        TypeResult targetTypeResult = (TypeResult) await Evaluate(typeCast.Type);
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

    public async Task<IEvaluationResult> VisitAsync(ImplementsExpr implementsExpr)
    {
        // do nothing
        return VoidResult.Instance;
    }
}