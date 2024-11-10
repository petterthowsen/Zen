namespace Zen.Common;

public abstract class AbstractSourceCode : ISourceCode {
    public string Code { get; init; }

    public int Length => Code.Length;

    public  string[] Lines {get; init;}

    public int LineCount => Lines.Length;

    protected AbstractSourceCode(string code)
    {
        Code = code;
        Lines = Code.Split('\n');
    }


    public string GetLine(int line) => Lines[line];

    public char GetChar(int index) => Code[index];

    public char GetCharAt(int line, int column) => GetLine(line)[column];

    public string Substring(Range range) => Code[range];

    public SourceLocation MakeLocation(int line, int column) {
        return new SourceLocation {
            Code = this,
            Line = line,
            Column = column
        };
    }

    public override string ToString()
    {
        return "[inline code of length " + Length + "]";
    }
}