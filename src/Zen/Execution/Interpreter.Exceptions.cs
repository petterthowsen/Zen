using Zen.Common;
using Zen.Exection;
using Zen.Execution.EvaluationResult;
using Zen.Parsing.AST.Statements;
using Zen.Typing;

namespace Zen.Execution;

public partial class Interpreter
{

    protected async Task<ZenObject> CreateZenException(string message) {
        ZenClass zenExceptionClass = (await FetchSymbol("Zen/Exception", "Exception")).Underlying!;
        return zenExceptionClass.CreateInstance(this, [new ZenValue(ZenType.String, message)], []);
    }

    protected async Task<ZenException> ConvertToZenException(string message)
    {
        ZenObject zenExceptionObject = await CreateZenException(message);
        ZenValue zenExceptionValue = new ZenValue(zenExceptionObject.Type, zenExceptionObject);
        return new ZenException(zenExceptionValue);
    }

    protected async Task<ZenException> ConvertToZenException(Exception e)
    {
        if (e is ZenException zenException) {
            return zenException;
        }

        return await ConvertToZenException(e.Message);
    }

    protected async Task<ZenException> ConvertToZenException(ZenValue message)
    {
        if (message.Type == ZenType.String) {
            return await ConvertToZenException(message.Underlying);
        }

        return await ConvertToZenException(message.Stringify());
    }

    public async Task<IEvaluationResult> VisitAsync(ThrowStmt throwStmt)
    {
        ZenValue zenException = Environment.GetValue("Exception");
        ZenType zenExceptionType = zenException.Underlying!.Type;
        

        IEvaluationResult exprResult = await Evaluate(throwStmt.Expression);
        ZenValue result = exprResult.Value;

        // is it a string?
        // If so, we automatically create an Exception
        if (result.Type == ZenType.String) {
            throw await ConvertToZenException(result);
        }

        // make sure the result type is compatable with zen's builtin Exception class
        if (!TypeChecker.IsCompatible(result.Type, zenExceptionType)) {
            throw Error($"Cannot throw value of type '{result.Type}'. Must either be a '{zenExceptionType}' or a subclass of it.", throwStmt.Location, Common.ErrorType.TypeError);
        }

        throw new ZenException(result);
    }

    public async Task<IEvaluationResult> VisitAsync(TryStmt tryStmt)
    {
        try {
            return await VisitAsync(tryStmt.Block);
        } catch (Exception ex) {
            ZenException zenEx;

            if (ex is not ZenException) {
                zenEx = await ConvertToZenException(ex);
            }else {
                zenEx = (ZenException) ex;
            }

            // find a matching catch block
            foreach (CatchStmt catchStmt in tryStmt.catchStmts) {
                // if there's no typehint, it's a catch-all
                if (catchStmt.TypeHint == null) {
                    return await EvaluateCatchStatement(catchStmt, zenEx.Exception);
                }else {
                    ZenType handlesType = (await Evaluate(catchStmt.TypeHint)).Type;

                    // compatible?
                    if (TypeChecker.IsCompatible(zenEx.Exception.Type, handlesType)) {
                        return await EvaluateCatchStatement(catchStmt, zenEx.Exception);
                    }
                }
            }

            Logger.Instance.Debug("No matching catch block found");
        } finally {
            // check if try has finally block, if so, execute it.
        }

        return VoidResult.Instance;
    }

    public async Task<IEvaluationResult> EvaluateCatchStatement(CatchStmt catchStmt, ZenValue exception)
    {
        // fork environment
        Environment blockEnv = new Environment(Environment);
        blockEnv.Define(false, catchStmt.Identifier.Name, exception.Type, false);
        blockEnv.Assign(catchStmt.Identifier.Name, exception);

        return await ExecuteBlock(catchStmt.Block, blockEnv);
    }

    public Task<IEvaluationResult> VisitAsync(CatchStmt catchStmt)
    {
        throw new NotImplementedException("This is not used.");
    }
}