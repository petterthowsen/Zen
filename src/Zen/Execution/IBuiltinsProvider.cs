namespace Zen.Execution;

public interface IBuiltinsProvider {

    public static abstract void RegisterBuiltins(Interpreter interpreter);

}