using Xunit.Abstractions;
using Zen.Typing;

namespace Zen.Tests.Execution;

public class AsyncTests : TestRunner
{
    public AsyncTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async void TestAsyncFuncDeclaration() {
        await RestartInterpreter();
        await Execute("async func hello() {}");

        Assert.True(Interpreter.environment.Exists("hello"));
        
        // get the value
        ZenValue hello = Interpreter.environment.GetValue("hello");

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

        // is async
        Assert.True(function.Async);
    }

    [Fact]
    public async void TestAsyncFuncWithTaskReturn() {
        await RestartInterpreter();
        await Execute("async func hello(): Task<int> { return 42 }");

        Assert.True(Interpreter.environment.Exists("hello"));
        
        // get the value
        ZenValue hello = Interpreter.environment.GetValue("hello");

        // make sure its callable
        Assert.True(hello.IsCallable());

        // is of type ZenFunction
        Assert.IsType<ZenFunction>(hello.Underlying);

        // get the ZenFunction
        ZenFunction function = (ZenFunction) hello.Underlying!;

        // takes 0 arguments
        Assert.Equal(0, function.Arity);

        // returns Task<int>
        Assert.True(function.ReturnType.IsTask);
        Assert.Equal(ZenType.Integer, function.ReturnType.Parameters[0]);
    }

    
    [Fact]
    public async void TestClassWithAsyncFunction() {
        await RestartInterpreter();
        await Execute(@"
            class Test {
                async hello(): string {
                    return ""hello world""
                }
            }
        ");

        ZenClass testClass = (ZenClass)Interpreter.environment.GetValue("Test")!.Underlying!;
        
        Assert.Equal("Test", testClass.Name);
        Assert.Equal("hello", testClass.Methods.First().Name);
        Assert.True(testClass.Methods.First().Async);

        string? result = await Execute(@"
            async func main() {
                var t = new Test()
                var helloWorld = await t.hello()
                print helloWorld
            }

            main()
        ", true);

        Assert.Equal("hello world", result);
    }

    [Fact]
    public async void TestAwait()
    {
        await RestartInterpreter();

        string? result = await Execute(@"
            async func test() {
                print ""before""
            }
            async func main() {
                await test()
                print ""after""
            }
            main()
        ", true);

        Assert.Equal("beforeafter", result);
    }

    [Fact]
    public async void TestAsyncDelay() {
        await RestartInterpreter();
        
        string? result = await Execute(@"
            async func test() {
                var start = time()
                await delay(100)
                var elapsed = time() - start
                print elapsed
            }
            test()
        ", true);
        Output.WriteLine("test Execute() finished, result output:" + result);

        int elapsed = int.Parse(result!);

        Assert.True(elapsed > 100, "Expected a delay of at least 100ms but got " + elapsed);
    }

    
    [Fact]
    public async void TestAsyncReturn()
    {
        await RestartInterpreter();
        
        string? result = await Execute(@"
            async func test(): string {
                await delay(50)
                return ""done""
            }
            async func main() {
                print await test()
            }

            main()
        ", true);

        Assert.Equal("done", result);
    }

    [Fact]
    public async void TestAsyncCallDotNet()
    {
        await RestartInterpreter();
        
        string? result = await Execute(@"
            async func test(): int {
                # Wait asynchronously for 100ms
                var start = time()
                await delay(100)
                var end = time()
                var elapsed = end - start
                return elapsed # should cause an error because it's missing a cast from int64 to int
            }

            async func main() {
                print await test()
            }

            main()
        ", true);

        int elapsed = int.Parse(result!);
        
        Assert.True(elapsed >= 100, "Expected a delay of at least 100ms but got " + elapsed);
    }
}
