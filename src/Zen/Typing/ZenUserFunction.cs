using Zen.Parsing.AST;
using Environment = Zen.Execution.Environment;
using Interpreter = Zen.Execution.Interpreter;

namespace Zen.Typing;

public class ZenUserFunction : ZenFunction {

    public Block Block;

    public ZenUserFunction(ZenType returnType, Parameter[] parameters, Block block, Environment closure) : base(returnType, parameters, closure) {
        Block = block;
    }

    public override ZenValue Call(Interpreter interpreter, ZenValue[] arguments) {
        throw new Exception("User functions cannot be called directly");
    }

    public override string ToString() {
        return $"UserFunction";
    }
}