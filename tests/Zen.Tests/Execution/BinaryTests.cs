using Xunit.Abstractions;
using Zen.Typing;

namespace Zen.Tests.Execution;
//TODO: test complex expressions with multiple groupings
public class BinaryTests : TestRunner
{
    public BinaryTests(ITestOutputHelper output) : base(output) {}

    [Fact]
    public void TestAddition() {
        RestartInterpreter();

        string? result = Execute("print 1 + 1");
        Assert.Equal("2", result);
    }
    
    [Fact]
    public void TestSubtraction() {
        RestartInterpreter();

        string? result = Execute("print 10 - 1");
        Assert.Equal("9", result);
    }

    [Fact]
    public void TestMultiplication() {
        RestartInterpreter();

        string? result = Execute("print 10 * 2");
        Assert.Equal("20", result);
    }

    [Fact]
    public void TestDivision() {
        RestartInterpreter();

        string? result = Execute("print 20 / 2");
        Assert.Equal("10", result);
    }

    [Fact]
    public void TestGroupings() {
        RestartInterpreter();

        string? result = Execute("print (1 + 1) * 2");
        Assert.Equal("4", result);
    }
}