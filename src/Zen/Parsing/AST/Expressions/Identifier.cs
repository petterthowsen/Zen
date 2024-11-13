using Zen.Common;
using Zen.Lexing;

namespace Zen.Parsing.AST.Expressions;

public class Identifier : Expr {
    public Token Token;

    public string Name => Token.Value;

    public override SourceLocation Location => Token.Location;

    public Identifier(Token token) {
        Token = token;
    }

    public override string ToString()
    {
        return $"Identifier: {Token.Value}";
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