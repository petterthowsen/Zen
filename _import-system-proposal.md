# Import System Redesign Proposal

## Current Issues

1. The import system is tightly coupled to the filesystem
2. No support for embedded resources (standard library)
3. No configurable search paths ($ZEN_HOME)
4. Module imports/aliases are stored globally
5. No abstraction for different module sources

## Proposed Solution

### 1. Module Provider Interface

```csharp
public interface IModuleProvider 
{
    // Check if this provider can handle the given module path
    bool CanProvide(string modulePath);
    
    // Get source code for a module
    ISourceCode GetModuleSource(string modulePath);
    
    // List available modules (for directory imports)
    IEnumerable<string> ListModules(string directoryPath);
    
    // Get provider-specific metadata
    ModuleMetadata GetMetadata(string modulePath);
}
```

### 2. Concrete Providers

#### FileSystemModuleProvider
- Handles modules from the local filesystem
- Root is the directory of the main script
- Implements current filesystem loading logic

```csharp
public class FileSystemModuleProvider : IModuleProvider 
{
    private readonly string _rootPath;
    
    public FileSystemModuleProvider(string rootPath) 
    {
        _rootPath = rootPath;
    }
    
    public bool CanProvide(string modulePath) 
    {
        var fullPath = Path.Combine(_rootPath, modulePath + ".zen");
        return File.Exists(fullPath);
    }
    
    // ... other interface implementations
}
```

#### EmbeddedResourceModuleProvider
- Handles standard library modules embedded in the assembly
- Uses assembly resources for module source code

```csharp
public class EmbeddedResourceModuleProvider : IModuleProvider 
{
    private readonly Assembly _assembly;
    private readonly string _resourcePrefix;
    
    public EmbeddedResourceModuleProvider(Assembly assembly, string resourcePrefix) 
    {
        _assembly = assembly;
        _resourcePrefix = resourcePrefix;
    }
    
    public bool CanProvide(string modulePath) 
    {
        var resourcePath = $"{_resourcePrefix}.{modulePath.Replace('/', '.')}.zen";
        return _assembly.GetManifestResourceStream(resourcePath) != null;
    }
    
    // ... other interface implementations
}
```

#### SystemModuleProvider
- Handles modules from $ZEN_HOME
- Supports multiple package versions
- Implements package search logic

```csharp
public class SystemModuleProvider : IModuleProvider 
{
    private readonly string _zenHome;
    
    public SystemModuleProvider(string zenHome) 
    {
        _zenHome = zenHome;
    }
    
    public bool CanProvide(string modulePath) 
    {
        // Check if module exists in any package in $ZEN_HOME
        var packageName = modulePath.Split('/')[0];
        return Directory.Exists(Path.Combine(_zenHome, packageName));
    }
    
    // ... other interface implementations
}
```

### 3. Module Class Enhancement

```csharp
public class Module 
{
    // Existing properties...
    
    // Module-specific imports and aliases
    private readonly Dictionary<string, Symbol> _imports = new();
    
    public void AddImport(string name, Symbol symbol, string? alias = null) 
    {
        _imports[alias ?? name] = symbol;
    }
    
    public Symbol? ResolveImport(string name) 
    {
        return _imports.GetValueOrDefault(name);
    }
}
```

### 4. Module Resolver

```csharp
public class ModuleResolver 
{
    private readonly List<IModuleProvider> _providers = new();
    private readonly Dictionary<string, Module> _moduleCache = new();
    
    public ModuleResolver(IEnumerable<IModuleProvider> providers) 
    {
        _providers.AddRange(providers);
    }
    
    public Module ResolveModule(string modulePath) 
    {
        // Check cache first
        if (_moduleCache.TryGetValue(modulePath, out var cachedModule))
            return cachedModule;
            
        // Find provider that can handle this module
        var provider = _providers.FirstOrDefault(p => p.CanProvide(modulePath))
            ?? throw new RuntimeError($"No provider found for module: {modulePath}");
            
        // Load module
        var source = provider.GetModuleSource(modulePath);
        var metadata = provider.GetMetadata(modulePath);
        
        // Create and cache module
        var module = CreateModule(source, metadata);
        _moduleCache[modulePath] = module;
        
        return module;
    }
}
```

### 5. Updated Importer

```csharp
public class Importer 
{
    private readonly ModuleResolver _resolver;
    
    public Importer(Parser parser, Lexer lexer, Interpreter interpreter) 
    {
        // Initialize providers
        var providers = new List<IModuleProvider> 
        {
            new EmbeddedResourceModuleProvider(typeof(Importer).Assembly, "Zen.StdLib"),
            new SystemModuleProvider(Environment.GetEnvironmentVariable("ZEN_HOME") ?? ""),
            new FileSystemModuleProvider(Directory.GetCurrentDirectory())
        };
        
        _resolver = new ModuleResolver(providers);
    }
    
    public void Import(string modulePath, string? alias = null) 
    {
        var module = _resolver.ResolveModule(modulePath);
        
        // Module execution remains the same
        if (!_executedModules.Contains(module.Path)) 
        {
            ExecuteModule(module);
        }
        
        // But imports are now stored per-module
        if (module.IsSingleSymbol) 
        {
            var symbol = module.Symbols[0];
            _currentModule.AddImport(symbol.Name, symbol, alias);
        } 
        else 
        {
            foreach (var symbol in module.Symbols) 
            {
                _currentModule.AddImport($"{alias ?? module.Namespace}.{symbol.Name}", symbol);
            }
        }
    }
}
```

## Benefits

1. **Modularity**: Each provider handles its own module source type
2. **Extensibility**: Easy to add new module sources
3. **Isolation**: Module-specific imports prevent naming conflicts
4. **Flexibility**: Configurable search paths and priorities
5. **Standard Library**: First-class support for embedded modules

## Implementation Steps

1. Create the IModuleProvider interface
2. Implement FileSystemModuleProvider (refactor existing code)
3. Add module-specific imports to Module class
4. Create ModuleResolver
5. Implement EmbeddedResourceModuleProvider
6. Implement SystemModuleProvider
7. Update Importer to use new system
8. Add tests for new functionality

## Migration Strategy

1. Keep existing filesystem loading as default
2. Add provider system in parallel
3. Gradually move functionality to providers
4. Update tests to cover new cases
5. Document new import system
