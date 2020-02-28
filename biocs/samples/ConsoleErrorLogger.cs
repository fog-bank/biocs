using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Biocs
{
    /// <summary>
    /// Sends log output to <see cref="Console.Error"/>.
    /// </summary>
    public class ConsoleErrorLogger : ILogger
    {
        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string message = formatter.Invoke(state, exception);
            var stderr = Console.Error;

            if (!string.IsNullOrEmpty(message))
            {
                stderr.Write('[');
                stderr.Write(DateTime.Now);
                stderr.Write("] ");
                WriteLogLevel(stderr, logLevel);
                stderr.Write(": ");
                stderr.WriteLine(message);
            }

            if (exception != null)
            {
                Write(stderr, exception.GetType().ToString(), ConsoleColor.White, ConsoleColor.Red);
                stderr.Write(": ");
                stderr.WriteLine(exception.Message);

                if (exception.StackTrace != null)
                {
                    Write(stderr, exception.StackTrace, ConsoleColor.Yellow, ConsoleColor.Black);
                    stderr.WriteLine();
                }
            }
            stderr.Flush();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NullScope.Instance;
        }

        private static void WriteLogLevel(TextWriter writer, LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    Write(writer, "TRACE", ConsoleColor.Gray, ConsoleColor.Black);
                    break;

                case LogLevel.Debug:
                    Write(writer, "DEBUG", ConsoleColor.Gray, ConsoleColor.Black);
                    break;

                case LogLevel.Information:
                    Write(writer, "INFO", ConsoleColor.DarkGreen, ConsoleColor.Black);
                    break;

                case LogLevel.Warning:
                    Write(writer, "WARN", ConsoleColor.Yellow, ConsoleColor.Black);
                    break;

                case LogLevel.Error:
                    Write(writer, "ERROR", ConsoleColor.Black, ConsoleColor.Red);
                    break;

                case LogLevel.Critical:
                    Write(writer, "CRITICAL", ConsoleColor.White, ConsoleColor.Red);
                    break;

                default:
                    writer.Write(logLevel);
                    break;
            }
        }

        private static void Write(TextWriter writer, string message, ConsoleColor foreground, ConsoleColor background)
        {
            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;
            writer.Write(message);
            Console.ResetColor();
        }
    }

    /// <summary>
    /// A provider of a <see cref="ConsoleErrorLogger"/> instance.
    /// </summary>
    public class ConsoleErrorLoggerProvider : ILoggerProvider
    {
        private readonly ConsoleErrorLogger logger = new ConsoleErrorLogger();

        public ILogger CreateLogger(string categoryName)
        {
            return logger;
        }

        public void Dispose()
        { }
    }

    internal class NullScope : IDisposable
    {
        public static IDisposable Instance { get; } = new NullScope();

        public void Dispose()
        { }
    }
}
