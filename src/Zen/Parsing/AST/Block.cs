using Zen.Common;
using Zen.Lexing;

namespace Zen.Parsing.AST;

public class Block(Token openBrace, Stmt[] statements, Token? closeBrace = null) : Node
{
    public Token OpenBrace = openBrace;

    public Stmt[] Statements = statements;

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

    public override ReturnType AcceptAsync<ReturnType>(IGenericVisitorAsync<ReturnType> visitor)
    {
        return visitor.VisitAsync(this);
    }

    public override string ToString()
    {
        return "Block";
    }
}