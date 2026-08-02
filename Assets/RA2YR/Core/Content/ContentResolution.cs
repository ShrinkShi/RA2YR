using System;
using System.Collections.Generic;
using System.Linq;

namespace RA2YR.Core.Content
{
    public sealed class ContentResolutionSource
    {
        internal ContentResolutionSource(ContentSourceIndex index)
        {
            if (index == null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            Id = index.Source.Id;
            Kind = index.Source.Kind;
            Priority = index.Source.Priority;
            Version = index.Source.Version;
            Fingerprint = index.Fingerprint;
            FileCount = index.Files.Count;
            TotalBytes = index.Files.Sum(file => file.Length);
            RootPath = index.Source.RootPath;
        }

        public string Id { get; }

        public ContentSourceKind Kind { get; }

        public int Priority { get; }

        public string Version { get; }

        public string Fingerprint { get; }

        public int FileCount { get; }

        public long TotalBytes { get; }

        internal string RootPath { get; }
    }

    public sealed class ContentProvenanceCandidate
    {
        internal ContentProvenanceCandidate(
            ContentResolutionSource source,
            ContentFileRecord file)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (!string.Equals(source.Id, file.SourceId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "The provenance candidate must belong to its source.",
                    nameof(file));
            }

            LogicalPath = file.LogicalPath;
            SourceRelativePath = file.RelativePath;
            Length = file.Length;
            Sha256 = file.Sha256;
        }

        public LogicalContentPath LogicalPath { get; }

        public ContentResolutionSource Source { get; }

        public string SourceRelativePath { get; }

        public long Length { get; }

        public string Sha256 { get; }
    }

    public sealed class ContentPathResolution
    {
        internal ContentPathResolution(
            LogicalContentPath logicalPath,
            ContentProvenanceCandidate selected,
            IEnumerable<ContentProvenanceCandidate> provenanceChain,
            IEnumerable<ContentDiagnostic> diagnostics)
        {
            LogicalPath = logicalPath ?? throw new ArgumentNullException(nameof(logicalPath));
            ContentProvenanceCandidate[] chain =
                (provenanceChain ?? throw new ArgumentNullException(nameof(provenanceChain)))
                .ToArray();
            ContentDiagnostic[] diagnosticArray =
                (diagnostics ?? Enumerable.Empty<ContentDiagnostic>()).ToArray();
            if (chain.Length == 0 || chain.Any(candidate => candidate == null))
            {
                throw new ArgumentException(
                    "A provenance chain must contain at least one candidate.",
                    nameof(provenanceChain));
            }

            if (diagnosticArray.Any(diagnostic => diagnostic == null))
            {
                throw new ArgumentException(
                    "Resolution diagnostics may not contain null.",
                    nameof(diagnostics));
            }

            if (selected != null && !chain.Contains(selected))
            {
                throw new ArgumentException(
                    "The selected candidate must be part of the provenance chain.",
                    nameof(selected));
            }

            Selected = selected;
            ProvenanceChain = Array.AsReadOnly(chain);
            Diagnostics = Array.AsReadOnly(diagnosticArray);
        }

        public LogicalContentPath LogicalPath { get; }

        public ContentProvenanceCandidate Selected { get; }

        public IReadOnlyList<ContentProvenanceCandidate> ProvenanceChain { get; }

        public IReadOnlyList<ContentDiagnostic> Diagnostics { get; }

        public bool IsResolved => Selected != null && Diagnostics.All(
            diagnostic => diagnostic.Severity != ContentDiagnosticSeverity.Error);

        public IReadOnlyList<ContentProvenanceCandidate> OverriddenCandidates =>
            Array.AsReadOnly(ProvenanceChain
                .Where(candidate => !ReferenceEquals(candidate, Selected))
                .ToArray());
    }

    public sealed class ContentResolutionResult
    {
        internal ContentResolutionResult(
            IEnumerable<ContentResolutionSource> sources,
            IEnumerable<ContentPathResolution> entries,
            IEnumerable<ContentDiagnostic> diagnostics,
            bool trustedCompleteInput)
        {
            ContentResolutionSource[] sourceArray =
                (sources ?? throw new ArgumentNullException(nameof(sources))).ToArray();
            ContentPathResolution[] entryArray =
                (entries ?? throw new ArgumentNullException(nameof(entries))).ToArray();
            ContentDiagnostic[] diagnosticArray =
                (diagnostics ?? Enumerable.Empty<ContentDiagnostic>()).ToArray();
            if (sourceArray.Any(source => source == null) ||
                entryArray.Any(entry => entry == null) ||
                diagnosticArray.Any(diagnostic => diagnostic == null))
            {
                throw new ArgumentException("Resolution collections may not contain null.");
            }

            if (sourceArray
                .GroupBy(source => source.Id, StringComparer.OrdinalIgnoreCase)
                .Any(group => group.Count() != 1))
            {
                throw new ArgumentException("Resolution source ids must be unique.", nameof(sources));
            }

            if (entryArray
                .GroupBy(entry => entry.LogicalPath)
                .Any(group => group.Count() != 1))
            {
                throw new ArgumentException(
                    "Resolution entries must have unique logical paths.",
                    nameof(entries));
            }

            Sources = Array.AsReadOnly(sourceArray);
            Entries = Array.AsReadOnly(entryArray);
            Diagnostics = Array.AsReadOnly(diagnosticArray);
            HasErrors = diagnosticArray.Any(
                diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Error);
            IsComplete = trustedCompleteInput &&
                         sourceArray.Length > 0 &&
                         !HasErrors &&
                         entryArray.All(entry => entry.IsResolved);
        }

