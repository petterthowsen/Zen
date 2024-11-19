using Zen.Common;
using Zen.Lexing;

namespace Zen.Parsing.AST.Expressions;

public class Set : Expr
{
    public Expr ObjectExpression;
    public Token Identifier;

    public Token Operator;

    public Expr ValueExpression;

    public override SourceLocation Location => Identifier.Location;

    public Set(Token op, Expr objectExpression, Token identifier, Expr valueExpression) {
        Operator = op;
        ObjectExpression = objectExpression;
        Identifier = identifier;
        ValueExpression = valueExpression;
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
        return "Set";
    }
}