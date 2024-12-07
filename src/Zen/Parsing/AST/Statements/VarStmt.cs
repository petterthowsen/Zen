using System.Linq.Expressions;
using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST.Expressions;

namespace Zen.Parsing.AST.Statements;

public class VarStmt : Stmt {

    public Token Token; // 'var' or 'const'
    public Token Identifier; // identifier

    public TypeHint? TypeHint;

    public Expr? Initializer; 

    public bool Constant => Token.Value == "const";

    public override SourceLocation Location => Token.Location;

    public VarStmt(Token token, Token identifier, TypeHint? typeHint, Expr? initializer) {
        Token = token;
        Identifier = identifier;
        TypeHint = typeHint;
        Initializer = initializer;
    }

    public override string ToString()
    {
        return "VarStmt" + (Constant ? " (const)" : "");
    }

    public override void Accept(IVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override ReturnType AcceptAsync<ReturnType>(IGenericVisitorAsync<ReturnType> visitor)
    {
        return visitor.VisitAsync(this);
    }

    public override ReturnType Accept<ReturnType>(IGenericVisitor<ReturnType> visitor)
    {
        return visitor.Visit(this);
    }
}