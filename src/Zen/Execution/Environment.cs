using Zen.Typing;

namespace Zen.Execution;

public class Environment {

    public Dictionary<string, object> Variables = [];

    public void Define(string name, ZenValue value) {
        Variables[name] = value;
    }

    public bool Exists(string name) {
        return Variables.ContainsKey(name);
    }

    public void Assign(string name, ZenValue value) {
        Variables[name] = value;
    }
}