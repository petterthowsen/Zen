namespace Zen.Execution.Import;

public interface IHasNamespaces
{
    Dictionary<string, Namespace> Namespaces { get; }
    void AddNamespace(Namespace @namespace);
}