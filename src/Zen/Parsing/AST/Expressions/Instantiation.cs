using Zen.Common;
using Zen.Lexing;

namespace Zen.Parsing.AST.Expressions;

public class Instantiation : Expr {
    
    public Token Token;
    public Call Call;

    public override SourceLocation Location => Call.Location;

    public Instantiation(Token token, Call call) {
        Token = token;
        Call = call;
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