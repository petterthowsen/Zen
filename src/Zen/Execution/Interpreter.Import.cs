using Zen.Execution.EvaluationResult;
using Zen.Execution.Import;
using Zen.Parsing.AST.Statements;
using Zen.Typing;

namespace Zen.Execution;

public partial class Interpreter {
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

        // Get all symbols from the module and define them in the environment
        var module = Importer.GetModule(modulePath);
        if (module.IsSingleSymbol)
        {
            var symbol = module.Symbols[0];
            var name = alias ?? symbol.Name;
            DefineSymbol(name, symbol, module);
        }
        else
        {
            var ns = alias ?? module.Namespace;
            foreach (var symbol in module.Symbols)
            {
                DefineSymbol($"{ns}.{symbol.Name}", symbol, module);
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

        // Define each imported symbol in the environment
        foreach (var name in symbolNames)
        {
            var symbol = Importer.ResolveSymbol(name, modulePath);
            if (symbol != null)
            {
                DefineSymbol(name, symbol, symbol.Module);
            }
        }

        return VoidResult.Instance;
    }

    private void DefineSymbol(string name, Symbol symbol, Module module)
    {
        if (module.Environment == null)
        {
            throw new RuntimeError($"Module {module.Path} has not been executed");
        }

        // Define the symbol as a constant in the environment
        environment.Define(true, name, symbol.Type switch
        {
            SymbolType.Function => ZenType.Function,
            SymbolType.Class => ZenType.Class,
            _ => ZenType.Any
        }, false);

        // Create the appropriate value based on symbol type
        ZenValue value;

        if (symbol.Type == SymbolType.Function) {
            FuncStmt funcStmt = (FuncStmt) symbol.Node;
            ZenUserFunction function = ParseFunctionStatement(funcStmt, module.Environment);
            value = new ZenValue(ZenType.Function, function);
        }
        else if (symbol.Type == SymbolType.Class) {
            ClassStmt classStmt = (ClassStmt) symbol.Node;
            ZenClass clazz = ParseClassStatement(classStmt);
            value = new ZenValue(ZenType.Class, clazz);
        }
        else {
            value = ZenValue.Null;
        }

        // Assign the value
        environment.Assign(name, value);
    }
}
