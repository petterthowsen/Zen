using Zen.Execution;
using Environment = Zen.Execution.Environment;

namespace Zen.Typing;

// todo: add a ZenUserMethod and ZenHostMethod
public class ZenHostMethod : ZenMethod
{

    public readonly Func<ZenObject, ZenValue[], ZenValue> Func;

    public ZenHostMethod(
        bool async,
        string name,
        ZenClass.Visibility visibility,
        ZenType returnType,
        List<Parameter> parameters,
        Func<ZenObject, ZenValue[], ZenValue> func
    ) : base(async, name, visibility, returnType, parameters)
    {
        Func = func;
    }

    public ZenHostMethod(
        bool async,
        string name,
        ZenClass.Visibility visibility,
        ZenType returnType,
        List<Parameter> parameters,
        Func<ZenObject, ZenValue[], ZenValue> func,
        Environment? closure
    ) : base(async, name, visibility, returnType, parameters)
    {
        Func = func;
        Closure = closure;
    }

    public ZenValue Call(Interpreter interpreter, ZenObject instance, ZenValue[] arguments)
    {
        if (arguments.Length < Arity) {
            throw new Exception($"Function called with {arguments.Length} arguments, but expected {Arity}");
        }

        return Func(instance, arguments);
    }
    public override ZenValue Call(Interpreter interpreter, ZenValue[] arguments)
    {
        throw new Exception("Methods must be called with a ZenObject instance");
    }
}