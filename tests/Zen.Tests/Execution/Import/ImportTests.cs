using Xunit.Abstractions;
using Zen.Common;
using Zen.Execution.Import.Providers;

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

        // Load the package which will register it with the FileSystemModuleProvider
        Importer.LoadPackage(_projectPath);
    }

    [Fact]
    public void TestBasicImport()
    {        
        // Execute Main.zen which imports and uses PrintHello
        var mainPath = Path.Combine(_projectPath, "Main.zen");
        var source = new FileSourceCode(mainPath);
        var result = Execute(source);

        Assert.Equal("hello", result?.Trim());
    }

    [Fact]
    public void TestAsyncImport()
    {    
        // Execute AsyncMain.zen which imports and uses DelayAndReturn
        var mainPath = Path.Combine(_projectPath, "AsyncMain.zen");
        var source = new FileSourceCode(mainPath);
        var result = Execute(source);

        Assert.Equal("true", result?.Trim());
    }

    [Fact]
    public void TestFromImport()
    {
        // Create and execute source that uses from-import syntax
        var source = @"
            from MyPackage/Utils import PrintHello
            PrintHello()
        ";
        var result = Execute(source);

        Assert.Equal("hello", result?.Trim());
    }

    [Fact]
    public void TestFromImportAsync()
    {        
        // Create and execute source that uses from-import syntax with async function
        var source = @"
            from MyPackage/Utils import DelayAndReturn

            async func test() {
                var elapsed = await DelayAndReturn(100)
                print elapsed >= 100
            }

            test()
        ";
        var result = Execute(source);

        Assert.Equal("true", result?.Trim());
    }

    [Fact]
    public void TestBuiltInImport()
    {
        // Execute Main.zen which imports and uses PrintHello
        var result = Execute(@"
            from System import Exception
            var e = new Exception(""test message"")
            print e.Message
        ");

        Assert.Equal("test message", result?.Trim());
    }
}
