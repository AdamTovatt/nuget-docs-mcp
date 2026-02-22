using System.Xml.Linq;
using NugetDocsMcp.Core.Interfaces;
using NugetDocsMcp.Core.Models;

namespace NugetDocsMcp.Core.Services
{
    /// <summary>
    /// Parses XML documentation files into structured models.
    /// </summary>
    public class XmlDocParser : IXmlDocParser
    {
        /// <inheritdoc/>
        public async Task<XmlDocDocument> ParseAsync(string xmlFilePath, CancellationToken cancellationToken)
        {
            string xml = await File.ReadAllTextAsync(xmlFilePath, cancellationToken);
            XDocument doc = XDocument.Parse(xml);

            string assemblyName = doc.Root?.Element("assembly")?.Element("name")?.Value ?? "Unknown";

            List<XmlDocMember> members = new List<XmlDocMember>();

            XElement? membersElement = doc.Root?.Element("members");
            if (membersElement != null)
            {
                foreach (XElement memberElement in membersElement.Elements("member"))
                {
                    string? nameAttr = memberElement.Attribute("name")?.Value;
                    if (nameAttr == null) continue;

                    XmlDocMember member = ParseMember(nameAttr, memberElement);
                    members.Add(member);
                }
            }

            return new XmlDocDocument(assemblyName, members);
        }

