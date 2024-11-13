using Xunit.Abstractions;
using Zen.Execution;
using Zen.Lexing;
using Zen.Parsing;
using Zen.Parsing.AST;
using Zen.Typing;

namespace Zen.Tests;

public class ExecutionTests {

    public static readonly bool Verbose = true; // prints tokens and AST when parsing

    private readonly ITestOutputHelper _output;

    public Lexer Lexer = new Lexer();
    public Parser Parser = new Parser();
    public Interpreter Interpreter = new Interpreter();

    public ExecutionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private void RestartInterpreter() {
        Interpreter = new();
        Interpreter.GlobalOutputBufferingEnabled = true;
    }

    private string? Execute(string code) {
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

        Interpreter.GlobalOutputBufferingEnabled = true;

        Interpreter.Interpret(node);
        return Interpreter.GlobalOutputBuffer.ToString();
    }

    [Fact]
    public void TestPrint() {
        RestartInterpreter();

        string? result = Execute("print \"hello world\"");
        Assert.Equal("hello world", result);
    }

    [Fact]
    public void TestPrintNewline() {
        RestartInterpreter();
        
        string? result = Execute("print \"hello\\nworld\"");
        Assert.Equal("hello\nworld", result);
    }

    [Fact]
    public void TestVariableDeclaration() {
        RestartInterpreter();

        Assert.False(Interpreter.environment.Exists("name"));
        Execute("var name = \"john\"");
        Assert.True(Interpreter.environment.Exists("name"));
        
        string? result = Execute("print name");
        Assert.Equal("john", result);

        Variable variable = Interpreter.environment.GetVariable("name");
        Assert.False(variable.Constant);
        Assert.False(variable.Nullable);
        
        ZenType type = variable.Type;
        Assert.Equal(ZenType.String, type);
    }

    
    [Fact]
    public void TestVariableDeclarationAndAssignment() {
        RestartInterpreter();

        Assert.False(Interpreter.environment.Exists("name"));
        Execute("var name = \"john\"");
        Assert.True(Interpreter.environment.Exists("name"));
        Variable variable = Interpreter.environment.GetVariable("name");
        ZenValue value = (ZenValue) variable.Value!;
        Assert.Equal("john", value.Underlying);

        Execute("name = \"doe\"");
        value = (ZenValue) variable.Value!;
        Assert.Equal("doe", value.Underlying);
    }

    [Fact]
    public void TestVariablePlusAsignment() {
        RestartInterpreter();

        Execute("var i = 0");
        Variable variable = Interpreter.environment.GetVariable("i");
        ZenValue value = (ZenValue) variable.Value!;
        Assert.Equal(0, value.Underlying);

        Execute("i += 1");
        value = (ZenValue) variable.Value!;
        Assert.Equal(1, value.Underlying);
    }

    
    [Fact]
    public void TestForLoop() {
        RestartInterpreter();

        string? result = Execute("for i = 0; i < 2; i++ { print i }");
        Assert.Equal("0\n1\n", result);
    }

}