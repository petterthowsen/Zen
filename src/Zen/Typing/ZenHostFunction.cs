using Environment = Zen.Execution.Environment;
using Interpreter = Zen.Execution.Interpreter;

namespace Zen.Typing;

public class ZenHostFunction : ZenFunction {

    // For synchronous functions
    public readonly Func<ZenValue[], ZenValue> SyncFunc;

    // For asynchronous functions
    public readonly Func<ZenValue[], Task<ZenValue>> AsyncFunc;

    /// <summary>
    /// Create a new synchronous host function.
    /// </summary>
    /// <param name="returnType">The return type of the function.</param>
    /// <param name="arguments">The list of arguments the function accepts.</param>
    /// <param name="func">The synchronous function implementation.</param>
    /// <param name="closure">The environment closure.</param>
    public ZenHostFunction(ZenType returnType, List<Argument> arguments, Func<ZenValue[], ZenValue> func, Environment closure) 
        : base(false, returnType, arguments, closure) 
    {
        SyncFunc = func ?? throw new ArgumentNullException(nameof(func));
    }

    /// <summary>
    /// Create a new asynchronous host function.
    /// </summary>
    /// <param name="returnType">The return type of the function.</param>
    /// <param name="arguments">The list of arguments the function accepts.</param>
    /// <param name="asyncFunc">The asynchronous function implementation.</param>
    /// <param name="closure">The environment closure.</param>
    public ZenHostFunction(ZenType returnType, List<Argument> arguments, Func<ZenValue[], Task<ZenValue>> asyncFunc, Environment closure) 
        : base(true, returnType, arguments, closure) 
    {
        AsyncFunc = asyncFunc ?? throw new ArgumentNullException(nameof(asyncFunc));
    }

    /// <summary>
    /// Calls the host function, handling both synchronous and asynchronous executions.
    /// </summary>
    /// <param name="interpreter">The interpreter instance.</param>
    /// <param name="argValues">The arguments passed to the function.</param>
    /// <returns>A task representing the asynchronous operation, yielding an IEvaluationResult.</returns>
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