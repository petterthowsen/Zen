using Zen.Common;
using Zen.Execution;

namespace Zen.Typing;

public class ZenObject {

    public static ZenObject Empty => new(ZenClass.Master);

    public ZenClass Class;

    public ZenTypeClass Type;

    public Dictionary<string, ZenValue> Properties = [];

    public Dictionary<string, dynamic?> Data = [];

    public Dictionary<string, ZenValue> Parameters = [];

    // Store concrete methods with substituted type parameters
    public List<ZenMethod> Methods = [];

    public ZenObject(ZenClass clazz) {
        Class = clazz;
        Type = clazz.Type;  // Initially use the class's type, can be updated later with specific type parameters
    }

    public void HasOwnConstructor(ZenType[] argTypes, out ZenMethod? method) {
        // First check concrete methods
        HasOwnMethod(Class.Name, ZenType.Void, argTypes, out method);
        if (method != null) return;

        // Fall back to class methods
        Class.HasOwnConstructor(argTypes, out method);
    }

    public void HasOwnMethod(string name, ZenType returnType, ZenType[] argTypes, out ZenMethod? method) {
        // First check concrete methods
        foreach (ZenMethod m in Methods) {
            if (m.Name == name && m.ReturnType == returnType) {
                if (m.Arguments.Count != argTypes.Length) {
                    continue;
                }

                bool match = true;
                for (int i = 0; i < argTypes.Length; i++) {
                    if (m.Arguments[i].Type != argTypes[i]) {
                        match = false;
                        break;
                    }
                }

                if (match) {
                    method = m;
                    return;
                }
            }
        }

        // Fall back to class methods
        Class.HasOwnMethod(name, returnType, argTypes, out method);
    }

    public void HasMethodHierarchically(string name, ZenType returnType, ZenType[] argTypes, out ZenMethod? method) {
        // First check concrete methods
        HasOwnMethod(name, returnType, argTypes, out method);
        if (method != null) return;

        // Fall back to class methods
        if (Class == ZenClass.Master) {
            return;
        }

        Class.SuperClass.HasMethodHierarchically(name, returnType, argTypes, out method);
    }

    public void HasMethodHierarchically(string name, out ZenMethod? method) {
        // First check concrete methods
        HasOwnMethod(name, out method);
        if (method != null) return;

        // Fall back to class methods
        if (Class == ZenClass.Master) {
            return;
        }

        Class.SuperClass.HasMethodHierarchically(name, out method);
    }

    public void HasOwnMethod(string name, out ZenMethod? method) {
        // First check concrete methods
        foreach (var m in Methods) {
            if (m.Name == name) {
                method = m;
                return;
            }
        }

        // Fall back to class methods
        Class.HasOwnMethod(name, out method);
    }

    public ZenValue Call(Interpreter interpreter, ZenMethod method, ZenValue[] args) {
        if (method is ZenHostMethod) {
            return ((ZenHostMethod)method).Call(interpreter, this, args);
        }else {
            throw new Exception("Cannot call ZenUserMethod directly.");
        }
    }

    public bool HasProperty(string name) {
        return Properties.ContainsKey(name);
    }

    public void SetProperty(string name, ZenValue value) {
        if (!Properties.ContainsKey(name)) {
            throw Interpreter.Error($"Property {name} not found on {Class.Name}");
        }

        // Get the property's type
        var propertyType = Properties[name].Type;
        Logger.Instance.Debug($"Setting property {name} of type {propertyType} to value of type {value.Type}");

        // Check type compatibility
        if (!TypeChecker.IsCompatible(value.Type, propertyType)) {
            throw Interpreter.Error($"Cannot assign value of type '{value.Type}' to target of type '{propertyType}'");
        }

        Properties[name] = value;
    }

    public ZenValue GetProperty(string name) {
        return Properties[name];
    }

    public bool HasMember(string name) {
        return Properties.ContainsKey(name) || Methods.Any(m => m.Name == name) || Class.Methods.Any(m => m.Name == name);
    }

    public bool HasParameter(string name) {
        return Parameters.ContainsKey(name);
    }

    public void SetParameter(string name, ZenValue value) {
        Parameters[name] = value;
    }

    public ZenValue GetParameter(string name) {
        return Parameters[name];
    }
}
