using Zen.Common;
using Zen.Execution;
using Environment = Zen.Execution.Environment;

namespace Zen.Typing;

public class BoundMethod : ZenFunction {

    public ZenObject Instance;
    public ZenMethod Method;

    public BoundMethod(ZenObject instance, ZenMethod method) : base(method.Async, method.ReturnType, method.Arguments) {
        Instance = instance;
        Method = method;
    }

    public BoundMethod(ZenObject instance, ZenMethod method, Environment closure) : base(method.Async, method.ReturnType, method.Arguments, closure) {
        Instance = instance;
        Method = method;
    }

    public override ZenValue Call(Interpreter interpreter, ZenValue[] argValues)
    {
        return Instance.Call(interpreter, Method, argValues);
    }
}
