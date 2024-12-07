using Zen.Common;
using Zen.Lexing;
using Zen.Typing;

namespace Zen.Parsing.AST.Expressions;

public class TypeHint : Expr
{
    public Token Token;

    public string Name;
    public TypeHint[] Parameters = [];
    public bool Nullable = false;
    public bool IsGeneric = false;
    
    public bool IsParametric => Parameters.Length > 0;
    public override SourceLocation Location => Token.Location;

    public TypeHint(Token token, TypeHint[] parameters, bool nullable = false, bool generic = false) {
        Token = token;
        Name = Token.Value;
        Parameters = parameters;
        Nullable = nullable;
        IsGeneric = generic;
    }

    public TypeHint(Token token, bool nullable = false) {
        Token = token;
        Name = Token.Value;
        Nullable = nullable;
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
        string s = $"TypeHint({Name}";
        
        if (IsParametric) {
            s += "<" + string.Join<TypeHint>(", ", Parameters) + ">";
        }

        if (Nullable) {
            s += "?";
        }
        return s + ")";
    }
}
