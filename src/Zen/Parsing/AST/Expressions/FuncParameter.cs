using Zen.Common;
using Zen.Lexing;

namespace Zen.Parsing.AST.Expressions;

public class FuncParameter : Expr
{
    public Token Identifier;

    public TypeHint? TypeHint;

    public Expr? DefaultValue;

    public override SourceLocation Location => Identifier.Location;

    public FuncParameter(Token identifier, TypeHint? typeHint, Expr? defaultValue) {
        Identifier = identifier;
        TypeHint = typeHint;
        DefaultValue = defaultValue;
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
        return "FuncParameter";
    }
}