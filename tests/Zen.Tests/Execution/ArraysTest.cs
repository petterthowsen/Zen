using Xunit.Abstractions;
using Zen.Execution;
using Zen.Typing;

namespace Zen.Tests.Execution;

public class ArrayTests : TestRunner
{
    public ArrayTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void TestArrayCreation()
    {
        Execute("var arr = new Array<any>()");
        var result = Execute("print arr.length");
        Assert.Equal("0", result);
    }

    [Fact]
    public void TestArrayPushAndLength()
    {
        RestartInterpreter();
        Execute(@"
            var arr = new Array<any>()
            arr.push(1)
            arr.push(2)
        ");
        var result = Execute("print arr.length");
        Assert.Equal("2", result);
    }

    [Fact]
    public void TestArrayBracketAccess()
    {
        RestartInterpreter();
        Execute(@"
            var arr = new Array<int>()
            arr.push(1)
            arr.push(2)
            arr.push(3)
        ");
        var result = Execute("print arr[1]");
        Assert.Equal("2", result);
    }

    [Fact]
    public void TestArrayBracketAssignment()
    {
        RestartInterpreter();
        Execute(@"
            var arr = new Array<int>()
            arr.push(1)
            arr.push(2)
            arr[1] = 42
        ");
        var result = Execute("print arr[1]");
        Assert.Equal("42", result);
    }

    [Fact]
    public void TestArrayPop()
    {
        RestartInterpreter();
        Execute(@"
            var arr = new Array<int>()
            arr.push(1)
            arr.push(2)
        ");
        var result = Execute("print arr.pop()");
        Assert.Equal("2", result);
        result = Execute("print arr.length");
        Assert.Equal("1", result);
    }

    [Fact]
    public void TestArrayToString()
    {
        RestartInterpreter();
        Execute(@"
            var arr = new Array<int>()
            arr.push(1)
            arr.push(2)
            arr.push(3)
        ");
        var result = Execute("print arr.ToString()");
        Assert.Equal("[1, 2, 3]", result);
    }

    [Fact]
    public void TestArrayBoundsChecking()
    {
        throw new NotImplementedException("This method freezes the runtime...");

        // RestartInterpreter();
        // Execute("var arr = new Array<any>()");
        // var result = Assert.Throws<RuntimeError>(() => Execute("arr[0]"));
        // Assert.Contains("Array index out of bounds", result.Message);
    }

    [Fact]
    public void TestArrayWithTypeParameter()
    {
        RestartInterpreter();
        Execute(@"
            var arr = new Array<int>()
            arr.push(1)
            arr.push(2)
        ");
        var result = Execute("print arr[0]");
        Assert.Equal("1", result);
    }

    [Fact]
    public void TestArrayTypeChecking()
    {
        throw new NotImplementedException("This method freezes the runtime...");

        // RestartInterpreter();
        // Execute("var arr = new Array<int>()");
        // var result = Assert.Throws<RuntimeError>(() => Execute("arr.push('hello')"));
        // Assert.Contains("Cannot pass argument of type 'string' to parameter of type 'int'", result.Message);
    }

    [Fact]
    public void TestArrayTypeInference()
    {
        RestartInterpreter();
        Execute(@"
            var arr = new Array<int>()
            arr.push(1)
            arr.push(2)
            var x: int = arr[0]
        ");
    }

    [Fact]
    public void TestArrayMethodTypeChecking()
    {
        RestartInterpreter();
        Execute(@"
            var arr = new Array<string>()
            arr.push('hello')
        ");
        var result = Assert.Throws<RuntimeError>(() => Execute("arr[0] = 42"));
        Assert.Contains("Cannot assign value of type 'int' to array element of type 'string'", result.Message);
    }

    [Fact]
    public void TestArrayWithValueConstraint()
    {
        RestartInterpreter();
        var result = Execute(@"
            class FixedArray<S: int = 10> {
                FixedArray() {
                    print S
                }
            }
            var arr = new FixedArray()
        ");
        Assert.Equal("10", result);
    }

    [Fact]
    public void TestArrayWithCustomValueConstraint()
    {
        RestartInterpreter();
        var result = Execute(@"
            class FixedArray<S: int = 10> {
                FixedArray() {
                    print S
                }
            }
            var arr = new FixedArray<20>()
        ");
        Assert.Equal("20", result);
    }
}
