using System;
using System.Threading.Tasks;
using ConsoleAppFramework;
using Microsoft.Extensions.Logging;

namespace Biocs
{
    public class LogCommandInterceptor : IConsoleAppInterceptor
    {
        public ValueTask OnEngineBeginAsync(IServiceProvider serviceProvider, ILogger<ConsoleAppEngine> logger)
        {
            return default;
        }

        public ValueTask OnMethodBeginAsync(ConsoleAppContext context)
        {
            return default;
        }

        public ValueTask OnMethodEndAsync()
        {
            return default;
        }

        public ValueTask OnEngineCompleteAsync(ConsoleAppContext context, string? errorMessageIfFailed, Exception? exceptionIfExists)
        {
            context.Logger.LogInformation("Elapsed time: {time}, Command: {command}",
                DateTimeOffset.UtcNow - context.Timestamp, string.Join(" ", context.Arguments));
            return default;
        }
    }
}
