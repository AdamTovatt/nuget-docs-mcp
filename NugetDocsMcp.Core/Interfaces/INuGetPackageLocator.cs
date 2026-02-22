namespace NugetDocsMcp.Core.Interfaces
{
    /// <summary>
    /// Locates NuGet packages in the global packages cache.
    /// </summary>
    public interface INuGetPackageLocator
    {
        /// <summary>
        /// Gets the global packages path by running dotnet nuget locals command.
        /// </summary>
        Task<string> GetGlobalPackagesPathAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Finds the package directory by case-insensitive folder name match.
        /// </summary>
        Task<string?> FindPackageDirectoryAsync(string packageName, CancellationToken cancellationToken);

        /// <summary>
        /// Lists available version folders for a package.
        /// </summary>
        Task<List<string>> GetVersionsAsync(string packageName, CancellationToken cancellationToken);

        /// <summary>
        /// Lists available target framework folders in lib/ for a specific package version.
        /// </summary>
        Task<List<string>> GetFrameworksAsync(string packageName, string version, CancellationToken cancellationToken);

        /// <summary>
        /// Finds the XML documentation file path in lib/{framework}/.
        /// </summary>
        Task<string?> GetXmlDocPathAsync(string packageName, string version, string framework, CancellationToken cancellationToken);
    }
}
