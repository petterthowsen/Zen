using Zen.Common;
using Zen.Lexing;

namespace Zen.Parsing.AST.Statements;

public class WhileStmt(Token token, Expr condition, Block body) : Stmt
{   
    public Token Token = token;
    public Expr Condition = condition;
    public Block Body = body;

    public override SourceLocation Location => Token.Location;

    public override string ToString()
    {
        return "WhileStmt";
    }

    public override void Accept(IVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override ReturnType Accept<ReturnType>(IGenericVisitor<ReturnType> visitor)
    {
        return visitor.Visit(this);
    }
}
