using Biocs;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.Services.AddSingleton<ILoggerProvider, ConsoleErrorColorLoggerProvider>();
});

using var serviceProvider = services.BuildServiceProvider();
ConsoleApp.ServiceProvider = serviceProvider;

//var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
ConsoleApp.Log = Console.Error.WriteLine;

var app = ConsoleApp.Create();
app.UseFilter<LogRunningTimeFilter>();
app.Add<Bgzf>();
app.Run(args);
