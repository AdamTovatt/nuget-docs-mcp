using Moq;
using NugetDocsMcp.Core.Commands;
using NugetDocsMcp.Core.Interfaces;

namespace NugetDocsMcp.Tests.Commands
{
    public class ReadmeCommandTests
    {
        private readonly Mock<INuGetPackageLocator> _locatorMock = new();
        private readonly Mock<IReadmeProvider> _readmeProviderMock = new();

        [Fact]
        public async Task ExecuteAsync_ReturnsError_WhenPackageNotFound()
        {
            _locatorMock.Setup(l => l.FindPackageDirectoryAsync("Missing", It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);

            ReadmeCommand command = new ReadmeCommand(_locatorMock.Object, _readmeProviderMock.Object, "Missing", null);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("not found", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_AutoResolvesVersion_WhenOnlyOneExists()
        {
            _locatorMock.Setup(l => l.FindPackageDirectoryAsync("TestPkg", It.IsAny<CancellationToken>()))
                .ReturnsAsync(@"C:\packages\testpkg");
            _locatorMock.Setup(l => l.GetVersionsAsync("TestPkg", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string> { "1.0.0" });
            _readmeProviderMock.Setup(r => r.GetReadmeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("# Hello World");

            ReadmeCommand command = new ReadmeCommand(_locatorMock.Object, _readmeProviderMock.Object, "TestPkg", null);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("1.0.0", result.Message);
            Assert.Contains("# Hello World", result.Details);
        }

        [Fact]
        public async Task ExecuteAsync_ListsVersions_WhenMultipleExistAndNoneSpecified()
        {
            _locatorMock.Setup(l => l.FindPackageDirectoryAsync("TestPkg", It.IsAny<CancellationToken>()))
                .ReturnsAsync(@"C:\packages\testpkg");
            _locatorMock.Setup(l => l.GetVersionsAsync("TestPkg", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string> { "1.0.0", "2.0.0" });

            ReadmeCommand command = new ReadmeCommand(_locatorMock.Object, _readmeProviderMock.Object, "TestPkg", null);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("Multiple versions", result.Message);
            Assert.Contains("1.0.0", result.Details);
            Assert.Contains("2.0.0", result.Details);
        }

        [Fact]
        public async Task ExecuteAsync_UsesSpecifiedVersion()
        {
            _locatorMock.Setup(l => l.FindPackageDirectoryAsync("TestPkg", It.IsAny<CancellationToken>()))
                .ReturnsAsync(@"C:\packages\testpkg");
            _locatorMock.Setup(l => l.GetVersionsAsync("TestPkg", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string> { "1.0.0", "2.0.0" });
            _readmeProviderMock.Setup(r => r.GetReadmeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("# Version 2");

            ReadmeCommand command = new ReadmeCommand(_locatorMock.Object, _readmeProviderMock.Object, "TestPkg", "2.0.0");
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("2.0.0", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_ReturnsError_WhenSpecifiedVersionNotFound()
        {
            _locatorMock.Setup(l => l.FindPackageDirectoryAsync("TestPkg", It.IsAny<CancellationToken>()))
                .ReturnsAsync(@"C:\packages\testpkg");
            _locatorMock.Setup(l => l.GetVersionsAsync("TestPkg", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string> { "1.0.0" });

            ReadmeCommand command = new ReadmeCommand(_locatorMock.Object, _readmeProviderMock.Object, "TestPkg", "9.9.9");
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("9.9.9", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_ReturnsError_WhenNoReadmeFound()
        {
            _locatorMock.Setup(l => l.FindPackageDirectoryAsync("TestPkg", It.IsAny<CancellationToken>()))
                .ReturnsAsync(@"C:\packages\testpkg");
            _locatorMock.Setup(l => l.GetVersionsAsync("TestPkg", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string> { "1.0.0" });
            _readmeProviderMock.Setup(r => r.GetReadmeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);

            ReadmeCommand command = new ReadmeCommand(_locatorMock.Object, _readmeProviderMock.Object, "TestPkg", null);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("No README.md", result.Message);
        }
    }
}
