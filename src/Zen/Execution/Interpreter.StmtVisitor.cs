using Zen.Common;
using Zen.Execution.EvaluationResult;
using Zen.Execution.Import;
using Zen.Lexing;
using Zen.Parsing.AST;
using Zen.Parsing.AST.Expressions;
using Zen.Parsing.AST.Statements;
using Zen.Typing;

namespace Zen.Execution;
public partial class Interpreter
{
    public IEvaluationResult Visit(ProgramNode programNode)
    {
        CurrentNode = programNode;
        return Visit(programNode, true);
    }

    public IEvaluationResult Visit(ProgramNode programNode, bool awaitEvents)
    {
        CurrentNode = programNode;
        // try {
            // First, process all top-level statements
            foreach (var statement in programNode.Statements)
            {
                statement.Accept(this);
            }

            if (awaitEvents) {
                // Then wait for all event loop tasks to complete
                var timeout = TimeSpan.FromSeconds(5);
                var startTime = DateTime.Now;

                while (EventLoop.HasPendingTasks)
                {
                    if (DateTime.Now - startTime >= timeout)
                    {
                        throw Error("Event loop tasks did not complete within timeout period");
                    }
                    Thread.Sleep(1); // Small delay to prevent CPU spinning
                }
            }
        // } catch (Exception ex) {
        //     // Wrap unknown exceptions in a RuntimeError with source code info
        //     if (ex is not Common.Error) {
        //         ex = Error("Unhandled exception in program: ", CurrentNode?.Location, ErrorType.RuntimeError, ex);
        //     }

        //     // log it
        //     Logger.Instance.Error(ex.ToString());

        //     throw ex;
        // }

        return VoidResult.Instance;
    }

    public IEvaluationResult Visit(Block block)
    {
        CurrentNode = block;

        return ExecuteBlock(block, new Environment(environment));
    }

    public IEvaluationResult ExecuteBlock(Block block, Environment environment)
    {
        CurrentNode = block;

        Environment previousEnvironment = this.environment;
        this.environment = environment;

        try
        {
            foreach (var statement in block.Statements)
            {
                statement.Accept(this);
            }
        }
        finally
        {
            this.environment = previousEnvironment;
        }

        return VoidResult.Instance;
    }

    public IEvaluationResult Visit(IfStmt ifStmt)
    {
        CurrentNode = ifStmt;

        if (Evaluate(ifStmt.Condition).IsTruthy())
        {
            ifStmt.Then.Accept(this);
        }
        else if (ifStmt.ElseIfs != null)
        {
            foreach (var elseIf in ifStmt.ElseIfs)
            {
                if (Evaluate(elseIf.Condition).IsTruthy())
                {
                    elseIf.Then.Accept(this);
                    break;
                }
            }
        }
        else if (ifStmt.Else != null)
        {
            ifStmt.Else.Accept(this);
        }

        return VoidResult.Instance;
    }

    public IEvaluationResult Visit(PrintStmt printStmt)
    {
        CurrentNode = printStmt;

        IEvaluationResult expResult = Evaluate(printStmt.Expression);

        ZenValue value = expResult.Value;

        string str;
        if (value.Type == ZenType.Object) {
            // custom ToString method
            str = CallObject(value.Underlying!, "ToString", ZenType.String).Underlying!;
        }else {
            // use the underlying ToString
            str = value.Stringify();
        }

        if (GlobalOutputBufferingEnabled)
        {
            GlobalOutputBuffer.Append(str);
        }
        else
        {
            Console.WriteLine(str);
        }

        return (ValueResult)ZenValue.Void;
    }

    public IEvaluationResult Visit(ExpressionStmt expressionStmt)
    {
        CurrentNode = expressionStmt;
        return Evaluate(expressionStmt.Expression);
    }

