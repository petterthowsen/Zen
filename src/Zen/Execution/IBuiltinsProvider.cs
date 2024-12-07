namespace Zen.Execution;

public interface IBuiltinsProvider {

    public static abstract Task RegisterBuiltins(Interpreter interpreter);

}