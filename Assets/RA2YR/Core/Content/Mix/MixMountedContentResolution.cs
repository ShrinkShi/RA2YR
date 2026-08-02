using System;
using System.Collections.Generic;
using System.Linq;

namespace RA2YR.Core.Content.Mix
{
    internal enum MixContentLayerKind
    {
        Directory,
        MixArchive
    }

    internal readonly struct MixContentLayerKey : IEquatable<MixContentLayerKey>
    {
        public MixContentLayerKey(
            string sourceId,
            MixContentLayerKind kind,
            LogicalContentPath layerPath)
        {
            if (!ContentConfigurationValueRules.IsValidSourceId(sourceId))
            {
                throw new ArgumentException("A valid source id is required.", nameof(sourceId));
            }

            if (!Enum.IsDefined(typeof(MixContentLayerKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            SourceId = sourceId;
            Kind = kind;
            LayerPath = layerPath ?? throw new ArgumentNullException(nameof(layerPath));
        }

        public string SourceId { get; }

        public MixContentLayerKind Kind { get; }

        public LogicalContentPath LayerPath { get; }

        public bool Equals(MixContentLayerKey other)
        {
            return string.Equals(SourceId, other.SourceId, StringComparison.OrdinalIgnoreCase) &&
                   Kind == other.Kind &&
                   LayerPath.Equals(other.LayerPath);
        }

        public override bool Equals(object obj)
        {
            return obj is MixContentLayerKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = StringComparer.OrdinalIgnoreCase.GetHashCode(SourceId);
                hash = (hash * 397) ^ (int)Kind;
                hash = (hash * 397) ^ LayerPath.GetHashCode();
                return hash;
            }
        }
    }

    internal sealed class MixLayerPrecedenceRule
    {
        public MixLayerPrecedenceRule(MixContentLayerKey layer, int priority)
        {
            Layer = layer;
            Priority = priority;
        }

        public MixContentLayerKey Layer { get; }

        public int Priority { get; }
    }

    internal sealed class MixLayerPrecedencePolicy
    {
        private readonly Dictionary<MixContentLayerKey, int> priorities;

        public MixLayerPrecedencePolicy(IEnumerable<MixLayerPrecedenceRule> rules)
        {
            MixLayerPrecedenceRule[] ruleArray =
                (rules ?? throw new ArgumentNullException(nameof(rules))).ToArray();
            if (ruleArray.Any(rule => rule == null))
            {
                throw new ArgumentException("Precedence rules may not contain null.", nameof(rules));
            }

            if (ruleArray.GroupBy(rule => rule.Layer).Any(group => group.Count() != 1))
            {
                throw new ArgumentException(
                    "Every content layer may have at most one explicit priority.",
                    nameof(rules));
            }

            priorities = ruleArray.ToDictionary(rule => rule.Layer, rule => rule.Priority);
        }

        public static MixLayerPrecedencePolicy None { get; } =
            new MixLayerPrecedencePolicy(Array.Empty<MixLayerPrecedenceRule>());

        public bool TryGetPriority(MixContentLayerKey layer, out int priority)
        {
            return priorities.TryGetValue(layer, out priority);
        }
    }

    internal sealed class MixMountedContentCandidate
    {
        public MixMountedContentCandidate(
            ExternalContentSourceDescriptor source,
            LogicalContentPath logicalPath,
            MixContentLayerKey layer,
            int? layerPriority,
            long length,
            string sha256,
            MixEntryProvenance mixProvenance)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            LogicalPath = logicalPath ?? throw new ArgumentNullException(nameof(logicalPath));
            if (!string.Equals(source.Id, layer.SourceId, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The layer must belong to the candidate source.", nameof(layer));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (sha256 != null && !Sha256Utilities.IsLowerSha256(sha256))
            {
                throw new ArgumentException("A lowercase SHA-256 value is required.", nameof(sha256));
            }

            if ((layer.Kind == MixContentLayerKind.MixArchive) != (mixProvenance != null))
            {
                throw new ArgumentException(
                    "Only MIX archive candidates carry MIX provenance.",
                    nameof(mixProvenance));
            }

            Layer = layer;
            LayerPriority = layerPriority;
            Length = length;
            Sha256 = sha256;
            MixProvenance = mixProvenance;
        }

        public ExternalContentSourceDescriptor Source { get; }

        public LogicalContentPath LogicalPath { get; }

        public MixContentLayerKey Layer { get; }

        public int? LayerPriority { get; }

        public long Length { get; }

        public string Sha256 { get; }

        public bool HasSha256 => Sha256 != null;

        public MixEntryProvenance MixProvenance { get; }
    }

    internal enum MixMountedResolutionDiagnosticCode
    {
        IncompleteDirectoryIndex,
        IncompleteMixMount,
        MountSourceMissing,
        MountSourceMismatch,
        AmbiguousExternalSourcePriority,
        MissingLayerPriority,
        AmbiguousLayerPriority,
        UnresolvedMixEntryIds
    }

    internal sealed class MixMountedResolutionDiagnostic
    {
        public MixMountedResolutionDiagnostic(
            MixMountedResolutionDiagnosticCode code,
            string message,
            LogicalContentPath logicalPath = null)
        {
            Code = code;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            LogicalPath = logicalPath;
        }

        public MixMountedResolutionDiagnosticCode Code { get; }

        public string Message { get; }

        public LogicalContentPath LogicalPath { get; }
    }

    internal sealed class MixMountedPathResolution
    {
        public MixMountedPathResolution(
            LogicalContentPath logicalPath,
            MixMountedContentCandidate selected,
            IEnumerable<MixMountedContentCandidate> provenanceChain,
            IEnumerable<MixMountedResolutionDiagnostic> diagnostics)
        {
            LogicalPath = logicalPath ?? throw new ArgumentNullException(nameof(logicalPath));
            MixMountedContentCandidate[] chain =
                (provenanceChain ?? throw new ArgumentNullException(nameof(provenanceChain)))
                .ToArray();
            if (chain.Length == 0 || chain.Any(candidate => candidate == null))
            {
                throw new ArgumentException("A provenance chain cannot be empty.", nameof(provenanceChain));
            }

            if (selected != null && !chain.Contains(selected))
            {
                throw new ArgumentException("The selected candidate must be in its chain.", nameof(selected));
            }

            Selected = selected;
            ProvenanceChain = Array.AsReadOnly(chain);
            Diagnostics = Array.AsReadOnly(
                (diagnostics ?? Enumerable.Empty<MixMountedResolutionDiagnostic>()).ToArray());
        }

        public LogicalContentPath LogicalPath { get; }

        public MixMountedContentCandidate Selected { get; }

        public IReadOnlyList<MixMountedContentCandidate> ProvenanceChain { get; }

        public IReadOnlyList<MixMountedResolutionDiagnostic> Diagnostics { get; }

        public bool IsResolved => Selected != null && Diagnostics.Count == 0;
    }

    internal sealed class MixMountedContentResolutionResult
    {
        public MixMountedContentResolutionResult(
            IEnumerable<MixMountedPathResolution> entries,
            IEnumerable<MixMountedResolutionDiagnostic> diagnostics,
            long unresolvedMixEntryCount,
            bool trustedCompleteInput)
        {
            if (unresolvedMixEntryCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(unresolvedMixEntryCount));
            }

            MixMountedPathResolution[] entryArray =
                (entries ?? throw new ArgumentNullException(nameof(entries))).ToArray();
            MixMountedResolutionDiagnostic[] diagnosticArray =
                (diagnostics ?? Enumerable.Empty<MixMountedResolutionDiagnostic>()).ToArray();
            Entries = Array.AsReadOnly(entryArray);
            Diagnostics = Array.AsReadOnly(diagnosticArray);
            UnresolvedMixEntryCount = unresolvedMixEntryCount;
            HasErrors = diagnosticArray.Length != 0 ||
                        entryArray.Any(entry => !entry.IsResolved);
            IsComplete = trustedCompleteInput && !HasErrors;
            HasAuditedDigests = unresolvedMixEntryCount == 0 && entryArray
                .SelectMany(entry => entry.ProvenanceChain)
                .All(candidate => candidate.HasSha256);
        }

        public IReadOnlyList<MixMountedPathResolution> Entries { get; }

        public IReadOnlyList<MixMountedResolutionDiagnostic> Diagnostics { get; }

        public bool HasErrors { get; }

        public bool IsComplete { get; }

        public bool HasAuditedDigests { get; }

        public long UnresolvedMixEntryCount { get; }
    }

    internal sealed class MixMountedContentResolver
    {
        private static readonly LogicalContentPath DirectoryLayerPath =
            LogicalContentPath.Parse("_directory");

        public MixMountedContentResolutionResult Resolve(
            ContentIndexResult directoryIndex,
            IEnumerable<MixVirtualContentMountResult> mounts,
            MixLayerPrecedencePolicy layerPolicy = null)
        {
            if (directoryIndex == null)
            {
                throw new ArgumentNullException(nameof(directoryIndex));
            }

            MixVirtualContentMountResult[] mountArray =
                (mounts ?? throw new ArgumentNullException(nameof(mounts))).ToArray();
            if (mountArray.Any(mount => mount == null))
            {
                throw new ArgumentException("MIX mounts may not contain null.", nameof(mounts));
            }

            MixLayerPrecedencePolicy policy = layerPolicy ?? MixLayerPrecedencePolicy.None;
            var diagnostics = new List<MixMountedResolutionDiagnostic>();
            bool trustedCompleteInput = directoryIndex.IsComplete;
            long unresolvedMixEntryCount = 0;
            if (!directoryIndex.IsComplete)
            {
                diagnostics.Add(new MixMountedResolutionDiagnostic(
                    MixMountedResolutionDiagnosticCode.IncompleteDirectoryIndex,
                    "Mounted content resolution requires a complete directory index."));
            }

            Dictionary<string, ContentSourceIndex> directorySources = directoryIndex.Sources
                .ToDictionary(source => source.Source.Id, StringComparer.OrdinalIgnoreCase);
            foreach (MixVirtualContentMountResult mount in mountArray)
            {
                ContentSourceIndex directorySource;
                if (!directorySources.TryGetValue(mount.Source.Id, out directorySource))
                {
                    diagnostics.Add(new MixMountedResolutionDiagnostic(
                        MixMountedResolutionDiagnosticCode.MountSourceMissing,
                        "A MIX mount does not have a corresponding directory source."));
                    trustedCompleteInput = false;
                    continue;
                }

                if (!DescriptorsMatch(directorySource.Source, mount.Source))
                {
                    diagnostics.Add(new MixMountedResolutionDiagnostic(
                        MixMountedResolutionDiagnosticCode.MountSourceMismatch,
                        "A MIX mount source descriptor differs from its directory source."));
                    trustedCompleteInput = false;
                }

                if (!mount.IsComplete)
                {
                    diagnostics.Add(new MixMountedResolutionDiagnostic(
                        MixMountedResolutionDiagnosticCode.IncompleteMixMount,
                        "An incomplete MIX mount cannot participate in trusted resolution."));
                    trustedCompleteInput = false;
                }

                try
                {
                    unresolvedMixEntryCount = checked(
                        unresolvedMixEntryCount +
                        mount.Entries.LongCount(entry => !entry.HasResolvedName));
                }
                catch (OverflowException)
                {
                    unresolvedMixEntryCount = long.MaxValue;
                }
            }

            if (unresolvedMixEntryCount != 0)
            {
                diagnostics.Add(new MixMountedResolutionDiagnostic(
                    MixMountedResolutionDiagnosticCode.UnresolvedMixEntryIds,
                    "Mounted MIX content contains unresolved numeric IDs and cannot be claimed as complete."));
                trustedCompleteInput = false;
            }

            var candidates = new List<MixMountedContentCandidate>();
            foreach (ContentSourceIndex source in directoryIndex.Sources)
            {
                var layer = new MixContentLayerKey(
                    source.Source.Id,
                    MixContentLayerKind.Directory,
                    DirectoryLayerPath);
                int layerPriorityValue;
                int? layerPriority = policy.TryGetPriority(layer, out layerPriorityValue)
                    ? layerPriorityValue
                    : (int?)null;
                candidates.AddRange(source.Files.Select(file =>
                    new MixMountedContentCandidate(
                        source.Source,
                        file.LogicalPath,
                        layer,
                        layerPriority,
                        file.Length,
                        file.Sha256,
                        null)));
            }

            foreach (MixVirtualContentMountResult mount in mountArray.Where(item => item.IsComplete))
            {
                foreach (MixVirtualEntry entry in mount.Entries.Where(item => item.HasResolvedName))
                {
                    var layer = new MixContentLayerKey(
                        mount.Source.Id,
                        MixContentLayerKind.MixArchive,
                        entry.Provenance.Steps.Last().ArchivePath);
                    int layerPriorityValue;
                    int? layerPriority = policy.TryGetPriority(layer, out layerPriorityValue)
                        ? layerPriorityValue
                        : (int?)null;
                    candidates.Add(new MixMountedContentCandidate(
                        mount.Source,
                        entry.LogicalName,
                        layer,
                        layerPriority,
                        entry.Length,
                        entry.Sha256,
                        entry.Provenance));
                }
            }

            var entries = new List<MixMountedPathResolution>();
            foreach (IGrouping<LogicalContentPath, MixMountedContentCandidate> group in candidates
                         .GroupBy(candidate => candidate.LogicalPath)
                         .OrderBy(group => group.Key, LogicalContentPathReportComparer.Instance))
            {
                MixMountedContentCandidate[] chain = group
                    .OrderByDescending(candidate => candidate.Source.Priority)
                    .ThenBy(candidate => candidate.Source.Id, StringComparer.Ordinal)
                    .ThenByDescending(candidate => candidate.LayerPriority.HasValue)
                    .ThenByDescending(candidate => candidate.LayerPriority ?? int.MinValue)
                    .ThenBy(candidate => candidate.Layer.Kind)
                    .ThenBy(
                        candidate => candidate.Layer.LayerPath,
                        LogicalContentPathReportComparer.Instance)
                    .ToArray();
                int topSourcePriority = chain[0].Source.Priority;
                MixMountedContentCandidate[] top = chain
                    .Where(candidate => candidate.Source.Priority == topSourcePriority)
                    .ToArray();
                MixMountedContentCandidate selected = null;
                var entryDiagnostics = new List<MixMountedResolutionDiagnostic>();
                string[] topSourceIds = top
                    .Select(candidate => candidate.Source.Id)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (topSourceIds.Length != 1)
                {
                    AddAmbiguity(
                        entryDiagnostics,
                        diagnostics,
                        MixMountedResolutionDiagnosticCode.AmbiguousExternalSourcePriority,
                        "Multiple external sources share the highest priority; source id was not used as a tiebreaker.",
                        group.Key);
                }
                else if (top.Length == 1)
                {
                    selected = top[0];
                }
                else if (top.Any(candidate => !candidate.LayerPriority.HasValue))
                {
                    AddAmbiguity(
                        entryDiagnostics,
                        diagnostics,
                        MixMountedResolutionDiagnosticCode.MissingLayerPriority,
                        "Same-source directory/MIX candidates require explicit layer priorities.",
                        group.Key);
                }
                else
                {
                    int highestLayerPriority = top.Max(candidate => candidate.LayerPriority.Value);
                    MixMountedContentCandidate[] highestLayers = top
                        .Where(candidate => candidate.LayerPriority.Value == highestLayerPriority)
                        .ToArray();
                    if (highestLayers.Length != 1)
                    {
                        AddAmbiguity(
                            entryDiagnostics,
                            diagnostics,
                            MixMountedResolutionDiagnosticCode.AmbiguousLayerPriority,
                            "Multiple same-source layers share the highest explicit layer priority.",
                            group.Key);
                    }
                    else
                    {
                        selected = highestLayers[0];
                    }
                }

                entries.Add(new MixMountedPathResolution(
                    group.Key,
                    selected,
                    chain,
                    entryDiagnostics));
            }

            return new MixMountedContentResolutionResult(
                entries,
                diagnostics,
                unresolvedMixEntryCount,
                trustedCompleteInput);
        }

        private static bool DescriptorsMatch(
            ExternalContentSourceDescriptor left,
            ExternalContentSourceDescriptor right)
        {
            return string.Equals(left.Id, right.Id, StringComparison.OrdinalIgnoreCase) &&
                   left.Kind == right.Kind &&
                   left.Priority == right.Priority &&
                   string.Equals(left.Version, right.Version, StringComparison.Ordinal) &&
                   string.Equals(left.RootPath, right.RootPath, StringComparison.OrdinalIgnoreCase);
        }

        private static void AddAmbiguity(
            ICollection<MixMountedResolutionDiagnostic> entryDiagnostics,
            ICollection<MixMountedResolutionDiagnostic> globalDiagnostics,
            MixMountedResolutionDiagnosticCode code,
            string message,
            LogicalContentPath path)
        {
            var diagnostic = new MixMountedResolutionDiagnostic(code, message, path);
            entryDiagnostics.Add(diagnostic);
            globalDiagnostics.Add(diagnostic);
        }
    }
}