    public IEvaluationResult Visit(VarStmt varStmt)
    {
        CurrentNode = varStmt;

        string name = varStmt.Identifier.Value;
        ZenType type = ZenType.Null;
        bool nullable = false;
        bool constant = varStmt.Constant;

        // Check if the variable is already defined
        if (environment.Exists(name))
        {
            throw Error($"Variable '{name}' is already defined", varStmt.Identifier.Location, Common.ErrorType.RedefinitionError);
        }

        // check for missing typehint without initializer
        if (varStmt.TypeHint == null && varStmt.Initializer == null)
        {
            throw Error($"Missing type hint for variable '{name}'", varStmt.Identifier.Location, Common.ErrorType.SyntaxError);
        }

        if (varStmt.TypeHint != null)
        {
            type = Evaluate(varStmt.TypeHint).Type;
            nullable = varStmt.TypeHint.Nullable;
        }

        // assign?
        if (varStmt.Initializer != null)
        {
            IEvaluationResult value = Evaluate(varStmt.Initializer);

            // infer type?
            if (varStmt.TypeHint == null)
            {
                type = value.Type;
            }

            environment.Define(constant, name, type!, nullable);
            environment.Assign(name, value.Value);
        }
        else
        {
            // declare only
            environment.Define(constant, name, type!, nullable);
        }

        return (ValueResult)ZenValue.Void;
    }

    public IEvaluationResult Visit(Assignment assignment)
    {
        CurrentNode = assignment;

        // evaluate the expression on the right hand side
        IEvaluationResult right = Evaluate(assignment.Expression);
        Variable leftVariable;

        try 
        {
            // Use resolved scope information if available
            if (Locals.TryGetValue(assignment, out int distance))
            {
                leftVariable = environment.GetAt(distance, assignment.Identifier.Name);
            }
            else
            {
                // Global variable
                leftVariable = globalEnvironment.GetVariable(assignment.Identifier.Name);
            }
        }
        catch
        {
            throw Error($"Undefined variable '{assignment.Identifier.Name}'",
                assignment.Identifier.Location, Common.ErrorType.UndefinedVariable);
        }

        if (leftVariable is null)
        {
            throw Error($"Cannot assign to non-variable '{assignment.Identifier.Name}'",
                assignment.Identifier.Location, Common.ErrorType.RuntimeError);
        }

        if (leftVariable.Constant)
        {
            throw Error($"Cannot assign to constant '{assignment.Identifier.Name}'",
                assignment.Identifier.Location, Common.ErrorType.RuntimeError);
        }

        // perform the assignment operation
        ZenValue newValue = PerformAssignment(assignment.Operator, (ZenValue) leftVariable.Value!, right.Value);

        // update the variable
        leftVariable.Assign(newValue);

        // return the variable
        return (VariableResult) leftVariable;
    }

    public IEvaluationResult Visit(WhileStmt whileStmt)
    {
        CurrentNode = whileStmt;

        IEvaluationResult conditionResult = Evaluate(whileStmt.Condition);

        while (conditionResult.IsTruthy())
        {
            Visit(whileStmt.Body);
            conditionResult = Evaluate(whileStmt.Condition);
        }

        return (ValueResult)ZenValue.Void;
    }

    public IEvaluationResult Visit(ForStmt forStmt)
    {
        CurrentNode = forStmt;

        Environment previousEnvironment = environment;
        environment = new Environment(previousEnvironment, "for");

        try
        {
            Token loopIdentifier = forStmt.LoopIdentifier;
            ValueResult loopValue = (ValueResult)Evaluate(forStmt.Initializer);

            environment.Define(false, loopIdentifier.Value, loopValue.Type, false);
            environment.Assign(loopIdentifier.Value, loopValue.Value);

            Expr condition = forStmt.Condition;
            Expr incrementor = forStmt.Incrementor;

            while (Evaluate(condition).Value.IsTruthy())
            {
                // execute body
                foreach (Stmt statement in forStmt.Body.Statements) {
                    statement.Accept(this);
                }
        
                // increment loop variable
                Evaluate(incrementor);
            }
        }
        finally
        {
            environment = previousEnvironment;
        }

        return (ValueResult)ZenValue.Void;
    }

