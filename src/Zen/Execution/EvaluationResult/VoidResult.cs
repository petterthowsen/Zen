using System.Diagnostics.CodeAnalysis;
using Zen.Typing;

namespace Zen.Execution.EvaluationResult;

public readonly struct VoidResult : IEvaluationResult {
    public static readonly VoidResult Instance = new();

    public ZenValue Value => ZenValue.Void;
    public ZenType Type => ZenType.Void;
    public bool IsTruthy() => false;
    public bool IsCallable() => false;

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is VoidResult) {
            return true;
        }
        else if (obj is ValueResult val) {
            return val.Type == ZenType.Void;
        }else {
            return false;
        }
    }
}