using Zen.Common;
using Zen.Lexing;

namespace Zen.Parsing.AST.Statements;

public class IfStmt(Token token, Expr condition, Block then) : Stmt
{
    public Token Token = token;
    public Expr Condition = condition;
    public Block Then = then;

    public IfStmt[] ElseIfs = [];

    public Block? Else = null;

    public override SourceLocation Location => Condition.Location;

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
        return "IfStmt";
    }
}