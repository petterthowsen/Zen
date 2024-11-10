using Zen.Common;

namespace Zen.Parsing.AST.Statements;

public class ExpressionStmt : Stmt {

    public Expr Expression;

    public override SourceLocation Location => Expression.Location;

    public ExpressionStmt(Expr expression) {
        Expression = expression;
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
        return "ExpressionStmt";
    }
}