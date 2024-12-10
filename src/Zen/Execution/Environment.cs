using Zen.Common;
using Zen.Typing;

namespace Zen.Execution;

/// <summary>
/// Represents a scope in the execution environment.
/// </summary>
public class Environment {

    public Environment? Parent;

    public Dictionary<string, Variable> Variables = [];

    public string Name;

    public Environment(Environment? parent = null, string name = "env") {
        Parent = parent;
        Name = name;
        Logger.Instance.Debug($"Created environment {Name}");
    }

    /// <summary>
    /// Returns the ancestor of the environment at the given distance.
    /// For example, a distance of 0 returns the current environment,
    /// a distance of 1 returns the parent environment, and so on.
    /// Throws an exception if the requested ancestor is not available.
    /// </summary>
    public Environment Ancestor(int distance) {
        if (distance == 0) {
            return this;
        }

        Environment? current = this;

        for (int i = 0; i < distance; i++) {
            current = current.Parent;
            if (current == null) {
                throw new Exception($"Cannot get ancestor at distance {distance} - environment chain too short");
            }
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
        Variable variable = GetVariable(name);

        return variable.Value;
    }

    public void Assign(string name, ZenValue value) {
        Variables[name].Assign(value);
    }

    public void Alias(string original, string alias)
    {
        Variables[alias] = Variables[original];
    }
}
