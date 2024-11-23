using System.Reflection;
using System.Text;
using Zen.Common;

namespace Zen.Execution.Import.Providers;

/// <summary>
/// Provides modules from embedded resources in the assembly.
/// Used primarily for the standard library.
/// </summary>
public class EmbeddedResourceModuleProvider : IModuleProvider
{
    private readonly Assembly _assembly;
    private readonly string _resourcePrefix;
    private readonly Dictionary<string, string> _resourceCache = new();
    private readonly Dictionary<string, string> _moduleToResourceMap = new();

    public int Priority => 100; // Highest priority for standard library

    public EmbeddedResourceModuleProvider(Assembly assembly, string resourcePrefix)
    {
        _assembly = assembly;
        _resourcePrefix = resourcePrefix;
        CacheResources();
    }

    public void DebugInfo(StringBuilder sb)
    {
        sb.Append($"Resource prefix: {_resourcePrefix}");
        sb.Append("Found resources:");
        foreach (var resource in _resourceCache.Keys)
        {
            sb.Append($"  {resource}");
        }
        sb.Append("Module mappings:");
        foreach (var (module, resource) in _moduleToResourceMap)
        {
            sb.Append($"  {module} -> {resource}");
        }
    }

    public bool CanProvide(string modulePath)
    {
        var canProvide = _moduleToResourceMap.ContainsKey(modulePath);
        Console.WriteLine($"CanProvide({modulePath}) = {canProvide}");
        return canProvide;
    }

    public ISourceCode GetModuleSource(string modulePath)
    {
        if (!_moduleToResourceMap.TryGetValue(modulePath, out var resourcePath))
        {
            throw new RuntimeError($"Module not found in embedded resources: {modulePath}");
        }

        if (!_resourceCache.TryGetValue(resourcePath, out var content))
        {
            throw new RuntimeError($"Module not found in embedded resources: {modulePath}");
        }

        // Return the content as a FileSourceCode even though it's an embedded resource
        // Might make a VirtualSourceCode since it's not a real file - however, FileSourceCode has the
        // content so it's not a big deal.
        return new FileSourceCode(modulePath, content);
    }

    public IEnumerable<string> ListModules(string directoryPath)
    {
        return _moduleToResourceMap.Keys
            .Where(k => k.StartsWith(directoryPath))
            .Select(k => k.Substring(directoryPath.Length).TrimStart('/'))
            .Where(k => !string.IsNullOrEmpty(k));
    }

    private void CacheResources()
    {
        var resources = _assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(_resourcePrefix));

        foreach (var resource in resources)
        {
            using var stream = _assembly.GetManifestResourceStream(resource);
            if (stream == null) continue;

            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            _resourceCache[resource] = content;

            // Map the resource path to a module path
            // e.g. "Zen.Execution.Builtins.System.Exception.zen" -> "System/Exception"
            var modulePath = resource.Substring(_resourcePrefix.Length + 1) // +1 for the dot
                .Replace(".zen", "")  // Remove .zen extension
                .Replace(".", "/");   // Convert dots to slashes
            _moduleToResourceMap[modulePath] = resource;
        }
    }
}
