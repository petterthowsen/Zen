using Zen.Execution;

namespace Zen.Typing;

public class ZenClass {

    public static readonly ZenClass Master = new ZenClass("Master", [
        // methods
        new ZenHostMethod("ToString", Visibility.Public, ZenType.String, [], (ZenValue[] args) => {
            ZenObject instance = ((ZenValue) args[0]).Underlying!;
            return new ZenValue(ZenType.String, "Object(" + instance.Class.Name + ")");
        }),
    ], [
        // properties
    ]);

    public enum Visibility {
        Public,
        Private,
        Protected
    }

    public struct Property(string name, ZenType type, ZenValue defaultValue, Visibility visibility = Visibility.Public) {
        public string Name = name;
        public ZenType Type = type;
        public ZenValue Default = defaultValue;
        public Visibility Visibility = visibility;
    }

    public string Name;

    public ZenClass SuperClass = Master;

    public List<ZenMethod> Methods = [];

    public List<Property> Properties = [];

    public ZenClass(string name, List<ZenMethod> methods, List<Property> properties) {
        Name = name;
        Methods = methods;
        Properties = properties;
    }

    public ZenClass(string name, List<ZenMethod> methods) : this(name, methods, []) {}

    public ZenObject CreateInstance(Interpreter interpreter, params ZenValue[] args) {
        ZenObject instance = new ZenObject(this);

        foreach (Property property in Properties) {
            instance.Properties.Add(property.Name, property.Default);
        }

        // find the init method
        instance.HasMethodHierarchically("init", ZenType.Void, args.Select(x => x.Type).ToArray(), out var initMethod);
        if (initMethod != null) {
            instance.Call(interpreter, initMethod, args);
        }

        return instance;
    }

    public void HasOwnMethod(string name, ZenType returnType, ZenType[] argTypes, out ZenMethod? method) {
        foreach (var m in Methods) {
            if (m.Name == name && m.ReturnType == returnType) {
                if (m.Parameters.Count != argTypes.Length) {
                    continue;
                }

                for (int i = 0; i < argTypes.Length; i++) {
                    if (m.Parameters[i].Type != argTypes[i]) {
                        continue;
                    }
                }

                method = m;
                return;
            }
        }

        method = null;
    }

    public void HasOwnMethod(string name, out ZenMethod? method) {
        foreach (var m in Methods) {
            if (m.Name == name) {
                method = m;
                return;
            }
        }

        method = null;
    }

    public void HasMethodHierarchically(string name, ZenType returnType, ZenType[] argTypes, out ZenMethod? method) {
        HasOwnMethod(name, returnType, argTypes, out method);
        if (method != null) {
            return;
        }

        if (this == Master) {
            return;
        }

        SuperClass.HasMethodHierarchically(name, returnType, argTypes, out method);
    }

    public void HasMethodHierarchically(string name, out ZenMethod? method) {
        HasOwnMethod(name, out method);
        if (method != null) {
            return;
        }

        if (this == Master) {
            return;
        }else {
            SuperClass.HasMethodHierarchically(name, out method);
        }
    }

    public override string ToString()
    {
        return $"Class {Name}";
    }
}