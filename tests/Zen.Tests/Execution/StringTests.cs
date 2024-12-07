using Xunit.Abstractions;

namespace Zen.Tests.Execution;

public class StringTests : TestRunner
{
    public StringTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async void StringMethodTest() {
        RestartInterpreter();

        string? result;
        
        await Execute("var name = \"john\"");

        result = await Execute("print name.toUpper()", true);

        Assert.Equal("JOHN", result);
    }

    [Fact]
    public async void StringConcatTest()
    {
        RestartInterpreter();

        string? result;

        result = await Execute("print \"hello\" + \" world\"", true);
        Assert.Equal("hello world", result);
    }

}