# Namespace System Analysis for Zen

## Overview of Proposed System

The proposal outlines a namespace system that:
1. Uses folder structure to define namespaces
2. Treats files as modules
3. Provides flexible import syntax
4. Includes a special include() function for direct code inclusion
5. Uses package.zen for root namespace definition

## Key Features Analysis

### 1. Folder-based Namespaces
**Strengths:**
- Intuitive mapping between file system and code organization
- Familiar to developers (similar to Java/C#)
- Easy to understand scope boundaries

**Potential Issues:**
- Need to handle case sensitivity in paths across different OS
- Need to handle special characters in folder names
- Need to validate namespace names against language keywords

### 2. Module System
**Strengths:**
- Automatic namespace creation for multi-symbol modules
- Direct imports for single-symbol modules
- Clear separation between modules and namespaces

**Potential Issues:**
- Need to track symbol counts during compilation
- Need to handle circular dependencies
- Need to handle module initialization order
- Need to consider module caching strategy

### 3. Import System
**Strengths:**
- Flexible import syntax
- Support for aliasing
- Selective symbol imports
- Automatic scoping based on module content

**Potential Issues:**
- Ambiguity when same symbol name exists in multiple imported modules
- Need to handle relative vs absolute imports
- Need to handle import resolution order
- Performance implications of scanning for single vs multi-symbol modules

### 4. Include Function
**Strengths:**
- Supports direct code inclusion
- Handles structured data formats
- Allows class method splitting

**Potential Issues:**
- Security implications of executing included code
- Need to handle recursive includes
- Need to handle file encoding issues
- Scope pollution with direct includes
- Need to handle circular includes

## Implementation Considerations

### 1. Package Resolution
```csharp
public class Package
{
    public string RootNamespace { get; }
    public string RootPath { get; }
    public Dictionary<string, Module> Modules { get; }
}

public class Module
{
    public string Name { get; }
    public string Path { get; }
    public List<Symbol> Symbols { get; }
    public bool IsSingleSymbol => Symbols.Count == 1;
}
```

### 2. Import Resolution
Need to implement:
- Path resolution (relative/absolute)
- Symbol table management
- Scope management
- Caching strategy

### 3. Symbol Management
```csharp
public class Symbol
{
    public string Name { get; }
    public SymbolType Type { get; }
    public Module DefiningModule { get; }
    public bool IsPublic { get; }
}

public class SymbolTable
{
    private Dictionary<string, Dictionary<string, Symbol>> _namespaceSymbols;
    private Dictionary<string, Symbol> _importedSymbols;
    
    public void AddSymbol(string ns, Symbol symbol) { }
    public void ImportSymbol(string name, Symbol symbol) { }
    public Symbol? ResolveSymbol(string name) { }
}
```

### 4. File System Interaction
Need to implement:
- File system abstraction for testing
- Path normalization
- File watching for development
- Module caching

## Potential Enhancements

2. **Export Control**
```zen
// Explicit exports
func publicFunction() {}
private func internalFunction() {}
```

## Implementation Phases

### Phase 1: Core Infrastructure
- [ ] Package resolution system
- [ ] Basic module loading
- [ ] Simple import syntax
- [ ] Symbol table management

### Phase 2: Advanced Features
- [ ] Include function
- [ ] Multi-symbol module handling
- [ ] Import aliasing
- [ ] Selective imports

### Phase 3: Optimizations
- [ ] Module caching
- [ ] Lazy loading
- [ ] Symbol resolution optimization
- [ ] Import resolution caching

### Phase 4: Developer Experience
- [ ] Better error messages
- [ ] Import suggestions
- [ ] Circular dependency detection
- [ ] Unused import detection

## Open Questions

1. **Module Resolution**
- How should version conflicts be handled?
- Should there be support for module versioning?
- How to handle optional dependencies?

2. **Scoping**
- How should symbol visibility be controlled?
- Should there be package-private scope?
- How to handle name collisions?

3. **Performance**
- How to optimize module loading?
- When should modules be initialized?
- How to handle large dependency trees?

4. **Security**
- How to secure the include() function?
- Should there be a way to restrict file system access?
- How to handle untrusted packages?

## Next Steps

1. Implement basic package resolution:
   - Parse package.zen
   - Build module graph
   - Resolve dependencies

2. Add import syntax to parser:
   - Add import statement
   - Add from-import statement
   - Add import aliasing

3. Implement symbol resolution:
   - Build symbol table
   - Resolve imports
   - Handle scoping

4. Add include() function:
   - Implement file loading
   - Add JSON support
   - Handle scope management
