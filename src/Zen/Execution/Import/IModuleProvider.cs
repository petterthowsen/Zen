using Zen.Common;

namespace Zen.Execution.Import;

/// <summary>
/// Represents a source of Zen modules, such as the filesystem, embedded resources, or system packages.
/// </summary>
public interface IModuleProvider
{
    /// <summary>
    /// Gets the priority of this provider. Higher priority providers are checked first.
    /// Standard library should have highest priority, followed by system packages, then filesystem.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Checks if this provider can handle the given module path.
    /// </summary>
    /// <param name="modulePath">The module path (e.g. "System/Exception")</param>
    /// <returns>True if this provider can provide the module</returns>
    bool CanProvide(string modulePath);

    /// <summary>
    /// Gets the source code for a module.
    /// </summary>
    /// <param name="modulePath">The module path (e.g. "System/Exception")</param>
    /// <returns>The source code for the module</returns>
    /// <exception cref="RuntimeError">If the module cannot be found or loaded</exception>
    ISourceCode GetModuleSource(string modulePath);

    /// <summary>
    /// Lists available modules in a directory.
    /// </summary>
    /// <param name="directoryPath">The directory path (e.g. "System")</param>
    /// <returns>A list of module paths relative to the directory</returns>
    IEnumerable<string> ListModules(string directoryPath);
}
