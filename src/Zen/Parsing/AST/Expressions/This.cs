using Zen.Common;
using Zen.Lexing;

namespace Zen.Parsing.AST.Expressions;

public class This(Token token) : Expr
{
    public Token Token = token;

    public override SourceLocation Location => Token.Location;

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
        return "This";
    }
}