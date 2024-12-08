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
    public async Task<IEvaluationResult> VisitAsync(ProgramNode programNode)
    {
        CurrentNode = programNode;
        foreach (var statement in programNode.Statements)
        {
            await statement.AcceptAsync(this);
        }

        return VoidResult.Instance;
    }

    public async Task<IEvaluationResult> VisitAsync(Block block)
    {
        CurrentNode = block;

        return await ExecuteBlock(block, new Environment(environment));
    }

    public async Task<IEvaluationResult> ExecuteBlock(Block block, Environment environment)
    {
        CurrentNode = block;

        Environment previousEnvironment = this.environment;
        this.environment = environment;

        try
        {
            foreach (var statement in block.Statements)
            {
                await statement.AcceptAsync(this);
            }
        }
        finally
        {
            this.environment = previousEnvironment;
        }

        return VoidResult.Instance;
    }

    public async Task<IEvaluationResult> VisitAsync(IfStmt ifStmt)
    {
        CurrentNode = ifStmt;

        if ((await Evaluate(ifStmt.Condition)).IsTruthy())
        {
            await ifStmt.Then.AcceptAsync(this);
        }
        else if (ifStmt.ElseIfs != null)
        {
            foreach (var elseIf in ifStmt.ElseIfs)
            {
                if ((await Evaluate(elseIf.Condition)).IsTruthy())
                {
                    await elseIf.Then.AcceptAsync(this);
                    break;
                }
            }
        }
        else if (ifStmt.Else != null)
        {
            await ifStmt.Else.AcceptAsync(this);
        }

        return VoidResult.Instance;
    }

    public async Task<IEvaluationResult> VisitAsync(PrintStmt printStmt)
    {
        CurrentNode = printStmt;

        IEvaluationResult expResult = await Evaluate(printStmt.Expression);

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
            if (OutputHandler != null) {
                OutputHandler(str);
            }else {
                Console.Write(str);
            }
        }

        return (ValueResult)ZenValue.Void;
    }

    public async Task<IEvaluationResult> VisitAsync(ThrowStmt throwStmt)
    {
        throw new NotImplementedException();
    }

    public async Task<IEvaluationResult> VisitAsync(ExpressionStmt expressionStmt)
    {
        CurrentNode = expressionStmt;
        return await Evaluate(expressionStmt.Expression);
    }

    public async Task<IEvaluationResult> VisitAsync(VarStmt varStmt)
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
            type = (await Evaluate(varStmt.TypeHint)).Type;
            nullable = varStmt.TypeHint.Nullable;
        }

        // assign?
        if (varStmt.Initializer != null)
        {
            IEvaluationResult value = await Evaluate(varStmt.Initializer);

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

    public async Task<IEvaluationResult> VisitAsync(Assignment assignment)
    {
        CurrentNode = assignment;

        // evaluate the expression on the right hand side
        IEvaluationResult right = await Evaluate(assignment.Expression);
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

    public async Task<IEvaluationResult> VisitAsync(WhileStmt whileStmt)
    {
        CurrentNode = whileStmt;

        IEvaluationResult conditionResult = await Evaluate(whileStmt.Condition);

        while (conditionResult.IsTruthy())
        {
            await VisitAsync(whileStmt.Body);
            conditionResult = await Evaluate(whileStmt.Condition);
        }

        return (ValueResult)ZenValue.Void;
    }

    public async Task<IEvaluationResult> VisitAsync(ForStmt forStmt)
    {
        CurrentNode = forStmt;

        Environment previousEnvironment = environment;
        environment = new Environment(previousEnvironment, "for");

        try
        {
            Token loopIdentifier = forStmt.LoopIdentifier;
            ValueResult loopValue = (ValueResult) await Evaluate(forStmt.Initializer);

            environment.Define(false, loopIdentifier.Value, loopValue.Type, false);
            environment.Assign(loopIdentifier.Value, loopValue.Value);

            Expr condition = forStmt.Condition;
            Expr incrementor = forStmt.Incrementor;

            while ((await Evaluate(condition)).Value.IsTruthy())
            {
                // execute body
                foreach (Stmt statement in forStmt.Body.Statements) {
                    await statement.AcceptAsync(this);
                }
        
                // increment loop variable
                await Evaluate(incrementor);
            }
        }
        finally
        {
            environment = previousEnvironment;
        }

        return (ValueResult)ZenValue.Void;
    }

    public async Task<IEvaluationResult> VisitAsync(ForInStmt forInStmt)
    {
        CurrentNode = forInStmt;

        ZenValue targetVal = (await Evaluate(forInStmt.Expression)).Value;

        if (targetVal.IsObject() == false)
        {
            throw Error("Cannot iterate over non-object.", forInStmt.Expression.Location, Common.ErrorType.TypeError);
        }

        ZenObject target = (ZenObject) targetVal.Underlying!;

        ZenInterface iterableInterface = (await FetchSymbol("System/Collections/Iterable", "Iterable")).Underlying!;
        ZenInterface enmeratorInterface = (await FetchSymbol("System/Collections/Enumerator", "Enumerator")).Underlying!;

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
            
            ZenObject enumerator = CallObject(target, "GetEnumerator", null).Underlying!;

            // the type of the value is the type of the enumerator
            ZenType elementType = target.GetParameter("T").Underlying!;

            environment.Define(false, valueIdentifier.Value, elementType, false);

            if (keyIdentifier != null) {
                environment.Define(false, keyIdentifier.Value.Value, elementType, false);
            }

            while (CallObject(enumerator, "MoveNext", null).IsTruthy()) {
                environment.Assign(valueIdentifier.Value, CallObject(enumerator, "Current", null));

                if (keyIdentifier != null) {
                    environment.Assign(keyIdentifier.Value.Value, enumerator.GetProperty("index"));
                }

                foreach (Stmt statement in forInStmt.Block.Statements) {
                    await statement.AcceptAsync(this);
                }
            }            
        }
        finally
        {
            environment = previousEnvironment;
        }

        return (ValueResult)ZenValue.Void;
    }

    public async Task<IEvaluationResult> VisitAsync(ReturnStmt returnStmt)
    {
        CurrentNode = returnStmt;

        IEvaluationResult result;

        if (returnStmt.Expression != null)
        {
            result = await Evaluate(returnStmt.Expression);
        }
        else
        {
            result = (ValueResult)ZenValue.Void;
        }

        throw new ReturnException(result, returnStmt.Location);
    }

    /// <summary>
    /// Creates a ZenFunction from a FuncStmt, capturing the current environment if none is provided.
    /// </summary>
    /// <returns></returns>
    protected async Task<ZenFunction> EvaluateFunctionStatement(FuncStmt funcStmt, Environment? closure = null)
    {
        CurrentNode = funcStmt;

        closure ??= environment;

        // function parameters
        List<ZenFunction.Argument> parameters = [];

        for (int i = 0; i < funcStmt.Parameters.Length; i++)
        {
            IEvaluationResult funcParam = await Evaluate(funcStmt.Parameters[i]);
            FunctionParameterResult funcParamResult = (FunctionParameterResult) funcParam;
            parameters.Add(funcParamResult.Parameter);
        }

        ZenType returnType = (await Evaluate(funcStmt.ReturnType)).Type;

        return ZenFunction.NewUserFunction(returnType, parameters, funcStmt.Block, closure, funcStmt.Async);
    }

    public async Task<IEvaluationResult> VisitAsync(FuncStmt funcStmt)
    {
        CurrentNode = funcStmt;

        ZenFunction zenFunction = await EvaluateFunctionStatement(funcStmt);
        RegisterFunction(funcStmt.Identifier.Value, zenFunction);

        return (ValueResult)ZenValue.Void;
    }

    public async Task<IEvaluationResult> VisitAsync(ClassStmt classStmt)
    {
        CurrentNode = classStmt;

        environment.Define(true, classStmt.Identifier.Value, ZenType.Class, false);

        ZenClass clazz = await EvaluateClassStatement(classStmt);

        // validate the class
        clazz.Validate();

        environment.Assign(classStmt.Identifier.Value, new ZenValue(ZenType.Class, clazz));

        return (ValueResult)ZenValue.Void;
    }

    public async Task<IEvaluationResult> VisitAsync(InterfaceStmt interfaceStmt)
    {
        CurrentNode = interfaceStmt;

        environment.Define(true, interfaceStmt.Identifier.Value, ZenType.Class, false);

        ZenInterface @interface = await EvaluateInterfaceStatement(interfaceStmt);

        environment.Assign(interfaceStmt.Identifier.Value, new ZenValue(ZenType.Interface, @interface));

        return (ValueResult)ZenValue.Void;
    }

    protected async Task<ZenInterface> EvaluateInterfaceStatement(InterfaceStmt interfaceStmt)
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
                    returnType = ZenType.GenericParameter(methodStmt.ReturnType.Name);
                }else {
                    returnType = (await Evaluate(methodStmt.ReturnType)).Type;
                }

                // method parameters
                List<ZenFunction.Argument> parameters = [];

                for (int i = 0; i < methodStmt.Parameters.Length; i++)
                {
                    IEvaluationResult funcParamResult = await Evaluate(methodStmt.Parameters[i]);
                    FunctionParameterResult funcParam = (FunctionParameterResult) funcParamResult;
                    parameters.Add(funcParam.Parameter);
                }

                ZenAbstractMethod method = new(methodStmt.Async, name, visibility, returnType, parameters);
                methods.Add(method);
            }

            // parameters
            List<IZenClass.Parameter> genericParameters = [];

            foreach (ParameterDeclaration parameter in interfaceStmt.Parameters) {
                ZenValue? defaultValue = null;
                if (parameter.DefaultValue != null) {
                    defaultValue = (await Evaluate(parameter.DefaultValue!)).Value;
                }

                IZenClass.Parameter param = new(parameter.Name, (await Evaluate(parameter)).Type, defaultValue);
                genericParameters.Add(param);
            }

            return new ZenInterface(interfaceStmt.Identifier.Value, methods, genericParameters);
        } finally {
            environment = previousEnvironment;
        }
    }

    protected async Task<ZenClass> EvaluateClassStatement(ClassStmt classStmt)
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
                    IEvaluationResult defaultValueResult = await Evaluate(property.Initializer);
                    defaultValue = defaultValueResult.Value;
                    type = defaultValue.Type;

                    if (property.TypeHint != null)
                    {
                        if (property.TypeHint.IsGeneric) {
                            type = ZenType.GenericParameter(property.TypeHint.Name);
                        }else {
                            type = (await Evaluate(property.TypeHint)).Type;
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
                        type = ZenType.GenericParameter(property.TypeHint.Name);
                    }else {
                        // type = property.TypeHint.GetZenType();
                        type = (await Evaluate(property.TypeHint)).Type;
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
            List<ZenFunction> methods = [];

            foreach (MethodStmt methodStmt in classStmt.Methods)
            {
                CurrentNode = methodStmt;

                string name = methodStmt.Identifier.Value;
                ZenClass.Visibility visibility = ZenClass.Visibility.Public;
                ZenType returnType;

                if (methodStmt.ReturnType.IsGeneric) {
                    //TODO: handle nullables
                    returnType = ZenType.GenericParameter(methodStmt.ReturnType.Name);
                }else {
                    returnType = (await Evaluate(methodStmt.ReturnType)).Type;
                }

                // method parameters
                // todo: handle generics
                List<ZenFunction.Argument> parameters = [];

                for (int i = 0; i < methodStmt.Parameters.Length; i++)
                {
                    IEvaluationResult funcParamResult = await Evaluate(methodStmt.Parameters[i]);
                    FunctionParameterResult funcParam = (FunctionParameterResult) funcParamResult;
                    parameters.Add(funcParam.Parameter);
                }

                ZenFunction method = ZenFunction.NewUserMethod(name, returnType, parameters, methodStmt.Block, environment, false);
                methods.Add(method);
            }

            // parameters
            List<IZenClass.Parameter> genericParameters = [];

            foreach (ParameterDeclaration parameter in classStmt.Parameters) {
                CurrentNode = parameter;

                ZenValue? defaultValue = null;
                if (parameter.DefaultValue != null) {
                    defaultValue = (await Evaluate(parameter.DefaultValue!)).Value;
                }

                IZenClass.Parameter param = new(parameter.Name, (await Evaluate(parameter)).Type, defaultValue);
                genericParameters.Add(param);
            }

            ZenClass clazz = new ZenClass(classStmt.Identifier.Value, methods, properties, genericParameters);

            // extends?
            if (classStmt.Extends != null) {
                ZenValue val = (await Evaluate(classStmt.Extends)).Value;
                if (val.Type == ZenType.Class) {
                    clazz.SuperClass = val.Underlying!;
                }else {
                    throw Error($"Class '{classStmt.Identifier.Value}' cannot extend non-class type '{val.Type}'", classStmt.Extends.Location, Common.ErrorType.SyntaxError);
                }
            }

            // implements?
            foreach (ImplementsExpr implements in classStmt.Implements) {
                CurrentNode = implements;
                ZenValue val = (await Evaluate(implements.Identifier)).Value;
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

    public async Task<IEvaluationResult> VisitAsync(PropertyStmt propertyStmt)
    {
        // this isn't used.
        return (ValueResult)ZenValue.Void;
    }

    public async Task<IEvaluationResult> VisitAsync(MethodStmt methodStmt)
    {
        // this isn't used.
        return (ValueResult)ZenValue.Void;
    }
    
    public async Task<IEvaluationResult> VisitAsync(AbstractMethodStmt abstractMethodStmt)
    {
        // this isn't used
        return (ValueResult) ZenValue.Void;
    }
}