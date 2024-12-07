using Zen.Parsing.AST;
using Environment = Zen.Execution.Environment;
using Interpreter = Zen.Execution.Interpreter;

namespace Zen.Typing;

public class ZenUserFunction : ZenFunction {

    public Block? Block;

    public ZenUserFunction(bool async, ZenType returnType, List<Argument> arguments, Block? block, Environment? closure) : base(async, returnType, arguments, closure) {
        Block = block;
    }

    public override Task<ZenValue> Call(Interpreter interpreter, ZenValue[] argValues) {
        if (Block == null)
        {
            throw new Exception("Cannot call placeholder function - implementation not yet available");
        }
        throw new Exception("User functions cannot be called directly");
    }

    public override string ToString() {
        return $"UserFunction";
    }
}
