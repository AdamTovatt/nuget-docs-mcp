using NugetDocsMcp.Core.Services;

namespace NugetDocsMcp.Tests.Services
{
    public class NuGetPackageLocatorTests
    {
        private readonly NuGetPackageLocator _locator = new();

        [Fact]
        public async Task GetGlobalPackagesPathAsync_ReturnsValidPath()
        {
            string path = await _locator.GetGlobalPackagesPathAsync(CancellationToken.None);

            Assert.False(string.IsNullOrWhiteSpace(path));
            Assert.True(Directory.Exists(path), $"Global packages path does not exist: {path}");
        }

        [Fact]
        public async Task GetGlobalPackagesPathAsync_CachesResult()
        {
            string path1 = await _locator.GetGlobalPackagesPathAsync(CancellationToken.None);
            string path2 = await _locator.GetGlobalPackagesPathAsync(CancellationToken.None);

            Assert.Equal(path1, path2);
        }

        [Fact]
        public async Task FindPackageDirectoryAsync_ReturnsNullForNonExistentPackage()
        {
            string? result = await _locator.FindPackageDirectoryAsync(
                "NonExistentPackage12345xyz", CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetVersionsAsync_ReturnsEmptyForNonExistentPackage()
        {
            List<string> versions = await _locator.GetVersionsAsync(
                "NonExistentPackage12345xyz", CancellationToken.None);

            Assert.Empty(versions);
        }

        [Fact]
        public async Task GetFrameworksAsync_ReturnsEmptyForNonExistentPackage()
        {
            List<string> frameworks = await _locator.GetFrameworksAsync(
                "NonExistentPackage12345xyz", "1.0.0", CancellationToken.None);

            Assert.Empty(frameworks);
        }

        [Fact]
        public async Task GetXmlDocPathAsync_ReturnsNullForNonExistentPackage()
        {
            string? path = await _locator.GetXmlDocPathAsync(
                "NonExistentPackage12345xyz", "1.0.0", "net8.0", CancellationToken.None);

            Assert.Null(path);
        }

        [Fact]
        public async Task FindPackageDirectoryAsync_IsCaseInsensitive()
        {
            // First find a package that exists using the global packages path
            string globalPath = await _locator.GetGlobalPackagesPathAsync(CancellationToken.None);
            string? firstDir = Directory.EnumerateDirectories(globalPath).FirstOrDefault();

            if (firstDir == null)
            {
                return; // Skip if no packages installed
            }

            string packageName = Path.GetFileName(firstDir);

            // Try with different casing
            string? resultLower = await _locator.FindPackageDirectoryAsync(
                packageName.ToLowerInvariant(), CancellationToken.None);
            string? resultUpper = await _locator.FindPackageDirectoryAsync(
                packageName.ToUpperInvariant(), CancellationToken.None);

            Assert.NotNull(resultLower);
            Assert.NotNull(resultUpper);
            Assert.Equal(resultLower, resultUpper);
        }
    }
}
