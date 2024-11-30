namespace Zen.Parsing;

using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST;
using Zen.Parsing.AST.Expressions;
using Zen.Parsing.AST.Statements;
using Zen.Typing;

public partial class Parser
{

	// public enum ParsingContext {
	// 	Default,
	// 	Class,
	// 	Function,
	// }

	// private ParsingContext Context = ParsingContext.Default;

	private List<Token> Tokens = [];

	public readonly List<Error> Errors = [];

	private int _index = 0;

	protected Token Current => Tokens[_index];

	protected Token Peek(int offset = 1) => Tokens[_index + offset];

	protected Token Next => Tokens[_index + 1];

	protected Token Previous => Tokens[_index - 1];

	protected bool IsAtEnd => Current.Type == TokenType.EOF;

	public ProgramNode Parse(List<Token> tokens, bool throwErrors = true)
	{
		Tokens = tokens;
		_index = 0;
		Errors.Clear();
		ProgramNode program = new();

		if (throwErrors) {
			ParseProgram(program);
		}else {
			try {
				ParseProgram(program);
			} catch (Error) {
				Synchronize();
				ParseProgram(program);
			}
		}

		return program;
	}

	private void ParseProgram(ProgramNode program) {
		while ( ! IsAtEnd) {
			MaybeSome(TokenType.Whitespace, TokenType.Newline);
			program.Statements.Add(Statement());
			MaybeSome(TokenType.Whitespace, TokenType.Newline);
		}
	}

	private Stmt Statement() {
		if (MatchKeyword("var") || MatchKeyword("const")) return VarStatement();
		if (MatchKeyword("print")) return PrintStatement();
		if (MatchKeyword("if")) return IfStatement();
		if (MatchKeyword("while")) return WhileStatement();
		if (MatchKeyword("for")) return ForStatement();
		if (MatchKeywordSequence("async", "func")) return FuncStatement(true);
		if (MatchKeyword("func")) return FuncStatement(false);
		if (MatchKeyword("class")) return ClassStatement();
		if (MatchKeyword("return")) return ReturnStatement();
		if (MatchKeyword("import")) return ImportStatement();
		if (MatchKeyword("from")) return FromImportStatement();
		if (MatchKeyword("package")) return PackageStatement();
		return ExpressionStatement();
	}

	protected Error Error(string message, ErrorType errorType = ErrorType.ParseError)
	{
		var err = new SyntaxError(message, errorType, Current.Location);
		Errors.Add(err);
		return err;
	}

	private void Synchronize()
	{
		Advance();

		while ( ! IsAtEnd)
		{
			if (Previous.Type == TokenType.Semicolon) return;

			if (Peek().Type == TokenType.Keyword) {
				switch (Peek().Value)
				{
					case "class":
					case "async":
					case "func":
					case "var":
					case "const":
					case "for":
					case "if":
					case "while":
					case "when":
					case "echo":
					case "return":
						return;
				}
			}
			Advance();
		}
	}

	private Token Advance()
	{
		if (!IsAtEnd)
		{
			_index++;
		}
		return Previous;
	}

	private bool Check(params TokenType[] types)
	{
		foreach (var type in types)
		{
			if (Current.Type == type)
			{
				return true;
			}
		}
		return false;
	}

