using Zen.Common;
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

    public Dictionary<string, Property> Properties = [];

    public ZenClass(string name, List<ZenMethod> methods, List<Property> properties) {
        Name = name;
        Methods = methods;
        Properties = properties.ToDictionary(x => x.Name, x => x);
    }

    public ZenClass(string name, List<ZenMethod> methods) : this(name, methods, []) {}

    public ZenObject CreateInstance(Interpreter interpreter, params ZenValue[] args) {
        ZenType[] argTypes = args.Select(x => x.Type).ToArray();

        // has a compatable constructor?
        HasOwnConstructor(argTypes, out var constructor);

        if (constructor == null && args.Length > 0) {
            throw Interpreter.Error("No valid constructor found for class " + Name);
        }

        ZenObject instance = new ZenObject(this);

        // add properties
        foreach (Property property in Properties.Values) {
            instance.Properties.Add(property.Name, new ZenValue(property.Type, property.Default));
        }

        // call the constructor
        // (required calls to super constructors are checked at compile time)
        interpreter.CallUserFunction(constructor!.Bind(instance), args);
        
        return instance;
    }

    public void HasOwnConstructor(ZenType[] argTypes, out ZenMethod? method) {
        HasOwnMethod(Name, ZenType.Void, argTypes, out method);
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