using Zen.Common;
using Zen.Lexing;

namespace Zen.Parsing.AST.Expressions;

public class Get : Expr
{
    public Expr Expression;
    public Token Identifier;

    public override SourceLocation Location => Identifier.Location;

    public Get(Expr expression, Token identifier) {
        Expression = expression;
        Identifier = identifier;
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
        return $"Get";
    }
}