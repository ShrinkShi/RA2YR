using System;
using System.Collections.Generic;
using System.Linq;

namespace RA2YR.Core.Content
{
    public sealed class ContentFileRecord
    {
        internal ContentFileRecord(
            string sourceId,
            string relativePath,
            long length,
            string sha256)
            : this(sourceId, LogicalContentPath.Parse(relativePath), length, sha256)
        {
        }

        internal ContentFileRecord(
            string sourceId,
            LogicalContentPath logicalPath,
            long length,
            string sha256)
        {
            if (!ContentConfigurationValueRules.IsValidSourceId(sourceId))
            {
                throw new ArgumentException("A valid source id is required.", nameof(sourceId));
            }

            LogicalPath = logicalPath ?? throw new ArgumentNullException(nameof(logicalPath));

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (!Sha256Utilities.IsLowerSha256(sha256))
            {
                throw new ArgumentException(
                    "A lowercase SHA-256 value is required.",
                    nameof(sha256));
            }

            SourceId = sourceId;
            RelativePath = logicalPath.Value;
            Length = length;
            Sha256 = sha256;
        }

        public string SourceId { get; }

        public string RelativePath { get; }

        public LogicalContentPath LogicalPath { get; }

        public long Length { get; }

        public string Sha256 { get; }
    }

    public sealed class ContentSourceIndex
    {
        internal ContentSourceIndex(
            ExternalContentSourceDescriptor source,
            IEnumerable<ContentFileRecord> files,
            string fingerprint,
            bool isComplete)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            if (!source.Enabled)
            {
                throw new ArgumentException(
                    "Only enabled sources can produce a source index.",
                    nameof(source));
            }
            ContentFileRecord[] fileArray =
                (files ?? throw new ArgumentNullException(nameof(files))).ToArray();
            if (fileArray.Any(file => file == null))
            {
                throw new ArgumentException("File records may not contain null.", nameof(files));
            }

            if (fileArray.Any(file =>
                    !string.Equals(file.SourceId, source.Id, StringComparison.Ordinal)))
            {
                throw new ArgumentException(
                    "Every file record must belong to the source index.",
                    nameof(files));
            }

            if (fileArray
                .GroupBy(file => file.RelativePath, StringComparer.Ordinal)
                .Any(group => group.Count() != 1))
            {
                throw new ArgumentException(
                    "File paths must be unique within a source index.",
                    nameof(files));
            }

            if (!Sha256Utilities.IsLowerSha256(fingerprint))
            {
                throw new ArgumentException(
                    "A lowercase SHA-256 source fingerprint is required.",
                    nameof(fingerprint));
            }

            string expectedFingerprint = ContentSourceFingerprint.Compute(source, fileArray);
            if (!string.Equals(fingerprint, expectedFingerprint, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "The source fingerprint does not match the indexed source metadata.",
                    nameof(fingerprint));
            }

            Files = Array.AsReadOnly(fileArray);
            Fingerprint = fingerprint;
            IsComplete = isComplete;
        }

        public ExternalContentSourceDescriptor Source { get; }

        public IReadOnlyList<ContentFileRecord> Files { get; }

        public string Fingerprint { get; }

        public bool IsComplete { get; }
    }

    public sealed class ContentIndexResult
    {
        internal ContentIndexResult(
            IEnumerable<ContentSourceIndex> sources,
            IEnumerable<ContentDiagnostic> diagnostics)
        {
            ContentSourceIndex[] sourceArray =
                (sources ?? throw new ArgumentNullException(nameof(sources))).ToArray();
            ContentDiagnostic[] diagnosticArray =
                (diagnostics ?? Enumerable.Empty<ContentDiagnostic>()).ToArray();
            if (sourceArray.Any(source => source == null))
            {
                throw new ArgumentException("Source indexes may not contain null.", nameof(sources));
            }

            if (diagnosticArray.Any(diagnostic => diagnostic == null))
            {
                throw new ArgumentException("Diagnostics may not contain null.", nameof(diagnostics));
            }

            if (sourceArray
                .GroupBy(source => source.Source.Id, StringComparer.OrdinalIgnoreCase)
                .Any(group => group.Count() != 1))
            {
                throw new ArgumentException("Source ids must be unique.", nameof(sources));
            }

            bool containsErrors = diagnosticArray.Any(
                item => item.Severity == ContentDiagnosticSeverity.Error);
            bool containsIncompleteSource = sourceArray.Any(source => !source.IsComplete);
            Sources = Array.AsReadOnly(sourceArray);
            Diagnostics = Array.AsReadOnly(diagnosticArray);
            IsComplete = sourceArray.Length > 0 &&
                         !containsErrors &&
                         !containsIncompleteSource;
        }

        public IReadOnlyList<ContentSourceIndex> Sources { get; }

        public IReadOnlyList<ContentDiagnostic> Diagnostics { get; }

        public bool HasErrors => Diagnostics.Any(
            item => item.Severity == ContentDiagnosticSeverity.Error);

        public bool IsComplete { get; }
    }

}
