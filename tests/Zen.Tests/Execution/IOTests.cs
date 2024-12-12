using System.IO;
using Xunit.Abstractions;

namespace Zen.Tests.Execution;

public class IOTests : TestRunner, IDisposable
{
    private readonly string testDir;

    public IOTests(ITestOutputHelper output) : base(output)
    {
        testDir = Path.Combine(Path.GetTempPath(), "ZenIOTests");
        if (Directory.Exists(testDir))
        {
            Directory.Delete(testDir, true);
        }
        Directory.CreateDirectory(testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(testDir))
        {
            Directory.Delete(testDir, true);
        }
    }

    [Fact]
    public async Task TestFileWriteAndRead()
    {
        await RestartInterpreter();

        var testFile = Path.Combine(testDir, "test.txt").Replace("\\", "\\\\");
        var result = await Execute($@"
            import Zen/IO/File

            async func main() {{
                var file = await File.Open(""{testFile}"", ""w"")
                await file.WriteText(""Hello, World!"")
                file.Close()

                file = await File.Open(""{testFile}"", ""r"")
                var content = await file.ReadText()
                print content
            }}

            main()
        ", true);

        Assert.Equal("Hello, World!", result?.Trim());
        Assert.True(File.Exists(testFile));
    }

    [Fact]
    public async Task TestFileLines()
    {
        await RestartInterpreter();

        var testFile = Path.Combine(testDir, "lines.txt").Replace("\\", "\\\\");
        var result = await Execute($@"
            import Zen/IO/File

            async func main() {{
                with file = await File.Open(""{testFile}"", ""w"") {{
                    await file.WriteLine(""Line 1"")
                    await file.WriteLine(""Line 2"")
                    await file.WriteLine(""Line 3"")
                }}

                with file = await File.Open(""{testFile}"") {{
                    var lines = await file.ReadLines()
                    print lines.Length
                    for line in lines {{
                        print line
                    }}
                }}
            }}

            main()
        ", true);

        var lines = result?.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.NotNull(lines);
        Assert.Equal("3", lines[0]);
        Assert.Equal("Line 1", lines[1]);
        Assert.Equal("Line 2", lines[2]);
        Assert.Equal("Line 3", lines[3]);
    }

    [Fact]
    public async Task TestBinaryOperations()
    {
        await RestartInterpreter();

        var testFile = Path.Combine(testDir, "binary.dat").Replace("\\", "\\\\");
        var result = await Execute($@"
            import Zen/IO/File

            async func main() {{
                with file = await File.Open(""{testFile}"", ""w"") {{
                    await file.WriteBytes([65, 66, 67])  # ABC in ASCII
                }}

                with file = await File.Open(""{testFile}"") {{
                    var bytes = await file.ReadBytes(3)
                    print bytes.Length
                    for byte in bytes {{
                        print byte
                    }}
                }}
            }}

            main()
        ", true);

        var lines = result?.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.NotNull(lines);
        Assert.Equal("3", lines[0]);
        Assert.Equal("65", lines[1]); // A
        Assert.Equal("66", lines[2]); // B
        Assert.Equal("67", lines[3]); // C
    }

    [Fact]
    public async Task TestFileInfo()
    {
        await RestartInterpreter();

        var testFile = Path.Combine(testDir, "info.txt").Replace("\\", "\\\\");
        File.WriteAllText(testFile, "Test content");

        var result = await Execute($@"
            import Zen/IO/FileInfo

            async func main() {{
                var info = FileInfo.GetInfo(""{testFile}"")
                print info.Name
                print info.Length
                print info.Exists
            }}

            main()
        ", true);

        var lines = result?.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.NotNull(lines);
        Assert.Equal("info.txt", lines[0]);
        Assert.Equal("12", lines[1]); // "Test content" length
        Assert.Equal("true", lines[2]);
    }

    [Fact]
    public async Task TestDirectory()
    {
        await RestartInterpreter();

        var subDir = Path.Combine(testDir, "subdir").Replace("\\", "\\\\");
        var result = await Execute($@"
            import Zen/IO/Directory

            async func main() {{
                await Directory.Create(""{subDir}"")
                print Directory.Exists(""{subDir}"")
                
                with file = await File.Open(""{subDir}\\\\test.txt"", ""w"") {{
                    await file.WriteText(""test"")
                }}

                var files = Directory.GetFiles(""{subDir}"")
                print files.Length
                print files[0].EndsWith(""test.txt"")

                await Directory.Delete(""{subDir}"", true)
                print Directory.Exists(""{subDir}"")
            }}

            main()
        ", true);

        var lines = result?.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.NotNull(lines);
        Assert.Equal("true", lines[0]);  // Directory exists after creation
        Assert.Equal("1", lines[1]);     // One file in directory
        Assert.Equal("true", lines[2]);  // File ends with test.txt
        Assert.Equal("false", lines[3]); // Directory doesn't exist after deletion
    }
}
