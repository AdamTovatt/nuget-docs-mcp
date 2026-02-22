using NugetDocsMcp.Core.Services;

namespace NugetDocsMcp.Tests.Services
{
    public class ReadmeProviderTests : IDisposable
    {
        private readonly ReadmeProvider _provider = new();
        private readonly string _tempDir;

        public ReadmeProviderTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"NugetDocsMcpTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        [Fact]
        public async Task GetReadmeAsync_ReturnsReadmeContent()
        {
            string readmePath = Path.Combine(_tempDir, "README.md");
            await File.WriteAllTextAsync(readmePath, "# Test Package\n\nThis is a test.");

            string? result = await _provider.GetReadmeAsync(_tempDir, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Contains("# Test Package", result);
        }

        [Fact]
        public async Task GetReadmeAsync_IsCaseInsensitive()
        {
            string readmePath = Path.Combine(_tempDir, "readme.md");
            await File.WriteAllTextAsync(readmePath, "# Lowercase readme");

            string? result = await _provider.GetReadmeAsync(_tempDir, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Contains("# Lowercase readme", result);
        }

        [Fact]
        public async Task GetReadmeAsync_ReturnsNullWhenNoReadme()
        {
            // Create some other file but no readme
            await File.WriteAllTextAsync(Path.Combine(_tempDir, "other.txt"), "not a readme");

            string? result = await _provider.GetReadmeAsync(_tempDir, CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetReadmeAsync_ReturnsNullForNonExistentDirectory()
        {
            string nonExistent = Path.Combine(_tempDir, "does_not_exist");

            string? result = await _provider.GetReadmeAsync(nonExistent, CancellationToken.None);

            Assert.Null(result);
        }
    }
}
