using Zen.Typing;

namespace Zen.Execution.Builtins.Core;

public class Async : IBuiltinsProvider
{
    public Async()
    {
    }

    public static async Task Initialize(Interpreter interp)
    {
        interp.RegisterHostFunction(
            "delay",
            ZenType.Integer,
            [new ZenFunction.Argument("ms", ZenType.Integer, false)],
            async (ZenValue[] args) =>
            {
                int ms = (int)args[0].Underlying;

                long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                await Task.Delay(ms);

                long time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                long elapsed = time - start;

                // Return the actual elapsed time, which may not be exactly the same as requested time.
                return new ZenValue(ZenType.Integer, (int) elapsed);
            }
        );

        // Run function
        // Queues a function to be run on the event loop
        interp.RegisterHostFunction(
            "run",
            ZenType.Task,
            [new ZenFunction.Argument("func", ZenType.Function)],
            (ZenValue[] args) =>
            {
                ZenFunction func = (ZenFunction) args[0].Underlying!;

                return interp.RunOnEventLoop(async () => {
                    return (await interp.CallFunction(func, [])).Value;
                });
            }
        );
        
        await Task.CompletedTask;
    }

    public static async Task Register(Interpreter interpreter)
    {
        await Task.CompletedTask;
    }
}