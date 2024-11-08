namespace Zen.Common;

public class Error : Exception {

    protected string prefix = "Error";

    public Error(string message) : base(message) {

    }

    public override string ToString()
    {
        return $"{prefix}: {Message}";
    }

}