using Zen.Exection.Import;
using Zen.Typing;
using Zen.Common;
using Zen.Parsing.AST;

namespace Zen.Execution.Import;

public enum State {
    NotLoaded,          // Initial state
    Parsing,            // During lexing/parsing
    ParseComplete,      // AST is ready
    DeclaringTypes,     // Creating placeholder types
    ResolvingImports,   // Finding and validating imports
    ImportsResolved,    // All imports found and validated
    Executing,          // Running module code
    Executed            // Module fully executed
}

/// <summary>
/// Represents a single .zen file that may have one or more exported symbols.
/// </summary>
public class Module 
{
    public string FullPath { get; }
    public string Name { get; }

    public State State = State.NotLoaded;

    // Execution environment
    public Environment environment;
    
    // Top-level symbols
    public List<Symbol> Symbols = [];

    // Source code for this module
    public ISourceCode Source { get; private set; }

    // AST after parsing
    public ProgramNode? AST { get; set; }

    // Track local imports (what this module imports)
    public Dictionary<string, ImportResolution> LocalImports = [];

    // Track what modules this module depends on
    public HashSet<Module> Dependencies = [];

    // Track what modules depend on this module
    public HashSet<Module> Dependers = [];

    // Cache for resolved imports to improve performance
    private Dictionary<string, ImportResolution> _importCache = [];

    public Module(string fullPath, ISourceCode source)
    {
        FullPath = fullPath;
        Name = FullPath.Split('/').Last();
        Source = source;
    }

    /// <summary>
    /// Checks if this module has a symbol with the given name
    /// </summary>
    public bool HasSymbol(string name)
    {
        foreach (var symbol in Symbols)
        {
            if (symbol.Name == name) return true;
        }
        
        return false;
    }

    /// <summary>
    /// Adds a local import and its resolution
    /// </summary>
    public void AddLocalImport(string importPath, ImportResolution resolution)
    {
        LocalImports[importPath] = resolution;
    }

    /// <summary>
    /// Adds a module that this module depends on
    /// </summary>
    public void AddDependency(Module module)
    {
        Dependencies.Add(module);
        // When we depend on a module, we should register ourselves as a depender of that module
        module.AddDepender(this);
    }

    /// <summary>
    /// Adds a module that depends on this module
    /// </summary>
    public void AddDepender(Module module)
    {
        Dependers.Add(module);
    }

    /// <summary>
    /// Gets a cached import resolution or returns null if not cached
    /// </summary>
    public ImportResolution? GetCachedImport(string importPath)
    {
        return _importCache.TryGetValue(importPath, out var resolution) ? resolution : null;
    }

    /// <summary>
    /// Caches an import resolution
    /// </summary>
    public void CacheImport(string importPath, ImportResolution resolution)
    {
        _importCache[importPath] = resolution;
    }

    /// <summary>
    /// Checks if this module has a circular dependency with another module
    /// </summary>
    public bool HasCircularDependencyWith(Module other)
    {
        // If we depend on the other module and they depend on us
        return Dependencies.Contains(other) && other.Dependencies.Contains(this);
    }

    /// <summary>
    /// Clears the import cache
    /// </summary>
    public void ClearImportCache()
    {
        _importCache.Clear();
    }
}
