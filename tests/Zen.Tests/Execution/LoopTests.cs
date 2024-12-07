using Xunit.Abstractions;

namespace Zen.Tests.Execution;

public class LoopTests : TestRunner
{
    public LoopTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async void TestForLoop() {
        RestartInterpreter();

        string? result = await Execute("for i = 0; i < 2; i += 1 { print i }", true);
        Assert.Equal("01", result);
    }
}