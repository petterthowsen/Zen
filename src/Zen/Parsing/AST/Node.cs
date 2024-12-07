using Zen.Common;
using Zen.Support;

namespace Zen.Parsing.AST;

public abstract class Node {

    public abstract SourceLocation Location { get; }

    public override abstract string ToString();
    public abstract void Accept(IVisitor visitor);
    public abstract ReturnType Accept<ReturnType>(IGenericVisitor<ReturnType> visitor);
    public abstract ReturnType AcceptAsync<ReturnType>(IGenericVisitorAsync<ReturnType> visitor);
}