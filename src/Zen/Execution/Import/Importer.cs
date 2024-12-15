using Zen.Exection.Import;
using Zen.Execution.Import.Providing;
using Zen.Parsing.AST.Statements;
using Zen.Common;
using Zen.Typing;
using Zen.Execution.EvaluationResult;

namespace Zen.Execution.Import;

/// <summary>
/// Manages the import system for the Zen runtime.
/// It can resolve names and nested names to namespaces, modules, and symbols.
/// It handles the execution/initialization of modules and provides their symbols.
/// </summary>
/// 
/// 
/// TODO: There's currently a bug where if a module imports something using 'from X import Y', only X will be resolved (but not Y)
/// 

public class Importer 
{
    public List<AbstractProvider> Providers { get; private set; } = [];
    private readonly ModuleHelper _moduleHelper;
    private readonly Interpreter _interpreter;
    private readonly Resolver _resolver;

    // Cache of resolved modules to prevent duplicate processing
    private Dictionary<string, Module> _moduleCache = [];

    private static string[] ModuleLocals = [
        "ZEN_THIS_FILE",
    ];

    public Importer(Interpreter interpreter, Resolver resolver)
    {
        _interpreter = interpreter;
        _resolver = resolver;
        _moduleHelper = new ModuleHelper(interpreter);
    }

    public void RegisterProvider(AbstractProvider provider)
    {
        Logger.Instance.Debug($"Registering provider: {provider.GetType().Name}");
        Providers.Add(provider);
    }

    /// <summary>
    /// Phase 1: Resolves symbols without executing modules
    /// </summary>
    public ImportResolution ResolveSymbols(string path, bool global = false)
    {
        Logger.Instance.Debug($"[Importer] ResolveSymbols, path: {path}");

        // Check cache first
        if (_moduleCache.TryGetValue(path, out var cachedModule))
        {
            Logger.Instance.Debug($"Found in cache: {path}");
            return new ModuleResolution(path, cachedModule);
        }

        // Try to resolve through providers
        ImportResolution? resolution = null;
        foreach (var provider in Providers) 
        {
            resolution = provider.Resolve(path);
            if (resolution != null)
            {
                break;
            }
        }

        if (resolution == null)
        {
            throw new Exception($"Cannot resolve {path}.");
        }

        // If this is a module, process it
        if (resolution.IsModule())
        {
            var module = resolution.AsModule().Module;
            ProcessModule(module, global);
        }

        return resolution;
    }

    /// <summary>
    /// Phase 2: Executes modules, supporting circular dependencies
    /// </summary>
    public async Task<ImportResolution> Import(string path, bool global = false)
    {
        Logger.Instance.Debug($"Importing path: {path} (global: {global})");
        var resolution = ResolveSymbols(path, global);
        
        if (resolution.IsModule())
        {
            var module = resolution.AsModule().Module;
            await ExecuteModule(module);
        }

        return resolution;
    }

