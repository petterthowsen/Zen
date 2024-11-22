# Runtime System Design for Zen

## Overview

Instead of having the Interpreter manage everything, we can create a Runtime class that orchestrates all the components of Zen. This would provide better separation of concerns and make the system more modular.

## Proposed Structure

```csharp
public class Runtime
{
    private readonly Lexer _lexer;
    private readonly Parser _parser;
    private readonly Resolver _resolver;
    private readonly Interpreter _interpreter;
    private readonly Importer _importer;
    private readonly EventLoop _eventLoop;

    public Runtime()
    {
        _importer = new Importer();
        _eventLoop = new EventLoop();
        _interpreter = new Interpreter(_importer, _eventLoop);
        _lexer = new Lexer();
        _parser = new Parser();
        _resolver = new Resolver(_importer);
    }

    public void Execute(string source, string? sourcePath = null)
    {
        var tokens = _lexer.Tokenize(source);
        var ast = _parser.Parse(tokens);
        _resolver.Resolve(ast);
        _interpreter.Interpret(ast);
    }

    public void LoadPackage(string path)
    {
        _importer.LoadPackage(path);
    }

    public void Shutdown()
    {
        _eventLoop.Stop();
    }
}
```

## Importer Design

```csharp
public class Importer
{
    private readonly Dictionary<string, Package> _packages = new();
    private readonly Dictionary<string, Module> _modules = new();
    private readonly Dictionary<string, Symbol> _symbols = new();

    public void LoadPackage(string path)
    {
        var package = LoadPackageDefinition(path);
        ScanModules(package);
        _packages[package.RootNamespace] = package;
    }

    public Symbol? ResolveSymbol(string name, string currentNamespace)
    {
        // First check local namespace
        if (_symbols.TryGetValue($"{currentNamespace}.{name}", out var symbol))
            return symbol;

        // Then check global imports
        return _symbols.GetValueOrDefault(name);
    }

    public void Import(string modulePath, string? alias = null)
    {
        var module = LoadModule(modulePath);
        if (module.IsSingleSymbol)
        {
            // Import single symbol directly
            var symbol = module.Symbols[0];
            _symbols[alias ?? symbol.Name] = symbol;
        }
        else
        {
            // Import all symbols under namespace/alias
            var ns = alias ?? module.Namespace;
            foreach (var symbol in module.Symbols)
            {
                _symbols[$"{ns}.{symbol.Name}"] = symbol;
            }
        }
    }

    private Package LoadPackageDefinition(string path)
    {
        // Load and parse package.zen
        // Create Package instance
        // Return package info
    }

    private void ScanModules(Package package)
    {
        // Scan directory for .zen files
        // Parse each file to count symbols
        // Create Module instances
        // Track dependencies
    }

    private Module LoadModule(string path)
    {
        // Load and parse module file
        // Create Module instance with symbols
        // Return module info
    }
}
```

## Benefits of This Design

1. **Clear Separation of Concerns**
   - Runtime: High-level orchestration
   - Importer: Package and module management
   - Interpreter: Code execution
   - EventLoop: Async operation management

2. **Better Module Management**
   - Centralized symbol resolution
   - Clear package boundaries
   - Proper namespace isolation

3. **Simplified Testing**
   - Each component can be tested in isolation
   - Easy to mock dependencies
   - Clear component boundaries

4. **Improved Error Handling**
   - Each layer can handle its specific errors
   - Clear error propagation path
   - Better error context

## Usage Example

```csharp
// Create runtime
var runtime = new Runtime();

// Load a package
runtime.LoadPackage("/path/to/mypackage");

// Execute some code
runtime.Execute(@"
    import MyPackage/Utils
    Utils.doSomething()
");

// Clean shutdown
runtime.Shutdown();
```

## Implementation Considerations

1. **Symbol Resolution Order**
   - Local scope
   - Current namespace
   - Imported symbols
   - Global scope

2. **Module Loading**
   - Lazy loading of modules
   - Dependency tracking
   - Circular dependency handling

3. **Error Handling**
   - Package loading errors
   - Import resolution errors
   - Symbol resolution errors
   - Runtime errors

4. **Performance**
   - Module caching
   - Symbol table optimization
   - Lazy module initialization

## Next Steps

1. Create the basic Runtime class
2. Implement the Importer
3. Modify Interpreter to use Importer
4. Add package loading support
5. Implement import statement handling
6. Add tests for the new components

This design provides a clean separation of concerns while maintaining flexibility for future enhancements. The Runtime class acts as the main entry point, while the Importer handles all module and symbol management, leaving the Interpreter to focus solely on code execution.
