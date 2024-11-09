namespace Zen.Parsing;

using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST;
using Zen.Parsing.AST.Expressions;

public class Parser
{

	private List<Token> Tokens = [];

	public readonly List<Error> Errors = [];

	private int _index = 0;


	protected Token Current => Tokens[_index];

	protected Token Peek(int offset = 1) => Tokens[_index + offset];

	protected Token Next => Tokens[_index + 1];

	protected Token Previous => Tokens[_index - 1];

	protected bool IsAtEnd => Current.Type == TokenType.EOF;

	public Node? Parse(List<Token> tokens)
	{
		Tokens = tokens;
		_index = 0;
		Errors.Clear();

		try {
			return Expression();
		} catch (Error parseError) {
			return null;
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
	/// <returns>The consumed token, or null if the token did not match</returns>
	private Token? Consume(TokenType type, string message)
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
	private int OptionalConsume(TokenType type, bool multiple = false)
	{
		int count = 0;
		while (Check(type))
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

	private bool MatchKeyword(string keyword)
	{
		if (Current.Type == TokenType.Keyword && Current.Value == keyword)
		{
			Advance();
			return true;
		}
		return false;
	}

	private Expr Expression()
	{
		return Equality();
	}

	private Expr Equality()
	{
		Expr expr = Comparison();

		while (Match(TokenType.NotEqual, TokenType.Equal))
		{
			Token op = Previous;
			Expr right = Comparison();
			expr = new Binary(expr, op, right);
		}

		return expr;
	}

	private Expr Comparison()
	{
		Expr expr = Term();

		while (Match(TokenType.GreaterThan, TokenType.GreaterThanOrEqual, TokenType.LessThan, TokenType.LessThanOrEqual))
		{
			Token op = Previous;
			Expr right = Term();
			expr = new Binary(expr, op, right);
		}

		return expr;
	}

	private Expr Term()
	{
		Expr expr = Factor();

		while (Match(TokenType.Minus, TokenType.Plus))
		{
			Token op = Previous;
			Expr right = Factor();
			expr = new Binary(expr, op, right);
		}

		return expr;
	}

	private Expr Factor()
	{
		Expr expr = Unary();

		while (Match(TokenType.Slash, TokenType.Star))
		{
			Token op = Previous;
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
			Expr right = Unary();
			return new Unary(op, right);
		}

		return Primary();
	}

	private Expr Primary()
	{
		if (MatchKeyword("false")) return new Literal(Literal.Kind.Bool, false, Previous);
		if (MatchKeyword("true")) return new Literal(Literal.Kind.Bool, true, Previous);
		if (MatchKeyword("null")) return new Literal(Literal.Kind.Null, null, Previous);

		// we store number literals as strings since we don't know if they should be 32-bit or 64-bit
		// it's up to the interpreter to decide based on the context.
		if (Match(TokenType.IntLiteral)) {
			return new Literal(Literal.Kind.Int, Previous.Value, Previous);
		}

		if (Match(TokenType.FloatLiteral)) {
			return new Literal(Literal.Kind.Float, Previous.Value, Previous);
		}

		if (Match(TokenType.StringLiteral))
		{
			return new Literal(Literal.Kind.String, Previous.Value, Previous);
		}

		if (Match(TokenType.OpenParen))
		{
			Expr expr = Expression();
			Consume(TokenType.CloseParen, "Expected a ')' after expression.");
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