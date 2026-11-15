using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NugetDocsMcp.Cli;

namespace NugetDocsMcp.Tests.Cli
{
    public class McpHostBuilderTests
    {
        [Fact]
        public void CreateMcpHostBuilder_RegistersNoReloadingFileConfiguration()
        {
            HostApplicationBuilder builder = Program.CreateMcpHostBuilder();
            IConfigurationBuilder configuration = builder.Configuration;

            // A reloading file source (appsettings.json in the default builder) watches the whole
            // working directory recursively, one inotify watch per folder.
            List<FileConfigurationSource> reloadingFileSources = configuration.Sources
                .OfType<FileConfigurationSource>()
                .Where(source => source.ReloadOnChange)
                .ToList();

            Assert.Empty(reloadingFileSources);
        }
    }
}
