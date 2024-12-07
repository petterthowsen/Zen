using Zen.Execution;

namespace Zen.Typing;

public interface ICallable {

    public int Arity { get; }
    public ZenType ReturnType { get; }
    public Task<ZenValue> Call(Interpreter interpreter, params ZenValue[] arguments);

}