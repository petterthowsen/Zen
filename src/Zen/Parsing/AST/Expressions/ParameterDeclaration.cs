using Zen.Common;
using Zen.Lexing;

namespace Zen.Parsing.AST.Expressions;

/// <summary>
/// Represents a generic class parameter in a class or function definition.
/// Can be either a type parameter "T" or a constraint like "SIZE:int".
/// constraints can have default values, E.g "SIZE:int = 100"
/// </summary>
public class ParameterDeclaration : Expr
{
    public string Name; // "T", "SIZE" etc.
    public TypeHint Type; // "int", "Array<string>" etc.
    public Expr? DefaultValue;
    public bool IsTypeParameter;
    public override SourceLocation Location => Type.Location;

    public ParameterDeclaration(string name, TypeHint type, Expr? defaultValue = null, bool isTypeParameter = true)
    {
        Name = name;
        Type = type;
        DefaultValue = defaultValue;
        IsTypeParameter = isTypeParameter;
    }

    public override string ToString()
    {
        if (IsTypeParameter)
            return Name;
        return $"{Name}: {Type}" + (DefaultValue != null ? $" = {DefaultValue}" : "");
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
}
