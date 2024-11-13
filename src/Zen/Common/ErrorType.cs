namespace Zen.Common;

public enum ErrorType
{
    // Lexical Errors
    SyntaxError,
    UnclosedStringLiteral,
    InvalidEscapeSequence,

    // Parsing Errors
    ParseError,

    // Execution Errors
    RuntimeError,
    
    RedefinitionError,
    UndefinedVariable,
    TypeError,

}