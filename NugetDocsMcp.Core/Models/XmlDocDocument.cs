namespace NugetDocsMcp.Core.Models
{
    /// <summary>
    /// A parsed XML documentation file containing assembly name and members.
    /// </summary>
    public record XmlDocDocument(string AssemblyName, List<XmlDocMember> Members);
}
