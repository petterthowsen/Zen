using System.Reflection;
using Zen.Common;
using Zen.Execution.Interop;
using Zen.Typing;

namespace Zen.Execution.Builtins.Core;

public class Interop : IBuiltinsProvider
{
    public void RegisterBuiltins(Interpreter interpreter)
    {
        interpreter.RegisterAsyncHostFunction(
            "CallDotNetAsync",
            ZenType.String, // Returns a String
            new List<ZenFunction.Argument>
            {
                new("target", ZenType.String),
                new("method", ZenType.String),
                new("args", ZenType.Any) // Array of arguments
            },
            CallDotNetAsync
        );
    }

    public static async Task<ZenValue> CallDotNetAsync(ZenValue[] args)
    {
        if (args.Length < 2)
            throw new ArgumentException("CallDotNetAsync requires at least two arguments: target and method name.");

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
       // Infer parameter types as System.Type[]
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
                await task.ConfigureAwait(false);

                if (task.GetType().IsGenericType)
                {
                    var taskResult = ((dynamic)task).Result;
                    return ToZen(taskResult);
                }
                return ZenValue.Null;
            }
            throw new Exception("Method does not return a Task.");
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
    public static ZenValue ToZen(dynamic value)
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
        }else
        {
            // Handle complex .NET objects by wrapping them as Zen objects
            return new ZenValue(ZenType.Object, new ZenObjectProxy(value));
        }
    }

    public static dynamic? ToDotNet(ZenValue value)
    {
        if (value.Type == ZenType.Type) {
            ZenType typ = value.Underlying!;
            if (typ == ZenType.String) {
                return "".GetType();
            }
            else if (typ == ZenType.Null) {
                return null;
            }else if (typ == ZenType.Boolean) {
                return true.GetType();
            }else if (typ == ZenType.Float) {

            }
        }
        return value.Underlying;
    }
}