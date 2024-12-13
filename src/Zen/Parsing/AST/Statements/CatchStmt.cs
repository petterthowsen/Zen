using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST;
using Zen.Parsing.AST.Expressions;

namespace Zen.Parsing.AST.Statements;

public class CatchStmt(Token token, Identifier identifier, TypeHint? typeHint, Block block) : Stmt
{
    public Token Token = token;
    public Identifier Identifier = identifier;
    public TypeHint? TypeHint = typeHint;
    public Block Block = block;    

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
        return "CatchStmt";
    }
}