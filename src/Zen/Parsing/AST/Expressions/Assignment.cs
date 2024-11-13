using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST.Expressions;

namespace Zen.Parsing.AST;

public class Assignment : Expr {

    public Token Operator;

    public Identifier Identifier;

    public Expr Expression;

    public Assignment(Token op, Identifier identifier, Expr expression) {
        Operator = op;
        Identifier = identifier;
        Expression = expression;
    }

    public override SourceLocation Location => Identifier.Location;

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
        return "Assignment";
    }
}