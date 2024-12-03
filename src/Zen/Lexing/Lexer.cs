using System.Text;
using Zen.Common;

namespace Zen.Lexing;

public class Lexer {

    private ISourceCode _SourceCode { get; set; }

    public ISourceCode SourceCode => _SourceCode;

    public List<Token> Tokens = [];

    public readonly List<Error> Errors = [];

    private int _position = 0;
    private int _line = 0;
    private int _column = 0;
    
    /**
     * The current character being looked at. It is not consumed yet.
     */
    protected char Current => SourceCode.GetChar(_position);

    protected char Next => SourceCode.GetChar(_position + 1);

    protected char Previous => SourceCode.GetChar(_position - 1);
    
    protected char Get(int index) => SourceCode.GetChar(index);

    protected bool Match(char ch) => Current == ch;

    protected bool EOF => _position >= SourceCode.Code.Length;

    protected void Advance(int length = 1) {
        for (int i = 0; i < length; i++) {
            if (EOF) {
                throw new Exception("Cannot Advance: Unexpected EOF!");
            }
            if (Current == '\n') {
                _line++;
                _column = 0;
            }else {
                _column++;
            }
            _position++;
        }
    }
    
    public Lexer() {
        _SourceCode = new InlineSourceCode("");
    }

    public Lexer(ISourceCode sourceCode) {
        _SourceCode = sourceCode;
    }
    
    protected Token AddToken(TokenType type, string literal) {
        Token token = new(type, literal, SourceCode.MakeLocation(_line, _column));
        Tokens.Add(token);
        return token;
    }

    /// <summary>
    /// Consume all characters of the given type until a non-matching character is encountered.
    /// </summary>
    /// <param name="character">The characters to match</param>
    /// <param name="type">The type of the token to be created</param>
    /// <returns>The created token, or null if no characters match</returns>
    protected Token? ConsumeAll(List<Char> characters, TokenType type) {
        if ( ! characters.Contains(Current)) {
            return null;
        }

        int start = _position;
        while (! EOF && characters.Contains(Current)) {
            Advance();
        }
        return AddToken(type, SourceCode.Code[start.._position]);
    }
    
    protected Token? ConsumeAll(char character, TokenType type) {
        return ConsumeAll([character], type);
    }

    protected Token? Consume(char character, TokenType type) {
        if (Current == character) {
            string literal = character.ToString();
            Advance();
            return AddToken(type, literal);
        }
        return null;
    }

    /// <summary>
    /// Checks if the given sequence of characters matches the current
    /// position in the source code. If the sequence matches, true is
    /// returned. Otherwise, false is returned.
    /// </summary>
    /// <param name="sequence">A string containing the sequence of characters
    /// to be checked.</param>
    protected bool MatchSequence(string sequence) {
        for (int i = 0; i < sequence.Length; i++) {
            if (sequence[i] != Get(_position + i)) {
                return false;
            }
        }
        
        return true;
    }
    
    
    /// <summary>
    /// Consumes a sequence of characters from the current position if
    /// the given sequence matches. If the sequence matches, the
    /// created token is returned. Otherwise, null is returned.
    /// </summary>
    /// <param name="sequence">A string containing the sequence of characters
    /// to be checked.</param>
    /// <param name="type">The type of token to be created if the sequence
    /// matches.</param>
    protected Token? ConsumeSequence(string sequence, TokenType type) {
        if (MatchSequence(sequence)) {
            string literal = sequence;
            
            // Advance the position by the length of the sequence
            Advance(sequence.Length);

            return AddToken(type, literal);
        }
        return null;
    }

    /// <summary>
    /// Scans a comment from the current position, starting with '#'.
    /// The comment is considered to continue until a newline is encountered.
    /// The created token is of type TokenType.Comment.
    /// </summary>
    protected void ScanComment(bool createCommentTokken = false) {
        if ( Current != '#') {
            throw new Exception("Comment must start with '#'!");
        }

        int start = _position;
        while ( ! EOF && Current != '\n') {
            Advance();
        }
        if (createCommentTokken) {
            AddToken(TokenType.Comment, SourceCode.Substring(start.._position));
        }
    }

