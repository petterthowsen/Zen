using Zen.Lexing;
using Zen.Parsing;
using Zen.Parsing.AST;
using Zen.Execution.Import;
using Zen.Common;

namespace Zen.Execution;

/// <summary>
/// The main runtime for Zen, managing lexing, parsing, and execution.
/// </summary>
public class Runtime
{
    public readonly Lexer Lexer;
    public readonly Parser Parser;
    public readonly Resolver Resolver;
    public readonly Interpreter Interpreter;
    public readonly EventLoop EventLoop;
    public readonly Importer Importer;

    public Runtime()
    {
        Lexer = new Lexer();
        Parser = new Parser();
        EventLoop = new EventLoop();
        EventLoop.Start();
        
        // Create interpreter first
        Interpreter = new Interpreter(EventLoop);
        
        // Then create importer with interpreter reference
        Importer = new Importer(Parser, Lexer, Interpreter);
        
        // Finally set importer on interpreter
        Interpreter.SetImporter(Importer);
        
        Resolver = new Resolver(Interpreter);
    }

    public string? Execute(ISourceCode sourceCode)
    {
        List<Token> tokens = Lexer.Tokenize(sourceCode);
        ProgramNode node = Parser.Parse(tokens);
        
        if (Parser.Errors.Count > 0)
        {
            throw new Exception("Parse errors: " + string.Join("\n", Parser.Errors));
        }

        Resolver.Resolve(node);
        
        if (Resolver.Errors.Count > 0)
        {
            throw new Exception("Resolver errors: " + string.Join("\n", Resolver.Errors));
        }

        Interpreter.GlobalOutputBufferingEnabled = true;
        Interpreter.Interpret(node);
        string output = Interpreter.GlobalOutputBuffer.ToString();
        Interpreter.GlobalOutputBuffer.Clear();
        return output;
    }

    public string? Execute(string sourceCodeText) => Execute(new InlineSourceCode(sourceCodeText));

    public void LoadPackage(string path)
    {
        Importer.LoadPackage(path);
    }

    public void Shutdown()
    {
        EventLoop.Stop();
    }
}
