using Zen.Parsing.AST;
using Zen.Parsing.AST.Expressions;
using Zen.Parsing.AST.Statements;
using Zen.Support;

namespace Zen.Parsing;

public class DebugPrintVisitor : IVisitor {

    private readonly IndentedStringBuilder _sb = new();

    public override string ToString() => _sb.ToString();

    public void Visit(ProgramNode programNode)
    {
        _sb.Add(programNode.ToString());
        _sb.Indent++;

        foreach (var stmt in programNode.Statements) {
            stmt.Accept(this);
        }

        _sb.Indent--;
    }

    public void Visit(IfStmt ifStmt)
    {
        _sb.Add(ifStmt.ToString());
        _sb.Indent++;

        ifStmt.Condition.Accept(this);
        ifStmt.Then.Accept(this);

        //TODO: else blocks

        _sb.Indent--;
    }

    public void Visit(Binary binary)
    {
        _sb.Add(binary.ToString());

        _sb.Indent++;
        
        binary.Left.Accept(this);
        _sb.Add(binary.Operator.ToString());
        binary.Right.Accept(this);
        
        _sb.Indent--;
    }

    public void Visit(Grouping grouping)
    {
        _sb.Add(grouping.ToString());

        _sb.Indent++;
        
        grouping.Expression.Accept(this);
        
        _sb.Indent--;
    }

    public void Visit(Unary unary)
    {
        _sb.Add(unary.ToString());
        _sb.Indent++;
        
        _sb.Add(unary.Operator.ToString());
        unary.Right.Accept(this);
        
        _sb.Indent--;
    }

    public void Visit(Literal literal)
    {
        _sb.Add(literal.ToString());
    }

    public void Visit(PrintStmt printStmt)
    {
        _sb.Add(printStmt.ToString());
        
        _sb.Indent++;
        
        printStmt.Expression.Accept(this);
        
        _sb.Indent--;
    }

    public void Visit(ExpressionStmt expressionStmt)
    {
        _sb.Add(expressionStmt.ToString());
        
        _sb.Indent++;
        
        expressionStmt.Expression.Accept(this);
        
        _sb.Indent--;
    }

    public void Visit(Block block)
    {
        _sb.Add(block.ToString());

        _sb.Indent++;
        
        foreach (var stmt in block.Statements) {
            stmt.Accept(this);
        }
        
        _sb.Indent--;
    }

    public void Visit(WhileStmt whileStmt)
    {
        _sb.Add(whileStmt.ToString());

        _sb.Indent++;
        
        whileStmt.Condition.Accept(this);
        whileStmt.Body.Accept(this);
        
        _sb.Indent--;
    }

    public void Visit(VarStmt varStmt)
    {
        _sb.Add(varStmt.ToString());
        
        _sb.Indent++;
        
        _sb.Add(varStmt.Identifier.ToString());

        if (varStmt.Initializer != null) {
            varStmt.Initializer.Accept(this);
        }

        _sb.Indent--;
    }

    public void Visit(ForStmt forStmt)
    {
        _sb.Add(forStmt.ToString());

        _sb.Indent++;

        if (forStmt.Initializer != null)
        {
            forStmt.Initializer.Accept(this);
        }
        
        forStmt.Condition.Accept(this);

        if (forStmt.Incrementor != null)
        {
            forStmt.Incrementor.Accept(this);
        }

        _sb.Add("Body:");
        _sb.Indent++;
        foreach (var stmt in forStmt.Body.Statements)
        {
            stmt.Accept(this);
        }
        _sb.Indent--;

        _sb.Indent--;
    }

    public void Visit(ForInStmt forInStmt) {
        _sb.Add(forInStmt.ToString());

        _sb.Indent++;

        forInStmt.Expression.Accept(this);
        forInStmt.Block.Accept(this);

        _sb.Indent--;
    }

    public void Visit(TypeHint typeHint) {
        _sb.Add(typeHint.ToString());
    }

    public void Visit(Identifier identifier) {
        _sb.Add(identifier.ToString());
    }

    public void Visit(Assignment assignment) {
        _sb.Add(assignment.ToString());

        _sb.Indent++;

        assignment.Identifier.Accept(this);
        assignment.Expression.Accept(this);

        _sb.Indent--;
    }

    public void Visit(Logical logical)
    {
        _sb.Add(logical.ToString());

        _sb.Indent++;
        
        logical.Left.Accept(this);
        _sb.Add(logical.Token.Value);
        logical.Right.Accept(this);
        
        _sb.Indent--;
    }

    public void Visit(Call call) {
        _sb.Add(call.ToString());

        _sb.Indent++;

        call.Callee.Accept(this);

        _sb.Add("Arguments:");
        _sb.Indent++;

        foreach (var arg in call.Arguments) {
            arg.Accept(this);
        }

        _sb.Indent--;

        _sb.Indent--;
    }