    /// <summary>
    /// Process a module through parsing and import resolution
    /// Handles circular dependencies by allowing partial processing
    /// </summary>
    private void ProcessModule(Module module, bool global = false)
    {
        Logger.Instance.Debug($"Processing module: {module.FullPath} (State: {module.State})");

        // If module is already fully processed, return
        if (module.State == State.ImportsResolved || module.State == State.Executing || module.State == State.Executed)
        {
            Logger.Instance.Debug($"Module already processed: {module.FullPath}");
            return;
        }

        // If module is being parsed or resolving imports, we've hit a circular dependency
        // This is okay - we'll continue with the partial state
        if (module.State == State.Parsing || module.State == State.ParseComplete || module.State == State.ResolvingImports)
        {
            Logger.Instance.Debug($"Circular dependency detected for module: {module.FullPath}");
            return;
        }

        // Parse if not already parsed
        if (module.State == State.NotLoaded)
        {
            Logger.Instance.Debug($"Parsing module: {module.FullPath}");
            _moduleHelper.Parse(module);
        }

        // Declare types if needed
        if (module.State == State.ParseComplete)
        {
            if (module.AST == null)
            {
                throw new Exception("Module AST is null after parsing");
            }

            module.State = State.DeclaringTypes;
            Logger.Instance.Debug($"Declaring types for module: {module.FullPath}");

            // Set up module environment
            if (global) {
                module.environment = _interpreter.globalEnvironment;
            }else {
                module.environment = new Environment(_interpreter.globalEnvironment, "module " + module.FullPath);
            }

            // First pass: Declare all class types with empty implementations
            foreach (var stmt in module.AST.Statements)
            {
                if (stmt is ClassStmt classStmt)
                {
                    // Just define the class name and create an empty class
                    var className = classStmt.Identifier.Value;
                    Logger.Instance.Debug($"Declaring class: {className}");
                    
                    module.environment.Define(true, className, ZenType.Class, false);
                    var emptyClass = new ZenClass(className, [], [], []);
                    module.environment.Assign(className, new ZenValue(ZenType.Class, emptyClass));
                }
                else if (stmt is InterfaceStmt interfaceStmt) {
                    var interfaceName = interfaceStmt.Identifier.Value;
                    Logger.Instance.Debug($"Declaring interface: {interfaceName}");
                    
                    module.environment.Define(true, interfaceName, ZenType.Interface, false);
                    var emptyInterface = new ZenInterface(interfaceName, [], []);
                    module.environment.Assign(interfaceName, new ZenValue(ZenType.Interface, emptyInterface));
                }
                else if (stmt is FuncStmt funcStmt)
                {
                    var funcName = funcStmt.Identifier.Value;
                    Logger.Instance.Debug($"Declaring func: {funcName}");

                    module.environment.Define(true, funcName, ZenType.Function, false);
                    var emptyFunc = ZenFunction.NewUserFunction(ZenType.Void, [], null, null, funcStmt.Async, false);
                    module.environment.Assign(funcName, new ZenValue(ZenType.Function, emptyFunc));
                }
            }
        }

        // Process imports if needed
        if (module.State == State.DeclaringTypes)
        {
            if (module.AST == null)
            {
                throw new Exception("Module AST is null");
            }

            module.State = State.ResolvingImports;
            Logger.Instance.Debug($"Resolving imports for module: {module.FullPath}");

            // Find and process all import statements
            foreach (var stmt in module.AST.Statements)
            {
                if (stmt is ImportStmt importStmt)
                {
                    Logger.Instance.Debug($"Processing import statement: {importStmt.PathString}");
                    
                    // Check for self-import
                    if (importStmt.PathString == module.FullPath)
                    {
                        throw new Exception($"Module cannot import itself: {module.FullPath}");
                    }

                    var importResolution = ResolveSymbols(importStmt.PathString);
                    module.AddLocalImport(importStmt.PathString, importResolution);
                    
                    if (importResolution.IsModule())
                    {
                        module.AddDependency(importResolution.AsModule().Module);
                    }
                }
                else if (stmt is FromImportStmt fromImportStmt)
                {
                    Logger.Instance.Debug($"Processing from-import statement: {fromImportStmt.PathString}");
                    
                    // Check for self-import
                    if (fromImportStmt.PathString == module.FullPath)
                    {
                        throw new Exception($"Module cannot import from itself: {module.FullPath}");
                    }

                    var importResolution = ResolveSymbols(fromImportStmt.PathString);
                    module.AddLocalImport(fromImportStmt.PathString, importResolution);
                    
                    if (importResolution.IsModule())
                    {
                        module.AddDependency(importResolution.AsModule().Module);
                    }
                }
            }

            // Resolve variables in the module's environment
            var oldEnv = _interpreter.Environment;
            try {
                _interpreter.Environment = module.environment;
                _resolver.Resolve(module.AST, global: global, ModuleLocals);
            } finally {
                _interpreter.Environment = oldEnv;
            }

            module.State = State.ImportsResolved;
            _moduleCache[module.FullPath] = module;
            Logger.Instance.Debug($"Module processing complete, adding to cache: {module.FullPath}");
        }
    }

    /// <summary>
    /// Execute a module, supporting circular dependencies through lazy initialization
    /// </summary>
    private async Task ExecuteModule(Module module)
    {
        // If already executed, nothing to do
        if (module.State == State.Executed)
        {
            Logger.Instance.Debug($"Module already executed: {module.FullPath}");
            return;
        }

        // If currently executing, we have a circular dependency
        // This is fine - the module's environment is already set up
        if (module.State == State.Executing)
        {
            Logger.Instance.Debug($"Module is currently executing: {module.FullPath}");
            return;
        }

        // Must have resolved imports before executing
        if (module.State != State.ImportsResolved)
        {
            throw new Exception($"Cannot execute module in state {module.State}");
        }

        // Set up module environment and mark as executing
        module.State = State.Executing;

        Logger.Instance.Debug($"Executing module: {module.FullPath} (State: {module.State})...");

        // Execute dependencies - they may reference back to this module
        foreach (var dependency in module.Dependencies)
        {
            Logger.Instance.Debug($"Executing dependency {dependency.FullPath} for module {module.FullPath}");
            await ExecuteModule(dependency);
        }

        // Execute the module
        if (module.AST == null)
        {
            throw new Exception("Module AST is null");
        }

        // Execute the module's code in its environment
        Logger.Instance.Debug($"Executing module: {module.FullPath}...");

        // module.environment may be == to interpreter.environment
        // in that case, it will be executed in global scope.
        // module.environment is set in ProcessModule.
        var oldEnv = _interpreter.Environment;
        try {
            _interpreter.Environment = module.environment;
            await _interpreter.Execute(module, mainScript: false);
        } finally {
            _interpreter.Environment = oldEnv;
        }

        module.State = State.Executed;
        Logger.Instance.Debug($"Module execution complete: {module.FullPath}.");
    }
}
