using Environment = Zen.Execution.Environment;
using Interpreter = Zen.Execution.Interpreter;

namespace Zen.Typing;

public class ZenHostFunction : ZenFunction {

    public readonly Func<ZenValue[], ZenValue> Func;

    public ZenHostFunction(bool async, ZenType returnType, List<Parameter> parameters, Func<ZenValue[], ZenValue> func, Environment closure) : base(async, returnType, parameters, closure) {
        Func = func;
    }

    public override ZenValue Call(Interpreter interpreter, ZenValue[] arguments)
    {
        if (arguments.Length < Arity) {
            throw new Exception($"Function called with {arguments.Length} arguments, but expected {Arity}");
        }

        return Func(arguments);
    }

    public override string ToString() {
        return $"HostFunction";
    }
}