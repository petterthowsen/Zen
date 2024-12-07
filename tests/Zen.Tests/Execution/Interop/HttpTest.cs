using Xunit.Abstractions;

namespace Zen.Tests.Execution.Interop;

public class InteropTests : TestRunner
{
    public InteropTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async void TestHttp()
    {
        // TODO: use httpbin.org to test
        // RestartInterpreter();

        await Execute(@"
            import System/Http/HttpServer
            var server = new HttpServer(""127.0.0.1"", 3000)
            func handler(method:string, url:string):string {
                return ""You sent a "" + method + "" request to "" + url
            }

            server.Listen(handler)
        ");
    }
}