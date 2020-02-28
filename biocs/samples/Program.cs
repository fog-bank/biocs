using System.Threading.Tasks;
using ConsoleAppFramework;
using Microsoft.Extensions.Hosting;

namespace Biocs
{
    partial class Program : ConsoleAppBase
    {
        static async Task Main(string[] args) => await Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<Program>(args);
    }
}
