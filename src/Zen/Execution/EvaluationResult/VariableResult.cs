using Zen.Typing;

namespace Zen.Execution.EvaluationResult;

public class VariableResult : IEvaluationResult {
    public required Variable Variable { get; init; }
    
    public ZenType Type => Variable.Type;
    public ZenValue Value => Variable.Value;
    public bool IsTruthy() => Variable.IsTruthy();
    public bool IsCallable() => Variable.IsCallable();

    public static implicit operator VariableResult(Variable variable) => new() { Variable = variable };
}