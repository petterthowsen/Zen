using Zen.Common;
using Zen.Lexing;
using Zen.Typing;

namespace Zen.Parsing.AST.Expressions;

public class Literal : Expr {

    public enum LiteralKind {
        String,
        Int,
        Float,
        Bool,
        Null,
    }

    public Token Token;

    public LiteralKind Kind;

    // is literal always a ZenValue?
    public ZenValue Value;

    public override SourceLocation Location => Token.Location;

    public Literal(LiteralKind kind, dynamic? underlying, Token token) {
        Kind = kind;
        Token = token;

        switch (Kind) {
            case LiteralKind.String:
                Value = new ZenValue(ZenType.String, underlying ?? "");
                break;
            case LiteralKind.Int:
                Value = new ZenValue(ZenType.Integer64, long.Parse(underlying));
                break;
            case LiteralKind.Float:
                Value = new ZenValue(ZenType.Float64, double.Parse(underlying));
                break;
            case LiteralKind.Bool:
                Value = new ZenValue(ZenType.Boolean, bool.Parse(underlying == true ? "true" : "false"));
                break;
            case LiteralKind.Null:
                Value = new ZenValue(ZenType.Null, null);
                break;
        }
    }

    public override void Accept(IVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override ReturnType Accept<ReturnType>(IGenericVisitor<ReturnType> visitor)
    {
        return visitor.Visit(this);
    }

    public override string ToString()
    {

        // string escapedValue = Value.ToString();
        // escapedValue = escapedValue.Replace("\n", "\\n");
        // escapedValue = escapedValue.Replace("\r", "\\r");
        // escapedValue = escapedValue.Replace("\t", "\\t");
        // escapedValue = escapedValue.Replace("\"", "\\\"");

        return $"Literal {Kind}, token: {Token}";
    }

}