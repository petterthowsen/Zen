namespace Zen.Typing;

public class ZenTypeClass : ZenType
{   
    public ZenClass Clazz;

    public ZenTypeClass(ZenClass clazz, string name, params ZenType[] parameters) : base(name, parameters)
    {
        Clazz = clazz;
    }
}