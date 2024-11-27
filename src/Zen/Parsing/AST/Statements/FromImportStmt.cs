using Microsoft.VisualBasic;
using Zen.Common;
using Zen.Lexing;

namespace Zen.Parsing.AST.Statements;

/// <summary>
/// Represents a from-import statement in the form:
/// from path[/to/module] import symbol1, symbol2
/// </summary>
public class FromImportStmt : Stmt
{
    public Token FromToken { get; }
    public string[] Path { get; }
    public Token[] Symbols { get; }

    public string PathString => string.Join("/", Path);

    public FromImportStmt(Token fromToken, string[] path, Token[] symbols)
    {
        FromToken = fromToken;
        Path = path;
        Symbols = symbols;
    }

    public List<string> GetSymbolNames()
    {
        List<string> names = [];

        foreach (var symbolToken in Symbols) {
            names.Add(symbolToken.Value);
        }

        return names;
    }

    public override SourceLocation Location => FromToken.Location;

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
        return "FromImport";
    }
}