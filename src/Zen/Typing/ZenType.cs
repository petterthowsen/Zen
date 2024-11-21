namespace Zen.Typing;

public class ZenType {
    public static ZenType Keyword = new("keyword");
    public static ZenType Any = new("any");
    public static ZenType Object = new("object");
    public static ZenType Class = new("class");
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
    public static ZenType Array = new("array", [ZenType.String, ZenType.Integer]);
    public static ZenType Map = new("map", [ZenType.String, ZenType.String]);
    public static ZenType Type = new("type"); // New: Represents type values themselves

    public static ZenType FromString(string name) {
        return name switch {
            "any" => Any,
            "object" => Object,
            "class" => Class,
            "func" => Function,
            "BoundMethod" => BoundMethod,
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
            "type" => Type,
            _ => new(name)
        };
    }

    public readonly string Name;
    public readonly bool IsNullable;
    public ZenType[] Parameters = [];

    public bool IsObject => this == Object;
    public bool IsPrimitive => this == Integer || this == Float || this == Integer64 || this == Float64 || this == Boolean || this == String || this == Null || this == Void;
    public bool IsNumeric => this == Integer || this == Float || this == Integer64 || this == Float64;
    public bool IsParametric => Parameters.Length > 0;

    public ZenType(string name, params ZenType[] parameters) {
        Name = name;
        Parameters = parameters;
        IsNullable = false;
    }

    protected ZenType(string name, bool isNullable, params ZenType[] parameters) {
        Name = name;
        IsNullable = isNullable;
        Parameters = parameters;
    }

    public ZenType MakeNullable() {
        return new ZenType(Name, true, Parameters);
    }

    // Returns true if this type can be assigned a value of the given type
    public bool IsAssignableFrom(ZenType other) {
        // Handle type-to-type comparisons
        if (this == Type && other == Type) {
            return true;
        }

        // Null can be assigned to any nullable type
        if (other == Null && IsNullable) {
            return true;
        }

        // Any type can be assigned to itself
        if (this == other) {
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

        // Any type can be assigned to Any
        if (this == Any) {
            return true;
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
