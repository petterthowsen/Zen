namespace Zen.Typing;

public class ZenTypeInterface : ZenType
{   
    public ZenInterface ZenInterface;

    public ZenTypeInterface(ZenInterface @interface, string name, params ZenType[] parameters) : base(name, parameters)
    {
        ZenInterface = @interface;
    }
}