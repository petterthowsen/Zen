using Xunit.Abstractions;
using Zen.Lexing;
using Zen.Parsing;
using Zen.Parsing.AST;
using Zen.Parsing.AST.Expressions;
using Zen.Parsing.AST.Statements;

namespace Zen.Tests;

public class ParserTests {

    private readonly ITestOutputHelper _output;

    public Lexer Lexer = new Lexer();
    public Parser Parser = new Parser();


    public ParserTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private ProgramNode Parse(string code) {
        List<Token> tokens = Lexer.Tokenize(code);
        ProgramNode node = Parser.Parse(tokens);

        if (Parser.Errors.Count > 0) {
            _output.WriteLine(Helper.GetErrors(Parser.Errors));
        }

        return node;
    }

    [Fact]
    public void TestEmpty() {
        ProgramNode program = Parse("");
        Assert.Empty(program.Statements);
    }

    [Fact]
    public void TestSingleExpressionStatement() {
        ProgramNode program = Parse("1");
        Assert.Single(program.Statements);

        Assert.IsType<ExpressionStmt>(program.Statements[0]);
    }

    [Fact]
    public void TestEmptyIfStatement() {
        ProgramNode program = Parse("if true {}");
        Assert.Single(program.Statements);
        Assert.IsType<IfStmt>(program.Statements[0]);

        // verify condition and block
        IfStmt ifStmt = (IfStmt)program.Statements[0];

        Assert.IsType<Literal>(ifStmt.Condition);
        Assert.IsType<Block>(ifStmt.Then);
    }

    [Fact]
    public void TestIfStatementWithPrint() {
        ProgramNode program = Parse("if true {\n    print \"hello\"\n}");
        Assert.Single(program.Statements);
        Assert.IsType<IfStmt>(program.Statements[0]);

        // verify condition and block
        IfStmt ifStmt = (IfStmt)program.Statements[0];

        Assert.IsType<Literal>(ifStmt.Condition);
        Assert.IsType<Block>(ifStmt.Then);

        // verify that the block contains a print statement
        Block block = (Block)ifStmt.Then;
        Assert.Single(block.Body);
        Assert.IsType<PrintStmt>(block.Body[0]);
    }

    
    [Fact]
    public void TestIfStatementWithElse() {
        ProgramNode program = Parse("if true {\n    print \"hello\"\n} else {\n    print \"world\"\n}");
        Assert.Single(program.Statements);
        Assert.IsType<IfStmt>(program.Statements[0]);

        // verify condition and block
        IfStmt ifStmt = (IfStmt)program.Statements[0];

        Assert.IsType<Literal>(ifStmt.Condition);
        Assert.IsType<Block>(ifStmt.Then);
        Assert.IsType<Block>(ifStmt.Else);

        // verify that the block contains a print statement
        Block thenBlock = (Block)ifStmt.Then;
        Assert.Single(thenBlock.Body);
        Assert.IsType<PrintStmt>(thenBlock.Body[0]);

        // verify that the else block contains a print statement
        Block elseBlock = (Block)ifStmt.Else;
        Assert.Single(elseBlock.Body);
        Assert.IsType<PrintStmt>(elseBlock.Body[0]);
    }

    [Fact]
    public void TestIfStatementWithElseIf() {
        ProgramNode program = Parse("if true {\n    print \"hello\"\n} else if false {\n    print \"world\"\n}");

        Assert.Single(program.Statements);
        Assert.IsType<IfStmt>(program.Statements[0]);

        // verify condition and blocks
        IfStmt ifStmt = (IfStmt)program.Statements[0];

        Assert.IsType<Literal>(ifStmt.Condition);
        Assert.IsType<Block>(ifStmt.Then);
        Assert.NotNull(ifStmt.ElseIfs);
        Assert.Single(ifStmt.ElseIfs);

        // verify that the 'then' block contains a print statement
        Block thenBlock = (Block)ifStmt.Then;
        Assert.Single(thenBlock.Body);
        Assert.IsType<PrintStmt>(thenBlock.Body[0]);

        // verify the 'else if' condition and block
        IfStmt elseIfStmt = ifStmt.ElseIfs[0];
        Assert.IsType<Literal>(elseIfStmt.Condition);
        Assert.IsType<Block>(elseIfStmt.Then);

        // verify that the 'else if' block contains a print statement
        Block elseIfBlock = (Block)elseIfStmt.Then;
        Assert.Single(elseIfBlock.Body);
        Assert.IsType<PrintStmt>(elseIfBlock.Body[0]);
    }
}