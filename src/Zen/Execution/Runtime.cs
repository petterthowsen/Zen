using Zen.Lexing;
using Zen.Parsing;
using Zen.Parsing.AST;
using Zen.Execution.Import;
using Zen.Common;

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
    public readonly Importer Importer;

    public Runtime()
    {
        Lexer = new Lexer();
        Parser = new Parser();
        EventLoop = new EventLoop();
        EventLoop.Start();
        
        // Create interpreter first
        Interpreter = new Interpreter(EventLoop);
        
        // Then create importer with interpreter reference
        Importer = new Importer(Parser, Lexer, Interpreter);
        
        // Finally set importer on interpreter
        Interpreter.SetImporter(Importer);
        
        Resolver = new Resolver(Interpreter);
    }

    public ProgramNode Parse(ISourceCode sourceCode) => Parser.Parse(Lexer.Tokenize(sourceCode));

    public ProgramNode parse(string sourceCode) => Parse(new InlineSourceCode(sourceCode));

    /// <summary>
    /// Execute the given source code. Pass true to execute as a module, this should be done for top-level main scripts.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="asModule"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public string? Execute(ISourceCode source, bool asModule = true)
    {
        List<Token> tokens = Lexer.Tokenize(source);
        var node = Parser.Parse(tokens);

        if (Parser.Errors.Count > 0)
        {
            throw new Exception("Parse errors: " + string.Join("\n", Parser.Errors));
        }

        if (asModule) {
            // Create a module for the code being executed
            var modulePath = source is FileSourceCode fileSource 
                ? Path.GetFileNameWithoutExtension(fileSource.FilePath)
                : "_inline";
            var module = Module.CreateFileModule(modulePath, [], node);

            // Execute the module in the global environment.
            // Subsequent imported modules will be executed in their own environment.
            Importer.ExecuteModule(module, global: true);
        }
        else {
            Resolver.Resolve(node);
            if (Resolver.Errors.Count > 0)
            {
                throw new Exception("Resolver errors: " + string.Join("\n", Resolver.Errors));
            }
            
            Interpreter.Interpret(node, true);
        }
        
        string output = Interpreter.GlobalOutputBuffer.ToString();
        return output;
    }

    public string? Execute(string sourceCodeText, bool asModule = false) => Execute(new InlineSourceCode(sourceCodeText), asModule);

    /// <summary>
    ///     A program must be a module, so this can be used to set the root module of the executing main script.
    /// </summary>
    /// <param name="module"></param>
    public void SetCurrentModule(Module module)
    {
        Importer.SetCurrentModule(module);
    }

    public bool PackageFileExists(string directory)
    {
        return Importer.PackageFileExists(directory);
    }

    public Package LoadPackage(string path)
    {
        return Importer.LoadPackage(path);
    }

    public void Shutdown()
    {
        EventLoop.Stop();
    }
}
