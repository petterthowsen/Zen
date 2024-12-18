using Zen.Common;
using Zen.Execution;

namespace Zen.Typing;


/// <summary>
/// A primitive value of a given type.
/// </summary>
public readonly struct ZenValue(ZenType type, dynamic? underlying = null)
{
    // Primitive Value Type Constants:
    public static readonly ZenValue Null = new(ZenType.Null);
    public static readonly ZenValue Void = new(ZenType.Void);
    public static readonly ZenValue True = new(ZenType.Boolean, true);
    public static readonly ZenValue False = new(ZenType.Boolean, false);
    
    public ZenType Type { get; } = type;
    public dynamic? Underlying { get; } = underlying;

    public readonly bool IsNumber() => Type.IsNumeric;

    public readonly bool IsObject() => Underlying is ZenObject;

    public readonly bool IsBool() {
        return Type == ZenType.Boolean;
    }

    public readonly bool IsString() {
        return Type == ZenType.String;
    }

    public readonly bool IsVoid() {
        return Type == ZenType.Void;
    }

    public readonly bool IsNull() {
        return Type == ZenType.Null;
    }

    public readonly bool IsCallable() {
        return Type == ZenType.Function || Type == ZenType.BoundMethod || Type == ZenType.Method;
    }
    
    public readonly bool IsHostFunction() {
        return Underlying is ZenFunction && ((ZenFunction)Underlying).Type == ZenFunction.TYPE.HostFunction;
    }

    public readonly bool IsUserFunction() {
        return Underlying is ZenFunction && ((ZenFunction)Underlying).Type == ZenFunction.TYPE.UserFunction;
    }

    public bool IsTruthy() {
        if (Type == ZenType.Boolean) {
            return (bool)Underlying!;
        }else if (Type == ZenType.Integer || Type == ZenType.Integer64 || Type == ZenType.Float || Type == ZenType.Float64) {
            return Underlying != 0;
        }else if (Type == ZenType.String) {
            return !string.IsNullOrEmpty((string)Underlying!);
        }else if (Type == ZenType.Null) {
            return false;
        }else if (Type == ZenType.Void) {
            return false;
        }

        return true;
    }

    public T GetValue<T>() {
        return (T)Underlying!;
    }

    public override string ToString() {
        return $"{Type} {Underlying}";
    }

    public string Stringify() {
        if (Type == ZenType.Void) {
            return "void";
        }else if (Type == ZenType.Null) {
            return "null";
        }else if (Type == ZenType.Boolean) {
            if (Underlying == true) {
                return "true";
            }else {
                return "false";
            }
        }else if (Type == ZenType.String) 
        {
            return $"{Underlying}";
        }else {
            return Underlying!.ToString();
        }
    }
}