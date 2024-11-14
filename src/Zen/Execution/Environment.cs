using Zen.Typing;

namespace Zen.Execution;

public class Environment {

    public Environment? Parent;

    public Dictionary<string, Variable> Variables = [];

    public Environment(Environment? parent = null) {
        Parent = parent;
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

        return Parent?.GetVariable(name) ?? null;
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