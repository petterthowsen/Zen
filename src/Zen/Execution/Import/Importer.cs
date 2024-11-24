using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Zen.Common;
using Zen.Execution.Import.Providers;
using Zen.Lexing;
using Zen.Parsing;
using Zen.Parsing.AST;
using Zen.Parsing.AST.Statements;

namespace Zen.Execution.Import;

/// <summary>
/// Manages package loading, module resolution, and symbol imports for Zen.
/// </summary>
public class Importer
{
    private readonly Dictionary<string, Package> _packages = new();
    private readonly HashSet<string> _executedModules = new();
    private readonly Parser _parser;
    private readonly Lexer _lexer;
    private readonly Interpreter _interpreter;
    private readonly ModuleResolver _resolver;
    private readonly FileSystemModuleProvider _fsProvider;
    private Module? _currentModule;

    public Importer(Parser parser, Lexer lexer, Interpreter interpreter)
    {
        _parser = parser;
        _lexer = lexer;
        _interpreter = interpreter;

        // Initialize providers
        _fsProvider = new FileSystemModuleProvider();
        var providers = new List<IModuleProvider>
        {
            // Standard library (embedded resources) has highest priority
            new EmbeddedResourceModuleProvider(
                typeof(Importer).Assembly,
                "Zen.Execution.Builtins"
            ),
            // Filesystem provider for packages and search paths
            _fsProvider
        };

        _resolver = new ModuleResolver(providers);
    }

    /// <summary>
    /// Registers a new module provider.
    /// </summary>
    public void RegisterProvider(IModuleProvider provider)
    {
        _resolver.RegisterProvider(provider);
    }

    /// <summary>
    /// Gets a module by its path. Throws if the module is not found.
    /// </summary>
    public Module GetModule(string modulePath)
    {
        return _resolver.ResolveModule(modulePath, _parser, _lexer, _interpreter);
    }

    /// <summary>
    /// Given a path, checks if the directory contains a 'package.zen' file.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public bool PackageFileExists(string filePath)
    {
        String fullPath = Path.GetFullPath(filePath);
        if (File.Exists(fullPath)) fullPath = Path.GetDirectoryName(fullPath);

        fullPath = Path.Combine(fullPath, "package.zen");

        return File.Exists(fullPath);
    }

    /// <summary>
    /// Loads a package from the specified directory path.
    /// The directory must contain a package.zen file.
    /// </summary>
    public Package LoadPackage(string path, bool throwIfNotFound = true)
    {
        if (Path.GetFileName(path) != "package.zen" && File.Exists(path))
        {
            path = Path.GetDirectoryName(path);
        }

        if (Path.GetFileName(path) != "package.zen")
        {
            path = Path.Combine(path, "package.zen");
        }

        if (!File.Exists(path) && throwIfNotFound)
        {
            throw new RuntimeError($"No package.zen found in {path}");
        }

        // Phase 1: Symbol Resolution
        Package package = LoadPackageDefinition(path);
        
        // Register the package with the filesystem provider
        _fsProvider.RegisterPackage(package.RootNamespace, package.RootPath);
        
        // Add to known packages
        _packages[package.RootNamespace] = package;
        
        // Scan for modules
        ScanModules(package);

        return package;
    }

    /// <summary>
    /// Resolves a symbol by name in the given namespace context.
    /// First checks the local namespace, then global imports.
    /// </summary>
    public Symbol? ResolveSymbol(string name, string currentNamespace)
    {
        // First check current module's imports
        if (_currentModule != null)
        {
            var symbol = _currentModule.ResolveImport(name);
            if (symbol != null) return symbol;
        }

        // Then check local namespace
        var modulePath = $"{currentNamespace}/{name}";
        try
        {
            var module = GetModule(modulePath);
            if (module.IsSingleSymbol)
            {
                return module.Symbols[0];
            }
        }
        catch (RuntimeError)
        {
            // Module not found, continue searching
        }

        return null;
    }

    /// <summary>
    /// Imports a module by path, optionally with an alias.
    /// For single-symbol modules, imports the symbol directly.
    /// For multi-symbol modules, imports under a namespace.
    /// </summary>
    public void Import(string modulePath, string? alias = null)
    {
        var module = GetModule(modulePath);
        
        // Phase 2: Module Execution (if not already executed)
        if (!_executedModules.Contains(module.Path))
        {
            ExecuteModule(module);
        }

        if (_currentModule == null)
        {
            throw new RuntimeError("No current module context for import");
        }

        if (module.IsSingleSymbol)
        {
            // Import single symbol directly
            var symbol = module.Symbols[0];
            _currentModule.AddImport(alias ?? symbol.Name, symbol);
        }
        else
        {
            // Import all symbols under namespace/alias
            var ns = alias ?? module.Namespace;
            foreach (var symbol in module.Symbols)
            {
                _currentModule.AddImport($"{ns}/{symbol.Name}", symbol);
            }
        }
    }

