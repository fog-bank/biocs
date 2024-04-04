using Microsoft.Extensions.Logging;

namespace Biocs;

/// <summary>
/// Sends log output to <see cref="Console.Error"/> with changing console color.
/// </summary>
/// <remarks>When the input and/or output are redirected, the color system will not work as expected.</remarks>
public class ConsoleErrorColorLogger : ConsoleErrorLogger
{
    public override void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        string message = formatter(state, exception);
        var stderr = Console.Error;

        if (!string.IsNullOrEmpty(message))
        {
            stderr.Write('[');
            stderr.Write(DateTime.Now);
            stderr.Write("] ");
            Write(stderr, LogLevelToString(logLevel), GetColors(logLevel));
            stderr.Write(": ");
            stderr.WriteLine(message);
        }

        if (exception != null)
        {
            Write(stderr, exception.GetType().ToString(), GetColors(LogLevel.Critical));
            stderr.Write(": ");
            stderr.WriteLine(exception.Message);

            if (exception.StackTrace != null)
            {
                Write(stderr, exception.StackTrace, GetColors(LogLevel.Warning));
                stderr.WriteLine();
            }
        }
        stderr.Flush();
    }

    private static (ConsoleColor, ConsoleColor) GetColors(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => (ConsoleColor.Gray, ConsoleColor.Black),
        LogLevel.Debug => (ConsoleColor.Gray, ConsoleColor.Black),
        LogLevel.Information => (ConsoleColor.DarkGreen, ConsoleColor.Black),
        LogLevel.Warning => (ConsoleColor.Yellow, ConsoleColor.Black),
        LogLevel.Error => (ConsoleColor.Black, ConsoleColor.Red),
        LogLevel.Critical => (ConsoleColor.White, ConsoleColor.Red),
        _ => (ConsoleColor.White, ConsoleColor.Black),
    };

    private static void Write(TextWriter writer, string message, (ConsoleColor, ConsoleColor) colors)
    {
        Console.ForegroundColor = colors.Item1;
        Console.BackgroundColor = colors.Item2;
        writer.Write(message);
        Console.ResetColor();
    }
}

/// <summary>
/// A provider of a <see cref="ConsoleErrorColorLogger"/> instance.
/// </summary>
public sealed class ConsoleErrorColorLoggerProvider : ILoggerProvider
{
    private readonly ConsoleErrorColorLogger logger = new();

    public ILogger CreateLogger(string categoryName) => logger;

    public void Dispose()
    { }
}
