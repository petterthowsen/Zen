using System.ComponentModel.Design;
using Zen.Common;
using Zen.Exection.Import;
using Zen.Execution.Builtins.Core;
using Zen.Execution.EvaluationResult;
using Zen.Execution.Import;
using Zen.Parsing.AST.Statements;
using Zen.Typing;

namespace Zen.Execution;

public partial class Interpreter {
 
    public async Task<IEvaluationResult> VisitAsync(PackageStmt package)
    {
        // Package statements are handled during module loading
        return VoidResult.Instance;
    }
    
    public async Task<Module> GetModule(string modulePath)
    {
        var resolution = await Importer.Import(modulePath);
        if (resolution.IsModule())
        {
            var module = resolution.AsModule().Module;
            return module;
        }
        else
        {
            throw new RuntimeError($"Cannot import '{modulePath}': not a module or namespace");
        }
    }

    public async Task<ZenValue> FetchSymbol(string modulePath, string symbol)
    {
        var module = await GetModule(modulePath);
        return module.environment.GetValue(symbol);
    }

    public async Task<IEvaluationResult> VisitAsync(ImportStmt import)
    {
        var modulePath = string.Join("/", import.Path);

        // an "import" statement may import:
        // - a package (E.g 'myPackage')
        // - a namespace (E.g 'myPackage.someNamespace')
        // - a module (E.g 'myPackage.UtilFunctions')
        // We cannot import a symbol - that's done via 'FromImportStmt'.

        // We always import the symbols

        // Import through the Importer
        ImportResolution resolution = await Importer.Import(modulePath);

        if (resolution.IsModule())
        {
            var module = resolution.AsModule().Module;

            // single-symbol module?
            if (module.Symbols.Count == 1) {
                await ApplyModuleImport(module, module.Symbols.Select(s => s.Name).ToArray());
            }else {
                // group import under alias?
                if (import.Alias != null) {
                    await ApplyModuleImport(module, import.Alias.Value.Value);
                }else {
                    await ApplyModuleImport(module, module.Name);
                }
            }
        }

        return VoidResult.Instance;
    }

    private void ApplyNamespaceImport(Namespace @namespace)
    {
        foreach (Module module in @namespace.Modules.Values)
        {
            ApplyModuleImport(module);
        }
    }

    private async Task ApplyModuleImport(Module module, string? alias = null)
    {
        alias = alias ?? module.Name;

        Logger.Instance.Debug($"Applying module import {module.FullPath}, alias: {alias}");
        
        // Execute the module if it hasn't been executed yet
        if (module.State != State.Executed)
        {
            await Importer.Import(module.FullPath);
        }

        // create an instance of ModuleContainer for the module
        var mod = ModuleContainer.Clazz.CreateInstance(this, [], []);
        foreach (Symbol symbol in module.Symbols)
        {
            mod.SetProperty(symbol.Name, module.environment.GetValue(symbol.Name), create: true);
        }

        Environment.Define(true, alias, ZenType.Object, false);
        Environment.Assign(alias, new ZenValue(mod.Type, mod));
    }

    private async Task ApplyModuleImport(Module module, string[] symbols)
    {
        Logger.Instance.Debug($"Applying module import {module.FullPath}, symbols: {string.Join(", ", symbols)}");
        
        // Execute the module if it hasn't been executed yet
        if (module.State != State.Executed)
        {
            await Importer.Import(module.FullPath);
        }
        
        // Now import the symbols
        foreach (string symbol in symbols)
        {
            if (!module.HasSymbol(symbol))
            {
                throw new RuntimeError($"Module '{module.FullPath}' has no exported symbol named '{symbol}'");
            }

            var alias = symbol;

            // module.HasSymbol says true but let's debug this
            if ( ! module.environment.Exists(symbol)) {
                throw new Exception($"module.HasSymbol({symbol}) is true but module.environment.Exists({symbol}) is false!!");
            }

            ZenValue value = module.environment.GetValue(symbol);
            Environment.Define(true, alias, value.Type, false);
            Environment.Assign(alias, value);
        }
    }

    private void ApplyModuleImport(Module module)
    {
        List<string> symbolNames = [];
        foreach (var symbol in module.Symbols)
        {
            symbolNames.Add(symbol.Name);
        }

        ApplyModuleImport(module, symbolNames.ToArray());
    }

    public async Task<IEvaluationResult> VisitAsync(FromImportStmt fromImport)
    {
        var modulePath = string.Join("/", fromImport.Path);
        var symbols = fromImport.GetSymbolNames();
        
        // Resolve the base path
        ImportResolution resolution = Importer.ResolveSymbols(modulePath);

        if (resolution.IsModule())
        {
            // If it's a module, import the specified symbols from it
            var module = resolution.AsModule().Module;
            ApplyModuleImport(module, [..symbols]);
        }
        else if (resolution.HasModules())
        {
            // If it's a namespace, try to find modules matching the symbol names
            foreach (var symbol in symbols)
            {
                // First try to find a module with this name
                var symbolPath = $"{modulePath}/{symbol}";
                var symbolResolution = Importer.ResolveSymbols(symbolPath);
                
                if (symbolResolution.IsModule())
                {
                    var module = symbolResolution.AsModule().Module;
                    
                    // If this module has only one symbol and it matches its name,
                    // we can import it directly
                    if (module.HasSymbol(symbol))
                    {
                        ApplyModuleImport(module, new[] { symbol });
                        continue;
                    }
                }

                // If we couldn't find a matching module or it didn't have a matching symbol,
                // throw an error
                throw new RuntimeError($"Cannot import '{symbol}' from namespace '{modulePath}'");
            }
        }
        else
        {
            throw new RuntimeError($"Cannot import from '{modulePath}': not a module or namespace");
        }

        return VoidResult.Instance;
    }
}
