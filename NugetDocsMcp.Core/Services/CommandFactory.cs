using NugetDocsMcp.Core.Commands;
using NugetDocsMcp.Core.Interfaces;

namespace NugetDocsMcp.Core.Services
{
    /// <summary>
    /// Factory for creating command instances from CLI arguments.
    /// </summary>
    public class CommandFactory : ICommandFactory
    {
        private readonly INuGetPackageLocator _locator;
        private readonly IXmlDocParser _parser;
        private readonly IReadmeProvider _readmeProvider;

        public CommandFactory(
            INuGetPackageLocator locator,
            IXmlDocParser parser,
            IReadmeProvider readmeProvider)
        {
            _locator = locator;
            _parser = parser;
            _readmeProvider = readmeProvider;
        }

        /// <inheritdoc/>
        public ICommand CreateCommand(string[] args)
        {
            if (args.Length == 0)
            {
                return new HelpCommand();
            }

            string commandName = args[0].ToLowerInvariant();

            return commandName switch
            {
                "readme" => CreateReadmeCommand(args),
                "search" => CreateSearchCommand(args),
                "types" => CreateTypesCommand(args),
                "help" or "--help" or "-h" => new HelpCommand(),
                _ => throw new ArgumentException(
                    $"Unknown command: '{commandName}'. Use 'nd help' to see available commands.")
            };
        }

        private ICommand CreateReadmeCommand(string[] args)
        {
            // nd readme <package> [--version <v>]
            if (args.Length < 2)
            {
                throw new ArgumentException("Readme command requires a package name. Usage: nd readme <package> [--version <v>]");
            }

            string packageName = args[1];
            string? version = GetOptionValue(args, "--version", 2);

            return new ReadmeCommand(_locator, _readmeProvider, packageName, version);
        }

        private ICommand CreateSearchCommand(string[] args)
        {
            // nd search <package> <query> [--version <v>] [--framework <f>]
            if (args.Length < 3)
            {
                throw new ArgumentException("Search command requires a package name and query. Usage: nd search <package> <query> [--version <v>] [--framework <f>]");
            }

            string packageName = args[1];
            string query = args[2];
            string? version = GetOptionValue(args, "--version", 3);
            string? framework = GetOptionValue(args, "--framework", 3);

            return new SearchCommand(_locator, _parser, packageName, query, version, framework);
        }

        private ICommand CreateTypesCommand(string[] args)
        {
            // nd types <package> [--version <v>] [--framework <f>]
            if (args.Length < 2)
            {
                throw new ArgumentException("Types command requires a package name. Usage: nd types <package> [--version <v>] [--framework <f>]");
            }

            string packageName = args[1];
            string? version = GetOptionValue(args, "--version", 2);
            string? framework = GetOptionValue(args, "--framework", 2);

            return new TypesCommand(_locator, _parser, packageName, version, framework);
        }

        private static string? GetOptionValue(string[] args, string optionName, int startIndex)
        {
            for (int i = startIndex; i < args.Length - 1; i++)
            {
                if (args[i].Equals(optionName, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
