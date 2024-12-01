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

        // get the value
        ZenValue test = Interpreter.environment.GetValue("t");

        // type should equal the Test type
        Assert.Equal(Test.Underlying!.Type, test.Type);
        Assert.IsType<ZenObject>(test.Underlying);

        string? result = Execute("print t.ToString()");
        Assert.Equal("Object(Test)", result);
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
        Point.HasOwnConstructor([ZenType.Integer, ZenType.Integer], out ZenMethod? constructor);
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
}