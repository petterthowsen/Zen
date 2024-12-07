using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST;


/// <summary>
/// Represents a package declaration in the form:
/// package name[.subpackage]
/// </summary>
public class PackageStmt : Stmt
{
    public Token PackageToken { get; }
    public string[] Path { get; }

    public PackageStmt(Token packageToken, string[] path)
    {
        PackageToken = packageToken;
        Path = path;
    }

    public override SourceLocation Location => PackageToken.Location;

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
        return "PackageStmt";
    }
}