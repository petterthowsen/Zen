using System.Reflection;
using Zen.Common;
using Zen.Typing;

namespace Zen.Execution.Builtins.Core;

public class Interop : IBuiltinsProvider
{
    private static Interpreter? interpreter;
    
    public static readonly Dictionary<Type, ZenClassProxy> ProxyClasses = [];

    public static async Task RegisterBuiltins(Interpreter interpreter)
    {
        Interop.interpreter = interpreter;
    
        // Synchronous CallDotNet
        interpreter.RegisterHostFunction(
            "CallDotNet",
            ZenType.Any, // Returns any
            new List<ZenFunction.Argument>
            {
                new("target", ZenType.Any), // todo: use type union of string|DotNetObject
                new("method", ZenType.String),
            },
            CallDotNet,
            variadic: true
        );

        // Async CallDotNet
        interpreter.RegisterHostFunction(
            "CallDotNetAsync",
            ZenType.Task, // Returns any
            new List<ZenFunction.Argument>
            {
                new("target", ZenType.Any),
                new("method", ZenType.String),
            },
            CallDotNetAsync,
            variadic: true
        );

        // Get a property on a .NET object
        interpreter.RegisterHostFunction(
            "GetDotNetField",
            ZenType.Any, // Returns any
            new List<ZenFunction.Argument>
            {
                new("target", ZenType.String),
                new("property", ZenType.String),
            },
            GetDotNetField,
            variadic: false
        );


        await Task.CompletedTask;
    }

    public static ZenClassProxy GetOrCreateProxyClass(Type dotnetClass) {
        if ( ! ProxyClasses.ContainsKey(dotnetClass)) {
            ProxyClasses[dotnetClass] = new ZenClassProxy(dotnetClass);
        }
        return ProxyClasses[dotnetClass];
    }

    private static ZenValue GetDotNetField(ZenValue[] args) {
        if (args.Length < 2)
            throw new ArgumentException("GetDotNet requires at least two arguments: target and property name.");

        // Convert arguments to .NET-compatible values
        dynamic?[] dotNetArgs = args.Select(ToDotNet).ToArray();
        
        dynamic target = dotNetArgs[0]!;
        
        // get the property value
        FieldInfo propertyInfo = ResolveField(dotNetArgs);

        object? value;
        if (target is string) {
            value = propertyInfo.GetValue(null);
        }else {
            value = propertyInfo.GetValue(target);
        }

        return ToZen(value);
    }

    private static ZenValue CallDotNet(ZenValue[] args)
    {
        if (args.Length < 2)
            throw new ArgumentException("CallDotNet requires at least two arguments: target and method name.");
        
        // Convert arguments to .NET-compatible values
        dynamic?[] dotNetArgs = args.Select(ToDotNet).ToArray();

        // method args
        var target = dotNetArgs[0];
        var methodName = dotNetArgs[1];
        dynamic?[] methodArgs = dotNetArgs.Skip(2).ToArray();

        dynamic funcInfo = ResolveMethod(dotNetArgs);
        if (funcInfo is ConstructorInfo constructorInfo) {

            var instance = constructorInfo.Invoke(methodArgs);
            return ToZen(instance);

        }else if (funcInfo is MethodInfo methodInfo) {

            Logger.Instance.Debug($"Selected method: {methodInfo.Name}({string.Join(", ", methodInfo.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})");

            try
            {
                bool isObjectInstance = IsTargetObjectInstance(target);
                var callResult = methodInfo.Invoke(isObjectInstance ? target : null, methodArgs);
                return ToZen(callResult);
            }
            catch (Exception e)
            {
                Logger.Instance.Error($"Failed to call method {methodInfo.Name} on {target.GetType().Name}: {e.Message}");
                throw;
            }

        } else {

            throw new Exception($"Failed to resolve method {methodName} on {target.GetType().Name}");
        }
    }

