using Zen.Typing;

namespace Zen.Execution.EvaluationResult;

public readonly struct ValueResult : IEvaluationResult {
    public required ZenValue Value { get; init; }
    public ZenType Type => Value.Type;
    public bool IsTruthy() => Value.IsTruthy();
    public bool IsCallable() => Value.IsCallable();

    public ValueResult(ZenValue value)
    {
        Value = value;
    }

    public static implicit operator ValueResult(ZenValue value) => new() { Value = value };
    public static implicit operator ValueResult(BoundMethod value) => new() { Value = new ZenValue(ZenType.BoundMethod, value) };
    public static implicit operator ValueResult(bool value) => new() { Value = value ? ZenValue.True : ZenValue.False };
}