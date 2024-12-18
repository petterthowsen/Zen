using Zen.Exection.Import;

namespace Zen.Execution.Import;

public class Package : IHasNamespaces, IHasModules
{
    public string Name { get; }
    public string FullPath { get; }

    public Dictionary<string, Module> Modules { get; } = [];
    public Dictionary<string, Namespace> Namespaces { get; } = [];

    public Package(string name, string fullPath)
    {
        Name = name;
        FullPath = fullPath;
    }

    public void AddNamespace(Namespace @namespace)
    {
        if (Namespaces.ContainsKey(@namespace.Name)) return;
        Namespaces[@namespace.Name] = @namespace;
    }

    public void AddModule(string name, Module module)
    {
        if (Modules.ContainsKey(name)) return;
        Modules[name] = module;
    }
}