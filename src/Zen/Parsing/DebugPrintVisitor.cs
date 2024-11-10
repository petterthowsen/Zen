using Zen.Parsing.AST;
using Zen.Parsing.AST.Expressions;
using Zen.Parsing.AST.Statements;
using Zen.Support;

namespace Zen.Parsing;

public class DebugPrintVisitor : IVisitor {

    private readonly IndentedStringBuilder _sb = new();

    public override string ToString() => _sb.ToString();

    public void Visit(ProgramNode programNode)
    {
        _sb.Add(programNode.ToString());
        _sb.Indent++;

        foreach (var stmt in programNode.Statements) {
            stmt.Accept(this);
        }

        _sb.Indent--;
    }

    public void Visit(IfStmt ifStmt)
    {
        _sb.Add(ifStmt.ToString());
        _sb.Indent++;

        ifStmt.Condition.Accept(this);
        ifStmt.Then.Accept(this);

        //TODO: else blocks

        _sb.Indent--;
    }

    public void Visit(Binary binary)
    {
        _sb.Add(binary.ToString());

        _sb.Indent++;
        
        binary.Left.Accept(this);
        _sb.Add(binary.Operator.ToString());
        binary.Right.Accept(this);
        
        _sb.Indent--;
    }

    public void Visit(Grouping grouping)
    {
        _sb.Add(grouping.ToString());

        _sb.Indent++;
        
        grouping.Expression.Accept(this);
        
        _sb.Indent--;
    }

    public void Visit(Unary unary)
    {
        _sb.Add(unary.ToString());
        _sb.Indent++;
        
        _sb.Add(unary.Operator.ToString());
        unary.Right.Accept(this);
        
        _sb.Indent--;
    }

    public void Visit(Literal literal)
    {
        _sb.Add(literal.ToString());
    }

    public void Visit(PrintStmt printStmt)
    {
        _sb.Add(printStmt.ToString());
        
        _sb.Indent++;
        
        printStmt.Expression.Accept(this);
        
        _sb.Indent--;
    }

    public void Visit(ExpressionStmt expressionStmt)
    {
        _sb.Add(expressionStmt.ToString());
        
        _sb.Indent++;
        
        expressionStmt.Expression.Accept(this);
        
        _sb.Indent--;
    }

    public void Visit(Block block)
    {
        _sb.Add(block.ToString());

        _sb.Indent++;
        
        foreach (var stmt in block.Body) {
            stmt.Accept(this);
        }
        
        _sb.Indent--;
    }
}