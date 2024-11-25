# Module System Design for Zen Language

## Core Abstractions

### Symbol
Represents an exportable element from a module (function, class, etc.).
```csharp
public class Symbol 
{
    public string Name;
    public SymbolKind Kind;  // Function, Class, etc.
    public Module DefiningModule;
    public ZenValue Value;  // The actual value/implementation
}
```

### Module
Represents a single .zen file and its exported symbols.
```csharp
public class Module 
{
    public string Name;
    public string FilePath;
    public Namespace ParentNamespace;
    private Dictionary<string, Symbol> ExportedSymbols;
    public bool IsInitialized { get; set; }  // For cycle detection
    
    // Lazily executes the module if needed and returns its exports
    public IReadOnlyDictionary<string, Symbol> GetExports() 
}
```

### Namespace
Represents a directory containing modules and/or other namespaces.
```csharp
public class Namespace 
{
    public string Name;
    public string Path;
    public Namespace? Parent;
    public readonly Dictionary<string, Module> Modules;
    private Dictionary<string, Namespace> Namespaces;
    
    // Resolves a symbol path within this namespace
    public Symbol ResolveSymbol(string[] path)
}
```

### Package
Top-level container representing a root directory with a package.zen file.
```csharp
public class Package : Namespace
{
    // Resolves a fully qualified symbol path
    public Symbol ResolveSymbol(string[] path)
    public Module ResolveModule(string module)
}
```

### Package Provider Interface
```csharp
public interface IPackageProvider 
{
    // Returns null if package not found
    Package GetPackage(string name);
    bool HasPackage(string name);
}
```

## Package Providers

### BuiltinPackageProvider
- Loads packages from embedded resources in execution/builtins
- Caches loaded packages
- Highest resolution priority

### SystemPackageProvider  
- Searches $ZEN_HOME/packages
- Reads package.zen files
- Caches loaded packages
- Second priority

### MainScriptPackageProvider
- Creates implicit package from main script directory
- Loads package.zen if present, otherwise uses default name
- Lowest priority

## Package Resolution & Import Process

### Import Statement Handler
```csharp
public class ImportHandler 
{
    private List<IPackageProvider> PackageProviders;
    private Dictionary<string, Module> LoadedModules;
    
    // Handles various import statement forms
    public void HandleImport(ImportStatement stmt, Environment targetEnv)
    
    // Resolves symbols and adds to target environment
    private void ImportSymbols(string[] path, Environment targetEnv)
}
```

### Symbol Resolution Process
1. Parse import path to identify:
   - Package name
   - Namespace path
   - Module name (if specified)
   - Symbol name(s) (if specified)
   
2. Find package by trying providers in priority order

3. Resolve symbol through Package → Namespace → Module hierarchy

4. For module imports:
   - Check if already loaded (to handle cycles)
   - If not, initialize module in new environment
   - Cache module's exports
   - Import requested symbols to target environment

## Cyclic Import Handling

To handle cyclic imports:
1. Track module initialization state
2. Allow access to already defined symbols in partially initialized modules
3. Detect cycles during module loading
4. Throw error only on actual circular dependencies (not just cyclic imports)

## Usage Examples

```csharp
// Example import statements and how they're handled:

// import math
ImportHandler.HandleImport("math", targetEnv)
// Loads all symbols from math package root

// from math/geometry import Circle
ImportHandler.HandleImport("math/geometry/Circle", targetEnv)
// Loads specific symbol from module

// import utils/strings
ImportHandler.HandleImport("utils/strings", targetEnv)
// Loads all symbols from strings module
```

## Extension Points

The design allows for future extensions:
1. Additional symbol types (variables, enums, etc.)
2. New package providers (e.g., remote packages)
3. Symbol visibility controls (public/private exports)
4. Package versioning and dependencies
5. Import aliases
6. Runtime symbol reflection/introspection
