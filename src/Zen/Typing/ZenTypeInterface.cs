namespace Zen.Typing;

public class ZenTypeInterface : ZenType
{   
    public ZenInterface Interface;

    public ZenTypeInterface(ZenInterface @interface, string name, params ZenType[] parameters) : base(name, parameters)
    {
        Interface = @interface;
    }
}