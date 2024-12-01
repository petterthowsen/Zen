using Environment = Zen.Execution.Environment;
using Interpreter = Zen.Execution.Interpreter;

namespace Zen.Typing;

public class ZenHostFunction : ZenFunction {

    public readonly Func<ZenValue[], ZenValue> Func;

    public ZenHostFunction(bool async, ZenType returnType, List<Argument> arguments, Func<ZenValue[], ZenValue> func, Environment closure) : base(async, returnType, arguments, closure) {
        Func = func;
    }

    public override ZenValue Call(Interpreter interpreter, ZenValue[] argValues)
    {
        if (argValues.Length < Arity) {
            throw new Exception($"Function called with {argValues.Length} argument values, but expected {Arity}");
        }

        return Func(argValues);
    }

    public override string ToString() {
        return $"HostFunction";
    }
}