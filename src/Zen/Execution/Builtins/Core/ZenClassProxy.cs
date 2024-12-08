using System.Reflection;
using Zen.Typing;

namespace Zen.Execution.Builtins.Core;

public class ZenClassProxy : ZenClass
{
    public Type Target;
    private readonly Dictionary<string, ZenFunction> _methodCache = new();
    private readonly Dictionary<(string name, string argTypes), ZenFunction> _overloadedMethodCache = new();

    public ZenClassProxy(Type dotnetClass) : base(dotnetClass.Name, [], [], [])
    {
        Target = dotnetClass;
    }

    public override ZenFunction? GetOwnConstructor(ZenValue[] argValues)
    {
        return null;
    }

    public override ZenFunction? GetOwnMethod(string name, ZenValue[] argValues, ZenType? returnType) {
        // Create a cache key using method name and argument types
        var argTypesKey = string.Join(",", argValues.Select(x => x.Type.ToString()));
        var cacheKey = (name, argTypesKey);

        // Check if we have a cached version
        if (_overloadedMethodCache.TryGetValue(cacheKey, out var cachedMethod))
        {
            // If returnType is specified, verify compatibility
            if (returnType == null || TypeChecker.IsCompatible(returnType, cachedMethod.ReturnType))
            {
                return cachedMethod;
            }
        }

        // If not in cache, create new method proxy
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
        var methodProxy = new ZenMethodProxy(methodInfo, methodReturnType, argTypes);
        
        // Cache the method proxy
        _overloadedMethodCache[cacheKey] = methodProxy;
        
        return methodProxy;
    }

    public override ZenFunction? GetOwnMethod(string name)
    {
        // Check if we have a cached version
        if (_methodCache.TryGetValue(name, out var cachedMethod))
        {
            return cachedMethod;
        }

        var methodInfo = Target.GetMethod(name);

        if (methodInfo != null) {
            ZenType returnType = Interop.ToZenType(methodInfo.ReturnType);
            List<ZenType> argTypes = [];

            foreach(ParameterInfo pInfo in methodInfo.GetParameters()) {
                ZenType zenType = Interop.ToZenType(pInfo.GetType());
                argTypes.Add(zenType);
            }

            var methodProxy = new ZenMethodProxy(methodInfo, returnType, [..argTypes]);
            
            // Cache the method proxy
            _methodCache[name] = methodProxy;
            
            return methodProxy;
        }

        return null;
    }
}
