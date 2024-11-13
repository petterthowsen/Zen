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
        
        foreach (var stmt in block.Statements) {
            stmt.Accept(this);
        }
        
        _sb.Indent--;
    }

    public void Visit(WhileStmt whileStmt)
    {
        _sb.Add(whileStmt.ToString());

        _sb.Indent++;
        
        whileStmt.Condition.Accept(this);
        whileStmt.Body.Accept(this);
        
        _sb.Indent--;
    }

    public void Visit(VarStmt varStmt)
    {
        _sb.Add(varStmt.ToString());
        
        _sb.Indent++;
        
        _sb.Add(varStmt.Identifier.ToString());

        if (varStmt.Initializer != null) {
            varStmt.Initializer.Accept(this);
        }

        _sb.Indent--;
    }

    public void Visit(ForStmt forStmt)
    {
        _sb.Add(forStmt.ToString());

        _sb.Indent++;

        if (forStmt.Initializer != null)
        {
            forStmt.Initializer.Accept(this);
        }
        
        forStmt.Condition.Accept(this);

        if (forStmt.Incrementor != null)
        {
            forStmt.Incrementor.Accept(this);
        }

        foreach (var stmt in forStmt.Body.Statements)
        {
            stmt.Accept(this);
        }

        _sb.Indent--;
    }

    public void Visit(ForInStmt forInStmt) {
        _sb.Add(forInStmt.ToString());

        _sb.Indent++;

        forInStmt.Expression.Accept(this);
        forInStmt.Block.Accept(this);

        _sb.Indent--;
    }

    public void Visit(TypeHint typeHint) {
        _sb.Add(typeHint.ToString());
    }

    public void Visit(Identifier identifier) {
        _sb.Add(identifier.ToString());
    }

    public void Visit(Assignment assignment) {
        _sb.Add(assignment.ToString());

        _sb.Indent++;

        assignment.Identifier.Accept(this);
        assignment.Expression.Accept(this);

        _sb.Indent--;
    }

}