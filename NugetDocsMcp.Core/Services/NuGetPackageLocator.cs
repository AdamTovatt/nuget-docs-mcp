using System.Diagnostics;
using NugetDocsMcp.Core.Interfaces;

namespace NugetDocsMcp.Core.Services
{
    /// <summary>
    /// Locates NuGet packages in the global packages cache.
    /// </summary>
    public class NuGetPackageLocator : INuGetPackageLocator
    {
        private string? _cachedGlobalPackagesPath;

        /// <inheritdoc/>
        public async Task<string> GetGlobalPackagesPathAsync(CancellationToken cancellationToken)
        {
            if (_cachedGlobalPackagesPath != null)
            {
                return _cachedGlobalPackagesPath;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "nuget locals global-packages --list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = new Process { StartInfo = startInfo };
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException("Failed to get global packages path from dotnet CLI.");
            }

            // Output format: "global-packages: C:\Users\...\.nuget\packages\"
            string trimmed = output.Trim();
            const string prefix = "global-packages: ";
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring(prefix.Length).Trim();
            }

            _cachedGlobalPackagesPath = trimmed;
            return _cachedGlobalPackagesPath;
        }

        /// <inheritdoc/>
        public async Task<string?> FindPackageDirectoryAsync(string packageName, CancellationToken cancellationToken)
        {
            string globalPath = await GetGlobalPackagesPathAsync(cancellationToken);

            if (!Directory.Exists(globalPath))
            {
                return null;
            }

            foreach (string directory in Directory.EnumerateDirectories(globalPath))
            {
                string folderName = Path.GetFileName(directory);
                if (folderName.Equals(packageName, StringComparison.OrdinalIgnoreCase))
                {
                    return directory;
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetVersionsAsync(string packageName, CancellationToken cancellationToken)
        {
            string? packageDir = await FindPackageDirectoryAsync(packageName, cancellationToken);
            if (packageDir == null || !Directory.Exists(packageDir))
            {
                return new List<string>();
            }

            return Directory.EnumerateDirectories(packageDir)
                .Select(Path.GetFileName)
                .Where(name => name != null)
                .Select(name => name!)
                .OrderBy(v => v)
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetFrameworksAsync(string packageName, string version, CancellationToken cancellationToken)
        {
            string? packageDir = await FindPackageDirectoryAsync(packageName, cancellationToken);
            if (packageDir == null)
            {
                return new List<string>();
            }

            string libPath = Path.Combine(packageDir, version, "lib");
            if (!Directory.Exists(libPath))
            {
                return new List<string>();
            }

            return Directory.EnumerateDirectories(libPath)
                .Select(Path.GetFileName)
                .Where(name => name != null)
                .Select(name => name!)
                .OrderBy(f => f)
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<string?> GetXmlDocPathAsync(string packageName, string version, string framework, CancellationToken cancellationToken)
        {
            string? packageDir = await FindPackageDirectoryAsync(packageName, cancellationToken);
            if (packageDir == null)
            {
                return null;
            }

            string frameworkPath = Path.Combine(packageDir, version, "lib", framework);
            if (!Directory.Exists(frameworkPath))
            {
                return null;
            }

            return Directory.EnumerateFiles(frameworkPath, "*.xml").FirstOrDefault();
        }
    }
}
