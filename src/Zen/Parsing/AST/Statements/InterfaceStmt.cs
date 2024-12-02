using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST.Expressions;

namespace Zen.Parsing.AST.Statements;

public class InterfaceStmt : Stmt
{
    public Token Token; // 'Interface' keyword token
    public Token Identifier; // Interface name

    public AbstractMethodStmt[] Methods = [];
    public ParameterDeclaration[] Parameters = [];

    public override SourceLocation Location => Token.Location;

    public InterfaceStmt(Token token, Token identifier, AbstractMethodStmt[] methods, ParameterDeclaration[] parameters)
    {
        Token = token;
        Identifier = identifier;
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
        return "InterfaceStmt";
    }
}