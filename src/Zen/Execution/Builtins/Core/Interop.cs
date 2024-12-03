using System.Reflection;
using Zen.Common;
using Zen.Typing;

namespace Zen.Execution.Builtins.Core;

public class Interop : IBuiltinsProvider
{
    public void RegisterBuiltins(Interpreter interpreter)
    {
        // Async CallDotNet
        interpreter.RegisterAsyncHostFunction(
            "CallDotNetAsync",
            ZenType.Any, // Returns a String
            new List<ZenFunction.Argument>
            {
                new("target", ZenType.String),
                new("method", ZenType.String),
            },
            CallDotNetAsync,
            variadic: true
        );

        // Synchronous CallDotNet
        interpreter.RegisterHostFunction(
            "CallDotNet",
            ZenType.Any, // Returns a String
            new List<ZenFunction.Argument>
            {
                new("target", ZenType.String),
                new("method", ZenType.String),
            },
            CallDotNet,
            variadic: true
        );
    }

    public static readonly Dictionary<Type, ZenClassProxy> ProxyClasses = [];

    public static ZenClassProxy GetOrCreateProxyClass(Type dotnetClass) {
        if ( ! ProxyClasses.ContainsKey(dotnetClass)) {
            ProxyClasses[dotnetClass] = new ZenClassProxy(dotnetClass);
        }
        return ProxyClasses[dotnetClass];
    }

    private static Task<ZenValue> CallDotNetAsync(ZenValue[] args)
    {
        return CallDotNetInternal(args, asyncExecution: true);
    }

    private static ZenValue CallDotNet(ZenValue[] args)
    {
        return CallDotNetInternal(args);
    }

    private static ZenValue CallDotNetInternal(ZenValue[] args)
    {
        if (args.Length < 2)
            throw new ArgumentException("CallDotNet requires at least two arguments: target and method name.");

        string targetName = args[0].Underlying as string ?? throw new ArgumentException("First argument must be a string (type name or object).");
        string methodName = args[1].Underlying as string ?? throw new ArgumentException("Second argument must be a string (method name).");

        Logger.Instance.Debug($"Attempting to call method {methodName} on {targetName}...");

        Type? targetType = Type.GetType(targetName);
        if (targetType == null)
        {
            throw new ArgumentException($"Type '{targetName}' not found.");
        }

        // Convert Zen arguments to .NET and infer parameter types
        var methodArgs = args.Skip(2).Select(arg => ToDotNet(arg)).ToArray();
        var parameterTypes = methodArgs
            .Select(arg => arg?.GetType() ?? typeof(object))
            .Cast<Type>()
            .ToArray();

        Logger.Instance.Debug($"Inferred parameter types: {string.Join(", ", parameterTypes.Select(pt => pt.Name))}");

        var methods = targetType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => m.Name == methodName && m.GetParameters().Length == parameterTypes.Length + 1)
            .ToList();

        // Find a method that matches exactly or with an additional CancellationToken
        var method = methods.FirstOrDefault(m =>
        {
            var parameters = m.GetParameters();
            return parameters.Take(parameterTypes.Length)
                .Select(p => p.ParameterType)
                .SequenceEqual(parameterTypes) &&
                parameters.LastOrDefault()?.ParameterType == typeof(CancellationToken);
        });

