namespace Zen.Common;

public enum TokenType {
    Illegal,
    Comment, // #
    Whitespace,
    Newline,
    EOF,

    Keyword,
    Identifier,

    StringLiteral,
    IntLiteral,
    FloatLiteral,

    // Punctuation
    Dot,
    Comma,
    Colon,
    Semicolon,
    QuestionMark,

    // Operators
    Plus,
    Minus,
    Star,
    Slash,
    Percent,

    // Compound Assignment
    PlusAssign, // +=
    MinusAssign, // -=
    StarAssign, // *=
    SlashAssign, // /=

    Assign, // =

    DoubleDot, // .. (for range operators) 
    Ellipsis, // ... (for variadic functions and spread operators)

    // Comparators
    Equal, // ==
    NotEqual, // !=
    LessThan, // <
    GreaterThan, // >
    LessThanOrEqual, // <=
    GreaterThanOrEqual, // >=

    Increment, // ++
    Decrement, // --

    // Delimiters
    OpenParen,
    CloseParen,
    OpenBracket,
    CloseBracket,
    OpenBrace,
    CloseBrace
}