using Xunit.Abstractions;
using Zen.Typing;

namespace Zen.Tests.Execution;

public class StringTests : TestRunner
{
    public StringTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async void StringToUpperTest() {
        await RestartInterpreter();

        string? result = await Execute(@"
            var name = ""john""
            print name.ToUpper()
        ", true);

        Assert.Equal("JOHN", result);
    }
    
    
    [Fact]
    public async void StringToLowerTest() {
        await RestartInterpreter();

        string? result = await Execute(@"
            var name = ""JOHN""
            print name.ToLower()
        ", true);

        Assert.Equal("john", result);
    }
    
    
    [Fact]
    public async void StringRepeatTest() {
        await RestartInterpreter();

        string? result = await Execute("print \"hello\".Repeat(3)", true);
        Assert.Equal("hellohellohello", result);
    }

    [Fact]
    public async void StringStartsWithTest()
    {
        await RestartInterpreter();

        string? result1 = await Execute(@"
            var name = ""john doe""
            print name.StartsWith(""john"")
        ", true);
        Assert.Equal("true", result1);

        string? result2 = await Execute(@"
            name = ""john doe""
            print name.StartsWith(""doe"")
        ", true);
        Assert.Equal("false", result2);
    }

    
    [Fact]
    public async void StringEndsWithTest()
    {
        await RestartInterpreter();

        string? result1 = await Execute(@"
            var name = ""john doe""
            print name.EndsWith(""doe"")
        ", true);
        Assert.Equal("true", result1);

        string? result2 = await Execute(@"
            name = ""john doe""
            print name.EndsWith(""john"")
        ", true);
        Assert.Equal("false", result2);
    }

    
    [Fact]
    public async void StringTrimTest()
    {
        await RestartInterpreter();

        string? result1 = await Execute(@"
            var name = ""  john doe  ""
            print name.Trim()
        ", true);
        Assert.Equal("john doe", result1);

        string? result2 = await Execute(@"
            name = ""  john doe  ""
            print name.TrimEnd()
        ", true);
        Assert.Equal("  john doe", result2);

        string? result3 = await Execute(@"
            name = ""  john doe  ""
            print name.TrimStart()
        ", true);
        Assert.Equal("john doe  ", result3);
    }

    
    [Fact]
    public async void StringTrimWithArgsTest()
    {
        await RestartInterpreter();

        string? result = await Execute(@"
            var name = ""__-john_doe-_""
            print name.Trim(""-_"")
        ", true);
        Assert.Equal("john_doe", result);
    }

    [Fact]
    public async void StringTrimStart()
    {
        await RestartInterpreter();

        string? result = await Execute(@"
            var name = ""  john doe  ""
            print name.TrimStart()
        ", true);
        Assert.Equal("john doe  ", result);
    }

    [Fact]
    public async void StringTrimEnd()
    {
        await RestartInterpreter();

        string? result = await Execute(@"
            var name = ""  john doe  ""
            print name.TrimEnd()
        ", true);
        Assert.Equal("  john doe", result);
    }

    
    [Fact]
    public async void StringIndexOfTest()
    {
        await RestartInterpreter();
        
        string ?result = await Execute(@"
            var name = ""john doe""
            print name.IndexOf(""n"")
        ", true);
        Assert.Equal("3", result);

        result = await Execute(@"
            name = ""john doe""
            print name.IndexOf(""e"")
        ", true);
        Assert.Equal("7", result);

        result = await Execute(@"
            name = ""john doe""
            print name.IndexOf(""z"")
        ", true);
        Assert.Equal("-1", result);
    }

    
    [Fact]
    public async void StringContainsTest()
    {
        await RestartInterpreter();

        string? result = await Execute(@"
            var name = ""john doe""
            print name.Contains(""doe"")
        ", true);
        Assert.Equal("true", result);
    }

    [Fact]
    public async void StringReverseTest()
    {
        await RestartInterpreter();

        string? result = await Execute(@"
            var name = ""abc""
            print name.Reverse()
        ", true);

        Assert.Equal("cba", result);
    }
    
    [Fact]
    public async void StringReplaceTest()
    {
        await RestartInterpreter();

        string? result = await Execute(@"
            var name = ""john doe""
            print name.Replace(""john"", ""jane"")
        ", true);
        Assert.Equal("jane doe", result);

        result = await Execute(@"
            name = ""john doe""
            print name.Replace(""doe"", ""smith"")
        ", true);
        Assert.Equal("john smith", result);

        result = await Execute(@"
            name = ""john doe""
            print name.Replace(""x"", ""y"")
        ", true);
        Assert.Equal("john doe", result);
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