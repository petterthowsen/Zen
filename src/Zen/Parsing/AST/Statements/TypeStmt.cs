using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST.Expressions;

namespace Zen.Parsing.AST.Statements;

public class TypeStmt(Token token, Identifier identifier, Identifier[] types) : Stmt
{
    public Token Token = token; // "type" keyword token
    public Identifier Identifier = identifier;
    public Identifier[] Types = types;

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
        return "TypeStmt";
    }
}
