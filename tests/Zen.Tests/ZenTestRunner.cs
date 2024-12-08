using Xunit.Abstractions;
using Zen.Common;
using Zen.Execution;
using Zen.Execution.Import;
using Zen.Lexing;
using Zen.Parsing;

namespace Zen.Tests;

public class TestRunner
{
    protected readonly ITestOutputHelper Output;
    protected Runtime? Runtime;
    
    // Keep these for backward compatibility with existing tests
    protected Lexer Lexer => Runtime.Lexer;
    protected Parser Parser => Runtime.Parser;
    protected Resolver Resolver => Runtime.Resolver;
    protected Interpreter Interpreter => Runtime.Interpreter;
    protected Importer Importer => Runtime.Importer;

    public TestRunner(ITestOutputHelper output)
    {
        Output = output;
        
        // Configure logger to use test output
        Logger.Instance.SetOutput(message => output.WriteLine(message));
        Logger.Instance.SetDebug(true);
    }

    protected virtual async Task RestartInterpreter()
    {
        if (Runtime != null) {
            Runtime.SyncContext.Stop();
        }

        Runtime = new Runtime();
        await Runtime.RegisterCoreBuiltins();
        await Task.CompletedTask;
    }

    private async Task<Runtime> GetRuntime()
    {
        if (Runtime == null) {
            await RestartInterpreter();
        }
        return Runtime!;
    }

    protected async Task<string?> Execute(ISourceCode source, bool outputBuffering = false)
    {
        await GetRuntime();

        // Enable output buffering before executing?
        Runtime!.Interpreter.GlobalOutputBufferingEnabled = outputBuffering;
        Runtime.Interpreter.GlobalOutputBuffer.Clear();
        Runtime.Interpreter.OutputHandler = Output.WriteLine;

        await Runtime!.Execute(source);
        return Runtime.Interpreter.GlobalOutputBuffer.ToString();
    }

    protected async Task<string?> Execute(string source, bool outputBuffering = false) => await Execute(new InlineSourceCode(source), outputBuffering);
}
