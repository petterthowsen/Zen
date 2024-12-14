using Zen.Common;
using Zen.Execution;
using Zen.Execution.Builtins.Core;

namespace Zen.Typing;

public class ZenObject {

    public static ZenObject Empty => new(ZenClass.Master);

    public ZenClass Class;

    public ZenType Type;

    public Dictionary<string, ZenValue> Properties = [];

    public Dictionary<string, dynamic?> Data = [];

    public Dictionary<string, ZenValue> Parameters = [];

    // Store concrete methods with substituted type parameters
    public List<ZenFunction> Methods = [];

    public ZenObject(ZenClass clazz) {
        Class = clazz;
        Type = clazz.Type.Copy();  // Initially use the class's type, can be updated later with specific type parameters
    }

    public ZenFunction? GetOwnMethod(string name, ZenValue[] argValues, ZenType? returnType = null) {
        var argTypes = argValues.Select(v => v.Type).ToArray();

        // First check concrete methods
        foreach (ZenFunction m in Methods) {
            if (m.Name != name) {
                continue;
            }

            if (returnType != null && false == TypeChecker.IsCompatible(returnType, m.ReturnType)) {
                continue;
            }
            //todo: this is wrong for variadics
            if (m.Arguments.Count != argTypes.Length) {
                continue;
            }

            bool match = true;
            for (int i = 0; i < argTypes.Length; i++) {
                if (false == TypeChecker.IsCompatible(argTypes[i], m.Arguments[i].Type)) {
                    match = false;
                    break;
                }
            }

            if (match) {
                return m;
            }
        }

        return null;
    }


    public ZenFunction? GetOwnConstructor(ZenValue[] argValues) {
        // First check concrete methods
        var method = GetOwnMethod(Class.Name, argValues, ZenType.Void);
        if (method != null) return method;

        // Fall back to class methods
        return Class.GetOwnConstructor(argValues);
    }
  

    public virtual ZenFunction? GetOwnMethod(string name)
    {
        foreach (ZenFunction m in Methods) {
            if (m.Name == name) {
                return m;
            }
        }

        // Fall back to class methods
        return Class.GetOwnMethod(name);
    }

    public ZenFunction? GetMethodHierarchically(string name, ZenValue[] argValues, ZenType? returnType = null) {
        // First check concrete methods
        var method = GetOwnMethod(name, argValues, returnType);
        if (method != null) return method;

        method = Class.GetMethodHierarchically(name);
        if (method != null) {
            foreach (var argDef in method.Arguments) {
                if (argDef.Type.IsGeneric) {
                    return null;
                }
            }
        }
        

        return method;
    }

    public ZenFunction? GetMethodHierarchically(string name)
    {
        // first check concrete methods
        var method = GetOwnMethod(name);
        if (method != null) return method;


        method = Class.GetMethodHierarchically(name);
        if (method != null) {
            foreach (var argDef in method.Arguments) {
                if (argDef.Type.IsGeneric) {
                    return null;
                }
            }
        }
        

        return method;
    }

    public bool HasMethodHierarchically(string name) {
        // First check concrete methods
        if (HasOwnMethod(name)) return true;

        return Class.HasMethodHierarchically(name);
    }

    public bool HasOwnMethod(string name) {
        // First check concrete methods
        foreach (var m in Methods) {
            if (m.Name == name) {
                return true;
            }
        }

        // Fall back to class methods
        return Class.HasOwnMethod(name);
    }

    // public ZenValue Call(Interpreter interpreter, ZenFunction method, ZenValue[] args) {
        
    // }

    public virtual bool HasProperty(string name) {
        return Properties.ContainsKey(name);
    }

    public virtual void SetProperty(string name, ZenValue value, bool create = false) {
        if (!Properties.ContainsKey(name)) {
            if (create) {
                Properties[name] = value;
                return;
            }
            
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

    public virtual ZenValue GetProperty(string name) {
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
