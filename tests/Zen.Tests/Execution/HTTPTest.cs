using Xunit.Abstractions;

namespace Zen.Tests.Execution;

public class HTTPTest : TestRunner
{
    public HTTPTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task TestServer() {
        await RestartInterpreter();
        
        await Execute(@"
            import Zen/Net/Http/HttpServer

            var server = new HttpServer()

            async func handler(method:string, url:string):string {
                return ""Hello, World!""
            }
            server.Listen(handler, ""127.0.0.1"", 3000)
        ");
    }
}