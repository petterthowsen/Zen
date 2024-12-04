using Zen.Execution.Import;
using Zen.Typing;

namespace Zen.Execution.Builtins.Core;

public class Array : IBuiltinsProvider
{

    private static ZenClass? clazz;

    private static ZenClass GetClass(Interpreter interp)
    {
        if (clazz != null) return clazz;

        Module bracketAccess = interp.GetModule("System/Collections/BracketAccess");

        ZenInterface bracketGet = bracketAccess.environment.GetValue("BracketGet")!.Underlying;

        ZenInterface bracketSet = bracketAccess.environment.GetValue("BracketSet")!.Underlying;

        //--- Class ---
        clazz = new ZenClass("Array", [], [], []);
        //--- Methods ---
        //constructor

        clazz.Methods.Add(new ZenHostMethod(false, "Array", ZenClass.Visibility.Public, ZenType.Void, [], (ZenObject obj, ZenValue[] args) => {
            obj.Data["list"] = new List<ZenValue>();
            return ZenValue.Void;
        }));

        // _BracketGet
        clazz.Methods.Add(new ZenHostMethod(false, "_BracketGet", ZenClass.Visibility.Public, new ZenType("T", false, generic: true),
            // arguments
            [
                new("index", ZenType.Integer, false)
            ],
            (ZenObject obj, ZenValue[] args) => {
                var idx = args[0].Underlying;

                List<ZenValue> list = obj.Data["list"]!;

                return list[idx];
            }
        ));

        // _BracketSet
        clazz.Methods.Add(new ZenHostMethod(false, "_BracketSet", ZenClass.Visibility.Public, ZenType.Void,
            // arguments
            [
                new("index", ZenType.Integer, false),
                new("value", new ZenType("T", false, generic: true), false)
            ],
            (ZenObject obj, ZenValue[] args) => {
                var idx = args[0].Underlying;

                List<ZenValue> list = obj.Data["list"]!;

                list[idx] = args[1];
                return ZenValue.Void;
            }
        ));

        // Append
        clazz.Methods.Add(new ZenHostMethod(false, "Append", ZenClass.Visibility.Public, ZenType.Void,
            // arguments
            [
                new("val", new ZenType("T", false, generic: true), false)
            ],
            (ZenObject obj, ZenValue[] args) => {
                List<ZenValue> list = obj.Data["list"]!;
                list.Add(args[0]);
                obj.SetProperty("Length", new ZenValue(ZenType.Integer, list.Count));
                return ZenValue.Void;
            }
        ));

        // Prepend
        clazz.Methods.Add(new ZenHostMethod(false, "Prepend", ZenClass.Visibility.Public, ZenType.Void,
            // arguments
            [
                new("val", new ZenType("T", false, generic: true), false)
            ],
            (ZenObject obj, ZenValue[] args) => {
                List<ZenValue> list = obj.Data["list"]!;
                list = (List<ZenValue>) list.Prepend(args[0]);
                obj.SetProperty("Length", new ZenValue(ZenType.Integer, list.Count));
                return ZenValue.Void;
            }
        ));

        // Remove First
        
        // Remove First
        clazz.Methods.Add(new ZenHostMethod(false, "RemoveFirst", ZenClass.Visibility.Public, new ZenType("T", false, generic: true),
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
        clazz.Methods.Add(new ZenHostMethod(false, "RemoveLast", ZenClass.Visibility.Public, new ZenType("T", false, generic: true),
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
       clazz.Methods.Add(new ZenHostMethod(false, "Clear", ZenClass.Visibility.Public, ZenType.Void,
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
        clazz.Methods.Add(new ZenHostMethod(false, "RemoveAt", ZenClass.Visibility.Public, ZenType.Void,
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
        clazz.Methods.Add(new ZenHostMethod(false, "First", ZenClass.Visibility.Public, new ZenType("T", false, generic: true),
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
        clazz.Methods.Add(new ZenHostMethod(false, "Last", ZenClass.Visibility.Public, new ZenType("T", false, generic: true),
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
        clazz.Methods.Add(new ZenHostMethod(false, "IndexOf", ZenClass.Visibility.Public, ZenType.Integer,
            // arguments
            [
                new("val", new ZenType("T", false, generic: true), false)
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
        clazz.Methods.Add(new ZenHostMethod(false, "Contains", ZenClass.Visibility.Public, ZenType.Boolean,
            // arguments
            [
                new("val", new ZenType("T", false, generic: true), false)
            ],
            (ZenObject obj, ZenValue[] args) => {
                List<ZenValue> list = obj.Data["list"]!;
                return new ZenValue(ZenType.Boolean, list.Contains(args[0]));
            }
        ));

        ZenTypeClass type = clazz.Type;

        // Slice
        clazz.Methods.Add(new ZenHostMethod(false, "Slice", ZenClass.Visibility.Public,
            // Return Type
            // for an Array<string> Slice will also return an Array<string>
            // but since we don't know this yet, return type should be Array<T>
            // we need a reference to Array.
            // we can get the Array<T> type from clazz.Type
            // when the Array is instantiated, the Slice method will be concretized and the return type will become Array<string>.
            clazz.Type,
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

        clazz.Properties.Add("Length", new("Length", ZenType.Integer, new ZenValue(ZenType.Integer, 0), ZenClass.Visibility.Public));

        //--- Parameters ---
        clazz.Parameters.Add(new ZenClass.Parameter("T", ZenType.Type));

        // implement Bracket Access
        clazz.Interfaces.Add(bracketGet);
        clazz.Interfaces.Add(bracketSet);

        return clazz;
    }

    public static void RegisterBuiltins(Interpreter interp)
    {
        var env = interp.globalEnvironment;
        env.Define(true, "Array", ZenType.Class, false);
        env.Assign("Array", new ZenValue(ZenType.Class, GetClass(interp)));
    }
}