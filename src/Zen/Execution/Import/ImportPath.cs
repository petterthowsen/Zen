using System.Diagnostics.CodeAnalysis;

namespace Zen.Execution.Import;

/// <summary>
/// Represents a path to a module or namespace in the import system.
/// </summary>
public struct ImportPath
{

    public readonly string Path;

    public readonly string[] Segments;

    public readonly string PackageName;

    /// <summary>
    /// The last segment of the path. Could be a module or a namespace.
    /// </summary>
    public readonly string Last => Segments[Segments.Length - 1];

    public ImportPath(string path) {
        Path = path.Trim().TrimEnd('/');
        Segments = path.Split('/');
        PackageName = Segments[0];
    }

    public bool IsPackageOnly => Segments.Length == 1;

    /// <summary>
    /// Get the path to the module or namespace without the package name.
    /// </summary>
    /// <returns></returns>
    public string GetModulePath() {
        if (Segments.Length == 1) {
            return Path;
        }
        string[] moduleSegments = Segments[1..];
        return string.Join("/", moduleSegments);
    }

    public override string ToString()
    {
        return Path;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null) return false;
        if (obj is not ImportPath) return false;
        return Path == ((ImportPath)obj).Path;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Path, Segments, PackageName);
    }

}