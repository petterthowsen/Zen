namespace Zen.Parsing;

using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST;
using Zen.Parsing.AST.Expressions;
using Zen.Parsing.AST.Statements;

public class Parser
{

	public enum ParsingContext {
		Default,
		Class,
		Function,
	}

	private ParsingContext Context = ParsingContext.Default;

	private List<Token> Tokens = [];

	public readonly List<Error> Errors = [];

	private int _index = 0;


	protected Token Current => Tokens[_index];

	protected Token Peek(int offset = 1) => Tokens[_index + offset];

	protected Token Next => Tokens[_index + 1];

	protected Token Previous => Tokens[_index - 1];

	protected bool IsAtEnd => Current.Type == TokenType.EOF;

	public ProgramNode Parse(List<Token> tokens)
	{
		Tokens = tokens;
		_index = 0;
		Errors.Clear();
		Context = ParsingContext.Default;
		ProgramNode program = new();


		try {
			while ( ! IsAtEnd) {
				program.Statements.Add(Statement());
				MaybeSome(TokenType.Whitespace, TokenType.Newline);
			}

			return program;
		} catch (Error _err) {
			//Synchronize();
			//return Parse();
			return program;
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

	private bool MatchKeyword(string keyword)
	{
		if (Current.Type == TokenType.Keyword && Current.Value == keyword)
		{
			Advance();
			return true;
		}
		return false;
	}

	private Stmt Statement() {
		if (MatchKeyword("var") || MatchKeyword("const")) return VarStatement();
		if (MatchKeyword("print")) return PrintStatement();
		if (MatchKeyword("if")) return IfStatement();
		if (MatchKeyword("while")) return WhileStatement();
		if (MatchKeyword("for")) return ForStatement();
		return ExpressionStatement();
	}

	private VarStmt VarStatement(Token? startingToken = null) {
		Token token = startingToken ?? Previous;

		AtleastOne(TokenType.Whitespace); // spaces

		Token identifier = Consume(TokenType.Identifier, $"Expected some identifier after '{token.Value}' declaration"); // identifier
		MaybeSome(TokenType.Whitespace); // any spaces

		// if we find a : then we have a TypeHint
		TypeHint? typeHint = null;
		if (Match(TokenType.Colon)) {
			MaybeSome(TokenType.Whitespace);
			typeHint = TypeHint();
		}
		MaybeSome(TokenType.Whitespace);

		// if we find a =, then we have an initializer expression
		Expr? initializer = null;
		
		if (Match(TokenType.Assign)) {
			AtleastOne(TokenType.Whitespace);
			initializer = Expression();
		}

		return new VarStmt(token, identifier, typeHint, initializer);
	}

	private TypeHint TypeHint() {
		if ( ! Match(TokenType.Identifier, TokenType.Keyword)) {
			throw Error($"Expected type name (identifier or keyword) for type hint", ErrorType.SyntaxError);
		}

		Token token = Previous;

		List<TypeHint> parameters = [];

		// generic?
		if (Match(TokenType.LessThan)) {
			// parameters
			MaybeSome(TokenType.Whitespace);


			// make sure we have at least one parameter
			if ( ! Check(TokenType.Identifier, TokenType.Keyword)) {
				throw Error($"Expected at least one identifier or keyword after '<'", ErrorType.SyntaxError);
			}

			parameters.Add(TypeHint());

			MaybeSome(TokenType.Whitespace);

			// parse more parameters separated by comma
			while ( Match(TokenType.Comma) ) {
				MaybeSome(TokenType.Whitespace);

				if ( ! Check(TokenType.Identifier, TokenType.Keyword)) {
					throw Error($"Expected at least one identifier or keyword after ','", ErrorType.SyntaxError);
				}

				parameters.Add(TypeHint());

				MaybeSome(TokenType.Whitespace);
			}

			Consume(TokenType.GreaterThan, "Expected '>' after generic type parameters");
		}

		return new TypeHint(token, [.. parameters]);
	}

	private PrintStmt PrintStatement() {
		Token token = Previous;
		
		AtleastOne(TokenType.Whitespace);

		Expr expr = Expression();
		return new PrintStmt(token, expr);
  	}

	private Block Block() {
		Token openBrace = Previous;

		MaybeSome(TokenType.Whitespace, TokenType.Newline);

		List<Stmt> statements = [];
		while ( ! Check(TokenType.CloseBrace) && !IsAtEnd) {
			MaybeSome(TokenType.Whitespace, TokenType.Newline);
			statements.Add(Statement());
			MaybeSome(TokenType.Whitespace, TokenType.Newline);
		}

		return new Block(openBrace, [.. statements]);
	}

	private IfStmt IfStatement() {
		Token token = Previous; // if
		
		AtleastOne(TokenType.Whitespace); // spaces

		Expr condition = Expression(); // condition

		MaybeSome(TokenType.Whitespace, TokenType.Newline); // any spaces/newlines

		Consume(TokenType.OpenBrace, "Expected '{' after 'if'"); // {

		Block thenBranch = Block();

		Consume(TokenType.CloseBrace, "Expected '}' after block"); // }
		
		MaybeSome(TokenType.Whitespace, TokenType.Newline); // spaces/newlines

		IfStmt ifStmt = new IfStmt(token, condition, thenBranch);

		List<IfStmt> elseIfs = [];

		// else if blocks
		while (MatchKeywordSequence("else", "if")) {
			AtleastOne(TokenType.Whitespace);

			// condition
			Expr elseIfCondition = Expression();

			// spaces or newlines
			MaybeSome(TokenType.Whitespace, TokenType.Newline);

			// {
			Consume(TokenType.OpenBrace, "Expected '{' after 'else if'");
			
			// parse block...
			Block elseIfBlock = Block();

			// consume }
			elseIfBlock.CloseBrace = Consume(TokenType.CloseBrace, "Expected '}' after block");

			// spaces or newlines
			MaybeSome(TokenType.Whitespace, TokenType.Newline);

			IfStmt elseIf = new IfStmt(Previous, elseIfCondition, elseIfBlock);
			elseIfs.Add(elseIf);
		}

		ifStmt.ElseIfs = [.. elseIfs];

		// else block?

		if (MatchKeyword("else")) {
			AtleastOne(TokenType.Whitespace);
			MaybeSome(TokenType.Whitespace, TokenType.Newline);

			Consume(TokenType.OpenBrace, "Expected '{' after 'else'");
			Block elseBlock = Block();
			elseBlock.CloseBrace = Consume(TokenType.CloseBrace, "Expected '}' after block");
			ifStmt.Else = elseBlock;
		}

		return ifStmt;
	}

	private WhileStmt WhileStatement() {
		// "while" keyword token
		Token token = Previous;

		// spaces
		AtleastOne(TokenType.Whitespace);

		// condition
		Expr condition = Expression();

		// spaces or newlines
		MaybeSome(TokenType.Whitespace, TokenType.Newline);

		// {
		Consume(TokenType.OpenBrace, "Expected '{' after 'while' condition");

		// parse block...
		Block block = Block();

		// consume }
		block.CloseBrace = Consume(TokenType.CloseBrace, "Expected '}' after block");

		// spaces or newlines
		MaybeSome(TokenType.Whitespace, TokenType.Newline);

		return new WhileStmt(token, condition, block);
	}

	/// <summary>
	/// Parses a traditional for loop with initializer, condition and incrementor.
	/// The "var" keyword is optional in the initializer.
	/// </summary>
	/// <returns>
	/// Either a ForStmt or a ForInStmt depending on the type of loop.
	/// </returns>
	protected Stmt ForStatement() {
		// traditional for loop:
		// for [var]? [identifier] = [expression]; [condition expr]; [incrementor expr] [block]
		//
		// for in loop:
		// for [var]? [identifier][, identifier]? in [expression] [block]

		// try traditional for loop first
		int saved_index = _index;
		Error[] saved_errors = Errors.ToArray();
		try {
			ForStmt forStmt = TraditionalForLoop();
			return forStmt;
		} catch {
			// restore index
			_index = saved_index;

			// restore errors
			Errors.Clear();
			Errors.AddRange(saved_errors);

			// try for in loop
			return ForInLoop();
		}
	}

	protected ForStmt TraditionalForLoop() {
		// "for" keyword token
		Token forToken = Previous;

		AtleastOne(TokenType.Whitespace);

		// var keyword is optional
		if (MatchKeyword("var")) {
			MaybeSome(TokenType.Whitespace);
		}else if (MatchKeyword("const")) {
			// error
			throw Error("`const` cannot be used as a loop variable", ErrorType.SyntaxError);
		}

		// loopIdentifier
		Token loopIdentifier = Consume(TokenType.Identifier, "Expected identifier after 'for' keyword");
		MaybeSome(TokenType.Whitespace);
		
		TypeHint? typeHint = null;
		
		// TypeHint is optional
		if (Match(TokenType.Colon)) {
			MaybeSome(TokenType.Whitespace);
			typeHint = TypeHint();
			MaybeSome(TokenType.Whitespace);
		}

		// =
		Consume(TokenType.Assign, "Expected '=' after loop variable identifier");
		MaybeSome(TokenType.Whitespace);

		// expression
		Expr initializer = Expression();
		MaybeSome(TokenType.Whitespace);

		// ;
		Consume(TokenType.Semicolon, "Expected ';' after loop variable initializer");
		MaybeSome(TokenType.Whitespace);

		// condition
		Expr condition = Expression();
		MaybeSome(TokenType.Whitespace);

		// ;
		Consume(TokenType.Semicolon, "Expected ';' after loop condition");
		MaybeSome(TokenType.Whitespace);

		// incrementor
		Expr incrementor = Expression();
		MaybeSome(TokenType.Whitespace, TokenType.Newline);

		// {
		Consume(TokenType.OpenBrace, "Expected '{' after loop condition");

		// parse block...
		Block block = Block();

		// consume }
		block.CloseBrace = Consume(TokenType.CloseBrace, "Expected '}' after block");

		if (typeHint == null) {
			return new ForStmt(forToken, loopIdentifier, initializer, condition, incrementor, block);
		}else {
			return new ForStmt(forToken, loopIdentifier, typeHint, initializer, condition, incrementor, block);
		}
	}

	protected ForInStmt ForInLoop() {
		// for in loop:
		// for [var]? [identifier][, identifier]? in [expression] [block]

		// "for" keyword token
		Token token = Previous;
		AtleastOne(TokenType.Whitespace);
		
		// var keyword is optional
		if (MatchKeyword("var")) {
			MaybeSome(TokenType.Whitespace);
		}else if (MatchKeyword("const")) {
			// error
			throw Error("`const` cannot be used as a loop variable", ErrorType.SyntaxError);
		}

		// valueIdentifier
		Token valueIdentifier;
		TypeHint? valueTypeHint = null;

		Token? keyIdentifier = null;
		TypeHint? keyTypeHint = null;

		valueIdentifier = Consume(TokenType.Identifier, "Expected identifier after 'for' keyword");
		MaybeSome(TokenType.Whitespace);

		// typehint?
		if (Match(TokenType.Colon)) {
			MaybeSome(TokenType.Whitespace);
			valueTypeHint = TypeHint();
			MaybeSome(TokenType.Whitespace);
		}


		// comma? if so we are looping over a key, value pair
		if (Match(TokenType.Comma)) {
			// the first identifier is the key in this case
			keyIdentifier = valueIdentifier;
			keyTypeHint = valueTypeHint;

			MaybeSome(TokenType.Whitespace);
			valueIdentifier = Consume(TokenType.Identifier, "Expected identifier after ','");
			MaybeSome(TokenType.Whitespace);

			// typehint?
			if (Match(TokenType.Colon)) {
				MaybeSome(TokenType.Whitespace);
				valueTypeHint = TypeHint();
				MaybeSome(TokenType.Whitespace);
			}
		}


		// "in" keyword
		if (!MatchKeyword("in")) {
			// error
			throw Error("Expected 'in' after loop variable identifier", ErrorType.SyntaxError);
		}

		MaybeSome(TokenType.Whitespace);

		// expression
		Expr expression = Expression();
		MaybeSome(TokenType.Whitespace, TokenType.Newline);

		// {
		Consume(TokenType.OpenBrace, "Expected '{' after loop condition");

		// parse block...
		Block block = Block();

		// consume }
		block.CloseBrace = Consume(TokenType.CloseBrace, "Expected '}' after block");

		// key, value pair?
		if (keyIdentifier != null) {
			return new ForInStmt(token, keyIdentifier, keyTypeHint, valueIdentifier, valueTypeHint, expression, block);
		}else {
			return new ForInStmt(token, valueIdentifier, valueTypeHint, expression, block);
		}
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
		Expr expr = Equality();

		MaybeSome(TokenType.Whitespace);

		if (Match(TokenType.Assign, TokenType.PlusAssign, TokenType.MinusAssign, TokenType.StarAssign, TokenType.SlashAssign)) {
			Token op = Previous;

			MaybeSome(TokenType.Whitespace);

			Expr valueExpression = Assignment();

			if (expr is Identifier identifier) {
				return new Assignment(op, identifier, valueExpression);
			}

			// error, but we don't throw it because the parser isn't in a confused state
			Error("Invalid assignment target.", ErrorType.RuntimeError);
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
		}

		return expr;
	}

	private Expr Comparison()
	{
		Expr expr = Term();

		MaybeSome(TokenType.Whitespace);

		while (Match(TokenType.GreaterThan, TokenType.GreaterThanOrEqual, TokenType.LessThan, TokenType.LessThanOrEqual))
		{
			Token op = Previous;

			MaybeSome(TokenType.Whitespace);

			Expr right = Term();
			expr = new Binary(expr, op, right);
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
		}

		return expr;
	}

	private Expr Unary()
	{
		if (Match(TokenType.Minus) || MatchKeyword("not"))
		{
			Token op = Previous;

			MaybeSome(TokenType.Whitespace);

			Expr right = Unary();
			return new Unary(op, right);
		}

		return Primary();
	}

	private Expr Primary()
	{
		if (MatchKeyword("false")) return new Literal(Literal.LiteralKind.Bool, false, Previous);
		if (MatchKeyword("true")) return new Literal(Literal.LiteralKind.Bool, true, Previous);
		if (MatchKeyword("null")) return new Literal(Literal.LiteralKind.Null, null, Previous);

		if (Match(TokenType.Identifier))
		{
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
}