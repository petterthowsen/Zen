using Zen.Common;
using Zen.Execution.Import.Providers;
using Zen.Lexing;
using Zen.Parsing;
using Zen.Parsing.AST;
using Zen.Parsing.AST.Statements;

namespace Zen.Execution.Import;

/// <summary>
/// Coordinates between different module providers to load modules from any available source.
/// </summary>
public class ModuleResolver
{
    private readonly List<IModuleProvider> _providers = new();
    private readonly Dictionary<string, Module> _moduleCache = new();

    public ModuleResolver(IEnumerable<IModuleProvider> providers)
    {
        // Sort providers by priority (highest first)
        _providers.AddRange(providers.OrderByDescending(p => p.Priority));
    }

    /// <summary>
    /// Registers a new module provider and re-sorts providers by priority.
    /// </summary>
    public void RegisterProvider(IModuleProvider provider)
    {
        _providers.Add(provider);
        // Re-sort providers by priority (highest first)
        _providers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    /// <summary>
    /// Resolves a module by path, checking providers in priority order.
    /// </summary>
    public Module ResolveModule(string modulePath, Parser parser, Lexer lexer, Interpreter interpreter)
    {
        // Check cache first
        if (_moduleCache.TryGetValue(modulePath, out var cachedModule))
            return cachedModule;

        // Find provider that can handle this module
        var provider = _providers.FirstOrDefault(p => p.CanProvide(modulePath))
            ?? throw new RuntimeError($"No provider found for module: {modulePath}");

        // Get module source
        var source = provider.GetModuleSource(modulePath);

        // Parse module
        var tokens = lexer.Tokenize(source.Code);
        var ast = parser.Parse(tokens);

        // Create module
        var module = Module.CreateFileModule(modulePath, [], ast);

        // Extract symbols
        var symbols = ExtractSymbols(ast, module);
        module.Symbols.AddRange(symbols);

        // Cache module
        _moduleCache[modulePath] = module;

        return module;
    }

    /// <summary>
    /// Lists available modules in a directory across all providers.
    /// </summary>
    public IEnumerable<string> ListModules(string directoryPath)
    {
        var modules = new HashSet<string>();

        foreach (var provider in _providers)
        {
            var providerModules = provider.ListModules(directoryPath);
            modules.UnionWith(providerModules);
        }

        return modules;
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
