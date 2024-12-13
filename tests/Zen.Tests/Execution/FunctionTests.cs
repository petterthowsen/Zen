using Xunit.Abstractions;
using Zen.Typing;

namespace Zen.Tests.Execution;

public class FunctionTests : TestRunner
{
    public FunctionTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async void TestFuncDeclaration() {
        await RestartInterpreter();
        await Execute("func hello() {}");

        Assert.True(Interpreter.Environment.Exists("hello"));
        
        // get the value
        ZenValue hello = Interpreter.Environment.GetValue("hello");

        // make sure its callable
        Assert.True(hello.IsCallable());

        // is of type ZenFunction
        Assert.IsType<ZenFunction>(hello.Underlying);

        // get the ZenFunction
        ZenFunction function = (ZenFunction) hello.Underlying!;

        // takes 0 arguments
        Assert.Equal(0, function.Arity);

        // returns void
        Assert.Equal(ZenType.Void, function.ReturnType);
    }

    
    [Fact]
    public async void TestFuncDeclarationWithIntReturnType() {
        await RestartInterpreter();
        await Execute("func hello(): int {}");

        Assert.True(Interpreter.Environment.Exists("hello"));
        
        // get the value
        ZenValue hello = Interpreter.Environment.GetValue("hello");

        // make sure its callable
        Assert.True(hello.IsCallable());

        // is of type ZenFunction
        Assert.IsType<ZenFunction>(hello.Underlying);

        // get the ZenFunction
        ZenFunction function = (ZenFunction) hello.Underlying!;

        // takes 0 arguments
        Assert.Equal(0, function.Arity);

        // returns int
        Assert.Equal(ZenType.Integer, function.ReturnType);
    }

    [Fact]
    public async void TestFuncExecution() {
        await RestartInterpreter();
        await Execute("func hello() { print \"hello!\" }");

        string? result = await Execute("hello()", true);

        Assert.Equal("hello!", result);
    }

    [Fact]
    public async void TestFuncWithLocalVars() {
        await RestartInterpreter();
        string? result = await Execute(@"
        func hello() {
            var a = 1
            var b = 2
            print a + b
            }
            hello()
        ", true);

        Assert.Equal("3", result);
    }

    [Fact]
    public async void TestScope() {
        await RestartInterpreter();

        await Execute(@"
        func makeCounter() : Func {
            var i = 0
            func increment():int {
                i += 1
                return i
            }
            return increment
        }");

        string? result = await Execute(@"
        var counter = makeCounter()
        print counter()
        ", true);

        Assert.Equal("1", result);
    }

    [Fact]
    public async void TestFunctionsWithDefaultArgument()
    {
        await RestartInterpreter();

        string? result = await Execute(@"
        func hello(name: string = ""john""): string {
            return name
        }
        print hello()
        ", true);

        Assert.Equal("john", result);
    }

    
    [Fact]
    public async void TestFunctionsWithOneArgumentAndDefault()
    {
        await RestartInterpreter();

        string? result = await Execute(@"
        func hello(name: string, greeting: string = ""hello""): string {
            return greeting + "" "" + name
        }
        print hello(""john"")
        ", true);

        Assert.Equal("hello john", result);
    }
}