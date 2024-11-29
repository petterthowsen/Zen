using Zen.Common;
using Zen.Lexing;

namespace Zen.Parsing.AST.Expressions;

/// <summary>
/// Represents a bracket access expression like 'target[element]'.
/// </summary>
public class BracketSet : Expr
{
    public Expr Target;
    public Expr Element;
    
    public Token Operator;

    public Expr ValueExpression;

    public override SourceLocation Location => Element.Location;

    public BracketSet(Token op, Expr target, Expr element, Expr valueExpr)
    {
        Operator = op;
        Target = target;
        Element = element;
        ValueExpression = valueExpr;
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
        return "BracketSet";
    }
}