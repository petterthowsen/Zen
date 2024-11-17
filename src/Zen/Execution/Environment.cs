using Zen.Typing;

namespace Zen.Execution;

public class Environment {

    public Environment? Parent;

    public Dictionary<string, Variable> Variables = [];

    public Environment(Environment? parent = null) {
        Parent = parent;
    }

    /// <summary>
    /// Returns the ancestor of the environment at the given distance.
    /// For example, a distance of 0 returns the current environment,
    /// a distance of 1 returns the parent environment, and so on.
    /// If the requested ancestor is not available (i.e., the environment
    /// at the requested distance has no parent), the function returns the
    /// highest ancestor available.
    /// </summary>
    public Environment Ancestor(int distance) {
        if (distance == 0) {
            return this;
        }

        Environment current = this;

        for (int i = 0; i < distance; i++) {
            if (current.Parent == null) {
                break;
            }
            current = current.Parent;
        }
        
        return current;
    }

    public void Define(bool constant, string name, ZenType type, bool nullable) {
        Variables[name] = new Variable(name, type, nullable, constant, ZenValue.Null);
    }

    public bool Exists(string name) {
        if (Variables.ContainsKey(name)) {
            return true;
        }
        return Parent?.Exists(name) ?? false;
    }

    public bool ExistsAt(int distance, string name) {
        return Ancestor(distance).Exists(name);
    }

    public Variable GetAt(int distance, string name) {
        return Ancestor(distance).GetVariable(name);
    }

    void AssignAt(int distance, string name, ZenValue value) {
        Ancestor(distance).Assign(name, value);
    }

    public bool IsConstant(string name) {
        if (Variables.ContainsKey(name)) {
            return Variables[name] is Variable variable && variable.Constant;
        }

        return Parent?.IsConstant(name) ?? false;
    }

    public Variable GetVariable(string name) {
        if (Variables.ContainsKey(name)) {
            return Variables[name];
        }
        
        return Parent!.GetVariable(name);
    }

    public dynamic? GetValue(string name) {
        Variable variable = Variables[name];
        if (variable.IsObject) {
            return variable.ObjectReference;
        }else {
            return variable.Value;
        }
    }

    public void Assign(string name, ZenValue value) {
        Variables[name].Assign(value);
    }

    public void Assign(string name, ZenObject value) {
        Variables[name].Assign(value);
    }
}