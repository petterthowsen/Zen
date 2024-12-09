using Xunit.Abstractions;
using Zen.Execution;
using Zen.Typing;

namespace Zen.Tests.Execution;

public class BinaryTests : TestRunner
{
    public BinaryTests(ITestOutputHelper output) : base(output) {}

    
    [Fact]
    public async void TestDivisionByZero() {
        await RestartInterpreter();

        var ex = await Assert.ThrowsAsync<RuntimeError>(() => Execute("print 10 / 0", true));
    }

    
    [Fact]
    public async void TestDivisionWithString() {
        await RestartInterpreter();

        var ex = await Assert.ThrowsAsync<RuntimeError>(() => Execute("print 10 / \"a\"", true));
    }

    [Fact]
    public async void TestAddition() {
        await RestartInterpreter();

        string? result = await Execute("print 1 + 1", true);
        Assert.Equal("2", result);
    }
    
    [Fact]
    public async void TestSubtraction() {
        await RestartInterpreter();

        string? result = await Execute("print 10 - 1", true);
        Assert.Equal("9", result);
    }

    [Fact]
    public async void TestMultiplication() {
        await RestartInterpreter();

        string? result = await Execute("print 10 * 2", true);
        Assert.Equal("20", result);
    }

    [Fact]
    public async void TestDivision() {
        await RestartInterpreter();

        string? result = await Execute("print 20 / 2", true);
        Assert.Equal("10", result);
    }

    [Fact]
    public async void TestGroupings() {
        await RestartInterpreter();

        string? result = await Execute("print (1 + 1) * 2", true);
        Assert.Equal("4", result);
    }
}