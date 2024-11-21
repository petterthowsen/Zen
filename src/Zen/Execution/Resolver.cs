using Zen.Common;
using Zen.Lexing;
using Zen.Parsing.AST;
using Zen.Parsing.AST.Expressions;
using Zen.Parsing.AST.Statements;

namespace Zen.Execution;

/// <summary>
/// Resolves expressions and statements by binding identifiers to their definitions.
/// Following the techniques from the book "Crafting Interpreters" by Robert Nystrom.
/// </summary>
public class Resolver : IVisitor
{
    protected enum FunctionType {
        NONE,
        FUNCTION,
        METHOD,
        CONSTRUCTOR,
    }
    
    protected Interpreter interpreter;
    protected Stack<Dictionary<string, bool>> scopes = new();
    protected FunctionType currentFunction = FunctionType.NONE;

    public readonly List<Error> Errors = [];

    public Resolver(Interpreter interpreter) {
        this.interpreter = interpreter;
    }

    /// <summary>
    /// Resets the resolver to its initial state. This is useful when
    /// reusing the same resolver instance across multiple scripts.
    /// </summary>
    public void Reset() {
        scopes.Clear();
        currentFunction = FunctionType.NONE;
        Errors.Clear();
    }

    private Error Error(string message, SourceLocation? location = null, ErrorType errorType = ErrorType.RuntimeError) {
        Error err = new(message, errorType, location);
        Errors.Add(err);
        return err;
    }

    public void Resolve(ProgramNode program) {
        foreach (var statement in program.Statements) {
            Resolve(statement);
        }
    }

    public void Resolve(Stmt[] statements) {
        foreach (var statement in statements) {
            Resolve(statement);
        }
    }
    
    public void Resolve(Stmt stmt) {
        stmt.Accept(this);
    }

    public void Resolve(Block block) {
        BeginScope();
        Resolve(block.Statements);
        EndScope();
    }

    public void Resolve(Expr expr) {
        expr.Accept(this);
    }

    protected void ResolveFunction(FuncStmt funcStmt, FunctionType type) {
        FunctionType enclosingFunction = currentFunction;
        currentFunction = type;

        BeginScope();
        foreach (var parameter in funcStmt.Parameters) {
            Declare(parameter.Identifier);
            Define(parameter.Identifier);
        }
        Resolve(funcStmt.Block.Statements);
        EndScope();
        currentFunction = enclosingFunction;
    }

    public void BeginScope() {
        scopes.Push([]);
    }

    public void EndScope() {
        scopes.Pop();
    }

    /// <summary>
    /// Declare a variable in the current scope. The variable is initially considered uninitialized.
    /// </summary>
    /// <param name="name">The name of the variable to declare.</param>
    private void Declare(Token name) {
        if (scopes.Count == 0) return;

        Dictionary<String, Boolean> scope = scopes.Peek();
        
        if (scope.ContainsKey(name.Value)) {
            throw Interpreter.Error("Variable with this name already declared in this scope.", name.Location);
        }

        scope.Add(name.Value, false);
    }

    private void Define(Token name) {
        if (scopes.Count == 0) return;

        Dictionary<String, Boolean> scope = scopes.Peek();
        scope[name.Value] = true;
    }

    private void ResolveLocal(Expr expr, string name) {
        foreach (var scope in scopes) {
            if (scope.ContainsKey(name)) {
                interpreter.Resolve(expr, scope.Count - 1);
                return;
            }
        }
    }

    public void Visit(ProgramNode programNode)
    {
        foreach (var statement in programNode.Statements) {
            Resolve(statement);
        }
    }

    public void Visit(VarStmt varStmt)
    {
        Declare(varStmt.Identifier);

        if (varStmt.Initializer != null) {
            Resolve(varStmt.Initializer);
        }

        Define(varStmt.Identifier);

        // if (varStmt.TypeHint != null) {
        //     Resolve(varStmt.TypeHint);
        // }
    }

    public void Visit(Assignment assignment)
    {
        Resolve(assignment.Expression);
        ResolveLocal(assignment, assignment.Identifier.Name);
    }

    public void Visit(Identifier identifier)
    {
        if (scopes.Count == 0) return;

        Dictionary<String, Boolean> scope = scopes.Peek();

        if (scope.ContainsKey(identifier.Name) && !scope[identifier.Name]) {
            Error("Cannot read local variable in its own initializer.", identifier.Location);
        }

        ResolveLocal(identifier, identifier.Name);
    }

