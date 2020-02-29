using System.Threading.Tasks;
using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Biocs
{
    partial class Program : ConsoleAppBase
    {
        static async Task Main(string[] args) => await Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ConsoleErrorLoggerProvider>());
            })
            .RunConsoleAppFrameworkAsync<Program>(args, new LogCommandInterceptor());
    }
}
