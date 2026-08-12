using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Formats.Tmp
{
    internal static class TheaterTileRegistryBuilder
    {
        private static readonly string[] SpecialRoles =
        {
            "RampBase", "RampSmooth", "SlopeSetPieces", "CliffSet", "WaterSet", "ShorePieces",
            "BridgeSet", "TrainBridgeSet", "WoodBridgeSet", "Ice1Set", "Ice2Set", "Ice3Set", "IceShoreSet",
            "RoughTile", "ClearToRoughLat", "SandTile", "ClearToSandLat", "PaveTile", "ClearToPaveLat",
            "GreenTile", "ClearToGreenLat"
        };

        internal static TheaterTileRegistry Build(TheaterControlDocument document, long maxGlobalTileIds = 10_000_000,
            int maxTileSets = 100_000, int maxDiagnostics = 4096)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (maxGlobalTileIds < 0 || maxTileSets < 0 || maxDiagnostics < 0) throw new ArgumentOutOfRangeException();
            var collector = new TmpDiagnosticCollector(maxDiagnostics);
            collector.Execution.Merge(document.Execution);
            var descriptors = new List<TheaterTileSetDescriptor>();
            var groups = document.Values.Where(v => v.Section.StartsWith("TileSet", StringComparison.OrdinalIgnoreCase))
                .GroupBy(v => ParseIndex(v.Section, document, collector))
                .Where(g => g.Key.HasValue)
                .OrderBy(g => g.Key.Value);
            foreach (var group in groups)
            {
                if (group.Count() > maxTileSets) { collector.Fail(Diagnostic(group.First(), TmpDiagnosticCode.DimensionBudgetExceeded, "registry", "TileSet budget exceeded.")); break; }
                var sectionValues = group.ToArray();
                string rawSection = sectionValues[0].Section;
                if (sectionValues.Select(v => v.Section).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
                    collector.Fail(Diagnostic(sectionValues[0], TmpDiagnosticCode.DuplicateTileSetIndex, "registry", "Duplicate normalized TileSet index was preserved and rejected."));
                TheaterValueView file = sectionValues.FirstOrDefault(v => string.Equals(v.Key, "FileName", StringComparison.OrdinalIgnoreCase));
                TheaterValueView count = sectionValues.FirstOrDefault(v => string.Equals(v.Key, "TilesInSet", StringComparison.OrdinalIgnoreCase));
                if (file == null || string.IsNullOrWhiteSpace(file.Raw)) collector.Fail(Diagnostic(sectionValues[0], TmpDiagnosticCode.MissingRequiredValue, "registry", "TileSet FileName is missing."));
                if (count == null || !count.IntegerCandidate.HasValue) collector.Fail(Diagnostic(sectionValues[0], TmpDiagnosticCode.InvalidInteger, "registry", "TileSet TilesInSet is not a valid integer."));
                descriptors.Add(new TheaterTileSetDescriptor(group.Key.Value, rawSection, file == null ? string.Empty : file.Raw,
                    count == null ? (int?)null : count.IntegerCandidate, sectionValues[0], sectionValues));
            }
            descriptors = descriptors.OrderBy(d => d.Index).ToList();
            for (int i = 1; i < descriptors.Count; i++)
                if (descriptors[i].Index != descriptors[i - 1].Index + 1)
                    collector.Add(Diagnostic(descriptors[i].SectionProvenance, TmpDiagnosticCode.TileSetGap, "registry", "A numeric TileSet gap was retained."), false);

            var ranges = new List<TheaterTileIdRange>();
            long next = 0;
            foreach (TheaterTileSetDescriptor descriptor in descriptors)
            {
                if (!descriptor.TilesInSetRaw.HasValue) continue;
                int count = descriptor.TilesInSetRaw.Value;
                if (count < 0) { collector.Fail(Diagnostic(descriptor.SectionProvenance, TmpDiagnosticCode.NegativeTilesInSet, "registry", "TilesInSet cannot be negative.")); continue; }
                if (count == 0) { collector.Fail(Diagnostic(descriptor.SectionProvenance, TmpDiagnosticCode.ZeroTilesInSet, "registry", "TilesInSet zero is not accepted by the strict registry profile.")); continue; }
                long end;
                try { end = checked(next + count); }
                catch (OverflowException) { collector.Fail(Diagnostic(descriptor.SectionProvenance, TmpDiagnosticCode.GlobalIdOverflow, "registry", "GlobalTileId range arithmetic overflowed.")); break; }
                if (end > maxGlobalTileIds) { collector.Fail(Diagnostic(descriptor.SectionProvenance, TmpDiagnosticCode.GlobalIdBudgetExceeded, "registry", "GlobalTileId budget exceeded.")); break; }
                ranges.Add(new TheaterTileIdRange(descriptor.Index, next, count, end, descriptor));
                next = end;
            }
            var roles = new List<TheaterSpecialRoleBinding>();
            foreach (string role in SpecialRoles)
            {
                TheaterValueView value = document.Find("General", role).FirstOrDefault();
                if (value == null || !value.IntegerCandidate.HasValue) continue;
                TheaterTileIdRange range = ranges.FirstOrDefault(r => r.TileSetIndex == value.IntegerCandidate.Value);
                if (range == null) collector.Fail(Diagnostic(value, TmpDiagnosticCode.SpecialRoleOutOfRange, "roles", "Special TileSet role references a missing range."));
                roles.Add(new TheaterSpecialRoleBinding(role, value.Raw, value.IntegerCandidate, range));
            }
            return new TheaterTileRegistry(document, descriptors, ranges, roles, collector.Diagnostics, collector.Execution, Hash(document, descriptors, ranges, roles));
        }

        private static int? ParseIndex(string section, TheaterControlDocument document, TmpDiagnosticCollector collector)
        {
            if (!section.StartsWith("TileSet", StringComparison.OrdinalIgnoreCase)) return null;
            string suffix = section.Substring(7);
            if (suffix.Length != 4 || !int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out int index))
            {
                TheaterValueView value = document.Values.FirstOrDefault(v => string.Equals(v.Section, section, StringComparison.OrdinalIgnoreCase));
                if (value != null) collector.Add(Diagnostic(value, TmpDiagnosticCode.InvalidTileSetSection, "registry", "TileSet section suffix is not exactly four decimal digits."), false);
                return null;
            }
            return index;
        }

        private static TmpDiagnostic Diagnostic(TheaterValueView value, TmpDiagnosticCode code, string stage, string message)
            => new TmpDiagnostic(BinaryDiagnosticSeverity.Error, code, value.Provenance == null ? throw new InvalidOperationException() : new BinarySourceContext("theater-registry", value.Provenance.SourceId, value.Provenance.LogicalChain[0]), value.Provenance, value.KeyPhysicalLineId, -1, stage, message);

        private static string Hash(TheaterControlDocument document, IEnumerable<TheaterTileSetDescriptor> sets, IEnumerable<TheaterTileIdRange> ranges, IEnumerable<TheaterSpecialRoleBinding> roles)
        {
            var builder = new StringBuilder();
            builder.Append(document.Profile.Id).Append('|');
            foreach (TheaterTileSetDescriptor set in sets.OrderBy(s => s.Index)) builder.Append(set.Index).Append(':').Append(set.FileNameRaw).Append(':').Append(set.TilesInSetRaw).Append('|');
            foreach (TheaterTileIdRange range in ranges) builder.Append(range.TileSetIndex).Append(':').Append(range.StartInclusive).Append(':').Append(range.EndExclusive).Append('|');
            foreach (TheaterSpecialRoleBinding role in roles.OrderBy(r => r.Role, StringComparer.Ordinal)) builder.Append(role.Role).Append(':').Append(role.Index).Append('|');
            using (SHA256 sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()))).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
