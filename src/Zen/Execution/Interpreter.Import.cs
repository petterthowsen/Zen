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

        // an "import" statement may import:
        // - a package
        // - a namespace
        // - a module

        // in the case of a package or namespace, we import all modules directly under that and import all symbols
        // in the case of a module, it depends on whether the module exports a single symbol or more than one
        // for single-symbol modules, the symbol will be exported as is
        // for multi-symbol modules, we 

        // Import the module through the Importer
        ImportResolution = Importer.Resolve(modulePath);

        // Get all symbols from the module and copy them to the current environment
        var module = Importer.GetModule(modulePath);
        if ( ! module.IsInitialized)
        {
            throw new RuntimeError($"Module {module.Path} has not been executed");
        }

        if (module.Symbols.Count == 1)
        {
            // todo: throw error if alias is already defined
            var symbol = module.Symbols[0];
            var name = alias ?? symbol.Name;
            ZenValue value = module.Environment.GetValue(symbol.Name);
            environment.Define(true, name, value.Type, false);
            environment.Assign(name, value);
        }
        else
        {
            // here we need to essentially create a object with all the symbols in it.
            throw new NotImplementedException("Implicit Multi-symbol imports not implemented yet");
        }

        return VoidResult.Instance;
    }

    public IEvaluationResult Visit(FromImportStmt fromImport)
    {
        return VoidResult.Instance;
    }
}
