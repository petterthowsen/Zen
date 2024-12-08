using Zen.Execution;

namespace Zen.Typing;

public interface ICallable {
    int Arity { get; }
    ZenType ReturnType { get; }
}