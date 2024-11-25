using Zen.Lexing;
using Zen.Parsing;
using Zen.Parsing.AST;
using Zen.Common;

namespace Zen.Execution;

/// <summary>
/// The main runtime for Zen, managing lexing, parsing, and execution.
/// </summary>
public class Runtime
{
    public readonly Lexer Lexer;
    public readonly Parser Parser;

    public readonly EventLoop EventLoop;
    public readonly Resolver Resolver;
    public readonly Interpreter Interpreter;

    public Runtime()
    {
        Lexer = new Lexer();
        Parser = new Parser();
        EventLoop = new EventLoop();
        EventLoop.Start();
        
        // Create interpreter first
        Interpreter = new Interpreter(EventLoop);
        
        // Then create importer with interpreter reference
        Resolver = new Resolver(Interpreter);
    }

    public ProgramNode Parse(ISourceCode sourceCode) => Parser.Parse(Lexer.Tokenize(sourceCode));

    public ProgramNode parse(string sourceCode) => Parse(new InlineSourceCode(sourceCode));

    /// <summary>
    /// Execute the given source code.
    /// </summary>
    /// <exception cref="Exception"></exception>
    public string? Execute(ISourceCode source)
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
        
        Interpreter.Interpret(node, true);
        
        string output = Interpreter.GlobalOutputBuffer.ToString();
        return output;
    }

    public string? Execute(string sourceCodeText) => Execute(new InlineSourceCode(sourceCodeText));

    public void Shutdown()
    {
        EventLoop.Stop();
    }
}
