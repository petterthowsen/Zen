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
    public async void TestUndefinedVariable() {
        await RestartInterpreter();

        RuntimeError error = await Assert.ThrowsAsync<RuntimeError>(async () => await Execute("number = 1"));
        Assert.Equal(Common.ErrorType.UndefinedVariable, error.Type);
    }

    [Fact]
    public async void TestVariableDeclaration() {
        await RestartInterpreter();

        Assert.False(Interpreter.Environment.Exists("name"));
        await Execute("var name = \"john\"");
        Assert.True(Interpreter.Environment.Exists("name"));
        
        string? result = await Execute("print name", true);
        Assert.Equal("john", result);

        Variable variable = Interpreter.Environment.GetVariable("name");
        Assert.False(variable.Constant);
        Assert.False(variable.Nullable);
        
        ZenType type = variable.Type;
        Assert.Equal(ZenType.String, type);
    }

    
    [Fact]
    public async void TestVariableDeclarationAndAssignment() {
        await RestartInterpreter();

        Assert.False(Interpreter.Environment.Exists("name"));
        await Execute("var name = \"john\"");
        Assert.True(Interpreter.Environment.Exists("name"));
        Variable variable = Interpreter.Environment.GetVariable("name");
        ZenValue value = (ZenValue) variable.Value!;
        Assert.Equal("john", value.Underlying);

        await Execute("name = \"doe\"");
        value = (ZenValue) variable.Value!;
        Assert.Equal("doe", value.Underlying);
    }

    [Fact]
    public async void TestVariablePlusAsignment() {
        await RestartInterpreter();

        await Execute("var i = 1");
        Variable variable = Interpreter.Environment.GetVariable("i");
        ZenValue value = (ZenValue) variable.Value!;
        Assert.Equal(1, value.Underlying);

        await Execute("i += 1");
        value = (ZenValue) variable.Value!;
        Assert.Equal(2, value.Underlying);
    }

    [Fact]
    public async void TestVariableMinusAsignment() {
        await RestartInterpreter();

        await Execute("var i = 1");
        Variable variable = Interpreter.Environment.GetVariable("i");
        ZenValue value = (ZenValue) variable.Value!;
        Assert.Equal(1, value.Underlying);

        await Execute("i -= 1");
        value = (ZenValue) variable.Value!;
        Assert.Equal(0, value.Underlying);
    }

    [Fact]
    public async void TestVariableMultiplyAsignment() {
        await RestartInterpreter();

        await Execute("var i = 5");
        Variable variable = Interpreter.Environment.GetVariable("i");
        ZenValue value = (ZenValue) variable.Value!;
        Assert.Equal(5, value.Underlying);

        await Execute("i *= 2");
        value = (ZenValue) variable.Value!;
        Assert.Equal(10, value.Underlying);
    }

    [Fact]
    public async void TestVariableDivideAsignment() {
        await RestartInterpreter();

        await Execute("var i = 10");
        Variable variable = Interpreter.Environment.GetVariable("i");
        ZenValue value = (ZenValue) variable.Value!;
        Assert.Equal(10, value.Underlying);

        await Execute("i /= 2");
        value = (ZenValue) variable.Value!;
        Assert.Equal(5, value.Underlying);
    }
}