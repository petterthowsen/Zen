using Zen.Lexing;
using Zen.Parsing;
using Zen.Parsing.AST;
using Zen.Common;
using Zen.Execution.Import;
using Zen.Execution.Import.Providing;
using Zen.Typing;

namespace Zen.Execution;

/// <summary>
/// The main runtime for Zen, managing lexing, parsing, and execution.
/// </summary>
public class Runtime
{
    public readonly Lexer Lexer;
    public readonly Parser Parser;

    public readonly ZenSynchronizationContext SyncContext;
    public readonly Resolver Resolver;
    public readonly Interpreter Interpreter;
    public readonly ModuleHelper ModuleHelper;

    public Importer Importer;

    public Runtime()
    {
        Lexer = new Lexer();
        Parser = new Parser();
        SyncContext = new ZenSynchronizationContext();

        // Set the synchronization context for async operations
        // This ensures that all async operations are executed on the same thread
        // as the event loop, without the need for locks.
        SynchronizationContext.SetSynchronizationContext(SyncContext);
        
        // Create interpreter first
        Interpreter = new Interpreter(SyncContext);

        // Then create a resolver, with the interpreter
        // The resolver handles scope resolution and populates the Interpreters.Locals
        Resolver = new Resolver(Interpreter);

        // create importer and module helper
        // add import providers
        Importer = new Importer(Interpreter, Resolver);
        Importer.RegisterProvider(new BuiltinProvider());
        ModuleHelper = new ModuleHelper(Interpreter);

        Interpreter.Importer = Importer;

        // define global runtime constants
        Interpreter.globalEnvironment.Define(true, "ZEN_THIS_FILE", ZenType.String, false);
        Interpreter.globalEnvironment.Define(true, "ZEN_SCRIPT_FILE", ZenType.String, false);
        Interpreter.globalEnvironment.Define(true, "ZEN_SCRIPT_DIR", ZenType.String, false);
        Interpreter.globalEnvironment.Define(true, "ZEN_RUNTIME_VERSION", ZenType.String, false);

        Interpreter.globalEnvironment.Assign("ZEN_RUNTIME_VERSION", new ZenValue(ZenType.String, "0.1.0"));
    }

    public async Task RegisterCoreBuiltins()
    {
        // Initialize core builtins
        await Builtins.Core.Typing.Initialize(Interpreter);
        await Builtins.Core.Async.Initialize(Interpreter);
        await Builtins.Core.String.Initialize(Interpreter);
        await Builtins.Core.ModuleContainer.Initialize(Interpreter);
        await Builtins.Core.Interop.Initialize(Interpreter);
        await Builtins.Core.Array.Initialize(Interpreter);
        await Builtins.Core.Time.Initialize(Interpreter);

        await Builtins.Core.Typing.Register(Interpreter);
        await Builtins.Core.Async.Register(Interpreter);
        await Builtins.Core.String.Register(Interpreter);
        await Builtins.Core.ModuleContainer.Register(Interpreter);
        await Builtins.Core.Interop.Register(Interpreter);
        await Builtins.Core.Array.Register(Interpreter);
        await Builtins.Core.Time.Register(Interpreter);
        
        // Globalize the Map class.
        ZenType mapType = (await Interpreter.FetchSymbol("Zen/Collections/Map", "Map")).Underlying!;
        Interpreter.globalEnvironment.Define(true, "Map", ZenType.Type, false);
        Interpreter.globalEnvironment.Assign("Map", new ZenValue(ZenType.Type, mapType));

        // Globalize the Exception class
        ZenType exceptionType = (await Interpreter.FetchSymbol("Zen/Exception", "Exception")).Underlying!;
        Interpreter.globalEnvironment.Define(true, "Exception", ZenType.Type, false);
        Interpreter.globalEnvironment.Assign("Exception", new ZenValue(ZenType.Type, exceptionType));

        // Globalize the Promise class ?
        ZenType promiseType = (await Interpreter.FetchSymbol("Zen/Promise", "Promise")).Underlying!;
        Interpreter.globalEnvironment.Define(true, "Promise", ZenType.Type, false);
        Interpreter.globalEnvironment.Assign("Promise", new ZenValue(ZenType.Type, promiseType));

        await Task.CompletedTask;
    }

    private string GetPackageName(string? scriptDirectory)
    {
        if (scriptDirectory == null) return "main";

        var packagePath = Path.Combine(scriptDirectory, "package.zen");
        if (!File.Exists(packagePath)) return "main";

        var packageContent = File.ReadAllText(packagePath).Trim();
        var parts = packageContent.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || parts[0] != "package") return "Main";

        return parts[1];
    }

    public ProgramNode Parse(ISourceCode sourceCode) => Parser.Parse(Lexer.Tokenize(sourceCode));

    public ProgramNode Parse(string sourceCode) => Parse(new InlineSourceCode(sourceCode));

    /// <summary>
    /// Execute source code as a module, setting up the import system
    /// </summary>
    public async Task<string?> Execute(ISourceCode source)
    {
        // Create a module for the main script
        string moduleName = source is FileSourceCode fs ? 
            Path.GetFileNameWithoutExtension(fs.FilePath) : 
            "__main__";

        // Get package name from package.zen if it exists
        string packageName = "Main";
        if (source is FileSourceCode fileSource)
        {
            var directory = Path.GetDirectoryName(fileSource.FilePath);
            packageName = GetPackageName(directory);
        }
        
        var mainModule = new Module(moduleName, source);
        mainModule.environment = Interpreter.globalEnvironment;

        // Register the main script provider
        var mainProvider = new MainScriptModuleProvider(mainModule, packageName);
        Importer.RegisterProvider(mainProvider);

        // assign the global runtime constants
        var cwd = Directory.GetCurrentDirectory();
        string? ZEN_SCRIPT_FILE = null;
        string ZEN_SCRIPT_DIR = null;
        
        if (source is FileSourceCode fileSourceCode)
        {
            ZEN_SCRIPT_FILE = Path.GetFullPath(fileSourceCode.FilePath);
            ZEN_SCRIPT_DIR = Path.GetDirectoryName(ZEN_SCRIPT_FILE)!;
        }

        Interpreter.globalEnvironment.Assign("ZEN_SCRIPT_FILE", new ZenValue(ZenType.String, ZEN_SCRIPT_FILE));
        Interpreter.globalEnvironment.Assign("ZEN_SCRIPT_DIR", new ZenValue(ZenType.String, ZEN_SCRIPT_DIR));
        Interpreter.globalEnvironment.Assign("ZEN_THIS_FILE", new ZenValue(ZenType.String, ZEN_SCRIPT_FILE));
        
        try
        {
            // Parse the module
            ModuleHelper.Parse(mainModule);

            if (mainModule.AST == null)
            {
                throw new Exception("Failed to parse main module");
            }

            // Run the Resolver in global scope (imported modules will be resolved in their own scope.)
            Resolver.Resolve(mainModule.AST, global: true);
            if (Resolver.Errors.Count > 0)
            {
                throw new Exception("Resolver errors: " + string.Join("\n", Resolver.Errors));
            }

            // Execute the module and run the event loop on the current thread
            // this will block until the event loop is finished.
            await Interpreter.Execute(mainModule, mainScript: true);
            
            string output = Interpreter.GlobalOutputBuffer.ToString();
            return output;
        }
        finally
        {
            Importer.Providers.Remove(mainProvider);
        }
    }

    public async Task<string?> Execute(string sourceCodeText) => await Execute(new InlineSourceCode(sourceCodeText));
}
