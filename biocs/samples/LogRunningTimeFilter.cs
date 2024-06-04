using System.Diagnostics;

namespace Biocs;

internal class LogRunningTimeFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        long timestamp = Stopwatch.GetTimestamp();
        try
        {
            await Next.InvokeAsync(context, cancellationToken);
        }
        finally
        {
            ConsoleApp.Log($"Elapsed time: {Stopwatch.GetElapsedTime(timestamp)}, Command: {string.Join(" ", context.Arguments)}");
        }
    }
}
