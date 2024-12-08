using System.Reflection;
using Zen.Typing;

namespace Zen.Execution.Builtins.Core;

public class ZenMethodProxy : ZenFunction
{
    public MethodInfo Method;

    public ZenMethodProxy(MethodInfo method, ZenType returnType, ZenType[] argTypes) : base(TYPE.HostMethod, false, false, returnType, [])
    {
        Method = method;

        // add arguments
        foreach (ParameterInfo argInfo in method.GetParameters()) {
            Arguments.Add(new Argument(argInfo.Name ?? "", Interop.ToZenType(argInfo.ParameterType)));
        }
    }

    public ZenValue Call(ZenObject instance, ZenValue[] argValues)
    {
        // cast instance to ZenObjectProxy
        ZenObjectProxy zenObjectProxy = (ZenObjectProxy) instance;

        // convert arguments
        dynamic?[] args = argValues.Select(a => Interop.ToDotNet(a)).ToArray();

        // invoke
        dynamic? result = Method.Invoke(zenObjectProxy.Target, args);

        // convert result to ZenValue
        return Interop.ToZenValue(result);
    }

    public override BoundMethod Bind(ZenObject instance) {
        return new BoundMethod(instance, this);
    }
}