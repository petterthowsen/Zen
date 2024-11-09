namespace Zen.Common;

public class SyntaxError : Error {

    public SyntaxError(string message, ErrorType errorType, SourceLocation? location) : base(message, errorType, location) {
        prefix = "Syntax Error";
    }

}