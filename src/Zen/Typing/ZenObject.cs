using Zen.Execution;

namespace Zen.Typing;

public class ZenObject {

    public static ZenObject Empty => new(ZenClass.Master);

    public ZenClass Class;

    public Dictionary<string, ZenValue> Properties = [];

    public Dictionary<string, dynamic?> Data = [];

    public ZenObject(ZenClass clazz) {
        Class = clazz;
    }

    public void HasOwnConstructor(ZenType[] argTypes, out ZenMethod? method) {
        Class.HasOwnConstructor(argTypes, out method);
    }

    public void HasOwnMethod(string name, ZenType returnType, ZenType[] argTypes, out ZenMethod? method) {
        Class.HasOwnMethod(name, returnType, argTypes, out method);
    }

    public void HasMethodHierarchically(string name, ZenType returnType, ZenType[] argTypes, out ZenMethod? method) {
        Class.HasMethodHierarchically(name, returnType, argTypes, out method);
    }

    public void HasMethodHierarchically(string name, out ZenMethod? method) {
        Class.HasMethodHierarchically(name, out method);
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
        Properties[name] = value;
    }

    public ZenValue GetProperty(string name) {
        return Properties[name];
    }

    public bool HasMember(string name) {
        return Properties.ContainsKey(name) || Class.Methods.Any(m => m.Name == name);
    }

}