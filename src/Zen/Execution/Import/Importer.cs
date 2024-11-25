using Zen.Exection.Import;
using Zen.Execution.Import.Providing;

namespace Zen.Execution.Import;

/// <summary>
/// Manages the import system for the Zen runtime.
/// It can resolve names and nested names to namespaces, modules, and symbols.
/// It handles the execution/initialization of modules and provides their symbols.
/// </summary>
/// <remarks>
/// 1. .zen file Modules are only ever executed once.
/// 2. It uses one or more AbstractProvider implementations and they are checked in the order they are added.
/// 3. AbstractProvider caches the resolutions.
/// </remarks>

/*
    Workflow:
    Main script is executed.
    Interpreter visits an import statement.
    interpreter calls Importer.Resolve()
*/

public class Importer 
{

    public List<AbstractProvider> Providers { get; private set; } = [];
    
    public Importer(AbstractProvider[] providers)
    {
        foreach (var provider in providers) {
            Providers.Add(provider);
        }
    }

    public Importer() {}

    public void RegisterProvider(AbstractProvider provider)
    {
        Providers.Add(provider);
    }

    public ImportResolution Resolve(string path)
    {
        ImportResolution? resolution;

        foreach (var provider in Providers) {
            resolution = provider.Resolve(path);
            if (resolution != null) return resolution;
        }

        throw new Exception($"Cannot resolve {path}.");
    }

    /// <summary>
    /// Resolves the given path to a ImportResolution, then extracts symbols.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public Symbol[] Import(string path)
    {
        List<Symbol> symbols = [];

        ImportResolution resolution = Resolve(path);

        return [..symbols];
    }
}