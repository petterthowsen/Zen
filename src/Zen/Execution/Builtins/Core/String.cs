using Zen.Typing;

namespace Zen.Execution.Builtins.Core;

public class String : IBuiltinsProvider
{

    // todo, make this a factory method instead
    // we'll simply use this class as a bunch of static methods useable on on string primitives
    public static ZenClass StringClass;
    
    private static void createClass() {
        StringClass = new ZenClass(
            "String",
            [
                // --- Methods ---
                // Reverse
                ZenFunction.NewStaticHostMethod("Reverse", ZenType.String, [new("str", ZenType.String)], (ZenValue[] args) => {
                    string str = args[0].Underlying ?? "";
                    char[] charArray = str.ToCharArray();
                    charArray = charArray.Reverse<char>().ToArray();
                    return new ZenValue(ZenType.String, charArray.ToString());
                }, false),

                // ToUpper
                ZenFunction.NewStaticHostMethod("ToUpper", ZenType.String, [new("str", ZenType.String)], (ZenValue[] args) => {
                    string str = args[0].Underlying ?? "";
                    return new ZenValue(ZenType.String, str.ToUpper());
                }),

                // ToLower
                ZenFunction.NewStaticHostMethod("ToLower", ZenType.String, [new("str", ZenType.String)], (ZenValue[] args) => {
                    string str = args[0].Underlying ?? "";
                    return new ZenValue(ZenType.String, str.ToLower());
                }),
                
                ZenFunction.NewStaticHostMethod("_GetProperty", ZenType.Any, [new("str", ZenType.String), new("property", ZenType.String)], (ZenValue[] args) => {
                    string str = args[0].Underlying ?? "";
                    string prop = args[1].Underlying ?? "";
                    
                    switch (prop) {
                        case "Length":
                            return new ZenValue(ZenType.Integer, str.Length);
                    }

                    return ZenValue.Null;
                })

            ],
            [
                // Properties
            ],
            [
                // Parameters
            ]
        );
    }

    public static async Task Initialize(Interpreter interp)
    {
        Environment env = interp.globalEnvironment;
        env.Define(true, "String", ZenType.Class, false);
        createClass();
        env.Assign("String", new ZenValue(ZenType.Type, StringClass.Type));
        await Task.CompletedTask;
    }

    public static async Task Register(Interpreter interp)
    {
        Environment env = interp.globalEnvironment;

        // get array class
        ZenType ArrayType = env.GetValue("Array")!.Underlying;
        ZenClass ArrayClass = (ZenClass) ArrayType.Clazz!;
        
        // make the Array<string> type
        Dictionary<string, ZenType> substitutions = [];
        substitutions.Add("T", ZenType.String);
        ZenType ArrayOfStringType = ArrayClass.Type.MakeGenericType(substitutions);

        // Split: returns an Array<string> of characters
        StringClass.Methods.Add(ZenFunction.NewStaticHostMethod(
            "Split",
            ArrayOfStringType,
            // arguments
            [
                new("str", ZenType.String),
                new("separator", ZenType.String, false, new ZenValue(ZenType.String, "")),
                new("max", ZenType.Integer, false, new ZenValue(ZenType.Integer, 0))
            ],
            (ZenValue[] args) => {
                string str = args[0].Underlying!;
                string separator = args[1].Underlying!;
                int max = args[2].Underlying!;

                string[] chars;
                if (max > 0) {
                    chars = str.Split(separator, max);
                }else {
                    chars = str.Split(separator);
                }
                
                List<ZenValue> characterValues = [];

                for (int i = 0; i < chars.Length; i++) {
                    if (max > 0 && i >= max) {
                        break;
                    }
                    characterValues.Add(new ZenValue(ZenType.String, chars[i]));
                }

                ZenValue array = Core.Array.CreateInstance(interp, [..characterValues], ZenType.String);
                ZenObject arrayObj = array.Underlying!;
                arrayObj.Data["list"] = characterValues;
                arrayObj.SetProperty("Length", new ZenValue(ZenType.Integer, characterValues.Count));

                return array;
            }
        ));

        await Task.CompletedTask;
    }
}