        if (method == null)
        {
            method = targetType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public, null, parameterTypes, null);
            if (method == null)
            {
                Logger.Instance.Debug($"Available methods on {targetType.FullName}:");
                foreach (var m in targetType.GetMethods(BindingFlags.Static | BindingFlags.Public))
                {
                    Logger.Instance.Debug($"- {m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})");
                }
                throw new ArgumentException($"No matching method found for {methodName} on {targetName} with parameter types {string.Join<Type>(", ", parameterTypes)}");
            }
        }
        else
        {
            // Append CancellationToken.None if the method expects it
            Logger.Instance.Debug($"Appending CancellationToken.None to arguments for method: {method.Name}");
            methodArgs = methodArgs.Append(CancellationToken.None).ToArray();
        }

        Logger.Instance.Debug($"Resolved method: {method.Name}");

        try
        {
            var result = method.Invoke(null, methodArgs);
            return ToZen(result); // Handle synchronous methods
        }
        catch (Exception ex)
        {
            Logger.Instance.Debug($"Error invoking method {methodName} on {targetName}: {ex.Message}");
            throw;
        }
    }

    private static async Task<ZenValue> CallDotNetInternal(ZenValue[] args, bool asyncExecution)
    {
        if (args.Length < 2)
            throw new ArgumentException("CallDotNet requires at least two arguments: target and method name.");

        string targetName = args[0].Underlying as string ?? throw new ArgumentException("First argument must be a string (type name or object).");
        string methodName = args[1].Underlying as string ?? throw new ArgumentException("Second argument must be a string (method name).");

        Logger.Instance.Debug($"Attempting to call method {methodName} on {targetName}...");

        Type? targetType = Type.GetType(targetName);
        if (targetType == null)
        {
            throw new ArgumentException($"Type '{targetName}' not found.");
        }

        // Convert Zen arguments to .NET and infer parameter types
        var methodArgs = args.Skip(2).Select(arg => ToDotNet(arg)).ToArray();
        var parameterTypes = methodArgs
            .Select(arg => arg?.GetType() ?? typeof(object))
            .Cast<Type>()
            .ToArray();

        Logger.Instance.Debug($"Inferred parameter types: {string.Join(", ", parameterTypes.Select(pt => pt.Name))}");

        var methods = targetType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => m.Name == methodName && m.GetParameters().Length == parameterTypes.Length + 1)
            .ToList();

        // Find a method that matches exactly or with an additional CancellationToken
        var method = methods.FirstOrDefault(m =>
        {
            var parameters = m.GetParameters();
            return parameters.Take(parameterTypes.Length)
                .Select(p => p.ParameterType)
                .SequenceEqual(parameterTypes) &&
                parameters.LastOrDefault()?.ParameterType == typeof(CancellationToken);
        });

        if (method == null)
        {
            method = targetType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public, null, parameterTypes, null);
            if (method == null)
            {
                Logger.Instance.Debug($"Available methods on {targetType.FullName}:");
                foreach (var m in targetType.GetMethods(BindingFlags.Static | BindingFlags.Public))
                {
                    Logger.Instance.Debug($"- {m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})");
                }
                throw new ArgumentException($"No matching method found for {methodName} with parameter types {string.Join<Type>(", ", parameterTypes)}");
            }
        }
        else
        {
            // Append CancellationToken.None if the method expects it
            Logger.Instance.Debug($"Appending CancellationToken.None to arguments for method: {method.Name}");
            methodArgs = methodArgs.Append(CancellationToken.None).ToArray();
        }

        Logger.Instance.Debug($"Resolved method: {method.Name}");

        try
        {
            var result = method.Invoke(null, methodArgs);
            if (result is Task task)
            {
                if (asyncExecution)
                {
                    await task.ConfigureAwait(false);

                    if (task.GetType().IsGenericType)
                    {
                        var taskResult = ((dynamic)task).Result;
                        return ToZen(taskResult);
                    }
                    return ZenValue.Null;
                }
                else
                {
                    throw new InvalidOperationException("Cannot invoke an asynchronous method synchronously.");
                }
            }

            return ToZen(result); // Handle synchronous methods
        }
        catch (Exception ex)
        {
            Logger.Instance.Debug($"Error invoking method {methodName} on {targetName}: {ex.Message}");
            throw;
        }
    }


    /// <summary>
    /// Converts a .NET object to a ZenValue
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static ZenValue ToZen(dynamic? value)
    {
        if (value == null)
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
        }else if (value is object)
        {
            var proxyClass = GetOrCreateProxyClass(value.GetType());
            var zenObjectProxy = new ZenObjectProxy(value, proxyClass);
            return new ZenValue(ZenType.DotNetObject, zenObjectProxy);
        }else {
            return ZenValue.Null;
        }
    }

    public static ZenType ToZen(Type type)
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
        else if (type == typeof(DateTime)) // Explicitly handle DateTime
        {
            return ZenType.DotNetObject; // Or ZenType.DateTime if supported
        }
        else if (!type.IsPrimitive) // Treat all non-primitive types as Object
        {
            return ZenType.DotNetObject;
        }
        else
        {
            return ZenType.Null;
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
        }else if (type == ZenType.Object) {
            return typeof(object);
        }else if (type == ZenType.Class) {
            return typeof(object);
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
        }

        return value.Underlying;
    }
}