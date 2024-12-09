using Xunit.Abstractions;
using Zen.Typing;

namespace Zen.Tests.Execution;

public class ClassTests : TestRunner
{
    public ClassTests(ITestOutputHelper output) : base(output) {}

    [Fact]
    public async void TestClassDeclaration() {
        await RestartInterpreter();
        await Execute("class Test {}");

        Assert.True(Interpreter.Environment.Exists("Test"));
        
        // get the value
        ZenValue Test = Interpreter.Environment.GetValue("Test");

        // make sure its a class
        Assert.Equal(ZenType.Class, Test.Type);
        Assert.IsType<ZenClass>(Test.Underlying);
    }

    
    [Fact]
    public async void TestClassInstantiation() {
        await RestartInterpreter();

        await Execute("class Test {}");

        await Execute("var t = new Test()");

        Assert.True(Interpreter.Environment.Exists("t"));

        // get the class
        ZenValue Test = Interpreter.Environment.GetValue("Test");
        
        Assert.True(Test.Type == ZenType.Class, "Type should be a ZenType.Class");

        ZenClass Clazz = Test.Underlying!;

        Assert.Equal("Test", Clazz.Name);

        // get the value
        ZenValue test = Interpreter.Environment.GetValue("t");

        // type should equal the Test type
        //Assert.Equal(Type, test.Type);
        Assert.IsType<ZenObject>(test.Underlying);

        string? result = await Execute("print t.ToString()", true);
        Assert.Equal("Object(Test)", result);
    }

    
    [Fact]
    public async void TestClassInstantiationWithLocal() {
        await RestartInterpreter();

        string? result = await Execute(@"
            class Test {
                Test() {
                    var text = ""hello""
                    print text
                }
            }

            var t = new Test()
        ", true);
        Assert.Equal("hello", result);
    }

    
    [Fact]
    public async void TestClassProperty() {
        await RestartInterpreter();

        await Execute("class Test { name: string = \"john\"}");

        await Execute("var t = new Test()");

        Assert.True(Interpreter.Environment.Exists("t"));

        // get the value
        ZenValue test = Interpreter.Environment.GetValue("t");

        // get the object
        ZenObject testObject = (ZenObject)test.Underlying!;

        // make sure it has the expected property
        Assert.True(testObject.Properties.ContainsKey("name"));
        
        // get the value
        ZenValue nameValue = testObject.Properties["name"];

        // make sure its a ZenType.String
        Assert.Equal(ZenType.String, nameValue.Type);
        Assert.Equal<string>("john", nameValue.Underlying);

        string? result = await Execute("print t.name", true);

        Assert.Equal("john", result);

        // set the property
        await Execute("t.name = \"bob\"");

        // get the value
        nameValue = testObject.Properties["name"];

        // make sure its a ZenType.String
        Assert.Equal(ZenType.String, nameValue.Type);
        Assert.Equal("bob", nameValue.Underlying);
    }

    [Fact]
    public async void TestConstructor()
    {
        await RestartInterpreter();

        string code = @"
            class Point {
                x: int
                y: int

                Point(x: int, y: int) {
                    this.x = x
                    this.y = y
                }
            }
            ";

        await Execute(code);

        ZenClass Point = (ZenClass) Interpreter.Environment.GetValue("Point")!.Underlying!;

        // test properties
        Assert.Equal(2, Point.Properties.Count);

        Assert.True(Point.Properties.ContainsKey("x"));
        Assert.True(Point.Properties.ContainsKey("y"));

        Assert.Equal(ZenType.Integer, Point.Properties["x"].Type);
        Assert.Equal(ZenType.Integer, Point.Properties["y"].Type);

        // test constructor
        var constructor = Point.GetOwnConstructor([new(ZenType.Integer, 0), new(ZenType.Integer, 0)]);
        Assert.NotNull(constructor);

        Assert.Equal(2, constructor!.Arity);
        Assert.Equal(ZenType.Void, constructor.ReturnType);

        // define a new point
        await Execute("var p = new Point(5, 10)");

        // make sure it has the expected properties
        Assert.True(Interpreter.Environment.Exists("p"));
        ZenValue pValue = Interpreter.Environment.GetValue("p");
        ZenObject pObject = (ZenObject)pValue.Underlying!;
        
        // make sure it has the expected properties
        Assert.True(pObject.Properties.ContainsKey("x"));
        Assert.True(pObject.Properties.ContainsKey("y"));
        ZenValue xValue = pObject.Properties["x"];
        ZenValue yValue = pObject.Properties["y"];
        Assert.Equal(ZenType.Integer, xValue.Type);
        Assert.Equal(5, xValue.Underlying);
        Assert.Equal(ZenType.Integer, yValue.Type);
        Assert.Equal(10, yValue.Underlying);

        // update the x property
        await Execute("p.x = 7");

        // get the value
        xValue = pObject.Properties["x"];

        // make sure its a ZenType.Int
        Assert.Equal(ZenType.Integer, xValue.Type);
        Assert.Equal(7, xValue.Underlying);
    }

    [Fact]
    public async void TestInterface()
    {
        await RestartInterpreter();

        await Execute(@"
            interface Printable {
                Print(): string
            }
        ");

        ZenValue PrintableValue = Interpreter.Environment.GetValue("Printable")!;
        Assert.Equal(PrintableValue.Type, ZenType.Interface);

        Assert.IsType<ZenInterface>(PrintableValue.Underlying);
        ZenInterface printableInterface = (ZenInterface)Interpreter.Environment.GetValue("Printable")!.Underlying!;
        
        Assert.Equal("Printable", printableInterface.Name);
        Assert.Single(printableInterface.Methods);
        Assert.Equal("Print", printableInterface.Methods.First().Name);
    }

    [Fact]
    public async void TestClassImplementsInterface()
    {
        await RestartInterpreter();

        await Execute(@"
            interface Printable {
                Print(): string
            }

            class Test implements Printable {
                Print(): string {
                    return ""hello world""
                }
            }
        ");

        ZenInterface printableInterface = (ZenInterface)Interpreter.Environment.GetValue("Printable")!.Underlying!;
        ZenClass testClass = (ZenClass)Interpreter.Environment.GetValue("Test")!.Underlying!;
        
        Assert.Equal("Test", testClass.Name);
        Assert.Equal("Print", printableInterface.Methods.First().Name);
        Assert.Equal("Print", testClass.Methods.First().Name);

        string? result = await Execute((@"
            var t = new Test()
            print t.Print()
        "), true);

        Assert.Equal("hello world", result);
    }

    [Fact]
    public async void TestParametricInterface()
    {
        await RestartInterpreter();
        await Execute(@"
        interface Printable<T> {
            Print(thing: T): string
        }");

        ZenInterface printableInterface = (ZenInterface)Interpreter.Environment.GetValue("Printable")!.Underlying!;
        Assert.Equal("Printable", printableInterface.Name);
        Assert.Single(printableInterface.Methods);
        Assert.Equal("Print", printableInterface.Methods.First().Name);

        IZenClass.Parameter T = printableInterface.Parameters[0];
        Assert.Equal("T", T.Name);
        Assert.Equal(ZenType.Type, T.Type);
        Assert.True(T.IsTypeParameter);
    }

    [Fact]
    public async void TestClassImplementsParametricInterface()
    {
        await RestartInterpreter();
        string? result = await Execute(@"
        interface Printable<T> {
            Print(thing: T): string
        }

        class Test<T> implements Printable<T> {
            Print(thing: T): string {
                print thing
            }
        }
        
        var obj = new Test<string>()
        
        obj.Print(""Hello World"")
        ", true);

        ZenInterface printableInterface = (ZenInterface)Interpreter.Environment.GetValue("Printable")!.Underlying!;
        ZenClass testClass = (ZenClass)Interpreter.Environment.GetValue("Test")!.Underlying!;
        
        Assert.Equal("Test", testClass.Name);
        Assert.Equal("Print", printableInterface.Methods.First().Name);
        Assert.Equal("Print", testClass.Methods.First().Name);

        Assert.Equal("Hello World", result);
    }

    [Fact]
    public async void TestClassImplementsPolyParametricInterface()
    {
        await RestartInterpreter();
        string? result = await Execute(@"
        interface Container<K, V> {
            Add(key:K, value:V): void
            Get(key:K): V
        }

        class MyContainer<K, V> implements Container<K, V> {
            
            LastKey:K
            LastValue:V

            Add(key:K, value:V): void {
                this.LastKey = key
                this.LastValue = value
            }

            Get(key:K): V {
                return this.LastValue
            }
        }
        
        var obj = new MyContainer<string, int>()
        
        obj.Add(""my_key"", 42)
        print obj.Get(""my_key"")
        ", true);

        Assert.Equal("42", result);
    }
}