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

    private dynamic? _value;

    public dynamic? Value => _value;

    public override SourceLocation Location => Token.Location;

    public Literal(LiteralKind kind, dynamic? value, Token token) {
        Kind = kind;
        Token = token;

        switch (Kind) {
            case LiteralKind.String:
                _value = new ZenValue(ZenType.String, value ?? "");
                break;
            case LiteralKind.Int:
                _value = new ZenValue(ZenType.Integer64, long.Parse(value));
                break;
            case LiteralKind.Float:
                _value = new ZenValue(ZenType.Float64, double.Parse(value));
                break;
            case LiteralKind.Bool:
                _value = new ZenValue(ZenType.Boolean, bool.Parse(value == true ? "true" : "false"));
                break;
            case LiteralKind.Null:
                _value = new ZenValue(ZenType.Null, null);
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
        return $"Literal: `{Value}`";
    }

}