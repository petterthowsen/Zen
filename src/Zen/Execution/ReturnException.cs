using Zen.Common;
using Zen.Typing;

namespace Zen.Execution;

public class ReturnException : Exception {
    
    public IEvaluationResult Result;

    public SourceLocation Location;

    public ReturnException(IEvaluationResult result, SourceLocation location) {
        Result = result;
        Location = location;
    }
}