using Zen.Typing;

namespace Zen.Execution;

public interface IEvaluationResult {
    ZenType Type { get; }
    ZenValue Value { get; }
    bool IsTruthy();
}

public readonly struct ValueResult : IEvaluationResult {
    public required ZenValue Value { get; init; }
    public ZenType Type => Value.Type;
    public bool IsTruthy() => Value.IsTruthy();

    public static implicit operator ValueResult(ZenValue value) => new() { Value = value };
}

public class VariableResult : IEvaluationResult {
    public required Variable Variable { get; init; }
    
    public ZenType Type => Variable.Type;
    public ZenValue Value => Variable.GetZenValue();
    public ZenObject Object => Variable.GetZenObject() ! ;
    public bool IsTruthy() => Variable.IsTruthy();

    public static implicit operator VariableResult(Variable variable) => new() { Variable = variable };
}

public readonly struct VoidResult : IEvaluationResult {
    public static readonly VoidResult Instance = new();
    public ZenValue Value => ZenValue.Void;
    public ZenType Type => ZenType.Void;
    public bool IsTruthy() => false;
}

public readonly struct TypeResult : IEvaluationResult {
    public required ZenType Type { get; init; }
    public ZenValue Value => ZenValue.Void;
    public bool IsTruthy() => false;

    public static implicit operator TypeResult(ZenType type) => new() { Type = type };
}