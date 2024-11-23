using Zen.Parsing.AST;

namespace Zen.Execution.Import;

/// <summary>
/// Represents a Zen module, which is a single source file containing one or more symbols.
/// </summary>
public class Module
{
    /// <summary>
    /// The path to the module file
    /// </summary>
    public string Path { get; }
    
    /// <summary>
    /// The namespace this module belongs to
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    /// The symbols defined in this module
    /// </summary>
    public List<Symbol> Symbols { get; }

    /// <summary>
    /// The AST for this module
    /// </summary>
    public ProgramNode? Ast { get; }

    /// <summary>
    /// The environment for this module's execution
    /// </summary>
    public Environment? Environment { get; set; }

    /// <summary>
    /// Whether this module contains exactly one symbol
    /// </summary>
    public bool IsSingleSymbol => Symbols.Count == 1;

    /// <summary>
    /// Module-specific imports and aliases
    /// </summary>
    private readonly Dictionary<string, Symbol> _imports = new();

    public Module(string path, List<Symbol> symbols, ProgramNode? ast)
    {
        Path = path;
        Namespace = path.Split('/')[0];
        Symbols = symbols;
        Ast = ast;
    }

    /// <summary>
    /// Creates a directory module that contains symbols from multiple files.
    /// </summary>
    public static Module CreateDirectoryModule(string path, List<Symbol> symbols)
    {
        return new Module(path, symbols, null);
    }

    /// <summary>
    /// Creates a file module that contains symbols from a single file.
    /// </summary>
    public static Module CreateFileModule(string path, List<Symbol> symbols, ProgramNode ast)
    {
        return new Module(path, symbols, ast);
    }

    /// <summary>
    /// Adds an imported symbol to this module, optionally with an alias.
    /// </summary>
    public void AddImport(string name, Symbol symbol)
    {
        _imports[name] = symbol;
    }

    /// <summary>
    /// Resolves an imported symbol by name.
    /// </summary>
    public Symbol? ResolveImport(string name)
    {
        return _imports.GetValueOrDefault(name);
    }
}
