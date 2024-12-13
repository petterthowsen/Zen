namespace Zen.Execution;

public interface IBuiltinsProvider {

    public static abstract Task Initialize(Interpreter interpreter);

    public static abstract Task Register(Interpreter interpreter);

}