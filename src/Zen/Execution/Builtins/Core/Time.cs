using Zen.Common;
using Zen.Typing;

namespace Zen.Execution.Builtins.Core;

public class Time : IBuiltinsProvider
{
    public static async Task Initialize(Interpreter interp)
    {
        // 'time' returns the current time in milliseconds.
        interp.RegisterHostFunction("time", ZenType.Integer64, [], (ZenValue[] args) =>
        {
            long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            return new ZenValue(ZenType.Integer64, milliseconds);
        });

        // Register a test async function that waits for a given number of milliseconds
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

        await Task.CompletedTask;
    }

    public static async Task Register(Interpreter interp)
    {
        await Task.CompletedTask;
    }
}