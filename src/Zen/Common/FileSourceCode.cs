namespace Zen.Common;

using System.IO;

public class FileSourceCode : AbstractSourceCode {

    public string FilePath;

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