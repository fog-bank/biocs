namespace Biocs;

internal class LogInjectionFilter(ConsoleAppFilter next, ILogger<Program> logger) : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA2254 // Template should be a static expression
        ConsoleApp.Log = message => logger.LogInformation(message);
        ConsoleApp.LogError = message => logger.LogError(message);
#pragma warning restore CA2254,IDE0079 // Template should be a static expression

        return Next.InvokeAsync(context, cancellationToken);
    }
}
