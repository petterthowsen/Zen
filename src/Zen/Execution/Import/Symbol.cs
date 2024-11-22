using Zen.Parsing.AST;
using Zen.Typing;

namespace Zen.Execution.Import;

/// <summary>
/// The type of a symbol.
/// </summary>
public enum SymbolType
{
    Function,
    Class,
    Variable,
    Type
}

/// <summary>
/// Represents a symbol that can be imported, such as a function or class.
/// Variables, even if they are constants, are not exported by default.
/// </summary>
public class Symbol
{
    /// <summary>
    /// The name of the symbol
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The type of the symbol (function, class, etc)
    /// </summary>
    public SymbolType Type { get; }

    /// <summary>
    /// The module that defines this symbol
    /// </summary>
    public Module Module { get; }

    /// <summary>
    /// The AST node that defines this symbol
    /// </summary>
    public Node Node { get; }

    /// <summary>
    /// Whether this symbol is public (can be imported)
    /// </summary>
    public bool IsPublic { get; }

    public Symbol(string name, SymbolType type, Module definingModule, Node node, bool isPublic = true)
    {
        Name = name;
        Type = type;
        Module = definingModule;
        Node = node;
        IsPublic = isPublic;
    }
}