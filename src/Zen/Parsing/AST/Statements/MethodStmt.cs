using Zen.Lexing;
using Zen.Parsing.AST.Expressions;

namespace Zen.Parsing.AST.Statements;

public class MethodStmt : FuncStmt
{

    public Token[] Modifiers;

    public MethodStmt(Token identifier, TypeHint returnType, FuncParameter[] parameters, Block block, Token[] modifiers) : base(false, identifier, returnType, parameters, block)
    {
        Modifiers = modifiers;
        Async = HasModifier("async");
    }

    public bool HasModifier(string modifier) => Modifiers.Any(m => m.Value == modifier);

    public override string ToString()
    {
        return "MethodStmt";
    }
}