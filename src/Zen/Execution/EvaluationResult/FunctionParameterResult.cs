using Zen.Typing;

namespace Zen.Execution.EvaluationResult;


/// <summary>
/// Used by the Interpreter to parse <see cref="Zen.Parsing.AST.Expressions.FuncParameter"/>.
/// </summary>
public class FunctionParameterResult : IEvaluationResult
{
    public ZenFunction.Parameter Parameter;

    public ZenType Type => Parameter.Type;
    public ZenValue Value => ZenValue.Void;
    public bool IsCallable() => false;
    public bool IsTruthy() => true;

    public static implicit operator FunctionParameterResult(ZenFunction.Parameter parameter) => new() { Parameter = parameter };
}