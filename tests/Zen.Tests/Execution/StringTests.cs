using Xunit.Abstractions;

namespace Zen.Tests.Execution;

public class StringTests : TestRunner
{
    public StringTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void StringMethodTest() {
        RestartInterpreter();

        string? result;
        
        Execute("var name = \"john\"");

        result = Execute("print name.toUpper()");

        Assert.Equal("JOHN", result);
    }

    [Fact]
    public void StringConcatTest()
    {
        RestartInterpreter();

        string? result;

        result = Execute("print \"hello\" + \" world\"");
        Assert.Equal("hello world", result);
    }

}