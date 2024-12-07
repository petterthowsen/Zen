using Xunit.Abstractions;
using Zen.Typing;

namespace Zen.Tests.Execution;
//TODO: test complex expressions with multiple groupings
public class BinaryTests : TestRunner
{
    public BinaryTests(ITestOutputHelper output) : base(output) {}

    [Fact]
    public async void TestAddition() {
        RestartInterpreter();

        string? result = await Execute("print 1 + 1", true);
        Assert.Equal("2", result);
    }
    
    [Fact]
    public async void TestSubtraction() {
        RestartInterpreter();

        string? result = await Execute("print 10 - 1", true);
        Assert.Equal("9", result);
    }

    [Fact]
    public async void TestMultiplication() {
        RestartInterpreter();

        string? result = await Execute("print 10 * 2", true);
        Assert.Equal("20", result);
    }

    [Fact]
    public async void TestDivision() {
        RestartInterpreter();

        string? result = await Execute("print 20 / 2", true);
        Assert.Equal("10", result);
    }

    [Fact]
    public async void TestGroupings() {
        RestartInterpreter();

        string? result = await Execute("print (1 + 1) * 2", true);
        Assert.Equal("4", result);
    }
}