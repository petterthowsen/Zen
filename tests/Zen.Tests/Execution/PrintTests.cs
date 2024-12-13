using Xunit.Abstractions;

namespace Zen.Tests.Execution;

public class PrintTests : TestRunner
{
    public PrintTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async void TestPrint() {
        await RestartInterpreter();

        string? result = await Execute("print \"hello world\"", true);
        Assert.Equal("hello world", result);
    }
    
    [Fact]
    public async void TestPrintNewline() {
        await RestartInterpreter();
        
        string? result = await Execute("print \"hello\\nworld\"", true);
        Assert.Equal("hello\nworld", result);
    }
    
}