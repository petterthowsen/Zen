// Interpreter.FuncHandler.cs
using Zen.Common;
using Zen.Execution.Builtins.Core;
using Zen.Execution.EvaluationResult;
using Zen.Parsing.AST;
using Zen.Typing;

namespace Zen.Execution;

public partial class Interpreter
{
    /// <summary>
    /// Call a method on an object
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="methodName"></param>
    /// <param name="returnType"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public ZenValue CallObject(ZenObject obj, string methodName, ZenType? returnType, ZenValue[] args)
    {
        ZenFunction? method = obj.GetMethodHierarchically(methodName, args, returnType);

        if (method == null) {
            throw Error($"{obj.Class} has no method {methodName}!");
        }

        BoundMethod boundMethod = method.Bind(obj);
        
        return CallFunction(boundMethod, args)
            .Value;
    }
    
    // CallObject with no arguments
    public ZenValue CallObject(ZenObject obj, string methodName, ZenType? returnType) => CallObject(obj, methodName, returnType, []);   
    
    /// <summary>
    /// Call a bound method. Routes to CallUserFunction or CallHostFunction.
    /// Can also handle ZenMethodProxy functions.
    /// </summary>
    public IEvaluationResult CallFunction(BoundMethod bound, ZenValue[] arguments) {
        if (bound.Method.IsUser) {
            // if the bound method is a user method, call the user function
            // bound methods have 'this' assigned in their environment.
            return CallUserFunction(bound.Method.Async, bound.Environment, bound.Method.UserCode!, bound.Arguments, bound.Method.ReturnType, arguments);
        }else {
            // It may be a regular host method or a proxy method for a .NET method
            if (bound.Method is ZenMethodProxy zenMethodProxy) {
                // proxy method handles itself
                return (ValueResult) zenMethodProxy.Call(bound.Instance, arguments);
            }else {
                // host methods
                return CallHostMethod(bound.Instance, bound.Method, arguments);
            }
        }
    }

    /// <summary>
    /// Utility method to call a user function or host function
    /// </summary>
    public IEvaluationResult CallFunction(ZenFunction function, ZenValue[] arguments)
    {
        switch (function.Type) {
            case ZenFunction.TYPE.UserFunction:
                return CallUserFunction(function.Async, function.Closure, function.UserCode!, function.Arguments, function.ReturnType, arguments);
            
            case ZenFunction.TYPE.HostFunction:
                return CallHostFunction(function, arguments);
        }

        throw Error($"CallFunction() Cannot handle ZenFunction.TYPE '{function.Type}'.", null, Common.ErrorType.RuntimeError);
    }

    /// <summary>
    /// Call the given host function. Handles both sync/async, validates argument and return types.
    /// </summary>
    /// <param name="function"></param>
    /// <param name="arguments"></param>
    /// <returns></returns>
    public IEvaluationResult CallHostFunction(ZenFunction function, ZenValue[] arguments)
    {
        if (function.IsHost == false) throw Error($"CallHostFunction() Cannot handle non-host functions.", null, Common.ErrorType.RuntimeError);

        // TODO: Validate arguments as needed

        if (function.Async)
        {
            if (function.AsyncHostFunc == null)
                throw Error("Missing async host function.", null, ErrorType.RuntimeError);

            // Execute the async host function. It returns Task<ZenValue>
            var task = function.AsyncHostFunc(arguments);

            // Return a ZenValue of type Task
            return new ValueResult { Value = new ZenValue(ZenType.Task, task) };
        }
        else
        {
            // static?
            if (function.IsStatic) {
                if (function.StaticHostMethod == null)
                    throw Error("Missing static host function implementation.", null, ErrorType.RuntimeError);
                return (ValueResult) function.StaticHostMethod(arguments);
            }

            if (function.Func == null)
                throw Error("Missing host function implementation.", null, ErrorType.RuntimeError);

            // Synchronous call
            var result = function.Func(arguments); // returns ZenValue
            return (ValueResult) result;
        }
    }
    
    public IEvaluationResult CallHostMethod(ZenObject instance, ZenFunction function, ZenValue[] arguments) {
        if (function.IsHost == false) throw Error($"CallHostMethod() Cannot handle non-host methods.", null, Common.ErrorType.RuntimeError);

        if (function.Async) {
            if (function.AsyncHostMethod == null) throw Error($"Missing AsyncHostMethod on ZenFunction!", null, Common.ErrorType.RuntimeError);
            
            // how do we handle this?
            // we need to pass it the ZenObject instance, and we need to return an awaitable.

            // Execute the async host function. It returns Task<ZenValue>
            var task = function.AsyncHostMethod(instance, arguments);

            // Return a ZenValue of type Task
            return new ValueResult { Value = new ZenValue(ZenType.Task, task) };
        }else {
            if (function.HostMethod == null) throw Error($"Missing HostMethod on ZenFunction!", null, Common.ErrorType.RuntimeError);
            
            return (ValueResult) function.HostMethod(instance, arguments);
        }
    }

    /// <summary>
    /// Main low-level method for calling a user function.
    /// Calls the given ZenFunction (function or method). Handles both sync/async, validates argument and return types.
    /// </summary>
    private IEvaluationResult CallUserFunction(bool async, Environment? closure, Block block, List<ZenFunction.Argument> arguments, ZenType returnType, ZenValue[] argValues)
    {
        // If this is an async function
        if (async)
        {
            Logger.Instance.Debug("Calling Async Function...");
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

                // Execute the function
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
            return (ValueResult) new ZenValue(ZenType.Task, tcs.Task);
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
                    statement.AcceptAsync(this).GetAwaiter().GetResult();
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
            catch (Exception ex)
            {
                // Handle other exceptions
                if (ex is RuntimeError runtimeError)
                {
                    throw runtimeError; // Re-throw to stop execution
                }
                else
                {
                    // Wrap other exceptions in a RuntimeError
                    throw Error(
                        ex.Message, 
                        CurrentNode?.Location, 
                        Common.ErrorType.RuntimeError, 
                        ex
                    );
                }
            }
            finally
            {
                environment = previousEnvironment;
            }

            return VoidResult.Instance;
        }
    }
}
