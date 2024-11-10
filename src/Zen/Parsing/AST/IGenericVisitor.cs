using Zen.Parsing.AST.Expressions;
using Zen.Parsing.AST.Statements;

namespace Zen.Parsing.AST;

public interface IGenericVisitor<T>
{   
    // Root Program Node
    T Visit(ProgramNode programNode);

    // Statements
    T Visit(Block block);
    T Visit(IfStmt ifStmt);
    T Visit(ExpressionStmt expressionStmt);
    T Visit(PrintStmt printStmt);

    // Expressions
    T Visit(Binary binary);
    T Visit(Grouping grouping);
    T Visit(Unary unary);
    T Visit(Literal literal);
}