using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST.Expressions;

namespace Zen.Parsing.AST.Statements;

public class ClassStmt : Stmt
{
    public Token Token;
    public Token Identifier;

    public PropertyStmt[] Properties = [];
    public MethodStmt[] Methods = [];
    public Parameter[] Parameters = [];

    public override SourceLocation Location => Token.Location;

    public ClassStmt(Token token, Token identifier, PropertyStmt[] properties, MethodStmt[] methods, Parameter[] parameters)
    {
        Token = token;
        Identifier = identifier;
        Properties = properties;
        Methods = methods;
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
        return "ClassStmt";
    }
}