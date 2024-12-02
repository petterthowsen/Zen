namespace Zen.Typing;

public class ZenInterface
{

    public string Name;
    public List<ZenAbstractMethod> Methods = [];
    public ZenTypeInterface Type;
    public List<ZenClass.Parameter> Parameters = [];

    public ZenInterface(string name, List<ZenAbstractMethod> methods, List<ZenClass.Parameter> parameters) {
        Name = name;
        Methods = methods;
        Parameters = parameters;

        var parameterTypeList = parameters.Select(p => p.Type).ToArray();
        Type = new ZenTypeInterface(this, Name, parameterTypeList);
    }

}