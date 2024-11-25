namespace Zen.Execution.Import;

/// <summary>
/// Represents what an import path resolved to
/// </summary>
public abstract class ImportResolution 
{
    public string FullPath { get; }
    
    protected ImportResolution(string fullPath)
    {
        FullPath = fullPath;
    }

    public dynamic Result => GetResult();

    public abstract dynamic GetResult();

    public void AddNamespace(Namespace @namespace) {
        if (Result is not IHasNamespaces hasNamespaces) {
            throw new InvalidOperationException("Result is not a IHasNamespaces");
        }

        hasNamespaces.AddNamespace(@namespace);
    }

    public void AddModule(Module module) {
        if (Result is not IHasModules hasModules) {
            throw new InvalidOperationException("Result is not a IHasModules");
        }

        hasModules.AddModule(module.Name, module);
    }
}

public class PackageResolution : ImportResolution 
{
    public Package Package { get; }
    
    public override dynamic GetResult() => Package;

    public PackageResolution(string fullPath, Package package) : base(fullPath)
    {
        Package = package;
    }
}

public class NamespaceResolution : ImportResolution 
{
    public string SubPath { get; }
    public Namespace Namespace { get; }
    
    public override dynamic GetResult() => Namespace;

    public NamespaceResolution(string fullPath, Namespace ns) : base(fullPath)
    {
        Namespace = ns;
        string[] segments = fullPath.Split("/").Skip(1).ToArray();
        SubPath = string.Join("/", segments);
    }
}
public class ModuleResolution : ImportResolution 
{
    public string SubPath { get; }
    public Module Module { get; }
    
    public override dynamic GetResult() => Module;

    public ModuleResolution(string fullPath, Module module) : base(fullPath)
    {
        Module = module;
        string[] segments = fullPath.Split("/").Skip(1).ToArray();
        SubPath = string.Join("/", segments);
    }
}



// Extension methods for the ImportResolution class to make it easier to work with
public static class ImportResolutionExtensions
{
    public static bool IsPackage(this ImportResolution resolution) => resolution is PackageResolution;
    public static bool IsNamespace(this ImportResolution resolution) => resolution is NamespaceResolution;
    public static bool IsModule(this ImportResolution resolution) => resolution is ModuleResolution;

    public static PackageResolution AsPackage(this ImportResolution resolution) => 
        resolution as PackageResolution ?? throw new InvalidOperationException("Resolution is not a package");
    
    public static NamespaceResolution AsNamespace(this ImportResolution resolution) => 
        resolution as NamespaceResolution ?? throw new InvalidOperationException("Resolution is not a namespace");
    
    public static ModuleResolution AsModule(this ImportResolution resolution) => 
        resolution as ModuleResolution ?? throw new InvalidOperationException("Resolution is not a module");
}