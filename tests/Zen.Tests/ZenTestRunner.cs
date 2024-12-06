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
    protected Runtime Runtime;
    
    // Keep these for backward compatibility with existing tests
    protected Lexer Lexer => Runtime.Lexer;
    protected Parser Parser => Runtime.Parser;
    protected Resolver Resolver => Runtime.Resolver;
    protected Interpreter Interpreter => Runtime.Interpreter;
    protected EventLoop EventLoop => Runtime.EventLoop;
    protected Importer Importer => Runtime.Importer;

    public TestRunner(ITestOutputHelper output)
    {
        Output = output;
        
        // Configure logger to use test output
        Logger.Instance.SetOutput(message => output.WriteLine(message));
        Logger.Instance.SetDebug(true);
        
        Runtime = new Runtime();
    }

    protected virtual void RestartInterpreter()
    {
        Runtime.EventLoop.Stop();
        Runtime = new Runtime();
    }

    protected string? Execute(ISourceCode source)
    {
        // Enable output buffering before executing
        Runtime.Interpreter.GlobalOutputBufferingEnabled = false;
        Runtime.Interpreter.GlobalOutputBuffer.Clear();

        Runtime.Interpreter.OutputHandler = Output.WriteLine;

        try
        {
            return Runtime.Execute(source);
        }
        finally
        {
            // Clear the buffer after execution
            Runtime.Interpreter.GlobalOutputBuffer.Clear();
        }
    }

    protected string? Execute(string source) => Execute(new InlineSourceCode(source));
}
