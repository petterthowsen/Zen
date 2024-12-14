using Environment = Zen.Execution.Environment;

namespace Zen.Typing;

public class BoundMethod {

    public ZenObject Instance;
    public ZenFunction Method;
    public Environment? Environment;

    public string name => Method.Name;
    public int Arity => Method.Arity;
    public List<ZenFunction.Argument> Arguments => Method.Arguments;

    public BoundMethod(ZenObject instance, ZenFunction method)  {
        Instance = instance;
        Method = method;

        if (method.IsUser) {
            Environment = new Environment(method.Closure, "bound method");
            
            // assign 'this' as the instance with its specific type
            Environment.Define(true, "this", instance.Type, false);
            Environment.Assign("this", new ZenValue(instance.Type, instance));

            // Make parameters available in the method's environment
            foreach (var parameter in instance.Class.Parameters) {
                var paramValue = instance.GetParameter(parameter.Name);
                
                // For type parameters, use the concrete type from the value
                ZenType paramType;
                if (parameter.Type.IsGeneric) {
                    paramType = (ZenType)paramValue.Underlying!;
                } else {
                    paramType = parameter.Type;
                }

                Environment.Define(true, parameter.Name, paramType, false);
                Environment.Assign(parameter.Name, paramValue);
            }
        }
    }

    public override string ToString()
    {
        return $"BoundMethod({Instance.Class.Name}.{Method.Name}: {Method.ReturnType})";
    }
}
