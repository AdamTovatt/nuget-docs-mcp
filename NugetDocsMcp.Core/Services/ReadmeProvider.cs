using NugetDocsMcp.Core.Interfaces;

namespace NugetDocsMcp.Core.Services
{
    /// <summary>
    /// Provides access to README files in NuGet packages.
    /// </summary>
    public class ReadmeProvider : IReadmeProvider
    {
        /// <inheritdoc/>
        public async Task<string?> GetReadmeAsync(string versionedPackagePath, CancellationToken cancellationToken)
        {
            if (!Directory.Exists(versionedPackagePath))
            {
                return null;
            }

            // Case-insensitive search for readme.md
            string? readmePath = Directory.EnumerateFiles(versionedPackagePath)
                .FirstOrDefault(f => Path.GetFileName(f).Equals("readme.md", StringComparison.OrdinalIgnoreCase));

            if (readmePath == null)
            {
                return null;
            }

            return await File.ReadAllTextAsync(readmePath, cancellationToken);
        }
    }
}
