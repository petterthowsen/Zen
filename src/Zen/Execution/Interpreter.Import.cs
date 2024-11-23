using Zen.Execution.EvaluationResult;
using Zen.Execution.Import;
using Zen.Parsing.AST.Statements;
using Zen.Typing;

namespace Zen.Execution;

public partial class Interpreter {
    private string? _workingNamespace;

    /// <summary>
    /// Sets the working namespace for resolving imports.
    /// </summary>
    public void SetWorkingNamespace(string? ns)
    {
        _workingNamespace = ns;
    }

    public IEvaluationResult Visit(PackageStmt package)
    {
        // Package statements are handled during module loading
        return VoidResult.Instance;
    }

    public IEvaluationResult Visit(ImportStmt import)
    {
        var modulePath = string.Join("/", import.Path);
        var alias = import.Alias?.Value;

        // Import the module through the Importer
        Importer.Import(modulePath, alias);

        // Get all symbols from the module and copy them to the current environment
        var module = Importer.GetModule(modulePath);
        if (module.Environment == null)
        {
            throw new RuntimeError($"Module {module.Path} has not been executed");
        }

        if (module.IsSingleSymbol)
        {
            var symbol = module.Symbols[0];
            var name = alias ?? symbol.Name;
            ZenValue value = module.Environment.GetValue(symbol.Name);
            environment.Define(true, name, value.Type, false);
            environment.Assign(name, value);
        }
        else
        {
            var ns = alias ?? module.Namespace;
            foreach (var symbol in module.Symbols)
            {
                ZenValue value = module.Environment.GetValue(symbol.Name);
                var name = $"{ns}.{symbol.Name}";
                environment.Define(true, name, value.Type, false);
                environment.Assign(name, value);
            }
        }

        return VoidResult.Instance;
    }

    public IEvaluationResult Visit(FromImportStmt fromImport)
    {
        var modulePath = string.Join("/", fromImport.Path);
        var symbolNames = fromImport.Symbols.Select(s => s.Value);

        // Import the symbols through the Importer
        Importer.ImportSymbols(modulePath, symbolNames);

        // Copy each imported symbol from its module's environment to the current environment
        foreach (var name in symbolNames)
        {
            var symbol = Importer.ResolveSymbol(name, modulePath);
            if (symbol != null && symbol.Module.Environment != null)
            {
                ZenValue value = symbol.Module.Environment.GetValue(symbol.Name);
                environment.Define(true, name, value.Type, false);
                environment.Assign(name, value);
            }
        }

        return VoidResult.Instance;
    }
}
