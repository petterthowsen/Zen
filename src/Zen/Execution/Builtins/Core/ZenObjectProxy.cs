using System.Reflection;
using Zen.Common;
using Zen.Typing;

namespace Zen.Execution.Builtins.Core;

// TODO:
// when creating a proxy object for a class, get or create the Class and Type and cache them for future use.
public class ZenObjectProxy : ZenObject
{
    public object Target { get; }

    public ZenObjectProxy(object target, ZenClassProxy proxyClass) : base(proxyClass)
    {
        Target = target;
    }
    
    public override bool HasProperty(string name) {
        PropertyInfo? property = Target.GetType().GetProperty(name);
        return property != null;
    }

    public override void SetProperty(string name, ZenValue value) {
        PropertyInfo? property = Target.GetType().GetProperty(name);

        if (property == null) {
            throw Interpreter.Error($"Property {name} not found on {Class.Name}");
        }

        // Get the property's type
        Type propertyType = property.PropertyType;
        ZenType propertyTypeZen = Interop.ToZen(propertyType);

        Logger.Instance.Debug($"Setting property {name} of type {propertyTypeZen} to value of type {value.Type}");

        // Check type compatibility
        if (!TypeChecker.IsCompatible(value.Type, propertyTypeZen)) {
            throw Interpreter.Error($"Cannot assign value of type '{value.Type}' to target of type '{propertyTypeZen}'");
        }

        property.SetValue(Target, Interop.ToDotNet(value));
    }

    public override ZenValue GetProperty(string name) {
        PropertyInfo? property = Target.GetType().GetProperty(name);

        if (property == null) {
            throw Interpreter.Error($"Property {name} not found on {Target.GetType().FullName}");
        }

        // Get the property's value
        object? propertyValue = property.GetValue(Target);

        // Convert the property value to a ZenValue
        return Interop.ToZen(propertyValue);
    }
}