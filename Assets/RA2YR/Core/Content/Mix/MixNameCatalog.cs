using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Formats.Mix;

namespace RA2YR.Core.Content.Mix
{
    internal static class MixLegacyVisualArchiveProfile
    {
        private static readonly IReadOnlyList<LogicalContentPath> visualChildren = Array.AsReadOnly(new[]
        {
            LogicalContentPath.Parse("cache.mix"),
            LogicalContentPath.Parse("cachemd.mix"),
            LogicalContentPath.Parse("conquer.mix"),
            LogicalContentPath.Parse("conqmd.mix"),
            LogicalContentPath.Parse("generic.mix"),
            LogicalContentPath.Parse("genericmd.mix"),
            LogicalContentPath.Parse("temperat.mix"),
            LogicalContentPath.Parse("snow.mix"),
            LogicalContentPath.Parse("temmd.mix"),
            LogicalContentPath.Parse("snowmd.mix"),
            LogicalContentPath.Parse("urbmd.mix"),
            LogicalContentPath.Parse("ubnmd.mix"),
            LogicalContentPath.Parse("desmd.mix"),
            LogicalContentPath.Parse("lunmd.mix")
        });

        public static IReadOnlyList<LogicalContentPath> VisualChildren => visualChildren;
    }

    internal sealed class MixNameCatalogCandidate
    {
        public MixNameCatalogCandidate(LogicalContentPath logicalName)
            : this(
                MixFileId.ComputeCandidateId(
                    (logicalName ?? throw new ArgumentNullException(nameof(logicalName))).Value),
                logicalName)
        {
        }

        internal MixNameCatalogCandidate(
            MixFileId id,
            LogicalContentPath logicalName)
        {
            Id = id;
            LogicalName = logicalName ?? throw new ArgumentNullException(nameof(logicalName));
        }

        public MixFileId Id { get; }

        public LogicalContentPath LogicalName { get; }
    }

    internal sealed class MixNameCatalog
    {
        private readonly Dictionary<MixFileId, LogicalContentPath> resolved;
        private readonly HashSet<MixFileId> ambiguous;

        public MixNameCatalog(IEnumerable<LogicalContentPath> candidateNames)
            : this((candidateNames ?? throw new ArgumentNullException(nameof(candidateNames)))
                .Select(name => new MixNameCatalogCandidate(name)))
        {
        }

        internal MixNameCatalog(IEnumerable<MixNameCatalogCandidate> candidates)
        {
            MixNameCatalogCandidate[] candidateArray =
                (candidates ?? throw new ArgumentNullException(nameof(candidates))).ToArray();
            if (candidateArray.Any(candidate => candidate == null))
            {
                throw new ArgumentException("Name candidates may not contain null.", nameof(candidates));
            }

            resolved = new Dictionary<MixFileId, LogicalContentPath>();
            ambiguous = new HashSet<MixFileId>();
            foreach (IGrouping<MixFileId, MixNameCatalogCandidate> group in candidateArray
                         .GroupBy(candidate => candidate.Id)
                         .OrderBy(group => group.Key))
            {
                LogicalContentPath[] distinctNames = group
                    .Select(candidate => candidate.LogicalName)
                    .GroupBy(name => name)
                    .Select(nameGroup => nameGroup
                        .OrderBy(name => name, LogicalContentPathReportComparer.Instance)
                        .First())
                    .OrderBy(name => name, LogicalContentPathReportComparer.Instance)
                    .ToArray();

                if (distinctNames.Length == 1)
                {
                    resolved.Add(group.Key, distinctNames[0]);
                }
                else
                {
                    ambiguous.Add(group.Key);
                }
            }
        }

        public int ResolvedIdCount => resolved.Count;

        public int AmbiguousIdCount => ambiguous.Count;

        public bool TryResolve(MixFileId id, out LogicalContentPath logicalName)
        {
            return resolved.TryGetValue(id, out logicalName);
        }

        public bool IsAmbiguous(MixFileId id)
        {
            return ambiguous.Contains(id);
        }
    }
}
