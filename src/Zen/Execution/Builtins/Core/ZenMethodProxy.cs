using System.Reflection;
using Zen.Typing;

namespace Zen.Execution.Builtins.Core;

public class ZenMethodProxy : ZenMethod
{

    public MethodInfo Method;

    public ZenMethodProxy(MethodInfo method, ZenType returnType, ZenType[] argTypes) : base(false, method.Name, ZenClass.Visibility.Public, returnType, [])
    {
        Method = method;
        // add arguments
        foreach (ParameterInfo argInfo in method.GetParameters()) {
            Arguments.Add(new Argument(argInfo.Name ?? "", Interop.ToZenType(argInfo.ParameterType)));
        }
    }

    public override ZenValue Call(Interpreter interpreter, ZenValue[] arguments)
    {
        throw new Exception("Methods must be called with a ZenObject instance");
    }

    public ZenValue Call(Interpreter interpreter, ZenObject instance, ZenValue[] argValues)
    {
        if (argValues.Length < Arity) {
            throw new Exception($"Function called with {argValues.Length} arguments, but expected {Arity}");
        }

        if (instance is not ZenObjectProxy) {
            throw new Exception("ZenMethodProxy must be called with a ZenObjectProxy instance!");
        }

        ZenObjectProxy zenObjectProxy = (ZenObjectProxy) instance;

        dynamic?[] args = argValues.Select(a => Interop.ToDotNet(a)).ToArray();
        dynamic? result = Method.Invoke(zenObjectProxy.Target, args);
        var resultZen = Interop.ToZenValue(result);
        return resultZen;
    }

    public override BoundMethod Bind(ZenObject instance) {
        return new BoundMethod(instance, this);
    }
}