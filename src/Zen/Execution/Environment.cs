using Zen.Typing;

namespace Zen.Execution;

public class Environment {

    public Dictionary<string, Variable> Variables = [];

    public void Define(bool constant, string name, ZenType type, bool nullable) {
        Variables[name] = new Variable(name, type, nullable, constant, ZenValue.Null);
    }

    public bool Exists(string name) {
        return Variables.ContainsKey(name);
    }

    public bool IsConstant(string name) {
        return Variables[name] is Variable variable && variable.Constant;
    }

    public Variable GetVariable(string name) {
        return Variables[name];
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