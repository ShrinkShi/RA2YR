using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using RA2YR.Core.Formats.Mix;

namespace RA2YR.Core.Content.Mix.Audit
{
    internal static class MixBaselineAuditSerializer
    {
        public const int ExternalManifestSchemaVersion = 1;
        public const int SanitizedSummarySchemaVersion = 1;

        public static byte[] SerializeExternalManifestUtf8(
            MixBaselineAuditModel model,
            long maximumUtf8Bytes)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (maximumUtf8Bytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumUtf8Bytes));
            }

            var builder = new StringBuilder();
            builder.Append("{\"schemaVersion\":");
            builder.Append(ExternalManifestSchemaVersion.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"manifestType\":\"wp02c-mix-baseline-audit\"");
            builder.Append(",\"baselineLogicalName\":");
            AppendJson(builder, model.Source.Id);
            builder.Append(",\"source\":");
            AppendSource(builder, model.Source);
            builder.Append(",\"directoryFingerprint\":");
            AppendJson(builder, model.DirectoryFingerprint);
            builder.Append(",\"startedUtc\":");
            AppendJson(builder, FormatUtc(model.StartedUtc));
            builder.Append(",\"completedUtc\":");
            AppendJson(builder, FormatUtc(model.CompletedUtc));
            builder.Append(",\"xccGlobalNameDatabase\":{\"length\":");
            builder.Append(model.XccDatabaseLength.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"sha256\":");
            AppendJson(builder, model.XccDatabaseSha256);
            builder.Append("},\"roots\":[");

            MixBaselineRootAudit[] roots = model.Roots
                .OrderBy(root => root.RootFile.LogicalPath, LogicalContentPathReportComparer.Instance)
                .ToArray();
            for (int index = 0; index < roots.Length; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                MixBaselineRootAudit root = roots[index];
                builder.Append("{\"logicalPath\":");
                AppendJson(builder, root.RootFile.LogicalPath.Value);
                builder.Append(",\"length\":");
                builder.Append(root.RootFile.Length.ToString(CultureInfo.InvariantCulture));
                builder.Append(",\"sha256\":");
                AppendJson(builder, root.RootFile.Sha256);
                builder.Append(",\"status\":");
                AppendJson(builder, root.IsParsed ? "parsed" : "failed");
                if (root.IsParsed)
                {
                    builder.Append(",\"mount\":");
                    builder.Append(
                        MixContentManifestSerializer.SerializeMountCanonicalJson(root.Mount));
                }
                else
                {
                    builder.Append(",\"diagnostics\":[");
                    AppendMountDiagnostics(builder, root.Mount.Diagnostics);
                    builder.Append(']');
                }

                builder.Append('}');
                EnsureCharacterBudget(builder, maximumUtf8Bytes);
            }

            builder.Append("]}");
            byte[] utf8 = new UTF8Encoding(false, true).GetBytes(builder.ToString());
            if (utf8.LongLength > maximumUtf8Bytes)
            {
                throw new MixBaselineAuditException(
                    MixBaselineAuditFailureCode.ManifestBudgetExceeded,
                    "The external MIX audit manifest exceeds its explicit UTF-8 budget.");
            }

            return utf8;
        }

        public static string SerializeSanitizedSummary(
            MixBaselineAuditModel model,
            MixAuditExternalManifestReference externalManifest)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (externalManifest == null)
            {
                throw new ArgumentNullException(nameof(externalManifest));
            }

            MixBaselineRootAudit[] parsedRoots = model.Roots
                .Where(root => root.IsParsed)
                .ToArray();
            MixMountedArchive[] archives = parsedRoots
                .SelectMany(root => root.Mount.Archives)
                .ToArray();
            MixVirtualEntry[] entries = parsedRoots
                .SelectMany(root => root.Mount.Entries)
                .ToArray();
            Dictionary<string, int> diagnostics = CountDiagnostics(model.Roots);

            var builder = new StringBuilder();
            builder.Append("{\"schemaVersion\":");
            builder.Append(SanitizedSummarySchemaVersion.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"baselineLogicalName\":");
            AppendJson(builder, model.Source.Id);
            builder.Append(",\"auditStatus\":");
            AppendJson(
                builder,
                parsedRoots.Length == model.Roots.Count
                    ? MixBaselineAuditStatus.Complete.ToString()
                    : MixBaselineAuditStatus.CompleteWithArchiveFailures.ToString());
            builder.Append(",\"sourceVersion\":");
            AppendJson(builder, model.Source.Version);
            builder.Append(",\"directoryFingerprint\":");
            AppendJson(builder, model.DirectoryFingerprint);
            builder.Append(",\"startedUtc\":");
            AppendJson(builder, FormatUtc(model.StartedUtc));
            builder.Append(",\"completedUtc\":");
            AppendJson(builder, FormatUtc(model.CompletedUtc));
            builder.Append(",\"externalManifest\":{\"schemaVersion\":");
            builder.Append(ExternalManifestSchemaVersion.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"cacheRelativePath\":");
            AppendJson(builder, externalManifest.CacheRelativePath);
            builder.Append(",\"length\":");
            builder.Append(externalManifest.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"sha256\":");
            AppendJson(builder, externalManifest.Sha256);
            builder.Append("},\"xccGlobalNameDatabase\":{\"length\":");
            builder.Append(model.XccDatabaseLength.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"sha256\":");
            AppendJson(builder, model.XccDatabaseSha256);
            builder.Append("},\"rootArchives\":{\"count\":");
            builder.Append(model.Roots.Count.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"totalBytes\":");
            builder.Append(ComputeRootTotalBytes(model.Roots)
                .ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"parsed\":");
            builder.Append(parsedRoots.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"failed\":");
            builder.Append((model.Roots.Count - parsedRoots.Length)
                .ToString(CultureInfo.InvariantCulture));
            builder.Append("},\"mountedArchives\":{\"count\":");
            builder.Append(archives.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"classicHeader\":");
            builder.Append(archives.Count(archive =>
                    archive.HeaderKind == MixArchiveHeaderKind.Classic)
                .ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"extendedHeader\":");
            builder.Append(archives.Count(archive =>
                    archive.HeaderKind == MixArchiveHeaderKind.Extended)
                .ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"encryptedDirectory\":");
            builder.Append(archives.Count(archive =>
                    (archive.Flags & MixArchiveFlags.EncryptedDirectory) != 0)
                .ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"checksum\":");
            builder.Append(archives.Count(archive =>
                    (archive.Flags & MixArchiveFlags.Checksum) != 0)
                .ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"nestedCount\":");
            builder.Append(archives.Count(archive => archive.NestedDepth > 0)
                .ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"maximumNestedDepth\":");
            builder.Append((archives.Length == 0 ? 0 : archives.Max(archive => archive.NestedDepth))
                .ToString(CultureInfo.InvariantCulture));
            builder.Append("},\"entries\":{\"count\":");
            builder.Append(entries.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"unknownIdCount\":");
            builder.Append(entries.Count(entry => !entry.HasResolvedName)
                .ToString(CultureInfo.InvariantCulture));
            builder.Append("},\"diagnostics\":[");
            int diagnosticIndex = 0;
            foreach (KeyValuePair<string, int> diagnostic in diagnostics
                         .OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                if (diagnosticIndex++ != 0)
                {
                    builder.Append(',');
                }

                builder.Append("{\"code\":");
                AppendJson(builder, diagnostic.Key);
                builder.Append(",\"count\":");
                builder.Append(diagnostic.Value.ToString(CultureInfo.InvariantCulture));
                builder.Append('}');
            }

            builder.Append("],\"targets\":[");
            MixBaselineTargetAudit[] targets = model.Targets
                .OrderBy(target => target.LogicalPath, LogicalContentPathReportComparer.Instance)
                .ToArray();
            for (int index = 0; index < targets.Length; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                AppendTarget(builder, targets[index]);
            }

            builder.Append("],\"limitations\":[");
            AppendJson(
                builder,
                "A target not found in completed directory/MIX mounts is not proof that the original game file is missing.");
            builder.Append(',');
            AppendJson(
                builder,
                "Multiple target matches are reported without inventing an unverified MIX layer precedence.");
            builder.Append("]}");
            return builder.ToString();
        }

        private static long ComputeRootTotalBytes(
            IEnumerable<MixBaselineRootAudit> roots)
        {
            try
            {
                long total = 0;
                foreach (MixBaselineRootAudit root in roots)
                {
                    total = checked(total + root.RootFile.Length);
                }

                return total;
            }
            catch (OverflowException)
            {
                throw new MixBaselineAuditException(
                    MixBaselineAuditFailureCode.ManifestBudgetExceeded,
                    "The MIX audit root byte total overflowed its report representation.");
            }
        }

        private static void AppendTarget(StringBuilder builder, MixBaselineTargetAudit target)
        {
            builder.Append("{\"logicalName\":");
            AppendJson(builder, target.LogicalPath.Value);
            builder.Append(",\"mixId\":");
            AppendJson(builder, target.Id.ToString());
            builder.Append(",\"status\":");
            AppendJson(builder, target.Status);
            builder.Append(",\"matchCount\":");
            builder.Append(target.Matches.Count.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"diagnosticCount\":");
            builder.Append(target.DiagnosticCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"matches\":[");
            for (int index = 0; index < target.Matches.Count; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                MixBaselineTargetMatch match = target.Matches[index];
                builder.Append("{\"storageKind\":");
                AppendJson(builder, match.StorageKind);
                builder.Append(",\"length\":");
                builder.Append(match.Length.ToString(CultureInfo.InvariantCulture));
                builder.Append(",\"sha256\":");
                AppendJson(builder, match.Sha256);
                builder.Append(",\"encryptedChain\":");
                builder.Append(match.EncryptedChain ? "true" : "false");
                builder.Append(",\"provenance\":");
                AppendProvenance(builder, match.Provenance);
                builder.Append('}');
            }

            builder.Append("]}");
        }

        private static void AppendProvenance(
            StringBuilder builder,
            MixEntryProvenance provenance)
        {
            if (provenance == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append("{\"sourceId\":");
            AppendJson(builder, provenance.Source.Id);
            builder.Append(",\"rootArchive\":");
            AppendJson(builder, provenance.RootArchivePath.Value);
            builder.Append(",\"chain\":[");
            for (int index = 0; index < provenance.Steps.Count; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                MixArchiveProvenanceStep step = provenance.Steps[index];
                builder.Append("{\"archive\":");
                AppendJson(builder, step.ArchivePath.Value);
                builder.Append(",\"entryId\":");
                AppendJson(builder, step.EntryId.ToString());
                builder.Append(",\"resolvedName\":");
                if (step.ResolvedName == null)
                {
                    builder.Append("null");
                }
                else
                {
                    AppendJson(builder, step.ResolvedName.Value);
                }

                builder.Append('}');
            }

            builder.Append("]}");
        }

        private static void AppendMountDiagnostics(
            StringBuilder builder,
            IReadOnlyList<MixMountDiagnostic> diagnostics)
        {
            MixMountDiagnostic[] ordered = diagnostics
                .OrderBy(diagnostic => diagnostic.Code)
                .ThenBy(diagnostic => diagnostic.ArchivePath, LogicalContentPathReportComparer.Instance)
                .ThenBy(diagnostic => diagnostic.EntryId?.Value ?? 0)
                .ToArray();
            for (int index = 0; index < ordered.Length; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                MixMountDiagnostic diagnostic = ordered[index];
                builder.Append("{\"code\":");
                AppendJson(builder, diagnostic.Code.ToString());
                builder.Append(",\"archivePath\":");
                if (diagnostic.ArchivePath == null)
                {
                    builder.Append("null");
                }
                else
                {
                    AppendJson(builder, diagnostic.ArchivePath.Value);
                }

                builder.Append(",\"entryId\":");
                if (diagnostic.EntryId.HasValue)
                {
                    AppendJson(builder, diagnostic.EntryId.Value.ToString());
                }
                else
                {
                    builder.Append("null");
                }

                builder.Append(",\"formatDiagnostic\":");
                if (diagnostic.FormatDiagnostic == null)
                {
                    builder.Append("null");
                }
                else
                {
                    builder.Append("{\"code\":");
                    AppendJson(builder, diagnostic.FormatDiagnostic.Code.ToString());
                    builder.Append(",\"absoluteOffset\":");
                    builder.Append(diagnostic.FormatDiagnostic.AbsoluteOffset
                        .ToString(CultureInfo.InvariantCulture));
                    builder.Append(",\"field\":");
                    AppendJson(builder, diagnostic.FormatDiagnostic.FieldOrSection);
                    builder.Append('}');
                }

                builder.Append('}');
            }
        }

        private static Dictionary<string, int> CountDiagnostics(
            IEnumerable<MixBaselineRootAudit> roots)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (MixMountDiagnostic diagnostic in roots.SelectMany(root => root.Mount.Diagnostics))
            {
                Increment(result, "MixMount." + diagnostic.Code);
                if (diagnostic.FormatDiagnostic != null)
                {
                    Increment(result, "MixFormat." + diagnostic.FormatDiagnostic.Code);
                }
            }

            return result;
        }

        private static void Increment(IDictionary<string, int> values, string key)
        {
            int count;
            values.TryGetValue(key, out count);
            values[key] = checked(count + 1);
        }

        private static void AppendSource(
            StringBuilder builder,
            ExternalContentSourceDescriptor source)
        {
            builder.Append("{\"id\":");
            AppendJson(builder, source.Id);
            builder.Append(",\"kind\":");
            AppendJson(builder, source.Kind.ToString());
            builder.Append(",\"version\":");
            AppendJson(builder, source.Version);
            builder.Append(",\"priority\":");
            builder.Append(source.Priority.ToString(CultureInfo.InvariantCulture));
            builder.Append('}');
        }

        private static void AppendJson(StringBuilder builder, string value)
        {
            ContentResolutionManifestSerializer.AppendJsonString(builder, value);
        }

        private static void EnsureCharacterBudget(
            StringBuilder builder,
            long maximumUtf8Bytes)
        {
            if (builder.Length > maximumUtf8Bytes)
            {
                throw new MixBaselineAuditException(
                    MixBaselineAuditFailureCode.ManifestBudgetExceeded,
                    "The external MIX audit manifest exceeds its explicit size budget.");
            }
        }

        private static string FormatUtc(DateTime value)
        {
            return value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        }
    }
}
