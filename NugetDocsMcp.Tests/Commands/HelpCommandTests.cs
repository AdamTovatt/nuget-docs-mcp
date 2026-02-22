using NugetDocsMcp.Core.Commands;
using NugetDocsMcp.Core.Interfaces;

namespace NugetDocsMcp.Tests.Commands
{
    public class HelpCommandTests
    {
        [Fact]
        public async Task ExecuteAsync_ReturnsSuccess()
        {
            HelpCommand command = new HelpCommand();
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
        }

        [Fact]
        public async Task ExecuteAsync_ContainsAllCommands()
        {
            HelpCommand command = new HelpCommand();
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.Contains("readme", result.Message);
            Assert.Contains("search", result.Message);
            Assert.Contains("types", result.Message);
            Assert.Contains("help", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_ContainsMcpMode()
        {
            HelpCommand command = new HelpCommand();
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.Contains("--mcp", result.Message);
        }
    }
}
