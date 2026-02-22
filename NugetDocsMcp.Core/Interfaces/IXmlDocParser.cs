using NugetDocsMcp.Core.Models;

namespace NugetDocsMcp.Core.Interfaces
{
    /// <summary>
    /// Parses XML documentation files into structured models.
    /// </summary>
    public interface IXmlDocParser
    {
        /// <summary>
        /// Parses an XML documentation file into a structured document.
        /// </summary>
        Task<XmlDocDocument> ParseAsync(string xmlFilePath, CancellationToken cancellationToken);

        /// <summary>
        /// Searches members by query string (case-insensitive contains match on FullName).
        /// </summary>
        List<XmlDocMember> Search(XmlDocDocument document, string query);
    }
}
