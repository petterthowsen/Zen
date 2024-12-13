using Xunit.Abstractions;
using Zen.Typing;

namespace Zen.Tests.Execution;

public class StringTests : TestRunner
{
    public StringTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async void StringMethodTest() {
        await RestartInterpreter();

        string? result;
        
        await Execute("var name = \"john\"");

        result = await Execute("print name.ToUpper()", true);

        Assert.Equal("JOHN", result);
    }

    [Fact]
    public async void StringConcatTest()
    {
        await RestartInterpreter();

        string? result;

        result = await Execute("print \"hello\" + \" world\"", true);
        Assert.Equal("hello world", result);
    }

    [Fact]
    public async void StringLengthTest()
    {
        await RestartInterpreter();

        string? result;

        result = await Execute(@"
            var name = ""john""
            print name.Length
        ",true);
        
        Assert.Equal("4", result);
    }

    [Fact]
    public async void StringSplitTest()
    {
        await RestartInterpreter();

        string? result = await Execute(@"
            var name = ""john doe""
            var parts = name.Split("" "")

            print parts
        ", true);

        Assert.Equal("[john, doe]", result);
    }

    [Fact]
    public async void StringSplitAndJoinTest()
    {
        await RestartInterpreter();

        string? result = await Execute(@"
            var name = ""john doe""
            var parts = name.Split("" "")
            var joined = parts.Join(""-"")

            print joined    
        ", true);

        Assert.Equal("john-doe", result);
    }

}