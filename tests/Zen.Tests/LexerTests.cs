using Zen.Lexing;
using Zen.Common;

namespace Zen.Tests;

public class LexerTests
{
    [Fact]
    public void TestEmptyHasEOFToken()
    {
        var lexer = new Lexer();
        List<Token> tokens = lexer.Tokenize("");

        Assert.NotEmpty(tokens);
        Assert.Equal(TokenType.EOF, tokens.Last().Type);
    }

    [Fact]
    public void TestNewline()
    {
        var lexer = new Lexer();
        List<Token> tokens = lexer.Tokenize("\n");

        Assert.NotEmpty(tokens);
        Assert.Equal(TokenType.Newline, tokens.First().Type);
        Assert.Equal(TokenType.EOF, tokens.Last().Type);
    }

    
    [Fact]
    public void TestSingleDigitInt()
    {
        var lexer = new Lexer();
        List<Token> tokens = lexer.Tokenize("1");

        // 1, EOF
        Assert.Equal(2, tokens.Count);

        Assert.Equal(TokenType.IntLiteral, tokens[0].Type);
        Assert.Equal("1", tokens[0].Value);

        Assert.Equal(TokenType.EOF, tokens.Last().Type);
    }
    
    [Fact]
    public void TestSingleDigitFloat()
    {
        var lexer = new Lexer();
        List<Token> tokens = lexer.Tokenize("1.0");

        // 1.0, EOF
        Assert.Equal(2, tokens.Count);

        Assert.Equal(TokenType.FloatLiteral, tokens[0].Type);
        Assert.Equal("1.0", tokens[0].Value);

        Assert.Equal(TokenType.EOF, tokens.Last().Type);
    }

    
    [Fact]
    public void TestLineCommentIsIgnored()
    {
        var lexer = new Lexer();
        List<Token> tokens = lexer.Tokenize("# comment here");

        // EOF
        Assert.Single(tokens);
        Assert.Equal(TokenType.EOF, tokens.Last().Type);
    }

    [Fact]
    public void TestVarWithStringLiteral()
    {
        var lexer = new Lexer();
        List<Token> tokens = lexer.Tokenize("var = \"hello\"");

        // var, whitespace, =, whitespace, "hello", EOF
        Assert.Equal(6, tokens.Count);

        Assert.Equal(TokenType.Keyword, tokens[0].Type);
        Assert.Equal("var", tokens[0].Value);

        Assert.Equal(TokenType.Whitespace, tokens[1].Type);

        Assert.Equal(TokenType.Assign, tokens[2].Type);

        Assert.Equal(TokenType.Whitespace, tokens[3].Type);

        Assert.Equal(TokenType.StringLiteral, tokens[4].Type);
        Assert.Equal("hello", tokens[4].Value);

        Assert.Equal(TokenType.EOF, tokens.Last().Type);
    }

    
    [Fact]
    public void TestIfStatement()
    {
        var lexer = new Lexer();
        List<Token> tokens = lexer.Tokenize("if true {\n    print \"hello\"\n}");

        // 13 tokens:
        // if, whitespace, true, whitespace, {, newline, whitespace, Keyword "print", whitespace, string literal, newline, }, EOF
        Assert.Equal(13, tokens.Count);

        Assert.Equal(TokenType.Keyword, tokens[0].Type);
        Assert.Equal("if", tokens[0].Value);

        Assert.Equal(TokenType.Whitespace, tokens[1].Type);

        Assert.Equal(TokenType.Keyword, tokens[2].Type);
        Assert.Equal("true", tokens[2].Value);

        Assert.Equal(TokenType.Whitespace, tokens[3].Type);

        Assert.Equal(TokenType.OpenBrace, tokens[4].Type);

        Assert.Equal(TokenType.Newline, tokens[5].Type);

        Assert.Equal(TokenType.Whitespace, tokens[6].Type);

        Assert.Equal(TokenType.Keyword, tokens[7].Type);
        Assert.Equal("print", tokens[7].Value);

        Assert.Equal(TokenType.Whitespace, tokens[8].Type);

        Assert.Equal(TokenType.StringLiteral, tokens[9].Type);
        Assert.Equal("hello", tokens[9].Value);

        Assert.Equal(TokenType.Newline, tokens[10].Type);

        Assert.Equal(TokenType.CloseBrace, tokens[11].Type);

        Assert.Equal(TokenType.EOF, tokens.Last().Type);
    }
    
