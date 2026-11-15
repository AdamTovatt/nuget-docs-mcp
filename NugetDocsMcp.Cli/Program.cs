using NugetDocsMcp.Core.Interfaces;
using NugetDocsMcp.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NugetDocsMcp.Cli
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            bool isMcpMode = args.Length > 0 &&
                (args[0].Equals("--mcp", StringComparison.OrdinalIgnoreCase) ||
                 args[0].Equals("mcp", StringComparison.OrdinalIgnoreCase));

            if (isMcpMode)
            {
                return await RunMcpServerAsync();
            }
            else
            {
                return await RunCliAsync(args);
            }
        }

        private static async Task<int> RunCliAsync(string[] args)
        {
            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider serviceProvider = services.BuildServiceProvider();

            try
            {
                ICommandFactory commandFactory = serviceProvider.GetRequiredService<ICommandFactory>();
                ICommand command = commandFactory.CreateCommand(args);
                CommandResult result = await command.ExecuteAsync(CancellationToken.None);

                Console.WriteLine(result.Message);
                if (!string.IsNullOrEmpty(result.Details))
                {
                    Console.WriteLine(result.Details);
                }

                return result.Success ? 0 : 1;
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunMcpServerAsync()
        {
            try
            {
                IHost host = CreateMcpHostBuilder().Build();
                await host.RunAsync();

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"MCP Server error: {ex.Message}");
                return 1;
            }
        }

        internal static HostApplicationBuilder CreateMcpHostBuilder()
        {
            // Empty builder: the default one loads appsettings.json with reload-on-change, which
            // watches the whole working directory recursively (one inotify watch per folder).
            HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(settings: null);

            builder.Logging.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = LogLevel.Trace;
            });

            ConfigureServices(builder.Services);

            builder.Services
                .AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly();

            builder.Services.AddSingleton<McpTools>();

            return builder;
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<INuGetPackageLocator, NuGetPackageLocator>();
            services.AddSingleton<IXmlDocParser, XmlDocParser>();
            services.AddSingleton<IReadmeProvider, ReadmeProvider>();
            services.AddSingleton<ICommandFactory, CommandFactory>();
        }
    }
}
