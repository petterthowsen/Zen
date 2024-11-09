namespace Zen.Common;

public class Error : Exception {

    protected string prefix = "Error";

    public ErrorType Type { get; init; }

    public SourceLocation? Location {get; init;}

    public Error(string message, ErrorType errorType, SourceLocation? location) : base(message) {
        prefix = "Error";
        Type = errorType;
        Location = location;
    }

    public override string ToString()
    {
        if (Location != null) {
            return $"{Location}: {prefix}: {base.ToString()}";
        } else {
            return base.ToString();
        }
    }

}