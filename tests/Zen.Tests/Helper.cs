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

    public static string PrintTokens(List<Token> tokens) {
        if (tokens.Count == 0) {
            return "0 Tokens.";
        }
        string str = "\n# Tokens: #\n";
        foreach (var token in tokens) {
            str += token;
            if (token.Type != TokenType.EOF) {
                str += ", ";
            }else {
                str += "\n";
            }
        }
        return str;
    }

    public static string PrintAST(Node node) {
        DebugPrintVisitor debugPrinter = new();
        node.Accept(debugPrinter);
        return "\n# AST: #\n" + debugPrinter.ToString();
    }

}