using Xunit.Abstractions;
using Zen.Execution;
using Zen.Lexing;
using Zen.Parsing;
using Zen.Parsing.AST;

namespace Zen.Tests;

public class TestRunner {

    public static readonly bool Verbose = true; // prints tokens and AST when parsing

    private readonly ITestOutputHelper _output;

    public Lexer Lexer = new Lexer();
    public Parser Parser = new Parser();
    public Interpreter Interpreter = new Interpreter();
    public Resolver Resolver;

    public TestRunner(ITestOutputHelper output)
    {
        _output = output;
        Resolver = new(Interpreter);
    }

    public void RestartInterpreter() {
        Interpreter = new();
        Interpreter.GlobalOutputBufferingEnabled = true;
        Resolver = new(Interpreter);
    }

    public string? Execute(string code) {
        List<Token> tokens = Lexer.Tokenize(code);
        ProgramNode node = Parser.Parse(tokens);

        if (Verbose) {
            _output.WriteLine(Helper.PrintTokens(tokens));
            _output.WriteLine(Helper.PrintAST(node));
        }

        if (Parser.Errors.Count > 0) {
            _output.WriteLine(Helper.GetErrors(Parser.Errors));
            return null;
        }

        Resolver.Resolve(node);

        if (Resolver.Errors.Count > 0) {
            _output.WriteLine(Helper.GetErrors(Resolver.Errors));
            return null;
        }

        Interpreter.GlobalOutputBufferingEnabled = true;
        Interpreter.Interpret(node);
        string output = Interpreter.GlobalOutputBuffer.ToString();
        Interpreter.GlobalOutputBuffer.Clear();
        return output;
    }

}