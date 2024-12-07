using Zen.Common;
using Zen.Lexing;

namespace Zen.Parsing.AST.Statements;

public class ThrowStmt(Token token, Expr expression) : Stmt
{
    public Token Token = token;
    public Expr Expression = expression;

    public override SourceLocation Location => Token.Location;
    
    public override void Accept(IVisitor visitor)
    {
        throw new NotImplementedException();
    }

    public override ReturnType Accept<ReturnType>(IGenericVisitor<ReturnType> visitor)
    {
        throw new NotImplementedException();
    }

    public override ReturnType AcceptAsync<ReturnType>(IGenericVisitorAsync<ReturnType> visitor)
    {
        return visitor.VisitAsync(this);
    }

    public override string ToString()
    {
        throw new NotImplementedException();
    }
}