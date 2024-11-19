using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST.Expressions;

namespace Zen.Parsing.AST.Statements;

public class PropertyStmt : Stmt
{

    public Token Identifier;

    public TypeHint? TypeHint;

    public Token[] Modifiers = [];

    public Expr? Initializer = null;

    public override SourceLocation Location => Identifier.Location;

    public PropertyStmt(Token identifier, TypeHint? typeHint, Expr? initializer, Token[] modifiers) {
        Identifier = identifier;
        TypeHint = typeHint;
        Initializer = initializer;
        Modifiers = modifiers;
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
        return "PropertyStmt";
    }
}