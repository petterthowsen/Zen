using Zen.Common;
using Zen.Typing;

namespace Zen.Execution.Interop;

public class Dotnet
{

    public static async Task<ZenValue> CallDotNetAsync(ZenValue[] args)
    {
        throw new Exception("err");

        if (args.Length < 2)
            throw new ArgumentException("CallDotNetAsync requires at least two arguments: target and method name.");

        string targetName = args[0].Underlying as string ?? throw new ArgumentException("First argument must be a string (type name or object).");
        string methodName = args[1].Underlying as string ?? throw new ArgumentException("Second argument must be a string (method name).");

        Logger.Instance.Debug($"Attempting to call method {methodName} on {targetName}...");

        // Resolve the method target
        object? target = null;
        Type? targetType = Type.GetType(targetName);
        if (targetType == null)
        {
            Logger.Instance.Debug($"Type '{targetName}' not found.");
            throw new ArgumentException($"Type '{targetName}' not found.");
        }

        // Convert Zen arguments to .NET
        var methodArgs = args.Skip(2).Select(arg => ToDotNet(arg)).ToArray();
        Logger.Instance.Debug($"Method arguments: {string.Join(", ", methodArgs.Select(a => a?.ToString() ?? "null"))}");

        var method = targetType.GetMethod(methodName);
        Logger.Instance.Debug("HELLO!");
        if (method == null)
        {
            Logger.Instance.Debug($"Method '{methodName}' not found on type '{targetName}'.");
            throw new ArgumentException($"Method '{methodName}' not found on type '{targetName}'.");
        }
        Logger.Instance.Debug($"Invoking method {methodName}...");

        // Invoke asynchronously if it returns a Task
        var result = method.Invoke(target, methodArgs);
        if (result is Task task)
        {
            await task.ConfigureAwait(false);

            if (task.GetType().IsGenericType)
            {
                // For Task<T>
                var taskResult = ((dynamic)task).Result;
                Logger.Instance.Debug($"Task completed. Result: {taskResult}");
                return ToZen(taskResult);
            }

            // For Task
            Logger.Instance.Debug($"Task completed with no result.");
            return ZenValue.Null;
        }

        return ZenValue.Null;
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