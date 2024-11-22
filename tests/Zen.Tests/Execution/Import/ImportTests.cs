using Xunit.Abstractions;
using Zen.Common;

namespace Zen.Tests.Execution.Import;

public class ImportTests : TestRunner
{
    private readonly string _projectPath;

    public ImportTests(ITestOutputHelper output) : base(output)
    {
        // Get the directory containing the test class file
        var testClassPath = typeof(ImportTests).Assembly.Location;
        var testClassDir = Path.GetDirectoryName(testClassPath)!;
        
        // Navigate to the ZenProject directory relative to the test class location
        _projectPath = Path.GetFullPath(Path.Combine(
            testClassDir,
            "../../../Execution/Import/ZenProject"
        )).Replace("\\", "/");
    }

    [Fact]
    public void TestBasicImport()
    {
        // Load the package
        Importer.LoadPackage(_projectPath);

        // Execute Main.zen which imports and uses PrintHello
        var mainPath = Path.Combine(_projectPath, "Main.zen");
        var source = new FileSourceCode(mainPath);
        var result = Execute(source);

        Assert.Equal("hello", result?.Trim());
    }

    [Fact]
    public void TestAsyncImport()
    {
        // Load the package
        Importer.LoadPackage(_projectPath);

        // Execute AsyncMain.zen which imports and uses DelayAndReturn
        var mainPath = Path.Combine(_projectPath, "AsyncMain.zen");
        var source = new FileSourceCode(mainPath);
        var result = Execute(source);

        Assert.Equal("true", result?.Trim());
    }

    [Fact]
    public void TestFromImport()
    {
        // Load the package
        Importer.LoadPackage(_projectPath);

        // Create and execute source that uses from-import syntax
        var source = @"
            from MyPackage.Utils import PrintHello
            PrintHello()
        ";
        var result = Execute(source);

        Assert.Equal("hello", result?.Trim());
    }

    [Fact]
    public void TestFromImportAsync()
    {
        // Load the package
        Importer.LoadPackage(_projectPath);

        // Create and execute source that uses from-import syntax with async function
        var source = @"
            from MyPackage.Utils import DelayAndReturn

            async func test() {
                var elapsed = await DelayAndReturn(100)
                print elapsed >= 100
            }

            test()
        ";
        var result = Execute(source);

        Assert.Equal("true", result?.Trim());
    }
}
