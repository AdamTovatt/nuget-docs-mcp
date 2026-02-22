using NugetDocsMcp.Core.Interfaces;

namespace NugetDocsMcp.Core.Commands
{
    /// <summary>
    /// Command to get the README for a NuGet package.
    /// </summary>
    public class ReadmeCommand : ICommand
    {
        private readonly INuGetPackageLocator _locator;
        private readonly IReadmeProvider _readmeProvider;
        private readonly string _packageName;
        private readonly string? _version;

        public ReadmeCommand(
            INuGetPackageLocator locator,
            IReadmeProvider readmeProvider,
            string packageName,
            string? version)
        {
            _locator = locator;
            _readmeProvider = readmeProvider;
            _packageName = packageName;
            _version = version;
        }

        /// <inheritdoc/>
        public async Task<CommandResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Find package directory
                string? packageDir = await _locator.FindPackageDirectoryAsync(_packageName, cancellationToken);
                if (packageDir == null)
                {
                    return new CommandResult(false, $"Package '{_packageName}' not found in local NuGet cache.");
                }

                // Resolve version
                List<string> versions = await _locator.GetVersionsAsync(_packageName, cancellationToken);
                if (versions.Count == 0)
                {
                    return new CommandResult(false, $"No versions found for package '{_packageName}'.");
                }

                string version;
                if (_version != null)
                {
                    if (!versions.Contains(_version))
                    {
                        return new CommandResult(false, $"Version '{_version}' not found for package '{_packageName}'.",
                            $"Available versions:\n{string.Join("\n", versions.Select(v => $"  {v}"))}");
                    }
                    version = _version;
                }
                else if (versions.Count == 1)
                {
                    version = versions[0];
                }
                else
                {
                    return new CommandResult(false, $"Multiple versions available for '{_packageName}'. Please specify --version.",
                        $"Available versions:\n{string.Join("\n", versions.Select(v => $"  {v}"))}");
                }

                // Get readme
                string versionedPath = Path.Combine(packageDir, version);
                string? readme = await _readmeProvider.GetReadmeAsync(versionedPath, cancellationToken);

                if (readme == null)
                {
                    return new CommandResult(false, $"No README.md found for {_packageName} v{version}.");
                }

                return new CommandResult(true, $"README for {_packageName} v{version}:", readme);
            }
            catch (Exception ex)
            {
                return new CommandResult(false, $"Error: {ex.Message}");
            }
        }
    }
}
