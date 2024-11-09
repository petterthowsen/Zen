using System.Text;

namespace Zen.Support;

public class IndentedStringBuilder
{
    private readonly StringBuilder _sb = new();
    public override string ToString() => _sb.ToString();

    public string IndentationString = "  ";

    private int _indentLevel = 0;
    public int Indent
    {
        get => _indentLevel;
        set => _indentLevel = value >= 0 ? value : 0;
    }
    
    public static string Repeat(string s, int count) {
        string str = "";
        for (int i = 0; i < count; i++) {
            str += s;
        }
        return str;
    }

    public void Add(string s) {
        // remove trailing and leading newlines
        s = s.TrimStart('\n');
        s = s.TrimEnd('\n');

        foreach (var line in s.Split('\n')) {
            _sb.Append(Repeat(IndentationString, _indentLevel) + line + "\n");
        }
    }
}