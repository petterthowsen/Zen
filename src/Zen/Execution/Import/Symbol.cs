using Zen.Typing;

namespace Zen.Exection.Import;

/// <summary>
/// Represents an exported class, function, etc.
/// </summary>
public class Symbol
{
    public string Name;
    public ZenValue Value;
    
    public Symbol(string name, ZenValue value)
    {
        Name = name;
        Value = value;
    }
}