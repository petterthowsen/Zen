using System.Security.Cryptography.X509Certificates;

namespace Zen.Typing;

/*

Value Types:
int, float, int64, float64, bool, string

Reference Types:
class, interface, 

*/

/// <summary>
/// Represents a declared variable.
/// It can be mutable or immutable.
/// It can be of any valid ZenType.
/// </summary>
public class Variable
{
    public string Name;

    public bool Constant;
    public bool Nullable;

    public ZenType Type;

    public ZenValue? Value { get; private set; }
    public ZenObject? ObjectReference { get; private set; }

    public bool IsObject => ObjectReference != null;
    public bool IsValue => Value != null;

    public Variable(string name, ZenType type, bool nullable, bool constant, ZenValue value) {
        Name = name;
        Type = type;
        Nullable = nullable;
        Constant = constant;
        Value = value;
    }

    public Variable(string name, ZenType type, bool nullable, bool constant, ZenObject objectReference) {
        Name = name;
        Type = type;
        Nullable = nullable;
        Constant = constant;
        ObjectReference = objectReference;
    }

    public void Assign(ZenValue value) {
        ObjectReference = null;
        Value = value;
    }

    public void Assign(ZenObject objectReference) {
        Value = null;
        ObjectReference = objectReference;
    }

    public ZenValue GetZenValue() {
        return Value ?? ZenValue.Null;
    }

    public ZenObject GetZenObject() {
        return ObjectReference ?? ZenObject.Empty;
    }

    public bool IsTruthy() {
        if (Value != null) {
            return ((ZenValue) Value).IsTruthy();
        }else if (ObjectReference != null) {
            return true;
        }

        return false;
    }

    public bool IsCallable() {
        if (Value != null) {
            return ((ZenValue) Value).IsCallable();
        }else if (ObjectReference != null) {
            return false;
        }

        return false;
    }
}