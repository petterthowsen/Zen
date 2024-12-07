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
        Execute("var arr = new Array<int>()");

        string? result = Execute("print arr.Length", true);
        Assert.Equal("0", result);
    }

    [Fact]
    public void TestArrayAppend()
    {
        RestartInterpreter();
        Execute(@"
            var arr = new Array<any>()
            arr.Append(1)
            arr.Append(2)
        ");
        var result = Execute("print arr.Length", true);
        Assert.Equal("2", result);
    }

    [Fact]
    public void TestArrayBracketGet()
    {
        RestartInterpreter();
        Execute(@"
            var arr = new Array<int>()
            arr.Append(1)
            arr.Append(2)
            arr.Append(3)
        ");
        var result = Execute("print arr[1]");
        Assert.Equal("2", result);
    }

    [Fact]
    public void TestArrayBracketSet()
    {
        RestartInterpreter();
        Execute(@"
            var arr = new Array<int>()
            arr.Append(1)
            arr.Append(2)
            arr[1] = 42
        ");
        var result = Execute("print arr[1]");
        Assert.Equal("42", result);
    }

    [Fact]
    public void TestArraySlice()
    {
        RestartInterpreter();
        string? result = Execute(@"
            var arr = new Array<string>()
            arr.Append(""one"")
            arr.Append(""two"")
            arr.Append(""three"")
            var slice = arr.Slice(1, 2)
            print slice[0]
        ");

        Assert.Equal("two", result);
    }

    [Fact]
    public void TestForInLoop()
    {
        RestartInterpreter();
        var result = Execute(@"
            var arr = new Array<string>()
            arr.Append(""one"")
            arr.Append(""two"")
            arr.Append(""three"")
            for key, value in arr {
                print ""key: "" +  key + ""\n""
                print ""value: "" + value + ""\n""
            }
        ");

        Assert.Equal("key: 0\nvalue: one\nkey: 1\nvalue: two\nkey: 2\nvalue: three\n", result);
    }

    [Fact]
    public void TestArrayRemoveAt()
    {
        RestartInterpreter();
        Execute(@"
            var arr = new Array<int>()
            arr.Append(1)
            arr.Append(2)
        ");
        var result = Execute("print arr.RemoveLast()");
        Assert.Equal("2", result);
        result = Execute("print arr.Length");
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
