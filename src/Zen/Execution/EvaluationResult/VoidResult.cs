using Zen.Typing;

namespace Zen.Execution.EvaluationResult;

public readonly struct VoidResult : IEvaluationResult {
    public static readonly VoidResult Instance = new();

    public ZenValue Value => ZenValue.Void;
    public ZenType Type => ZenType.Void;
    public bool IsTruthy() => false;
    public bool IsCallable() => false;
}