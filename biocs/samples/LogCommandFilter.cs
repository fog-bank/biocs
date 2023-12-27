using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Biocs;

public class LogCommandFilter : ConsoleAppFilter
{
    public override async ValueTask Invoke(ConsoleAppContext context, Func<ConsoleAppContext, ValueTask> next)
    {
        await next(context);
        context.Logger.LogInformation("Elapsed time: {time}, Command: {command}",
            DateTimeOffset.UtcNow - context.Timestamp, string.Join(" ", context.Arguments));
    }
}
