using Zen.Common;
using Zen.Lexing;

namespace Zen.Parsing.AST.Statements;

public class PrintStmt : Stmt
{   
    public Token Token;
    public Expr Expression;

    public override SourceLocation Location => Token.Location;

    public PrintStmt(Token token, Expr expression) {
        Token = token;
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

    public override ReturnType AcceptAsync<ReturnType>(IGenericVisitorAsync<ReturnType> visitor)
    {
        return visitor.VisitAsync(this);
    }

    public override string ToString()
    {
        return "PrintStmt";
    }
}