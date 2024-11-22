using Zen.Common;
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
    private readonly Dictionary<string, Module> _modules = new();
    private readonly Dictionary<string, Symbol> _symbols = new();
    private readonly HashSet<string> _executedModules = new();
    private readonly Parser _parser;
    private readonly Lexer _lexer;
    private readonly Interpreter _interpreter;

    public Importer(Parser parser, Lexer lexer, Interpreter interpreter)
    {
        _parser = parser;
        _lexer = lexer;
        _interpreter = interpreter;
    }

    /// <summary>
    /// Gets a module by its path. Throws if the module is not found.
    /// </summary>
    public Module GetModule(string modulePath)
    {
        if (!_modules.TryGetValue(modulePath, out var module))
        {
            throw new RuntimeError($"Module not found: {modulePath}");
        }
        return module;
    }

    /// <summary>
    /// Loads a package from the specified directory path.
    /// The directory must contain a package.zen file.
    /// </summary>
    public void LoadPackage(string path)
    {
        var packagePath = Path.Combine(path, "package.zen");
        if (!File.Exists(packagePath))
        {
            throw new RuntimeError($"No package.zen found in {path}");
        }

        // Phase 1: Symbol Resolution
        var package = LoadPackageDefinition(packagePath);
        ScanModules(package);
        _packages[package.RootNamespace] = package;
    }

    /// <summary>
    /// Resolves a symbol by name in the given namespace context.
    /// First checks the local namespace, then global imports.
    /// </summary>
    public Symbol? ResolveSymbol(string name, string currentNamespace)
    {
        // First check local namespace
        if (_symbols.TryGetValue($"{currentNamespace}.{name}", out var symbol))
            return symbol;

        // Then check global imports
        return _symbols.GetValueOrDefault(name);
    }

    /// <summary>
    /// Imports a module by path, optionally with an alias.
    /// For single-symbol modules, imports the symbol directly.
    /// For multi-symbol modules, imports under a namespace.
    /// </summary>
    public void Import(string modulePath, string? alias = null)
    {
        var module = LoadModule(modulePath);
        
        // Phase 2: Module Execution (if not already executed)
        if (!_executedModules.Contains(module.Path))
        {
            ExecuteModule(module);
        }

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

    /// <summary>
    /// Imports specific symbols from a module.
    /// </summary>
    public void ImportSymbols(string modulePath, IEnumerable<string> symbolNames)
    {
        // First try to load the module as a directory
        var module = LoadModule(modulePath);

        foreach (var name in symbolNames)
        {
            // First try to find the symbol in the directory module
            var symbol = module.Symbols.FirstOrDefault(s => s.Name == name);
            
            if (symbol == null)
            {
                // If not found, try to load it as a separate file
                var symbolModulePath = Path.Combine(modulePath, name).Replace("\\", "/");
                try
                {
                    var symbolModule = LoadModule(symbolModulePath);
                    
                    // Execute the symbol module if needed
                    if (!_executedModules.Contains(symbolModule.Path))
                    {
                        ExecuteModule(symbolModule);
                    }

                    if (symbolModule.Symbols.Count > 0)
                    {
                        symbol = symbolModule.Symbols[0];
                    }
                }
                catch (RuntimeError)
                {
                    // If we can't find the symbol module, throw an error
                    throw new RuntimeError($"Symbol '{name}' not found in module '{modulePath}'");
                }
            }
            else
            {
                // If we found the symbol in the directory module, make sure its source module is executed
                var sourceModulePath = Path.Combine(modulePath, name).Replace("\\", "/");
                if (_modules.TryGetValue(sourceModulePath, out var sourceModule) && !_executedModules.Contains(sourceModule.Path))
                {
                    ExecuteModule(sourceModule);
                }
            }

            if (symbol != null)
            {
                _symbols[name] = symbol;
            }
            else
            {
                throw new RuntimeError($"Symbol '{name}' not found in module '{modulePath}'");
            }
        }
    }

    private void ExecuteModule(Module module)
    {
        if (module.Ast != null)
        {
            // Create a new environment for the module
            var moduleEnv = new Environment(_interpreter.globalEnvironment);
            var prevEnv = _interpreter.environment;
            _interpreter.environment = moduleEnv;

            try
            {
                // Execute the module
                _interpreter.Interpret(module.Ast);
                _executedModules.Add(module.Path);

                // Store the module's environment
                module.Environment = moduleEnv;
            }
            finally
            {
                // Restore the previous environment
                _interpreter.environment = prevEnv;
            }
        }
    }

    private Package LoadPackageDefinition(string path)
    {
        var source = File.ReadAllText(path);
        var tokens = _lexer.Tokenize(source);
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
        var files = Directory.GetFiles(package.RootPath, "*.zen", SearchOption.AllDirectories)
            .Where(f => !Path.GetFileName(f).StartsWith("_")); // Ignore files starting with _

        foreach (var file in files)
        {
            if (Path.GetFileName(file) == "package.zen")
                continue;

            var relativePath = Path.GetRelativePath(package.RootPath, file);
            var modulePath = Path.Combine(package.RootNamespace, Path.ChangeExtension(relativePath, null)).Replace("\\", "/");
            var module = LoadModuleFile(file, modulePath);
            _modules[modulePath] = module;
            package.Modules[modulePath] = module;
        }
    }

    private Module LoadModule(string modulePath)
    {
        // First check if we already have this module loaded
        if (_modules.TryGetValue(modulePath, out var existingModule))
            return existingModule;

        // Find the package that contains this module
        var packageName = modulePath.Split('/')[0];
        if (!_packages.TryGetValue(packageName, out var package))
        {
            throw new RuntimeError($"Package '{packageName}' not found");
        }

        // Get the relative path within the package
        var relativePath = string.Join("/", modulePath.Split('/').Skip(1));
        
        // Try as a directory first
        var dirPath = Path.Combine(package.RootPath, relativePath);
        if (Directory.Exists(dirPath))
        {
            // Load all .zen files in the directory
            var files = Directory.GetFiles(dirPath, "*.zen")
                .Where(f => !Path.GetFileName(f).StartsWith("_"));
            
            var symbols = new List<Symbol>();
            foreach (var file in files)
            {
                var fileModule = LoadModuleFile(file, Path.Combine(modulePath, Path.GetFileNameWithoutExtension(file)));
                symbols.AddRange(fileModule.Symbols);
                _modules[fileModule.Path] = fileModule;
                package.Modules[fileModule.Path] = fileModule;
            }

            // Create a directory module that contains all symbols
            var module = Module.CreateDirectoryModule(modulePath, symbols);
            _modules[modulePath] = module;
            package.Modules[modulePath] = module;
            return module;
        }

        // Try as a file
        var filePath = Path.Combine(package.RootPath, relativePath + ".zen");
        if (File.Exists(filePath))
        {
            var module = LoadModuleFile(filePath, modulePath);
            _modules[modulePath] = module;
            package.Modules[modulePath] = module;
            return module;
        }

        throw new RuntimeError($"Module not found: {modulePath}");
    }

    private Module LoadModuleFile(string filePath, string modulePath)
    {
        var source = File.ReadAllText(filePath);
        var tokens = _lexer.Tokenize(source);
        var ast = _parser.Parse(tokens);

        // Extract symbols with the module reference
        var module = Module.CreateFileModule(modulePath, [], ast);
        var symbols = ExtractSymbols(ast, module);
        module.Symbols.AddRange(symbols);
        
        return module;
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

    private List<Symbol> ExtractSymbols(ProgramNode ast, Module module)
    {
        var symbols = new List<Symbol>();

        foreach (var stmt in ast.Statements)
        {
            switch (stmt)
            {
                case FuncStmt func:
                    symbols.Add(new Symbol(
                        func.Identifier.Value,
                        SymbolType.Function,
                        module,
                        func
                    ));
                    break;

                case ClassStmt cls:
                    symbols.Add(new Symbol(
                        cls.Identifier.Value,
                        SymbolType.Class,
                        module,
                        cls
                    ));
                    break;

                // Variables are not exported by default
                // case VarStmt:
                //     break;
            }
        }

        return symbols;
    }
}
