using Zen.Execution;

namespace Zen.Typing;

public abstract class ZenMethod : ZenFunction
{
    public string Name;
    public ZenClass.Visibility Visibility;

    public ZenMethod(string name, ZenClass.Visibility visibility, ZenType returnType, List<ZenFunction.Parameter> parameters) : base(returnType, parameters) {
        Name = name;
        Visibility = visibility;
    }

    public override ZenValue Call(Interpreter interpreter, ZenValue[] arguments)
    {
        throw new Exception("Methods must be called with a ZenObject instance");
    }
}