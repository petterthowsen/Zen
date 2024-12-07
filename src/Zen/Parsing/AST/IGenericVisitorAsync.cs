using Zen.Parsing.AST.Expressions;
using Zen.Parsing.AST.Statements;
using System.Threading.Tasks;

namespace Zen.Parsing.AST;

public interface IGenericVisitorAsync<T>
{   
    // Root Program Node
    T VisitAsync(ProgramNode programNode);

    // Statements
    T VisitAsync(VarStmt varStmt);
    T VisitAsync(Block block);
    T VisitAsync(IfStmt ifStmt);
    T VisitAsync(WhileStmt whileStmt);
    T VisitAsync(ForStmt forStmt);
    T VisitAsync(ForInStmt forInStmt);
    T VisitAsync(ExpressionStmt expressionStmt);
    T VisitAsync(PrintStmt printStmt);
    T VisitAsync(FuncStmt funcStmt);
    T VisitAsync(ReturnStmt returnStmt);
    T VisitAsync(ClassStmt classStmt);
    T VisitAsync(InterfaceStmt interfaceStmt);
    T VisitAsync(AbstractMethodStmt abstractMethodStmt);
    T VisitAsync(PropertyStmt propertyStmt);
    T VisitAsync(MethodStmt methodStmt);
    T VisitAsync(ImplementsExpr implementsExpr);
    T VisitAsync(ThrowStmt @throwStmt);

    T VisitAsync(Instantiation instantiation);
    T VisitAsync(ImportStmt importStmt);
    T VisitAsync(FromImportStmt fromImportStmt);
    T VisitAsync(PackageStmt packageStmt);

    // Expressions
    T VisitAsync(Binary binary);
    T VisitAsync(Grouping grouping);
    T VisitAsync(Unary unary);
    T VisitAsync(Literal literal);
    T VisitAsync(TypeHint typeHint);
    T VisitAsync(ParameterDeclaration typeHintParam);
    T VisitAsync(Identifier identifier);
    T VisitAsync(Assignment assignment);
    T VisitAsync(Logical logical);
    T VisitAsync(Call call);
    T VisitAsync(FuncParameter funcParameter);
    T VisitAsync(Get get);
    T VisitAsync(Set set);
    T VisitAsync(This dis);
    T VisitAsync(TypeCheck typeCheck);
    T VisitAsync(TypeCast typeCast);
    T VisitAsync(Await await);
    T VisitAsync(BracketGet bracketGet);
    T VisitAsync(BracketSet bracketSet);
}
