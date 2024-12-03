using Zen.Execution.Import;
using Zen.Typing;

namespace Zen.Exection.Import;

public enum SymbolType {
    Function,
    Class,
    Interface
}

/// <summary>
/// Represents an exported class, function, etc.
/// </summary>
public class Symbol
{
    public readonly string Name;
    public readonly SymbolType Type;
    
    public readonly Module Module;

    public Symbol(string name, SymbolType type, Module module)
    {
        Name = name;
        Type = type;
        Module = module;
    }
}