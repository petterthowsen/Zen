using Xunit.Abstractions;
using Zen.Common;
using Zen.Execution;
using Zen.Execution.Import;
using Zen.Lexing;
using Zen.Parsing;

namespace Zen.Tests;

public class TestRunner
{
    protected readonly ITestOutputHelper Output;
    protected readonly Lexer Lexer;
    protected readonly Parser Parser;
    protected Resolver Resolver;
    protected Interpreter Interpreter;
    protected readonly EventLoop EventLoop;
    protected Importer Importer;

    public TestRunner(ITestOutputHelper output)
    {
        Output = output;
        Lexer = new Lexer();
        Parser = new Parser();
        EventLoop = new EventLoop();
        EventLoop.Start();
        
        RestartInterpreter();
    }

    protected virtual void RestartInterpreter()
    {
        // Create interpreter first
        Interpreter = new Interpreter(EventLoop);
        
        // Then create importer with interpreter reference
        Importer = new Importer(Parser, Lexer, Interpreter);
        
        // Finally set importer on interpreter
        Interpreter.SetImporter(Importer);
        
        Resolver = new Resolver(Interpreter);
    }

    protected string? Execute(ISourceCode source)
    {
        List<Token> tokens = Lexer.Tokenize(source);
        var node = Parser.Parse(tokens);

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

    protected string? Execute(string source) => Execute(new InlineSourceCode(source));
}
