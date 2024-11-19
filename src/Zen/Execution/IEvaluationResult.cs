using Zen.Typing;

namespace Zen.Execution;

public interface IEvaluationResult {
    ZenType Type { get; }
    ZenValue Value { get; }
    bool IsTruthy();
    bool IsCallable();
}

public readonly struct ValueResult : IEvaluationResult {
    public required ZenValue Value { get; init; }
    public ZenType Type => Value.Type;
    public bool IsTruthy() => Value.IsTruthy();
    public bool IsCallable() => Value.IsCallable();

    public static implicit operator ValueResult(ZenValue value) => new() { Value = value };
    public static implicit operator ValueResult(BoundMethod value) => new() { Value = new ZenValue(ZenType.BoundMethod, value) };
}

public class VariableResult : IEvaluationResult {
    public required Variable Variable { get; init; }
    
    public ZenType Type => Variable.Type;
    public ZenValue Value => Variable.GetZenValue();
    public ZenObject Object => Variable.GetZenObject() ! ;
    public bool IsTruthy() => Variable.IsTruthy();
    public bool IsCallable() => Variable.IsCallable();

    public static implicit operator VariableResult(Variable variable) => new() { Variable = variable };
}

public class FunctionParameterResult : IEvaluationResult
{
    public ZenType Type => Parameter.Type;

    public ZenValue Value => ZenValue.Void;

    public ZenFunction.Parameter Parameter;

    public static implicit operator FunctionParameterResult(ZenFunction.Parameter parameter) => new() { Parameter = parameter };

    public bool IsCallable()
    {
        return false;
    }

    public bool IsTruthy()
    {
        return true;
    }
}

public readonly struct VoidResult : IEvaluationResult {
    public static readonly VoidResult Instance = new();
    public ZenValue Value => ZenValue.Void;
    public ZenType Type => ZenType.Void;
    public bool IsTruthy() => false;
    public bool IsCallable() => false;
}

public readonly struct TypeResult : IEvaluationResult {
    public required ZenType Type { get; init; }
    public ZenValue Value => ZenValue.Void;
    public bool IsTruthy() => true;
    public bool IsCallable() => false;

    public static implicit operator TypeResult(ZenType type) => new() { Type = type };
}