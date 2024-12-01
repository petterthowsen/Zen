using Xunit.Abstractions;
using Zen.Typing;

namespace Zen.Tests.Execution;

public class FunctionTests : TestRunner
{
    public FunctionTests(ITestOutputHelper output) : base(output)
    {
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
    public void TestFuncWithLocalVars() {
        RestartInterpreter();
        string? result = Execute(@"func hello() {
    var a = 1
    var b = 2
    print a + b
}
hello()
");

        Assert.Equal("3", result);
    }

    [Fact]
    public void TestScope() {
        RestartInterpreter();

        Execute(@"
        func makeCounter() : func {
            var i = 0
            func increment():int {
                i += 1
                return i
            }
            return increment
        }");

        string? result = Execute(@"
        var counter = makeCounter()
        print counter()
        ");

        Assert.Equal("1", result);
    }
}