    /// <summary>
    /// Imports specific symbols from a module.
    /// </summary>
    public void ImportSymbols(string modulePath, IEnumerable<string> symbolNames)
    {
        if (_currentModule == null)
        {
            throw new RuntimeError("No current module context for import");
        }

        foreach (var name in symbolNames)
        {
            // First try to load the symbol as a direct module
            var symbolPath = $"{modulePath}/{name}";
            try
            {
                var symbolModule = GetModule(symbolPath);
                
                // Execute the symbol module if needed
                if (!_executedModules.Contains(symbolModule.Path))
                {
                    ExecuteModule(symbolModule);
                }

                if (symbolModule.IsSingleSymbol)
                {
                    _currentModule.AddImport(name, symbolModule.Symbols[0]);
                    continue;
                }
            }
            catch (RuntimeError)
            {
                // If not found as a direct module, try to find the symbol in the module itself
                try
                {
                    var module = GetModule(modulePath);
                    
                    // Execute the module if needed
                    if (!_executedModules.Contains(module.Path))
                    {
                        ExecuteModule(module);
                    }

                    var symbol = module.Symbols.FirstOrDefault(s => s.Name == name);
                    if (symbol != null)
                    {
                        _currentModule.AddImport(name, symbol);
                        continue;
                    }
                }
                catch (RuntimeError)
                {
                    // If module not found, throw error
                    throw new RuntimeError($"Symbol '{name}' not found in module '{modulePath}'");
                }

                // If we get here, the symbol wasn't found
                throw new RuntimeError($"Symbol '{name}' not found in module '{modulePath}'");
            }
        }
    }

    /// <summary>
    /// Executes a module.
    /// If global is true, the module is executed in the global environment,
    /// otherwise it's executed in its own Environment.
    /// </summary>
    public void ExecuteModule(Module module, bool global = false)
    {
        var prevModule = _currentModule;

        try {
            _currentModule = module;

            if (global) {
            Execute(module.Ast!);
            }else {
                ExecuteModule(module);
            }

            // we probably don't need to tag it as executed
            //_executedModules.Add(module.Path);
        } finally {
            if (prevModule != null) {
                _currentModule = prevModule;
            }
        }
    }

    protected void Execute(ProgramNode node)
    {
        // Create a new resolver for this module
        var resolver = new Resolver(_interpreter);
        resolver.Resolve(node);

        // Execute the node
        _interpreter.Interpret(node);
    }

    public void SetCurrentModule(Module module)
    {
        _currentModule = module;
    }

    public void ExecuteModule(Module module) 
    {
        if (module.Ast != null)
        {
            // Store the module's environment if it already exists
            var moduleEnv = module.Environment ?? new Environment(_interpreter.globalEnvironment);
            var prevEnv = _interpreter.environment;
            var prevModule = _currentModule;
            //var prevLocals = _interpreter.Locals;
            // we don't neccecarily need to reset the locals because:
            // each entry is always unique because they're keyed by the AST Node.

            try
            {
                // Set up module context
                _interpreter.environment = moduleEnv;
                _interpreter.Locals = new Dictionary<Node, int>();
                _currentModule = module;

                // Create a new resolver for this module
                var resolver = new Resolver(_interpreter);
                resolver.ResolveModule(module.Ast, true);

                if (resolver.Errors.Count > 0)
                {
                    throw new RuntimeError(string.Join("\n", resolver.Errors));
                }

                // Execute the module
                _interpreter.Interpret(module.Ast);
                _executedModules.Add(module.Path);

                // Store the module's environment
                module.Environment = moduleEnv;
            }
            finally
            {
                // Restore the previous context
                _interpreter.environment = prevEnv;
                _currentModule = prevModule;

                // Reset the locals
                //_interpreter.Locals = prevLocals;
            }
        }
    }

    private Package LoadPackageDefinition(string path)
    {
        var source = new FileSourceCode(path);
        var tokens = _lexer.Tokenize(source.Code);
        var ast = _parser.Parse(tokens);

        // Find the package statement
        var packageName = ExtractPackageName(ast);
        if (packageName == null)
        {
            throw new RuntimeError($"No package declaration found in {path}");
        }

        return new Package(packageName, Path.GetDirectoryName(path)!);
    }

    private void ScanModules(Package package)
    {
        var modules = _resolver.ListModules(package.RootNamespace);

        foreach (var modulePath in modules)
        {
            if (modulePath.EndsWith("/package"))
                continue;

            var module = GetModule(modulePath);
            package.Modules[modulePath] = module;
        }
    }

    private string? ExtractPackageName(ProgramNode ast)
    {
        foreach (var stmt in ast.Statements)
        {
            if (stmt is PackageStmt packageStmt)
            {
                return string.Join("/", packageStmt.Path);
            }
        }
        return null;
    }
}
