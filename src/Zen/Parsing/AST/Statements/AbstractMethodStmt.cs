using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST.Expressions;
using Zen.Typing;

namespace Zen.Parsing.AST.Statements;

public class AbstractMethodStmt : Stmt
{

    public Token Identifier;
    public TypeHint ReturnType;
    public FuncParameter[] Parameters;
    public Token[] Modifiers;
    public bool Async;

    public AbstractMethodStmt(Token identifier, TypeHint returnType, FuncParameter[] parameters, Token[] modifiers)
    {
        Identifier = identifier;
        ReturnType = returnType;
        Parameters = parameters;
        Modifiers = modifiers;
        Async = HasModifier("async");
    }

    public AbstractMethodStmt(bool async, Token identifier, ZenType returnType, FuncParameter[] parameters, Token[] modifiers)
    : this(
        identifier,
        new TypeHint(new Token(TokenType.StringLiteral, returnType.Name, identifier.Location), false),
        parameters,
        modifiers)
    {
        
    }

    public override SourceLocation Location => throw new NotImplementedException();

    public override void Accept(IVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override ReturnType Accept<ReturnType>(IGenericVisitor<ReturnType> visitor)
    {
        return visitor.Visit(this);
    }

    public bool HasModifier(string modifier) => Modifiers.Any(m => m.Value == modifier);

    public override string ToString()
    {
        return "AbstractMethodStmt";
    }
}