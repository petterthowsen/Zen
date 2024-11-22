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
    public List<Symbol> Symbols { get; }
    public ProgramNode? Ast { get; }
    public Environment? Environment { get; set; }

    public bool IsSingleSymbol => Symbols.Count == 1;

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
}
