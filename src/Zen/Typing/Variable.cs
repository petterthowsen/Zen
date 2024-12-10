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

    public ZenValue Value { get; private set; }

    public Variable(string name, ZenType type, bool nullable, bool constant, ZenValue value) {
        Name = name;
        Type = type;
        Nullable = nullable;
        Constant = constant;
        Value = value;
    }

    public Variable(string name, ZenType type, bool nullable, bool constant)
        : this(name, type, nullable, constant, ZenValue.Null)
    {
        
    }


    public void Assign(ZenValue value) {
        Value = value;
    }

    public bool IsTruthy() {
        return Value.IsTruthy();
    }

    public bool IsCallable() {
        return Value.IsCallable();
    }
}