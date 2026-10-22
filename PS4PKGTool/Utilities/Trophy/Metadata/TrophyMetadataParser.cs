#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace PS4PKGTool.Utilities.TrophyMetadata
{
    public sealed class TrophyMetadataParser
    {
        public IReadOnlyList<TrophyInfo> Parse(ReadOnlySpan<byte> xmlBytes, string? expectedNpCommunicationId = null)
        {
            string xml;
            try
            {
                xml = new UTF8Encoding(false, true).GetString(xmlBytes);
            }
            catch (DecoderFallbackException ex)
            {
                throw new InvalidDataException("Trophy metadata is not valid UTF-8.", ex);
            }

            XDocument document;
            try
            {
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    MaxCharactersInDocument = 16 * 1024 * 1024
                };
                using var stringReader = new StringReader(xml);
                using XmlReader reader = XmlReader.Create(stringReader, settings);
                document = XDocument.Load(reader, LoadOptions.None);
            }
            catch (XmlException ex)
            {
                throw new InvalidDataException("Trophy metadata XML is invalid.", ex);
            }

            XElement root = document.Root ?? throw new InvalidDataException("Trophy metadata XML has no root element.");
            if (!string.Equals(root.Name.LocalName, "trophyconf", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Unsupported trophy metadata root element '{root.Name.LocalName}'.");

            string? documentId = Child(root, "npcommid")?.Value.Trim();
            if (expectedNpCommunicationId != null && !string.Equals(documentId, expectedNpCommunicationId, StringComparison.Ordinal))
                throw new InvalidDataException($"Decrypted metadata NP Communication ID '{documentId}' does not match '{expectedNpCommunicationId}'.");

            var groups = root.Elements().Where(e => e.Name.LocalName == "group")
                .Select(e => new { Id = ParseRequiredInt(e, "id"), Name = Child(e, "name")?.Value.Trim() })
                .ToDictionary(g => g.Id, g => string.IsNullOrWhiteSpace(g.Name) ? $"Group {g.Id:000}" : g.Name!);

            var trophies = new List<TrophyInfo>();
            foreach (XElement element in root.Elements().Where(e => e.Name.LocalName == "trophy"))
            {
                int id = ParseRequiredInt(element, "id");
                int? groupId = ParseOptionalInt(element.Attribute("gid")?.Value, allowNegativeOne: false);
                if (groupId == 0) groupId = null;
                int? platinumId = ParseOptionalInt(element.Attribute("pid")?.Value, allowNegativeOne: true);
                string hidden = element.Attribute("hidden")?.Value ?? "no";
                if (!hidden.Equals("yes", StringComparison.OrdinalIgnoreCase) && !hidden.Equals("no", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Trophy {id:000} has invalid hidden value '{hidden}'.");

                trophies.Add(new TrophyInfo
                {
                    Id = id,
                    Name = Child(element, "name")?.Value.Trim() ?? string.Empty,
                    Description = Child(element, "detail")?.Value.Trim() ?? string.Empty,
                    Grade = ParseGrade(element.Attribute("ttype")?.Value),
                    IsHidden = hidden.Equals("yes", StringComparison.OrdinalIgnoreCase),
                    GroupId = groupId,
                    GroupName = groupId.HasValue ? groups.GetValueOrDefault(groupId.Value, $"Group {groupId:000}") : "Base Game",
                    PlatinumId = platinumId
                });
            }

            if (trophies.Count == 0)
                throw new InvalidDataException("Trophy metadata contains no trophy definitions.");
            if (trophies.Select(t => t.Id).Distinct().Count() != trophies.Count)
                throw new InvalidDataException("Trophy metadata contains duplicate trophy IDs.");
            return trophies;
        }

        private static XElement? Child(XElement parent, string localName) =>
            parent.Elements().FirstOrDefault(e => e.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));

        private static int ParseRequiredInt(XElement element, string attributeName)
        {
            string? value = element.Attribute(attributeName)?.Value;
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int result) || result < 0)
                throw new InvalidDataException($"Element {element.Name.LocalName} has invalid {attributeName} '{value}'.");
            return result;
        }

        private static int? ParseOptionalInt(string? value, bool allowNegativeOne)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (!int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int result))
                throw new InvalidDataException($"Invalid trophy relationship ID '{value}'.");
            if (allowNegativeOne && result == -1) return null;
            if (result < 0) throw new InvalidDataException($"Invalid trophy relationship ID '{value}'.");
            return result;
        }

        private static TrophyGrade ParseGrade(string? value) => value?.ToUpperInvariant() switch
        {
            "B" => TrophyGrade.Bronze,
            "S" => TrophyGrade.Silver,
            "G" => TrophyGrade.Gold,
            "P" => TrophyGrade.Platinum,
            _ => TrophyGrade.Unknown
        };
    }
}
