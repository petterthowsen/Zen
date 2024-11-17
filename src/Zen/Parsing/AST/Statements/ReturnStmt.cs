using Zen.Common;
using Zen.Lexing;

namespace Zen.Parsing.AST.Statements;

public class ReturnStmt : Stmt
{
    public Token Token;
    public Expr? Expression;

    public override SourceLocation Location => Token.Location;

    public ReturnStmt(Token token, Expr? expression = null) {
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

    public override string ToString()
    {
        return $"ReturnStmt";
    }
}