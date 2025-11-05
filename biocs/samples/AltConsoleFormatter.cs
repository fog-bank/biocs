using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Biocs;

// https://learn.microsoft.com/en-us/dotnet/core/extensions/console-log-formatter
public sealed class AltConsoleFormatter : ConsoleFormatter, IDisposable
{
    public const string FormatterName = "alt";

    public AltConsoleFormatter(IOptionsMonitor<AltConsoleFormatterOptions> options) : base(FormatterName)
    {
        OptionsReloadToken = options.OnChange(OptionsOnChange);
        FormatterOptions = options.CurrentValue;
    }

    private AltConsoleFormatterOptions FormatterOptions { get; set; }

    private IDisposable? OptionsReloadToken { get; }

    public sealed override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        string? message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);

        if (message == null)
            return;

        if (FormatterOptions.TimestampFormat != null)
            WriteTimestamp(textWriter);

        if (FormatterOptions.IncludeScopes)
            WriteScope(textWriter, scopeProvider);

        WriteLogLevel(textWriter, logEntry.LogLevel);
        textWriter.WriteLine(message);

        if (logEntry.Exception != null)
            WriteException(textWriter, logEntry.Exception);
    }

    public void Dispose() => OptionsReloadToken?.Dispose();

    private void WriteTimestamp(TextWriter writer)
    {
        var timestamp = FormatterOptions.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
        string message = timestamp.ToString(FormatterOptions.TimestampFormat);

        writer.Write('[');
        WriteWithColor(writer, message, LogLevel.Information);
        writer.Write("] ");
    }

    private void OptionsOnChange(AltConsoleFormatterOptions options) => FormatterOptions = options;

    private static void WriteScope(TextWriter writer, IExternalScopeProvider? scopeProvider)
    {
        scopeProvider?.ForEachScope(static (scope, state) =>
        {
            state.Write('[');
            state.Write(scope);
            state.Write("] ");
        }, writer);
    }

    private static void WriteLogLevel(TextWriter writer, LogLevel logLevel)
    {
        WriteWithColor(writer, LogLevelToString(logLevel), logLevel);
        writer.Write(": ");
    }

    private static void WriteException(TextWriter writer, Exception exception)
    {
        WriteWithColor(writer, exception.GetType().ToString(), ConsoleColor.Red, ConsoleColor.Black);
        writer.Write(": ");
        WriteWithColor(writer, exception.Message, LogLevel.Warning);
        writer.WriteLine();

        if (exception.StackTrace != null)
        {
            WriteWithColor(writer, exception.StackTrace, LogLevel.Debug);
            writer.WriteLine();
        }
    }

    private static void WriteWithColor(TextWriter writer, string message, LogLevel logLevel)
    {
        (var foreground, var background) = GetColors(logLevel);
        WriteWithColor(writer, message, foreground, background);
    }

    private static void WriteWithColor(TextWriter writer, string message, ConsoleColor foreground, ConsoleColor background)
    {
        bool redirect = Console.IsErrorRedirected || Console.IsOutputRedirected;

        if (!redirect)
        {
            writer.Write(AnsiEscape.BackgroundToString(background));
            writer.Write(AnsiEscape.ForegroundToString(foreground));
        }
        writer.Write(message);

        if (!redirect)
        {
            writer.Write(AnsiEscape.ForegroundToString(AnsiEscape.DefaultColor));
            writer.Write(AnsiEscape.BackgroundToString(AnsiEscape.DefaultColor));
        }
    }

    private static string LogLevelToString(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "Trace",
        LogLevel.Debug => "DEBUG",
        LogLevel.Information => "INFO",
        LogLevel.Warning => "WARN",
        LogLevel.Error => "ERROR",
        LogLevel.Critical => "CRITICAL",
        _ => logLevel.ToString()
    };

    private static (ConsoleColor foreground, ConsoleColor background) GetColors(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => (ConsoleColor.Gray, ConsoleColor.Black),
        LogLevel.Debug => (ConsoleColor.Gray, ConsoleColor.Black),
        LogLevel.Information => (ConsoleColor.DarkGreen, ConsoleColor.Black),
        LogLevel.Warning => (ConsoleColor.Yellow, ConsoleColor.Black),
        LogLevel.Error => (ConsoleColor.Black, ConsoleColor.Red),
        LogLevel.Critical => (ConsoleColor.White, ConsoleColor.Red),
        _ => (ConsoleColor.White, ConsoleColor.Black),
    };
}

public sealed class AltConsoleFormatterOptions : ConsoleFormatterOptions
{ }

// https://en.wikipedia.org/wiki/ANSI_escape_code
internal static class AnsiEscape
{
    public const ConsoleColor DefaultColor = ConsoleColor.DarkGray;

    public static string ForegroundToString(ConsoleColor color) => color switch
    {
        // 30-37: 3-bit color, 38: RGB, 39: default, 90-97: bright color
        ConsoleColor.Black => "\e[30m",
        ConsoleColor.DarkRed => "\e[31m",
        ConsoleColor.DarkGreen => "\e[32m",
        ConsoleColor.DarkYellow => "\e[33m",
        ConsoleColor.DarkBlue => "\e[34m",
        ConsoleColor.DarkMagenta => "\e[35m",
        ConsoleColor.DarkCyan => "\e[36m",
        ConsoleColor.Gray => "\e[90m",
        ConsoleColor.Red => "\e[91m",
        ConsoleColor.Green => "\e[92m",
        ConsoleColor.Yellow => "\e[93m",
        ConsoleColor.Blue => "\e[94m",
        ConsoleColor.Magenta => "\e[95m",
        ConsoleColor.Cyan => "\e[96m",
        ConsoleColor.White => "\e[97m",
        _ => "\e[39m"
    };

    public static string BackgroundToString(ConsoleColor color) => color switch
    {
        // 40-47: 3-bit color, 48: RGB, 49: reset, 100-107: bright color
        ConsoleColor.Black => "\e[40m",
        ConsoleColor.DarkRed => "\e[41m",
        ConsoleColor.DarkGreen => "\e[42m",
        ConsoleColor.DarkYellow => "\e[43m",
        ConsoleColor.DarkBlue => "\e[44m",
        ConsoleColor.DarkMagenta => "\e[45m",
        ConsoleColor.DarkCyan => "\e[46m",
        ConsoleColor.Gray => "\e[100m",
        ConsoleColor.Red => "\e[101m",
        ConsoleColor.Green => "\e[102m",
        ConsoleColor.Yellow => "\e[103m",
        ConsoleColor.Blue => "\e[104m",
        ConsoleColor.Magenta => "\e[105m",
        ConsoleColor.Cyan => "\e[106m",
        ConsoleColor.White => "\e[107m",
        _ => "\e[49m"
    };
}
