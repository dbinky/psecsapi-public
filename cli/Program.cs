using Microsoft.Extensions.DependencyInjection;
using psecsapi.Console.Commands;
using psecsapi.Console.Infrastructure.DependencyInjection;
using System.CommandLine;

namespace psecsapi.Console
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            // Build service provider with all dependencies
            var services = new ServiceCollection()
                .AddConfiguration()
                .AddHttpClients()
                .AddCommands()
                .BuildServiceProvider();

            // Build root command and register all discovered commands
            var rootCommand = new RootCommand("PSECSAPI Command Line Interface");

            foreach (var command in services.GetServices<ICommand>())
            {
                rootCommand.AddCommand(command.Build());
            }

            return await rootCommand.InvokeAsync(args);
        }
    }
}
