using Zen.Common;
using Zen.Lexing;
using Zen.Parsing;
using Zen.Parsing.AST;

namespace Zen.Tests;

public class Helper {

    public static string GetErrors(List<Error> errors) {
        if (errors.Count == 0) {
            return "";
        }
        string str = "\n# Errors: #\n";
        foreach (var error in errors) {
            str += error;
        }
        return str;
    }

    public static void PrintTokens(List<Token> tokens) {
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

    public static void PrintAST(Node node) {
        DebugPrintVisitor debugPrinter = new();
        node.Accept(debugPrinter);
        Console.WriteLine("\n# AST: #\n");
        Console.WriteLine(debugPrinter.ToString());
    }

}