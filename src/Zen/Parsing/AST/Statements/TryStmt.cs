using Zen.Common;
using Zen.Lexing;

namespace Zen.Parsing.AST.Statements;

public class TryStmt(Token token, Block block, CatchStmt[] catchStmts) : Stmt
{
    public Token Token = token;
    public Block Block = block;

    public CatchStmt[] catchStmts = catchStmts;

    public override SourceLocation Location => Token.Location;

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
        return "TryStmt";
    }
}