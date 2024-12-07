// Interpreter.FuncHandler.cs
using Zen.Common;
using Zen.Execution.Builtins.Core;
using Zen.Execution.EvaluationResult;
using Zen.Parsing.AST;
using Zen.Typing;

namespace Zen.Execution;

public partial class Interpreter
{
    // todo: should clean up/organize/comment these methods
    public ZenValue CallObject(ZenObject obj, string methodName, ZenType returnType, ZenValue[] args)
    {
        ZenMethod? method = obj.GetMethodHierarchically(methodName, args, returnType);

        if (method == null) {
            throw Error($"{obj.Class} has no method {methodName}!");
        }

        BoundMethod boundMethod = method.Bind(obj);
        var result = CallFunction(boundMethod, args);
        return result.Value;
    }

    public ZenValue CallObject(ZenObject obj, string methodName, ZenType returnType)
    {
        return CallObject(obj, methodName, returnType, []);
    }

    public async Task<ZenValue> CallObject(ZenObject obj, string methodName)
    {
        ZenMethod? method = obj.GetMethodHierarchically(methodName);

        if (method == null) {
            throw Error($"{obj.Class} has no method {methodName}!");
        }

        BoundMethod boundMethod = method.Bind(obj);
        return CallFunction(boundMethod, []).Value;
    }

    public IEvaluationResult CallFunction(ZenFunction function, ZenValue[] arguments)
    {
        if (function is ZenHostFunction hostFunc)
        {
            var task = hostFunc.Call(this, arguments);
            if (hostFunc.Async)
            {
                // For async functions, return the task wrapped in a ZenValue
                return new ValueResult { Value = new ZenValue(ZenType.Task, task) };
            }
            else
            {
                // For non-async functions, return the result directly
                return new ValueResult { Value = task.Result };
            }
        }
        else if (function is ZenUserFunction userFunc)
        {
            return CallUserFunction(userFunc, arguments);
        }
        else if (function is BoundMethod boundMethod)
        {
            return CallUserFunction(boundMethod, arguments);
        }
        else if (function is ZenHostMethod hostMethod) 
        {
            var task = hostMethod.Call(this, arguments);
            if (hostMethod.Async)
            {
                // For async methods, return the task wrapped in a ZenValue
                return new ValueResult { Value = new ZenValue(ZenType.Task, task) };
            }
            else
            {
                // For non-async methods, return the result directly
                return new ValueResult { Value = task.Result };
            }
        }
        
        throw Error($"Cannot call unknown function type '{function.GetType()}'", null, Common.ErrorType.RuntimeError);
    }

    public IEvaluationResult CallUserFunction(BoundMethod bound, ZenValue[] arguments) {
        if (bound.Method is ZenUserMethod userMethod) {
            return CallUserFunction(userMethod.Async, bound.Closure, userMethod.Block, bound.Arguments, bound.ReturnType, arguments);
        }
        else if (bound.Method is ZenHostMethod hostMethod) {
            var task = hostMethod.Call(this, bound.Instance, arguments);
            if (hostMethod.Async)
            {
                return new ValueResult { Value = new ZenValue(ZenType.Task, task) };
            }
            else
            {
                return new ValueResult { Value = task };
            }
        }
        else if (bound.Method is ZenMethodProxy methodProxy) {
            var task = methodProxy.Call(this, bound.Instance, arguments);
            if (methodProxy.Async)
            {
                return new ValueResult { Value = new ZenValue(ZenType.Task, task) };
            }
            else
            {
                return new ValueResult { Value = task };
            }
        }
        
        throw Error($"Cannot call unknown function type '{bound.Method.GetType()}'", null, Common.ErrorType.RuntimeError);
    }

    public IEvaluationResult CallUserFunction(ZenUserFunction function, ZenValue[] arguments)
    {
        return CallUserFunction(function.Async, function.Closure, function.Block!, function.Arguments, function.ReturnType, arguments);
    }

