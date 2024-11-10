namespace Zen.Typing;

/*

Everything is an object.
int, float, int64, float64, bool, string are aliases to their respective Classes (Integer, Float, Integer64, Float64, Boolean, String)

*/

public class ZenValue(ZenType type, dynamic? underlying) {

    public ZenType Type = type;
    public dynamic? Underlying = underlying;

    public override string ToString() {
        return $"{Type.ToString()} {Underlying}";
    }

    public bool IsNumber() {
        return Type == ZenType.Integer || Type == ZenType.Float || Type == ZenType.Integer64 || Type == ZenType.Float64;
    }
}