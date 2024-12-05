using System.Reflection;
using Zen.Common;

namespace Zen.Execution.Import.Providing;

/// <summary>
/// Provides access to built-in packages embedded as resources in the assembly
/// </summary>
public class BuiltinProvider : AbstractProvider
{
    private readonly Assembly _assembly;
    private readonly string _basePath;
    private Dictionary<string, Package> _packages = [];

    public BuiltinProvider()
    {
        _assembly = Assembly.GetExecutingAssembly();
        _basePath = "Zen.Execution.Builtins.Packages";
        
        // Initialize packages by scanning embedded resources
        InitializePackages();
    }

    private void InitializePackages()
    {
        var resources = _assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(_basePath))
            .ToList();

        Logger.Instance.Debug($"Found {resources.Count} builtin resources");
        
        foreach (var resource in resources)
        {
            // Convert resource name to package path
            // e.g. Zen.Execution.Builtins.Packages.System.Exception.zen -> System/Exception
            var relativePath = resource.Substring(_basePath.Length + 1) // +1 for the dot
                .Replace(".zen", "")
                .Replace(".", "/");

            // First segment is package name
            var segments = relativePath.Split('/');
            var packageName = segments[0];

            // Create or get package
            if (!_packages.TryGetValue(packageName, out var package))
            {
                package = new Package(packageName, packageName);
                _packages[packageName] = package;
            }

            // If this is a module (ends in .zen), create it
            if (resource.EndsWith(".zen"))
            {
                using var stream = _assembly.GetManifestResourceStream(resource);
                if (stream == null) continue;

                using var reader = new StreamReader(stream);
                var content = reader.ReadToEnd();
                
                var modulePath = string.Join("/", segments);
                var module = new Module(modulePath, new InlineSourceCode(content));
                
                // Add module to its parent namespace or package
                if (segments.Length > 2)
                {
                    // Module is in a namespace
                    var namespacePath = string.Join("/", segments.Take(segments.Length - 1));
                    var ns = FindOrCreateNamespace(package, namespacePath);
                    ns.AddModule(segments.Last(), module);
                }
                else
                {
                    // Module is directly in package
                    package.AddModule(segments.Last(), module);
                }
            }
        }
    }

    private Namespace FindOrCreateNamespace(Package package, string fullPath)
    {
        var segments = fullPath.Split('/');
        IHasNamespaces current = package;
        Namespace? currentNs = null;

        // Skip the package name
        for (int i = 1; i < segments.Length; i++)
        {
            var segment = segments[i];
            var ns = current.Namespaces.GetValueOrDefault(segment);
            
            if (ns == null)
            {
                var nsPath = string.Join("/", segments.Take(i + 1));
                ns = new Namespace(nsPath);
                current.AddNamespace(ns);
            }

            current = ns;
            currentNs = ns;
        }

        return currentNs!;
    }

    protected override Package? FindPackage(string name)
    {
        return _packages.GetValueOrDefault(name);
    }

    protected override Namespace? FindNamespace(string fullPath)
    {
        var segments = fullPath.Split('/');
        var packageName = segments[0];

        if (!_packages.TryGetValue(packageName, out var package))
        {
            return null;
        }

        // If just the package name, return null (not a namespace)
        if (segments.Length == 1) return null;

        // Navigate through namespaces
        IHasNamespaces current = package;
        for (int i = 1; i < segments.Length; i++)
        {
            var ns = current.Namespaces.GetValueOrDefault(segments[i]);
            if (ns == null) return null;
            current = ns;
        }

        return current as Namespace;
    }

    protected override Module? FindModule(string fullPath)
    {
        var segments = fullPath.Split('/');
        var packageName = segments[0];

        if (!_packages.TryGetValue(packageName, out var package))
        {
            return null;
        }

        // Navigate through namespaces to find the module
        IHasModules current = package;
        for (int i = 1; i < segments.Length - 1; i++)
        {
            var ns = (current as IHasNamespaces)?.Namespaces.GetValueOrDefault(segments[i]);
            if (ns == null) return null;
            current = ns;
        }

        return current.Modules.GetValueOrDefault(segments.Last());
    }
}
