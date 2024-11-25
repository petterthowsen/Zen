using Zen.Exection.Import;
using Zen.Typing;

namespace Zen.Execution.Import;

/// <summary>
/// Represents a single .zen file that may have one or more exported symbols.
/// </summary>
public class Module 
{
    public string FullPath { get; }
    public string Name { get; }
    public Dictionary<string, Symbol> Symbols;
    private bool _isInitialized;
    
    public Module(string fullPath)
    {
        FullPath = fullPath;
        Name = FullPath.Split('/').Last();
    }
}