    public IEvaluationResult Visit(ForInStmt forInStmt)
    {
        CurrentNode = forInStmt;

        ZenValue targetVal = Evaluate(forInStmt.Expression).Value;

        if (targetVal.IsObject() == false)
        {
            throw Error("Cannot iterate over non-object.", forInStmt.Expression.Location, Common.ErrorType.TypeError);
        }

        ZenObject target = (ZenObject) targetVal.Underlying!;

        ZenInterface iterableInterface = FetchSymbol("System/Collections/Iterable", "Iterable").Underlying!;
        ZenInterface enmeratorInterface = FetchSymbol("System/Collections/Enumerator", "Enumerator").Underlying!;

        if (target.Class.Implements(iterableInterface) == false) {
            throw Error($"Class '{target.Class.Name}' does not implement '{iterableInterface.Name}'.", forInStmt.Expression.Location, Common.ErrorType.TypeError);
        }

        Environment previousEnvironment = environment;
        environment = new Environment(previousEnvironment, "for in");

        try
        {
            Token valueIdentifier = forInStmt.ValueIdentifier;
            Token? keyIdentifier = forInStmt.KeyIdentifier;
            TypeHint? keyTypeHint = forInStmt.KeyTypeHint;
            TypeHint? valueTypeHint = forInStmt.ValueTypeHint;
            
            ZenObject enumerator = CallObject(target, "GetEnumerator").Underlying!;

            // the type of the value is the type of the enumerator
            ZenType elementType = target.GetParameter("T").Underlying!;

            environment.Define(false, valueIdentifier.Value, elementType, false);

            if (keyIdentifier != null) {
                environment.Define(false, keyIdentifier.Value.Value, elementType, false);
            }

            while (CallObject(enumerator, "MoveNext").IsTruthy()) {
                environment.Assign(valueIdentifier.Value, CallObject(enumerator, "Current"));

                if (keyIdentifier != null) {
                    environment.Assign(keyIdentifier.Value.Value, enumerator.GetProperty("index"));
                }

                foreach (Stmt statement in forInStmt.Block.Statements) {
                    statement.Accept(this);
                }
            }            
        }
        finally
        {
            environment = previousEnvironment;
        }

        return (ValueResult)ZenValue.Void;
    }

    public IEvaluationResult Visit(ReturnStmt returnStmt)
    {
        CurrentNode = returnStmt;

        IEvaluationResult result;

        if (returnStmt.Expression != null)
        {
            result = Evaluate(returnStmt.Expression);
        }
        else
        {
            result = (ValueResult)ZenValue.Void;
        }

        throw new ReturnException(result, returnStmt.Location);
    }

    protected ZenUserFunction EvaluateFunctionStatement(FuncStmt funcStmt, Environment? closure = null)
    {
        CurrentNode = funcStmt;

        closure ??= environment;

        // function parameters
        List<ZenFunction.Argument> parameters = [];

        for (int i = 0; i < funcStmt.Parameters.Length; i++)
        {
            FunctionParameterResult funcParamResult = (FunctionParameterResult) funcStmt.Parameters[i].Accept(this);
            parameters.Add(funcParamResult.Parameter);
        }

        ZenType returnType = ZenType.Void;
        returnType = Evaluate(funcStmt.ReturnType).Type;

        return new ZenUserFunction(funcStmt.Async, returnType, parameters, funcStmt.Block, closure);
    }

    public IEvaluationResult Visit(FuncStmt funcStmt)
    {
        CurrentNode = funcStmt;

        ZenUserFunction zenFunction = EvaluateFunctionStatement(funcStmt);

        RegisterFunction(funcStmt.Identifier.Value, zenFunction);

        return (ValueResult)ZenValue.Void;
    }

    public IEvaluationResult Visit(ClassStmt classStmt)
    {
        CurrentNode = classStmt;

        environment.Define(true, classStmt.Identifier.Value, ZenType.Class, false);

        ZenClass clazz = EvaluateClassStatement(classStmt);

        // validate the class
        clazz.Validate();

        environment.Assign(classStmt.Identifier.Value, new ZenValue(ZenType.Class, clazz));

        return (ValueResult)ZenValue.Void;
    }

