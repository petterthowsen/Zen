namespace Zen.Execution.Import;

/// <summary>
/// Represents a Zen package, which is a collection of modules under a root namespace.
/// </summary>
public class Package
{
    /// <summary>
    /// The root namespace of the package, as defined in package.zen
    /// </summary>
    public string RootNamespace { get; }

    /// <summary>
    /// The filesystem path to the package root
    /// </summary>
    public string RootPath { get; }

    /// <summary>
    /// The modules contained in this package
    /// </summary>
    public Dictionary<string, Module> Modules { get; } = new();

    public Package(string rootNamespace, string rootPath)
    {
        RootNamespace = rootNamespace;
        RootPath = rootPath;
    }
}
