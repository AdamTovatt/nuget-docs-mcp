using NugetDocsMcp.Core.Interfaces;

namespace NugetDocsMcp.Core.Commands
{
    /// <summary>
    /// Command that displays help information.
    /// </summary>
    public class HelpCommand : ICommand
    {
        /// <inheritdoc/>
        public Task<CommandResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            string help = @"NuGet Docs MCP - Find documentation for locally-installed NuGet packages.

Usage: nd <command> [options]

Commands:
  readme <package> [--version <v>]
    Get the README for a NuGet package.

  search <package> <query> [--version <v>] [--framework <f>]
    Search XML docs by member name. Query matches case-insensitively
    against the member name (e.g. ""GetProviderForResource"" or
    ""ResourceManager.GetProviderForResource"").

  types <package> [--version <v>] [--framework <f>]
    List all types in a package (just names, no details).

  help
    Show this help message.

Options:
  --version <v>      Specify package version (auto-resolves if only one exists)
  --framework <f>    Specify target framework (auto-resolves if only one exists)

MCP Mode:
  nd --mcp           Start as an MCP server (stdio transport)

Examples:
  nd readme Dapper
  nd types Dapper --version 2.1.35 --framework net8.0
  nd search Dapper Query
  nd search Dapper ""SqlMapper.Query""";

            return Task.FromResult(new CommandResult(true, help));
        }
    }
}
