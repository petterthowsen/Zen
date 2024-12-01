namespace Zen.Parsing;

using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST;
using Zen.Parsing.AST.Expressions;

public partial class Parser {

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

}