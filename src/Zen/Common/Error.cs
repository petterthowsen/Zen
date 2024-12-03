namespace Zen.Common;

public class Error : Exception {

    protected string prefix = "Error";

    public ErrorType Type { get; init; }

    public SourceLocation? Location {get; init;}

    private string _message;
    public override string Message => GetFormattedMessage();

    public Error(string message, ErrorType errorType, SourceLocation? location, Exception? innerException = null) : base(message, innerException) {
        _message = message;
        prefix = "Error";
        Type = errorType;
        Location = location;
    }

    public string GetFormattedMessage() {
        string msg = $"{Location}: {prefix}: {_message}";
        if (Location != null) {
            msg += "\n\n" + ((SourceLocation) Location!).GetExcerpt() + "\n";
        }
        return msg;
    }

    // public override string ToString()
    // {
    //     if (Location != null) {
    //         string msg = $"{Location}: {prefix}: {Message}\n";
    //         msg += ((SourceLocation) Location).GetExcerpt() + "\n";
    //         return msg;
    //     } else {
    //         return base.ToString();
    //     }
    // }

}