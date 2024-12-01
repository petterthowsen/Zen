using Zen.Common;
using Zen.Lexing;

namespace Zen.Parsing.AST.Expressions;

/// <summary>
/// Represents a type check in the form: Expr is Expr
/// </summary>
public class TypeCheck : Expr
{
    public Expr Expression { get; }
    public Token Token; // "is"
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
