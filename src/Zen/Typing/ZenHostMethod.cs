using Zen.Execution;
using Environment = Zen.Execution.Environment;

namespace Zen.Typing;

public class ZenHostMethod : ZenMethod
{

    public readonly Func<ZenObject, ZenValue[], ZenValue>? Func;
    public readonly Func<ZenValue[], ZenValue>? StaticFunc;

    public ZenHostMethod(
        bool async,
        string name,
        ZenClass.Visibility visibility,
        ZenType returnType,
        List<Argument> arguments,
        Func<ZenObject, ZenValue[], ZenValue> func
    ) : base(async, name, visibility, returnType, arguments, false)
    {
        Func = func;
    }

    public ZenHostMethod(
        bool async,
        string name,
        ZenClass.Visibility visibility,
        ZenType returnType,
        List<Argument> arguments,
        Func<ZenValue[], ZenValue> staticFunc
    ) : base(async, name, visibility, returnType, arguments, true)
    {
        StaticFunc = staticFunc;
    }

    public ZenValue Call(Interpreter interpreter, ZenObject instance, ZenValue[] argValues)
    {
        if (this.Static) {
            return StaticFunc!(argValues);
        } else {
            return Func!(instance, argValues);
        }
    }

    public override ZenValue Call(Interpreter interpreter, ZenValue[] argValues)
    {
        if (Static) {
            return StaticFunc!(argValues);
        }else {
            throw new Exception("Instance Methods must be called with a ZenObject instance.");
        }
    }

    public override BoundMethod Bind(ZenObject instance) {
        return new BoundMethod(instance, this);
    }
}