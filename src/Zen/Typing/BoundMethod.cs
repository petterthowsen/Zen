using Zen.Execution;

namespace Zen.Typing;

public class BoundMethod : ZenFunction {

    public ZenObject Instance;
    public ZenMethod Method;

    public BoundMethod(ZenObject instance, ZenMethod method) : base(method.ReturnType, method.Parameters) {
        Instance = instance;
        Method = method;
    }

    public override ZenValue Call(Interpreter interpreter, ZenValue[] arguments)
    {
        return Instance.Call(interpreter, Method, arguments);
    }
}