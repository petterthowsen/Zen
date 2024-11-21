using Xunit.Abstractions;
using Zen.Execution;
using Zen.Typing;

namespace Zen.Tests.Execution;

public class VariablesTest : TestRunner
{
    public VariablesTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void TestUndefinedVariable() {
        RestartInterpreter();

        RuntimeError error = Assert.Throws<RuntimeError>(() => Execute("number = 1"));
        Assert.Equal(Common.ErrorType.UndefinedVariable, error.Type);
    }

    [Fact]
    public void TestVariableDeclaration() {
        RestartInterpreter();

        Assert.False(Interpreter.environment.Exists("name"));
        Execute("var name = \"john\"");
        Assert.True(Interpreter.environment.Exists("name"));
        
        string? result = Execute("print name");
        Assert.Equal("john", result);

        Variable variable = Interpreter.environment.GetVariable("name");
        Assert.False(variable.Constant);
        Assert.False(variable.Nullable);
        
        ZenType type = variable.Type;
        Assert.Equal(ZenType.String, type);
    }

    
    [Fact]
    public void TestVariableDeclarationAndAssignment() {
        RestartInterpreter();

        Assert.False(Interpreter.environment.Exists("name"));
        Execute("var name = \"john\"");
        Assert.True(Interpreter.environment.Exists("name"));
        Variable variable = Interpreter.environment.GetVariable("name");
        ZenValue value = (ZenValue) variable.Value!;
        Assert.Equal("john", value.Underlying);

        Execute("name = \"doe\"");
        value = (ZenValue) variable.Value!;
        Assert.Equal("doe", value.Underlying);
    }

    [Fact]
    public void TestVariablePlusAsignment() {
        RestartInterpreter();

        Execute("var i = 1");
        Variable variable = Interpreter.environment.GetVariable("i");
        ZenValue value = (ZenValue) variable.Value!;
        Assert.Equal(1, value.Underlying);

        Execute("i += 1");
        value = (ZenValue) variable.Value!;
        Assert.Equal(2, value.Underlying);
    }

    [Fact]
    public void TestVariableMinusAsignment() {
        RestartInterpreter();

        Execute("var i = 1");
        Variable variable = Interpreter.environment.GetVariable("i");
        ZenValue value = (ZenValue) variable.Value!;
        Assert.Equal(1, value.Underlying);

        Execute("i -= 1");
        value = (ZenValue) variable.Value!;
        Assert.Equal(0, value.Underlying);
    }

    [Fact]
    public void TestVariableMultiplyAsignment() {
        RestartInterpreter();

        Execute("var i = 5");
        Variable variable = Interpreter.environment.GetVariable("i");
        ZenValue value = (ZenValue) variable.Value!;
        Assert.Equal(5, value.Underlying);

        Execute("i *= 2");
        value = (ZenValue) variable.Value!;
        Assert.Equal(10, value.Underlying);
    }

    [Fact]
    public void TestVariableDivideAsignment() {
        RestartInterpreter();

        Execute("var i = 10");
        Variable variable = Interpreter.environment.GetVariable("i");
        ZenValue value = (ZenValue) variable.Value!;
        Assert.Equal(10, value.Underlying);

        Execute("i /= 2");
        value = (ZenValue) variable.Value!;
        Assert.Equal(5, value.Underlying);
    }
}