    public void Visit(FuncStmt funcStmt)
    {
        _sb.Add(funcStmt.ToString());
        _sb.Indent++;
        
        _sb.Add("Async: " + funcStmt.Async);
        _sb.Add("Identifier: " + funcStmt.Identifier.ToString());
        _sb.Add("Return Type: " + funcStmt.ReturnType.ToString());

        _sb.Add("Parameters:");
        _sb.Indent++;
        foreach (var param in funcStmt.Parameters) {
            param.Accept(this);
        }
        _sb.Indent--;

        _sb.Indent--;
    }

    public void Visit(FuncParameter funcParameter)
    {
        _sb.Add(funcParameter.ToString());
        _sb.Indent++;
        
        _sb.Add("Identifier: " + funcParameter.Identifier.ToString());
        if (funcParameter.TypeHint != null) {
            _sb.Add("Type Hint: " + funcParameter.TypeHint.ToString());
        }
        if (funcParameter.DefaultValue != null) {
            _sb.Add("Default Value: " + funcParameter.DefaultValue.ToString());
        }
        
        _sb.Indent--;
    }

    public void Visit(ReturnStmt returnStmt)
    {
        _sb.Add(returnStmt.ToString());

        _sb.Indent++;

        if (returnStmt.Expression != null) {
            returnStmt.Expression.Accept(this);
        }
        
        _sb.Indent--;
    }

    public void Visit(ClassStmt classStmt)
    {
        _sb.Add(classStmt.ToString());
        _sb.Indent++;

        _sb.Add("Token: " + classStmt.Token.ToString());
        _sb.Add("Identifier: " + classStmt.Identifier.ToString());

        _sb.Add("Properties:");
        foreach (var property in classStmt.Properties) {
            property.Accept(this);
        }
        _sb.Indent++;
        _sb.Indent--;

        _sb.Add("Methods:");
        foreach (var method in classStmt.Methods) {
            method.Accept(this);
        }
        _sb.Indent++;
        _sb.Indent--;

        _sb.Indent--;
    }

    public void Visit(InterfaceStmt iStmt)
    {
        _sb.Add(iStmt.ToString());
        _sb.Indent++;

        _sb.Add("Token: " + iStmt.Token.ToString());
        _sb.Add("Identifier: " + iStmt.Identifier.ToString());


        _sb.Add("Methods:");
        foreach (var method in iStmt.Methods) {
            method.Accept(this);
        }
        _sb.Indent++;
        _sb.Indent--;

        _sb.Indent--;
    }

    public void Visit(PropertyStmt propertyStmt)
    {
        _sb.Add(propertyStmt.ToString());
        _sb.Indent++;

        _sb.Add("Identifier: " + propertyStmt.Identifier.ToString());

        if (propertyStmt.TypeHint != null) {
            _sb.Add("Type Hint: " + propertyStmt.TypeHint.ToString());
        }
        
        _sb.Add("Modifiers: " + string.Join(", ", propertyStmt.Modifiers.Select(m => m.Value)));

        if (propertyStmt.Initializer != null) {
            _sb.Add("Initializer:");
            _sb.Indent++;
            propertyStmt.Initializer.Accept(this);
            _sb.Indent--;
        }


        _sb.Indent--;
    }

    public void Visit(MethodStmt methodStmt)
    {
        _sb.Add(methodStmt.ToString());
        _sb.Indent++;

        _sb.Add("Identifier: " + methodStmt.Identifier.ToString());
        _sb.Add("Return Type: " + methodStmt.ReturnType.ToString());
        _sb.Add("Modifiers: " + string.Join(", ", methodStmt.Modifiers.Select(m => m.Value)));

        _sb.Add("Parameters:");
        _sb.Indent++;
        foreach (var param in methodStmt.Parameters) {
            param.Accept(this);
        }
        _sb.Indent--;
        //parameters
        
        _sb.Add("Body:");
        _sb.Indent++;
        foreach (var stmt in methodStmt.Block.Statements) {
            stmt.Accept(this);
        }
        _sb.Indent--;
        //body

        _sb.Indent--;
    }

    public void Visit(AbstractMethodStmt methodStmt)
    {
        _sb.Add(methodStmt.ToString());
        _sb.Indent++;

        _sb.Add("Identifier: " + methodStmt.Identifier.ToString());
        _sb.Add("Return Type: " + methodStmt.ReturnType.ToString());
        _sb.Add("Modifiers: " + string.Join(", ", methodStmt.Modifiers.Select(m => m.Value)));

        _sb.Add("Parameters:");
        _sb.Indent++;
        foreach (var param in methodStmt.Parameters) {
            param.Accept(this);
        }
        _sb.Indent--;

        _sb.Indent--;
    }

    public void Visit(Instantiation instantiation)
    {
        _sb.Add(instantiation.ToString());
        _sb.Indent++;

        instantiation.Call.Accept(this);

        _sb.Indent--;
    }

    public void Visit(Get get)
    {
        _sb.Add(get.ToString());
        _sb.Indent++;

        _sb.Add("Expression:");
        _sb.Indent++;
        get.Expression.Accept(this);
        _sb.Indent--;

        _sb.Add("Identifier: " + get.Identifier.ToString());

        _sb.Indent--;
    }

