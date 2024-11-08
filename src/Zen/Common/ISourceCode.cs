namespace Zen.Common;

public interface ISourceCode{   
    public string Code { get; }
    public int Length { get; }
    public int LineCount { get; }
    public string GetLine(int lineNumber);
    public char GetCharAt(int line, int column);
    public char GetChar(int index);
    public SourceLocation MakeLocation(int line, int column);
}