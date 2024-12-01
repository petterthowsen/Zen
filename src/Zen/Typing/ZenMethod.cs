using Zen.Common;
using Zen.Execution;
using Environment = Zen.Execution.Environment;

namespace Zen.Typing;

public abstract class ZenMethod : ZenFunction
{
    public string Name;
    public ZenClass.Visibility Visibility;

    public ZenMethod(bool async, string name, ZenClass.Visibility visibility, ZenType returnType, List<Argument> arguments) : base(async, returnType, arguments) {
        Name = name;
        Visibility = visibility;
    }

    public override ZenValue Call(Interpreter interpreter, ZenValue[] arguments)
    {
        throw new Exception("Methods must be called with a ZenObject instance");
    }

    public BoundMethod Bind(ZenObject instance) {
        Environment environment = new Environment(Closure);
        
        // assign 'this' as the instance with its specific type
        environment.Define(true, "this", instance.Type, false);
        environment.Assign("this", new ZenValue(instance.Type, instance));

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

            environment.Define(true, parameter.Name, paramType, false);
            environment.Assign(parameter.Name, paramValue);
            Logger.Instance.Debug($"Assigning class parameter {parameter.Name} to {paramValue} for bound method.");
        }

        return new BoundMethod(instance, this, environment);
    }
}
