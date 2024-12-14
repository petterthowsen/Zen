using Xunit.Abstractions;
using Zen.Exection;
using Zen.Typing;

namespace Zen.Tests.Execution;

public class AsyncTests : TestRunner
{
    public AsyncTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async void TestRunFunc() {
        await RestartInterpreter();
        await Execute(@"
            func hello() {
                print ""hello""
            }

            run(hello)
        ", true);
    }

    [Fact]
    public async void TestRunFuncThatThrowsException() {
        await RestartInterpreter();
        await Execute(@"
            func hello() {
                throw new Exception(""oops"")
                print ""hello""
            }");

        await Assert.ThrowsAsync<ZenException>(async () => await Execute("run(hello)", true));
    }

    [Fact]
    public async Task TestPromise() 
    {
        await RestartInterpreter();
        string? result = await Execute(@"
            import Zen/Promise

            func universe():int {
                print ""universe executed\n""
                return 42
            }

            # create a promise which executes the function on the event loop right away
            var promise = new Promise<int>(universe)
            
            func thenCallback() {
                print promise.Result
            }
            promise.Then(thenCallback)
        ", true);
        
        Output.WriteLine(Interpreter.GlobalOutputBuffer.ToString());

        //Assert.Equal("42", result);
    }

    [Fact]
    public async Task TestAwaitPromise() 
    {
        await RestartInterpreter();
        string? result = await Execute(@"
            import Zen/Promise

            func universe():int {
                return 42
            }

            # create a promise which executes the function on the event loop right away
            var promise = new Promise<int>(universe)
            
            func thenCallback() {
                print promise.Result
            }
            promise.Then(thenCallback)

            func main() {
                await promise
                print promise.Result
            }

            run(main)
        ", true);
        
        Output.WriteLine(Interpreter.GlobalOutputBuffer.ToString());

        //Assert.Equal("42", result);
    }

    [Fact]
    public async Task TestDelay()
    {
        await RestartInterpreter();

        string? result = await Execute(@"
            import Zen/Promise

            func main() {
                var t = time()
                await delay(100)
                var elapsed = time() - t
                print elapsed
            }

            run(main)
        ", true);

        int elapsed = int.Parse(result!);
        Assert.True(elapsed > 90 && elapsed < 110);
    }

    [Fact]
    public async void TestAsyncFunc()
    {
        await RestartInterpreter();

        string? result = await Execute(@"
            import Zen/Promise

            async func add(a: int, b: int): int {
                print ""adding "" + a + "" and "" + b
                return a + b
            }

            async func main() {
                print ""main... calling add(1, 2)""
                var result = await add(1, 2)
                print ""main result: "" + result
            }

            main()
        ", true);

        Output.WriteLine(Interpreter.GlobalOutputBuffer.ToString());

        //Assert.Equal("3", result);
    }

    // //----------- old tests below: -----------//
    // [Fact]
    // public async void TestAsyncFuncDeclaration() {
    //     await RestartInterpreter();
    //     await Execute("async func hello() {}");

    //     Assert.True(Interpreter.Environment.Exists("hello"));
        
    //     // get the value
    //     ZenValue hello = Interpreter.Environment.GetValue("hello");

    //     // make sure its callable
    //     Assert.True(hello.IsCallable());

    //     // is of type ZenFunction
    //     Assert.IsType<ZenFunction>(hello.Underlying);

    //     // get the ZenFunction
    //     ZenFunction function = (ZenFunction) hello.Underlying!;

    //     // takes 0 arguments
    //     Assert.Equal(0, function.Arity);

    //     // returns void
    //     Assert.Equal(ZenType.Void, function.ReturnType);

    //     // is async
    //     Assert.True(function.Async);
    // }

    // [Fact]
    // public async void TestAsyncFuncWithTaskReturn() {
    //     await RestartInterpreter();
    //     await Execute("async func hello(): Task<int> { return 42 }");

    //     Assert.True(Interpreter.Environment.Exists("hello"));
        
    //     // get the value
    //     ZenValue hello = Interpreter.Environment.GetValue("hello");

    //     // make sure its callable
    //     Assert.True(hello.IsCallable());

    //     // is of type ZenFunction
    //     Assert.IsType<ZenFunction>(hello.Underlying);

    //     // get the ZenFunction
    //     ZenFunction function = (ZenFunction) hello.Underlying!;

    //     // takes 0 arguments
    //     Assert.Equal(0, function.Arity);

    //     // returns Task<int>
    //     Assert.True(function.ReturnType.IsTask);
    //     Assert.Equal(ZenType.Integer, function.ReturnType.Parameters[0]);
    // }

    
    // [Fact]
    // public async void TestClassWithAsyncFunction() {
    //     await RestartInterpreter();
    //     await Execute(@"
    //         class Test {
    //             async hello(): string {
    //                 return ""hello""
    //             }
    //         }
    //     ");

    //     ZenClass testClass = Interpreter.Environment.GetClass("Test");
        
    //     Assert.Equal("Test", testClass.Name);
    //     Assert.Equal("hello", testClass.Methods.First().Name);
    //     Assert.True(testClass.Methods.First().Async);

    //     string? result = await Execute(@"
    //         async func main() {
    //             var t = new Test()
    //             var helloStr = await t.hello()
    //             print helloStr + "" world""
    //         }

    //         main()
    //     ", true);

    //     Assert.Equal("hello world", result);
    // }

    // [Fact]
    // public async void TestAwait()
    // {
    //     await RestartInterpreter();

    //     string? result = await Execute(@"
    //         async func test() {
    //             print ""before""
    //         }
    //         async func main() {
    //             await test()
    //             print ""after""
    //         }
    //         main()
    //     ", true);
    //     Assert.Equal("beforeafter", result);
    // }

    // [Fact]
    // public async void TestAsyncDelay() {
    //     await RestartInterpreter();
        
    //     string? result = await Execute(@"
    //         async func test() {
    //             var start = time()
    //             await delay(100)
    //             var elapsed = time() - start
    //             print elapsed
    //         }
    //         test()
    //     ", true);
    //     Output.WriteLine("test Execute() finished, result output:" + result);

    //     int elapsed = int.Parse(result!);

    //     Assert.True(elapsed > 95, "Expected a delay of at least 100ms but got " + elapsed);
    // }

    
    // [Fact]
    // public async void TestAsyncReturn()
    // {
    //     await RestartInterpreter();
        
    //     string? result = await Execute(@"
    //         async func test(): string {
    //             await delay(50)
    //             return ""done""
    //         }
    //         async func main() {
    //             print await test()
    //         }

    //         main()
    //     ", true);

    //     Assert.Equal("done", result);
    // }

    // [Fact]
    // public async void TestAsyncCallDotNet()
    // {
    //     await RestartInterpreter();
        
    //     string? result = await Execute(@"
    //         async func test(): int {
    //             # Wait asynchronously for 100ms
    //             var start = time()
    //             # use dot net task.delay
    //             await CallDotNetAsync(""System.Threading.Tasks.Task"", ""Delay"", 100)
    //             var end = time()
    //             var elapsed = end - start
    //             return (int) elapsed
    //         }

    //         async func main() {
    //             print await test()
    //         }

    //         main()
    //     ", true);

    //     int elapsed = int.Parse(result!);
        
    //     Assert.True(elapsed >= 100, "Expected a delay of at least 100ms but got " + elapsed);
    // }
}
