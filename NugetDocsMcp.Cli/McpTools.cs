using NugetDocsMcp.Core.Commands;
using NugetDocsMcp.Core.Interfaces;
using NugetDocsMcp.Core.Models;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace NugetDocsMcp.Cli
{
    /// <summary>
    /// MCP tools for NuGet documentation lookup.
    /// </summary>
    [McpServerToolType]
    public class McpTools
    {
        private readonly INuGetPackageLocator _locator;
        private readonly IXmlDocParser _parser;
        private readonly IReadmeProvider _readmeProvider;

        public McpTools(
            INuGetPackageLocator locator,
            IXmlDocParser parser,
            IReadmeProvider readmeProvider)
        {
            _locator = locator;
            _parser = parser;
            _readmeProvider = readmeProvider;
        }

        [McpServerTool(Name = "nd_readme")]
        [Description("Get the README for a NuGet package. If multiple versions are installed, check the project's .csproj to find which version is used.")]
        public async Task<string> GetReadmeAsync(
            [Description("The NuGet package name (e.g. 'Dapper')")]
            string packageName,
            [Description("Optional package version. Auto-resolves if only one version is installed.")]
            string? version,
            CancellationToken cancellationToken)
        {
            ReadmeCommand command = new ReadmeCommand(_locator, _readmeProvider, packageName, version);
            CommandResult result = await command.ExecuteAsync(cancellationToken);
            return FormatResult(result);
        }

        [McpServerTool(Name = "nd_search")]
        [Description("Search XML docs by member name. Returns full documentation for all matching members (overloads, params, returns, exceptions). Query is a case-insensitive contains match on the fully qualified name — e.g. 'Query', 'SqlMapper.Query', or 'Execute'. If multiple versions are installed, check the project's .csproj to find which version is used.")]
        public async Task<string> SearchAsync(
            [Description("The NuGet package name (e.g. 'Dapper')")]
            string packageName,
            [Description("Search query to match against member names (case-insensitive contains match)")]
            string query,
            [Description("Optional package version. Auto-resolves if only one version is installed.")]
            string? version,
            [Description("Optional target framework (e.g. 'net8.0'). Auto-resolves if only one exists.")]
            string? targetFramework,
            CancellationToken cancellationToken)
        {
            SearchCommand command = new SearchCommand(_locator, _parser, packageName, query, version, targetFramework);
            CommandResult result = await command.ExecuteAsync(cancellationToken);
            return FormatResult(result);
        }

        [McpServerTool(Name = "nd_types")]
        [Description("List all types in a NuGet package (just names, no details). Useful for browsing what's available. If multiple versions are installed, check the project's .csproj to find which version is used.")]
        public async Task<string> ListTypesAsync(
            [Description("The NuGet package name (e.g. 'Dapper')")]
            string packageName,
            [Description("Optional package version. Auto-resolves if only one version is installed.")]
            string? version,
            [Description("Optional target framework (e.g. 'net8.0'). Auto-resolves if only one exists.")]
            string? targetFramework,
            CancellationToken cancellationToken)
        {
            TypesCommand command = new TypesCommand(_locator, _parser, packageName, version, targetFramework);
            CommandResult result = await command.ExecuteAsync(cancellationToken);
            return FormatResult(result);
        }

        [McpServerTool(Name = "nd_help")]
        [Description("Show usage information for all NuGet Docs MCP tools.")]
        public async Task<string> GetHelpAsync(CancellationToken cancellationToken)
        {
            HelpCommand command = new HelpCommand();
            CommandResult result = await command.ExecuteAsync(cancellationToken);
            return FormatResult(result);
        }

        private static string FormatResult(CommandResult result)
        {
            if (string.IsNullOrEmpty(result.Details))
            {
                return result.Message;
            }

            return $"{result.Message}\n\n{result.Details}";
        }
    }
}
