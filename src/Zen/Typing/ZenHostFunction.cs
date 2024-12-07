using Environment = Zen.Execution.Environment;
using Interpreter = Zen.Execution.Interpreter;

namespace Zen.Typing;

public class ZenHostFunction : ZenFunction {

    public readonly Func<ZenValue[], ZenValue> SyncFunc;
    public readonly Func<ZenValue[], Task<ZenValue>> AsyncFunc;

    public ZenHostFunction(ZenType returnType, List<Argument> arguments, Func<ZenValue[], ZenValue> func, Environment closure) 
        : base(false, returnType, arguments, closure) 
    {
        SyncFunc = func ?? throw new ArgumentNullException(nameof(func));
    }

    public ZenHostFunction(ZenType returnType, List<Argument> arguments, Func<ZenValue[], Task<ZenValue>> asyncFunc, Environment closure) 
        : base(true, returnType, arguments, closure) 
    {
        AsyncFunc = asyncFunc ?? throw new ArgumentNullException(nameof(asyncFunc));
    }

    public override async Task<ZenValue> Call(Interpreter interpreter, ZenValue[] argValues)
    {
        if (Async) 
        {
            if (AsyncFunc == null)
                throw new InvalidOperationException("Async function implementation is not provided.");

            // Execute the asynchronous function with arguments
            return await AsyncFunc(argValues);
        }
        else 
        {
            if (SyncFunc == null)
                throw new InvalidOperationException("Synchronous function implementation is not provided.");

            // Execute the synchronous function with arguments
            return SyncFunc(argValues);
        }
    }

    public override string ToString() 
    {
        return $"HostFunction{(Async ? " (Async)" : " (Sync)")}";
    }
}