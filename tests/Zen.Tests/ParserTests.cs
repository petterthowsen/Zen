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
    public void TestComplexExpression() {
        ProgramNode program = Parse("10 + 2 * 10 - 3");
        Assert.Single(program.Statements);

        Assert.IsType<ExpressionStmt>(program.Statements[0]);

        ExpressionStmt exprStmt = (ExpressionStmt)program.Statements[0];
        Binary binaryExpr = Assert.IsType<Binary>(exprStmt.Expression);
        Assert.IsType<Binary>(binaryExpr.Left);
        Binary leftExpr = (Binary)binaryExpr.Left;

        Literal leftLiteral = Assert.IsType<Literal>(leftExpr.Left);
        Assert.Equal(TokenType.IntLiteral, leftLiteral.Token.Type);
        Assert.Equal("10", leftLiteral.Token.Value);

        Assert.Equal(TokenType.Plus, leftExpr.Operator.Type);

        Binary rightExprLeft = Assert.IsType<Binary>(leftExpr.Right);
        Literal rightExprLeftLiteral = Assert.IsType<Literal>(rightExprLeft.Left);
        Assert.Equal(TokenType.IntLiteral, rightExprLeftLiteral.Token.Type);
        Assert.Equal("2", rightExprLeftLiteral.Token.Value);

        Assert.Equal(TokenType.Star, rightExprLeft.Operator.Type);

        Literal rightExprRightLiteral = Assert.IsType<Literal>(rightExprLeft.Right);
        Assert.Equal(TokenType.IntLiteral, rightExprRightLiteral.Token.Type);
        Assert.Equal("10", rightExprRightLiteral.Token.Value);

        Assert.Equal(TokenType.Minus, binaryExpr.Operator.Type);

        Literal rightLiteral = Assert.IsType<Literal>(binaryExpr.Right);
        Assert.Equal(TokenType.IntLiteral, rightLiteral.Token.Type);
        Assert.Equal("3", rightLiteral.Token.Value);
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
    public void TestIfStatementWithAndCondition() {
        ProgramNode program = Parse("if true and false {}");

        Assert.Single(program.Statements);
        Assert.IsType<IfStmt>(program.Statements[0]);

        // verify condition and block
        IfStmt ifStmt = (IfStmt)program.Statements[0];

        // Check that the condition is a binary expression with 'and' operator
        Logical logical = Assert.IsType<Logical>(ifStmt.Condition);

        Assert.Equal(TokenType.Keyword, logical.Token.Type);
        Assert.Equal("and", logical.Token.Value);

        // Verify that the left expression is a literal
        Assert.IsType<Literal>(logical.Left);
        Literal leftLiteral = (Literal) logical.Left;
        Assert.Equal(ZenValue.True, leftLiteral.Value);

        // Verify that the right expression is a literal
        Assert.IsType<Literal>(logical.Right);
        Literal rightLiteral = (Literal) logical.Right;
        Assert.Equal(ZenValue.False, rightLiteral.Value);

        // Verify that the block is present
        Assert.IsType<Block>(ifStmt.Then);
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

    
    [Fact]
    public void TestCall() {
        ProgramNode program = Parse("test()");

        Assert.Single(program.Statements);
        Assert.IsType<ExpressionStmt>(program.Statements[0]);

        // verify that the expression is a Call expression
        ExpressionStmt exprStmt = (ExpressionStmt)program.Statements[0];
        Assert.IsType<Call>(exprStmt.Expression);

        // verify that the callee is a identifier
        Call call = (Call)exprStmt.Expression;
        Assert.IsType<Identifier>(call.Callee);

        Assert.Empty(call.Arguments);
    }

    
    [Fact]
    public void TestCallWithArgument() {
        ProgramNode program = Parse("test(5)");

        Assert.Single(program.Statements);
        Assert.IsType<ExpressionStmt>(program.Statements[0]);

        // verify that the expression is a Call expression
        ExpressionStmt exprStmt = (ExpressionStmt)program.Statements[0];
        Assert.IsType<Call>(exprStmt.Expression);

        // verify that the callee is a identifier
        Call call = (Call)exprStmt.Expression;
        Assert.IsType<Identifier>(call.Callee);

        Assert.Single(call.Arguments);
        Assert.IsType<Literal>(call.Arguments[0]);
    }

    
    [Fact]
    public void TestCallWithTwoArguments() {
        ProgramNode program = Parse("test(one, 5)");

        Assert.Single(program.Statements);
        Assert.IsType<ExpressionStmt>(program.Statements[0]);

        // verify that the expression is a Call expression
        ExpressionStmt exprStmt = (ExpressionStmt)program.Statements[0];
        Assert.IsType<Call>(exprStmt.Expression);

        // verify that the callee is a identifier
        Call call = (Call)exprStmt.Expression;
        Assert.IsType<Identifier>(call.Callee);

        Assert.Equal(2, call.Arguments.Length);
        Assert.IsType<Identifier>(call.Arguments[0]);
        Assert.IsType<Literal>(call.Arguments[1]);
    }

    [Fact]
    public void TestFuncStmt() {
        ProgramNode program = Parse("func test() { }");
        Assert.Single(program.Statements);
        Assert.IsType<FuncStmt>(program.Statements[0]);

        FuncStmt funcStmt = (FuncStmt)program.Statements[0];
        Assert.Equal(TokenType.Identifier, funcStmt.Identifier.Type);
        Assert.Equal("test", funcStmt.Identifier.Value);

        Assert.Empty(funcStmt.Parameters);
        Assert.IsType<Block>(funcStmt.Block);
        Assert.IsType<TypeHint>(funcStmt.ReturnType);
    }

    
    [Fact]
    public void TestFuncStmtWithExplicitVoidReturnType() {
        ProgramNode program = Parse("func test() : void { }");
        Assert.Single(program.Statements);
        Assert.IsType<FuncStmt>(program.Statements[0]);

        FuncStmt funcStmt = (FuncStmt)program.Statements[0];
        Assert.Equal(TokenType.Identifier, funcStmt.Identifier.Type);
        Assert.Equal("test", funcStmt.Identifier.Value);

        Assert.Empty(funcStmt.Parameters);
        Assert.IsType<Block>(funcStmt.Block);
        Assert.IsType<TypeHint>(funcStmt.ReturnType);
        var typeHint = (TypeHint)funcStmt.ReturnType;
        
        Assert.Equal(ZenType.Void, typeHint.GetZenType());
    }

    [Fact]
    public void TestFuncStmtWithOneArgument() {
        ProgramNode program = Parse("func test(arg) { }");
        Assert.Single(program.Statements);
        Assert.IsType<FuncStmt>(program.Statements[0]);

        FuncStmt funcStmt = (FuncStmt)program.Statements[0];
        Assert.Equal(TokenType.Identifier, funcStmt.Identifier.Type);
        Assert.Equal("test", funcStmt.Identifier.Value);

        Assert.Single(funcStmt.Parameters);
        Assert.Equal("arg", funcStmt.Parameters[0].Identifier.Value);
        Assert.IsType<Block>(funcStmt.Block);
        Assert.IsType<TypeHint>(funcStmt.ReturnType);
    }

    [Fact]
    public void TestFuncStmtWithStringArgument() {
        ProgramNode program = Parse("func test(arg:string) { }");
        Assert.Single(program.Statements);
        Assert.IsType<FuncStmt>(program.Statements[0]);

        FuncStmt funcStmt = (FuncStmt)program.Statements[0];
        Assert.Equal(TokenType.Identifier, funcStmt.Identifier.Type);
        Assert.Equal("test", funcStmt.Identifier.Value);

        Assert.Single(funcStmt.Parameters);
        Assert.Equal("arg", funcStmt.Parameters[0].Identifier.Value);
        Assert.IsType<TypeHint>(funcStmt.Parameters[0].TypeHint);

        var paramTypeHint = (TypeHint)funcStmt.Parameters[0].TypeHint!;
        Assert.Equal(ZenType.String, paramTypeHint.GetZenType());
        
        Assert.IsType<Block>(funcStmt.Block);
        Assert.IsType<TypeHint>(funcStmt.ReturnType);
    }
    
    [Fact]
    public void TestClassDeclaration() {
        ProgramNode program = Parse("class Test { }");
        Assert.Single(program.Statements);
        Assert.IsType<ClassStmt>(program.Statements[0]);

        ClassStmt classStmt = (ClassStmt)program.Statements[0];
        Assert.Equal("Test", classStmt.Identifier.Value);
    }

    [Fact]
    public void TestClassDeclarationWithProperty() {
        ProgramNode program = Parse("class Test { name: string }");
        Assert.Single(program.Statements);
        Assert.IsType<ClassStmt>(program.Statements[0]);

        ClassStmt classStmt = (ClassStmt)program.Statements[0];
        Assert.Equal("Test", classStmt.Identifier.Value);
        Assert.Single(classStmt.Properties);
        Assert.Equal("name", classStmt.Properties[0].Identifier.Value);
        Assert.Equal(ZenType.String, classStmt.Properties[0].TypeHint!.GetZenType());
    }

    
    [Fact]
    public void TestClassDeclarationWithPropertyAndDefaultValue() {
        ProgramNode program = Parse("class Test { name: string = \"default\" }");
        Assert.Single(program.Statements);
        Assert.IsType<ClassStmt>(program.Statements[0]);

        ClassStmt classStmt = (ClassStmt)program.Statements[0];
        Assert.Equal("Test", classStmt.Identifier.Value);
        Assert.Single(classStmt.Properties);
        Assert.Equal("name", classStmt.Properties[0].Identifier.Value);
        Assert.Equal(ZenType.String, classStmt.Properties[0].TypeHint!.GetZenType());

        Assert.IsType<Literal>(classStmt.Properties[0].Initializer);
        Literal literal = (Literal) classStmt.Properties[0].Initializer!;

        Assert.Equal(Literal.LiteralKind.String, literal.Kind);

        Assert.Equal(ZenType.String, literal.Value.Type);
        Assert.Equal("default", literal.Value.Underlying);
    }

    [Fact]
    public void TestClassDeclarationWithMethod() {
        ProgramNode program = Parse("class Test { test() { } }");
        Assert.Single(program.Statements);
        Assert.IsType<ClassStmt>(program.Statements[0]);

        ClassStmt classStmt = (ClassStmt)program.Statements[0];
        Assert.Equal("Test", classStmt.Identifier.Value);
        Assert.Single(classStmt.Methods);
        Assert.Equal("test", classStmt.Methods[0].Identifier.Value);
    }

    
    [Fact]
    public void TestInstantiationExpression() {
        ProgramNode program = Parse("var obj = new Object()");

        Assert.Single(program.Statements);
        Assert.IsType<VarStmt>(program.Statements[0]);

        VarStmt varStmt = (VarStmt)program.Statements[0];

        Assert.Equal("obj", varStmt.Identifier.Value);

        Assert.IsType<Instantiation>(varStmt.Initializer);

        Instantiation instantiation = (Instantiation)varStmt.Initializer!;
        Call call = instantiation.Call;

        Assert.Empty(call.Arguments);
    }

    [Fact]
    public void TestInstantiationExpressionWithArgument() {
        ProgramNode program = Parse("var obj = new Object(\"hello\")");

        Assert.Single(program.Statements);
        Assert.IsType<VarStmt>(program.Statements[0]);

        VarStmt varStmt = (VarStmt)program.Statements[0];

        Assert.Equal("obj", varStmt.Identifier.Value);

        Assert.IsType<Instantiation>(varStmt.Initializer);

        Instantiation instantiation = (Instantiation)varStmt.Initializer!;
        Call call = instantiation.Call;

        Assert.Single(call.Arguments);
        Assert.IsType<Literal>(call.Arguments[0]);
    }

      [Fact]
    public void TestTypeCheckExpression() {
        ProgramNode program = Parse("x is string");
        Assert.Single(program.Statements);
        Assert.IsType<ExpressionStmt>(program.Statements[0]);

        ExpressionStmt exprStmt = (ExpressionStmt)program.Statements[0];
        TypeCheck typeCheck = Assert.IsType<TypeCheck>(exprStmt.Expression);

        Assert.Equal("is", typeCheck.Token.Value);
        Assert.IsType<Identifier>(typeCheck.Expression);
        Assert.IsType<TypeHint>(typeCheck.Type);

        TypeHint typeHint = typeCheck.Type;
        Assert.Equal("string", typeHint.Name);
        Assert.False(typeHint.Nullable);
    }

    [Fact]
    public void TestTypeCheckWithNullableType() {
        ProgramNode program = Parse("x is string?");
        Assert.Single(program.Statements);
        Assert.IsType<ExpressionStmt>(program.Statements[0]);

        ExpressionStmt exprStmt = (ExpressionStmt)program.Statements[0];
        TypeCheck typeCheck = Assert.IsType<TypeCheck>(exprStmt.Expression);

        Assert.Equal("is", typeCheck.Token.Value);
        Assert.IsType<Identifier>(typeCheck.Expression);
        Assert.IsType<TypeHint>(typeCheck.Type);

        TypeHint typeHint = typeCheck.Type;
        Assert.Equal("string", typeHint.Name);
        Assert.True(typeHint.Nullable);
    }

    [Fact]
    public void TestTypeCheckInIfCondition() {
        ProgramNode program = Parse("if x is string { print x }");
        Assert.Single(program.Statements);
        Assert.IsType<IfStmt>(program.Statements[0]);

        IfStmt ifStmt = (IfStmt)program.Statements[0];
        TypeCheck typeCheck = Assert.IsType<TypeCheck>(ifStmt.Condition);

        Assert.Equal("is", typeCheck.Token.Value);
        Assert.IsType<Identifier>(typeCheck.Expression);
        Assert.IsType<TypeHint>(typeCheck.Type);

        TypeHint typeHint = typeCheck.Type;
        Assert.Equal("string", typeHint.Name);
        Assert.False(typeHint.Nullable);
    }

    [Fact]
    public void TestTypeCastExpression() {
        ProgramNode program = Parse("(int) x");
        Assert.Single(program.Statements);
        Assert.IsType<ExpressionStmt>(program.Statements[0]);

        ExpressionStmt exprStmt = (ExpressionStmt)program.Statements[0];
        TypeCast typeCast = Assert.IsType<TypeCast>(exprStmt.Expression);

        Assert.Equal(TokenType.OpenParen, typeCast.Token.Type);
        Assert.IsType<Identifier>(typeCast.Expression);
        Assert.IsType<TypeHint>(typeCast.Type);

        TypeHint typeHint = typeCast.Type;
        Assert.Equal("int", typeHint.Name);
        Assert.False(typeHint.Nullable);
    }

    [Fact]
    public void TestTypeCastWithNullableType() {
        ProgramNode program = Parse("(string?) x");
        Assert.Single(program.Statements);
        Assert.IsType<ExpressionStmt>(program.Statements[0]);

        ExpressionStmt exprStmt = (ExpressionStmt)program.Statements[0];
        TypeCast typeCast = Assert.IsType<TypeCast>(exprStmt.Expression);

        Assert.Equal(TokenType.OpenParen, typeCast.Token.Type);
        Assert.IsType<Identifier>(typeCast.Expression);
        Assert.IsType<TypeHint>(typeCast.Type);

        TypeHint typeHint = typeCast.Type;
        Assert.Equal("string", typeHint.Name);
        Assert.True(typeHint.Nullable);
    }

    [Fact]
    public void TestTypeCastInAssignment() {
        ProgramNode program = Parse("x = (int) 5.5");
        Assert.Single(program.Statements);
        Assert.IsType<ExpressionStmt>(program.Statements[0]);

        ExpressionStmt exprStmt = (ExpressionStmt)program.Statements[0];
        Assignment assignment = Assert.IsType<Assignment>(exprStmt.Expression);

        Assert.Equal(TokenType.Assign, assignment.Operator.Type);
        Assert.IsType<Identifier>(assignment.Identifier);
        Assert.IsType<TypeCast>(assignment.Expression);

        TypeCast typeCast = (TypeCast)assignment.Expression;
        Assert.Equal(TokenType.OpenParen, typeCast.Token.Type);
        Assert.IsType<Literal>(typeCast.Expression);
        Assert.IsType<TypeHint>(typeCast.Type);

        TypeHint typeHint = typeCast.Type;
        Assert.Equal("int", typeHint.Name);
        Assert.False(typeHint.Nullable);
    }
}