	private bool Match(params TokenType[] types)
	{
		foreach (var type in types)
		{
			if (Current.Type == type)
			{
				Advance();
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Consumes the current token if it matches the given type, advancing the index.
	/// If the token does not match, throws an error with the given message.
	/// </summary>
	/// <param name="type">The type of token to consume</param>
	/// <param name="message">The error message to throw if the token does not match</param>
	/// <returns>The consumed token</returns>
	private Token Consume(TokenType type, string message)
	{
		if (Current.Type == type)
		{
			return Advance();
		}
		throw Error(message);
	}

	/// <summary>
	/// Consumes the given token type if it matches the current token.
	/// If the token does not match, returns 0.
	/// If multiple is true, consumes all sequential tokens of the given type.
	/// </summary>
	/// <param name="type">The type of token to consume</param>
	/// <param name="multiple">If true, consumes all sequential tokens of the given type</param>
	/// <returns>The number of consumed tokens, or 0 if the token did not match</returns>
	private int MaybeOneOrMore(TokenType[] types, bool multiple = false)
	{
		int count = 0;
		while (Check(types))
		{
			Advance();
			count += 1;
			if ( ! multiple)
			{
				break;
			}
		}
		
		return count;
	}

	private int MaybeSome(params TokenType[] types) {
		return MaybeOneOrMore(types, true);
	}

	private void Maybe(params TokenType[] types) {
		MaybeOneOrMore(types, false);
            }

            private void AtleastOne(TokenType type) {
		if (MaybeSome(type) == 0) {
			throw Error($"Expected at least one {type}");
		}
	}

	private bool CheckSequence(bool ignoreWhiteSpace = false, params TokenType[] types) {
		int offset = 0;

		foreach (var type in types) {
			// Skip any whitespace tokens if ignoreWhiteSpace is true
			while (ignoreWhiteSpace && Peek(offset).Type == TokenType.Whitespace) {
				offset += 1;
        }

        // Check if the current token matches the expected type
			if (Peek(offset).Type == type) {
				offset += 1;
			} else {
				return false;
			}
		}
		
		return true;
	}

	private bool CheckKeywordSequence(params string[] keywords) {
		int offset = 0;
		foreach (var keyword in keywords) {
			if (Peek(offset).Type != TokenType.Keyword || Peek(offset).Value != keyword) {
				return false;
			}
			offset += 1;
			while (Peek(offset).Type == TokenType.Whitespace) {
				offset += 1;
			}
		}
		return true;
	}

	/// <summary>
	/// Matches a sequence of keywords from the current position in the token stream.
	/// Whitespaces between the keywords are consumed, but not before or after the sequence.
	/// </summary>
	/// <param name="keywords">An array of keyword strings to be matched in sequence.</param>
	/// <returns>True if the sequence of keywords matches; otherwise, false.</returns>
	private bool MatchKeywordSequence(params string[] keywords) {
		int index = _index;
		foreach (var keyword in keywords) {
			if (Current.Type != TokenType.Keyword || Current.Value != keyword) {
				_index = index;
				return false;
			}
			Advance();
			while (keywords.Last() != keyword && Current.Type == TokenType.Whitespace) {
				Advance();
			}
		}
		return true;
	}

	private bool MatchKeyword(params string[] keywords)
	{
		foreach (var keyword in keywords) {
			if (Current.Type == TokenType.Keyword && Current.Value == keyword)
			{
				Advance();
				return true;
			}
		}
		return false;
    }

private Parameter ParseParameter() {
    // Parse parameter name
    if (!Match(TokenType.Identifier, TokenType.Keyword)) {
        throw Error($"Expected parameter name (identifier or keyword)", ErrorType.SyntaxError);
    }
    string name = Previous.Value;
    MaybeSome(TokenType.Whitespace);

    // Check if it's a value constraint (has colon)
    bool isTypeParameter = !Match(TokenType.Colon);
    if (isTypeParameter) {
        // It's a type parameter like "T"
        // Create a TypeHint marked as generic
        return new Parameter(name, new TypeHint(Previous, [], false, true), null);
    }

    // It's a value constraint like max: int
    MaybeSome(TokenType.Whitespace);
    TypeHint type = TypeHint();
    MaybeSome(TokenType.Whitespace);

    // Check for default value
    Expr? defaultValue = null;
    if (Match(TokenType.Assign)) {
        MaybeSome(TokenType.Whitespace);
        defaultValue = Primary(); // Only allow literal values as defaults
        MaybeSome(TokenType.Whitespace);
    }

    return new Parameter(name, type, defaultValue, false);
}

    private TypeHint TypeHint()
    {
        return TypeHint([]);
    }

    /// <summary>
    /// Parses a type hint, E.g 'int', 'Array<string>', or 'MyClass'
    /// </summary>
    /// <param name="genericTypes">Will create a generic type parameters when encountering identifiers matching the given names.</param>
    /// <returns></returns>
    private TypeHint TypeHint(string[] genericTypes) {
        if (!Match(TokenType.Identifier, TokenType.Keyword)) {
            throw Error($"Expected type name (identifier or keyword) for type hint", ErrorType.SyntaxError);
        }

        Token token = Previous;
        List<TypeHint> parameters = [];

        bool generic = genericTypes.Contains(token.Value);

        if (Match(TokenType.LessThan)) {
            // parameters
            MaybeSome(TokenType.Whitespace);

            // make sure we have at least one parameter
            parameters.Add(TypeHint());
            MaybeSome(TokenType.Whitespace);

            // parse more parameters separated by comma
            while (Match(TokenType.Comma)) {
                MaybeSome(TokenType.Whitespace);
                parameters.Add(TypeHint());
                MaybeSome(TokenType.Whitespace);
            }

            Consume(TokenType.GreaterThan, "Expected '>' after generic type parameters");
            MaybeSome(TokenType.Whitespace);
        }

        //nullable?
        bool nullable = false;
        if (Match(TokenType.QuestionMark)) {
            MaybeSome(TokenType.Whitespace);
            nullable = true;
        }

        return new TypeHint(token, [.. parameters], nullable, generic);
    }

	protected ExpressionStmt ExpressionStatement() {
		Expr expr = Expression();
		return new ExpressionStmt(expr);
	}

	private Expr Expression()
	{
		return Assignment();
	}

	private Expr Assignment() {
		Expr expr = Or();

		MaybeSome(TokenType.Whitespace);

		if (Match(TokenType.Assign, TokenType.PlusAssign, TokenType.MinusAssign, TokenType.StarAssign, TokenType.SlashAssign)) {
			Token op = Previous;

			MaybeSome(TokenType.Whitespace);

			Expr valueExpression = Assignment();

			if (expr is Identifier identifier) {
				return new Assignment(op, identifier, valueExpression);
			}else if (expr is Get get) {
				return new Set(op, get.Expression, get.Identifier, valueExpression);
			}else if (expr is BracketGet bracketGet) {
				return new BracketSet(op, bracketGet.Target, bracketGet.Element, valueExpression);
			}

			// error, but we don't throw it because the parser isn't in a confused state
			Error("Invalid assignment target.", ErrorType.RuntimeError);
		}

		return expr;
	}

	private Expr Or() {
		Expr expr = And();

		MaybeSome(TokenType.Whitespace);

		while (MatchKeyword("or")) {
			Token op = Previous;

			MaybeSome(TokenType.Whitespace);

			Expr right = And();
			expr = new Logical(expr, op, right);
			MaybeSome(TokenType.Whitespace);
		}

		return expr;
	}

	private Expr And() {
		Expr expr = Equality();

		MaybeSome(TokenType.Whitespace);

		while (MatchKeyword("and")) {
			Token op = Previous;
			MaybeSome(TokenType.Whitespace);

			Expr right = Equality();
			expr = new Logical(expr, op, right);
			MaybeSome(TokenType.Whitespace);
		}

		return expr;
	}

	private Expr Equality()
	{
		Expr expr = Comparison();

		MaybeSome(TokenType.Whitespace);

		while (Match(TokenType.NotEqual, TokenType.Equal))
		{
			Token op = Previous;

			MaybeSome(TokenType.Whitespace);

			Expr right = Comparison();
			expr = new Binary(expr, op, right);
			MaybeSome(TokenType.Whitespace);
		}

		return expr;
	}

	private Expr Comparison()
	{
		Expr expr = Term();

		MaybeSome(TokenType.Whitespace);

		while (Match(TokenType.GreaterThan, TokenType.GreaterThanOrEqual, TokenType.LessThan, TokenType.LessThanOrEqual) || MatchKeyword("is"))
		{
			Token op = Previous;

			MaybeSome(TokenType.Whitespace);

			if (op.Value == "is") {
				// Parse type hint after 'is' keyword
				TypeHint type = TypeHint();
				expr = new TypeCheck(op, expr, type);
			} else {
				Expr right = Term();
				expr = new Binary(expr, op, right);
			}
			
			MaybeSome(TokenType.Whitespace);
		}

		return expr;
	}

	private Expr Term()
	{
		Expr expr = Factor();

		MaybeSome(TokenType.Whitespace);

		while (Match(TokenType.Minus, TokenType.Plus))
		{
			Token op = Previous;

			MaybeSome(TokenType.Whitespace);

			Expr right = Factor();
			expr = new Binary(expr, op, right);
			MaybeSome(TokenType.Whitespace);
		}

		return expr;
	}

	private Expr Factor()
	{
		Expr expr = Unary();

		MaybeSome(TokenType.Whitespace);

		while (Match(TokenType.Slash, TokenType.Star))
		{
			Token op = Previous;

			MaybeSome(TokenType.Whitespace);

			Expr right = Unary();
			expr = new Binary(expr, op, right);
			MaybeSome(TokenType.Whitespace);
		}

		return expr;
	}

    private Expr Unary()
    {
        // await expression
        if (MatchKeyword("await"))
        {
            Token awaitToken = Previous;
            MaybeSome(TokenType.Whitespace);
            Expr expr = Unary();
            return new Await(awaitToken, expr);
        }

        // Type cast using parentheses
        if (Match(TokenType.OpenParen))
        {
            Token leftParen = Previous;
            MaybeSome(TokenType.Whitespace);

            // Look ahead to see if this is a type cast or just a grouping
            int savedIndex = _index;
            Error[] savedErrors = Errors.ToArray();
            try {
                // Try parsing as type hint
                TypeHint type = TypeHint();
                MaybeSome(TokenType.Whitespace);
                Consume(TokenType.CloseParen, "Expected ')' after type in cast expression");
                MaybeSome(TokenType.Whitespace);
                Expr expr = Unary();
                return new TypeCast(leftParen, type, expr);
            }
            catch {
                // If parsing as type hint fails, restore index and treat as grouping
                _index = savedIndex;
                Errors.Clear();
                Errors.AddRange(savedErrors);

                Expr expr = Expression();
                MaybeSome(TokenType.Whitespace);
                Consume(TokenType.CloseParen, "Expected a matching ')' after expression.");
                return new Grouping(expr);
            }
        }

        // unary or negation
        if (Match(TokenType.Minus) || MatchKeyword("not"))
        {
            Token op = Previous;

            MaybeSome(TokenType.Whitespace);

            Expr right = Unary();
            return new Unary(op, right);
        }
        
        // class instantiation
        if (MatchKeyword("new"))
        {
            Token newKeyword = Previous;
            MaybeSome(TokenType.Whitespace);

            // Parse the class name and any type parameters
            Expr classExpr = Primary();
            List<Expr> parameters = [];

            // Check for type/value parameters
            if (Match(TokenType.LessThan))
            {
                MaybeSome(TokenType.Whitespace);

                // Parse first parameter
                parameters.Add(Primary());
                MaybeSome(TokenType.Whitespace);

                // Parse additional parameters
                while (Match(TokenType.Comma))
                {
                    MaybeSome(TokenType.Whitespace);
                    parameters.Add(Primary());
                    MaybeSome(TokenType.Whitespace);
                }

                Consume(TokenType.GreaterThan, "Expected '>' after type parameters");
                MaybeSome(TokenType.Whitespace);
            }

            // Parse constructor arguments
            List<Expr> arguments = [];
            if (Match(TokenType.OpenParen))
            {
                MaybeSome(TokenType.Whitespace);

                if (!Check(TokenType.CloseParen))
                {
                    do
                    {
                        MaybeSome(TokenType.Whitespace);
                        arguments.Add(Expression());
                        MaybeSome(TokenType.Whitespace);
                    } while (Match(TokenType.Comma));
                }

                Consume(TokenType.CloseParen, "Expected ')' after constructor arguments");
            }

            return new Instantiation(newKeyword, new Call(newKeyword, classExpr, [..arguments]), parameters);
        }

        return Call();
    }

	private Expr Call() {
		Expr expr = Primary();

		while (true) {
			MaybeSome(TokenType.Whitespace);

			if (Match(TokenType.OpenParen)) {
				expr = FinishCall(expr);
			}
			else if (Match(TokenType.Dot)) {
				Token name = Consume(TokenType.Identifier, "Expect property name after '.'");
				expr = new Get(expr, name);
			}
			else if (Match(TokenType.OpenBracket)) {
				MaybeSome(TokenType.Whitespace);
				Expr element = Expression();
				MaybeSome(TokenType.Whitespace);
				Consume(TokenType.CloseBracket, "Expected ']' after collection key or index");
				expr = new BracketGet(expr, element);
			}
			else {
				break;
			}
		}

		return expr;
	}

	private Call FinishCall(Expr callee) {
		List<Expr> arguments = [];

		MaybeSome(TokenType.Whitespace);

		if ( ! Check(TokenType.CloseParen)) {

			do {
				MaybeSome(TokenType.Whitespace);
				arguments.Add(Expression());
				MaybeSome(TokenType.Whitespace);

			} while (Match(TokenType.Comma));
		}

		MaybeSome(TokenType.Whitespace);
		
		Token paren = Consume(TokenType.CloseParen, "Expectd ')' after function arguments");

		return new Call(paren, callee, [.. arguments]);
	}

	private Expr Primary()
	{
		if (MatchKeyword("false")) return new Literal(Literal.LiteralKind.Bool, false, Previous);
		if (MatchKeyword("true")) return new Literal(Literal.LiteralKind.Bool, true, Previous);
		if (MatchKeyword("null")) return new Literal(Literal.LiteralKind.Null, null, Previous);
		if (MatchKeyword("this")) return new This(Previous);

		if (Match(TokenType.Identifier))
		{
			return new Identifier(Previous);
		}

		if (Match(TokenType.Keyword)) {
			return new Identifier(Previous);
		}

		if (Match(TokenType.IntLiteral)) {
			return new Literal(Literal.LiteralKind.Int, Previous.Value, Previous);
		}

		if (Match(TokenType.FloatLiteral)) {
			return new Literal(Literal.LiteralKind.Float, Previous.Value, Previous);
		}

		if (Match(TokenType.StringLiteral))
		{
			return new Literal(Literal.LiteralKind.String, Previous.Value, Previous);
		}

		if (Match(TokenType.OpenParen))
		{
			MaybeSome(TokenType.Whitespace);
			Expr expr = Expression();
			MaybeSome(TokenType.Whitespace);

			Consume(TokenType.CloseParen, "Expected a matching ')' after expression.");
			
			return new Grouping(expr);
		}

		throw Error("Expected expression.");
	}
}
