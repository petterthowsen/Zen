using Xunit.Abstractions;

namespace Zen.Tests.Execution;

public class ExceptionTests : TestRunner
{
    public ExceptionTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async void TestTryCatch() {
        await RestartInterpreter();

        string? result = await Execute(@"
            try {
                throw new Exception(""oops!"")
            } catch ex: Exception {
                print ex.Message
            }
        ", true);
        Assert.Equal("oops!", result);

    }

    [Fact]
    public async void TestThrowStringLiteral()
    {
        await RestartInterpreter();
        string? result = await Execute(@"
            try {
                throw ""oops""
            } catch ex: Exception {
                print ex.Message
            }
        ", true);

        Assert.Equal("oops", result);
    }

    [Fact]
    public async void TestCatchInternalException()
    {
        await RestartInterpreter();

        string? result = await Execute(@"
            try {
                var a = 1 / 0
            } catch ex: Exception {
                print ex.Message
            }
        ", true);

        Assert.Equal("oops!", result);
    }

}