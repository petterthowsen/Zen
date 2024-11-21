namespace Zen.Typing;

public class NullableZenType : ZenType {

    public ZenType BaseType { get; init; }

    public NullableZenType(ZenType baseType) : base(baseType.Name + "?") {
        BaseType = baseType;
    }

    public override string ToString()
    {
        return base.ToString() + "?";
    }
}