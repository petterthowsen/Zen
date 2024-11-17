using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST.Expressions;
using Zen.Typing;

namespace Zen.Parsing.AST.Statements;

public class FuncStmt : Stmt
{
    public bool Async;
    
    public Token Identifier;

    public TypeHint ReturnType;

    public FuncParameter[] Parameters;

    public Block Block;

    public FuncStmt(bool async, Token identifier, TypeHint returnType, FuncParameter[] parameters, Block block) {
        Async = async;
        Identifier = identifier;
        ReturnType = returnType;
        Parameters = parameters;
        Block = block;
    }

    public FuncStmt(bool async, Token identifier, ZenType returnType, FuncParameter[] parameters, Block block) {
        Async = async;
        Identifier = identifier;
        ReturnType = new TypeHint(new Token(TokenType.StringLiteral, returnType.Name, Identifier.Location), false);
        Parameters = parameters;
        Block = block;
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

    public override string ToString()
    {
        return "FuncStmt";
    }
}