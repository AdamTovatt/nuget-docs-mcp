using System.Text;
using NugetDocsMcp.Core.Interfaces;
using NugetDocsMcp.Core.Models;

namespace NugetDocsMcp.Core.Commands
{
    /// <summary>
    /// Command to search XML docs by member name.
    /// </summary>
    public class SearchCommand : ICommand
    {
        private readonly INuGetPackageLocator _locator;
        private readonly IXmlDocParser _parser;
        private readonly string _packageName;
        private readonly string _query;
        private readonly string? _version;
        private readonly string? _framework;

        public SearchCommand(
            INuGetPackageLocator locator,
            IXmlDocParser parser,
            string packageName,
            string query,
            string? version,
            string? framework)
        {
            _locator = locator;
            _parser = parser;
            _packageName = packageName;
            _query = query;
            _version = version;
            _framework = framework;
        }

        /// <inheritdoc/>
        public async Task<CommandResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Find package directory
                string? packageDir = await _locator.FindPackageDirectoryAsync(_packageName, cancellationToken);
                if (packageDir == null)
                {
                    return new CommandResult(false, $"Package '{_packageName}' not found in local NuGet cache.");
                }

                // Resolve version
                List<string> versions = await _locator.GetVersionsAsync(_packageName, cancellationToken);
                if (versions.Count == 0)
                {
                    return new CommandResult(false, $"No versions found for package '{_packageName}'.");
                }

                string version;
                if (_version != null)
                {
                    if (!versions.Contains(_version))
                    {
                        return new CommandResult(false, $"Version '{_version}' not found for package '{_packageName}'.",
                            $"Available versions:\n{string.Join("\n", versions.Select(v => $"  {v}"))}");
                    }
                    version = _version;
                }
                else if (versions.Count == 1)
                {
                    version = versions[0];
                }
                else
                {
                    return new CommandResult(false, $"Multiple versions available for '{_packageName}'. Please specify --version.",
                        $"Available versions:\n{string.Join("\n", versions.Select(v => $"  {v}"))}");
                }

                // Resolve framework
                List<string> frameworks = await _locator.GetFrameworksAsync(_packageName, version, cancellationToken);
                if (frameworks.Count == 0)
                {
                    return new CommandResult(false, $"No framework targets found for {_packageName} v{version}.");
                }

                string framework;
                if (_framework != null)
                {
                    if (!frameworks.Contains(_framework))
                    {
                        return new CommandResult(false, $"Framework '{_framework}' not found for {_packageName} v{version}.",
                            $"Available frameworks:\n{string.Join("\n", frameworks.Select(f => $"  {f}"))}");
                    }
                    framework = _framework;
                }
                else if (frameworks.Count == 1)
                {
                    framework = frameworks[0];
                }
                else
                {
                    return new CommandResult(false, $"Multiple frameworks available for {_packageName} v{version}. Please specify --framework.",
                        $"Available frameworks:\n{string.Join("\n", frameworks.Select(f => $"  {f}"))}");
                }

                // Find and parse XML docs
                string? xmlPath = await _locator.GetXmlDocPathAsync(_packageName, version, framework, cancellationToken);
                if (xmlPath == null)
                {
                    return new CommandResult(false, $"No XML documentation found for {_packageName} v{version} ({framework}).");
                }

                XmlDocDocument document = await _parser.ParseAsync(xmlPath, cancellationToken);
                List<XmlDocMember> matches = _parser.Search(document, _query);

                if (matches.Count == 0)
                {
                    return new CommandResult(true, $"No matches found for '{_query}' in {_packageName} v{version} ({framework}).");
                }

                string details = FormatMatches(matches);
                return new CommandResult(true, $"Found {matches.Count} match(es) in {_packageName} v{version} ({framework}):", details);
            }
            catch (Exception ex)
            {
                return new CommandResult(false, $"Error: {ex.Message}");
            }
        }

        private static string FormatMatches(List<XmlDocMember> matches)
        {
            StringBuilder sb = new StringBuilder();

            foreach (XmlDocMember member in matches)
            {
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }

                sb.AppendLine($"[{member.Kind}] {member.FullName}");

                if (member.Summary != null)
                {
                    sb.AppendLine($"  Summary: {member.Summary}");
                }

                if (member.TypeParameters.Count > 0)
                {
                    sb.AppendLine("  Type Parameters:");
                    foreach (XmlDocTypeParameter tp in member.TypeParameters)
                    {
                        sb.AppendLine($"    - {tp.Name}: {tp.Description}");
                    }
                }

                if (member.Parameters.Count > 0)
                {
                    sb.AppendLine("  Parameters:");
                    foreach (XmlDocParameter p in member.Parameters)
                    {
                        string typeInfo = p.Type != null ? $" ({p.Type})" : "";
                        sb.AppendLine($"    - {p.Name}{typeInfo}: {p.Description}");
                    }
                }

                if (member.Returns != null)
                {
                    sb.AppendLine($"  Returns: {member.Returns}");
                }

                if (member.Exceptions.Count > 0)
                {
                    sb.AppendLine("  Exceptions:");
                    foreach (XmlDocException ex in member.Exceptions)
                    {
                        sb.AppendLine($"    - {ex.Type}: {ex.Description}");
                    }
                }

                if (member.Remarks != null)
                {
                    sb.AppendLine($"  Remarks: {member.Remarks}");
                }

                if (member.Example != null)
                {
                    sb.AppendLine($"  Example: {member.Example}");
                }
            }

            return sb.ToString().TrimEnd();
        }
    }
}
