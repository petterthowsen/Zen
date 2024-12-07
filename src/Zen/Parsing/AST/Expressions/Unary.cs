using Zen.Common;
using Zen.Lexing;

namespace Zen.Parsing.AST.Expressions;

public class Unary(Token op, Expr right) : Expr {
    public Token Operator = op;
    public Expr Right = right;

    public override SourceLocation Location => Operator.Location;

    public bool IsNot() => Operator.Type == TokenType.Keyword && Operator.Value == "not";
    public bool IsMinus() => Operator.Type == TokenType.Minus;

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
        return "Unary";
    }
}