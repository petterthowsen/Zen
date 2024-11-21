using Zen.Typing;

namespace Zen.Execution.Builtins.Core;

public class Time : IBuiltinsProvider
{
    public void RegisterBuiltins(Interpreter interp)
    {
        // 'time' returns the current time in milliseconds.
        interp.RegisterHostFunction("time", ZenType.Integer64, [], (ZenValue[] args) =>
        {
            long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            return new ZenValue(ZenType.Integer64, milliseconds);
        });
    }
}