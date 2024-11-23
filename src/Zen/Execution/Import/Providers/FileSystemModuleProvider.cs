using Zen.Common;

namespace Zen.Execution.Import.Providers;

/// <summary>
/// Provides modules from the filesystem.
/// Handles multiple search paths including $ZEN_HOME and current working directory.
/// </summary>
public class FileSystemModuleProvider : IModuleProvider
{
    private readonly Dictionary<string, string> _packageRoots = new();
    private readonly List<string> _searchPaths = new();

    public int Priority => 1; // Lowest priority, try standard library first

    public FileSystemModuleProvider()
    {
        // Initialize with $ZEN_HOME if set
        var zenHome = System.Environment.GetEnvironmentVariable("ZEN_HOME");
        if (!string.IsNullOrEmpty(zenHome))
        {
            RegisterSearchPath(zenHome);
        }
    }

    /// <summary>
    /// Registers a package namespace and its root directory.
    /// </summary>
    public void RegisterPackage(string packageNamespace, string rootPath)
    {
        if (!Directory.Exists(rootPath))
        {
            throw new RuntimeError($"Package root path does not exist: {rootPath}");
        }

        _packageRoots[packageNamespace] = Path.GetFullPath(rootPath);
    }

    /// <summary>
    /// Registers a new directory to search for modules.
    /// </summary>
    public void RegisterSearchPath(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new RuntimeError($"Search path does not exist: {path}");
        }
        
        var fullPath = Path.GetFullPath(path);
        if (!_searchPaths.Contains(fullPath))
        {
            _searchPaths.Add(fullPath);
        }
    }

    public bool CanProvide(string modulePath)
    {
        // First check if this is a package-qualified path
        var firstSlash = modulePath.IndexOf('/');
        if (firstSlash != -1)
        {
            var packageName = modulePath[..firstSlash];
            if (_packageRoots.TryGetValue(packageName, out var packageRoot))
            {
                // Strip package name and look in package root
                var relativePath = modulePath[(firstSlash + 1)..];
                var filePath = Path.Combine(packageRoot, relativePath + ".zen");
                return File.Exists(filePath);
            }
        }

        // If not found in package roots, try search paths
        foreach (var searchPath in _searchPaths)
        {
            var filePath = Path.Combine(searchPath, modulePath + ".zen");
            if (File.Exists(filePath))
                return true;
        }

        return false;
    }

    public ISourceCode GetModuleSource(string modulePath)
    {
        // First check if this is a package-qualified path
        var firstSlash = modulePath.IndexOf('/');
        if (firstSlash != -1)
        {
            var packageName = modulePath[..firstSlash];
            if (_packageRoots.TryGetValue(packageName, out var packageRoot))
            {
                // Strip package name and look in package root
                var relativePath = modulePath[(firstSlash + 1)..];
                var filePath = Path.Combine(packageRoot, relativePath + ".zen");
                if (File.Exists(filePath))
                {
                    return new FileSourceCode(filePath);
                }
            }
        }

        // If not found in package roots, try search paths
        foreach (var searchPath in _searchPaths)
        {
            var filePath = Path.Combine(searchPath, modulePath + ".zen");
            if (File.Exists(filePath))
            {
                return new FileSourceCode(filePath);
            }
        }

        throw new RuntimeError($"Module not found: {modulePath}");
    }

    public IEnumerable<string> ListModules(string directoryPath)
    {
        var modules = new HashSet<string>();

        // First check if this is a package-qualified path
        var firstSlash = directoryPath.IndexOf('/');
        if (firstSlash != -1)
        {
            var packageName = directoryPath[..firstSlash];
            if (_packageRoots.TryGetValue(packageName, out var packageRoot))
            {
                // Strip package name and look in package root
                var relativePath = directoryPath[(firstSlash + 1)..];
                var fullPath = Path.Combine(packageRoot, relativePath);
                if (Directory.Exists(fullPath))
                {
                    // List all .zen files recursively
                    var files = Directory.GetFiles(fullPath, "*.zen", SearchOption.AllDirectories)
                        .Where(f => !Path.GetFileName(f).StartsWith("_"));

                    foreach (var file in files)
                    {
                        var moduleRelativePath = Path.GetRelativePath(packageRoot, file);
                        modules.Add(Path.Combine(packageName, Path.ChangeExtension(moduleRelativePath, null)).Replace("\\", "/"));
                    }
                }
            }
        }

        // Then check search paths
        foreach (var searchPath in _searchPaths)
        {
            var fullPath = Path.Combine(searchPath, directoryPath);
            if (Directory.Exists(fullPath))
            {
                // List all .zen files recursively
                var files = Directory.GetFiles(fullPath, "*.zen", SearchOption.AllDirectories)
                    .Where(f => !Path.GetFileName(f).StartsWith("_"));

                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(searchPath, file);
                    modules.Add(Path.ChangeExtension(relativePath, null).Replace("\\", "/"));
                }
            }
        }

        return modules;
    }
}