    private static async Task<ZenValue> CallDotNetAsync(ZenValue[] args)
    {
        if (args.Length < 2)
            throw new ArgumentException("CallDotNet requires at least two arguments: target and method name.");
        
        // Convert arguments to .NET-compatible values
        dynamic?[] dotNetArgs = args.Select(ToDotNet).ToArray();

        // method args
        var target = dotNetArgs[0];
        var methodName = dotNetArgs[1];
        dynamic?[] methodArgs = dotNetArgs.Skip(2).ToArray();

        dynamic funcInfo = ResolveMethod(dotNetArgs);
        if (funcInfo is ConstructorInfo constructorInfo) {

            throw new Exception("Cannot call constructor asynchronously.");

        }else if (funcInfo is MethodInfo methodInfo) {

            Logger.Instance.Debug($"Selected method: {methodInfo.Name}({string.Join(", ", methodInfo.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})");

            try
            {
                bool isObjectInstance = IsTargetObjectInstance(target);
                var callResult = methodInfo.Invoke(isObjectInstance ? target : null, methodArgs);

                if (callResult is not Task) {
                    throw new Exception($"Method {methodInfo.Name} is not async.");
                }
                
                var task = (Task)callResult;

                await task;

                // Get the type of the task
                var taskType = task.GetType();

                // Check if the task is a generic Task<T>
                if (taskType.IsGenericType && taskType.GetGenericTypeDefinition() == typeof(Task<>))
                {
<<<<<<< HEAD
                    // Access the Result property via reflection
                    var resultProperty = taskType.GetProperty("Result");
                    if (resultProperty == null)
                    {
                        throw new Exception("Task<T> does not have a Result property.");
                    }

                    var taskResult = resultProperty.GetValue(task);
=======
                    // Use GetAwaiter().GetResult() to safely retrieve the result
                    var taskResult = task.GetType().GetProperty("Result")!.GetValue(task);
>>>>>>> aidev
                    return ToZen(taskResult);
                }

                // For non-generic Task that return void, return null
                return ZenValue.Null;
            }
            catch (Exception e)
            {
                Logger.Instance.Error($"Failed to call method {methodInfo.Name} on {target}: {e.Message}");
                throw;
            }

        } else {

            throw new Exception($"Failed to resolve method {methodName} on {target}");
        }
    }

    private static bool IsTargetObjectInstance(object? target)
    {
        return target is not string && target is not Type && target is not null;
    }

    private static async Task<ZenValue> WrapDotNetTask(Task task)
    {
        await task;

        // If it's a generic Task<T>, get its result
        if (task.GetType().IsGenericType)
        {
            var taskResult = ((dynamic)task).Result;
            return ToZen(taskResult);
        }

        // For non-generic Task, return null
        return ZenValue.Null;
    }

    private static FieldInfo ResolveField(dynamic?[] dotNetArgs)
    {
        // The target: either a string (type name) or an object
        object? target = dotNetArgs[0];

        // If the target is a string, resolve it to a Type
        if (target is string targetName)
        {
            Logger.Instance.Debug($"Resolving type: {targetName}...");

            // Attempt to resolve the type directly
            target = Type.GetType(targetName);

            if (target == null)
            {
                Logger.Instance.Debug($"Type '{targetName}' not found directly. Searching loaded assemblies...");

                // Search all loaded assemblies
                target = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName == targetName || t.Name == targetName);

                // Attempt to load missing assembly if still not found
                if (target == null)
                {
                    throw new ArgumentException($"Type '{targetName}' not found in loaded assemblies or system libraries.");
                }
            }
        }

        // Ensure the target is a Type object
        if (target is not Type t)
        {
            throw new ArgumentException("The target must be a Type or a string representing a type name.");
        }

        // Get the field name
        var fieldName = dotNetArgs[1];

        // Get the field value
        return t.GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dotNetArgs"></param>
    /// <returns>MethodInfo or ConstructorInfo</returns>
    /// <exception cref="ArgumentException"></exception>
    private static dynamic ResolveMethod(dynamic?[] dotNetArgs) 
    {
        // The target: either a string (type name) or an object
        object? target = dotNetArgs[0];
        if (target is not string && target != null)
        {
            Logger.Instance.Debug($"Using instance of type {target.GetType()} as target.");
        }
        else if (target is string targetName)
        {
            Logger.Instance.Debug($"Resolving type: {targetName}...");

            // Attempt to resolve the type directly
            target = Type.GetType(targetName);

            if (target == null)
            {
                Logger.Instance.Debug($"Type '{targetName}' not found directly. Searching loaded assemblies...");
                
                // Search all loaded assemblies
                target = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName == targetName || t.Name == targetName);

                // Attempt to load missing assembly if still not found
                if (target == null)
                {
                    throw new ArgumentException($"Type '{targetName}' not found in loaded assemblies or system libraries.");
                }
            }

            if (target == null)
                throw new ArgumentException($"Type '{targetName}' not found in loaded assemblies.");
            
            Logger.Instance.Debug($"Resolved type: {(target as Type)?.FullName}");
        }
        else
        {
            throw new ArgumentException("First argument must be a string (type name) or an object instance.");
        }

        // The method name
        string methodName = dotNetArgs[1] as string ?? throw new ArgumentException("Second argument must be a string (method name).");

        // The actual method arguments
        dynamic?[] methodArgs = dotNetArgs.Skip(2).ToArray();

        Logger.Instance.Debug($"Looking for method {methodName} on {(target is Type t ? t.FullName : target.GetType().FullName)}");
        Logger.Instance.Debug($"Method arguments: [{string.Join(", ", methodArgs.Select(a => $"{a?.GetType().Name ?? "null"}({a})"))}]");

        // Determine if the target is a type or an object
        var targetType = target as Type ?? target.GetType();

