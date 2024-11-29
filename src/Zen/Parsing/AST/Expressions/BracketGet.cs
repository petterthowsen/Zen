using Zen.Common;

namespace Zen.Parsing.AST.Expressions;

/// <summary>
/// Represents a bracket access expression like 'target[element]'.
/// </summary>
public class BracketGet : Expr
{
    public Expr Target;

    public Expr Element;

    public BracketGet(Expr target, Expr element)
    {
        Target = target;
        Element = element;
    }

    public override SourceLocation Location => Element.Location;

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
        return "BracketGet";
    }
}