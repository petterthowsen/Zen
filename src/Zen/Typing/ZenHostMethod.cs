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
        List<Argument> arguments,
        Func<ZenObject, ZenValue[], ZenValue> func
    ) : base(async, name, visibility, returnType, arguments)
    {
        Func = func;
    }

    public ZenHostMethod(
        bool async,
        string name,
        ZenClass.Visibility visibility,
        ZenType returnType,
        List<Argument> arguments,
        Func<ZenObject, ZenValue[], ZenValue> func,
        Environment? closure
    ) : base(async, name, visibility, returnType, arguments)
    {
        Func = func;
        Closure = closure;
    }

    public ZenValue Call(Interpreter interpreter, ZenObject instance, ZenValue[] argValues)
    {
        if (argValues.Length < Arity) {
            throw new Exception($"Function called with {argValues.Length} arguments, but expected {Arity}");
        }

        return Func(instance, argValues);
    }
    public override ZenValue Call(Interpreter interpreter, ZenValue[] argValues)
    {
        throw new Exception("Methods must be called with a ZenObject instance");
    }
}