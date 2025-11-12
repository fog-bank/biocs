using System.Diagnostics;

namespace Biocs;

internal class LogRunningTimeFilter(ConsoleAppFilter next, ILogger<Program> logger) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        long timestamp = Stopwatch.GetTimestamp();
        try
        {
            await Next.InvokeAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "The following error occurred.");
        }
        finally
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Elapsed time: {ElapsedTime}, Command: {Command}",
                    Stopwatch.GetElapsedTime(timestamp), string.Join(' ', context.Arguments));
            }
        }
    }
}