    public IEvaluationResult Visit(InterfaceStmt interfaceStmt)
    {
        CurrentNode = interfaceStmt;

        environment.Define(true, interfaceStmt.Identifier.Value, ZenType.Class, false);

        ZenInterface @interface = EvaluateInterfaceStatement(interfaceStmt);

        environment.Assign(interfaceStmt.Identifier.Value, new ZenValue(ZenType.Interface, @interface));

        return (ValueResult)ZenValue.Void;
    }

    protected ZenInterface EvaluateInterfaceStatement(InterfaceStmt interfaceStmt)
    {
        CurrentNode = interfaceStmt;

        var previousEnvironment = environment;

        try {
            environment = new Environment(environment, "interface");

            // create the methods
            List<ZenAbstractMethod> methods = [];

            foreach (AbstractMethodStmt methodStmt in interfaceStmt.Methods)
            {
                string name = methodStmt.Identifier.Value;
                ZenClass.Visibility visibility = ZenClass.Visibility.Public;
                ZenType returnType;

                if (methodStmt.ReturnType.IsGeneric) {
                    returnType = new ZenType(methodStmt.ReturnType.Name, methodStmt.ReturnType.Nullable, generic: true);
                }else {
                    returnType = Evaluate(methodStmt.ReturnType).Type;
                }

                // method parameters
                List<ZenFunction.Argument> parameters = [];

                for (int i = 0; i < methodStmt.Parameters.Length; i++)
                {
                    FunctionParameterResult funcParamResult = (FunctionParameterResult) methodStmt.Parameters[i].Accept(this);
                    parameters.Add(funcParamResult.Parameter);
                }

                ZenAbstractMethod method = new(methodStmt.Async, name, visibility, returnType, parameters);
                methods.Add(method);
            }

            // parameters
            List<ZenClass.Parameter> genericParameters = [];

            foreach (ParameterDeclaration parameter in interfaceStmt.Parameters) {
                ZenValue? defaultValue = null;
                if (parameter.DefaultValue != null) {
                    defaultValue = Evaluate(parameter.DefaultValue!).Value;
                }

                ZenClass.Parameter param = new(parameter.Name, Evaluate(parameter.Type).Type, defaultValue);
                genericParameters.Add(param);
            }

            return new ZenInterface(interfaceStmt.Identifier.Value, methods, genericParameters);
        } finally {
            environment = previousEnvironment;
        }
    }

