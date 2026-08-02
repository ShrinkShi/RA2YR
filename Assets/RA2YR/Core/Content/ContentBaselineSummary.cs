using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace RA2YR.Core.Content
{
    public sealed class ContentExtensionAggregate
    {
        internal ContentExtensionAggregate(string extension, int fileCount, long totalBytes)
        {
            Extension = extension ?? throw new ArgumentNullException(nameof(extension));
            FileCount = fileCount;
            TotalBytes = totalBytes;
        }

        public string Extension { get; }

        public int FileCount { get; }

        public long TotalBytes { get; }
    }

    public sealed class ContentRepresentativeFile
    {
        internal ContentRepresentativeFile(ContentProvenanceCandidate candidate)
        {
            if (candidate == null)
            {
                throw new ArgumentNullException(nameof(candidate));
            }

            LogicalPath = candidate.SourceRelativePath;
            Length = candidate.Length;
            Sha256 = candidate.Sha256;
        }

        public string LogicalPath { get; }

        public long Length { get; }

        public string Sha256 { get; }
    }

    public sealed class ContentBaselineSummary
    {
        internal ContentBaselineSummary(
            string baselineLogicalName,
            int manifestSchemaVersion,
            string manifestSha256,
            int totalFileCount,
            long totalBytes,
            string sourceVersion,
            DateTime indexStartedUtc,
            DateTime indexCompletedUtc,
            int diagnosticCount,
            bool changesDetected,
            IEnumerable<ContentExtensionAggregate> extensionAggregates,
            IEnumerable<ContentRepresentativeFile> representatives,
            IEnumerable<string> contentNotes,
            string directoryVisibilityStatement)
        {
            BaselineLogicalName = baselineLogicalName;
            ManifestSchemaVersion = manifestSchemaVersion;
            ManifestSha256 = manifestSha256;
            TotalFileCount = totalFileCount;
            TotalBytes = totalBytes;
            SourceVersion = sourceVersion;
            IndexStartedUtc = DateTime.SpecifyKind(indexStartedUtc, DateTimeKind.Utc);
            IndexCompletedUtc = DateTime.SpecifyKind(indexCompletedUtc, DateTimeKind.Utc);
            DiagnosticCount = diagnosticCount;
            ChangesDetected = changesDetected;
            ExtensionAggregates = Array.AsReadOnly(extensionAggregates.ToArray());
            Representatives = Array.AsReadOnly(representatives.ToArray());
            ContentNotes = Array.AsReadOnly(contentNotes.ToArray());
            DirectoryVisibilityStatement = directoryVisibilityStatement;
        }

        public string BaselineLogicalName { get; }

        public int ManifestSchemaVersion { get; }

        public string ManifestSha256 { get; }

        public int TotalFileCount { get; }

        public long TotalBytes { get; }

        public string SourceVersion { get; }

        public DateTime IndexStartedUtc { get; }

        public DateTime IndexCompletedUtc { get; }

        public int DiagnosticCount { get; }

        public bool ChangesDetected { get; }

        public IReadOnlyList<ContentExtensionAggregate> ExtensionAggregates { get; }

        public IReadOnlyList<ContentRepresentativeFile> Representatives { get; }

        public IReadOnlyList<string> ContentNotes { get; }

        public string DirectoryVisibilityStatement { get; }
    }

    public static class ContentBaselineSummaryBuilder
    {
        public static ContentBaselineSummary Build(
            string baselineLogicalName,
            ContentResolutionResult resolution,
            ExternalManifestWriteResult manifest,
            DateTime indexStartedUtc,
            DateTime indexCompletedUtc,
            IEnumerable<string> approvedRepresentativePaths,
            IEnumerable<string> contentNotes,
            string directoryVisibilityStatement)
        {
            if (!ContentConfigurationValueRules.IsValidSourceId(baselineLogicalName))
            {
                throw new ArgumentException(
                    "The baseline logical name must be a safe source identifier.",
                    nameof(baselineLogicalName));
            }

            if (resolution == null || !resolution.IsComplete || resolution.HasErrors)
            {
                throw new InvalidOperationException(
                    "A public summary requires a complete, error-free resolution.");
            }

            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }

            if (indexStartedUtc.Kind != DateTimeKind.Utc ||
                indexCompletedUtc.Kind != DateTimeKind.Utc ||
                indexCompletedUtc < indexStartedUtc)
            {
                throw new ArgumentException("Index timestamps must be ordered UTC values.");
            }

            ContentResolutionSource baselineSource = resolution.Sources.SingleOrDefault(
                source => string.Equals(
                    source.Id,
                    baselineLogicalName,
                    StringComparison.Ordinal));
            if (baselineSource == null)
            {
                throw new InvalidOperationException(
                    "The requested baseline is not present in the resolution source set.");
            }

            string[] representativePathValues =
                (approvedRepresentativePaths ?? Enumerable.Empty<string>()).ToArray();
            if (representativePathValues
                .Select(LogicalContentPath.Parse)
                .Distinct()
                .Count() != representativePathValues.Length)
            {
                throw new ArgumentException(
                    "Approved representative paths must be logically unique.",
                    nameof(approvedRepresentativePaths));
            }

            ContentRepresentativeFile[] representatives = representativePathValues
                .Select(LogicalContentPath.Parse)
                .Select(path =>
                {
                    ContentPathResolution entry = resolution.Entries.SingleOrDefault(
                        candidate => candidate.LogicalPath.Equals(path));
                    if (entry == null || entry.Selected == null ||
                        !string.Equals(
                            entry.Selected.Source.Id,
                            baselineLogicalName,
                            StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            "An approved representative was not selected from the baseline: " +
                            path.Value);
                    }

                    return new ContentRepresentativeFile(entry.Selected);
                })
                .OrderBy(item => item.LogicalPath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.LogicalPath, StringComparer.Ordinal)
                .ToArray();

            string[] notes = (contentNotes ?? Enumerable.Empty<string>()).ToArray();
            if (notes.Any(note => !IsSafePublicText(note)))
            {
                throw new ArgumentException("Content notes contain unsafe text.", nameof(contentNotes));
            }

            if (!IsSafePublicText(directoryVisibilityStatement))
            {
                throw new ArgumentException(
                    "The directory visibility statement contains unsafe text.",
                    nameof(directoryVisibilityStatement));
            }

            ContentExtensionAggregate[] aggregates = resolution.Entries
                .Select(entry => entry.Selected)
                .GroupBy(candidate => NormalizeExtension(candidate.SourceRelativePath))
                .Select(group => new ContentExtensionAggregate(
                    group.Key,
                    group.Count(),
                    group.Sum(candidate => candidate.Length)))
                .OrderBy(item => item.Extension, StringComparer.Ordinal)
                .ToArray();
            bool changesDetected = resolution.Diagnostics.Any(diagnostic =>
                diagnostic.Code == ContentDiagnosticCode.FileChangedDuringHash ||
                diagnostic.Code == ContentDiagnosticCode.SourceTreeChangedDuringIndex);

            return new ContentBaselineSummary(
                baselineLogicalName,
                manifest.SchemaVersion,
                manifest.Sha256,
                resolution.Entries.Count,
                resolution.Entries.Sum(entry => entry.Selected.Length),
                baselineSource.Version,
                indexStartedUtc,
                indexCompletedUtc,
                resolution.Diagnostics.Count,
                changesDetected,
                aggregates,
                representatives,
                notes,
                directoryVisibilityStatement);
        }

        private static string NormalizeExtension(string path)
        {
            string extension = Path.GetExtension(path);
            return string.IsNullOrEmpty(extension)
                ? "(none)"
                : extension.ToLowerInvariant();
        }

        private static bool IsSafePublicText(string value)
        {
            return ContentPublicValueRules.IsSafePublicText(value, 512);
        }
    }

    public static class ContentBaselineSummarySerializer
    {
        public const int SchemaVersion = 1;

        public static string SerializeJson(ContentBaselineSummary summary)
        {
            if (summary == null)
            {
                throw new ArgumentNullException(nameof(summary));
            }

            var builder = new StringBuilder();
            builder.Append("{\"schemaVersion\":");
            builder.Append(SchemaVersion.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"baselineLogicalName\":");
            ContentResolutionManifestSerializer.AppendJsonString(
                builder, summary.BaselineLogicalName);
            builder.Append(",\"manifestSchemaVersion\":");
            builder.Append(summary.ManifestSchemaVersion.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"manifestSha256\":");
            ContentResolutionManifestSerializer.AppendJsonString(
                builder, summary.ManifestSha256);
            builder.Append(",\"totalFileCount\":");
            builder.Append(summary.TotalFileCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"totalBytes\":");
            builder.Append(summary.TotalBytes.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"sourceVersion\":");
            ContentResolutionManifestSerializer.AppendJsonString(builder, summary.SourceVersion);
            builder.Append(",\"indexStartedUtc\":");
            ContentResolutionManifestSerializer.AppendJsonString(
                builder, summary.IndexStartedUtc.ToString("O", CultureInfo.InvariantCulture));
            builder.Append(",\"indexCompletedUtc\":");
            ContentResolutionManifestSerializer.AppendJsonString(
                builder, summary.IndexCompletedUtc.ToString("O", CultureInfo.InvariantCulture));
            builder.Append(",\"diagnosticCount\":");
            builder.Append(summary.DiagnosticCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"changesDetected\":");
            builder.Append(summary.ChangesDetected ? "true" : "false");
            builder.Append(",\"extensionAggregates\":[");
            for (int index = 0; index < summary.ExtensionAggregates.Count; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                ContentExtensionAggregate aggregate = summary.ExtensionAggregates[index];
                builder.Append("{\"extension\":");
                ContentResolutionManifestSerializer.AppendJsonString(
                    builder, aggregate.Extension);
                builder.Append(",\"fileCount\":");
                builder.Append(aggregate.FileCount.ToString(CultureInfo.InvariantCulture));
                builder.Append(",\"totalBytes\":");
                builder.Append(aggregate.TotalBytes.ToString(CultureInfo.InvariantCulture));
                builder.Append('}');
            }

            builder.Append("],\"contentNotes\":[");
            AppendStringArray(builder, summary.ContentNotes);
            builder.Append("],\"directoryVisibilityStatement\":");
            ContentResolutionManifestSerializer.AppendJsonString(
                builder, summary.DirectoryVisibilityStatement);
            builder.Append(",\"representatives\":[");
            for (int index = 0; index < summary.Representatives.Count; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                ContentRepresentativeFile representative = summary.Representatives[index];
                builder.Append("{\"logicalPath\":");
                ContentResolutionManifestSerializer.AppendJsonString(
                    builder, representative.LogicalPath);
                builder.Append(",\"length\":");
                builder.Append(representative.Length.ToString(CultureInfo.InvariantCulture));
                builder.Append(",\"sha256\":");
                ContentResolutionManifestSerializer.AppendJsonString(
                    builder, representative.Sha256);
                builder.Append('}');
            }

            builder.Append("]}");
            return builder.ToString();
        }

        private static void AppendStringArray(
            StringBuilder builder,
            IReadOnlyList<string> values)
        {
            for (int index = 0; index < values.Count; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                ContentResolutionManifestSerializer.AppendJsonString(builder, values[index]);
            }
        }
    }
}
