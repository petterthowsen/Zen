using Xunit.Abstractions;
using Zen.Execution;
using Zen.Typing;

namespace Zen.Tests.Execution;

public class MapTests : TestRunner
{
    public MapTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task TestBasicFunctions()
    {
        await RestartInterpreter();

        string? result;
        
        await Execute(@"
            var map = new Map<string, int>()

            map.Set(""zero"", 0)
            map.Set(""one"", 1)
            map.Set(""two"", 2)
        ");
        
        // Get
        result = await Execute("print map.Get(\"two\")", true);
        Assert.Equal("2", result);

        // Contains value
        result = await Execute("print map.Contains(1)", true);
        Assert.Equal("true", result);

        // Has key
        result = await Execute("print map.Has(\"zero\")", true);
        Assert.Equal("true", result);

        // Count
        result = await Execute("print map.Count", true);
        Assert.Equal("3", result);

        // KeyOf
        result = await Execute("print map.KeyOf(1)", true);
        Assert.Equal("one", result);
    }

    
    [Fact]
    public async Task TestBracketGetSet()
    {
        await RestartInterpreter();

        string? result;
        
        await Execute(@"
            var map = new Map<string, int>()

            map[""zero""] = 0
            map[""one""] = 1
            map[""two""] = 2
        ");
        
        // Get
        result = await Execute("print map[\"zero\"]", true);
        Assert.Equal("0", result);

        result = await Execute("print map[\"one\"]", true);
        Assert.Equal("1", result);

        result = await Execute("print map[\"two\"]", true);
        Assert.Equal("2", result);
    }
    
    
    [Fact]
    public async Task TestForLoop()
    {
        await RestartInterpreter();

        string? result;
        
        await Execute(@"
            var map = new Map<string, int>()

            map[""zero""] = 0
            map[""one""] = 1
            map[""two""] = 2
        ");
        
        result = await Execute(@"
            for key, value in map {
                print key + ""="" + value + ""\n""
            }
        ", true);
        
        Assert.Equal("zero=0\none=1\ntwo=2\n", result);
    }

}