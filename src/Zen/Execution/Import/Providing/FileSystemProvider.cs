using Zen.Common;

namespace Zen.Execution.Import.Providing;

/// <summary>
/// Searches one or more directories for Packages and their namespaces and modules.
/// </summary>
public class FileSystemProvider : AbstractProvider
{

    public string[] PackageDirectories { get; private set; }

    public FileSystemProvider(string[] packageDirectories) {
        PackageDirectories = packageDirectories;
    }

    protected override Package? FindPackage(string name)
    {
        foreach (var root in PackageDirectories) {
            var path = Path.Combine(root, name);
            if (Directory.Exists(path)) {
                return new Package(name, path);
            }
        }

        return null;
    }

    protected override Namespace? FindNamespace(string fullPath)
    {
        foreach (var root in PackageDirectories) {
            var path = Path.Combine(root, fullPath);
            if (Directory.Exists(path)) {
                return new Namespace(fullPath);
            }
        }

        return null;
    }

    protected override Module? FindModule(string fullPath)
    {
        foreach (var root in PackageDirectories) {
            var path = Path.Combine(root, fullPath + ".zen");
            if (File.Exists(path)) {
                return new Module(fullPath, new FileSourceCode(path));
            }
        }

        return null;
    }

}