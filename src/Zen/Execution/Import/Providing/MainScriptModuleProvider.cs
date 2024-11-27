using Zen.Common;

namespace Zen.Execution.Import.Providing;

/// <summary>
/// Provides modules from the current Main Script module.
/// </summary>
public class MainScriptModuleProvider : AbstractProvider
{
    private readonly Module Main;
    private readonly Package Package;

    private string? Root;

    public MainScriptModuleProvider(Module mainScriptModule, string packageName)
    {
        Main = mainScriptModule;
        
        if (Main.Source is FileSourceCode fs) {
            // Get the script's directory first, then get its parent
            Root = Path.GetDirectoryName(fs.FilePath);
            
            Logger.Instance.Debug($"MainScriptModuleProvider initialized with Root: {Root}");
        }

        // Create a package to represent the current module's context
        Package = new Package(packageName, packageName);
        Package.AddModule(Main.Name, Main);
        Logger.Instance.Debug($"Created package: {packageName} with module: {Main.Name}");
    }

    protected override Package? FindPackage(string name)
    {
        // Only return our package if the name matches
        Logger.Instance.Debug($"FindPackage called with name: {name}, our package is: {Package.Name}");
        return name == Package.Name ? Package : null;
    }

    protected override Namespace? FindNamespace(string fullPath)
    {
        string[] subPathArray = fullPath.Split("/")[1..];
        string subPath = string.Join("/", subPathArray);

        if (Root == null)
        {
            Logger.Instance.Debug($"FindNamespace: Root is null, cannot find namespace: {fullPath}");
            return null;
        }

        var path = Path.Combine(Root, subPath);
        Logger.Instance.Debug($"FindNamespace: Checking directory: {path}");
        
        var exists = Directory.Exists(path);
        Logger.Instance.Debug($"FindNamespace: Directory.Exists returned: {exists} for path: {path}");

        if (exists)
        {
            return new Namespace(fullPath);
        }

        return null;
    }

    protected override Module? FindModule(string fullPath)
    {
        if (Root == null)
        {
            Logger.Instance.Debug($"FindModule: Root is null, cannot find module: {fullPath}");
            return null;
        }

        string[] subPathArray = fullPath.Split("/")[1..];
        string subPath = string.Join("/", subPathArray);

        var path = Path.Combine(Root, subPath + ".zen");
        Logger.Instance.Debug($"FindModule: Checking file: {path}");
        
        var exists = File.Exists(path);
        Logger.Instance.Debug($"FindModule: File.Exists returned: {exists} for path: {path}");

        if (exists)
        {
            return new Module(fullPath, new FileSourceCode(path));
        }

        return null;
    }
}
