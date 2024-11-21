using Zen.Typing;

namespace Zen.Execution.Builtins.Core;

/// <summary>
/// Provides built-in functions for type conversions.
/// </summary>
public class Typing : IBuiltinsProvider
{
    public void RegisterBuiltins(Interpreter interp)
    {
        // 'type' returns the string representation of a type.
        interp.RegisterHostFunction("type", ZenType.String, [new ZenFunction.Parameter("val", ZenType.Any)], (ZenValue[] args) =>
        {
            return new ZenValue(ZenType.String, args[0].Type.ToString());
        });

        // 'int' converts a number to a Integer.
        interp.RegisterHostFunction("int", ZenType.Integer, [new ZenFunction.Parameter("val", ZenType.Any)], (ZenValue[] args) =>
        {
            return TypeConverter.Convert(args[0], ZenType.Integer);
        });

        // 'int64' converts a number to a Integer64.
        interp.RegisterHostFunction("to_int64", ZenType.Integer64, [new ZenFunction.Parameter("val", ZenType.Any)], (ZenValue[] args) =>
        {
            return TypeConverter.Convert(args[0], ZenType.Integer64);
        });

        // 'float' converts a number to a Float.
        interp.RegisterHostFunction("to_float", ZenType.Float, [new ZenFunction.Parameter("val", ZenType.Any)], (ZenValue[] args) =>
        {
            return TypeConverter.Convert(args[0], ZenType.Float);
        });

        // 'float64' converts a number to a Float64.    
        interp.RegisterHostFunction("to_float64", ZenType.Float64, [new ZenFunction.Parameter("val", ZenType.Any)], (ZenValue[] args) =>
        {
            return TypeConverter.Convert(args[0], ZenType.Float64);
        });
    }
}
