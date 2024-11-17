using Zen.Common;

namespace Zen.Execution;

public class RuntimeError : Error {
    public RuntimeError(string message, ErrorType errorType, SourceLocation? location) : base(message, errorType, location) {
        prefix = "Runtime Error";
    }

    public RuntimeError(string message, SourceLocation? location) : base(message, ErrorType.RuntimeError, location) {}

    public RuntimeError(string message) : base(message, ErrorType.RuntimeError, null) {}
}