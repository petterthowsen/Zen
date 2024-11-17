namespace Zen;

using SysEnv = System.Environment;

using Zen.Common;
using Zen.Execution;
using Zen.Lexing;
using Zen.Parsing;
using Zen.Parsing.AST;

public class Program
{
    public static void Main(string[] args)
    {
        // Handle unhandled exceptions
        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
        {
            // Retrieve and display exception details
            if (eventArgs.ExceptionObject is Exception ex)
            {
                Console.WriteLine("Oops! Unhandled Exception!");
                Console.WriteLine(ex.ToString()); // Print the full exception details
                Console.WriteLine("This is a problem with the Zen Runtime. Sorry!");
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


        // check args:
        // if empty, start REPL
        // if not, check if it's a .zen file
        // if not a .zen file, attempt to execute input as is

        if (args.Length == 0) {
            // start REPL
            Console.WriteLine("Starting REPL");
            REPL();
        } else {
            var lexer = new Lexer();

            // check if it's a .zen file
            if (args[0].EndsWith(".zen")) {
                // check if file exists
                if (!File.Exists(args[0])) {
                    Console.WriteLine("File does not exist");
                    return;
                }

                // read file
                string code = File.ReadAllText(args[0]);
                Execute(code);
            } else {
                // it's not a .zen file, execute input as is
                Execute(args[0]);
            }
        }
    }

    protected static void Execute(string code, Interpreter? interpreter = null) {
        var lexer = new Lexer();
        var tokens = lexer.Tokenize(code);

        PrintTokens(tokens);

        if (lexer.Errors.Count > 0) {
            PrintErrors(lexer.Errors);
            return;
        }

        var parser = new Parser();
        var program = parser.Parse(tokens);

        if (program != null) {
            PrintAST(program);
        }

        if (parser.Errors.Count > 0) {
            PrintErrors(parser.Errors);
            return;
        }

        // execute program
        if (program != null) {
            interpreter ??= new Interpreter();
            Resolver resolver = new(interpreter);
            
            try {
                resolver.Resolve(program);

                if (resolver.Errors.Count > 0) {                    
                    PrintErrors(resolver.Errors);
                    return;
                }

                interpreter.Interpret(program);
            } catch (Exception error) {
                Console.WriteLine(error);
            }
        }
    }

    protected static void REPL() {
        Console.WriteLine("Zen REPL v0.1");
        
        Interpreter interpreter = new();

        while (true) {
            Console.Write(">> ");
            string? input = Console.ReadLine();
            if (input == null) {
                break;
            }

            // Wrap the expression in print
            // if ( ! input.StartsWith("print")) {
            //     input = "print " + input;
            // }

            Execute(input, interpreter);
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