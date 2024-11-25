namespace Zen.Execution.Import;

public interface IHasModules
{

    Dictionary<string, Module> Modules { get; }
    void AddModule(string name, Module module);
}