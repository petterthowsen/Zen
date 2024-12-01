using Zen.Typing;

namespace Zen.Execution.Interop;

public class ZenObjectProxy : ZenObject
{
    public static ZenClass ZenObjectProxyClass = new ZenClass("ZenObjectProxy",
    [
        // Methods
        
    ],
    [
        // Properties
    
    ],
    [
        // Parameters
    ]);

    public object Target { get; }

    public ZenObjectProxy(object target) : base(ZenObjectProxyClass)
    {
        Target = target;
    }

    public ZenValue CallMethod(string methodName, params ZenValue[] args)
    {
        var method = Target.GetType().GetMethod(methodName);
        if (method == null)
            throw new Exception($"Method {methodName} not found on {Target.GetType()}");

        var result = method.Invoke(Target, args.Select(a => Dotnet.ToDotNet(a)).ToArray());
        return Dotnet.ToZen(result!);
    }
}