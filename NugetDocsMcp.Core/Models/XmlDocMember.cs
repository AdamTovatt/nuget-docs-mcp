namespace NugetDocsMcp.Core.Models
{
    /// <summary>
    /// A parsed XML documentation member with all doc elements.
    /// </summary>
    public class XmlDocMember
    {
        public MemberKind Kind { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Summary { get; init; }
        public List<XmlDocParameter> Parameters { get; init; } = new();
        public List<XmlDocTypeParameter> TypeParameters { get; init; } = new();
        public string? Returns { get; init; }
        public List<XmlDocException> Exceptions { get; init; } = new();
        public string? Remarks { get; init; }
        public string? Example { get; init; }
    }

    /// <summary>
    /// A parameter documented in XML docs.
    /// </summary>
    public record XmlDocParameter(string Name, string? Type, string Description);

    /// <summary>
    /// A type parameter documented in XML docs.
    /// </summary>
    public record XmlDocTypeParameter(string Name, string Description);

    /// <summary>
    /// An exception documented in XML docs.
    /// </summary>
    public record XmlDocException(string Type, string Description);
}
