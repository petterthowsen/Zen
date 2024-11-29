using Zen.Parsing.AST.Expressions;
using Zen.Parsing.AST.Statements;

namespace Zen.Parsing.AST;

public interface IGenericVisitor<T>
{   
    // Root Program Node
    T Visit(ProgramNode programNode);

    // Statements
    T Visit(VarStmt varStmt);
    T Visit(Block block);
    T Visit(IfStmt ifStmt);
    T Visit(WhileStmt whileStmt);
    T Visit(ForStmt forStmt);
    T Visit(ForInStmt forInStmt);
    T Visit(ExpressionStmt expressionStmt);
    T Visit(PrintStmt printStmt);
    T Visit(FuncStmt funcStmt);
    T Visit(ReturnStmt returnStmt);
    T Visit(ClassStmt classStmt);
    T Visit(PropertyStmt propertyStmt);
    T Visit(MethodStmt methodStmt);
    T Visit(Instantiation instantiation);
    T Visit(ImportStmt importStmt);
    T Visit(FromImportStmt fromImportStmt);
    T Visit(PackageStmt packageStmt);

    // Expressions
    T Visit(Binary binary);
    T Visit(Grouping grouping);
    T Visit(Unary unary);
    T Visit(Literal literal);
    T Visit(TypeHint typeHint);
    T Visit(Identifier identifier);
    T Visit(Assignment assignment);
    T Visit(Logical logical);
    T Visit(Call call);
    T Visit(FuncParameter funcParameter);
    T Visit(Get get);
    T Visit(Set set);
    T Visit(This dis);
    T Visit(TypeCheck typeCheck);
    T Visit(TypeCast typeCast);
    T Visit(Await await);
    T Visit(BracketGet bracketGet);
    T Visit(BracketSet bracketSet);
}
