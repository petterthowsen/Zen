using Xunit.Abstractions;
using Zen.Common;

namespace Zen.Tests.Execution;

public class ComplexTests : TestRunner
{
    public ComplexTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task TestZenExpress() {
        await RestartInterpreter();
        var script = "/home/pelatho/Documents/work/zen-projects/ZenExpress/Test.zen";

        await Execute(new FileSourceCode(script), false);
    }

}