using Zen.Common;

namespace Zen.Parsing.AST.Expressions;

public class ImplementsExpr : Expr
{

    public Identifier Identifier;

    public List<Expr> Parameters;

    public override SourceLocation Location => Identifier.Location;

    public ImplementsExpr(Identifier identifier, List<Expr> parameters) {
        Identifier = identifier;
        Parameters = parameters;
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
        return $"ImplementsExpr";
    }
}