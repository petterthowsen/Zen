namespace Zen.Typing;

public class ZenType {
    public static ZenType Type = new("type"); // Represents type values themselves
    public static ZenType Keyword = new("keyword");
    public static ZenType Any = new("any");
    public static ZenType Object = new("Object");
    public static ZenType DotNetObject = new("DotNetObject");
    public static ZenType DotNetType = new("DotNetType");
    public static ZenType Class = new("Class");
    public static ZenType Interface = new("Interface");
    public static ZenType Function = new("func");
    public static ZenType BoundMethod = new("BoundMethod");
    public static ZenType Integer = new("int");
    public static ZenType Float = new("float");
    public static ZenType Integer64 = new("int64");
    public static ZenType Float64 = new("float64");
    public static ZenType Boolean = new("bool");
    public static ZenType String = new("string");
    public static ZenType Null = new("null");
    public static ZenType Void = new("void");
    public static ZenType Promise = new("Promise"); // Represents Promise type

    private static readonly Dictionary<string, ZenType> _primitives = new() {
        { "int", Integer },
        { "float", Float },
        { "int64", Integer64 },
        { "float64", Float64 },
        { "bool", Boolean },
        { "string", String },
        { "null", Null },
        { "void", Void },
        { "func", Function },
        { "Promise", Promise },
        { "any", Any },
    };

    public static bool Exists(string name) => _primitives.ContainsKey(name);

    public static ZenType FromString(string name, bool nullable = false) {
        ZenType type;
        if (_primitives.TryGetValue(name, out type)) {
            return nullable ? type.MakeNullable() : type;
        } else {
            return new ZenType(name, nullable);
        }
    }

    public readonly string Name;
    public readonly bool IsNullable;
    public ZenType[] Parameters = [];

    public bool IsObject => this == Object;
    public bool IsPrimitive => this == Integer || this == Float || this == Integer64 || this == Float64 || this == Boolean || this == String || this == Null || this == Void;
    public bool IsNumeric => this == Integer || this == Float || this == Integer64 || this == Float64;
    public bool IsParametric => Parameters.Length > 0;
    public bool IsPromise => this == Promise || (IsParametric && Name == "Promise"); // New: Check if type is Promise

    public bool IsGeneric = false;

    public ZenType(string name, params ZenType[] parameters) {
        Name = name;
        Parameters = parameters;
        IsNullable = false;
    }

    public ZenType(string name, bool isNullable, params ZenType[] parameters) {
        Name = name;
        IsNullable = isNullable;
        Parameters = parameters;
    }

    public ZenType(string name, bool nullable, bool generic)
    {
        Name = name;
        IsNullable = nullable;
        IsGeneric = generic;
    }

    public ZenType MakeNullable() {
        return new ZenType(Name, true, Parameters);
    }

    // Returns true if this type can be assigned a value of the given type
    public bool IsAssignableFrom(ZenType other) {
        if (other == this) return true;

        // Any type can be assigned to Any
        if (this == Any) {
            return true;
        }

        // Null can be assigned to any nullable type
        if (other == Null && IsNullable) {
            return true;
        }

        // If types are exactly the same, they're assignable
        if (Name == other.Name && IsNullable == other.IsNullable) {
            return true;
        }

        // A non-nullable type can be assigned to its nullable version
        if (IsNullable && !other.IsNullable && Name == other.Name) {
            return true;
        }

        // A nullable type cannot be assigned to its non-nullable version
        if (!IsNullable && other.IsNullable) {
            return false;
        }

        // Check parametric types
        if (IsParametric && other.IsParametric) {
            if (Name != other.Name || Parameters.Length != other.Parameters.Length) {
                return false;
            }
            
            for (int i = 0; i < Parameters.Length; i++) {
                if (!Parameters[i].IsAssignableFrom(other.Parameters[i])) {
                    return false;
                }
            }
            return true;
        }

        return false;
    }

    public override string ToString() {
        string result;
        if (IsParametric) {
            string paramString = string.Join(", ", Parameters.Select(p => p.ToString()));
            result = $"{Name}<{paramString}>";
        } else {
            result = Name;
        }

        if (IsNullable) {
            result += "?";
        }

        return result;
    }

    public override bool Equals(object? obj)
    {
        if (obj is ZenType zenType) {
            return Name == zenType.Name && 
                   IsNullable == zenType.IsNullable && 
                   Parameters.SequenceEqual(zenType.Parameters);
        }
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, IsNullable, Parameters);
    }
}
