using Zen.Common;
using Zen.Lexing;
using Zen.Typing;

namespace Zen.Parsing.AST.Expressions;

public class TypeHint : Expr
{
    public Token Token;
    public TypeHint[] Parameters = [];
    public bool Nullable = false;
    
    public string Name => Token.Value;
    public bool IsParametric => Parameters.Length > 0;
    public bool IsGeneric => Parameters.Length > 0;

    public override SourceLocation Location => Token.Location;

    public TypeHint(Token token, TypeHint[] parameters, bool nullable = false) {
        Token = token;
        Parameters = parameters;
        Nullable = nullable;
    }

    public ZenType GetBaseZenType() {
        return ZenType.FromString(Name);
    }

    public ZenType GetZenType() {
        if (IsParametric) {
            return new ZenType(Name, Parameters.Select(p => p.GetZenType()).ToArray());
        }
        return GetBaseZenType();
    }

    public override bool Equals(object? obj) {
        if (obj is TypeHint other) {
            return Name == other.Name && Nullable == other.Nullable && Parameters.SequenceEqual(other.Parameters);
        }
        return false;
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
        string s = $"TypeHint: {Name}";
        
        if (IsParametric) {
            s += "<";
            s += "<" + string.Join<TypeHint>(", ", Parameters) + ">";
        }

        if (Nullable) {
            s += "?";
        }
        return s;
    }
}