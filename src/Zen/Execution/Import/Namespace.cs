

namespace Zen.Execution.Import;

/// <summary>
/// Represents a namespace in the import system.
/// </summary>
public class Namespace : IHasNamespaces, IHasModules
{
    public string Name;
    public string FullPath;

    public Dictionary<string, Module> Modules { get; } = [];
    public Dictionary<string, Namespace> Namespaces { get; } = [];

    public Namespace(string fullPath)
    {
        FullPath = fullPath;
        Name = FullPath.Split('/').Last();
    }

    public void AddModule(string name, Module module)
    {
        Modules[name] = module;
    }

    public void AddNamespace(Namespace @namespace)
    {
        Namespaces[@namespace.Name] = @namespace;
    }
}