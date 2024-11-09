using Zen.Common;

namespace Zen.Parsing.AST.Expressions;

public class Grouping(Expr expr) : Expr {

    public Expr Expression = expr;

    public override SourceLocation Location => Expression.Location;

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
        return "Grouping";
    }

}