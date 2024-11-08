namespace Zen.Common;

public readonly struct SourceLocation {
    public required AbstractSourceCode Code { get; init; }
    public required int Line { get; init; }
    public required int Column { get; init; }

    public override string ToString()
    {
        return $"Code({Line},{Column})";
    }
}