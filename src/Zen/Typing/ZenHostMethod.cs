using Zen.Execution;

namespace Zen.Typing;

// todo: add a ZenUserMethod and ZenHostMethod
public class ZenHostMethod : ZenMethod
{

    public readonly Func<ZenValue[], ZenValue> Func;

    public ZenHostMethod(
        string name,
        ZenClass.Visibility visibility,
        ZenType returnType,
        List<Parameter> parameters,
        Func<ZenValue[], ZenValue> func
    ) : base(name, visibility, returnType, parameters)
    {
        Func = func;
    }

    public ZenValue Call(Interpreter interpreter, ZenObject instance, ZenValue[] arguments)
    {
        if (arguments.Length < Arity) {
            throw new Exception($"Function called with {arguments.Length} arguments, but expected {Arity}");
        }

        List<ZenValue> args = new List<ZenValue>(arguments);
        args.Insert(0, new ZenValue(ZenType.Object, instance));

        return Func([..args]);
    }
    public override ZenValue Call(Interpreter interpreter, ZenValue[] arguments)
    {
        throw new Exception("Methods must be called with a ZenObject instance");
    }
}