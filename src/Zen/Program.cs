﻿namespace Zen;

using SysEnv = System.Environment;

using Zen.Common;
using Zen.Execution;
using Zen.Lexing;
using Zen.Parsing;
using Zen.Parsing.AST;

public class Program
{
    public static async Task Main(string[] args)
    {

        Console.WriteLine(typeof(System.Net.HttpListener));

        // Handle unhandled exceptions
        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
        {
            // Retrieve and display exception details
            if (eventArgs.ExceptionObject is Exception ex)
            {
                Console.WriteLine(ex); // Print the full exception details
            }

            // Perform any additional cleanup if needed
            SysEnv.Exit(70); // Exit with a non-zero code to indicate an error
        };

        // Handle the SIGINT (CTRL+C) signal
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            // Prevent the process from terminating immediately
            eventArgs.Cancel = true;

            // Restore terminal settings or perform cleanup actions here
            Console.WriteLine("Bye!");

            // Exit the application
            SysEnv.Exit(0);
        };

        Logger.Instance.SetDebug(true);

        // check args:
        // if empty, start REPL
        // if not, check if it's a .zen file
        // if not a .zen file, attempt to execute input as is

        Console.WriteLine(args);

        if (args.Length == 0 || args[0] == "") {
            // start REPL
            Console.WriteLine("Starting REPL");
            await REPL();
        } else {
            ISourceCode script;

            bool debugEnabled = args.Contains("--debug");
            Logger.Instance.SetDebug(debugEnabled);
            
            // check if it's a .zen file
            if (args[0].EndsWith(".zen")) {
                Console.WriteLine(args[0]);
                // check if file exists
                if ( ! File.Exists(args[0])) {
                    Console.WriteLine("File does not exist");
                    return;
                }

                // read file
                script = new FileSourceCode(args[0]);
            } else {
                // it's not a .zen file, execute input as is
                script = new InlineSourceCode(args[0]);
            }

            await Execute(script);
        }
    }


    protected static async Task Execute(ISourceCode script)
    {
        Console.WriteLine("Executing script " + script);
        Runtime runtime = new();
        await runtime.RegisterCoreBuiltins();
        
        await runtime.Execute(script);
    }

    protected static async Task REPL() {
        Console.WriteLine("Zen REPL v0.1");
        
        Logger.Instance.SetDebug(true);

        Runtime runtime = new();
        await runtime.RegisterCoreBuiltins();
        

        while (true) {
            Console.Write("\n>> ");
            string? input = Console.ReadLine();
            if (input == null) {
                break;
            }

            // Wrap the expression in print
            // if ( ! input.StartsWith("print")) {
            //     input = "print " + input;
            // }

            await runtime.Execute(input);
        }
    }

    protected static void PrintErrors(List<Error> errors) {
        if (errors.Count == 0) {
            return;
        }
        Console.WriteLine("\n# Errors: #\n");
        foreach (var error in errors) {
            Console.WriteLine(error);
        }
    }

    protected static void PrintTokens(List<Token> tokens) {
        if (tokens.Count == 0) {
            Console.WriteLine("0 Tokens.");
        }
        Console.WriteLine("\n# Tokens: #\n");
        foreach (var token in tokens) {
            Console.Write(token);
            if (token.Type != TokenType.EOF) {
                Console.Write(", ");
            }else {
                Console.Write("\n");
            }
        }
    }

    protected static void PrintAST(Node node) {
        DebugPrintVisitor debugPrinter = new();
        node.Accept(debugPrinter);
        Console.WriteLine("\n# AST: #\n");
        Console.WriteLine(debugPrinter.ToString());
    }
}