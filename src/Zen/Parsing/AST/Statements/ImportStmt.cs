using Zen.Common;
using Zen.Lexing;

namespace Zen.Parsing.AST.Statements;

/// <summary>
/// Represents an import statement in the form:
/// import path[/to/module] [as alias]
/// </summary>
public class ImportStmt : Stmt
{
    public Token ImportToken { get; }
    public string[] Path { get; }
    public Token? Alias { get; }

    public string PathString => string.Join("/", Path);

    public ImportStmt(Token importToken, string[] path, Token? alias = null)
    {
        ImportToken = importToken;
        Path = path;
        Alias = alias;
    }

    public override SourceLocation Location => ImportToken.Location;

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
        return "ImportStmt";
    }
}