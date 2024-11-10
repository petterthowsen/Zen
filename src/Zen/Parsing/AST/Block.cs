using Zen.Common;
using Zen.Lexing;

namespace Zen.Parsing.AST;

public class Block(Token openBrace, Stmt[] body, Token? closeBrace = null) : Node
{
    public Token OpenBrace = openBrace;

    public Stmt[] Body = body;

    public Token? CloseBrace = closeBrace;

    public override SourceLocation Location => OpenBrace.Location;

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
        return "Block";
    }
}