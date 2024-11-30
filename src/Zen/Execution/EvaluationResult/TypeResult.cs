using Zen.Typing;

namespace Zen.Execution.EvaluationResult;

/// <summary>
/// Represents the result of evaluating a TypeHint expression.
/// </summary>
public readonly struct TypeResult : IEvaluationResult {
    public ZenType Type { get; init; }
    public ZenValue Value { get; } = ZenValue.Void;
    public bool IsTruthy() => true;
    public bool IsCallable() => false;

    public bool IsClass() => Type is ZenTypeClass;

    public TypeResult(ZenType type)
    {
        Type = type;
        Value = new ZenValue(ZenType.Type, Type);
    }

    public static implicit operator TypeResult(ZenType type) => new TypeResult(type);
}