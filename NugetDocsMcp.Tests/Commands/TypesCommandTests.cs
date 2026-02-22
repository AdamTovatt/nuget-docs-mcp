using Moq;
using NugetDocsMcp.Core.Commands;
using NugetDocsMcp.Core.Interfaces;
using NugetDocsMcp.Core.Models;

namespace NugetDocsMcp.Tests.Commands
{
    public class TypesCommandTests
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

            TypesCommand command = new TypesCommand(_locatorMock.Object, _parserMock.Object, "Missing", null, null);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("not found", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_ListsTypes()
        {
            SetupLocatorForSuccess();

            XmlDocDocument doc = new XmlDocDocument("TestAssembly", new List<XmlDocMember>
            {
                new XmlDocMember
                {
                    Kind = MemberKind.Type,
                    FullName = "TestNamespace.ClassA",
                    Name = "ClassA",
                    Summary = "Class A."
                },
                new XmlDocMember
                {
                    Kind = MemberKind.Type,
                    FullName = "TestNamespace.ClassB",
                    Name = "ClassB",
                    Summary = "Class B."
                },
                new XmlDocMember
                {
                    Kind = MemberKind.Method,
                    FullName = "TestNamespace.ClassA.DoStuff()",
                    Name = "DoStuff",
                    Summary = "Should not appear."
                }
            });

            _parserMock.Setup(p => p.ParseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(doc);

            TypesCommand command = new TypesCommand(_locatorMock.Object, _parserMock.Object, "TestPkg", null, null);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("Types in TestPkg", result.Message);
            Assert.Contains("TestNamespace.ClassA", result.Details);
            Assert.Contains("TestNamespace.ClassB", result.Details);
            Assert.DoesNotContain("DoStuff", result.Details);
        }

        [Fact]
        public async Task ExecuteAsync_ReturnsNoTypes_WhenNoneExist()
        {
            SetupLocatorForSuccess();

            XmlDocDocument doc = new XmlDocDocument("TestAssembly", new List<XmlDocMember>
            {
                new XmlDocMember
                {
                    Kind = MemberKind.Method,
                    FullName = "Ns.Class.Method()",
                    Name = "Method"
                }
            });

            _parserMock.Setup(p => p.ParseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(doc);

            TypesCommand command = new TypesCommand(_locatorMock.Object, _parserMock.Object, "TestPkg", null, null);
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("No types found", result.Message);
        }

        [Fact]
        public async Task ExecuteAsync_UsesSpecifiedVersionAndFramework()
        {
            _locatorMock.Setup(l => l.FindPackageDirectoryAsync("TestPkg", It.IsAny<CancellationToken>()))
                .ReturnsAsync(@"C:\packages\testpkg");
            _locatorMock.Setup(l => l.GetVersionsAsync("TestPkg", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string> { "1.0.0", "2.0.0" });
            _locatorMock.Setup(l => l.GetFrameworksAsync("TestPkg", "2.0.0", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string> { "net6.0", "net8.0" });
            _locatorMock.Setup(l => l.GetXmlDocPathAsync("TestPkg", "2.0.0", "net8.0", It.IsAny<CancellationToken>()))
                .ReturnsAsync(@"C:\packages\testpkg\2.0.0\lib\net8.0\TestPkg.xml");

            XmlDocDocument doc = new XmlDocDocument("TestAssembly", new List<XmlDocMember>
            {
                new XmlDocMember { Kind = MemberKind.Type, FullName = "Ns.MyClass", Name = "MyClass" }
            });

            _parserMock.Setup(p => p.ParseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(doc);

            TypesCommand command = new TypesCommand(_locatorMock.Object, _parserMock.Object, "TestPkg", "2.0.0", "net8.0");
            CommandResult result = await command.ExecuteAsync(CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("v2.0.0", result.Message);
            Assert.Contains("net8.0", result.Message);
        }
    }
}
