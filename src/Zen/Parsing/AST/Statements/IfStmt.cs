namespace Zen.Parsing.AST.Statements;

public class IfStmt : Stmt
{
    public required Expr Condition { get; set; }
    public required Stmt Then { get; set; }

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