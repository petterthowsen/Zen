using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST.Expressions;

namespace Zen.Parsing.AST.Statements;

public class ForInStmt : Stmt {

    public Token Token;
    public override SourceLocation Location => Token.Location;
    
    public Token? KeyIdentifier = null;
    public TypeHint? KeyTypeHint = null;

    public Token ValueIdentifier;
    public TypeHint? ValueTypeHint = null;

    public Expr Expression;

    public Block Block;
    
    public bool IsKeyValuePair => KeyIdentifier != null;

    /// <summary>
    /// Create a For In Statmenet in of the form: 
    /// for key, value in expression { }
    /// </summary>
    /// <param name="token"></param>
    /// <param name="keyIdentifier"></param>
    /// <param name="keyTypeHint"></param>
    /// <param name="valueIdentifier"></param>
    /// <param name="valueTypeHint"></param>
    /// <param name="expression"></param>
    /// <param name="block"></param>
    public ForInStmt(Token token, Token? keyIdentifier, TypeHint? keyTypeHint, Token valueIdentifier, TypeHint? valueTypeHint, Expr expression, Block block) {
        Token = token;
        KeyIdentifier = keyIdentifier;
        KeyTypeHint = keyTypeHint;
        ValueIdentifier = valueIdentifier;
        ValueTypeHint = valueTypeHint;
        Expression = expression;
        Block = block;
    }

    /// <summary>
    /// Create a For In Statmenet in of the form: 
    /// for value in expression { }
    /// </summary>
    /// <param name="token"></param>
    /// <param name="keyIdentifier"></param>
    /// <param name="keyTypeHint"></param>
    /// <param name="valueIdentifier"></param>
    /// <param name="valueTypeHint"></param>
    /// <param name="expression"></param>
    /// <param name="block"></param>
    public ForInStmt(Token token, Token valueIdentifier, TypeHint? valueTypeHint, Expr expression, Block block) : this(token, null, null, valueIdentifier, valueTypeHint, expression, block) {

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
        if (KeyIdentifier != null) {
            return $"forIn({KeyIdentifier}:{KeyTypeHint}, {ValueIdentifier}:{ValueTypeHint})";
        }

        return $"forIn({ValueIdentifier}:{ValueTypeHint})";
    }
}