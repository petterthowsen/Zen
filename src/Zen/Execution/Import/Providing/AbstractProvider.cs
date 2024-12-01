namespace Zen.Execution.Import.Providing;

public abstract class AbstractProvider
{

    private Dictionary<string, Package> _packages = [];

    private Dictionary<string, ImportResolution> _cache = [];

    private ImportResolution Cache(string fullPath, ImportResolution resolution)
    {
        _cache[fullPath] = resolution;
        return resolution;
    }

    private ImportResolution Cache(string fullPath, Package package)
    {
        ImportResolution resolution = new PackageResolution(fullPath, package);
        _cache[fullPath] = resolution;
        return resolution;
    }
    
    private NamespaceResolution Cache(string fullPath, Namespace ns)
    {
        NamespaceResolution resolution = new NamespaceResolution(fullPath, ns);
        _cache[fullPath] = resolution;
        return resolution;
    }

    private ModuleResolution Cache(string fullPath, Module module)
    {
        ModuleResolution resolution = new ModuleResolution(fullPath, module);
        _cache[fullPath] = resolution;
        return resolution;
    }

    private bool IsCached(string fullPath) => 
        _cache.ContainsKey(fullPath);
    
    private ImportResolution GetCached(string fullPath) => _cache[fullPath];

    protected abstract Package? FindPackage(string name);

    protected abstract Namespace? FindNamespace(string fullPath);

    protected abstract Module? FindModule(string fullPath);

    public Package? ResolvePackage(string name)
    {
        if (_packages.TryGetValue(name, out var package))
            return package;
        
        package = FindPackage(name);
        if (package == null)
            return null;
        
        _packages[name] = package;
        _cache[name] = new PackageResolution(name, package);

        return package;
    }

    public ImportResolution? Resolve(string fullPath)
    {
        if ( IsCached(fullPath)) return _cache[fullPath];

        List<string> segments = fullPath.Split('/').ToList();
        
        string packageName = segments[0];
        Package? package = ResolvePackage(packageName);
        if (package == null) return null;

        // package only?
        if (segments.Count == 1)
            return Cache(fullPath, package);

        ImportResolution current = new PackageResolution(fullPath, package);
        string currentFullPath = package.FullPath;

        for (int i = 1; i < segments.Count; i++)
        {
            bool isLast = i == segments.Count - 1;

            string segment = segments[i];
            currentFullPath += "/" + segment;

            // cached?
            if (IsCached(currentFullPath))
            {
                ImportResolution resolution = _cache[currentFullPath];
                current = resolution;
            }else {
                if (isLast)
                {
                    // if this is the last segment, we'll prioritize modules before namespaces.
                    Module? module = FindModule(currentFullPath);

                    if (module != null) {
                        current.AddModule(module);
                        current = new ModuleResolution(currentFullPath, module);
                    }else {
                        // try namespace
                        Namespace? ns = FindNamespace(currentFullPath);
                        
                        if (ns == null) return null;

                        current.AddNamespace(ns);
                        current = new NamespaceResolution(currentFullPath, ns);
                    }

                    // resolve to a symbol?
                    // perhaps this is the wrong place to but this logic...
                    //Module module = current.AsModule().Module;
                    // we can't resolve symbols here since the module's AST has not yet been parsed
                }
                else
                {
                    // namespace or module
                    // try namespace
                    Namespace? ns = FindNamespace(currentFullPath);
                    
                    if (ns != null) {
                        current.AddNamespace(ns);
                        current = new NamespaceResolution(currentFullPath, ns);
                    }else {
                        Module? module = FindModule(currentFullPath);
                        if (module != null) {
                            current.AddModule(module);
                            current = new ModuleResolution(currentFullPath, module);
                        }else {
                            return null;
                        }
                    }
                }

                // cache it
                Cache(currentFullPath, current);
            }
        }

        return current;
    }

}