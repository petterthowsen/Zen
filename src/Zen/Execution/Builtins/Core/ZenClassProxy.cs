using System.Reflection;
using Zen.Typing;

namespace Zen.Execution.Builtins.Core;

public class ZenClassProxy : ZenClass
{

    public Type Target;

    public ZenClassProxy(Type dotnetClass) : base(dotnetClass.Name, [], [], [])
    {
        Target = dotnetClass;
    }

    public override ZenMethod? GetOwnConstructor(ZenType[] argTypes)
    {
        return null;
    }

    public override ZenMethod? GetOwnMethod(string name, ZenType returnType, ZenType[] argTypes) {
        // use reflection on the Target to find a matching method
        List<Type> argTypesDotnet = [];

        foreach (var argType in argTypes) {
            Type? t = Interop.ToDotNet(argType);
            if (t != null) {
                argTypesDotnet.Add(t);
            }
        }

        var methodInfo = Target.GetMethod(name, [..argTypesDotnet]);

        if (methodInfo != null) {
            var methodReturnType = Interop.ToZen(methodInfo.ReturnType);

            if (TypeChecker.IsCompatible(returnType, methodReturnType)) {

                return new ZenMethodProxy(methodInfo, returnType, argTypes);
            }
        }

        return null;
    }

    public override ZenMethod? GetOwnMethod(string name)
    {
        var methodInfo = Target.GetMethod(name);

        if (methodInfo != null) {
            ZenType returnType = Interop.ToZen(methodInfo.ReturnType);
            List<ZenType> argTypes = [];

            foreach(ParameterInfo pInfo in methodInfo.GetParameters()) {
                ZenType zenType = Interop.ToZen(pInfo.GetType());
                argTypes.Add(zenType);
            }

            return new ZenMethodProxy(methodInfo, returnType, [..argTypes]);
        }

        return null;
    }

}