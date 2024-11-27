namespace Zen.Common;

public class Logger
{
    private static Logger? _instance;
    private Action<string>? _output;
    private bool _debugEnabled = false;

    private Logger() {}

    public static Logger Instance
    {
        get
        {
            _instance ??= new Logger();
            return _instance;
        }
    }

    /// <summary>
    /// Configure where log output should go
    /// </summary>
    public void SetOutput(Action<string> output)
    {
        _output = output;
    }

    /// <summary>
    /// Enable or disable debug logging
    /// </summary>
    public void SetDebug(bool enabled)
    {
        _debugEnabled = enabled;
    }

    private void Log(string level, string message)
    {
        var formattedMessage = $"[{level}] {message}";
        if (_output != null)
        {
            _output(formattedMessage);
        }
        else
        {
            Console.WriteLine(formattedMessage);
        }
    }

    /// <summary>
    /// Log a debug message
    /// </summary>
    public void Debug(string message)
    {
        if (!_debugEnabled) return;
        Log("DEBUG", message);
    }

    /// <summary>
    /// Log an info message
    /// </summary>
    public void Info(string message)
    {
        Log("INFO", message);
    }

    /// <summary>
    /// Log a warning message
    /// </summary>
    public void Warning(string message)
    {
        Log("WARN", message);
    }

    /// <summary>
    /// Log an error message
    /// </summary>
    public void Error(string message)
    {
        Log("ERROR", message);
    }

    /// <summary>
    /// Reset the logger (mainly for testing)
    /// </summary>
    public static void Reset()
    {
        _instance = null;
    }
}