    // this works and makse sense
    public IEvaluationResult CallUserFunction(bool async, Environment? closure, Block block, List<ZenFunction.Argument> arguments, ZenType returnType, ZenValue[] argValues)
    {
        // If this is an async function
        if (async)
        {
            // Create a TaskCompletionSource that will be completed when the function finishes
            var tcs = new TaskCompletionSource<ZenValue>();
            
            // Schedule the function execution on the sync context
            SyncContext.Post(async _ =>
            {
                Environment previousEnvironment = environment;

                // Create outer environment for Arguments
                environment = new Environment(closure, "function env");
                for (int i = 0; i < arguments.Count; i++)
                {
                    environment.Define(false, arguments[i].Name, arguments[i].Type, arguments[i].Nullable);

                    ZenValue argValue = argValues[i];

                    // type check                
                    Logger.Instance.Debug($"Function argument {i} expects {arguments[i].Type}. Checking if compatible with {argValue.Type}");
                    if (TypeChecker.IsCompatible(argValue.Type, arguments[i].Type) == false) {
                        throw Error($"{arguments[i].Name} is expected to be a {arguments[i].Type}, not a {argValue.Type}!");
                    }

                    // type convert if needed
                    argValue = TypeConverter.Convert(argValue, arguments[i].Type, false);

                    // assign
                    environment.Assign(arguments[i].Name, argValue);
                }

                try
                {
                    foreach (var statement in block.Statements)
                    {
                        await statement.AcceptAsync(this);
                    }
                    tcs.SetResult(ZenValue.Void);
                }
                catch (ReturnException returnException)
                {
                    // type check return value against the function's return type
                    if (!TypeChecker.IsCompatible(returnException.Result.Type, returnType))
                    {
                        var error = Error($"Cannot return value of type '{returnException.Result.Type}' from async function expecting '{returnType}'",
                            returnException.Location, ErrorType.TypeError);
                        tcs.SetException(error);
                        throw error; // Re-throw to stop execution
                    }
                    else
                    {
                        tcs.SetResult(returnException.Result.Value);
                    }
                }
                catch (Exception ex)
                {
                    // If it's already a RuntimeError, use it directly
                    if (ex is RuntimeError runtimeError)
                    {
                        tcs.SetException(runtimeError);
                        throw runtimeError; // Re-throw to stop execution
                    }
                    else
                    {
                        // Wrap other exceptions in a RuntimeError
                        var error = Error(ex.Message, CurrentNode?.Location, ErrorType.RuntimeError, ex);
                        tcs.SetException(error);
                        throw error; // Re-throw to stop execution
                    }
                }
                finally
                {
                    environment = previousEnvironment;
                }
            }, null);

            // Return the task immediately without waiting
            return new ValueResult { Value = new ZenValue(ZenType.Task, tcs.Task) };
        }
        else
        {
            // For non-async functions, execute synchronously
            Environment previousEnvironment = environment;

            // Create outer environment for Arguments
            environment = new Environment(closure, "function env");

            for (int i = 0; i < arguments.Count; i++)
            {
                environment.Define(false, arguments[i].Name, arguments[i].Type, arguments[i].Nullable);

                    ZenValue argValue = argValues[i];

                    // type check                
                    Logger.Instance.Debug($"Function argument {i} expects {arguments[i].Type}. Checking if compatible with {argValue.Type}");
                    if (TypeChecker.IsCompatible(argValue.Type, arguments[i].Type) == false) {
                        throw Error($"{arguments[i].Name} is expected to be a {arguments[i].Type}, not a {argValue.Type}!");
                    }

                    // type convert if needed
                    argValue = TypeConverter.Convert(argValue, arguments[i].Type, false);

                    // assign
                    environment.Assign(arguments[i].Name, argValue);
            }

            try
            {
                foreach (var statement in block.Statements)
                {
                    statement.AcceptAsync(this).Wait();
                }
            }
            catch (ReturnException returnException)
            {
                // type check return value
                if (!TypeChecker.IsCompatible(returnException.Result.Type, returnType))
                {
                    throw Error($"Cannot return value of type '{returnException.Result.Type}' from function of type '{returnType}'", 
                        returnException.Location, Common.ErrorType.TypeError);
                }
                return returnException.Result;
            }
            finally
            {
                environment = previousEnvironment;
            }

            return new ValueResult { Value = ZenValue.Void };
        }
    }
}
