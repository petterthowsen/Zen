using Zen.Typing;

namespace Zen.Execution.Builtins.Core;

public class String : IBuiltinsProvider
{

    // todo, make this a factory method instead
    // we'll simply use this class as a bunch of static methods useable on on string primitives
    public static ZenClass ZenString = new ZenClass(
        "String",
        [
            // --- Methods ---
            // Reverse
            new ZenHostMethod(false, "reverse", ZenClass.Visibility.Public, ZenType.String, [new("str", ZenType.String)], (ZenValue[] args) => {
                string str = args[0].Underlying ?? "";
                char[] charArray = str.ToCharArray();
                charArray = charArray.Reverse<char>().ToArray();
                return new ZenValue(ZenType.String, charArray.ToString());
            }),

            // ToUpper
            new ZenHostMethod(false, "toUpper", ZenClass.Visibility.Public, ZenType.String, [new("str", ZenType.String)], (ZenValue[] args) => {
                string str = args[0].Underlying ?? "";
                return new ZenValue(ZenType.String, str.ToUpper());
            }),

            // ToLower
            new ZenHostMethod(false, "toLower", ZenClass.Visibility.Public, ZenType.String, [new("str", ZenType.String)], (ZenValue[] args) => {
                string str = args[0].Underlying ?? "";
                return new ZenValue(ZenType.String, str.ToLower());
            }),
            
            new ZenHostMethod(false, "_getProperty", ZenClass.Visibility.Public, ZenType.Any, [new("str", ZenType.String), new("property", ZenType.String)], (ZenValue[] args) => {
                string str = args[0].Underlying ?? "";
                string prop = args[1].Underlying ?? "";
                
                switch (prop) {
                    case "length":
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

    public static void RegisterBuiltins(Interpreter interp)
    {
        Environment env = interp.globalEnvironment;
        env.Define(true, "String", ZenType.Class, false);
        env.Assign("String", new ZenValue(ZenType.Class, ZenString));
    }
}