    protected void ScanNumber() {
        if ( ! char.IsDigit(Current)) {
            throw new Exception("Number must start with a digit!");
        }

        int start = _position;
        
        while ( ! EOF && char.IsDigit(Current)) {
            Advance();
        }
        
        if ( ! EOF && Current == '.') {
            // it's a float literal
            Advance();
            while ( ! EOF && char.IsDigit(Current)) {
                Advance();
            }
            AddToken(TokenType.FloatLiteral, SourceCode.Substring(start.._position));
        } else {
            // it's an int literal
            AddToken(TokenType.IntLiteral, SourceCode.Substring(start.._position));
        }
    }

    protected void ScanStringLiteral() {
        if (Current != '"') {
            throw new Exception("String literal must start with '\"'!");
        }

        Advance(); // Consume the opening quote
        int start = _position;
        var stringBuilder = new StringBuilder();

        while (true) {
            if (EOF) {
                Error("Unclosed string literal", ErrorType.UnclosedStringLiteral, start);
                return;
            } else if (Current == '\n') {
                Error("Unexpected newline in string literal", ErrorType.UnclosedStringLiteral, start);
                return;
            } else if (Current == '"') {
                break; // End of string literal
            } else if (Current == '\\') {
                Advance(); // Consume the backslash

                // Handle escape sequences
                if (EOF) {
                    Error("Unclosed string literal after escape character", ErrorType.UnclosedStringLiteral, start);
                    return;
                }

                switch (Current) {
                    case 'n':
                        stringBuilder.Append('\n');
                        break;
                    case 'r':
                        stringBuilder.Append('\r');
                        break;
                    case 't':
                        stringBuilder.Append('\t');
                        break;
                    case '\\':
                        stringBuilder.Append('\\');
                        break;
                    case '"':
                        stringBuilder.Append('"');
                        break;
                    default:
                        Error($"Unknown escape sequence: \\{Current}", ErrorType.InvalidEscapeSequence, _position);
                        return;
                }
            } else {
                stringBuilder.Append(Current);
            }

            Advance();
        }

        Advance(); // Consume the closing quote
        AddToken(TokenType.StringLiteral, stringBuilder.ToString());
    }

    private void ScanIdentifierOrKeyword() {
        int start = _position;
        Advance();

        while ( ! EOF && (char.IsLetterOrDigit(Current) || Current == '_')) {
            Advance();
        }

        string literal = _SourceCode.Substring(start.._position);

        bool isKeyword = Enum.TryParse<Keywords>(literal, true, out _);

        if (isKeyword) {
            AddToken(TokenType.Keyword, literal);
        } else {
            AddToken(TokenType.Identifier, literal);
        }
    }

    public List<Token> Tokenize(string code) {
        _SourceCode = new InlineSourceCode(code);
        return Tokenize();
    }

    public List<Token> Tokenize(ISourceCode sourceCode) {
        _SourceCode = sourceCode;
        return Tokenize();
    }

