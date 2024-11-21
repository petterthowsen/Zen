using Xunit.Abstractions;

namespace Zen.Tests.Execution;

public class PrintTests : TestRunner
{
    public PrintTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void TestPrint() {
        RestartInterpreter();

        string? result = Execute("print \"hello world\"");
        Assert.Equal("hello world", result);
    }
    
    [Fact]
    public void TestPrintNewline() {
        RestartInterpreter();
        
        string? result = Execute("print \"hello\\nworld\"");
        Assert.Equal("hello\nworld", result);
    }
    
}