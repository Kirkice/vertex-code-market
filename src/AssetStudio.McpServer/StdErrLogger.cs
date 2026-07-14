using Microsoft.Extensions.Logging;

namespace AssetStudio.McpServer;

/// <summary>
/// Logger provider that writes to stderr, keeping stdout clean for MCP stdio protocol.
/// </summary>
internal class StdErrLoggerProvider : ILoggerProvider
{
    public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
    {
        return new StdErrLogger(categoryName);
    }

    public void Dispose() { }
}

internal class StdErrLogger : Microsoft.Extensions.Logging.ILogger
{
    private readonly string _categoryName;

    public StdErrLogger(string categoryName)
    {
        _categoryName = categoryName;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);
        Console.Error.WriteLine($"[{logLevel}] {_categoryName}: {message}");
        if (exception != null)
        {
            Console.Error.WriteLine(exception.ToString());
        }
    }
}
