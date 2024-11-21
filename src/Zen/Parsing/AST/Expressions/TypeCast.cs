using Zen.Common;
using Zen.Lexing;
using Zen.Typing;

namespace Zen.Parsing.AST.Expressions;

public class TypeCast : Expr
{
    public Token Token; // Left parenthesis token for error reporting
    public TypeHint Type { get; }
    public Expr Expression { get; }
    public override SourceLocation Location => Token.Location;

    public TypeCast(Token token, TypeHint type, Expr expression)
    {
        Token = token;
        Type = type;
        Expression = expression;
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
        return $"TypeCast";
    }
}