    public void Visit(Block block)
    {
        BeginScope();
        Resolve(block.Statements);
        EndScope();
    }

    public void Visit(IfStmt ifStmt)
    {
        Resolve(ifStmt.Condition);
        Resolve(ifStmt.Then);
        Resolve(ifStmt.ElseIfs);
        if (ifStmt.Else != null) {
            Resolve(ifStmt.Else);
        }
    }

    public void Visit(WhileStmt whileStmt)
    {
        Resolve(whileStmt.Condition);
        Resolve(whileStmt.Body);
    }

    public void Visit(ForStmt forStmt)
    {
        BeginScope();

        Declare(forStmt.LoopIdentifier);
        Define(forStmt.LoopIdentifier);

        Resolve(forStmt.Initializer);
        Resolve(forStmt.Condition);
        Resolve(forStmt.Incrementor);

        Resolve(forStmt.Body.Statements);
        EndScope();
    }

    public void Visit(ForInStmt forInStmt)
    {
        if (forInStmt.KeyIdentifier != null) {
            Declare((Token) forInStmt.KeyIdentifier);
            Define((Token) forInStmt.KeyIdentifier);
        }

        Declare(forInStmt.ValueIdentifier);
        Define(forInStmt.ValueIdentifier);

        Resolve(forInStmt.Expression);
    }

    public void Visit(ExpressionStmt expressionStmt)
    {
        Resolve(expressionStmt.Expression);
    }

    public void Visit(PrintStmt printStmt)
    {
        Resolve(printStmt.Expression);
    }

    public void Visit(FuncStmt funcStmt)
    {
        Declare(funcStmt.Identifier);
        Define(funcStmt.Identifier);

        ResolveFunction(funcStmt, FunctionType.FUNCTION);
    }

    public void Visit(ReturnStmt returnStmt)
    {
        if (returnStmt.Expression != null) {
            if (currentFunction == FunctionType.CONSTRUCTOR) {
                // constructor can't return a value
                throw Error("Constructors can't return a value.", returnStmt.Expression.Location);
            }
            
            Resolve(returnStmt.Expression);
        }
    }

    public void Visit(Binary binary)
    {
        Resolve(binary.Left);
        Resolve(binary.Right);
    }

    public void Visit(Grouping grouping)
    {
        Resolve(grouping.Expression);
    }

    public void Visit(Unary unary)
    {
        Resolve(unary.Right);
    }

    public void Visit(Literal literal)
    {
        // Do nothing
    }

    public void Visit(TypeHint typeHint)
    {
        // Do nothing
    }

    public void Visit(Logical logical)
    {
        Resolve(logical.Left);
        Resolve(logical.Right);
    }

    public void Visit(Call call)
    {
        Resolve(call.Callee);
        foreach (var argument in call.Arguments) {
            Resolve(argument);
        }
    }

    public void Visit(FuncParameter funcParameter)
    {
        if (funcParameter.DefaultValue != null) {
            Resolve(funcParameter.DefaultValue);
        }
    }

    public void Visit(ClassStmt classStmt)
    {
        Declare(classStmt.Identifier);
        Define(classStmt.Identifier);

        BeginScope();
        scopes.Peek().Add("this", true);

        foreach (MethodStmt method in classStmt.Methods) {
            FunctionType declaration = FunctionType.METHOD;
            if (method.Identifier.Value == classStmt.Identifier.Value) {
                declaration = FunctionType.CONSTRUCTOR;
                
            }
            ResolveFunction(method, declaration);
        }

        EndScope();
    }

    public void Visit(PropertyStmt propertyStmt)
    {
        // don't need this yet since properties inside methods are via 'this'
    }

    public void Visit(MethodStmt methodStmt)
    {
        // I guess this is handled by the visit to ClassStmt
    }

    public void Visit(Instantiation instantiation)
    {
        Resolve(instantiation.Call);
    }

    public void Visit(Get get)
    {
        Resolve(get.Expression);
    }

    public void Visit(Set set)
    {
        Resolve(set.ObjectExpression);
        Resolve(set.ValueExpression);
    }

    public void Visit(This dis)
    {
        ResolveLocal(dis, "this");
    }
}