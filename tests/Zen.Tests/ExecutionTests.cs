using Xunit.Abstractions;
using Zen.Execution;
using Zen.Lexing;
using Zen.Parsing;
using Zen.Parsing.AST;
using Zen.Parsing.AST.Statements;
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

        Execute("var i = 1");
        Variable variable = Interpreter.environment.GetVariable("i");
        ZenValue value = (ZenValue) variable.Value!;
        Assert.Equal(1, value.Underlying);

        Execute("i += 1");
        value = (ZenValue) variable.Value!;
        Assert.Equal(2, value.Underlying);
    }

    [Fact]
    public void TestVariableMinusAsignment() {
        RestartInterpreter();

        Execute("var i = 1");
        Variable variable = Interpreter.environment.GetVariable("i");
        ZenValue value = (ZenValue) variable.Value!;
        Assert.Equal(1, value.Underlying);

        Execute("i -= 1");
        value = (ZenValue) variable.Value!;
        Assert.Equal(0, value.Underlying);
    }

    [Fact]
    public void TestVariableMultiplyAsignment() {
        RestartInterpreter();

        Execute("var i = 5");
        Variable variable = Interpreter.environment.GetVariable("i");
        ZenValue value = (ZenValue) variable.Value!;
        Assert.Equal(5, value.Underlying);

        Execute("i *= 2");
        value = (ZenValue) variable.Value!;
        Assert.Equal(10, value.Underlying);
    }

    [Fact]
    public void TestVariableDivideAsignment() {
        RestartInterpreter();

        Execute("var i = 10");
        Variable variable = Interpreter.environment.GetVariable("i");
        ZenValue value = (ZenValue) variable.Value!;
        Assert.Equal(10, value.Underlying);

        Execute("i /= 2");
        value = (ZenValue) variable.Value!;
        Assert.Equal(5, value.Underlying);
    }

    [Fact]
    public void TestFloat() {
        RestartInterpreter();

        Execute("var pi = 3.0");
        Variable pi = Interpreter.environment.GetVariable("pi");
        ZenValue value = (ZenValue) pi.Value!;
        Assert.Equal(3.0, value.Underlying);
    }
    
    [Fact]
    public void TestForLoop() {
        RestartInterpreter();

        string? result = Execute("for i = 0; i < 2; i += 1 { print i }");
        Assert.Equal("01", result);
    }

    
    [Fact]
    public void TestMath() {
        RestartInterpreter();

        string? result = Execute("print 2 * 2 + 1");
        Assert.Equal("5", result);
    }

    
    [Fact]
    public void TestMath2() {
        RestartInterpreter();

        string? result = Execute("print 2 * (2 + 1)");
        Assert.Equal("6", result);
    }

    [Fact]
    public void TestFuncDeclaration() {
        RestartInterpreter();
        Execute("func hello() {}");

        Assert.True(Interpreter.environment.Exists("hello"));
        
        // get the value
        ZenValue hello = Interpreter.environment.GetValue("hello");

        // make sure its callable
        Assert.True(hello.IsCallable());

        // is of type ZenUserFunction
        Assert.IsType<ZenUserFunction>(hello.Underlying);

        // get the ZenFunction
        ZenUserFunction function = (ZenUserFunction) hello.Underlying!;

        // takes 0 arguments
        Assert.Equal(0, function.Arity);

        // returns void
        Assert.Equal(ZenType.Void, function.ReturnType);
    }

    
    [Fact]
    public void TestFuncDeclarationWithIntReturnType() {
        RestartInterpreter();
        Execute("func hello(): int {}");

        Assert.True(Interpreter.environment.Exists("hello"));
        
        // get the value
        ZenValue hello = Interpreter.environment.GetValue("hello");

        // make sure its callable
        Assert.True(hello.IsCallable());

        // is of type ZenUserFunction
        Assert.IsType<ZenUserFunction>(hello.Underlying);

        // get the ZenFunction
        ZenUserFunction function = (ZenUserFunction) hello.Underlying!;

        // takes 0 arguments
        Assert.Equal(0, function.Arity);

        // returns int
        Assert.Equal(ZenType.Integer, function.ReturnType);
    }

    [Fact]
    public void TestFuncExecution() {
        RestartInterpreter();
        Execute("func hello() { print \"hello!\" }");

        string? result = Execute("hello()");

        Assert.Equal("hello!", result);
    }
}