        // Find matching method
        // constructor?
        if (methodName == "new")
        {
            Logger.Instance.Debug($"Creating new instance of {targetType.FullName}...");

            try {
                // Find the constructor matching the argument types
                var constructors = targetType.GetConstructors();
                var constructor = constructors.FirstOrDefault(ctor => {
                    var parameters = ctor.GetParameters();
                    if (parameters.Length != methodArgs.Length)
                        return false;

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var paramType = parameters[i].ParameterType;
                        var argType = methodArgs[i]?.GetType();

                        // If argument is null, any reference type parameter is valid
                        if (methodArgs[i] == null && !paramType.IsValueType)
                            continue;

                        // Check if parameter type is assignable from argument type
                        if (argType != null && !paramType.IsAssignableFrom(argType))
                            return false;
                    }
                    return true;
                });

                if (constructor == null)
                {
                    Logger.Instance.Debug($"No matching constructor found for {targetType.FullName} with parameter types {string.Join(", ", methodArgs.Select(a => a?.GetType().Name ?? "null"))}");
                    throw new ArgumentException($"No matching constructor found for {targetType.FullName}");
                }

                return constructor;
            }
            catch (Exception ex)
            {
                Logger.Instance.Debug($"Error creating instance of {targetType.FullName}: {ex.Message}");
                throw;
            }
        }

        var methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(m => m.Name == methodName && m.GetParameters().Length == methodArgs.Length)
            .ToList();

        Logger.Instance.Debug($"Found {methods.Count} method(s) with matching name and parameter count");
        foreach (var m in methods)
        {
            Logger.Instance.Debug($"Candidate method: {m.Name}({string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})");
        }

        if (methods.Count == 0)
        {
            Logger.Instance.Debug($"Available methods on {targetType.FullName}:");
            foreach (var m in targetType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                Logger.Instance.Debug($"- {m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})");
            }
            throw new ArgumentException($"No matching method found for {methodName} on {targetType.FullName}");
        }

        // First try to find an exact type match
        MethodInfo? bestMatch = methods.FirstOrDefault(method => {
            var parameters = method.GetParameters();

            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;
                var argType = methodArgs[i]?.GetType();

                // If argument is null, any reference type parameter is valid
                if (methodArgs[i] == null && !paramType.IsValueType)
                    continue;

                // Check for exact type match
                if (argType != paramType)
                    return false;
            }
            return true;
        });

        // If no exact match found, try with implicit conversions
        if (bestMatch == null)
        {
            Logger.Instance.Debug("No exact type match found, trying implicit conversions");
            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                bool isMatch = true;
                
                Logger.Instance.Debug($"\nChecking method: {method.Name}({string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"))})");
                
                for (int i = 0; i < parameters.Length; i++)
                {
                    var paramType = parameters[i].ParameterType;
                    var argType = methodArgs[i]?.GetType();
                    
                    Logger.Instance.Debug($"Parameter {i}: Expected {paramType.Name}, Got {argType?.Name ?? "null"}");
                    
                    // If argument is null, any reference type parameter is valid
                    if (methodArgs[i] == null && !paramType.IsValueType)
                    {
                        Logger.Instance.Debug($"Null argument matches reference type parameter {paramType.Name}");
                        continue;
                    }
                    
                    // If argument type doesn't match parameter type and there's no implicit conversion
                    if (argType != null && !paramType.IsAssignableFrom(argType) && 
                        !CanConvertImplicitly(argType, paramType))
                    {
                        Logger.Instance.Debug($"Type mismatch: Cannot convert {argType.Name} to {paramType.Name}");
                        isMatch = false;
                        break;
                    }
                    
                    Logger.Instance.Debug($"Parameter {i} matches");
                }
                
                if (isMatch)
                {
                    Logger.Instance.Debug($"Found matching method with implicit conversion: {method.Name}");
                    bestMatch = method;
                    break;
                }
            }
        }

        if (bestMatch == null)
        {
            var argTypes = string.Join(", ", methodArgs.Select(a => a?.GetType().Name ?? "null"));
            Logger.Instance.Debug($"No matching method found for {methodName} with argument types: {argTypes}");
            Logger.Instance.Debug("Available methods:");
            foreach (var m in methods)
            {
                Logger.Instance.Debug($"- {m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})");
            }
            throw new ArgumentException($"No matching method found for {methodName} with the given argument types");
        }

        Logger.Instance.Debug($"Found matching method: {bestMatch.Name}({string.Join(", ", bestMatch.GetParameters().Select(p => p.ParameterType.Name))})");
        return bestMatch;
    }

    private static bool CanConvertImplicitly(Type from, Type to)
    {
        Logger.Instance.Debug($"Checking if {from.Name} can be converted to {to.Name}");
        
        // Handle numeric conversions
        if (from == typeof(int))
        {
            var canConvert = to == typeof(long) || to == typeof(float) || to == typeof(double) || 
                   to == typeof(decimal);
            Logger.Instance.Debug($"Can convert int to {to.Name}: {canConvert}");
            return canConvert;
        }
        if (from == typeof(long))
        {
            var canConvert = to == typeof(float) || to == typeof(double) || to == typeof(decimal);
            Logger.Instance.Debug($"Can convert long to {to.Name}: {canConvert}");
            return canConvert;
        }
        if (from == typeof(float))
        {
            var canConvert = to == typeof(double);
            Logger.Instance.Debug($"Can convert float to {to.Name}: {canConvert}");
            return canConvert;
        }
        
        Logger.Instance.Debug($"No implicit conversion from {from.Name} to {to.Name}");
        return false;
    }

    /// <summary>
    /// Converts a .NET object to a ZenValue.
    /// </summary>
    /// <param name="value"></param>
    public static ZenValue ToZen(dynamic? value)
    {
        // is value a Type?
        if (value is Type type)
        {
            ZenType zenType = ToZenType(type);
            return new ZenValue(ZenType.DotNetType, zenType);
        }else {
            return ToZenValue(value);
        }
    }

    /// <summary>
    /// Converts a .NET object to a ZenValue that is not a ZenType.Type.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static ZenValue ToZenValue(dynamic? value) {
        if (value is ZenValue) {
            return value;
        }
        else if (value is ZenObject) {
            return new ZenValue(ZenType.Object, value);
        }
        else if (value == null)
        {
            return ZenValue.Null;
        }
        else if (value is bool)
        {
            return value ? ZenValue.True : ZenValue.False;
        }
        else if (value is int)
        {
            return new ZenValue(ZenType.Integer, value);
        }
        else if (value is long)
        {
            return new ZenValue(ZenType.Integer64, value);
        }
        else if (value is float)
        {
            return new ZenValue(ZenType.Float, value);
        }
        else if (value is double)
        {
            return new ZenValue(ZenType.Float64, value);
        }
        else if (value is string)
        {
            return new ZenValue(ZenType.String, value);
        }
        else if (value is object)
        {
            var proxyClass = GetOrCreateProxyClass(value.GetType());
            var zenObjectProxy = new ZenObjectProxy(value, proxyClass);
            return new ZenValue(ZenType.DotNetObject, zenObjectProxy);
        }else {
            return ZenValue.Null;
        }
    }

    public static ZenType ToZenType(Type type)
    {
        if (type == typeof(string))
        {
            return ZenType.String;
        }
        else if (type == typeof(bool))
        {
            return ZenType.Boolean;
        }
        else if (type == typeof(int))
        {
            return ZenType.Integer;
        }
        else if (type == typeof(long))
        {
            return ZenType.Integer64;
        }
        else if (type == typeof(float))
        {
            return ZenType.Float;
        }
        else if (type == typeof(double))
            {
            return ZenType.Float64;
        }
        else
        {
            return ZenType.MakeDotNetType(type.FullName!);
        }
    }

    public static Type? ToDotNet(ZenType type) {
        if (type == ZenType.String) {
            return typeof(string);
        }else if (type == ZenType.Boolean) {
            return typeof(bool);
        }else if (type == ZenType.Integer) {
            return typeof(int);
        }else if (type == ZenType.Integer64) {
            return typeof(long);
        }else if (type == ZenType.Float) {
            return typeof(float);
        }else if (type == ZenType.Float64) {
            return typeof(double);
        }else if (type == ZenType.Any) {
            return typeof(object);
        }else if (type == ZenType.Null) {
            return null;
        }else if (type == ZenType.Object || type == ZenType.DotNetObject || type == ZenType.Class) {
            return typeof(object);
        }else if (type == ZenType.Type) {
            return Type.GetType(type.Name);
        }else {
            return null;
        }
    }

    public static dynamic? ToDotNet(ZenValue value)
    {
        if (value.Type == ZenType.String || value.Type == ZenType.Integer || value.Type == ZenType.Integer64 || value.Type == ZenType.Float || value.Type == ZenType.Float64 || value.Type == ZenType.Boolean || value.Type == ZenType.Null) {
            return value.Underlying;
        }else if (value.Type == ZenType.DotNetObject) {
            var proxy = value.Underlying as ZenObjectProxy;
            return proxy?.Target;
        }else if (value.Type == ZenType.Object) {
            var obj = value.Underlying as ZenObject;
            return obj;
        }else if (value.Type == ZenType.Type) {
            ZenType type = value.Underlying!;
            return ToDotNet(type);
        }else if (value.Type == ZenType.DotNetType) {
            ZenType type = value.Underlying!;
            return Type.GetType(type.Name);
        }

        return value.Underlying;
    }
}
