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
    public async void TestBasicImport()
    {        
        // Execute Main.zen which imports and uses PrintHello
        var mainPath = Path.Combine(_projectPath, "Main.zen");
        var source = new FileSourceCode(mainPath);
        var result = await Execute(source);

        Assert.Equal("hello", result?.Trim());
    }

    [Fact]
    public async void TestClassImport()
    {
        // Execute ClassImport.zen which imports OOP/Point.zen class
        var mainPath = Path.Combine(_projectPath, "ClassImport.zen");
        var source = new FileSourceCode(mainPath);
        var result = await Execute(source);

        Assert.Equal("10", result?.Trim());
    }

    [Fact]
    public async void TestCyclicImport()
    {
        // Execute Cyclic.zen which imports cyclic/A which in turn depends on B, which depends on A cyclicly
        var path = Path.Combine(_projectPath, "Cyclic.zen");
        var source = new FileSourceCode(path);
        var result = await Execute(source);

        Assert.Equal("hello from Ahello from B", result);
    }

    [Fact]
    public async void TestImportClassModuleFromSystemPackage()
    {
        // Execute Main.zen which imports and uses PrintHello
        var result = await Execute(@"
            from System import Exception
            var e = new Exception(""test message"")
            print e.Message
        ");

        Assert.Equal("test message", result?.Trim());
    }

    [Fact]
    public async void TestImportModuleFromSystem_Time()
    {
        string? result = await Execute(@"
        from System/Time import CurrentTimeMillis
        print CurrentTimeMillis()
        ");
        
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async void TestAsyncImport()
    {    
        // Execute AsyncMain.zen which imports and uses DelayAndReturn
        var mainPath = Path.Combine(_projectPath, "AsyncMain.zen");
        var source = new FileSourceCode(mainPath);
        var result = await Execute(source);

        Assert.Equal("true", result?.Trim());
    }

    [Fact]
    public async void TestFromImport()
    {
        // Create and execute source that uses from-import syntax
        var source = @"
            from MyPackage/Utils import PrintHello
            PrintHello()
        ";
        var result = await Execute(source);

        Assert.Equal("hello", result?.Trim());
    }

    [Fact]
    public async void TestFromImportAsync()
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
        var result = await Execute(source);

        Assert.Equal("true", result?.Trim());
    }

    
}
