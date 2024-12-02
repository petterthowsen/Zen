using Zen.Execution.Import;
using Zen.Parsing.AST.Expressions;
using Zen.Parsing.AST.Statements;

namespace Zen.Parsing.AST;

public interface IVisitor
{
    // Root ProgramNode
    void Visit(ProgramNode programNode);

    void Visit(PackageStmt packageStmt);
    void Visit(ImportStmt importStmt);
    void Visit(FromImportStmt fromImportStmt);
    
    void Visit(VarStmt varStmt);
    void Visit(ExpressionStmt expressionStmt);

    void Visit(Block block);
    void Visit(IfStmt ifStmt);
    void Visit(WhileStmt whileStmt);
    void Visit(ForStmt forStmt);
    void Visit(ForInStmt forInStmt);
    
    void Visit(PrintStmt printStmt);
    
    void Visit(FuncStmt funcStmt);
    void Visit(ReturnStmt returnStmt);
    
    void Visit(ClassStmt classStmt);
    void Visit(InterfaceStmt interfaceStmt);
    void Visit(AbstractMethodStmt abstractMethodStmt);
    void Visit(PropertyStmt propertyStmt);
    void Visit(MethodStmt methodStmt);
    void Visit(ImplementsExpr implementsExpr);

    void Visit(Instantiation instantiation);

    // Expressions
    void Visit(Binary binary);
    void Visit(Grouping grouping);
    void Visit(Unary unary);
    void Visit(Literal literal);
    void Visit(TypeHint typeHint);
    void Visit(Identifier identifier);
    void Visit(Assignment assignment);
    void Visit(Logical logical);
    void Visit(Call call);
    void Visit(FuncParameter funcParameter);
    void Visit(Get get);
    void Visit(Set set);
    void Visit(This dis);
    void Visit(TypeCheck typeCheck);
    void Visit(TypeCast typeCast);
    void Visit(ParameterDeclaration parameter);
    void Visit(Await await);
    void Visit(BracketGet bracketGet);
    void Visit(BracketSet bracketSet);
}
