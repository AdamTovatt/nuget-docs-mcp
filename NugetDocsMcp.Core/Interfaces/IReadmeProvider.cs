namespace NugetDocsMcp.Core.Interfaces
{
    /// <summary>
    /// Provides access to README files in NuGet packages.
    /// </summary>
    public interface IReadmeProvider
    {
        /// <summary>
        /// Finds and reads a README.md file (case-insensitive) in the versioned package directory.
        /// </summary>
        Task<string?> GetReadmeAsync(string versionedPackagePath, CancellationToken cancellationToken);
    }
}
