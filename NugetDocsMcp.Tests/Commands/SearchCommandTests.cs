using Moq;
using NugetDocsMcp.Core.Commands;
using NugetDocsMcp.Core.Interfaces;
using NugetDocsMcp.Core.Models;

namespace NugetDocsMcp.Tests.Commands
{
    public class SearchCommandTests
    {
        private readonly Mock<INuGetPackageLocator> _locatorMock = new();
        private readonly Mock<IXmlDocParser> _parserMock = new();

        private void SetupLocatorForSuccess(string version = "1.0.0", string framework = "net8.0")
        {
            _locatorMock.Setup(l => l.FindPackageDirectoryAsync("TestPkg", It.IsAny<CancellationToken>()))
                .ReturnsAsync(@"C:\packages\testpkg");
            _locatorMock.Setup(l => l.GetVersionsAsync("TestPkg", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string> { version });
            _locatorMock.Setup(l => l.GetFrameworksAsync("TestPkg", version, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string> { framework });
            _locatorMock.Setup(l => l.GetXmlDocPathAsync("TestPkg", version, framework, It.IsAny<CancellationToken>()))
                .ReturnsAsync(@"C:\packages\testpkg\1.0.0\lib\net8.0\TestPkg.xml");
        }

        [Fact]
        public async Task ExecuteAsync_ReturnsError_WhenPackageNotFound()
        {
            _locatorMock.Setup(l => l.FindPackageDirectoryAsync("Missing", It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);

            SearchCommand command = new SearchCommand(_locatorMock.Object, _parserMock.Object, "Missing", "Query", null, null);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("not found", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_ReturnsMatches_WhenFound()
        {
            SetupLocatorForSuccess();

            XmlDocDocument doc = new XmlDocDocument("TestAssembly", new List<XmlDocMember>
            {
                new XmlDocMember
                {
                    Kind = MemberKind.Method,
                    FullName = "TestNamespace.TestClass.DoSomething(System.String)",
                    Name = "DoSomething",
                    Summary = "Does something.",
                    Parameters = new List<XmlDocParameter>
                    {
                        new XmlDocParameter("input", "System.String", "The input value.")
                    },
                    Returns = "A result."
                }
            });

            _parserMock.Setup(p => p.ParseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(doc);
            _parserMock.Setup(p => p.Search(doc, "DoSomething"))
                .Returns(doc.Members);

            SearchCommand command = new SearchCommand(_locatorMock.Object, _parserMock.Object, "TestPkg", "DoSomething", null, null);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("1 match", result.Message);
            Assert.Contains("[Method]", result.Details);
            Assert.Contains("DoSomething", result.Details);
            Assert.Contains("Does something.", result.Details);
            Assert.Contains("input", result.Details);
            Assert.Contains("A result.", result.Details);
        }

        [Fact]
        public async Task ExecuteAsync_ReturnsNoMatches_WhenNoneFound()
        {
            SetupLocatorForSuccess();

            XmlDocDocument doc = new XmlDocDocument("TestAssembly", new List<XmlDocMember>());
            _parserMock.Setup(p => p.ParseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(doc);
            _parserMock.Setup(p => p.Search(doc, "NotExist"))
                .Returns(new List<XmlDocMember>());

            SearchCommand command = new SearchCommand(_locatorMock.Object, _parserMock.Object, "TestPkg", "NotExist", null, null);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("No matches", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_ListsFrameworks_WhenMultipleExist()
        {
            _locatorMock.Setup(l => l.FindPackageDirectoryAsync("TestPkg", It.IsAny<CancellationToken>()))
                .ReturnsAsync(@"C:\packages\testpkg");
            _locatorMock.Setup(l => l.GetVersionsAsync("TestPkg", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string> { "1.0.0" });
            _locatorMock.Setup(l => l.GetFrameworksAsync("TestPkg", "1.0.0", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string> { "net6.0", "net8.0" });

            SearchCommand command = new SearchCommand(_locatorMock.Object, _parserMock.Object, "TestPkg", "Query", null, null);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("Multiple frameworks", result.Message);
            Assert.Contains("net6.0", result.Details);
            Assert.Contains("net8.0", result.Details);
        }

        [Fact]
        public async Task ExecuteAsync_ReturnsError_WhenNoXmlDocFound()
        {
            _locatorMock.Setup(l => l.FindPackageDirectoryAsync("TestPkg", It.IsAny<CancellationToken>()))
                .ReturnsAsync(@"C:\packages\testpkg");
            _locatorMock.Setup(l => l.GetVersionsAsync("TestPkg", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string> { "1.0.0" });
            _locatorMock.Setup(l => l.GetFrameworksAsync("TestPkg", "1.0.0", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string> { "net8.0" });
            _locatorMock.Setup(l => l.GetXmlDocPathAsync("TestPkg", "1.0.0", "net8.0", It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);

            SearchCommand command = new SearchCommand(_locatorMock.Object, _parserMock.Object, "TestPkg", "Query", null, null);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("No XML documentation", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_FormatsExceptions()
        {
            SetupLocatorForSuccess();

            XmlDocDocument doc = new XmlDocDocument("TestAssembly", new List<XmlDocMember>
            {
                new XmlDocMember
                {
                    Kind = MemberKind.Method,
                    FullName = "Ns.Class.Method()",
                    Name = "Method",
                    Summary = "A method.",
                    Exceptions = new List<XmlDocException>
                    {
                        new XmlDocException("System.InvalidOperationException", "When something is wrong.")
                    }
                }
            });

            _parserMock.Setup(p => p.ParseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(doc);
            _parserMock.Setup(p => p.Search(doc, "Method"))
                .Returns(doc.Members);

            SearchCommand command = new SearchCommand(_locatorMock.Object, _parserMock.Object, "TestPkg", "Method", null, null);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("Exceptions:", result.Details);
            Assert.Contains("System.InvalidOperationException", result.Details);
        }
    }
}
