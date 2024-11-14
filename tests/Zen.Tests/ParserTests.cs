using Xunit.Abstractions;
using Zen.Lexing;
using Zen.Parsing;
using Zen.Parsing.AST;
using Zen.Parsing.AST.Expressions;
using Zen.Parsing.AST.Statements;
using Zen.Typing;

namespace Zen.Tests;

public class ParserTests {

    public static readonly bool Verbose = true; // prints tokens and AST when parsing

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

        if (Verbose) {
            _output.WriteLine(Helper.PrintTokens(tokens));
            _output.WriteLine(Helper.PrintAST(node));
        }

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
    public void TestVariableDeclaration() {
        ProgramNode program = Parse("var name = \"john\"");
        Assert.Single(program.Statements);

        Assert.IsType<VarStmt>(program.Statements[0]);

        VarStmt varStmt = (VarStmt)program.Statements[0];
        Assert.Equal("name", varStmt.Identifier.Value);
        Assert.False(varStmt.Constant);
        Assert.IsType<Literal>(varStmt.Initializer);

        Literal initializer = (Literal)varStmt.Initializer;
        Assert.Equal<string>("john", initializer.Value.Underlying);
    }

    [Fact]
    public void TestConstantVariableDeclaration() {
        ProgramNode program = Parse("const name = \"jane\"");
        Assert.Single(program.Statements);

        Assert.IsType<VarStmt>(program.Statements[0]);

        VarStmt varStmt = (VarStmt)program.Statements[0];
        Assert.Equal("name", varStmt.Identifier.Value);
        Assert.True(varStmt.Constant);
        Assert.IsType<Literal>(varStmt.Initializer);

        Literal initializer = (Literal)varStmt.Initializer;
        Assert.Equal<string>("jane", initializer.Value.Underlying);
    }

    
    [Fact]
    public void TestTypeHintedVariableDeclaration() {
        ProgramNode program = Parse("var name:string = \"hello\"");
        Assert.Single(program.Statements);

        Assert.IsType<VarStmt>(program.Statements[0]);

        VarStmt varStmt = (VarStmt)program.Statements[0];
        Assert.Equal("name", varStmt.Identifier.Value);
        Assert.False(varStmt.Constant);
        Assert.IsType<TypeHint>(varStmt.TypeHint);

        // check the TypeHint
        TypeHint typeHint = varStmt.TypeHint;
        Assert.Equal("string", typeHint.Name);
        Assert.False(typeHint.Nullable);
        Assert.False(typeHint.IsParametric);
        Assert.IsType<ZenType>(typeHint.GetBaseZenType());

        ZenType zenType = typeHint.GetZenType();
        Assert.Equal(ZenType.String, zenType);

        Assert.IsType<Literal>(varStmt.Initializer);

        Literal initializer = (Literal)varStmt.Initializer;
        Assert.Equal<string>("hello", initializer.Value.Underlying);
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
        Assert.Single(block.Statements);
        Assert.IsType<PrintStmt>(block.Statements[0]);
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
        Assert.Single(thenBlock.Statements);
        Assert.IsType<PrintStmt>(thenBlock.Statements[0]);

        // verify that the else block contains a print statement
        Block elseBlock = (Block)ifStmt.Else;
        Assert.Single(elseBlock.Statements);
        Assert.IsType<PrintStmt>(elseBlock.Statements[0]);
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
        Assert.Single(thenBlock.Statements);
        Assert.IsType<PrintStmt>(thenBlock.Statements[0]);

        // verify the 'else if' condition and block
        IfStmt elseIfStmt = ifStmt.ElseIfs[0];
        Assert.IsType<Literal>(elseIfStmt.Condition);
        Assert.IsType<Block>(elseIfStmt.Then);

        // verify that the 'else if' block contains a print statement
        Block elseIfBlock = (Block)elseIfStmt.Then;
        Assert.Single(elseIfBlock.Statements);
        Assert.IsType<PrintStmt>(elseIfBlock.Statements[0]);
    }

    
    [Fact]
    public void TestWhileStatementWithPrint() {
        ProgramNode program = Parse("while true {\n    print \"hello\"\n}");

        Assert.Single(program.Statements);
        Assert.IsType<WhileStmt>(program.Statements[0]);

        // verify condition and block
        WhileStmt whileStmt = (WhileStmt)program.Statements[0];

        Assert.IsType<Literal>(whileStmt.Condition);
        Assert.IsType<Block>(whileStmt.Body);

        // verify that the block contains a print statement
        Block block = (Block)whileStmt.Body;
        Assert.Single(block.Statements);
        Assert.IsType<PrintStmt>(block.Statements[0]);
    }

    
    [Fact]
    public void TestForStatementWithPrint() {
        ProgramNode program = Parse("for i = 0; i < 2; i += 1 {\n    print i\n}");

        Assert.Single(program.Statements);
        Assert.IsType<ForStmt>(program.Statements[0]);

        // verify initializer, condition, and incrementor
        ForStmt forStmt = (ForStmt)program.Statements[0];

        Assert.IsType<Token>(forStmt.LoopIdentifier);

        Assert.IsType<Literal>(forStmt.Initializer);
        Assert.IsType<Binary>(forStmt.Condition);
        Assert.IsType<Assignment>(forStmt.Incrementor);

        // verify block
        Assert.IsType<Block>(forStmt.Body);

        // verify that the block contains a print statement
        Block block = (Block)forStmt.Body;
        Assert.Single(block.Statements);
        Assert.IsType<PrintStmt>(block.Statements[0]);
    }
}