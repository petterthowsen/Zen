using Zen.Common;

namespace Zen.Lexing;

public readonly struct Token
{
    public TokenType Type { get; }
    public readonly string Name => Type.ToString();

    public readonly string Value { get; }
    public SourceLocation Location { get; }

    public Token(TokenType type, string value, SourceLocation location)
    {
        Type = type;
        Value = value;
        Location = location;
    }

    public override readonly string ToString()
    {
        string name = Type.ToString();

        return $"{name}" + (Value != "" ? $"(`{Value}`)" : "");
    }
}