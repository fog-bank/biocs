using Biocs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = ConsoleApp.CreateBuilder(args, options =>
{
    options.GlobalFilters = new[] { new LogRunningTimeFilter() };
    options.HelpSortCommandsByFullName = true;
});

builder.ConfigureLogging(builder =>
{
    builder.ClearProviders();
    builder.Services.AddSingleton<ILoggerProvider, ConsoleErrorColorLoggerProvider>();
});

var app = builder.Build();
app.AddCommands<Bgzf>();
app.Run();
