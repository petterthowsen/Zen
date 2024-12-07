using Zen.Common;
using Zen.Lexing;

namespace Zen.Parsing.AST.Expressions;

public class Call : Expr {

    public Token Token;

    public Expr Callee;

    public Expr[] Arguments;

    public Call(Token token, Expr callee, Expr[] arguments) {
        Token = token;
        Callee = callee;
        Arguments = arguments;
    }

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
        return "Call";
    }
}