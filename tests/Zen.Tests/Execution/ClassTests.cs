using Xunit.Abstractions;
using Zen.Typing;

namespace Zen.Tests.Execution;

public class ClassTests : TestRunner
{
    public ClassTests(ITestOutputHelper output) : base(output) {}

    [Fact]
    public void TestClassDeclaration() {
        RestartInterpreter();
        Execute("class Test {}");

        Assert.True(Interpreter.environment.Exists("Test"));
        
        // get the value
        ZenValue Test = Interpreter.environment.GetValue("Test");

        // make sure its a class
        Assert.Equal(ZenType.Class, Test.Type);
        Assert.IsType<ZenClass>(Test.Underlying);
    }

    
    [Fact]
    public void TestClassInstantiation() {
        RestartInterpreter();

        Execute("class Test {}");

        Execute("var t = new Test()");

        Assert.True(Interpreter.environment.Exists("t"));

        // get the class
        ZenValue Test = Interpreter.environment.GetValue("Test");
        
        Assert.True(Test.Type == ZenType.Class, "Type should be a ZenType.Class");

        ZenClass Clazz = Test.Underlying!;

        Assert.Equal("Test", Clazz.Name);

        // get the value
        ZenValue test = Interpreter.environment.GetValue("t");

        // type should equal the Test type
        //Assert.Equal(Type, test.Type);
        Assert.IsType<ZenObject>(test.Underlying);

        string? result = Execute("print t.ToString()");
        Assert.Equal("Object(Test)", result);
    }

    
    [Fact]
    public void TestClassInstantiationWithLocal() {
        RestartInterpreter();

        string? result = Execute(@"
            class Test {
                Test() {
                    var text = ""hello""
                    print text
                }
            }

            var t = new Test()
        ");
        Assert.Equal("hello", result);
    }

    
    [Fact]
    public void TestClassProperty() {
        RestartInterpreter();

        Execute("class Test { name: string = \"john\"}");

        Execute("var t = new Test()");

        Assert.True(Interpreter.environment.Exists("t"));

        // get the value
        ZenValue test = Interpreter.environment.GetValue("t");

        // get the object
        ZenObject testObject = (ZenObject)test.Underlying!;

        // make sure it has the expected property
        Assert.True(testObject.Properties.ContainsKey("name"));
        
        // get the value
        ZenValue nameValue = testObject.Properties["name"];

        // make sure its a ZenType.String
        Assert.Equal(ZenType.String, nameValue.Type);
        Assert.Equal<string>("john", nameValue.Underlying);

        string? result = Execute("print t.name");

        Assert.Equal("john", result);

        // set the property
        Execute("t.name = \"bob\"");

        // get the value
        nameValue = testObject.Properties["name"];

        // make sure its a ZenType.String
        Assert.Equal(ZenType.String, nameValue.Type);
        Assert.Equal("bob", nameValue.Underlying);
    }

    [Fact]
    public void TestConstructor()
    {
        RestartInterpreter();

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

        Execute(code);

        ZenClass Point = (ZenClass) Interpreter.environment.GetValue("Point")!.Underlying!;

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
        Execute("var p = new Point(5, 10)");

        // make sure it has the expected properties
        Assert.True(Interpreter.environment.Exists("p"));
        ZenValue pValue = Interpreter.environment.GetValue("p");
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
        Execute("p.x = 7");

        // get the value
        xValue = pObject.Properties["x"];

        // make sure its a ZenType.Int
        Assert.Equal(ZenType.Integer, xValue.Type);
        Assert.Equal(7, xValue.Underlying);
    }

    [Fact]
    public void TestInterface()
    {
        RestartInterpreter();

        Execute(@"
            interface Printable {
                Print(): string
            }
        ");

        ZenValue PrintableValue = Interpreter.environment.GetValue("Printable")!;
        Assert.Equal(PrintableValue.Type, ZenType.Interface);

        Assert.IsType<ZenInterface>(PrintableValue.Underlying);
        ZenInterface printableInterface = (ZenInterface)Interpreter.environment.GetValue("Printable")!.Underlying!;
        
        Assert.Equal("Printable", printableInterface.Name);
        Assert.Single(printableInterface.Methods);
        Assert.Equal("Print", printableInterface.Methods.First().Name);
    }

    [Fact]
    public void TestClassImplementsInterface()
    {
        RestartInterpreter();

        Execute(@"
            interface Printable {
                Print(): string
            }

            class Test implements Printable {
                Print(): string {
                    return ""hello world""
                }
            }
        ");

        ZenInterface printableInterface = (ZenInterface)Interpreter.environment.GetValue("Printable")!.Underlying!;
        ZenClass testClass = (ZenClass)Interpreter.environment.GetValue("Test")!.Underlying!;
        
        Assert.Equal("Test", testClass.Name);
        Assert.Equal("Print", printableInterface.Methods.First().Name);
        Assert.Equal("Print", testClass.Methods.First().Name);

        string? result = Execute((@"
            var t = new Test()
            print t.Print()
        "));

        Assert.Equal("hello world", result);
    }

    [Fact]
    public void TestParametricInterface()
    {
        RestartInterpreter();
        Execute(@"
        interface Printable<T> {
            Print(thing: T): string
        }");

        ZenInterface printableInterface = (ZenInterface)Interpreter.environment.GetValue("Printable")!.Underlying!;
        Assert.Equal("Printable", printableInterface.Name);
        Assert.Single(printableInterface.Methods);
        Assert.Equal("Print", printableInterface.Methods.First().Name);

        IZenClass.Parameter T = printableInterface.Parameters[0];
        Assert.Equal("T", T.Name);
        Assert.Equal(ZenType.Type, T.Type);
        Assert.True(T.IsTypeParameter);
    }

    [Fact]
    public void TestClassImplementsParametricInterface()
    {
        RestartInterpreter();
        string? result = Execute(@"
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
        ");

        ZenInterface printableInterface = (ZenInterface)Interpreter.environment.GetValue("Printable")!.Underlying!;
        ZenClass testClass = (ZenClass)Interpreter.environment.GetValue("Test")!.Underlying!;
        
        Assert.Equal("Test", testClass.Name);
        Assert.Equal("Print", printableInterface.Methods.First().Name);
        Assert.Equal("Print", testClass.Methods.First().Name);

        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void TestClassImplementsPolyParametricInterface()
    {
        RestartInterpreter();
        string? result = Execute(@"
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
        ");

        Assert.Equal("42", result);
    }
}