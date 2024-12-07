using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST.Expressions;

namespace Zen.Parsing.AST.Statements;

/// <summary>
/// A traditional for statement with a initializer, condition, and incrementor.
/// </summary>
/// <example>
/// <code>
/// for i = 0; i < 10; i++ {
///     print i
/// }
/// </code>
/// </example>
public class ForStmt : Stmt {
    
    public Token Token;
    public Token LoopIdentifier;
    public TypeHint? TypeHint = null;

    public Expr Initializer;
    public Expr Condition;
    public Expr Incrementor;

    public Block Body;

    public override SourceLocation Location => Token.Location;

    public ForStmt(Token token, Token loopIdentifier, TypeHint typeHint, Expr initializer, Expr condition, Expr incrementor, Block block) {
        Token = token;
        LoopIdentifier = loopIdentifier;
        TypeHint = typeHint;
        Initializer = initializer;
        Condition = condition;
        Incrementor = incrementor;
        Body = block;
    }

    public ForStmt(Token token, Token loopIdentifier, Expr initializer, Expr condition, Expr incrementor, Block block) {
        Token = token;
        LoopIdentifier = loopIdentifier;
        Initializer = initializer;
        Condition = condition;
        Incrementor = incrementor;
        Body = block;
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
        return "ForStmt";
    }
}