    [Fact]
    public void TestIfElseIfStatement() {
        var lexer = new Lexer();
        List<Token> tokens = lexer.Tokenize("if true {\n    print \"hello\"\n} else {\n    print \"world\"\n}");

        // 24 tokens:
        // if, whitespace, true, whitespace, {, newline, whitespace, Keyword "print", whitespace, string literal, newline, }, Keyword "else", whitespace, {, newline, whitespace, Keyword "print", whitespace, string literal, newline, }, EOF
        Assert.Equal(24, tokens.Count);
        
        Assert.Equal(TokenType.Keyword, tokens[0].Type);
        Assert.Equal("if", tokens[0].Value);

        Assert.Equal(TokenType.Whitespace, tokens[1].Type);

        Assert.Equal(TokenType.Keyword, tokens[2].Type);
        Assert.Equal("true", tokens[2].Value);

        Assert.Equal(TokenType.Whitespace, tokens[3].Type);

        Assert.Equal(TokenType.OpenBrace, tokens[4].Type);

        Assert.Equal(TokenType.Newline, tokens[5].Type);

        Assert.Equal(TokenType.Whitespace, tokens[6].Type);

        Assert.Equal(TokenType.Keyword, tokens[7].Type);
        Assert.Equal("print", tokens[7].Value);

        Assert.Equal(TokenType.Whitespace, tokens[8].Type);

        Assert.Equal(TokenType.StringLiteral, tokens[9].Type);
        Assert.Equal("hello", tokens[9].Value);

        Assert.Equal(TokenType.Newline, tokens[10].Type);

        Assert.Equal(TokenType.CloseBrace, tokens[11].Type);

        Assert.Equal(TokenType.Whitespace, tokens[12].Type);

        Assert.Equal(TokenType.Keyword, tokens[13].Type);
        Assert.Equal("else", tokens[13].Value);

        Assert.Equal(TokenType.Whitespace, tokens[14].Type);

        Assert.Equal(TokenType.OpenBrace, tokens[15].Type);

        Assert.Equal(TokenType.Newline, tokens[16].Type);

        Assert.Equal(TokenType.Whitespace, tokens[17].Type);

        Assert.Equal(TokenType.Keyword, tokens[18].Type);
        Assert.Equal("print", tokens[18].Value);

        Assert.Equal(TokenType.Whitespace, tokens[19].Type);

        Assert.Equal(TokenType.StringLiteral, tokens[20].Type);
        Assert.Equal("world", tokens[20].Value);

        Assert.Equal(TokenType.Newline, tokens[21].Type);

        Assert.Equal(TokenType.CloseBrace, tokens[22].Type);

        Assert.Equal(TokenType.EOF, tokens.Last().Type);
    }

    [Fact]
    public void TestUnclosedStringLiteral()
    {
        var lexer = new Lexer();
        List<Token> tokens = lexer.Tokenize("var = \"hello");

        Console.WriteLine(tokens);

        // var, whitespace, =, whitespace, EOF
        Assert.Equal(5, tokens.Count);

        Assert.Equal(TokenType.Keyword, tokens[0].Type);
        Assert.Equal("var", tokens[0].Value);

        Assert.Equal(TokenType.Whitespace, tokens[1].Type);

        Assert.Equal(TokenType.Assign, tokens[2].Type);

        Assert.Equal(TokenType.Whitespace, tokens[3].Type);

        Assert.Equal(TokenType.EOF, tokens.Last().Type);

        Assert.NotEmpty(lexer.Errors);
        Assert.Single(lexer.Errors);
        Assert.IsType<SyntaxError>(lexer.Errors[0]);
        Assert.Equal(ErrorType.UnclosedStringLiteral, lexer.Errors[0].Type);
    }
}