    protected ZenClass EvaluateClassStatement(ClassStmt classStmt)
    {
        CurrentNode = classStmt;
        var previousEnvironment = environment;

        try {
            environment = new Environment(environment, "class");
            
            // create the Properties
            List<ZenClass.Property> properties = [];

            foreach (var property in classStmt.Properties)
            {
                CurrentNode = property;

                ZenType type = ZenType.Any;
                ZenValue defaultValue = ZenValue.Null;

                if (property.Initializer != null)
                {
                    IEvaluationResult defaultValueResult = Evaluate(property.Initializer);
                    defaultValue = defaultValueResult.Value;
                    type = defaultValue.Type;

                    if (property.TypeHint != null)
                    {
                        if (property.TypeHint.IsGeneric) {
                            type = new ZenType(property.TypeHint.Name, property.TypeHint.Nullable, generic: true);
                        }else {
                            type = Evaluate(property.TypeHint).Type;
                        }

                        if ( ! TypeChecker.IsCompatible(defaultValue.Type, type))
                        {
                            throw Error($"Cannot assign value of type '{defaultValue.Type}' to property of type '{type}'", property.TypeHint.Location, Common.ErrorType.TypeError);
                        }
                }
                }else {
                    if (property.TypeHint == null) {
                        throw Error($"Property '{property.Identifier.Value}' must have a type hint", property.Identifier.Location, Common.ErrorType.SyntaxError);
                    }

                    if (property.TypeHint.IsGeneric) {
                        type = new ZenType(property.TypeHint.Name, property.TypeHint.Nullable, generic: true);
                    }else {
                        // type = property.TypeHint.GetZenType();
                        type = Evaluate(property.TypeHint).Type;
                        defaultValue = new ZenValue(type, null);
                    }
                }

                ZenClass.Visibility visibility = ZenClass.Visibility.Public;
                
                // check modifiers
                foreach (Token modifier in property.Modifiers)
                {
                    if (modifier.Value == "public")
                    {
                        visibility = ZenClass.Visibility.Public;
                    }
                    else if (modifier.Value == "private")
                    {
                        visibility = ZenClass.Visibility.Private;
                    }
                    else if (modifier.Value == "protected")
                    {
                        visibility = ZenClass.Visibility.Protected;
                    }
                }

                properties.Add(new ZenClass.Property(property.Identifier.Value, type, defaultValue, visibility));
            }

            // create the methods
            List<ZenMethod> methods = [];

            foreach (MethodStmt methodStmt in classStmt.Methods)
            {
                CurrentNode = methodStmt;

                string name = methodStmt.Identifier.Value;
                ZenClass.Visibility visibility = ZenClass.Visibility.Public;
                ZenType returnType;

                if (methodStmt.ReturnType.IsGeneric) {
                    returnType = new ZenType(methodStmt.ReturnType.Name, methodStmt.ReturnType.Nullable, generic: true);
                }else {
                    returnType = Evaluate(methodStmt.ReturnType).Type;
                }

                // method parameters
                List<ZenFunction.Argument> parameters = [];

                for (int i = 0; i < methodStmt.Parameters.Length; i++)
                {
                    FunctionParameterResult funcParamResult = (FunctionParameterResult) methodStmt.Parameters[i].Accept(this);
                    parameters.Add(funcParamResult.Parameter);
                }

                ZenUserMethod method = new(methodStmt.Async, name, visibility, returnType, parameters, methodStmt.Block, environment);
                methods.Add(method);
            }

            // parameters
            List<ZenClass.Parameter> genericParameters = [];

            foreach (ParameterDeclaration parameter in classStmt.Parameters) {
                CurrentNode = parameter;

                ZenValue? defaultValue = null;
                if (parameter.DefaultValue != null) {
                    defaultValue = Evaluate(parameter.DefaultValue!).Value;
                }

                ZenClass.Parameter param = new(parameter.Name, Evaluate(parameter.Type).Type, defaultValue);
                genericParameters.Add(param);
            }

            ZenClass clazz = new ZenClass(classStmt.Identifier.Value, methods, properties, genericParameters);

            // extends?
            if (classStmt.Extends != null) {
                ZenValue val = Evaluate(classStmt.Extends).Value;
                if (val.Type == ZenType.Class) {
                    clazz.SuperClass = val.Underlying!;
                }else {
                    throw Error($"Class '{classStmt.Identifier.Value}' cannot extend non-class type '{val.Type}'", classStmt.Extends.Location, Common.ErrorType.SyntaxError);
                }
            }

            // implements?
            foreach (ImplementsExpr implements in classStmt.Implements) {
                CurrentNode = implements;
                ZenValue val = Evaluate(implements.Identifier).Value;
                if (val.Type == ZenType.Interface) {
                    clazz.Interfaces.Add(val.Underlying!);
                }else {
                    throw Error($"Class '{classStmt.Identifier.Value}' cannot implement non-interface type '{val.Type}'", implements.Location, Common.ErrorType.SyntaxError);
                }
            }
            
            return clazz;
        } finally {
            environment = previousEnvironment;
        }
    }

    public IEvaluationResult Visit(PropertyStmt propertyStmt)
    {
        // this isn't used.
        return (ValueResult)ZenValue.Void;
    }

    public IEvaluationResult Visit(MethodStmt methodStmt)
    {
        // this isn't used.
        return (ValueResult)ZenValue.Void;
    }
    
    public IEvaluationResult Visit(AbstractMethodStmt abstractMethodStmt)
    {
        // this isn't used
        return (ValueResult) ZenValue.Void;
    }
}
