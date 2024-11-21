using Zen.Common;
using Zen.Lexing;
using Zen.Typing;

namespace Zen.Parsing.AST.Expressions;

public class TypeCheck : Expr
{
    public Token Token; // "is"
    public Expr Expression { get; }
    public TypeHint Type { get; }
    public override SourceLocation Location => Token.Location;

    public TypeCheck(Token token, Expr expression, TypeHint type)
    {
        Token = token;
        Expression = expression;
        Type = type;
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
        return $"TypeCheck";
    }
}
