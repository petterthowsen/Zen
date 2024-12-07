using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST.Expressions;

namespace Zen.Parsing.AST.Statements;

public class ClassStmt : Stmt
{
    public Token Token;
    public Token Identifier;

    public Token[] Modifiers;

    public PropertyStmt[] Properties = [];
    public MethodStmt[] Methods = [];
    public ParameterDeclaration[] Parameters = [];

    public Identifier? Extends;

    public ImplementsExpr[] Implements = [];

    public override SourceLocation Location => Token.Location;

    public ClassStmt(
        Token token,
        Token identifier,
        PropertyStmt[] properties,
        MethodStmt[] methods,
        ParameterDeclaration[] parameters,
        Token[] modifiers,
        Identifier? extends,
        ImplementsExpr[] implements
    )
    {
        Token = token;
        Identifier = identifier;
        Properties = properties;
        Methods = methods;
        Parameters = parameters;
        Modifiers = modifiers;
        Extends = extends;
        Implements = implements;
    }

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
        return "ClassStmt";
    }
}