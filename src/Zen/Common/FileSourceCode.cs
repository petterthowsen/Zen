namespace Zen.Common;

using System.IO;

class FileSourceCode : AbstractSourceCode {

    public required string FilePath { get; init; }

    private static string ReadFile(string filePath) {
        return File.ReadAllText(filePath);
    }

    public FileSourceCode(string filePath) : base(ReadFile(filePath)) {
        FilePath = filePath;
    }

    public override string ToString()
    {
        return $"{FilePath}";
    }
}