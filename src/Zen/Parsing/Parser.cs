namespace Zen.Parsing;

using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST;
using Zen.Parsing.AST.Expressions;
using Zen.Parsing.AST.Statements;
using Zen.Typing;

public class Parser
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

	private ImportStmt ImportStatement() {
		Token token = Previous;
		AtleastOne(TokenType.Whitespace);

		var path = new List<string>();

		// Parse import path (e.g., "MyPackage/Utils/Module")
		do {
			var name = Consume(TokenType.Identifier, "Expected module name.");
			path.Add(name.Value);
		} while (Match(TokenType.Slash, TokenType.Dot));

		MaybeSome(TokenType.Whitespace);

		Token? alias = null;
		if (MatchKeyword("as")) {
			AtleastOne(TokenType.Whitespace);
			alias = Consume(TokenType.Identifier, "Expected alias name after 'as'.");
		}

		return new ImportStmt(token, path.ToArray(), alias);
	}

	private FromImportStmt FromImportStatement() {
		Token token = Previous; // "from" keyword token
		AtleastOne(TokenType.Whitespace);

		var path = new List<string>();
		var symbols = new List<Token>();

		// Parse import path (e.g., "MyPackage/Utils/Module")
		do {
			var name = Consume(TokenType.Identifier, "Expected module name.");
			path.Add(name.Value);
		} while (Match(TokenType.Slash, TokenType.Dot));

		// parse "import" token
		AtleastOne(TokenType.Whitespace);
		Consume(TokenType.Keyword, "Expected 'import' keyword after path.");
		MaybeSome(TokenType.Whitespace);

		// Parse symbols (e.g., "MyClass", "myFunction")
		do {
			var name = Consume(TokenType.Identifier, "Expected symbol name.");
			symbols.Add(name);
		} while (Match(TokenType.Comma));

		return new FromImportStmt(token, path.ToArray(), [..symbols]);
	}

	private PackageStmt PackageStatement() {
		Token token = Previous;
		AtleastOne(TokenType.Whitespace);

		var path = new List<string>();

		// Parse package path (e.g., "MyPackage/SubPackage")
		do {
			var name = Consume(TokenType.Identifier, "Expected package name.");
			path.Add(name.Value);
		} while (Match(TokenType.Slash));

		return new PackageStmt(token, path.ToArray());
	}

	private VarStmt VarStatement(Token? startingToken = null) {
		Token token = startingToken ?? Previous;

		AtleastOne(TokenType.Whitespace); // spaces

		if ( ! Match(TokenType.Identifier, TokenType.Keyword)) {
			throw Error($"Expected some identifier after '{token.Value}' declaration", ErrorType.SyntaxError);
		}
		Token identifier = Previous; // identifier

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

	private TypeHint TypeHint(bool silent = false) {
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

			MaybeSome(TokenType.Whitespace);
		}

		//nullable?
		bool nullable = false;
		if (Match(TokenType.QuestionMark)) {
			MaybeSome(TokenType.Whitespace);
			nullable = true;
		}

		return new TypeHint(token, [.. parameters], nullable);
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

			// try for in loop
			ForInStmt forInStmt = ForInLoop();

			// restore errors
			Errors.Clear();
			Errors.AddRange(saved_errors);

			return forInStmt;
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

	protected ClassStmt ClassStatement() {
		// "class" keyword token
		Token token = Previous;
		AtleastOne(TokenType.Whitespace);

		// identifier
		if ( ! Match(TokenType.Identifier, TokenType.Keyword)) {
			throw Error($"Expected 'Identifier' or 'Keyword' for class name after 'class' keyword", ErrorType.SyntaxError);
		}

		Token identifier = Previous;

		MaybeSome(TokenType.Whitespace, TokenType.Newline);
		
		// open brace
		Consume(TokenType.OpenBrace, "Expected '{' after class declaration");
		MaybeSome(TokenType.Whitespace, TokenType.Newline);

		// properties and methods
		List<MethodStmt> methods = [];
		List<PropertyStmt> properties = [];

		// parse untill we find a close brace
		while ( ! Check(TokenType.CloseBrace) && ! IsAtEnd) {

			if (CheckMethodDeclaration()) {
				methods.Add(MethodStatement());
			}else {
				properties.Add(PropertyStatement());
			}

			MaybeSome(TokenType.Whitespace, TokenType.Newline);
		}

		// close brace
		Consume(TokenType.CloseBrace, "Expected '}' after block");

		return new ClassStmt(token, identifier, [.. properties], [.. methods]);
	}

	private static readonly string[] methodModifiers = ["async", "public", "protected", "private", "abstract", "override", "final"];
	private static readonly string[] propertyModifiers = ["public", "protected", "private", "readonly"];

	private bool CheckMethodDeclaration() {
		int index = _index;

		while (Current.Type == TokenType.Keyword && methodModifiers.Contains(Current.Value)) {
			Advance();
			MaybeSome(TokenType.Whitespace);
		}

		// identifier
		if ( ! Match(TokenType.Identifier, TokenType.Keyword)) {
			_index = index;
			return false;
		}

		MaybeSome(TokenType.Whitespace);

		// open paren
		if ( ! Match(TokenType.OpenParen)) {
			_index = index;
			return false;
		}

		_index = index;
		return true;
	}

	protected MethodStmt MethodStatement() {
		List<Token> modifiers = [];

		while (Current.Type == TokenType.Keyword && methodModifiers.Contains(Current.Value)) {
			modifiers.Add(Current);
			Advance();
			MaybeSome(TokenType.Whitespace);
		}

		// identifier
		Token identifier;
		if ( Match(TokenType.Identifier, TokenType.Keyword)) {
			identifier = Previous;
		} else {
			throw Error($"Expected 'Identifier' or 'Keyword' for method name after modifiers", ErrorType.SyntaxError);
		}

		// open paren
		Token openParen = Consume(TokenType.OpenParen, "Expected '(' after method identifier");
		MaybeSome(TokenType.Whitespace);

		// parameters
		FuncParameter[] parameters = FuncParameters();

		// close paren
		Token closeParen = Consume(TokenType.CloseParen, "Expected ')' after function parameters");
		MaybeSome(TokenType.Whitespace);

		// return type?
		TypeHint? returnTypeTypeHint = null;

		if (Match(TokenType.Colon)) {
			MaybeSome(TokenType.Whitespace);
			returnTypeTypeHint = TypeHint();
			MaybeSome(TokenType.Whitespace);
		}else {
			returnTypeTypeHint = new TypeHint(new Token(TokenType.StringLiteral, "void", identifier.Location), false);
		}

		// block
		Consume(TokenType.OpenBrace, "Expected '{' after function parameters");
		Block block = Block();

		// consume }
		block.CloseBrace = Consume(TokenType.CloseBrace, "Expected '}' after block");

		return new MethodStmt(identifier, returnTypeTypeHint, parameters, block, [..modifiers]);
	}

	protected PropertyStmt PropertyStatement() {
		Token[] modifiers = [];

		while (Current.Type == TokenType.Keyword && propertyModifiers.Contains(Current.Value)) {
			modifiers.Append(Current);
			Advance();
			MaybeSome(TokenType.Whitespace);
		}

		// identifier
		Token identifier;
		if ( Match(TokenType.Identifier, TokenType.Keyword)) {
			identifier = Previous;
		} else {
			throw Error($"Expected 'Identifier' or 'Keyword' for method name after modifiers", ErrorType.SyntaxError);
		}

		MaybeSome(TokenType.Whitespace);

		// typehint?
		TypeHint? typeHint = null;

		if (Match(TokenType.Colon)) {
			MaybeSome(TokenType.Whitespace);
			typeHint = TypeHint();
			MaybeSome(TokenType.Whitespace);
		}

		// initializer ?
		Expr? initializer = null;

		if (Match(TokenType.Assign)) {
			MaybeSome(TokenType.Whitespace);
			initializer = Expression();
			MaybeSome(TokenType.Whitespace);
		}

		return new PropertyStmt(identifier, typeHint, initializer, modifiers);
	}

	protected FuncStmt FuncStatement(bool async) {
		// "func" keyword token
		Token token = Previous;
		AtleastOne(TokenType.Whitespace);

		// identifier
		Token identifier = Consume(TokenType.Identifier, "Expected identifier after 'func' keyword");
		
		// open paren
		Consume(TokenType.OpenParen, "Expected '(' after function identifier");
		MaybeSome(TokenType.Whitespace);

		// parameters
		FuncParameter[] parameters = FuncParameters();

		// close paren
		Consume(TokenType.CloseParen, "Expected ')' after function parameters");
		MaybeSome(TokenType.Whitespace);

		// return type?
		TypeHint? returnTypeTypeHint = null;

		if (Match(TokenType.Colon)) {
			MaybeSome(TokenType.Whitespace);
			returnTypeTypeHint = TypeHint();
			MaybeSome(TokenType.Whitespace);
		}

		// block
		Consume(TokenType.OpenBrace, "Expected '{' after function parameters");
		Block block = Block();
		Consume(TokenType.CloseBrace, "Expected '}' after block");

		if (returnTypeTypeHint != null) {
			return new FuncStmt(async, identifier, returnTypeTypeHint, parameters, block);
		}else {
			return new FuncStmt(async, identifier, ZenType.Void, parameters, block);
		}
	}

	protected FuncParameter[] FuncParameters() {
		List<FuncParameter> parameters = new List<FuncParameter>();
		
		while (!Check(TokenType.CloseParen)) {
			parameters.Add(FuncParameter());

			if (!Match(TokenType.Comma)) {
				break;
			}

			MaybeSome(TokenType.Whitespace, TokenType.Newline);
		}

		return [..parameters];
	}

	protected ReturnStmt ReturnStatement() {
		// "return" keyword token
		Token token = Previous;
		AtleastOne(TokenType.Whitespace);

		// expression?
		Expr? expr = null;
		if ( ! Check(TokenType.Semicolon, TokenType.Newline)) {
			expr = Expression();
		}

		return new ReturnStmt(token, expr?? null);
	}

	protected FuncParameter FuncParameter() {
		// identifier [:typehint]? [ = defaultValue]?

		// identifier
		Token identifier = Consume(TokenType.Identifier, "Expected identifier after 'func' keyword");
		MaybeSome(TokenType.Whitespace);

		// typehint?
		TypeHint? typeHint = null;
		if (Match(TokenType.Colon)) {
			MaybeSome(TokenType.Whitespace);
			typeHint = TypeHint();
			MaybeSome(TokenType.Whitespace);
		}

		// defaultValue?
		Expr? defaultValue = null;
		if (Match(TokenType.Assign)) {
			MaybeSome(TokenType.Whitespace);
			defaultValue = Expression();
			MaybeSome(TokenType.Whitespace);
		}

		return new FuncParameter(identifier, typeHint, defaultValue);
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

            Call call = (Call) Call(); // Use Call to handle potential nested calls or nested instantiations
            
            return new Instantiation(newKeyword, call);
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
}