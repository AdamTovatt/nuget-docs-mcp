using NugetDocsMcp.Core.Models;
using NugetDocsMcp.Core.Services;

namespace NugetDocsMcp.Tests.Services
{
    public class XmlDocParserTests
    {
        private readonly XmlDocParser _parser = new();

        private const string SampleXml = @"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>TestAssembly</name>
    </assembly>
    <members>
        <member name=""T:TestNamespace.TestClass"">
            <summary>A test class for demonstration.</summary>
        </member>
        <member name=""T:TestNamespace.ITestInterface"">
            <summary>A test interface.</summary>
        </member>
        <member name=""M:TestNamespace.TestClass.DoSomething(System.String,System.Int32)"">
            <summary>Does something with the given parameters.</summary>
            <param name=""name"">The name to use.</param>
            <param name=""count"">The count value.</param>
            <returns>A result string.</returns>
            <exception cref=""T:System.ArgumentNullException"">Thrown when name is null.</exception>
            <exception cref=""T:System.ArgumentOutOfRangeException"">Thrown when count is negative.</exception>
        </member>
        <member name=""M:TestNamespace.TestClass.SimpleMethod"">
            <summary>A simple method with no parameters.</summary>
        </member>
        <member name=""P:TestNamespace.TestClass.Name"">
            <summary>Gets or sets the name.</summary>
        </member>
        <member name=""F:TestNamespace.TestClass.DefaultValue"">
            <summary>The default value constant.</summary>
        </member>
        <member name=""E:TestNamespace.TestClass.Changed"">
            <summary>Raised when the value changes.</summary>
        </member>
        <member name=""M:TestNamespace.TestClass.GenericMethod``1(``0)"">
            <summary>A generic method.</summary>
            <typeparam name=""T"">The type parameter.</typeparam>
            <param name=""value"">The value.</param>
            <returns>The converted value.</returns>
        </member>
        <member name=""M:TestNamespace.TestClass.MethodWithSee"">
            <summary>Returns a <see cref=""T:TestNamespace.TestClass""/> instance.</summary>
            <remarks>Use <paramref name=""value""/> carefully and check <see langword=""null""/>.</remarks>
        </member>
    </members>
</doc>";