        /// <inheritdoc/>
        public List<XmlDocMember> Search(XmlDocDocument document, string query)
        {
            return document.Members
                .Where(m => m.FullName.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private static XmlDocMember ParseMember(string nameAttribute, XElement memberElement)
        {
            MemberKind kind = ParseMemberKind(nameAttribute);
            string fullName = nameAttribute.Length > 2 ? nameAttribute.Substring(2) : nameAttribute;
            string shortName = ExtractShortName(fullName, kind);

            // Extract parameter types from the full name (the part in parentheses)
            List<string> paramTypes = ExtractParameterTypes(fullName);

            // Parse param elements
            List<XmlDocParameter> parameters = new List<XmlDocParameter>();
            int paramIndex = 0;
            foreach (XElement paramElement in memberElement.Elements("param"))
            {
                string paramName = paramElement.Attribute("name")?.Value ?? "unknown";
                string paramDescription = GetInnerText(paramElement);
                string? paramType = paramIndex < paramTypes.Count ? paramTypes[paramIndex] : null;
                parameters.Add(new XmlDocParameter(paramName, paramType, paramDescription));
                paramIndex++;
            }

            // Parse typeparam elements
            List<XmlDocTypeParameter> typeParameters = new List<XmlDocTypeParameter>();
            foreach (XElement typeParamElement in memberElement.Elements("typeparam"))
            {
                string tpName = typeParamElement.Attribute("name")?.Value ?? "unknown";
                string tpDescription = GetInnerText(typeParamElement);
                typeParameters.Add(new XmlDocTypeParameter(tpName, tpDescription));
            }

            // Parse exception elements
            List<XmlDocException> exceptions = new List<XmlDocException>();
            foreach (XElement exceptionElement in memberElement.Elements("exception"))
            {
                string exType = exceptionElement.Attribute("cref")?.Value ?? "unknown";
                if (exType.StartsWith("T:"))
                {
                    exType = exType.Substring(2);
                }
                string exDescription = GetInnerText(exceptionElement);
                exceptions.Add(new XmlDocException(exType, exDescription));
            }

            return new XmlDocMember
            {
                Kind = kind,
                FullName = fullName,
                Name = shortName,
                Summary = GetElementText(memberElement, "summary"),
                Parameters = parameters,
                TypeParameters = typeParameters,
                Returns = GetElementText(memberElement, "returns"),
                Exceptions = exceptions,
                Remarks = GetElementText(memberElement, "remarks"),
                Example = GetElementText(memberElement, "example")
            };
        }

        private static MemberKind ParseMemberKind(string nameAttribute)
        {
            if (nameAttribute.Length < 2 || nameAttribute[1] != ':')
            {
                return MemberKind.Unknown;
            }

            return nameAttribute[0] switch
            {
                'T' => MemberKind.Type,
                'M' => MemberKind.Method,
                'P' => MemberKind.Property,
                'F' => MemberKind.Field,
                'E' => MemberKind.Event,
                _ => MemberKind.Unknown
            };
        }

        private static string ExtractShortName(string fullName, MemberKind kind)
        {
            // Remove parameters portion for methods
            string nameWithoutParams = fullName;
            int parenIndex = fullName.IndexOf('(');
            if (parenIndex >= 0)
            {
                nameWithoutParams = fullName.Substring(0, parenIndex);
            }

            // Get the last segment after the last dot
            int lastDot = nameWithoutParams.LastIndexOf('.');
            string shortName;
            if (lastDot >= 0 && lastDot < nameWithoutParams.Length - 1)
            {
                shortName = nameWithoutParams.Substring(lastDot + 1);
            }
            else
            {
                shortName = nameWithoutParams;
            }

            // Strip generic arity suffix (e.g. `1, ``2)
            int backtickIndex = shortName.IndexOf('`');
            if (backtickIndex >= 0)
            {
                shortName = shortName.Substring(0, backtickIndex);
            }

            return shortName;
        }

        private static List<string> ExtractParameterTypes(string fullName)
        {
            List<string> types = new List<string>();

            int parenStart = fullName.IndexOf('(');
            int parenEnd = fullName.LastIndexOf(')');
            if (parenStart < 0 || parenEnd <= parenStart)
            {
                return types;
            }

            string paramString = fullName.Substring(parenStart + 1, parenEnd - parenStart - 1);
            if (string.IsNullOrWhiteSpace(paramString))
            {
                return types;
            }

            // Split by comma, but respect nested generics
            int depth = 0;
            int start = 0;
            for (int i = 0; i < paramString.Length; i++)
            {
                char c = paramString[i];
                if (c == '{' || c == '<') depth++;
                else if (c == '}' || c == '>') depth--;
                else if (c == ',' && depth == 0)
                {
                    types.Add(paramString.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }

            types.Add(paramString.Substring(start).Trim());
            return types;
        }

        private static string? GetElementText(XElement parent, string elementName)
        {
            XElement? element = parent.Element(elementName);
            if (element == null) return null;

            string text = GetInnerText(element).Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }

        private static string GetInnerText(XElement element)
        {
            // Process nodes, replacing <see cref="..."/> and <paramref name="..."/> with readable text
            return string.Concat(element.Nodes().Select(ProcessNode)).Trim();
        }

        private static string ProcessNode(XNode node)
        {
            if (node is XText textNode)
            {
                return textNode.Value;
            }

            if (node is XElement el)
            {
                if (el.Name.LocalName == "see")
                {
                    string? cref = el.Attribute("cref")?.Value;
                    if (cref != null)
                    {
                        // Strip prefix like "T:" or "M:"
                        if (cref.Length > 2 && cref[1] == ':')
                        {
                            cref = cref.Substring(2);
                        }
                        return cref;
                    }

                    // Might have a langword attribute
                    string? langword = el.Attribute("langword")?.Value;
                    if (langword != null)
                    {
                        return langword;
                    }
                }

                if (el.Name.LocalName == "paramref")
                {
                    string? name = el.Attribute("name")?.Value;
                    if (name != null)
                    {
                        return name;
                    }
                }

                if (el.Name.LocalName == "typeparamref")
                {
                    string? name = el.Attribute("name")?.Value;
                    if (name != null)
                    {
                        return name;
                    }
                }

                if (el.Name.LocalName == "c" || el.Name.LocalName == "code")
                {
                    return el.Value;
                }

                // For other elements, process their children
                return string.Concat(el.Nodes().Select(ProcessNode));
            }

            return string.Empty;
        }
    }
}
