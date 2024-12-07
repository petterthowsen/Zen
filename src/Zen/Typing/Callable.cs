using Zen.Execution;

namespace Zen.Typing;

public interface ICallable {

    public int Arity { get; }
    public ZenType ReturnType { get; }
    public ZenValue Call(Interpreter interpreter, params ZenValue[] arguments);

}