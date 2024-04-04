using Microsoft.Extensions.Logging;

namespace Biocs;

/// <summary>
/// Sends log output to <see cref="Console.Error"/>.
/// </summary>
public class ConsoleErrorLogger : ILogger
{
    public virtual void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        string message = formatter(state, exception);

        if (!string.IsNullOrEmpty(message))
            Console.Error.WriteLine($"[{DateTime.Now}] {LogLevelToString(logLevel)}: {message}");

        if (exception != null)
            Console.Error.WriteLine(exception);
    }

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

    protected static string LogLevelToString(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "Trace",
        LogLevel.Debug => "DEBUG",
        LogLevel.Information => "INFO",
        LogLevel.Warning => "WARN",
        LogLevel.Error => "ERROR",
        LogLevel.Critical => "CRITICAL",
        _ => logLevel.ToString()
    };
}

/// <summary>
/// A provider of a <see cref="ConsoleErrorLogger"/> instance.
/// </summary>
public sealed class ConsoleErrorLoggerProvider : ILoggerProvider
{
    private readonly ConsoleErrorLogger logger = new();

    public ILogger CreateLogger(string categoryName) => logger;

    public void Dispose()
    { }
}

internal sealed class NullScope : IDisposable
{
    public static NullScope Instance { get; } = new();

    public void Dispose()
    { }
}
