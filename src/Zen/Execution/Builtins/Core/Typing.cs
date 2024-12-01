using Zen.Typing;

namespace Zen.Execution.Builtins.Core;

/// <summary>
/// Provides built-in functions for type conversions.
/// </summary>
public class Typing : IBuiltinsProvider
{
    public void RegisterBuiltins(Interpreter interp)
    {
        // 'type' returns the type of a value as a ZenValue of type ZenType.Type
        interp.RegisterHostFunction("type", ZenType.Type, [new ZenFunction.Argument("val", ZenType.Any)], (ZenValue[] args) =>
        {
            ZenValue val = args[0];
            return new ZenValue(ZenType.Type, val.Type);
        });

        // register built in value constants 
        interp.globalEnvironment.Define(true, "true", ZenType.Boolean, false);
        interp.globalEnvironment.Assign("true", new ZenValue(ZenType.Boolean, true));

        interp.globalEnvironment.Define(true, "false", ZenType.Boolean, false);
        interp.globalEnvironment.Assign("false", new ZenValue(ZenType.Boolean, false));

        interp.globalEnvironment.Define(true, "null", ZenType.Null, false);
        interp.globalEnvironment.Assign("null", new ZenValue(ZenType.Null, null));

        // register built in types
        interp.globalEnvironment.Define(true, "int", ZenType.Type, false);
        interp.globalEnvironment.Assign("int", new ZenValue(ZenType.Type, ZenType.Integer));

        interp.globalEnvironment.Define(true, "int64", ZenType.Type, false);
        interp.globalEnvironment.Assign("int64", new ZenValue(ZenType.Type, ZenType.Integer64));

        interp.globalEnvironment.Define(true, "float", ZenType.Type, false);
        interp.globalEnvironment.Assign("float", new ZenValue(ZenType.Type, ZenType.Float));

        interp.globalEnvironment.Define(true, "float64", ZenType.Type, false);
        interp.globalEnvironment.Assign("float64", new ZenValue(ZenType.Type, ZenType.Float64));

        interp.globalEnvironment.Define(true, "string", ZenType.Type, false);
        interp.globalEnvironment.Assign("string", new ZenValue(ZenType.Type, ZenType.String));

        interp.globalEnvironment.Define(true, "bool", ZenType.Type, false);
        interp.globalEnvironment.Assign("bool", new ZenValue(ZenType.Type, ZenType.Boolean));

        interp.globalEnvironment.Define(true, "function", ZenType.Type, false);
        interp.globalEnvironment.Assign("function", new ZenValue(ZenType.Type, ZenType.Function));

        interp.globalEnvironment.Define(true, "object", ZenType.Type, false);
        interp.globalEnvironment.Assign("object", new ZenValue(ZenType.Type, ZenType.Object));

        interp.globalEnvironment.Define(true, "class", ZenType.Type, false);
        interp.globalEnvironment.Assign("class", new ZenValue(ZenType.Type, ZenType.Class));
    }
}
