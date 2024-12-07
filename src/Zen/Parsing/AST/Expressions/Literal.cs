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
        Keyword,
    }

    public Token Token;

    public LiteralKind Kind;

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
                Value = new ZenValue(ZenType.Integer, int.Parse(underlying));
                break;
            case LiteralKind.Float:
                Value = new ZenValue(ZenType.Float, float.Parse(underlying));
                break;
            case LiteralKind.Bool:
                Value = underlying == true ? ZenValue.True : ZenValue.False;
                break;
            case LiteralKind.Null:
                Value = ZenValue.Null;
                break;
            case LiteralKind.Keyword:
                Value = new ZenValue(ZenType.Keyword, underlying);
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

    public override ReturnType AcceptAsync<ReturnType>(IGenericVisitorAsync<ReturnType> visitor)
    {
        return visitor.VisitAsync(this);
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