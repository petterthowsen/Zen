using Zen.Common;
using Zen.Execution.Builtins.Core;
using Zen.Execution.EvaluationResult;
using Zen.Parsing.AST;
using Zen.Parsing.AST.Expressions;
using Zen.Typing;

namespace Zen.Execution;

public partial class Interpreter
{
    private async Task<IEvaluationResult> Evaluate(Expr expr)
    {
        return await expr.AcceptAsync(this);
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

    public async Task<IEvaluationResult> EvaluateArrayLiteral(ArrayLiteral arrayLiteral, ZenType? defaultTypeParam)
    {
        // evaluate each item
        List<ZenValue> itemValues = [];
        
        // store types that occur in the array literal
        List<ZenType> itemTypes = [];

        foreach (var item in arrayLiteral.Items)
        {
            IEvaluationResult itemResult = await Evaluate(item);
            
            // must be a non-void value
            if (itemResult is VoidResult) {
                throw Error("Array items cannot be void", item.Location);
            }

            // add the value
            itemValues.Add(itemResult.Value);

            // add the type to itemTypes if not already present
            if (!itemTypes.Contains(itemResult.Value.Type)) {
                itemTypes.Add(itemResult.Value.Type);
            }
        }

        // infer the type of the array
        // if there is more than one type, use Any
        // otherwise, use the type encountered.
        ZenType typeParam;
        if (itemTypes.Count == 1) {
            typeParam = itemTypes[0];
        }else if (itemTypes.Count > 1) {
            typeParam = ZenType.Any;
        }else {
            typeParam = defaultTypeParam ?? ZenType.Any;
        }

        return (ValueResult) Builtins.Core.Array.CreateInstance(this, [..itemValues], typeParam);
    }

    public async Task<IEvaluationResult> VisitAsync(ArrayLiteral arrayLiteral)
    {
        CurrentNode = arrayLiteral;

        return await EvaluateArrayLiteral(arrayLiteral, null);
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
        else if (result.Type == ZenType.String) {

            ZenFunction getPropertyFunc = Builtins.Core.String.StringClass.GetOwnMethod("_GetProperty")!;

            return await CallFunction(getPropertyFunc, [
                new(ZenType.String, result.Value.Underlying!),
                new(ZenType.String, get.Identifier.Value)]
            );
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

            // Get the property's type and value's type for comparison
            ZenValue propertyValue = instance.GetProperty(propertyName);

            // evaluate the value expression
            IEvaluationResult valueExpression;
            if (set.ValueExpression is ArrayLiteral arrayLiteralExpr) {
                ZenType arrayTypeParam = propertyValue.Type.Parameters[0];
                valueExpression = await EvaluateArrayLiteral(arrayLiteralExpr, arrayTypeParam);
            }else {
                valueExpression = await Evaluate(set.ValueExpression);
            }

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

        Logger.Instance.Debug($"await {awaitNode} with expression: {awaitNode.Expression}...");

        ZenValue value = (await Evaluate(awaitNode.Expression)).Value;

        ZenType PromiseType = globalEnvironment.GetClass("Promise").Type;

        // we can only await ZenType.Task objects
        if (value.Type.IsTask == false && TypeChecker.IsCompatible(value.Type, PromiseType) == false)
            throw Error($"Cannot await non-task value of type {value.Type}", awaitNode.Location);

        // get the task
        Task<ZenValue> task;

        if (value.Type.IsTask) {
            task = value.Underlying!;
        }else {
            // promise
            ZenObject promiseObject = value.Underlying!;
            ZenValue promiseZenTask = promiseObject.GetProperty("Task");
            task = promiseZenTask.Underlying!;
        }

        Logger.Instance.Debug($"awaiting task: {task}");
    
        ZenValue result = await task;

        return (ValueResult) result; // Return the final result, not another Task
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

            // callable property?
            if (instance.HasProperty(get.Identifier.Value)) {
                ZenValue property = instance.GetProperty(get.Identifier.Value);
                if (property.IsCallable()) {
                    return (ValueResult) property;
                }
            }
        }
        // string?
        else if (result.Type == ZenType.String) {
            var argsWithObj = argValues.ToList();
            argsWithObj.Insert(0, result.Value);
            var args = argsWithObj.ToArray();

            method = Builtins.Core.String.StringClass.GetOwnMethod(get.Identifier.Value, args);
            
            if (method != null && method.IsStatic) {
                return new PrimitiveMethodResult(result.Value, method, args);
            }
        }
        // static method?
        else if (result.Type is ZenType) {
            ZenType type = result.Value.Underlying!;
            if (type.IsClass) {
                ZenClass clazz = (ZenClass) type.Clazz!;

                method = clazz.GetOwnMethod(get.Identifier.Value, argValues);
                if (method != null && method.IsStatic) {
                    return (ValueResult) new ZenValue(ZenType.Method, method);
                }
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

            return await CallFunction(method, args);
        }
        else if (callee.Value.Underlying is ZenFunction function) {
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

        // default value?
        ZenValue? defaultValue = null;
        if (funcParameter.DefaultValue != null)
        {
            defaultValue = (await Evaluate(funcParameter.DefaultValue)).Value;
        }

        ZenFunction.Argument parameter = new(name, type, nullable, defaultValue);

        return (FunctionParameterResult)parameter;
    }

    public async Task<IEvaluationResult> VisitAsync(Instantiation instantiation)
    {
        CurrentNode = instantiation;

        Call call = instantiation.Call;
        IEvaluationResult clazzResult = await Evaluate(call.Callee);

        if (clazzResult.Type != ZenType.Type) {
            throw Error($"Cannot instantiate non-type value '{clazzResult.Type}'", instantiation.Location, Common.ErrorType.TypeError);
        }

        ZenType clazzType = (ZenType) clazzResult.Value.Underlying!;

        // make sure it's a Class type
        if (clazzType.Kind != ZenTypeKind.Class)
        {
            throw Error($"Cannot instantiate non-class type '{clazzType}'", instantiation.Location, Common.ErrorType.TypeError);
        }

        // get the underlying ZenClass
        ZenClass clazz = (ZenClass) clazzType.Clazz!;

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
        if (instantiation.Parameters.Count == 0 && clazz.Parameters.Count == 0)
        {
            Logger.Instance.Debug("No parameters needed.");
        }
        else
        {
            Logger.Instance.Debug($"Processing {clazz.Parameters.Count} parameters");

            int paramIndex = 0;
            // loop through class parameters
            foreach (IZenClass.Parameter param in clazz.Parameters)
            {
                // no more parameters?
                if (paramIndex >= instantiation.Parameters.Count) {
                    // provides default value ?
                    if (param.DefaultValue == null) {
                        throw Error($"{clazz.Name} expects a {param.Type} parameter '{param.Name}' like `new {clazz.Name}<{param.Type}>()` ?", 
                            instantiation.Location);
                    }

                    ZenValue val = (ZenValue) param.DefaultValue;
                    paramValues[param.Name] = val;
                }else {
                    // evaluate the parameter
                    Expr valueExpr = instantiation.Parameters[paramIndex];
                    IEvaluationResult paramResult = await Evaluate(valueExpr);
                    
                    // For type parameters (like "T" or "T:Type")
                    if (param.IsTypeParameter) {
                        // If it's a TypeResult
                        if (paramResult.Type == ZenType.Type) {
                            paramValues[param.Name] = paramResult.Value;
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
                    if (param.DefaultValue != null)
                    {
                        paramValues[param.Name] = (ZenValue) param.DefaultValue;
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
            type = type.Copy();
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

        // syntax is
        // expr is type
        //
        // where expr is an expression and type is a type hint
        //
        // we need to check if expr can be assigned to type

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
        return (ValueResult) sourceType.IsAssignableFrom(targetType);
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