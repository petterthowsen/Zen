using Zen.Common;
using Zen.Lexing;

namespace Zen.Parsing.AST.Expressions;

public class Instantiation : Expr {
    
    public Token Token;
    public Call Call;
    public List<Expr> Parameters = [];

    public override SourceLocation Location => Call.Location;

    public Instantiation(Token token, Call call, List<Expr> parameters) {
        Token = token;
        Call = call;
        Parameters = parameters;
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
        return $"Instantiation";
    }
}
