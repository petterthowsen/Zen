namespace Zen.Typing;

public class ZenType {
    public static ZenType Keyword = new("keyword");
    public static ZenType Any = new("any");
    public static ZenType Object = new("object");
    public static ZenType Function = new("func");
    public static ZenType Integer = new("int");
    public static ZenType Float = new("float");
    public static ZenType Integer64 = new("int64");
    public static ZenType Float64 = new("float64");
    public static ZenType Boolean = new("bool");
    public static ZenType String = new("string");
    public static ZenType Null = new("null");
    public static ZenType Void = new("void");
    public static ZenType Array = new("array", [ZenType.String, ZenType.Integer]);
    public static ZenType Map = new("map", [ZenType.String, ZenType.String]);

    public static ZenType FromString(string name) {
        return name switch {
            "any" => Any,
            "object" => Object,
            "func" => Function,
            "int" => Integer,
            "float" => Float,
            "int64" => Integer64,
            "float64" => Float64,
            "bool" => Boolean,
            "string" => String,
            "null" => Null,
            "void" => Void,
            "array" => Array,
            "map" => Map,
            _ => new(name)
        };
    }

    public readonly string Name;

    public ZenType[] Parameters = [];

    public bool IsObject => this == Object;
    public bool IsPrimitive => this == Integer || this == Float || this == Integer64 || this == Float64 || this == Boolean || this == String || this == Null || this == Void;
    public bool IsNumeric => this == Integer || this == Float || this == Integer64 || this == Float64;

    public bool IsParametric => Parameters.Length > 0;

    public ZenType(string name, params ZenType[] parameters) {
        Name = name;
        Parameters = parameters;
    }

    public override string ToString() {
        if (IsParametric) {
            
            string paramString = "";
            for (int i = 0; i < Parameters.Length; i++) {
                paramString += $"{Parameters[i]}";
            }
            paramString = paramString.TrimEnd(',');

            return $"{Name}<{paramString}>";
        } else {
            return Name;
        }
    }
}