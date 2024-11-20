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

    /// <summary>
    /// return the current line, plus a few lines above and below.
    /// </summary>
    /// <returns></returns>
    public string GetExcerpt() {
        if (Code == null) return "";

        string result = "";
        if (Line > 0) {
            result += Code!.GetLine(Line - 1) + "\n";
        }

        result += Code!.GetLine(Line) + "\n";

        // add a caret at the column
        for (int i = 0; i < Column; i++) {
            result += " ";
        }
        result += "^";

        if (Line < Code.Lines.Length - 1) {
            result += "\n" + Code!.GetLine(Line + 1) + "\n";
        }

        return result;
    }
}