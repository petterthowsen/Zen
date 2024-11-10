using Zen.Parsing.AST.Expressions;
using Zen.Parsing.AST.Statements;

namespace Zen.Parsing.AST;

public interface IVisitor
{
    // Root ProgramNode
    void Visit(ProgramNode programNode);

    // Statements
    void Visit(Block block);
    void Visit(IfStmt ifStmt);
    void Visit(ExpressionStmt expressionStmt);
    void Visit(PrintStmt printStmt);

    // Expressions
    void Visit(Binary binary);
    void Visit(Grouping grouping);
    void Visit(Unary unary);
    void Visit(Literal literal);
}