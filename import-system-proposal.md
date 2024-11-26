# Import System Overview

Zen's import system is composed of Packages, namespaces and modules.

Packages are a root namespace. Packages contain modules and namespaces. Namespaces also contain other sub-namespaces and modules.
Modules are the .zen files. All top-level classes and functions are exportable public symbols.

## Requirements

- Ccyclical/circular dependencies. I.E, module A imports module B and module B imports module A.
- Aliasing imported symbols.
- Imported modules (.zen files) must be executed by the interpreter, but only once.
- Cache the resolutions to improve performance.

## Syntax
Import statements come in two flavours. 'From Import' and simple 'Import'.

From import is in the form: `from [path] import [symbol]`.

Regular import is in the form: `import [path]`.

Aliases can be set, for example `import Package/MyModule/MyFunc as MyCustomFunc`.


## Current Partial implementation

We currently have a set of classes that provide the foundation for an import system.

We have classes for Packages, Namespaces and Modules, as well as a ImportResolution class and a AbstractProvider class.

Package, Namespace and Module classes are self-explanatory.

The ImportResolution wraps the result of a Resolution attempt.

Importantly, when import resolution happens, we take care to always build the Package, Namespace and Module instances in an ordered way such that the top-level namespace is created first, then any sub-namespaces or sub-modules and so on.

We never resolve `package/namespace/module` before first resolving `package/namespace` and `package` etc.

The ImportResolution class has helper methods to add namespaces or modules to a given package or namespace.

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
```

And a namespace class:

```csharp
class Namespace
{
    string Name; // SomeNamespace
    string FullPath; // e.g MyPackage/SomeNamespace
    Dictionary<string, Module> Modules;
    Dictionary<string, Namespace> Namespaces; 

    void AddModule(string name, Module module);
    void AddNamespace(Namespace @namespace);
}
```

```csharp
class Module
{
    string FullPath;
    string Name;
    Dictionary<string, Symbol> Symbols;
}
```

```csharp

class Importer
{
    List<AbstractProvider> Providers;

    Importer(AbstractProvider[] providers);
    void RegisterProvider(AbstractProvider provider);
    ...
}
```

```csharp
abstract class AbstractProvider
{
    Package? FindPackage(string name);
    Namespace? FindNamespace(string fullPath);
    Module? FindModule(string fullPath);
    Package? ResolvePackage(string name);
    ImportResolution? Resolve(string fullPath);
}

```csharp
class FileSystemProvider : AbstractProvider
{
    string[] PackageDirectories;

    FileSystemProvider(string[] packageDirectories);
    Package? FindPackage(string name);
    Namespace? FindNamespace(string fullPath);
    Module? FindModule(string fullPath);
}
```