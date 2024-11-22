// Interpreter.FuncHandler.cs
using Zen.Execution.EvaluationResult;
using Zen.Parsing.AST;
using Zen.Typing;

namespace Zen.Execution;

public partial class Interpreter
{
    public IEvaluationResult CallUserFunction(BoundMethod bound, ZenValue[] arguments) {
        if (bound.Method is ZenUserMethod userMethod) {
            return CallUserFunction(bound.Closure, userMethod.Block, bound.Parameters, bound.ReturnType, arguments);
        }
        else if (bound.Method is ZenHostMethod hostMethod) {
            return (ValueResult) hostMethod.Call(this, bound.Instance, arguments);
        }

        return VoidResult.Instance;
    }

    public IEvaluationResult CallUserFunction(ZenUserFunction function, ZenValue[] arguments)
    {
        return CallUserFunction(function.Closure, function.Block, function.Parameters, function.ReturnType, arguments);
    }

    public IEvaluationResult CallUserFunction(Environment? closure, Block block, List<ZenFunction.Parameter> parameters, ZenType returnType, ZenValue[] arguments)
    {
        Environment previousEnvironment = environment;

        // Create outer environment for parameters
        Environment paramEnv = new Environment(closure);
        for (int i = 0; i < parameters.Count; i++)
        {
            paramEnv.Define(false, parameters[i].Name, parameters[i].Type, parameters[i].Nullable);
            paramEnv.Assign(parameters[i].Name, arguments[i]);
        }

        // Create inner environment for method body, which will contain 'this'
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
                throw Error($"Cannot return value of type '{returnException.Result.Type}' from function of type '{returnType}'", returnException.Location, Common.ErrorType.TypeError);
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
