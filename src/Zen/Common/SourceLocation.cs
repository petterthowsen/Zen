namespace Zen.Common;

public readonly struct SourceLocation {

    public static readonly SourceLocation Unknown = new SourceLocation { Line = -1, Column = -1, Code = null };

    public required AbstractSourceCode? Code { get; init; }
    public required int Line { get; init; }
    public required int Column { get; init; }

    public override string ToString()
    {
        if (Code == null) {
            return $"({Line},{Column})";
        }else {
            return $"{Code}({Line},{Column})";
        }
    }
}