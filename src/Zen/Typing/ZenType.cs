using System.Data;

namespace Zen.Typing;

/// <summary>
/// Represents a kind of type.
/// </summary>
public enum ZenTypeKind {
    Primitive,          // For primitive types like int, string, null etc.
    Class,              // For classes
    Interface,          // For interfaces
    GenericParameter,   // For placeholders like 'T'
    Union,              // For union types like 'int or float'
    DotNet,             // For .NET types
}

public class ZenType {
    // Primitive types
    public static readonly ZenType DotNetObject = new(ZenTypeKind.Primitive, "DotNetObject");
    public static readonly ZenType DotNetType = new(ZenTypeKind.Primitive, "DotNetType");

    public static readonly ZenType Type = new(ZenTypeKind.Primitive, "type");
    public static readonly ZenType Boolean = new(ZenTypeKind.Primitive, "bool");
    public static readonly ZenType Null = new(ZenTypeKind.Primitive, "null");
    public static readonly ZenType Void = new(ZenTypeKind.Primitive, "void");
    public static readonly ZenType Integer = new(ZenTypeKind.Primitive, "int");
    public static readonly ZenType Integer64 = new(ZenTypeKind.Primitive, "int64");
    public static readonly ZenType Float = new(ZenTypeKind.Primitive, "float");
    public static readonly ZenType Float64 = new(ZenTypeKind.Primitive, "float64");
    public static readonly ZenType String = new(ZenTypeKind.Primitive, "string");

    public static readonly ZenType Any = new(ZenTypeKind.Primitive, "any");
    public static readonly ZenType Object = new(ZenTypeKind.Primitive, "object");

    public static readonly ZenType Function = new(ZenTypeKind.Primitive, "Func");
    public static readonly ZenType Class = new(ZenTypeKind.Primitive, "class");
    public static readonly ZenType Interface = new(ZenTypeKind.Primitive, "interface");
    public static readonly ZenType BoundMethod = new(ZenTypeKind.Primitive, "BoundMethod");
    public static readonly ZenType Method = new(ZenTypeKind.Primitive, "Method");
    public static readonly ZenType Promise = new(ZenTypeKind.Primitive, "Promise", null, [new(ZenTypeKind.GenericParameter, "T")]);
    public static readonly ZenType Task = new(ZenTypeKind.Primitive, "Task", null, [new(ZenTypeKind.GenericParameter, "T")]);
    public static readonly ZenType Keyword = new(ZenTypeKind.Primitive, "Keyword");

    // Nullable    
    public static readonly ZenClass NullableClass = new ZenClass("Nullable", [], [], [new IZenClass.Parameter("T", Type)]);
    public static readonly ZenType NullableType = FromClass(NullableClass);

    public ZenTypeKind Kind;
    public string Name;
    public IZenClass? Clazz;
    public ZenType[] Parameters = [];

    public bool IsObject => this == Object;
    public bool IsPrimitive => this == Integer || this == Function || this == Float || this == Integer64 || this == Float64 || this == Boolean || this == String || this == Null || this == Void;
    public bool IsNumeric => this == Integer || this == Float || this == Integer64 || this == Float64;
    public bool IsParametric => Parameters.Length > 0;
    public bool IsPromise => this == Promise || (IsParametric && Name == "Promise");
    public bool IsTask => this == Task || (IsParametric && Name == "Task");
    public bool IsClass => Kind == ZenTypeKind.Class || Kind == ZenTypeKind.Interface;
    public bool IsUnion => Kind == ZenTypeKind.Union;

    /// <summary>
    /// Returns true if this type or any of its parameters is generic and hence unresolved.
    /// See <see cref="MakeGenericType"/> to substitute generic parameters with concrete types.
    /// </summary>
    public bool IsGeneric => Kind == ZenTypeKind.GenericParameter || Parameters.Any(p => p.IsGeneric);

    // Factory constructor for primitives
    private ZenType(ZenTypeKind kind, string name) {
        Kind = kind;
        Name = name;
        Parameters = [];
    }

    // Factory constructor for classes/interfaces with parameters
    private ZenType(ZenTypeKind kind, IZenClass clazz, ZenType[] parameters) {
        Kind = kind;
        Clazz = clazz;
        Name = clazz.Name;
        Parameters = parameters;
    }

    // Generic parameter factory
    private ZenType(string paramName) {
        Kind = ZenTypeKind.GenericParameter;
        Name = paramName;
        Parameters = [];
    }

    private ZenType(ZenTypeKind kind, string name, IZenClass? clazz, ZenType[] parameters) {
        Kind = kind;
        Name = name;
        Clazz = clazz;
        Parameters = parameters;
    }

    // DotNet type
    public static ZenType MakeDotNetType(string name) => new ZenType(ZenTypeKind.DotNet, name);

    // Generic parameter placeholder
    public static ZenType GenericParameter(string name) => new(name);

    // Create a union type from a list of types
    public static ZenType Union(string name, params ZenType[] types) {
        return new ZenType(ZenTypeKind.Union, name, null, types);
    }

