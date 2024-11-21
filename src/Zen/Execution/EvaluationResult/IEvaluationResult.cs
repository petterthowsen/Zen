using Zen.Typing;

namespace Zen.Execution.EvaluationResult;

/// <summary>
/// Represents the result of evaluating an expression or statement.
/// </summary>
public interface IEvaluationResult {

    /// <summary>
    /// The type of the result.
    /// </summary>
    ZenType Type { get; }

    /// <summary>
    /// The value of the result.
    /// </summary>
    ZenValue Value { get; }

    /// <summary>
    /// Whether the result is truthy.
    /// </summary>
    bool IsTruthy();

    /// <summary>
    /// Whether the result is callable.
    /// </summary>
    /// <remarks>
    /// A callable value is a ZenFunction or a bound method.
    /// </remarks>
    bool IsCallable();
}