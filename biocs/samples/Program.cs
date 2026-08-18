using Biocs;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddLogging(static builder =>
{
    builder.ClearProviders();
    builder.AddConsole(static options =>
    {
        options.FormatterName = AltConsoleFormatter.FormatterName;
        options.LogToStandardErrorThreshold = LogLevel.Trace;
    });
    builder.AddConsoleFormatter<AltConsoleFormatter, AltConsoleFormatterOptions>(static options =>
    {
        options.TimestampFormat = "G";
    });
    builder.SetMinimumLevel(LogLevel.Trace);
});
ConsoleApp.ServiceProvider = services.BuildServiceProvider();

var cts = new CancellationTokenSource();

var app = ConsoleApp.Create();
//app.UseFilter<LogInjectionFilter>();
ConsoleApp.Log = Console.Error.WriteLine;
app.UseFilter<LogRunningTimeFilter>();
app.Add<Bgzf>();
await app.RunAsync(args, cts.Token);
