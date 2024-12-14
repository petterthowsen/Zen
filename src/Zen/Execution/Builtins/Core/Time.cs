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

        await Task.CompletedTask;
    }

    public static async Task Register(Interpreter interp)
    {
        await Task.CompletedTask;
    }
}