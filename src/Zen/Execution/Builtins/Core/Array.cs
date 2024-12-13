using Zen.Execution.Import;
using Zen.Typing;

namespace Zen.Execution.Builtins.Core;

public class Array : IBuiltinsProvider
{
    public static ZenClass? ArrayClass;

    public static async Task Initialize(Interpreter interp)
    {
        var env = interp.globalEnvironment;

        ArrayClass = new ZenClass("Array", [], [], [
            new IZenClass.Parameter("T", ZenType.Type)
        ]);

        env.Define(true, "Array", ZenType.Class, false);
        env.Assign("Array", new ZenValue(ZenType.Class, ArrayClass));

        await Task.CompletedTask;
    }

    public static async Task Register(Interpreter interp)
    {
        var env = interp.globalEnvironment;

        Module bracketAccessModule = await interp.GetModule("Zen/Collections/BracketAccess");
        Module iterableModule = await interp.GetModule("Zen/Collections/Iterable");
        Module arrayEnumeratorModule = await interp.GetModule("Zen/Collections/ArrayEnumerator");

        ZenInterface bracketGet = bracketAccessModule.environment.GetValue("BracketGet")!.Underlying;
        ZenInterface bracketSet = bracketAccessModule.environment.GetValue("BracketSet")!.Underlying;
        ZenInterface iterable = iterableModule.environment.GetValue("Iterable")!.Underlying;
        ZenClass arrayEnumerator = arrayEnumeratorModule.environment.GetValue("ArrayEnumerator")!.Underlying;

        //--- Properties ---
        ArrayClass!.Properties.Add("Length", new("Length", ZenType.Integer, new ZenValue(ZenType.Integer, 0), ZenClass.Visibility.Public));

        // implement interfaces
        ArrayClass.Interfaces.Add(bracketGet);
        ArrayClass.Interfaces.Add(bracketSet);
        ArrayClass.Interfaces.Add(iterable);
        
        //--- Methods ---
        // constructor
        ArrayClass.Methods.Add(ZenFunction.NewHostMethod("Array", ZenType.Void, [], (ZenObject obj, ZenValue[] args) => {
            obj.Data["list"] = new List<ZenValue>();
            return ZenValue.Void;
        }, true));

        // GetEnumerator
        ArrayClass.Methods.Add(ZenFunction.NewHostMethod("GetEnumerator", iterable.Type,
            [],
            (ZenObject self, ZenValue[] args) => {
                Dictionary<string, ZenValue> paramValues = [];
                paramValues.Add("T", self.GetParameter("T"));

                ZenObject enumerator = arrayEnumerator.CreateInstance(Interpreter.Instance, [new ZenValue(self.Type, self)], paramValues);
                return new ZenValue(iterable.Type, enumerator);
            }
        ));

        // _BracketGet
        ArrayClass.Methods.Add(ZenFunction.NewHostMethod("_BracketGet", ZenType.GenericParameter("T"),
            // arguments
            [
                new("index", ZenType.Integer, false)
            ],
            (ZenObject self, ZenValue[] args) => {
                var idx = args[0].Underlying;

                List<ZenValue> list = self.Data["list"]!;

                return list[idx];
            }
        ));

        // _BracketSet
        ArrayClass.Methods.Add(ZenFunction.NewHostMethod("_BracketSet", ZenType.Void,
            // arguments
            [
                new("index", ZenType.Integer, false),
                new("value", ZenType.GenericParameter("T"), false)
            ],
            (ZenObject obj, ZenValue[] args) => {
                var idx = args[0].Underlying;

                List<ZenValue> list = obj.Data["list"]!;

                list[idx] = args[1];
                return ZenValue.Void;
            }
        ));

        // Append
        ArrayClass.Methods.Add(ZenFunction.NewHostMethod("Append", ZenType.Void,
            // arguments
            [
                new("val", ZenType.GenericParameter("T"), false)
            ],
            (ZenObject obj, ZenValue[] args) => {
                List<ZenValue> list = obj.Data["list"]!;
                list.Add(args[0]);
                obj.SetProperty("Length", new ZenValue(ZenType.Integer, list.Count));
                return ZenValue.Void;
            }
        ));

        // Prepend
        ArrayClass.Methods.Add(ZenFunction.NewHostMethod("Prepend", ZenType.Void,
            // arguments
            [
                new("val", ZenType.GenericParameter("T"), false)
            ],
            (ZenObject obj, ZenValue[] args) => {
                List<ZenValue> list = obj.Data["list"]!;
                list = (List<ZenValue>) list.Prepend(args[0]);
                obj.SetProperty("Length", new ZenValue(ZenType.Integer, list.Count));
                return ZenValue.Void;
            }
        ));

        // Remove First
        ArrayClass.Methods.Add(ZenFunction.NewHostMethod("RemoveFirst", ZenType.GenericParameter("T"),
            // arguments
            [],
            (ZenObject obj, ZenValue[] args) => {
                List<ZenValue> list = obj.Data["list"]!;
                if (list.Count == 0) {
                    throw Interpreter.Error("Cannot Remove First from an empty list.");
                }
                ZenValue firstElement = list[0];
                list.RemoveAt(0);
                obj.SetProperty("Length", new ZenValue(ZenType.Integer, list.Count));
                return firstElement;
            }
        ));

        // RemoveLast
        ArrayClass.Methods.Add(ZenFunction.NewHostMethod("RemoveLast", ZenType.GenericParameter("T"),
            // arguments
            [],
            (ZenObject obj, ZenValue[] args) => {
                List<ZenValue> list = obj.Data["list"]!;
                if (list.Count == 0) {
                    throw Interpreter.Error("Cannot Remove Last from an empty list.");
                }
                ZenValue lastElement = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
                obj.SetProperty("Length", new ZenValue(ZenType.Integer, list.Count));
                return lastElement;
            }
        ));

        // Clear
       ArrayClass.Methods.Add(ZenFunction.NewHostMethod("Clear", ZenType.Void,
            // arguments
            [],
            (ZenObject obj, ZenValue[] args) => {
                List<ZenValue> list = obj.Data["list"]!;
                list.Clear();
                obj.SetProperty("Length", new ZenValue(ZenType.Integer, list.Count));
                return ZenValue.Void;
            }
        ));

        // RemoveAt
        ArrayClass.Methods.Add(ZenFunction.NewHostMethod("RemoveAt", ZenType.Void,
            // arguments
            [
                new("index", ZenType.Integer, false)
            ],
            (ZenObject obj, ZenValue[] args) => {
                List<ZenValue> list = obj.Data["list"]!;
                ZenValue val = args[0];

                int idx = list.IndexOf(args[0]);
                if (idx == -1) {
                    throw Interpreter.Error($"Element {val} not found in list");
                }
                else {
                    list.RemoveAt(idx);
                    obj.SetProperty("Length", new ZenValue(ZenType.Integer, list.Count));
                }
                return ZenValue.Void;
            }
        ));

        // First
        ArrayClass.Methods.Add(ZenFunction.NewHostMethod("First", ZenType.GenericParameter("T"),
            // arguments
            [],
            (ZenObject obj, ZenValue[] args) => {
                List<ZenValue> list = obj.Data["list"]!;
                if (list.Count == 0) {
                    throw Interpreter.Error("Empty list");
                }
                else {
                    return list[0];
                }
            }
        ));

        // Last
        ArrayClass.Methods.Add(ZenFunction.NewHostMethod("Last", ZenType.GenericParameter("T"),
            // arguments
            [],
            (ZenObject obj, ZenValue[] args) => {
                List<ZenValue> list = obj.Data["list"]!;
                if (list.Count == 0) {
                    throw Interpreter.Error("Empty list");
                }
                else {
                    return list[list.Count - 1];
                }
            }
        ));

        // IndexOf
        ArrayClass.Methods.Add(ZenFunction.NewHostMethod("IndexOf", ZenType.Integer,
            // arguments
            [
                new("val", ZenType.GenericParameter("T"), false)
            ],
            (ZenObject obj, ZenValue[] args) => {
                List<ZenValue> list = obj.Data["list"]!;
                int idx = list.IndexOf(args[0]);
                if (idx == -1) {
                    return new ZenValue(ZenType.Integer, -1);
                }
                else {
                    return new ZenValue(ZenType.Integer, idx);
                }
            }
        ));
        
        // Contains
        ArrayClass.Methods.Add(ZenFunction.NewHostMethod("Contains", ZenType.Boolean,
            // arguments
            [
                new("val", ZenType.GenericParameter("T"), false)
            ],
            (ZenObject obj, ZenValue[] args) => {
                List<ZenValue> list = obj.Data["list"]!;
                bool contains = list.Contains(args[0]);
                return new ZenValue(ZenType.Boolean, contains);
            }
        ));

        // Slice
        ArrayClass.Methods.Add(ZenFunction.NewHostMethod("Slice",
            // Return Type
            // for an Array<string> Slice will also return an Array<string>
            // but since we don't know this yet, return type should be Array<T>
            // we need a reference to Array.
            // we can get the Array<T> type from ArrayClass.Type
            // when the Array is instantiated, the Slice method will be concretized and the return type will become Array<string>.
            ArrayClass.Type,
            // arguments
            [
                new("start", ZenType.Integer, false),
                new("end", ZenType.Integer, false)
            ],
            (ZenObject obj, ZenValue[] args) => {
                List<ZenValue> list = obj.Data["list"]!;
                int start = args[0].Underlying;
                int end = args[1].Underlying;

                List<ZenValue> subList = list.GetRange(start, end - start);

                Dictionary<string, ZenValue> paramValues = [];
                paramValues.Add("T", obj.GetParameter("T"));

                ZenObject subArray = obj.Class.CreateInstance(Interpreter.Instance, [], paramValues);
                subArray.Data["list"] = subList;
                subArray.SetProperty("Length", new ZenValue(ZenType.Integer, subList.Count));
                return new ZenValue(subArray.Type, subArray);
            }
        ));

        // Join
        ArrayClass.Methods.Add(ZenFunction.NewHostMethod("Join", ZenType.String,
            // arguments
            [
                new("separator", ZenType.String, false, new ZenValue(ZenType.String, ""))
            ],
            (ZenObject obj, ZenValue[] args) => {
                var sep = args[0].Underlying;

                List<ZenValue> list = obj.Data["list"]!;

                var str = "";
                for (int i = 0; i < list.Count; i++) {
                    if (list[i].Type != ZenType.String) {
                        str += list[i].Stringify();
                    }else {
                        str += list[i].Underlying;
                    }
                    if (i < list.Count - 1) {
                        str += sep;
                    }
                }
                return new ZenValue(ZenType.String, str);
            }
        ));

        // ToString
        ArrayClass.Methods.Add(ZenFunction.NewHostMethod("ToString", ZenType.String,
            // arguments
            [],
            (ZenObject obj, ZenValue[] args) => {
                var list = obj.Data["list"]!;
                var str = "[";
                for (int i = 0; i < list.Count; i++) {
                    str += list[i].Stringify();
                    if (i < list.Count - 1) {
                        str += ", ";
                    }
                }
                str += "]";
                return new ZenValue(ZenType.String, str);
            }
        ));

        await Task.CompletedTask;
    }

    public static ZenValue CreateInstance(Interpreter interp, ZenValue[] items, ZenType typeParam) {
        // prepare the parameters
        Dictionary<string, ZenValue> paramValues = [];
        paramValues.Add("T", new ZenValue(ZenType.Type, typeParam));
        
        // create the instance
        ZenObject instance = ArrayClass!.CreateInstance(interp, [], paramValues);

        // append the items
        instance.Data["list"] = new List<ZenValue>(items);
        instance.SetProperty("Length", new ZenValue(ZenType.Integer, items.Length));

        // wrap it in a ZenValue with the instance's specific type
        return new ZenValue(instance.Type, instance);
    } 
}