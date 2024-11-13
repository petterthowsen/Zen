using Zen.Parsing.AST.Expressions;
using Zen.Parsing.AST.Statements;

namespace Zen.Parsing.AST;

public interface IVisitor
{
    // Root ProgramNode
    void Visit(ProgramNode programNode);

    // Statements
    void Visit(VarStmt varStmt);
    void Visit(Block block);
    void Visit(IfStmt ifStmt);
    void Visit(WhileStmt whileStmt);
    void Visit(ForStmt forStmt);
    void Visit(ForInStmt forInStmt);
    void Visit(ExpressionStmt expressionStmt);
    void Visit(PrintStmt printStmt);

    // Expressions
    void Visit(Binary binary);
    void Visit(Grouping grouping);
    void Visit(Unary unary);
    void Visit(Literal literal);
    void Visit(TypeHint typeHint);
    void Visit(Identifier identifier);
    void Visit(Assignment assignment);
}