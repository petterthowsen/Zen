using Zen.Parsing.AST.Expressions;

namespace Zen.Typing;

public class ZenAbstractMethod
{
    public bool Async = false;
    public bool Static = false;

    public ZenClass.Visibility Visibility;
    public string Name;
    public ZenType ReturnType { get; set; }
    public TypeHint? ReturnTypeHint { get; set; }  // Store original return type hint for resolving generics
    public List<ZenFunction.Argument> Arguments;

    public int Arity => Arguments.Count;

    public ZenAbstractMethod(bool async, bool @static, string name, ZenClass.Visibility visibility, ZenType returnType, List<ZenFunction.Argument> arguments)
    {
        Name = name;
        Visibility = visibility;
        ReturnType = returnType;
        Arguments = arguments;
        Async = async;
        Static = @static;
    }

    public override string ToString()
    {
        string asyncStr = Async ? "async " : "";
        return $"{asyncStr}{Name}({string.Join(", ", Arguments.Select(a => a.ToString()))}): {ReturnType.Name}";
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not ZenAbstractMethod other) return false;
        return Name == other.Name && Async == other.Async && Visibility == other.Visibility&& ReturnType == other.ReturnType && Arguments.SequenceEqual(other.Arguments);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Async, Visibility, ReturnType, Arguments);
    }

}