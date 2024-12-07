using Xunit;
using Xunit.Abstractions;

namespace Zen.Tests.Execution;

public class TypeTests : TestRunner
{
    public TypeTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async void TestValueIsType()
    {
        RestartInterpreter();

        string? result;

        await Execute("var a = 5");
        await Execute("var b = null");

        result = await Execute("print a is int", true);
        Assert.Equal("true", result);

        // result = Execute("print a is int?");
        // Assert.Equal("true", result);

        // result = Execute("print b is int?");
        // Assert.Equal("true", result);

        result = await Execute("print b is int", true);
        Assert.Equal("false", result);
    }

    [Fact]
    public async void TestTypeCheckClass() {
        RestartInterpreter();

        await Execute("class Point { x: int y: int }");
        await Execute("var p = new Point()");
        string? result = await Execute("print p is Point", true);
        Assert.Equal("true", result);
    }

    [Fact]
    public async void TestTypeCast() {
        RestartInterpreter();

        await Execute("var pi = 3.14");
        await Execute("var intPi = (int) pi");
        string? result = await Execute("print intPi", true);
        Assert.Equal("3", result);
    }

}
