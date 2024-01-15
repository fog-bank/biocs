using Microsoft.Extensions.Logging;

namespace Biocs;

public class LogRunningTimeFilter : ConsoleAppFilter
{
    public override async ValueTask Invoke(ConsoleAppContext context, Func<ConsoleAppContext, ValueTask> next)
    {
        try
        {
            await next(context);
        }
        finally
        {
            context.Logger.LogInformation("Elapsed time: {time}, Command: {command}",
                DateTimeOffset.UtcNow - context.Timestamp, string.Join(" ", context.Arguments));
        }
    }
}
