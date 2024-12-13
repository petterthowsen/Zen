using Zen.Common;
using Zen.Execution;
using Zen.Typing;

namespace Zen.Exection;

// Rename this to ZenException
// and make it creatable from a ZenObject of type Exception
// this makes it essentially a throwable wrapper around a ZenObject

public class ZenException : Exception {

    public ZenValue Exception;
    public SourceLocation? Location;

    public ZenException(ZenValue zenException)
    {
        Exception = zenException;
    }

}