using Zen.Lexing;
using Zen.Parsing;
using Zen.Parsing.AST;
using Zen.Common;
using Zen.Execution.Import;
using Zen.Execution.Import.Providing;

namespace Zen.Execution;

/// <summary>
/// The main runtime for Zen, managing lexing, parsing, and execution.
/// </summary>
public class Runtime
{
    public readonly Lexer Lexer;
    public readonly Parser Parser;

    public readonly EventLoop EventLoop;
    public readonly Resolver Resolver;
    public readonly Interpreter Interpreter;
    public readonly ModuleHelper ModuleHelper;

    public Importer Importer;

    public Runtime()
    {
        Lexer = new Lexer();
        Parser = new Parser();
        EventLoop = new EventLoop();
        EventLoop.Start();
        
        // Create interpreter first
        Interpreter = new Interpreter(EventLoop);

        // Then create a resolver, with the interpreter
        // The resolver handles scope resolution and populates the Interpreters.Locals
        Resolver = new Resolver(Interpreter);

        // create importer and module helper
        // add import providers
        Importer = new Importer(Interpreter, Resolver);
        Importer.RegisterProvider(new BuiltinProvider());
        ModuleHelper = new ModuleHelper(Interpreter);

        Interpreter.Importer = Importer;

        // Register core builtins
        Builtins.Core.Typing.RegisterBuiltins(Interpreter);
        Builtins.Core.Interop.RegisterBuiltins(Interpreter);
        Builtins.Core.Array.RegisterBuiltins(Interpreter);
        Builtins.Core.Time.RegisterBuiltins(Interpreter);

        // Import Always-Loaded modules
        // Importer.Import("System/Collections/BracketGet", true);
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
    public string? Execute(ISourceCode source)
    {
        EventLoop.Start();

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

        try
        {
            // Parse the module
            ModuleHelper.Parse(mainModule);

            if (mainModule.AST == null)
            {
                throw new Exception("Failed to parse main module");
            }

            // Resolve scope
            Resolver.Resolve(mainModule.AST, global: true);
            if (Resolver.Errors.Count > 0)
            {
                throw new Exception("Resolver errors: " + string.Join("\n", Resolver.Errors));
            }

            // Execute the module
            // We pass true to awaitEvents since this is the main script
            Interpreter.Interpret(mainModule.AST, awaitEvents: true);
            
            string output = Interpreter.GlobalOutputBuffer.ToString();
            return output;
        }
        catch (Exception)
        {
            // Clean up the provider on error
            Importer.Providers.Remove(mainProvider);
            EventLoop.Stop();
            throw;
        }
    }

    public string? Execute(string sourceCodeText) => Execute(new InlineSourceCode(sourceCodeText));

    public void Shutdown()
    {
        EventLoop.Stop();
    }
}
