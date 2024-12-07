using Zen.Common;

namespace Zen.Parsing.AST;

public class ProgramNode : Node {

    public List<Stmt> Statements = [];

    public override SourceLocation Location => Statements[0].Location;

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

    public override string ToString() {
        return "Program";
    }

}