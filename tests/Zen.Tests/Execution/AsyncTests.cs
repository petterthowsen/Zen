using Xunit.Abstractions;
using Zen.Typing;

namespace Zen.Tests.Execution;

public class AsyncTests : TestRunner
{
    public AsyncTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void TestAsyncFuncDeclaration() {
        RestartInterpreter();
        Execute("async func hello() {}");

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

        // is async
        Assert.True(function.Async);
    }

    [Fact]
    public void TestAsyncFuncWithTaskReturn() {
        RestartInterpreter();
        Execute("async func hello(): Task<int> { return 42 }");

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

        // returns Promise<int>
        Assert.True(function.ReturnType.IsTask);
        Assert.Equal(ZenType.Integer, function.ReturnType.Parameters[0]);
    }

    [Fact]
    public void TestAwait()
    {
        RestartInterpreter();

        string? result = Execute(@"
            async func test() {
                print ""before""
            }
            await test()
            print ""after""
        ", true);

        Assert.Equal("beforeafter", result);
    }

    [Fact]
    public void TestAsyncDelay() {
        RestartInterpreter();
        
        string? result = Execute(@"
            async func test() {
                var start = time()
                await delay(100)
                var elapsed = time() - start
                print elapsed >= 100
                print true
            }
            test()
        ", true);

        Assert.Equal("true", result);
    }

    [Fact]
    public void TestAsyncFuncWithLocalVars() {
        RestartInterpreter();

        string? result = Execute(@"
            async func test() {
                var b = await delay(100)
                print ""after""
            }
            test()
            print ""before""
        ");

        Assert.Equal("beforeafter", result);
    }

    [Fact]
    public void TestMultipleAwaits() {
        RestartInterpreter();

        string? result = Execute(@"
            async func test() {
                var start = time()
                await delay(50)
                await delay(50)
                var elapsed = time() - start
                print elapsed >= 100
            }
            test()
        ");

        Assert.Equal("true", result);
    }

    [Fact]
    public void TestAsyncError() {
        RestartInterpreter();

        string? result = Execute(@"
            async func test() {
                print ""this should throw an error""
                var error = 5 / 0
            }
            test()
        ");
    }
}
