using Zen.Typing;

namespace Zen.Execution.EvaluationResult;

/// <summary>
/// Represents the result of evaluating a TypeHint expression.
/// </summary>
public readonly struct TypeResult : IEvaluationResult {
    public required ZenType Type { get; init; }
    public ZenValue Value => ZenValue.Void;
    public bool IsTruthy() => true;
    public bool IsCallable() => false;

    public static implicit operator TypeResult(ZenType type) => new() { Type = type };
}