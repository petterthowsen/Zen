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

    public void Resolve(ProgramNode program, bool global = false) {
        Reset();

        Logger.Instance.Debug("RESOLVING PROGRAM: " + program.Location);

        if (global == false) {
            BeginScope();
        }

        foreach (var statement in program.Statements) {
            Resolve(statement);
        }

        if (global == false) {
            EndScope();
        }
    }


    public void Resolve(Stmt[] statements) {
        foreach (var statement in statements) {
            statement.Accept(this);
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

    public void BeginScope() {
        scopes.Push([]);
        //Logger.Instance.Debug($"[RESOLVER] Pushed new scope. Total scopes: {scopes.Count}");
    }

    public void EndScope() {
        var scope = scopes.Pop();
        //Logger.Instance.Debug($"[RESOLVER] Popped scope with variables: {string.Join(", ", scope.Keys)}. Remaining scopes: {scopes.Count}");
    }

    /// <summary>
    /// Declare a variable in the current scope. The variable is initially considered uninitialized.
    /// </summary>
    /// <param name="name">The name of the variable to declare.</param>
    private void Declare(Token name) {
        if (scopes.Count == 0) return;

        Dictionary<String, Boolean> scope = scopes.Peek();
        
        if (scope.ContainsKey(name.Value)) {
            throw Interpreter.Error("Variable with this name already declared in this scope.", name.Location, ErrorType.RedefinitionError);
        }

        //Logger.Instance.Debug($"[RESOLVER] Declaring variable '{name.Value}' in scope at depth {scopes.Count - 1}");
        scope.Add(name.Value, false);
    }

    private void Declare(string name, SourceLocation? location) {
        if (scopes.Count == 0) return;

        Dictionary<String, Boolean> scope = scopes.Peek();
        
        if (scope.ContainsKey(name)) {
            throw Interpreter.Error("Variable with this name already declared in this scope.", location, ErrorType.RedefinitionError);
        }

        //Logger.Instance.Debug($"[RESOLVER] Declaring variable '{name}' in scope at depth {scopes.Count - 1}");
        scope.Add(name, false);
    }

    private void Define(Token name) {
        if (scopes.Count == 0) return;

        Dictionary<String, Boolean> scope = scopes.Peek();
        //Logger.Instance.Debug($"[RESOLVER] Defining variable '{name.Value}' in scope at depth {scopes.Count - 1}");
        scope[name.Value] = true;
    }

    private void Define(string name)
    {
        if (scopes.Count == 0) return;

        Dictionary<String, Boolean> scope = scopes.Peek();
        //Logger.Instance.Debug($"[RESOLVER] Defining variable '{name}' in scope at depth {scopes.Count - 1}");
        scope[name] = true;
    }

    /// <summary>
    ///     Resolves a local variable by name.
    /// </summary>
    /// <remarks>
    ///     This looks, for good reason, a lot like the code in Environment for evaluating a variable.
    ///     We start at the innermost scope and work outwards, looking in each map for a matching name.
    ///     If we find the variable, we resolve it, passing in the number of scopes between the current innermost scope and the scope where the variable was found.
    ///     So, if the variable was found in the current scope, we pass in 0. If it's in the immediately enclosing scope, 1. You get the idea.
    /// </remarks>
    /// <param name="expr"></param>
    /// <param name="name"></param>
    private void ResolveLocal(Node expr, string name) {
        var scopesList = scopes.ToList(); // Top scope is at index 0
        //Logger.Instance.Debug($"[RESOLVER] Resolving '{name}'. Scope chain from innermost to outermost:");
        for (int i = 0; i < scopesList.Count; i++) { // Iterate from innermost to outermost
            var vars = string.Join(", ", scopesList[i].Keys);
            //Logger.Instance.Debug($"[RESOLVER] Scope {i}: {vars}");
            if (scopesList[i].ContainsKey(name)) {
                var distance = i; // Correct distance calculation
                //Logger.Instance.Debug($"[RESOLVER] Found '{name}' in scope {i}, distance: {distance}");
                interpreter.Resolve(expr, distance);
                return;
            }
        }

        //Logger.Instance.Debug($"[RESOLVER] Variable '{name}' not found in any scope, assuming global");
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

        if (varStmt.TypeHint != null) {
            Resolve(varStmt.TypeHint);
        }
    }

    public void Visit(Assignment assignment)
    {
        Resolve(assignment.Expression);
        ResolveLocal(assignment, assignment.Identifier.Name);
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
        BeginScope();

        if (forInStmt.KeyIdentifier != null) {
            Declare((Token) forInStmt.KeyIdentifier);
            Define((Token) forInStmt.KeyIdentifier);
        }

        Declare(forInStmt.ValueIdentifier);
        Define(forInStmt.ValueIdentifier);

        Resolve(forInStmt.Expression);
        Resolve(forInStmt.Block.Statements);

        EndScope();
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
        // if scopes.Count == 0, we're in the global scope
        // there's therefore no need to resolve the identifier since
        // the Interpreter will get it from the global environment
        if (scopes.Count == 0) return;

        Dictionary<String, Boolean> scope = scopes.Peek();

        if (scope.ContainsKey(typeHint.Name) && !scope[typeHint.Name]) {
            Error("Cannot read local variable in its own initializer.", typeHint.Location);
        }

        // types like T don't need to be resolved, they're handled in the interpreter.
        if (typeHint.IsGeneric) {
            return;
        }

        ResolveLocal(typeHint, typeHint.Name);
        if (typeHint.IsParametric) {
            foreach (TypeHint parameterTypeHint in typeHint.Parameters) {
                Resolve(parameterTypeHint);
            }
        }
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

        // Class scope for properties and method signatures
        BeginScope();

        // resolve super class
        if (classStmt.Extends != null) {
            Resolve(classStmt.Extends);
        }

        // resolve interfaces
        foreach (var implementsExpr in classStmt.Implements) {
            Resolve(implementsExpr);
        }

        //Logger.Instance.Debug($"[RESOLVER] Beginning class scope for {classStmt.Identifier.Value}");
        scopes.Peek().Add("this", true);

        // Make class parameters available in method scopes
        foreach (var param in classStmt.Parameters) {
            scopes.Peek().Add(param.Name, true);
        }

        foreach (MethodStmt method in classStmt.Methods) {
            FunctionType declaration = FunctionType.METHOD;
            if (method.Identifier.Value == classStmt.Identifier.Value) {
                declaration = FunctionType.CONSTRUCTOR;
            }
            ResolveFunction(method, declaration);
        }

        //Logger.Instance.Debug($"[RESOLVER] Ending class scope for {classStmt.Identifier.Value}");
        EndScope();
    }

      protected void ResolveFunction(FuncStmt funcStmt, FunctionType type) {
        FunctionType enclosingFunction = currentFunction;
        currentFunction = type;

        Resolve(funcStmt.ReturnType);

        BeginScope();
        //Logger.Instance.Debug($"[RESOLVER] Beginning function scope for {funcStmt.Identifier.Value}, type: {type}");

        // If this is a method, make class parameters available in its scope
        if (type == FunctionType.METHOD || type == FunctionType.CONSTRUCTOR) {
            // The class parameters are already in the parent scope (from Visit(ClassStmt))
            // We need to copy them into the method's scope
            var classScope = scopes.Skip(1).First();  // Parent scope is the class scope
            foreach (var param in classScope.Where(p => p.Key != "this")) {
                scopes.Peek().Add(param.Key, true);
            }
        }

        foreach (var parameter in funcStmt.Parameters) {
            Declare(parameter.Identifier);
            Define(parameter.Identifier);
        }
        
        Resolve(funcStmt.Block.Statements);
        
        //Logger.Instance.Debug($"[RESOLVER] Ending function scope for {funcStmt.Identifier.Value}");
        EndScope();
        currentFunction = enclosingFunction;
    }

      protected void ResolveAbstractMethod(AbstractMethodStmt funcStmt, FunctionType type) {
        FunctionType enclosingFunction = currentFunction;
        currentFunction = type;

        // return type
        Resolve(funcStmt.ReturnType);

        BeginScope();
        //Logger.Instance.Debug($"[RESOLVER] Beginning function scope for {funcStmt.Identifier.Value}, type: {type}");

        // If this is a method, make class parameters available in its scope
        if (type == FunctionType.METHOD || type == FunctionType.CONSTRUCTOR) {
            // The class parameters are already in the parent scope (from Visit(ClassStmt))
            // We need to copy them into the method's scope
            // var classScope = scopes.Skip(1).First();  // Parent scope is the class scope
            // foreach (var param in classScope.Where(p => p.Key != "this")) {
            //     scopes.Peek().Add(param.Key, true);
            // }
        }

        foreach (var parameter in funcStmt.Parameters) {
            Declare(parameter.Identifier);
            Define(parameter.Identifier);
        }

        //Logger.Instance.Debug($"[RESOLVER] Ending function scope for {funcStmt.Identifier.Value}");
        EndScope();
        currentFunction = enclosingFunction;
    }

    public void Visit(ImplementsExpr implementsExpr)
    {
        Resolve(implementsExpr.Identifier);
        foreach(Expr expr in implementsExpr.Parameters) {
            Resolve(expr); //not entirely sure if we need to do this
        }
    }

    public void Visit(InterfaceStmt interfaceStmt)
    {
        Declare(interfaceStmt.Identifier);
        Define(interfaceStmt.Identifier);

        BeginScope();
        scopes.Peek().Add("this", true);

        // Make class parameters available in method scopes
        foreach (var param in interfaceStmt.Parameters) {
            scopes.Peek().Add(param.Name, true);
        }

        //abstract methods don't have bodies, so no need for this here.
        foreach (AbstractMethodStmt method in interfaceStmt.Methods) {
            FunctionType declaration = FunctionType.METHOD;
            if (method.Identifier.Value == interfaceStmt.Identifier.Value) {
                declaration = FunctionType.CONSTRUCTOR;
            }
            ResolveAbstractMethod(method, declaration);
        }

        EndScope();
    }

    public void Visit(PropertyStmt propertyStmt)
    {
        // don't need this yet since properties inside methods are via 'this'
    }

    public void Visit(MethodStmt methodStmt)
    {
        // handled by the visit to ClassStmt
    }

    public void Visit(AbstractMethodStmt abstractMethodStmt)
    {
        // handled by visit to InterfaceStmt
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

    public void Visit(BracketGet bracketGet)
    {
        Resolve(bracketGet.Target);
        Resolve(bracketGet.Element);
    }

    public void Visit(BracketSet bracketSet)
    {
        Resolve(bracketSet.Target);
        Resolve(bracketSet.Element);
        Resolve(bracketSet.ValueExpression);
    }

    public void Visit(This dis)
    {
        ResolveLocal(dis, "this");
    }

    public void Visit(TypeCheck typeCheck)
    {
        Resolve(typeCheck.Expression);
    }

    public void Visit(TypeCast typeCast)
    {
        Resolve(typeCast.Expression);
    }

    public void Visit(ParameterDeclaration parameter)
    {
        // Resolve the type hint
        Resolve(parameter.Type);

        // For value constraints, resolve the default value if present
        if (!parameter.IsTypeParameter && parameter.DefaultValue != null)
        {
            Resolve(parameter.DefaultValue);
        }
    }

    public void Visit(Await await)
    {
        Resolve(await.Expression);
    }

    public void Visit(Identifier identifier)
    {
        // if scopes.Count == 0, we're in the global scope
        // there's therefore no need to resolve the identifier since
        // the Interpreter will get it from the global environment
        if (scopes.Count == 0) return;

        Dictionary<String, Boolean> scope = scopes.Peek();

        if (scope.ContainsKey(identifier.Name) && !scope[identifier.Name]) {
            Error("Cannot read local variable in its own initializer.", identifier.Location);
        }

        ResolveLocal(identifier, identifier.Name);//identifier.Name == identifier.Token.Value
    }

    public void Visit(ImportStmt importStmt)
    {
        string name;

        // For "import X as Y", declare Y in the current scope
        if (importStmt.Alias != null)
        {
            Token alias = (Token) importStmt.Alias;
            name = alias.Value;
        }
        else
        {
            // For "import X", declare X in the current scope
            name = importStmt.Path.Last();
        }
        Declare(name, importStmt.Location);
        Define(name);
    }

    public void Visit(FromImportStmt fromImportStmt)
    {
        // For "from X import Y", declare Y in the current scope
        foreach (var symbol in fromImportStmt.Symbols)
        {
            Declare(symbol);
            Define(symbol);
        }
    }

    public void Visit(PackageStmt packageStmt)
    {
        // handled by the Importer
    }

    /// <summary>
    /// Declares a union type and resolves its member types.
    /// </summary>
    /// <param name="typeStmt"></param>
    public void Visit(TypeStmt typeStmt)
    {
        Declare(typeStmt.Identifier.Name, typeStmt.Location);
        Define(typeStmt.Identifier.Name);

        // Resolve the type expression
        foreach (Identifier type in typeStmt.Types)
        {
            Resolve(type);
        }
    }
}
