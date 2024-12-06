namespace Zen.Typing;

public class ZenInterface : IZenClass
{

    public string Name { get; set; }
    public ZenType Type { get; set; }
    public List<ZenAbstractMethod> Methods = [];
    public List<IZenClass.Parameter> Parameters {get; set; } = [];

    public ZenInterface(string name, List<ZenAbstractMethod> methods, List<IZenClass.Parameter> parameters) {
        Name = name;
        Methods = methods;
        Parameters = parameters;
        Type = ZenType.FromClass(this);
    }

    /// <summary>
    /// Returns true if this type can be assigned a value of the given type
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool IsAssignableFrom(IZenClass other) {
        if (this == other) return true;

        if (other is ZenClass @class) {
            return @class.Implements(this);
        }

        return false;
    }

}