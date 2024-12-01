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
    public void TestGenericType()
    {
        Execute(@"
        class Container<T> {
            instance: T
            Container(inst: T) {
                this.instance = inst
            }
        }");

        ZenValue Container = Interpreter.environment.GetValue("Container");
        
        // Container is a class type
        Assert.Equal(ZenType.Class, Container.Type);

        // underlying is a ZenClass
        ZenClass Class = Container.Underlying!;
        Assert.IsType<ZenClass>(Class);

        // ZenClass is generic - I.E has a parameter
        Assert.NotEmpty(Class.Parameters);

        // name is "T"
        ZenClass.Parameter T = Class.Parameters[0];
        Assert.Equal("T", T.Name);

        // standalone "T" is inferred to be a ZenType.Type - I.E a Type, not a integer constraint or other type.
        Assert.Equal(ZenType.Type, T.Type);

        // for *Type* parameters the default value is "any"
        // for non-Type parameters (int etc) the default value is Null
        Assert.Equal(ZenType.Type, T.DefaultValue.Type);
        Assert.Equal(ZenType.Any, T.DefaultValue.Underlying);
    }

    [Fact]
    public void TestGenericInstantiation()
    {
        var result = Execute(@"
        class Container<T> {
            value: T
            Container(v: T) {
                this.value = v
            }
            get(): T {
                return this.value
            }
        }
        
        var strBox = new Container<string>(""hello"")
        print strBox.get()
        ");
        
        Assert.Equal("hello", result);
    }

    [Fact]
    public void TestMultipleTypeParameters()
    {
        var result = Execute(@"
        class Pair<T, U> {
            first: T
            second: U
            
            Pair(a: T, b: U) {
                this.first = a
                this.second = b
            }
            
            toString(): string {
                return ""("" + this.first + "", "" + this.second + "")""
            }
        }
        
        var pair = new Pair<int, string>(42, ""hello"")
        print pair.toString()
        ");
        
        Assert.Equal("(42, hello)", result);
    }

    [Fact]
    public void TestValueParameter()
    {
        var result = Execute(@"
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
        ");
        
        Assert.Equal("20", result);
    }

    [Fact]
    public void TestDefaultTypeParameter()
    {
        var result = Execute(@"
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
        ");
        
        Assert.Equal("42", result);
    }

    [Fact]
    public void TestGenericMethodInGenericClass()
    {
        var result = Execute(@"
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
        ");
        
        Assert.Equal("42", result);
    }

    [Fact]
    public void TestNestedGenerics()
    {
        var result = Execute(@"
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
        ");
        
        Assert.Equal("hello", result);
    }
}
