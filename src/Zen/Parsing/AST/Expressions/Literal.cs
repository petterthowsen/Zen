using Zen.Common;
using Zen.Lexing;

namespace Zen.Parsing.AST.Expressions;

public class Literal(Literal.Kind kind, object? value, Token token) : Expr {

    public enum Kind {
        String,
        Int,
        Float,
        Bool,
        Null,
    }

    public Token Token = token;

    public Kind Type => kind;

    public object? Value => value;

    public override SourceLocation Location => Token.Location;

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