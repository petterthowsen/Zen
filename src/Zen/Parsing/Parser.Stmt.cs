namespace Zen.Parsing;

using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST;
using Zen.Parsing.AST.Expressions;
using Zen.Parsing.AST.Statements;
using Zen.Typing;

public partial class Parser
{
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

	private TypeStmt TypeStatement() {
		// "type" keyword token
		Token token = Previous;
		AtleastOne(TokenType.Whitespace);

		// Parse the type name identifier
		Identifier name = Identifier();
		MaybeSome(TokenType.Whitespace);

		// Expect =
		Consume(TokenType.Assign, "Expected '=' after type name");
		MaybeSome(TokenType.Whitespace);

		// Parse the first type in the union
		var types = new List<Identifier>();
		types.Add(Identifier());
		MaybeSome(TokenType.Whitespace);

		// Parse additional types in the union separated by 'or'
		while (MatchKeyword("or")) {
			MaybeSome(TokenType.Whitespace);
			types.Add(Identifier());
			MaybeSome(TokenType.Whitespace);
		}

		return new TypeStmt(token, name, [..types]);
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

	protected InterfaceStmt InterfaceStatement() {
		// "interface" keyword token
		Token token = Previous;
		AtleastOne(TokenType.Whitespace);

		// identifier
		if ( ! Match(TokenType.Identifier, TokenType.Keyword)) {
			throw Error($"Expected 'Identifier' or 'Keyword' for class name after 'class' keyword", ErrorType.SyntaxError);
		}

		Token identifier = Previous;

		// Parametric Parameters?
		List<ParameterDeclaration> parameters = [];
        if (Match(TokenType.LessThan)) {
			do {
				MaybeSome(TokenType.Whitespace);
				parameters.Add(ParseParameter());
				MaybeSome(TokenType.Whitespace);
			} while(Match(TokenType.Comma));

			MaybeSome(TokenType.Whitespace);

			Consume(TokenType.GreaterThan, "Expected '>' after class generic parameters");
        }


		MaybeSome(TokenType.Whitespace, TokenType.Newline);

		// open brace
		Consume(TokenType.OpenBrace, "Expected '{' after class declaration");
		MaybeSome(TokenType.Whitespace, TokenType.Newline);

		// properties and methods
		List<AbstractMethodStmt> methods = [];
		
		// gather genericp arameter names
        string[] generics = parameters.Select((p) => p.Name).ToArray();
	
		// parse untill we find a close brace
		while ( ! Check(TokenType.CloseBrace) && ! IsAtEnd) {
            
			methods.Add(AbstractMethodStatement(generics));
			MaybeSome(TokenType.Whitespace, TokenType.Newline);
		}

		// close brace
		Consume(TokenType.CloseBrace, "Expected '}' after block");

		return new InterfaceStmt(token, identifier, [..methods], [..parameters]);
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
        
        // Parametric Parameters?
        List<ParameterDeclaration> parameters = [];
        if (Match(TokenType.LessThan)) {
			do {
				MaybeSome(TokenType.Whitespace);
				parameters.Add(ParseParameter());
				MaybeSome(TokenType.Whitespace);
			} while(Match(TokenType.Comma));

			MaybeSome(TokenType.Whitespace);

			Consume(TokenType.GreaterThan, "Expected '>' after class generic parameters");
        }

		MaybeSome(TokenType.Whitespace, TokenType.Newline);

		// extends ?
		Identifier? extendsIdentifier = null;

		if (MatchKeyword("extends")) {
			
			Token extendsToken = Previous;
			MaybeSome(TokenType.Whitespace);
			if (Check(TokenType.Identifier, TokenType.Keyword)) {
				extendsIdentifier = Identifier();
			}else {
				throw Error($"Expected Identifier or Keyword for class name after 'extends' keyword", ErrorType.SyntaxError);
			}
			MaybeSome(TokenType.Whitespace, TokenType.Newline);
		}

		// implements ?
		List<ImplementsExpr> implements = [];

		// parse one or more identifiers separated by either ',' token or keyword token 'and'
		if (MatchKeyword("implements")) {
			do {
				MaybeSome(TokenType.Whitespace);
				if ( ! Check(TokenType.Identifier, TokenType.Keyword)) {
					throw Error($"Expected 'Identifier' or 'Keyword' for implemented interface after 'implements' keyword", ErrorType.SyntaxError);
				}
				implements.Add(ImplementsExpression());
				MaybeSome(TokenType.Whitespace);
			} while (MatchKeyword("and") || Match(TokenType.Comma));
		}

		MaybeSome(TokenType.Whitespace, TokenType.Newline);
		
		// open brace
		Consume(TokenType.OpenBrace, "Expected '{' after class declaration");
		MaybeSome(TokenType.Whitespace, TokenType.Newline);

		// properties and methods
		List<MethodStmt> methods = [];
		List<PropertyStmt> properties = [];
		
		// gather generic parameter names
        string[] generics = parameters.Select((p) => p.Name).ToArray();

		// parse untill we find a close brace
		while ( ! Check(TokenType.CloseBrace) && ! IsAtEnd) {
            
			if (CheckMethodDeclaration()) {
				methods.Add(MethodStatement(generics));
			}else {
				properties.Add(PropertyStatement(generics));
			}

			MaybeSome(TokenType.Whitespace, TokenType.Newline);
		}

		// close brace
		Consume(TokenType.CloseBrace, "Expected '}' after block");

		return new ClassStmt(token, identifier, [.. properties], [.. methods], [.. parameters], [/*modifiers*/], extendsIdentifier, [..implements]);
	}

	private ImplementsExpr ImplementsExpression() {
		Token token = Previous; // 'implements' keyword
		MaybeSome(TokenType.Whitespace);

		// identifier for the interface name
		Identifier identifier = Identifier();
		
		// paremeters?
		List<Expr> parameters = [];

		if (Match(TokenType.LessThan)) {
			while ( ! Check(TokenType.GreaterThan)) {
				parameters.Add(Primary());

				if (!Match(TokenType.Comma)) {
					break;
				}

				MaybeSome(TokenType.Whitespace, TokenType.Newline);
			}

			Consume(TokenType.GreaterThan, "Expected '>' after generic parameters");
		}

		return new ImplementsExpr(identifier, [..parameters]);
	}

	private static readonly string[] methodModifiers = ["async", "public", "protected", "private", "abstract", "override", "final"];
	private static readonly string[] propertyModifiers = ["public", "protected", "private"];

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

	protected MethodStmt MethodStatement(string[] generics) {
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
		MaybeSome(TokenType.Whitespace, TokenType.Newline);

		// parameters
		FuncParameter[] parameters = FuncParameters(generics);

		// close paren
		Token closeParen = Consume(TokenType.CloseParen, "Expected ')' after function parameters");
		MaybeSome(TokenType.Whitespace, TokenType.Newline);

		// return type?
		TypeHint? returnTypeTypeHint;

		if (Match(TokenType.Colon)) {
			MaybeSome(TokenType.Whitespace);
			returnTypeTypeHint = TypeHint(generics);
			MaybeSome(TokenType.Whitespace);
		}else {
			returnTypeTypeHint = new TypeHint(new Token(TokenType.StringLiteral, "void", identifier.Location), false);
		}

		MaybeSome(TokenType.Whitespace, TokenType.Newline);

		// block
		Consume(TokenType.OpenBrace, "Expected '{' after function parameters");
		Block block = Block();

		// consume }
		block.CloseBrace = Consume(TokenType.CloseBrace, "Expected '}' after block");

		return new MethodStmt(identifier, returnTypeTypeHint, parameters, block, [..modifiers]);
	}

	protected AbstractMethodStmt AbstractMethodStatement(string[] generics) {
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
		MaybeSome(TokenType.Whitespace, TokenType.Newline);

		// parameters
		FuncParameter[] parameters = FuncParameters(generics);

		// close paren
		Token closeParen = Consume(TokenType.CloseParen, "Expected ')' after function parameters");
		MaybeSome(TokenType.Whitespace, TokenType.Newline);

		// return type?
		TypeHint? returnTypeTypeHint;

		if (Match(TokenType.Colon)) {
			MaybeSome(TokenType.Whitespace);
			returnTypeTypeHint = TypeHint(generics);
			MaybeSome(TokenType.Whitespace);
		}else {
			returnTypeTypeHint = new TypeHint(new Token(TokenType.StringLiteral, "void", identifier.Location), false);
		}

		MaybeSome(TokenType.Whitespace, TokenType.Newline);

		return new AbstractMethodStmt(identifier, returnTypeTypeHint, parameters, [..modifiers]);
	}

	protected PropertyStmt PropertyStatement(string[] generics) {
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
			typeHint = TypeHint(generics);
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

    protected FuncParameter[] FuncParameters()
    {
        return FuncParameters([]);
    }

	protected FuncParameter[] FuncParameters(string[] generics) {
		List<FuncParameter> parameters = new List<FuncParameter>();
		
		while (!Check(TokenType.CloseParen)) {
			parameters.Add(FuncParameter(generics));

			if (!Match(TokenType.Comma)) {
				break;
			}

			MaybeSome(TokenType.Whitespace, TokenType.Newline);
		}

		return [..parameters];
	}

	protected FuncParameter FuncParameter(string[] generics) {
		// identifier [:typehint]? [ = defaultValue]?

		// identifier
		Token identifier = Consume(TokenType.Identifier, "Expected identifier after 'func' keyword");
		MaybeSome(TokenType.Whitespace);

		// typehint?
		TypeHint? typeHint = null;
		if (Match(TokenType.Colon)) {
			MaybeSome(TokenType.Whitespace);
			typeHint = TypeHint(generics);
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
}
