using Xunit.Abstractions;
using Zen.Execution;
using Zen.Typing;

namespace Zen.Tests.Execution;

public class GenericsTest : TestRunner
{
    public GenericsTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async void TestGenericType()
    {
        await RestartInterpreter();

        await Execute(@"
        class Container<T> {
            instance: T
            Container(inst: T) {
                this.instance = inst
            }
        }");

        ZenValue Container = Interpreter.Environment.GetValue("Container");
        
        // Container is a class type
        Assert.Equal(ZenType.Class, Container.Type);

        // underlying is a ZenClass
        ZenClass Class = Container.Underlying!;
        Assert.IsType<ZenClass>(Class);

        // ZenClass has parameters
        Assert.NotEmpty(Class.Parameters);

        IZenClass.Parameter T = Class.Parameters[0];

        // name is "T"
        Assert.Equal("T", T.Name);

        // T is a Type parameter (not a constraint like int)
        Assert.Equal(ZenType.Type, T.Type);

        // for *Type* parameters the default value is "any"
        // for non-Type parameters (int etc) the default value is Null
        Assert.Equal(ZenType.Type, T.DefaultValue.Type);
        Assert.Equal(ZenType.Any, T.DefaultValue.Underlying);
    }

    [Fact]
    public async void TestGenericInstantiation()
    {
        await RestartInterpreter();
        var result = await Execute(@"
        class Container<T> {
            instance: T
            
            Container(inst: T) {
                this.instance = inst
            }
            
            Get(): T {
                return this.instance
            }
        }
        
        var strBox = new Container<string>(""hello"")
        print strBox.Get()
        ", true);
        
        Assert.Equal("hello", result);
    }

    [Fact]
    public async void TestMultipleTypeParameters()
    {
        await RestartInterpreter();
        var result = await Execute(@"
        class Pair<T, U> {
            first: T
            second: U
            
            Pair(a: T, b: U) {
                this.first = a
                this.second = b
            }
            
            ToString(): string {
                return ""("" + this.first + "", "" + this.second + "")""
            }
        }
        
        var pair = new Pair<int, string>(42, ""hello"")
        print pair.ToString()
        ", true);
        
        Assert.Equal("(42, hello)", result);
    }

    [Fact]
    public async void TestValueParameter()
    {
        await RestartInterpreter();
        var result = await Execute(@"
        class FixedArray<SIZE: int> {
            values: Array<int>
            
            FixedArray() {
                this.values = new Array<int>(SIZE)
            }
            
            set(index: int, value: int) {
                if (index >= SIZE) {
                    print ""Index out of bounds""
                    return
                }
                this.values[index] = value
            }
            
            get(index: int): int {
                if (index >= SIZE) {
                    print ""Index out of bounds""
                    return -1
                }
                return this.values[index]
            }
        }
        
        var arr = new FixedArray<3>()
        arr.set(0, 10)
        arr.set(1, 20)
        arr.set(2, 30)
        arr.set(3, 40)  // Should print ""Index out of bounds""
        print arr.get(1)
        ", true);
        
        Assert.Equal("20", result);
    }

    [Fact]
    public async void TestDefaultTypeParameter()
    {
        await RestartInterpreter();
        var result = await Execute(@"
        class Container<T = int> {
            value: T
            Container(v: T) {
                this.value = v
            }
            get(): T {
                return this.value
            }
        }
        
        var box = new Container(42)  // T defaults to int
        print box.get()
        ", true);
        
        Assert.Equal("42", result);
    }

    [Fact]
    public async void TestGenericMethodInGenericClass()
    {
        await RestartInterpreter();
        var result = await Execute(@"
        class Container<T> {
            value: T
            Container(v: T) {
                this.value = v
            }
            
            map<U>(transform: (T) -> U): Container<U> {
                return new Container<U>(transform(this.value))
            }
        }
        
        var box = new Container<int>(42)
        var strBox = box.map((x) => x.toString())
        print strBox.value
        ", true);
        
        Assert.Equal("42", result);
    }

    [Fact]
    public async void TestNestedGenerics()
    {
        await RestartInterpreter();
        var result = await Execute(@"
        class Container<T> {
            value: T
            Container(v: T) {
                this.value = v
            }
        }
        
        class Wrapper<T> {
            inner: Container<T>
            Wrapper(v: T) {
                this.inner = new Container<T>(v)
            }
            get(): T {
                return this.inner.value
            }
        }
        
        var wrapper = new Wrapper<string>(""hello"")
        print wrapper.get()
        ", true);
        
        Assert.Equal("hello", result);
    }
}
