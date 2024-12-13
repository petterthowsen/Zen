// Interpreter.FuncHandler.cs
using Zen.Common;
using Zen.Execution.Builtins.Core;
using Zen.Execution.EvaluationResult;
using Zen.Parsing.AST;
using Zen.Typing;

namespace Zen.Execution;

public partial class Interpreter
{
    public bool TypeCheckArguments(ZenFunction function, ZenValue[] args)
    {
        var i = 0;
        foreach (var argDef in function.Arguments) {
            if (i >= args.Length) {
                if (!argDef.Nullable && argDef.DefaultValue == null) {
                    return false;
                }
            }
            if (!TypeChecker.IsCompatible(args[i].Type, argDef.Type)) {
                return false;
            }
            i++;
        }

        return true;
    }

    public void ValidateFunctionArguments(ZenFunction function, ZenValue[] args) {
        List<ZenFunction.Argument> missingArgs = new List<ZenFunction.Argument>();

        var i = 0;
        foreach (var argDef in function.Arguments) {
            if (i >= args.Length) {
                if (!argDef.Nullable && argDef.DefaultValue == null) {
                    missingArgs.Add(argDef);
                }
                continue;
            }

            if (!TypeChecker.IsCompatible(args[i].Type, argDef.Type)) {
                throw Error($"Argument {i} is expected to be a {argDef.Type}, not a {args[i].Type}!");
            }
            i++;
        }

        if (missingArgs.Count > 0) {
            throw Error($"Missing arguments for function {function.Name}: {string.Join(", ", missingArgs.Select(a => a.Name))}");
        }
    }

    public ZenValue[] PrepareFunctionArguments(ZenFunction function, ZenValue[] callArgs) {
        
        List<ZenValue> prepared = new List<ZenValue>();
        int i = 0;

        // Iterate through the function's defined arguments
        for (; i < function.Arguments.Count; i++)
        {
            if (i < callArgs.Length)
            {
                // Argument is provided in callArgs; perform type conversion
                var argDef = function.Arguments[i];
                prepared.Add(TypeConverter.Convert(callArgs[i], argDef.Type, argDef.Nullable));
            }
            else
            {
                // Argument is missing in callArgs
                var argDef = function.Arguments[i];
                if (!argDef.Nullable && argDef.DefaultValue == null)
                {
                    throw new ArgumentException($"Missing required argument '{argDef.Name}' for function '{function.Name}'.");
                }

                // Use default value if provided; otherwise, use null
                prepared.Add(argDef.DefaultValue != null 
                    ? (ZenValue)argDef.DefaultValue 
                    : ZenValue.Null); // Assuming ZenValue has a Null representation
            }
        }

        // Handle variadic arguments if the function supports them
        if (callArgs.Length > function.Arguments.Count)
        {
            if (function.Variadic)
            {
                for (; i < callArgs.Length; i++)
                {
                    prepared.Add(callArgs[i]);
                }
            }
            else
            {
                throw new ArgumentException($"Function '{function.Name}' does not accept {callArgs.Length - function.Arguments.Count} additional argument(s).");
            }
        }

        return prepared.ToArray();
    }

    /// <summary>
    /// Call a method on an object
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="methodName"></param>
    /// <param name="returnType"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public async Task<ZenValue> CallObject(ZenObject obj, string methodName, ZenType? returnType, ZenValue[] args)
    {
        ZenFunction? method = obj.GetMethodHierarchically(methodName, args, returnType);

        if (method == null) {
            throw Error($"{obj.Class} has no method {methodName} with return type {returnType} and argument types {string.Join<ZenValue>(", ", args)}!");
        }

        //ValidateFunctionArguments(method, args); this is already done in CallFunction

        BoundMethod boundMethod = method.Bind(obj);
        
        IEvaluationResult result = await CallFunction(boundMethod, args);
        return result.Value;
    }
    