        private async Task<XmlDocDocument> ParseSampleXmlAsync()
        {
            string tempFile = Path.GetTempFileName();
            try
            {
                await File.WriteAllTextAsync(tempFile, SampleXml);
                return await _parser.ParseAsync(tempFile, CancellationToken.None);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task ParseAsync_ParsesAssemblyName()
        {
            XmlDocDocument doc = await ParseSampleXmlAsync();
            Assert.Equal("TestAssembly", doc.AssemblyName);
        }

        [Fact]
        public async Task ParseAsync_ParsesAllMembers()
        {
            XmlDocDocument doc = await ParseSampleXmlAsync();
            Assert.Equal(9, doc.Members.Count);
        }

        [Fact]
        public async Task ParseAsync_ParsesTypeMembers()
        {
            XmlDocDocument doc = await ParseSampleXmlAsync();
            List<XmlDocMember> types = doc.Members.Where(m => m.Kind == MemberKind.Type).ToList();
            Assert.Equal(2, types.Count);
            Assert.Contains(types, t => t.FullName == "TestNamespace.TestClass");
            Assert.Contains(types, t => t.FullName == "TestNamespace.ITestInterface");
        }

        [Fact]
        public async Task ParseAsync_ParsesMethodWithParameters()
        {
            XmlDocDocument doc = await ParseSampleXmlAsync();
            XmlDocMember method = doc.Members.First(m => m.Name == "DoSomething");

            Assert.Equal(MemberKind.Method, method.Kind);
            Assert.Equal("Does something with the given parameters.", method.Summary);
            Assert.Equal(2, method.Parameters.Count);
            Assert.Equal("name", method.Parameters[0].Name);
            Assert.Equal("System.String", method.Parameters[0].Type);
            Assert.Equal("The name to use.", method.Parameters[0].Description);
            Assert.Equal("count", method.Parameters[1].Name);
            Assert.Equal("System.Int32", method.Parameters[1].Type);
            Assert.Equal("A result string.", method.Returns);
        }

        [Fact]
        public async Task ParseAsync_ParsesExceptions()
        {
            XmlDocDocument doc = await ParseSampleXmlAsync();
            XmlDocMember method = doc.Members.First(m => m.Name == "DoSomething");

            Assert.Equal(2, method.Exceptions.Count);
            Assert.Equal("System.ArgumentNullException", method.Exceptions[0].Type);
            Assert.Equal("Thrown when name is null.", method.Exceptions[0].Description);
            Assert.Equal("System.ArgumentOutOfRangeException", method.Exceptions[1].Type);
        }

        [Fact]
        public async Task ParseAsync_ParsesPropertyKind()
        {
            XmlDocDocument doc = await ParseSampleXmlAsync();
            XmlDocMember prop = doc.Members.First(m => m.Name == "Name" && m.Kind == MemberKind.Property);

            Assert.Equal(MemberKind.Property, prop.Kind);
            Assert.Equal("Gets or sets the name.", prop.Summary);
        }

        [Fact]
        public async Task ParseAsync_ParsesFieldKind()
        {
            XmlDocDocument doc = await ParseSampleXmlAsync();
            XmlDocMember field = doc.Members.First(m => m.Kind == MemberKind.Field);

            Assert.Equal("DefaultValue", field.Name);
        }

        [Fact]
        public async Task ParseAsync_ParsesEventKind()
        {
            XmlDocDocument doc = await ParseSampleXmlAsync();
            XmlDocMember evt = doc.Members.First(m => m.Kind == MemberKind.Event);

            Assert.Equal("Changed", evt.Name);
        }

        [Fact]
        public async Task ParseAsync_ParsesTypeParameters()
        {
            XmlDocDocument doc = await ParseSampleXmlAsync();
            XmlDocMember method = doc.Members.First(m => m.Name == "GenericMethod");

            Assert.Single(method.TypeParameters);
            Assert.Equal("T", method.TypeParameters[0].Name);
            Assert.Equal("The type parameter.", method.TypeParameters[0].Description);
        }

        [Fact]
        public async Task ParseAsync_ExtractsShortName()
        {
            XmlDocDocument doc = await ParseSampleXmlAsync();
            XmlDocMember cls = doc.Members.First(m => m.FullName == "TestNamespace.TestClass");

            Assert.Equal("TestClass", cls.Name);
        }

        [Fact]
        public async Task ParseAsync_HandlesSeeElements()
        {
            XmlDocDocument doc = await ParseSampleXmlAsync();
            XmlDocMember method = doc.Members.First(m => m.Name == "MethodWithSee");

            Assert.NotNull(method.Summary);
            Assert.Contains("TestNamespace.TestClass", method.Summary);
        }

        [Fact]
        public async Task Search_FindsByFullName()
        {
            XmlDocDocument doc = await ParseSampleXmlAsync();
            List<XmlDocMember> results = _parser.Search(doc, "TestNamespace.TestClass.DoSomething");

            Assert.Single(results);
            Assert.Equal("DoSomething", results[0].Name);
        }

        [Fact]
        public async Task Search_FindsByShortNameContains()
        {
            XmlDocDocument doc = await ParseSampleXmlAsync();
            List<XmlDocMember> results = _parser.Search(doc, "DoSomething");

            Assert.Single(results);
        }

        [Fact]
        public async Task Search_IsCaseInsensitive()
        {
            XmlDocDocument doc = await ParseSampleXmlAsync();
            List<XmlDocMember> results = _parser.Search(doc, "dosomething");

            Assert.Single(results);
        }

        [Fact]
        public async Task Search_ReturnsMultipleMatches()
        {
            XmlDocDocument doc = await ParseSampleXmlAsync();
            List<XmlDocMember> results = _parser.Search(doc, "TestClass");

            Assert.True(results.Count > 1);
        }

        [Fact]
        public async Task Search_ReturnsEmptyForNoMatch()
        {
            XmlDocDocument doc = await ParseSampleXmlAsync();
            List<XmlDocMember> results = _parser.Search(doc, "NonExistentMember");

            Assert.Empty(results);
        }
    }
}
