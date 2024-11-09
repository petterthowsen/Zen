using Zen.Common;

namespace Zen.Execution;

public class RuntimeError : Error {
    public RuntimeError(string message, ErrorType errorType, SourceLocation? location) : base(message, errorType, location) {
        prefix = "Runtime Error";
    }
}