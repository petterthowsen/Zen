using Zen.Common;
using Zen.Lexing;

namespace Zen.Parsing.AST.Expressions;

public class Binary(Expr left, Token op, Expr right) : Expr {
    public Expr Left = left;
    public Token Operator = op;
    public Expr Right = right;

    public override SourceLocation Location => Operator.Location;

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

    public override string ToString() {
        return "Binary";
    }
}