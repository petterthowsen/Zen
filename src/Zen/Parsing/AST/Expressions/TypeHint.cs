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

    public override SourceLocation Location => Token.Location;

    public bool IsGeneric = false;

    public TypeHint(Token token, TypeHint[] parameters, bool nullable = false, bool generic = false) {
        Token = token;
        Parameters = parameters;
        Nullable = nullable;
        IsGeneric = generic;
    }

    public TypeHint(Token token, bool nullable = false) {
        Token = token;
        Nullable = nullable;
    }

    public bool IsPrimitive() {
        return ZenType.Exists(Name);
    }

    public ZenType GetBaseZenType() {
        // If this is a generic parameter (like T), return ZenType.Type
        if (IsGeneric) {
            return ZenType.Type;
        }
        return ZenType.FromString(Name, Nullable);
    }

    public ZenType GetZenType() {
        if (IsParametric) {
            var paramTypes = Parameters.Select(p => p.GetZenType()).ToArray();
            return new ZenType(Name, Nullable, paramTypes);
        }
        return GetBaseZenType();
    }

    public override bool Equals(object? obj) {
        if (obj is TypeHint other) {
            return Name == other.Name && 
                   Nullable == other.Nullable && 
                   Parameters.SequenceEqual(other.Parameters);
        }
        return false;
    }

    public override int GetHashCode() {
        return HashCode.Combine(Name, Nullable, Parameters);
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
            s += "<" + string.Join<TypeHint>(", ", Parameters) + ">";
        }

        if (Nullable) {
            s += "?";
        }
        return s;
    }
}