        public IReadOnlyList<ContentResolutionSource> Sources { get; }

        public IReadOnlyList<ContentPathResolution> Entries { get; }

        public IReadOnlyList<ContentDiagnostic> Diagnostics { get; }

        public bool HasErrors { get; }

        public bool IsComplete { get; }
    }

    public sealed class ContentResolver
    {
        public ContentResolutionResult Resolve(ContentIndexResult index)
        {
            if (index == null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            ContentResolutionSource[] sources = index.Sources
                .Select(source => new ContentResolutionSource(source))
                .OrderByDescending(source => source.Priority)
                .ThenBy(source => source.Id, StringComparer.Ordinal)
                .ToArray();
            var sourcesById = sources.ToDictionary(
                source => source.Id,
                StringComparer.Ordinal);
            var diagnostics = new List<ContentDiagnostic>(index.Diagnostics);

            if (!index.IsComplete)
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.ResolutionInputIncomplete,
                    "Content resolution requires a complete, error-free source index."));
            }

            var conflictedPathsBySource = new Dictionary<string, HashSet<LogicalContentPath>>(
                StringComparer.Ordinal);
            foreach (ContentSourceIndex sourceIndex in index.Sources)
            {
                var conflicts = new HashSet<LogicalContentPath>();
                foreach (IGrouping<LogicalContentPath, ContentFileRecord> group in sourceIndex.Files
                             .GroupBy(file => file.LogicalPath)
                             .Where(group => group.Count() > 1)
                             .OrderBy(group => group.Key, LogicalContentPathReportComparer.Instance))
                {
                    conflicts.Add(group.Key);
                    string[] actualPaths = group
                        .Select(file => file.RelativePath)
                        .OrderBy(path => path, StringComparer.Ordinal)
                        .ToArray();
                    diagnostics.Add(new ContentDiagnostic(
                        ContentDiagnosticSeverity.Error,
                        ContentDiagnosticCode.SourceLogicalPathConflict,
                        "Source-internal case collision; no candidate may be selected: " +
                        string.Join(", ", actualPaths),
                        sourceIndex.Source.Id,
                        group.Key.Value));
                }

                conflictedPathsBySource.Add(sourceIndex.Source.Id, conflicts);
            }

            var candidates = new List<ContentProvenanceCandidate>();
            foreach (ContentSourceIndex sourceIndex in index.Sources)
            {
                ContentResolutionSource safeSource = sourcesById[sourceIndex.Source.Id];
                candidates.AddRange(sourceIndex.Files.Select(
                    file => new ContentProvenanceCandidate(safeSource, file)));
            }

            var entries = new List<ContentPathResolution>();
            IEnumerable<IGrouping<LogicalContentPath, ContentProvenanceCandidate>> groups =
                candidates
                    .GroupBy(candidate => candidate.LogicalPath)
                    .OrderBy(group => group.Key, LogicalContentPathReportComparer.Instance);
            foreach (IGrouping<LogicalContentPath, ContentProvenanceCandidate> group in groups)
            {
                ContentProvenanceCandidate[] chain = group
                    .OrderByDescending(candidate => candidate.Source.Priority)
                    .ThenBy(candidate => candidate.Source.Id, StringComparer.Ordinal)
                    .ThenBy(candidate => candidate.SourceRelativePath, StringComparer.Ordinal)
                    .ToArray();
                string displayPath = chain
                    .Select(candidate => candidate.SourceRelativePath)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(path => path, StringComparer.Ordinal)
                    .First();
                var entryDiagnostics = new List<ContentDiagnostic>();

                bool hasInternalConflict = chain.Any(candidate =>
                    conflictedPathsBySource[candidate.Source.Id].Contains(candidate.LogicalPath));
                ContentProvenanceCandidate selected = null;
                if (hasInternalConflict)
                {
                    entryDiagnostics.Add(new ContentDiagnostic(
                        ContentDiagnosticSeverity.Error,
                        ContentDiagnosticCode.SourceLogicalPathConflict,
                        "At least one source has multiple physical files for this logical path.",
                        path: displayPath));
                }
                else
                {
                    int highestPriority = chain[0].Source.Priority;
                    ContentProvenanceCandidate[] highestCandidates = chain
                        .Where(candidate => candidate.Source.Priority == highestPriority)
                        .ToArray();
                    if (highestCandidates.Length != 1)
                    {
                        string sourceIds = string.Join(
                            ", ",
                            highestCandidates
                                .Select(candidate => candidate.Source.Id)
                                .OrderBy(id => id, StringComparer.Ordinal));
                        var diagnostic = new ContentDiagnostic(
                            ContentDiagnosticSeverity.Error,
                            ContentDiagnosticCode.AmbiguousContentResolution,
                            "Multiple enabled sources share the highest priority " +
                            highestPriority + " for this logical path: " + sourceIds,
                            path: displayPath);
                        entryDiagnostics.Add(diagnostic);
                        diagnostics.Add(diagnostic);
                    }
                    else
                    {
                        selected = highestCandidates[0];
                        displayPath = selected.SourceRelativePath;
                    }
                }

                entries.Add(new ContentPathResolution(
                    LogicalContentPath.Parse(displayPath),
                    selected,
                    chain,
                    entryDiagnostics));
            }

            return new ContentResolutionResult(
                sources,
                entries.OrderBy(
                    entry => entry.LogicalPath,
                    LogicalContentPathReportComparer.Instance),
                diagnostics,
                index.IsComplete);
        }
    }
}
