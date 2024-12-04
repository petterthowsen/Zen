// Interpreter.FuncHandler.cs
using Zen.Common;
using Zen.Execution.Builtins.Core;
using Zen.Execution.EvaluationResult;
using Zen.Parsing.AST;
using Zen.Typing;

namespace Zen.Execution;

public partial class Interpreter
{
    public ZenValue CallObject(ZenObject obj, string methodName, ZenType returnType, ZenValue[] args)
    {
        ZenMethod? method = obj.GetMethodHierarchically(methodName, args, returnType);

        if (method == null) {
            throw Error($"{obj.Class} has no method {methodName}!");
        }

        BoundMethod boundMethod = method.Bind(obj);
        return CallFunction(boundMethod, args).Value;
    }

    public ZenValue CallObject(ZenObject obj, string methodName, ZenType returnType)
    {
        return CallObject(obj, methodName, returnType, []);
    }

    public IEvaluationResult CallFunction(ZenFunction function, ZenValue[] arguments)
    {
        if (function is ZenHostFunction hostFunc)
        {
            var result = hostFunc.Call(this, arguments);
            // If the result is a Promise, don't await it - let the caller handle it
            return (ValueResult)result;
        }
        else if (function is ZenUserFunction userFunc)
        {
            return CallUserFunction(userFunc, arguments);
        }
        else if (function is BoundMethod boundMethod)
        {
            return CallUserFunction(boundMethod, arguments);
        }else if (function is ZenHostMethod) {
            return (ValueResult) function.Call(this, arguments);
        }
        
        throw Error($"Cannot call unknown function type '{function.GetType()}'", null, Common.ErrorType.RuntimeError);
    }

    public IEvaluationResult CallUserFunction(BoundMethod bound, ZenValue[] arguments) {
        if (bound.Method is ZenUserMethod userMethod) {
            return CallUserFunction(userMethod.Async, bound.Closure, userMethod.Block, bound.Arguments, bound.ReturnType, arguments);
        }
        else if (bound.Method is ZenHostMethod hostMethod) {
            return (ValueResult) hostMethod.Call(this, bound.Instance, arguments);
        }else if (bound.Method is ZenMethodProxy methodProxy) {
            return (ValueResult) methodProxy.Call(this, bound.Instance, arguments);
        }

        return VoidResult.Instance;
    }

    public IEvaluationResult CallUserFunction(ZenUserFunction function, ZenValue[] arguments)
    {
        return CallUserFunction(function.Async, function.Closure, function.Block!, function.Arguments, function.ReturnType, arguments);
    }

    public IEvaluationResult CallUserFunction(bool async, Environment? closure, Block block, List<ZenFunction.Argument> arguments, ZenType returnType, ZenValue[] argValues)
    {
        // If this is an async function
        if (async)
        {
            // Create a promise that will be resolved when the function completes
            var promise = new ZenPromise(environment, returnType);
            
            // Schedule the function execution on the event loop
            EventLoop.EnqueueTask(() =>
            {
                Environment previousEnvironment = environment;

                // Create outer environment for Arguments
                Environment paramEnv = new Environment(closure);
                for (int i = 0; i < arguments.Count; i++)
                {
                    paramEnv.Define(false, arguments[i].Name, arguments[i].Type, arguments[i].Nullable);

                    ZenValue argValue = argValues[i];

                    // type check                
                    Logger.Instance.Debug($"Function argument {i} expects {arguments[i].Type}. Checking if compatible with {argValue.Type}");
                    if (TypeChecker.IsCompatible(argValue.Type, arguments[i].Type) == false) {
                        throw Error($"{arguments[i].Name} is expected to be a {arguments[i].Type}, not a {argValue.Type}!");
                    }

                    // type convert if needed
                    argValue = TypeConverter.Convert(argValue, arguments[i].Type, false);

                    // assign
                    paramEnv.Assign(arguments[i].Name, argValue);
                }

                // Create inner environment for method body
                environment = new Environment(paramEnv);

                try
                {
                    foreach (var statement in block.Statements)
                    {
                        statement.Accept(this);
                    }
                    promise.Resolve(ZenValue.Void);
                }
                catch (ReturnException returnException)
                {
                    // type check return value against the promise's inner type
                    if (!TypeChecker.IsCompatible(returnException.Result.Type, returnType))
                    {
                        promise.Reject(new ZenValue(ZenType.String, 
                            $"Cannot return value of type '{returnException.Result.Type}' from async function expecting '{returnType}'"));
                    }
                    else
                    {
                        promise.Resolve(returnException.Result.Value);
                    }
                }
                catch (Exception ex)
                {
                    promise.Reject(new ZenValue(ZenType.String, ex.Message));
                }
                finally
                {
                    environment = previousEnvironment;
                }
            });

            // Return the promise immediately without waiting
            return (ValueResult)new ZenValue(ZenType.Promise, promise);
        }
        else
        {
            // For non-async functions, execute synchronously
            Environment previousEnvironment = environment;

            // Create outer environment for Arguments
            Environment paramEnv = new Environment(closure);
            for (int i = 0; i < arguments.Count; i++)
            {
                paramEnv.Define(false, arguments[i].Name, arguments[i].Type, arguments[i].Nullable);

                    ZenValue argValue = argValues[i];

                    // type check                
                    Logger.Instance.Debug($"Function argument {i} expects {arguments[i].Type}. Checking if compatible with {argValue.Type}");
                    if (TypeChecker.IsCompatible(argValue.Type, arguments[i].Type) == false) {
                        throw Error($"{arguments[i].Name} is expected to be a {arguments[i].Type}, not a {argValue.Type}!");
                    }

                    // type convert if needed
                    argValue = TypeConverter.Convert(argValue, arguments[i].Type, false);

                    // assign
                    paramEnv.Assign(arguments[i].Name, argValue);
            }

            // Create inner environment for method body
            environment = new Environment(paramEnv);

            try
            {
                foreach (var statement in block.Statements)
                {
                    statement.Accept(this);
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

            return (ValueResult)ZenValue.Void;
        }
    }
}