    // Create a non-generic class type
    public static ZenType FromClass(IZenClass clazz) {
        ZenTypeKind kind = clazz is ZenInterface ? ZenTypeKind.Interface : ZenTypeKind.Class;

        if (clazz.Parameters.Count > 0) {
            // Return the generic type definition by using generic parameters from class definition
            var parameters = clazz.Parameters
                                  .Select(p => GenericParameter(p.Name))
                                  .ToArray();
            return new ZenType(kind, clazz, parameters);
        } else {
            return new ZenType(kind, clazz, []);
        }
    }

    /// <summary>
    /// Create a constructed generic type from this base type, where generic parameters are substituted with the types in given dictionary.
    /// Substitutions can be recursive.
    /// </summary>
    /// <param name="substitutions"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public ZenType MakeGenericType(Dictionary<string, ZenType> substitutions) {
        if (Kind != ZenTypeKind.Class && Kind != ZenTypeKind.Interface)
            throw new InvalidOperationException("Cannot make generic type from non-class/interface type");

        if ( ! IsGeneric) return Copy();

        if (Clazz!.Parameters.Count == 0 || substitutions.Count != Clazz.Parameters.Count)
            return Copy(); // nothing to do

        // if this type is for example: Array<T>, return Array<U> where U is the substitution for T
        // we support multiple substitutions as well as recursive substitutions
        return SubstitutedGenerics(substitutions);
    }

    /// <summary>
    /// Returns a copy of this type.
    /// </summary>
    public ZenType Copy() {
        var paramss = Parameters.Select(p => p.Copy()).ToArray();
        return new ZenType(Kind, Name, Clazz, paramss);
    }

    /// <summary>
    /// Returns a copy of this type with generic parameters substituted with the given dictionary.
    /// For example, if this type is Array<T> and substitutions contains T -> U, then the result is Array<U>.
    /// </summary>
    /// <param name="substitutions"></param>
    /// <returns></returns>
    public ZenType SubstitutedGenerics(Dictionary<string, ZenType> substitutions) {
        ZenType newType;

        if (Kind == ZenTypeKind.GenericParameter && substitutions.ContainsKey(Name)) {
            newType = substitutions[Name].Copy();
        }else {
            newType = Copy();
        }

        for (int i = 0; i < Parameters.Length; i++) {
            newType.Parameters[i] = Parameters[i].SubstitutedGenerics(substitutions);
        }

        return newType;
    }

    // Returns true if this type can be assigned a value of the given type
    //TODO: move this to the TypeChecker
    public bool IsAssignableFrom(ZenType other) {
        if (other == this) return true;

        // Any type can be assigned to Any
        if (this == Any) {
            return true;
        }

        // If this is a union type
        if (IsUnion) {
            if (other.IsUnion) {
                // All members of 'other' must be assignable to at least one member of 'this'
                return other.Parameters.All(o => this.Parameters.Any(t => t.IsAssignableFrom(o)));
            } else {
                // 'other' is a single type; it must be assignable to at least one member of 'this'
                return this.Parameters.Any(t => t.IsAssignableFrom(other));
            }
        }

        // If 'other' is a union type and 'this' is not
        if (other.IsUnion) {
            // All members of 'other' must be assignable to 'this'
            return other.Parameters.All(o => this.IsAssignableFrom(o));
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
        if (IsUnion) {
            result = string.Join(" or ", Parameters.Select(p => p.ToString()));
        } else if (IsParametric) {
            string paramString = string.Join(", ", Parameters.Select(p => p.ToString()));
            result = $"{Name}<{paramString}>";
        } else {
            result = Name;
        }

        return result;
    }

    public static bool operator ==(ZenType left, ZenType right)
    {
        // If both sides are null, return true
        if (ReferenceEquals(left, right)) return true;

        // If one side is null, return false
        if (left is null || right is null) return false;

        // Otherwise, use the overridden Equals method
        return left.Equals(right);
    }

    public static bool operator !=(ZenType left, ZenType right)
    {
        return !(left == right);
    }

    // Type equality check
    public override bool Equals(object? obj) {
        if (obj is not ZenType other) return false;
        if (Kind != other.Kind) return false;

        // For primitive
        if (Kind == ZenTypeKind.Primitive)
            return Name == other.Name;

        // For generic parameter
        if (Kind == ZenTypeKind.GenericParameter)
            return Name == other.Name; // parameter name matches

        // For union types, order doesn't matter
        if (Kind == ZenTypeKind.Union)
            return Parameters.OrderBy(p => p.ToString()).SequenceEqual(other.Parameters.OrderBy(p => p.ToString()));

        // For class/interface:
        if (Clazz != other.Clazz) return false;
        return Parameters.SequenceEqual(other.Parameters);
    }

    // TODO: might need to include Clazz here.
    public override int GetHashCode() {
        // If using .NET 5 or higher, you can use HashCode.Combine:
        var hash = HashCode.Combine(Name, Kind, Clazz);
        foreach (var p in Parameters) {
            hash = HashCode.Combine(hash, p);
        }
        return hash;
    }
}
