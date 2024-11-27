# Import System Overview

Zen's import system is composed of packages, namespaces and modules.

Packages are a root namespace. Packages contain modules and namespaces. Namespaces also contain other sub-namespaces and modules.
Modules are the .zen files. All top-level classes and functions are exportable public symbols.

## Requirements

- Cyclical/circular dependencies. I.E, module A imports module B and module B imports module A.
- Aliasing imported symbols.
- Imported modules (.zen files) must be executed by the interpreter, but only once.
- Cache the resolutions to improve performance.

## Syntax
Import statements come in two flavours. 'From Import' and simple 'Import'.

From import is in the form: `from [path] import [symbol]`.

Regular import is in the form: `import [path]`.

Aliases can be set, for example `import Package/MyModule/MyFunc as MyCustomFunc`.

## What we have so far

We currently have a set of classes that provide the foundation for an import system.

We have classes `Package`, `Namespace` and `Module`:
```csharp
class Package
{
    string Name;
    string FullPath;
    Dictionary<string, Module> Modules;
    Dictionary<string, Namespace> Namespaces;

    Package(string name, string fullPath);
    void AddNamespace(Namespace @namespace);
    void AddModule(string name, Module module);
}

class Namespace
{
    string Name; // SomeNamespace
    string FullPath; // e.g MyPackage/SomeNamespace
    Dictionary<string, Module> Modules;
    Dictionary<string, Namespace> Namespaces; 

    void AddModule(string name, Module module);
    void AddNamespace(Namespace @namespace);
}

class Module
{
    string FullPath; // eg MyPackage/SomeNamespace/MyModule
    string Name; // E.g MyModule
    Dictionary<string, Symbol> Symbols;
}
```

We also have a `Importer` class, which leverages one or more `AbstractProviders` that provide implementations for package resolution:

```csharp
class Importer
{
    List<AbstractProvider> Providers;

    Importer(AbstractProvider[] providers);
    void RegisterProvider(AbstractProvider provider);
}
```

Importer and Providers leverage the `ImportResolution` class as a wrapper for the result of a resolution:

```csharp
abstract class ImportResolution
{
    string FullPath;

    dynamic Result { get; }
    abstract dynamic GetResult();

    void AddNamespace(Namespace @namespace);
    void AddModule(Module module);
}

abstract class AbstractProvider
{
    Package? FindPackage(string name);
    Namespace? FindNamespace(string fullPath);
    Module? FindModule(string fullPath);
    Package? ResolvePackage(string name);
    ImportResolution? Resolve(string fullPath);
}

class FileSystemProvider : AbstractProvider
{
    string[] PackageDirectories;

    FileSystemProvider(string[] packageDirectories);
    Package? FindPackage(string name);
    Namespace? FindNamespace(string fullPath);
    Module? FindModule(string fullPath);
}
```

Importantly, when import resolution happens, we take care to always build the Package, Namespace and Module instances in an ordered way such that the top-level namespace is created first, then any sub-namespaces or sub-modules and so on.

We never resolve `package/namespace/module` before first resolving `package/namespace` and `package` etc.

The ImportResolution class has helper methods to add namespaces or modules to a given package or namespace.

# Enhanced Design

## Module Execution States
```csharp
enum ModuleState 
{
    NotLoaded,      // Initial state
    Loading,        // During symbol resolution phase
    Loaded,         // After symbol resolution, before execution
    Executing,      // During module execution
    Executed        // After successful execution
}

class Module 
{
    // Existing properties...
    ModuleState State { get; private set; }
    Dictionary<string, Symbol> Symbols;
    Dictionary<string, Symbol> ExportedSymbols;
    
    // Track dependencies for cycle detection
    HashSet<Module> Dependencies;
    
    // Cache for resolved imports
    Dictionary<string, ImportResolution> ImportCache;
}
```

## Two-Phase Import Processing

### Phase 1: Symbol Resolution
- Parse the module and collect all import statements
- Create symbol table entries for all top-level declarations
- Mark imported symbols as "unresolved"
- Detect and handle circular dependencies
- State transitions: NotLoaded -> Loading -> Loaded

### Phase 2: Module Execution
- Execute module code in topological order
- Initialize variables and execute functions
- Resolve previously marked symbols
- Handle circular references through indirection
- State transitions: Loaded -> Executing -> Executed

## Circular Dependency Handling

1. **Detection**:
```csharp
class ImportCycleDetector 
{
    HashSet<Module> VisitedModules;
    Stack<Module> CurrentPath;
    
    bool DetectCycle(Module module) 
    {
        if (CurrentPath.Contains(module)) return true;
        if (VisitedModules.Contains(module)) return false;
        
        CurrentPath.Push(module);
        foreach (var dep in module.Dependencies) 
        {
            if (DetectCycle(dep)) return true;
        }
        CurrentPath.Pop();
        VisitedModules.Add(module);
        return false;
    }
}
```

2. **Resolution**:
- Allow forward references through lazy symbol resolution
- Use proxy objects for circular dependencies
- Defer actual symbol resolution until execution phase

## Implementation Steps

### Phase 1: Core Infrastructure
1. [ ] Add ModuleState enum and update Module class
2. [ ] Implement ImportCycleDetector
3. [ ] Add two-phase import processing to ImportResolution
4. [ ] Update AbstractProvider to support phased loading
5. [ ] Add import caching to Module class

### Phase 2: Symbol Resolution
1. [ ] Implement symbol collection during parsing
2. [ ] Add support for forward declarations
3. [ ] Create proxy objects for circular references
4. [ ] Implement lazy symbol resolution
5. [ ] Add validation for import paths and aliases

### Phase 3: Module Execution
1. [ ] Implement topological sorting for execution order
2. [ ] Add support for deferred symbol resolution
3. [ ] Handle circular dependency initialization
4. [ ] Implement module execution state management
5. [ ] Add error handling for execution failures

### Phase 4: Optimization & Cleanup
1. [ ] Implement import caching
2. [ ] Add performance monitoring
3. [ ] Optimize symbol resolution
4. [ ] Add debug logging
5. [ ] Write comprehensive tests

## Example Usage

```zen
// moduleA.zen
from moduleB import B
export class A {
    func method() {
        return B.method()
    }
}

// moduleB.zen
from moduleA import A
export class B {
    func method() {
        return A.method()
    }
}
```

The system will:
1. Detect the circular dependency
2. Create proxy objects for A and B
3. Allow symbol resolution through indirection
4. Execute modules in the correct order
5. Resolve actual symbols during execution

This design provides a robust foundation for handling imports while addressing the key requirements of circular dependencies, aliasing, and single execution.
