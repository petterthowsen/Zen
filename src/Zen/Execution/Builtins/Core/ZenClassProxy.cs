using System.Reflection;
using Zen.Typing;

namespace Zen.Execution.Builtins.Core;

public class ZenClassProxy : ZenClass
{
    public Type Target;

    private List<ZenMethodProxy> _methods = [];

    public ZenClassProxy(Type dotnetClass) : base(dotnetClass.Name, [], [], [])
    {
        Target = dotnetClass;
    }

    public override ZenMethod? GetOwnConstructor(ZenValue[] argValues)
    {
        return null;
    }

    public override ZenMethod? GetOwnMethod(string name, ZenValue[] argValues, ZenType? returnType) {
        // use reflection on the Target to find a matching method
        List<Type> argTypesDotnet = [];

        foreach (ZenValue zenValue in argValues) {
            ZenType zenType = zenValue.Type;
            Type dotnetType;
            if (zenType == ZenType.DotNetObject) {
                var proxy = zenValue.Underlying as ZenObjectProxy;
                dotnetType = proxy!.Target.GetType();
            }else {
                dotnetType = Interop.ToDotNet(zenType);
            }

            argTypesDotnet.Add(dotnetType);
        }

        var methodInfo = Target.GetMethod(name, [..argTypesDotnet]);

        if (methodInfo == null) return null;

        var methodReturnType = Interop.ToZenType(methodInfo.ReturnType);

        if (returnType != null) {
            if (false == TypeChecker.IsCompatible(returnType, methodReturnType)) {
                return null;
            }
        }

        var argTypes = argValues.Select(x => x.Type).ToArray();
        return new ZenMethodProxy(methodInfo, methodReturnType, argTypes);
    }

    public override ZenMethod? GetOwnMethod(string name)
    {
        var methodInfo = Target.GetMethod(name);

        if (methodInfo != null) {
            ZenType returnType = Interop.ToZenType(methodInfo.ReturnType);
            List<ZenType> argTypes = [];

            foreach(ParameterInfo pInfo in methodInfo.GetParameters()) {
                ZenType zenType = Interop.ToZenType(pInfo.GetType());
                argTypes.Add(zenType);
            }

            return new ZenMethodProxy(methodInfo, returnType, [..argTypes]);
        }

        return null;
    }

}