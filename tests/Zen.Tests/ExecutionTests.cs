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
    public Resolver Resolver;

    public ExecutionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private void RestartInterpreter() {
        Interpreter = new();
        Interpreter.GlobalOutputBufferingEnabled = true;
        Resolver = new(Interpreter);
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

        Resolver.Resolve(node);

        if (Resolver.Errors.Count > 0) {
            _output.WriteLine(Helper.GetErrors(Resolver.Errors));
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

    [Fact]
    public void TestScope() {
        RestartInterpreter();

        Execute("func makeCounter() : func { var i = 0\n func increment():int { i += 1\n return i }\n return increment }");

        string? result = Execute("var counter = makeCounter()\nprint counter()");
        Assert.Equal("1", result);
    }

    [Fact]
    public void TestUndefinedVariable() {
        RestartInterpreter();

        RuntimeError error = Assert.Throws<RuntimeError>(() => Execute("number = 1"));

        Assert.Equal(Common.ErrorType.UndefinedVariable, error.Type);
    }

    [Fact]
    public void TestClassDeclaration() {
        RestartInterpreter();
        Execute("class Test {}");

        Assert.True(Interpreter.environment.Exists("Test"));
        
        // get the value
        ZenValue test = Interpreter.environment.GetValue("Test");

        // make sure its a class
        Assert.Equal(ZenType.Class, test.Type);
        Assert.IsType<ZenClass>(test.Underlying);
    }

    
    [Fact]
    public void TestClassInstantiation() {
        RestartInterpreter();

        Execute("class Test {}");

        Execute("var t = new Test()");

        Assert.True(Interpreter.environment.Exists("t"));

        // get the value
        ZenValue test = Interpreter.environment.GetValue("t");

        // make sure itsa ZenType.Object
        Assert.Equal(ZenType.Object, test.Type);
        Assert.IsType<ZenObject>(test.Underlying);

        string? result = Execute("print t.ToString()");
        Assert.Equal("Object(Test)", result);
    }

    
    [Fact]
    public void TestClassProperty() {
        RestartInterpreter();

        Execute("class Test { name: string = \"john\"}");

        Execute("var t = new Test()");

        Assert.True(Interpreter.environment.Exists("t"));

        // get the value
        ZenValue test = Interpreter.environment.GetValue("t");

        // make sure its a ZenType.Object
        Assert.Equal(ZenType.Object, test.Type);
        Assert.IsType<ZenObject>(test.Underlying);

        // get the object
        ZenObject testObject = (ZenObject)test.Underlying!;

        // make sure it has the expected property
        Assert.True(testObject.Properties.ContainsKey("name"));
        
        // get the value
        ZenValue nameValue = testObject.Properties["name"];

        // make sure its a ZenType.String
        Assert.Equal(ZenType.String, nameValue.Type);
        Assert.Equal("john", nameValue.Underlying);

        string? result = Execute("print t.name");

        Assert.Equal("john", result);

        // set the property
        Execute("t.name = \"bob\"");

        // get the value
        nameValue = testObject.Properties["name"];

        // make sure its a ZenType.String
        Assert.Equal(ZenType.String, nameValue.Type);
        Assert.Equal("bob", nameValue.Underlying);
    }

    
    [Fact]
    public void TestConstructor()
    {
        RestartInterpreter();

        string code = @"
class Point {
    x: int
    y: int

    Point(x: int, y: int) {
        this.x = x
        this.y = y
    }
}
";

        Execute(code);

        ZenClass Point = (ZenClass) Interpreter.environment.GetValue("Point")!.Underlying!;

        // test properties
        Assert.Equal(2, Point.Properties.Count);

        Assert.True(Point.Properties.ContainsKey("x"));
        Assert.True(Point.Properties.ContainsKey("y"));

        Assert.Equal(ZenType.Integer, Point.Properties["x"].Type);
        Assert.Equal(ZenType.Integer, Point.Properties["y"].Type);

        // test constructor
        Point.HasOwnConstructor([ZenType.Integer, ZenType.Integer], out ZenMethod? constructor);
        Assert.NotNull(constructor);

        Assert.Equal(2, constructor!.Arity);
        Assert.Equal(ZenType.Void, constructor.ReturnType);

        // // define a new point
        Execute("var p = new Point(5, 10)");

        // // make sure it has the expected properties
        Assert.True(Interpreter.environment.Exists("p"));
        ZenValue pValue = Interpreter.environment.GetValue("p");
        Assert.Equal(ZenType.Object, pValue.Type);
        Assert.IsType<ZenObject>(pValue.Underlying);
        ZenObject pObject = (ZenObject)pValue.Underlying!;
        
        Assert.True(pObject.Properties.ContainsKey("x"));
        Assert.True(pObject.Properties.ContainsKey("y"));
        ZenValue xValue = pObject.Properties["x"];
        ZenValue yValue = pObject.Properties["y"];
        Assert.Equal(ZenType.Integer, xValue.Type);
        // Assert.Equal(5, xValue.Underlying);
        Assert.Equal(ZenType.Integer, yValue.Type);
        // Assert.Equal(10, yValue.Underlying);

        // update the x property
        Execute("p.x = 7");

        // get the value
        xValue = pObject.Properties["x"];

        // make sure its a ZenType.Int
        Assert.Equal(ZenType.Integer, xValue.Type);
        Assert.Equal(7, xValue.Underlying);
    }
}