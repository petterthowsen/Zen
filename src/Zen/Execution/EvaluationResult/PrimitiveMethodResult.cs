using Zen.Typing;

namespace Zen.Execution.EvaluationResult;

public struct PrimitiveMethodResult : IEvaluationResult
{
    public ZenValue Target;
    public ZenFunction Method;
    public ZenValue[] Arguments;

    public ZenType Type => ZenType.BoundMethod;

    public ZenValue Value => new ZenValue(ZenType.BoundMethod, Method);

    public bool IsCallable() => true;

    public bool IsTruthy() => true;

    public PrimitiveMethodResult(ZenValue target, ZenFunction method, ZenValue[] argValues)
    {
        Target = target;
        Method = method;
        Arguments = argValues;
    }
}