using Xunit.Abstractions;

namespace Zen.Tests.Execution;

public class LoopTests : TestRunner
{
    public LoopTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void TestForLoop() {
        RestartInterpreter();

        string? result = Execute("for i = 0; i < 2; i += 1 { print i }");
        Assert.Equal("01", result);
    }
}