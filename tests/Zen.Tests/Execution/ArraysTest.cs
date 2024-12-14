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
    public async void TestArrayCreation()
    {
        await RestartInterpreter();
        await Execute("var arr = new Array<int>()");

        string? result = await Execute("print arr.Length", true);
        Assert.Equal("0", result);
    }

    [Fact]
    public async void TestArrayAppend()
    {
        await RestartInterpreter();
        await Execute(@"
            var arr = new Array<any>()
            arr.Append(1)
            arr.Append(2)
        ");
        var result = await Execute("print arr.Length", true);
        Assert.Equal("2", result);
    }

    [Fact]
    public async void TestArrayBracketGet()
    {
        await RestartInterpreter();
        await Execute(@"
            var arr = new Array<int>()
            arr.Append(1)
            arr.Append(2)
            arr.Append(3)
        ");
        var result = await Execute("print arr[1]", true);
        Assert.Equal("2", result);
    }

    [Fact]
    public async void TestArrayBracketSet()
    {
        await RestartInterpreter();
        await Execute(@"
            var arr = new Array<int>()
            arr.Append(1)
            arr.Append(2)
            arr[1] = 42
        ");
        var result = await Execute("print arr[1]", true);
        Assert.Equal("42", result);
    }

    [Fact]
    public async void TestArraySlice()
    {
        await RestartInterpreter();
        string? result = await Execute(@"
            var arr = new Array<string>()
            arr.Append(""one"")
            arr.Append(""two"")
            arr.Append(""three"")
            var slice = arr.Slice(1, 2)
            print slice[0]
        ", true);

        Assert.Equal("two", result);
    }

    [Fact]
    public async void TestForInLoop()
    {
        await RestartInterpreter();
        var result = await Execute(@"
            var arr = new Array<string>()
            arr.Append(""one"")
            arr.Append(""two"")
            arr.Append(""three"")
            for key, value in arr {
                print ""key: "" +  key + ""\n""
                print ""value: "" + value + ""\n""
            }
        ", true);

        Assert.Equal("key: 0\nvalue: one\nkey: 1\nvalue: two\nkey: 2\nvalue: three\n", result);
    }

    [Fact]
    public async void TestArrayRemoveAt()
    {
        await RestartInterpreter();
        await Execute(@"
            var arr = new Array<int>()
            arr.Append(1)
            arr.Append(2)
        ");
        var result = await Execute("print arr.RemoveLast()", true);
        Assert.Equal("2", result);
        result = await Execute("print arr.Length", true);
        Assert.Equal("1", result);
    }

    [Fact]
    public async void TestArrayToString()
    {
        await RestartInterpreter();
        await Execute(@"
            var arr = new Array<int>()
            arr.Append(1)
            arr.Append(2)
            arr.Append(3)
        ");
        var result = await Execute("print arr.ToString()", true);
        Assert.Equal("[1, 2, 3]", result);
    }

    [Fact]
    public async void TestArrayBoundsChecking()
    {
        await RestartInterpreter();
        await Execute("var arr = new Array<any>()");
        var result = await Assert.ThrowsAsync<RuntimeError>(async () => await Execute("arr[0]"));
        Assert.Contains("Array index out of bounds", result.Message);
    }

    [Fact]
    public async void TestArrayWithTypeParameter()
    {
        await RestartInterpreter();
        await Execute(@"
            var arr = new Array<int>()
            arr.Append(1)
            arr.Append(2)
        ");
        var result = await Execute("print arr[0]", true);
        Assert.Equal("1", result);
    }

    [Fact]
    public async void TestArrayTypeChecking()
    {
        await RestartInterpreter();
        await Execute("var arr = new Array<int>()");
        var result = await Assert.ThrowsAsync<RuntimeError>(async () =>  await Execute("arr.Append(\"hello\")"));
    }


    [Fact]
    public async void TestArrayLiteral()
    {
        await RestartInterpreter();
        string? result = await Execute(@"
            var arr = [1, 2, 3]
            print arr[2]
        ", true);

        Assert.Equal("3", result);
    }

    [Fact]
    public async void TestArrayWithValueConstraint()
    {
        await RestartInterpreter();
        var result = await Execute(@"
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
    public async void TestArrayWithCustomValueConstraint()
    {
        await RestartInterpreter();
        var result = await Execute(@"
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