    public void Visit(Set set)
    {
        _sb.Add(set.ToString());
        _sb.Indent++;

        _sb.Add("ObjectExpression:");
        _sb.Indent++;
        set.ObjectExpression.Accept(this);
        _sb.Indent--;

        _sb.Add("Identifier: " + set.Identifier.ToString());

        _sb.Add("Operator: " + set.Operator.ToString());

        _sb.Add("ValueExpression:");
        _sb.Indent++;
        set.ValueExpression.Accept(this);
        _sb.Indent--;

        _sb.Indent--;
    }

    public void Visit(This dis)
    {
        _sb.Add(dis.ToString());
    }

    public void Visit(TypeCheck typeCheck)
    {
        _sb.Add(typeCheck.ToString());
        _sb.Indent++;
        
        _sb.Add("Type: " + typeCheck.Type.ToString());

        _sb.Add("Expression:");
        _sb.Indent++;
        typeCheck.Expression.Accept(this);
        _sb.Indent--;

        _sb.Indent--;
    }

    public void Visit(TypeCast typeCast)
    {
        _sb.Add(typeCast.ToString());
        _sb.Indent++;

        _sb.Add("Expression: ");
        _sb.Indent++;
        typeCast.Expression.Accept(this);
        _sb.Indent--;

        _sb.Indent--;
    }

    public void Visit(Await await)
    {
        _sb.Add(await.ToString());
        _sb.Indent++;
        await.Expression.Accept(this);
        _sb.Indent--;
    }

    public void Visit(ImportStmt importStmt)
    {
        _sb.Add(importStmt.ToString());
    }

    public void Visit(FromImportStmt fromImportStmt)
    {
        _sb.Add(fromImportStmt.ToString());
    }

    public void Visit(PackageStmt packageStmt)
    {
        _sb.Add(packageStmt.ToString());
    }

    public void Visit(BracketGet bracketGet)
    {
        _sb.Add(bracketGet.ToString());

        _sb.Add("Target:");
        _sb.Indent++;
        bracketGet.Target.Accept(this);
        _sb.Indent--;

        _sb.Add("Element:");
        _sb.Indent++;
        bracketGet.Element.Accept(this);
        _sb.Indent--;
    }

    public void Visit(BracketSet bracketSet)
    {
        _sb.Add(bracketSet.ToString());

        _sb.Add("Target:");
        _sb.Indent++;
        bracketSet.Target.Accept(this);
        _sb.Indent--;

        _sb.Add("Element:");
        _sb.Indent++;
        bracketSet.Element.Accept(this);

        _sb.Add("Value:");
        _sb.Indent++;
        bracketSet.ValueExpression.Accept(this);
        _sb.Indent--;
    }

    public void Visit(ParameterDeclaration typeHintParam)
    {
        _sb.Add(typeHintParam.ToString());
    }

    public void Visit(ImplementsExpr implementsExpr)
    {
        _sb.Add(implementsExpr.ToString());
        _sb.Indent++;

        _sb.Add("Identifier: " + implementsExpr.Identifier.ToString());
        _sb.Add("Parameters: " + string.Join(", ", implementsExpr.Parameters.Select(p => p.ToString())));

        _sb.Indent--;
    }

    public void Visit(TypeStmt typeStmt)
    {
        _sb.Add(typeStmt.ToString());
        _sb.Indent++;

        _sb.Add("Identifier: " + typeStmt.Identifier.ToString());
        _sb.Add("Types: " + string.Join(", ", typeStmt.Types.Select(t => t.ToString())));    

        _sb.Indent--;
    }

    public void Visit(ArrayLiteral arrayLiteral)
    {
        _sb.Add(arrayLiteral.ToString());
        _sb.Indent++;

        _sb.Add("Elements:");
        foreach (var item in arrayLiteral.Items)
        {
            _sb.Indent++;
            item.Accept(this);
            _sb.Indent--;
        }

        _sb.Indent--;
    }

    public void Visit(ThrowStmt throwStmt)
    {
        _sb.Add(throwStmt.ToString());
        _sb.Indent++;

        _sb.Add("Expression:");
        _sb.Indent++;
        throwStmt.Expression.Accept(this);
        _sb.Indent--;

        _sb.Indent--;
    }

    public void Visit(TryStmt tryStmt)
    {
        _sb.Add(tryStmt.ToString());
        _sb.Indent++;

        _sb.Add("Block:");
        _sb.Indent++;
        tryStmt.Block.Accept(this);
        _sb.Indent--;

        _sb.Indent--;
    }

    public void Visit(CatchStmt catchStmt)
    {
        _sb.Add(catchStmt.ToString());
        _sb.Indent++;

        _sb.Add("Identifier: " + catchStmt.Identifier.Name);

        _sb.Add("Block:");
        _sb.Indent++;
        catchStmt.Block.Accept(this);
        _sb.Indent--;

        _sb.Indent--;
    }
}