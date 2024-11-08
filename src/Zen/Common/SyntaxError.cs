namespace Zen.Common;

class SyntaxError : Error {

    private SourceLocation? location;

    public SyntaxError(string message, SourceLocation? location) : base(message) {
        prefix = "Syntax Error";
        this.location = location;
    }

    public override string ToString()
    {
        if (location != null) {
            return $"{location}: {prefix}: {Message}";
        } else {
            return base.ToString();
        }
    }

}