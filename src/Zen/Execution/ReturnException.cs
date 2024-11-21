using Zen.Common;
using Zen.Execution.EvaluationResult;

namespace Zen.Execution;

/// <summary>
/// Thrown when a return statement is encountered.
/// </summary>
/// <remarks>
/// A <see cref="IEvaluationResult"/> is stored in the <see cref="Result"/> property.
/// </remarks>
public class ReturnException : Exception {
    
    public IEvaluationResult Result;

    public SourceLocation Location;

    public ReturnException(IEvaluationResult result, SourceLocation location) {
        Result = result;
        Location = location;
    }
}