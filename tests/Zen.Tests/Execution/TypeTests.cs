using Xunit;
using Xunit.Abstractions;
using Zen.Execution;
using Zen.Typing;

namespace Zen.Tests.Execution;

public class TypeTests : TestRunner
{
    public TypeTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async void TestValueIsType()
    {
        await RestartInterpreter();

        string? result;

        await Execute("var a = 5");
        await Execute("var b = null");

        result = await Execute("print a is int", true);
        Assert.Equal("true", result);

        // result = Execute("print a is int?");
        // Assert.Equal("true", result);

        // result = Execute("print b is int?");
        // Assert.Equal("true", result);

        result = await Execute("print b is int", true);
        Assert.Equal("false", result);
    }

    [Fact]
    public async void TestTypeCheckClass() {
        await RestartInterpreter();

        await Execute("class Point { x: int y: int }");
        await Execute("var p = new Point()");
        string? result = await Execute("print p is Point", true);
        Assert.Equal("true", result);
    }

    [Fact]
    public async void TestTypeCast() {
        await RestartInterpreter();

        await Execute("var pi = 3.14");
        await Execute("var intPi = (int) pi");
        string? result = await Execute("print intPi", true);
        Assert.Equal("3", result);
    }

    [Fact]
    public async void TestUnionTypeDeclaration() {
        await RestartInterpreter();

        await Execute("type number = int or float");
        await Execute("var x: number = 42");
        string? result = await Execute("print x", true);
        Assert.Equal("42", result);

        await Execute("x = 3.14");
        result = await Execute("print x", true);
        Assert.Equal("3.14", result);
    }

    [Fact]
    public async void TestUnionTypeIsCheck() {
        await RestartInterpreter();

        await Execute("type number = int or float");
        await Execute("var x: number = 42");
        
        ZenType number = Interpreter.Environment.GetValue("number")!.Underlying;

        string? result = await Execute("print x is int", true);

        Variable x = Interpreter.Environment.GetVariable("x");

        Assert.Equal(number, x.Type);

        Assert.Equal("true", result);

        await Execute("x = 3.14");
        result = await Execute("print x is float", true);
        Assert.Equal("true", result);

        result = await Execute("print x is number", true);
        Assert.Equal("true", result);
    }

    [Fact]
    public async void TestUnionTypeInvalidAssignment() {
        await RestartInterpreter();

        await Execute("type number = int or float");
        await Execute("var x: number = 42");

        // Should throw a type error when assigning string to number type
        await Assert.ThrowsAsync<RuntimeError>(async () => 
            await Execute("x = \"hello\"")
        );
    }

    [Fact]
    public async void TestComplexUnionType() {
        await RestartInterpreter();

        await Execute("class Point { x: int y: int }");
        await Execute("type shape = Point or string");
        await Execute("var s: shape = new Point()");
        await Execute("s = \"circle\"");

        // Both assignments should work since s can be either Point or string
        string? result = await Execute("print s is string", true);
        Assert.Equal("true", result);
    }

    [Fact]
    public async void TestMaxFunction() {
        await RestartInterpreter();

        await Execute(@"
            type number = int or float
            func max(a:number, b:number): number {
                if a > b {
                    return a
                }

                return b
            }
        ");

        string? result = await Execute("print max(5, 10)", true);
        Assert.Equal("10", result);

        result = await Execute("print max(3.5, 2.1)", true);
        Assert.Equal("3.5", result);

        result = await Execute("print max(7, 7)", true);
        Assert.Equal("7", result);
    }
}
