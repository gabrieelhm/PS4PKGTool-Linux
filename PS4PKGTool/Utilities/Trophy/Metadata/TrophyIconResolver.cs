#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PS4PKGTool.Utilities.TrophyMetadata
{
    public sealed class TrophyIconResolver
    {
        private static readonly Regex TrophyIconName = new(@"^TROP(?<id>\d{3})\.PNG$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private readonly TrpReader _reader;
        private readonly TrophyResourceDetector _detector;

        public TrophyIconResolver(TrpReader reader, TrophyResourceDetector detector)
        {
            _reader = reader;
            _detector = detector;
        }

        public IReadOnlyList<TrophyInfo> AttachIcons(TrpArchive archive, IReadOnlyList<TrophyInfo> trophies)
        {
            var icons = new Dictionary<int, (TrpEntry Entry, byte[] Bytes)>();
            foreach (TrpEntry entry in archive.Entries)
            {
                Match match = TrophyIconName.Match(entry.Name);
                if (!match.Success) continue;
                byte[] bytes = _reader.ReadEntry(archive, entry);
                if (_detector.Detect(bytes, entry) != TrophyResourceKind.Png) continue;
                int id = int.Parse(match.Groups["id"].Value);
                icons.TryAdd(id, (entry, bytes));
            }

            return trophies.Select(trophy =>
            {
                icons.TryGetValue(trophy.Id, out var icon);
                return new TrophyInfo
                {
                    Id = trophy.Id,
                    Name = trophy.Name,
                    Description = trophy.Description,
                    Grade = trophy.Grade,
                    IsHidden = trophy.IsHidden,
                    GroupId = trophy.GroupId,
                    GroupName = trophy.GroupName,
                    PlatinumId = trophy.PlatinumId,
                    IconData = icon.Bytes,
                    IconEntryName = icon.Entry?.Name ?? string.Empty
                };
            }).ToArray();
        }
    }
}
