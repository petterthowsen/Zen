using Zen.Parsing.AST.Expressions;
using Zen.Parsing.AST.Statements;

namespace Zen.Parsing.AST;

public interface IVisitor
{
    void Visit(ProgramNode programNode);
    void Visit(IfStmt ifStmt);
    void Visit(Binary binary);
    void Visit(Grouping grouping);
    void Visit(Unary unary);
    void Visit(Literal literal);
}