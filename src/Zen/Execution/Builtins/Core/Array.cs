using Zen.Typing;

namespace Zen.Execution.Builtins.Core;

public class Array : IBuiltinsProvider
{

    // constructor
    private Func<ZenValue[], ZenValue> constructor = (ZenValue[] args) => {
        ZenObject instance = args[0].GetValue<ZenObject>();

        List<dynamic> list = [];
        instance.Data["list"] = list;

        instance.SetProperty("length", new ZenValue(ZenType.Integer64, 0));
        return ZenValue.Void;
    };

    public void RegisterBuiltins(Interpreter interp)
    {
        interp.globalEnvironment.Define(true, "Array", ZenType.Class, false);
        
        ZenClass array = new ZenClass("Array", [
            // methods
            new ZenHostMethod(false, "Array", ZenClass.Visibility.Public, ZenType.Void, [], constructor)
        ],
        [
            // properties
            new ZenClass.Property("length", ZenType.Integer64, new ZenValue(ZenType.Integer64, 0)),
        ], [
            // the type of the objects - though it would be nice to have a way to have generic parameters
            // have a default value, so we can default to ZenType.Any
            ZenType.Type
        ]);

        interp.globalEnvironment.Assign("Array", new ZenValue(ZenType.Class, array));
        interp.globalEnvironment.Alias("Array", "array");
    }
}