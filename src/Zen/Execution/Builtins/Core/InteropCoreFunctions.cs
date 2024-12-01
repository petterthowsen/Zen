using Zen.Execution.Interop;
using Zen.Typing;

namespace Zen.Execution.Builtins.Core;

public class InteropCoreFunctions : IBuiltinsProvider
{
    public void RegisterBuiltins(Interpreter interpreter)
    {
        interpreter.RegisterAsyncHostFunction(
            "CallDotNetAsync",
            ZenType.String, // Returns a String
            new List<ZenFunction.Argument>
            {
                new("target", ZenType.String),
                new("method", ZenType.String),
                new("args", ZenType.Any) // Array of arguments
            },
            Dotnet.CallDotNetAsync
        );
    }
}