    public List<Token> Tokenize() {
        _position = 0;
        _line = 0;
        _column = 0;
        Errors.Clear();
        Tokens = [];

        while (_position <= SourceCode.Length) {
            if (EOF) {
                AddToken(TokenType.EOF, "");
                break;
            }

            // Parse multiple character tokens
            if (Current == '#') {
                // # comments
                ScanComment();
                continue;
            } else if (Current == '"') {
                ScanStringLiteral();
                continue;
            } else if (char.IsDigit(Current)) {
                ScanNumber();
                continue;
            } else if (Current == ' ' || Current == '\t') {
                // All consecutive spaces and tabs is a single whitespace token
                ConsumeAll([' ', '\t'], TokenType.Whitespace);
                continue;
            } else if (char.IsLetter(Current) || Current == '_') {
                ScanIdentifierOrKeyword();
                continue;
            } else if (MatchSequence("==")) {
                ConsumeSequence("==", TokenType.Equal);
                continue;
            } else if (MatchSequence("!=")) {
                ConsumeSequence("!=", TokenType.NotEqual);
                continue;
            } else if (MatchSequence("<=")) {
                ConsumeSequence("<=", TokenType.LessThanOrEqual);
                continue;
            } else if (MatchSequence(">=")) {
                ConsumeSequence(">=", TokenType.GreaterThanOrEqual);
                continue;
            } else if (MatchSequence("+=")) {
                ConsumeSequence("+=", TokenType.PlusAssign);
                continue;
            } else if (MatchSequence("-=")) {
                ConsumeSequence("-=", TokenType.MinusAssign);
                continue;
            } else if (MatchSequence("*=")) {
                ConsumeSequence("*=", TokenType.StarAssign);
                continue;
            } else if (MatchSequence("/=")) {
                ConsumeSequence("/=", TokenType.SlashAssign);
                continue;
            } else if (MatchSequence("++")) {
                ConsumeSequence("++", TokenType.Increment);
                continue;
            } else if (MatchSequence("--")) {
                ConsumeSequence("--", TokenType.Decrement);
                continue;
            } else if (MatchSequence("...")) {
                ConsumeSequence("...", TokenType.Ellipsis);
                continue;
            } else if (MatchSequence("..")) {
                ConsumeSequence("..", TokenType.DoubleDot);
                continue;
            }
            
            // Parse single character tokens
            switch (Current) {
                case '\n':
                    ConsumeAll(['\n'], TokenType.Newline);
                    continue;
                case '.':
                    Consume('.', TokenType.Dot);
                    continue;
                case ',':
                    Consume(',', TokenType.Comma);
                    continue;
                case ':':
                    Consume(':', TokenType.Colon);
                    continue;
                case ';':
                    Consume(';', TokenType.Semicolon);
                    continue;
                case '?':
                    Consume('?', TokenType.QuestionMark);
                    continue;
                case '+':
                    Consume('+', TokenType.Plus);
                    continue;
                case '-':
                    Consume('-', TokenType.Minus);
                    continue;
                case '*':
                    Consume('*', TokenType.Star);
                    continue;
                case '/':
                    Consume('/', TokenType.Slash);
                    continue;
                case '%':
                    Consume('%', TokenType.Percent);
                    continue;
                case '=':
                    Consume('=', TokenType.Assign);
                    continue;
                case '<':
                    Consume('<', TokenType.LessThan);
                    continue;
                case '>':
                    Consume('>', TokenType.GreaterThan);
                    continue;
                case '(':
                    Consume('(', TokenType.OpenParen);
                    continue;
                case ')':
                    Consume(')', TokenType.CloseParen);
                    continue;
                case '{':
                    Consume('{', TokenType.OpenBrace);
                    continue;
                case '}':
                    Consume('}', TokenType.CloseBrace);
                    continue;
                case '[':
                    Consume('[', TokenType.OpenBracket);
                    continue;
                case ']':
                    Consume(']', TokenType.CloseBracket);
                    continue;
            }

            Error("Unrecognized character '" + Current + "'", ErrorType.SyntaxError, _position);
            break;
        }
        
        return Tokens;
    }

    protected void Error(string message, ErrorType errorType = ErrorType.SyntaxError, int? position = null) {
        int pos = position ?? _position;

        int l = 0;
        int c = 0;
        for (int i = 0; i < pos; i++) {
            if (SourceCode.GetChar(i) == '\n') {
                l++;
                c = 0;
            } else {
                c++;
            }
        }

        var err = new SyntaxError(message, errorType, SourceCode.MakeLocation(l, c));
        Errors.Add(err);
    }

}