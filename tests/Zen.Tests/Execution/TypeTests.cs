using Xunit;
using Xunit.Abstractions;

namespace Zen.Tests.Execution;

public class TypeTests : TestRunner
{
    public TypeTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void TestValueIsType()
    {
        RestartInterpreter();

        Execute("var a = 5");
        Execute("var b = null");

        string? result = Execute("print a is int");
        Assert.Equal("true", result);

        result = Execute("print a is int?");
        Assert.Equal("true", result);

        result = Execute("print b is int?");
        Assert.Equal("true", result);

        result = Execute("print b is int");
        Assert.Equal("false", result);
    }

    [Fact]
    public void TestTypeCast() {
        RestartInterpreter();

        Execute("var pi = 3.14");
        Execute("var intPi = (int) pi");
        string? result = Execute("print intPi");
        Assert.Equal("3", result);
    }

    [Fact]
    public void TestTypeCastClass() {
        RestartInterpreter();

        Execute("class Point { x: int y: int }");
        Execute("var p = new Point()");
        string? result = Execute("print p is Point");
        Assert.Equal("true", result);
    }
}
