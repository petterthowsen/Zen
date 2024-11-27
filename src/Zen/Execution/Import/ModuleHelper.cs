using Zen.Exection.Import;
using Zen.Lexing;
using Zen.Parsing;
using Zen.Parsing.AST;
using Zen.Parsing.AST.Statements;

namespace Zen.Execution.Import;

/// <summary>
/// Handles module processing including parsing, import resolution, and execution
/// </summary>
public class ModuleHelper
{
    protected Lexer lexer;
    protected Parser parser;
    protected Interpreter interpreter;

    public ModuleHelper(Interpreter interpreter)
    {
        lexer = new Lexer();
        parser = new Parser();
        this.interpreter = interpreter;
    }

    /// <summary>
    /// Parse the module's source code into an AST and extracts all top-level exportable Symbols.
    /// </summary>
    public void Parse(Module module)
    {
        if (module.State != State.NotLoaded)
        {
            throw new Exception($"Cannot parse module in state {module.State}");
        }
        
        module.State = State.Parsing;

        // Tokenize
        var tokens = lexer.Tokenize(module.Source);

        if (lexer.Errors.Count > 0)
        {
            throw new Exception(string.Join("\n", lexer.Errors));
        }

        // Parse
        var programNode = parser.Parse(tokens, throwErrors: true);

        // Set the AST on the module
        module.AST = programNode;
        
        module.Symbols = ExtractSymbols(module);

        module.State = State.ParseComplete;
    }

    /// <summary>
    /// Extracts the symbols from the given module's top level FuncStmt and ClassStmt.
    /// </summary>
    private static List<Symbol> ExtractSymbols(Module module)
    {
        List<Symbol> symbols = [];

        ProgramNode ast = module.AST ?? throw new Exception("Cannot extract symbols, module is not parsed!");

        foreach (var stmt in ast.Statements)
        {
            Symbol symbol;
            if (stmt is FuncStmt funcStmt) {
                symbol = new Symbol(funcStmt.Identifier.Value, SymbolType.Function, module);
            }
            else if (stmt is ClassStmt classStmt)
            {
                symbol = new Symbol(classStmt.Identifier.Value, SymbolType.Class, module);
            }else {
                continue;
            }

            symbols.Add(symbol);
        }

        return symbols;
    }
}