    // CallObject with no arguments
    public async Task<ZenValue> CallObject(ZenObject obj, string methodName, ZenType? returnType) => await CallObject(obj, methodName, returnType, []);   
    
    /// <summary>
    /// Call a bound method. Routes to CallUserFunction or CallHostFunction.
    /// Can also handle ZenMethodProxy functions.
    /// </summary>
    public async Task<IEvaluationResult> CallFunction(BoundMethod bound, ZenValue[] arguments) {
        ValidateFunctionArguments(bound.Method, arguments);
        arguments = PrepareFunctionArguments(bound.Method, arguments);

        if (bound.Method.IsUser) {
            // if the bound method is a user method, call the user function
            // bound methods have 'this' assigned in their environment.
            Logger.Instance.Debug($"Calling method {bound}");

            return await CallUserFunction(bound.Method.Async, bound.Environment, bound.Method.UserCode!, bound.Arguments, bound.Method.ReturnType, arguments);
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

    public IEvaluationResult CallFunctionSync(BoundMethod bound, ZenValue[] arguments)
    {
        if (bound.Method.Async) throw Error("CallFunctionSync() cannot call an async function.", null, Common.ErrorType.RuntimeError);
        
        return CallFunction(bound, arguments).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Utility method to call a user function or host function
    /// </summary>
    public async Task<IEvaluationResult> CallFunction(ZenFunction function, ZenValue[] arguments)
    {
        ValidateFunctionArguments(function, arguments);
        arguments = PrepareFunctionArguments(function, arguments);

        switch (function.Type) {
            case ZenFunction.TYPE.UserFunction:
                Logger.Instance.Debug($"Calling {function}");
                return await CallUserFunction(function.Async, function.Closure, function.UserCode!, function.Arguments, function.ReturnType, arguments);
            
            case ZenFunction.TYPE.UserMethod:
                Logger.Instance.Debug($"Calling {function}");
                return await CallUserFunction(function.Async, function.Closure, function.UserCode!, function.Arguments, function.ReturnType, arguments);

            case ZenFunction.TYPE.HostFunction:
                Logger.Instance.Debug($"Calling {function}");
                return CallHostFunction(function, arguments);
            
            case ZenFunction.TYPE.HostMethod:
                Logger.Instance.Debug($"Calling {function}");
                return CallHostMethod(null, function, arguments);
                
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
        if (function.IsMethod) throw Error($"CallHostFunction() Cannot handle methods.", null, Common.ErrorType.RuntimeError);

        // TODO: Validate arguments as needed
        Logger.Instance.Debug($"Calling {function}");

        if (function.Async)
        {
            if (function.AsyncHostFunc == null)
                throw Error($"Missing AsyncHostFunc on ZenFunction {function.Name}.", null, ErrorType.RuntimeError);

            // Execute the async host function. It returns Task<ZenValue>
            var task = RunOnEventLoop(() => function.AsyncHostFunc(arguments));
            return (ValueResult) task;
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
        if (function.IsMethod == false) throw Error($"CallHostMethod() Cannot handle non-methods.", null, Common.ErrorType.RuntimeError);

        Logger.Instance.Debug($"CallHostMethod {function}");

        if (function.Async) {
            if (function.AsyncHostMethod == null) throw Error($"Missing AsyncHostMethod on ZenFunction!", null, Common.ErrorType.RuntimeError);
            
            // Execute the async host function. It returns Task<ZenValue>
            var task = RunOnEventLoop(() => function.AsyncHostMethod(instance, arguments));
            return (ValueResult) task;
        }else {

            if (function.IsStatic) {
                if (function.StaticHostMethod == null) throw Error($"Missing StaticHostMethod on ZenFunction!", null, Common.ErrorType.RuntimeError);
                return (ValueResult) function.StaticHostMethod(arguments);
            }
            
            if (function.HostMethod == null) throw Error($"Missing HostMethod on ZenFunction!", null, Common.ErrorType.RuntimeError);

            return (ValueResult) function.HostMethod(instance, arguments);
        }
    }

    private async Task<ZenValue> ExecuteUserFunction(
        Environment? closure, 
        Block block, 
        List<ZenFunction.Argument> arguments,
        ZenType returnType, 
        ZenValue[] argValues)
    {
        Environment previousEnvironment = Environment;
        try
        {
            // Create outer environment for Arguments
            Environment = new Environment(closure, "function env");
            for (int i = 0; i < arguments.Count; i++)
            {
                Environment.Define(false, arguments[i].Name, arguments[i].Type, arguments[i].Nullable);

                ZenValue argValue;
                if (argValues.Length <= i) {
                    argValue = (ZenValue) arguments[i].DefaultValue!;
                }else {
                    argValue = argValues[i];
                }

                // Assign
                Environment.Assign(arguments[i].Name, argValue);
            }

            // Execute the function
            foreach (var statement in block.Statements)
            {
                await statement.AcceptAsync(this);
            }

            return ZenValue.Void;
        }
        catch (ReturnException returnException)
        {
            // Handle return value
            if (!TypeChecker.IsCompatible(returnException.Result.Type, returnType))
            {
                throw Error(
                    $"Cannot return value of type '{returnException.Result.Type}' from async function expecting '{returnType}'",
                    returnException.Location, 
                    ErrorType.TypeError
                );
            }
            else
            {
                // Type convert if needed
                return TypeConverter.Convert(returnException.Result.Value, returnType, false);
            }
        }
        finally
        {
            Environment = previousEnvironment;
        }
    }

    /// <summary>
    /// Run a task on the event loop as a Zen Task. Handles exceptions.
    /// </summary>
    /// <param name="task"></param>
    /// <returns>a ZenValue of type ZenType.Task, the underlying value is the task itself.</returns>
    public ZenValue RunOnEventLoop(Func<Task<ZenValue>> taskFunc) {
        var tcs = new TaskCompletionSource<ZenValue>();

        SendOrPostCallback callback = state => {
            // Start the async task without making the callback itself async
            _ = ExecuteCallbackAsync(taskFunc, tcs);
        };

        // Post the callback to the synchronization context
        SyncContext.Post(callback, null);

        // Handle global environment continuations if necessary
        if (Environment == globalEnvironment) {
            SyncContext.TrackContinuation();

            tcs.Task.ContinueWith(t => {
                if (t.IsFaulted) {
                    Exception? ex = t.Exception?.InnerException;
                    if (ex != null) {
                        Logger.Instance.Error($"Top-level async function failed. Calling SyncContext.Fail with the exception {ex.Message}");
                        SyncContext.Fail(ex);
                    }
                }
                SyncContext.CompleteContinuation();
            });
        }

        ZenValue zenTask = new ZenValue(ZenType.Task, tcs.Task);
        return zenTask;
    }

    private async Task ExecuteCallbackAsync(Func<Task<ZenValue>> taskFunc, TaskCompletionSource<ZenValue> tcs) {
        try {
            var task = taskFunc();
            var result = await task;
            tcs.SetResult(result);
        }
        catch (Exception ex) {
            tcs.SetException(ex);
        }
    }

    /// <summary>
    /// Main low-level method for calling a user function.
    /// Calls the given ZenFunction (function or method). Handles both sync/async, validates argument and return types.
    /// </summary>
    private async Task<IEvaluationResult> CallUserFunction(bool async, Environment? closure, Block block, List<ZenFunction.Argument> arguments, ZenType returnType, ZenValue[] argValues)
    {
        Func<Task<ZenValue>> func = () => ExecuteUserFunction(closure, block, arguments, returnType, argValues);
        
        // If this is an async function
        if (async)
        {
            ZenValue zenTask = RunOnEventLoop(func);

            // Return the task
            return (ValueResult) zenTask;
        }
        else
        {
            var task = func();
            await task;
            return (ValueResult) task.Result;
        }
    }
}
