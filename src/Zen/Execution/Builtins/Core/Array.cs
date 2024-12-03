using Zen.Typing;

namespace Zen.Execution.Builtins.Core;

public class Array : IBuiltinsProvider
{
    // Constructor
    private Func<ZenObject, ZenValue[], ZenValue> constructor = (ZenObject instance, ZenValue[] args) => {
        List<ZenValue> list = [];
        instance.Data["list"] = list;
        instance.SetProperty("length", new ZenValue(ZenType.Integer64, 0));
        return ZenValue.Void;
    };

    // Get element by index
    private Func<ZenObject, ZenValue[], ZenValue> get = (ZenObject instance, ZenValue[] args) => {
        var list = (List<ZenValue>)instance.Data["list"]!;
        var index = (int)args[1].Underlying!;

        if (index < 0 || index >= list.Count)
        {
            throw new RuntimeError($"Array index out of bounds: {index}");
        }

        return list[index];
    };

    // Set element by index
    private Func<ZenObject, ZenValue[], ZenValue> set = (ZenObject instance, ZenValue[] args) => {
        var list = (List<ZenValue>)instance.Data["list"]!;
        var index = (int)args[1].Underlying!;
        var value = args[2];
        var elementType = instance.GetParameter("T").Underlying!;

        if (index < 0 || index >= list.Count)
        {
            throw new RuntimeError($"Array index out of bounds: {index}");
        }

        if (!elementType.IsAssignableFrom(value.Type))
        {
            throw new RuntimeError($"Cannot assign value of type '{value.Type}' to array element of type '{elementType}'");
        }

        list[index] = value;
        return ZenValue.Void;
    };

    // Push element to end
    private Func<ZenObject, ZenValue[], ZenValue> push = (ZenObject instance, ZenValue[] args) => {
        var list = (List<ZenValue>)instance.Data["list"]!;
        var value = args[0];
        var elementType = instance.GetParameter("T").Underlying!;

        if (!elementType.IsAssignableFrom(value.Type))
        {
            throw new RuntimeError($"Cannot push value of type '{value.Type}' to array of type '{elementType}'");
        }

        list.Add(value);
        instance.SetProperty("length", new ZenValue(ZenType.Integer64, list.Count));
        return ZenValue.Void;
    };

    // Pop last element
    private Func<ZenObject, ZenValue[], ZenValue> pop = (ZenObject instance, ZenValue[] args) => {
        var list = (List<ZenValue>)instance.Data["list"]!;
        
        if (list.Count == 0)
        {
            throw new RuntimeError("Cannot pop from empty array");
        }

        var lastIndex = list.Count - 1;
        var value = list[lastIndex];
        list.RemoveAt(lastIndex);
        instance.SetProperty("length", new ZenValue(ZenType.Integer64, list.Count));
        return value;
    };

    // Convert to string
    private Func<ZenObject, ZenValue[], ZenValue> toString = (ZenObject instance, ZenValue[] args) => {
        var list = (List<ZenValue>)instance.Data["list"]!;
        var elements = list.Select(x => x.Stringify());
        return new ZenValue(ZenType.String, $"[{string.Join(", ", elements)}]");
    };

    public void RegisterBuiltins(Interpreter interp)
    {
        interp.globalEnvironment.Define(true, "Array", ZenType.Class, false);
        
        ZenClass array = new ZenClass("Array", [
            // Constructor
            new ZenHostMethod(false, "Array", ZenClass.Visibility.Public, ZenType.Void, [], constructor),
            
            // Bracket access methods
            new ZenHostMethod(false, "get", ZenClass.Visibility.Public, ZenType.Any, 
                [new ZenFunction.Argument("index", ZenType.Integer)], get),
            new ZenHostMethod(false, "set", ZenClass.Visibility.Public, ZenType.Void,
                [
                    new ZenFunction.Argument("index", ZenType.Integer),
                    new ZenFunction.Argument("value", ZenType.Any)
                ], set),
            
            // Array manipulation methods
            new ZenHostMethod(false, "push", ZenClass.Visibility.Public, ZenType.Void,
                [new ZenFunction.Argument("item", ZenType.Any)], push),
            new ZenHostMethod(false, "pop", ZenClass.Visibility.Public, ZenType.Any, [], pop),
            
            // toString method
            new ZenHostMethod(false, "ToString", ZenClass.Visibility.Public, ZenType.String, [], toString)
        ],
        [
            // Length property
            new ZenClass.Property("length", ZenType.Integer64, new ZenValue(ZenType.Integer64, 0)),
        ], [
            // Type parameter for element type
            new ZenClass.Parameter("T", ZenType.Type)
        ]);

        // implement BracketGet & BracketSet
        

        interp.globalEnvironment.Assign("Array", new ZenValue(ZenType.Class, array));
    }
}
