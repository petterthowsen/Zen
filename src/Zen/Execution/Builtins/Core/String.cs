using Zen.Typing;

namespace Zen.Execution.Builtins.Core;

public class String : IBuiltinsProvider
{

    public static ZenClass StringClass;
    
    private static void createClass() {
        StringClass = new ZenClass(
            "String",
            [
                // --- Methods ---
                // Reverse
                ZenFunction.NewStaticHostMethod("Reverse", ZenType.String, [new("str", ZenType.String)], (ZenValue[] args) => {
                    string str = args[0].Underlying!;
                    char[] charArray = str.ToCharArray();
                    charArray = charArray.Reverse<char>().ToArray();
                    str = new string(charArray);
                    return new ZenValue(ZenType.String, str);
                }, false),

                // Repeat(str, count)
                ZenFunction.NewStaticHostMethod("Repeat", ZenType.String, [new("str", ZenType.String), new("count", ZenType.Integer)], (ZenValue[] args) => {
                    string sequence = args[0].Underlying!;
                    int count = args[1].Underlying!;

                    string str = "";
                    for (int i = 0; i < count; i++) {
                        str += sequence;
                    }

                    return new ZenValue(ZenType.String, str);
                }),

                // ToUpper
                ZenFunction.NewStaticHostMethod("ToUpper", ZenType.String, [new("str", ZenType.String)], (ZenValue[] args) => {
                    string str = args[0].Underlying!;
                    return new ZenValue(ZenType.String, str.ToUpper());
                }),

                // ToLower
                ZenFunction.NewStaticHostMethod("ToLower", ZenType.String, [new("str", ZenType.String)], (ZenValue[] args) => {
                    string str = args[0].Underlying!;
                    return new ZenValue(ZenType.String, str.ToLower());
                }),

                // Trim(str, chars)
                ZenFunction.NewStaticHostMethod("Trim", ZenType.String,
                [
                    new("str", ZenType.String),
                    new("chars", ZenType.String, false, new ZenValue(ZenType.String, " \t\r\n"))
                ], (ZenValue[] args) => {
                    string str = args[0].Underlying!;
                    string chars = args[1].Underlying!;

                    str = str.Trim(chars.ToCharArray());

                    return new ZenValue(ZenType.String, str);
                }),

                // TrimStart(str, chars)
                ZenFunction.NewStaticHostMethod("TrimStart", ZenType.String,
                [
                    new("str", ZenType.String),
                    new("chars", ZenType.String, false, new ZenValue(ZenType.String, " \t\r\n"))
                ], (ZenValue[] args) => {
                    string str = args[0].Underlying!;
                    string chars = args[1].Underlying!;
                    str = str.TrimStart(chars.ToCharArray());
                    return new ZenValue(ZenType.String, str);
                }),

                // TrimEnd(str, chars)
                ZenFunction.NewStaticHostMethod("TrimEnd", ZenType.String,
                [
                    new("str", ZenType.String),
                    new("chars", ZenType.String, false, new ZenValue(ZenType.String, " \t\r\n"))
                ], (ZenValue[] args) => {
                    string str = args[0].Underlying!;
                    string chars = args[1].Underlying!;

                    str = str.TrimEnd(chars.ToCharArray());

                    return new ZenValue(ZenType.String, str);
                }),

                // StartsWith(str, sequence): bool
                ZenFunction.NewStaticHostMethod("StartsWith", ZenType.Boolean, [new("str", ZenType.String), new("sequence", ZenType.String)], (ZenValue[] args) => {
                    string str = args[0].Underlying!;
                    string sequence = args[1].Underlying!;
                    return new ZenValue(ZenType.Boolean, str.StartsWith(sequence));
                }),

                // EndsWith(str, sequence): bool
                ZenFunction.NewStaticHostMethod("EndsWith", ZenType.Boolean, [new("str", ZenType.String), new("sequence", ZenType.String)], (ZenValue[] args) => {
                    string str = args[0].Underlying!;
                    string sequence = args[1].Underlying!;
                    return new ZenValue(ZenType.Boolean, str.EndsWith(sequence));
                }),

                // EnsureStartsWith(str, sequence): string
                ZenFunction.NewStaticHostMethod("EnsureStartsWith", ZenType.String, [new("str", ZenType.String), new("sequence", ZenType.String)], (ZenValue[] args) => {
                    string str = args[0].Underlying!;
                    string sequence = args[1].Underlying!;

                    str = str.StartsWith(sequence) ? str : sequence + str;
                    return new ZenValue(ZenType.String, str);
                }),

                // EnsureEndsWith(str, sequence): string
                ZenFunction.NewStaticHostMethod("EnsureEndsWith", ZenType.String, [new("str", ZenType.String), new("sequence", ZenType.String)], (ZenValue[] args) => {
                    string str = args[0].Underlying!;
                    string sequence = args[1].Underlying!;

                    str = str.EndsWith(sequence) ? str : str + sequence;
                    return new ZenValue(ZenType.String, str);
                }),

                // IndexOf(str, sequence): int
                // Returns the index of the first occurrence of the sequence in the string.
                ZenFunction.NewStaticHostMethod("IndexOf", ZenType.Integer, [new("str", ZenType.String), new("sequence", ZenType.String)], (ZenValue[] args) => {
                    string str = args[0].Underlying!;
                    string sequence = args[1].Underlying!;
                    return new ZenValue(ZenType.Integer, str.IndexOf(sequence));
                }),

                // LastIndexOf(str, sequence): int
                // Returns the index of the last occurrence of the sequence in the string.
                ZenFunction.NewStaticHostMethod("LastIndexOf", ZenType.Integer, [new("str", ZenType.String), new("sequence", ZenType.String)], (ZenValue[] args) => {
                    string str = args[0].Underlying!;
                    string sequence = args[1].Underlying!;
                    return new ZenValue(ZenType.Integer, str.LastIndexOf(sequence));                    
                }),

                // Contains(str, sequence): bool
                // Returns true if the string contains the sequence.
                ZenFunction.NewStaticHostMethod("Contains", ZenType.Boolean, [new("str", ZenType.String), new("sequence", ZenType.String)], (ZenValue[] args) => {
                    string str = args[0].Underlying!;
                    string sequence = args[1].Underlying!;
                    return new ZenValue(ZenType.Boolean, str.Contains(sequence));
                }),

                // Replace(str, sequence, replacement): string
                // Replaces all occurrences of the sequence in the string with the replacement.
                ZenFunction.NewStaticHostMethod("Replace", ZenType.String, [new("str", ZenType.String), new("sequence", ZenType.String), new("replacement", ZenType.String)], (ZenValue[] args) => {
                    string str = args[0].Underlying!;
                    string sequence = args[1].Underlying!;
                    string replacement = args[2].Underlying!;
                    return new ZenValue(ZenType.String, str.Replace(sequence, replacement));
                }),

                // get a property
                ZenFunction.NewStaticHostMethod("_GetProperty", ZenType.Any, [new("str", ZenType.String), new("property", ZenType.String)], (ZenValue[] args) => {
                    string str = args[0].Underlying!;
                    string prop = args[1].Underlying ?? "";
                    
                    switch (prop) {
                        case "Length":
                            return new ZenValue(ZenType.Integer, str.Length);
                    }

                    throw Interpreter.Error($"Access to unknown property {prop} on String.", null, Common.ErrorType.RuntimeError);
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

                string[] stringsArray;
                if (max > 0) {
                    stringsArray = str.Split(separator, max);
                }else {
                    stringsArray = str.Split(separator);
                }
                
                List<ZenValue> stringsList = [];

                for (int i = 0; i < stringsArray.Length; i++) {
                    if (max > 0 && i >= max) {
                        break;
                    }
                    stringsList.Add(new ZenValue(ZenType.String, stringsArray[i]));
                }

                ZenValue array = Core.Array.CreateInstance(interp, [..stringsList], ZenType.String);
                ZenObject arrayObj = array.Underlying!;
                arrayObj.Data["list"] = stringsList;
                arrayObj.SetProperty("Length", new ZenValue(ZenType.Integer, stringsList.Count));

                return array;
            }
        ));

        await Task.CompletedTask;
    }
}