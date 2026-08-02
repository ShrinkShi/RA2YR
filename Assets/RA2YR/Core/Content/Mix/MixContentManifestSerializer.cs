using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using RA2YR.Core.Formats.Mix;

namespace RA2YR.Core.Content.Mix
{
    internal static class MixContentManifestSerializer
    {
        public const int SchemaVersion = 1;

        public static string SerializeMountCanonicalJson(
            MixVirtualContentMountResult mount)
        {
            EnsureCompleteMount(mount);
            var builder = new StringBuilder();
            builder.Append("{\"schemaVersion\":");
            builder.Append(SchemaVersion.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"manifestType\":\"mix-virtual-content\",\"source\":");
            AppendSource(builder, mount.Source);
            builder.Append(",\"archives\":[");
            MixMountedArchive[] archives = mount.Archives
                .OrderBy(archive => archive.LogicalPath, LogicalContentPathReportComparer.Instance)
                .ToArray();
            for (int index = 0; index < archives.Length; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                MixMountedArchive archive = archives[index];
                builder.Append("{\"logicalPath\":");
                AppendJson(builder, archive.LogicalPath.Value);
                builder.Append(",\"nestedDepth\":");
                builder.Append(archive.NestedDepth.ToString(CultureInfo.InvariantCulture));
                builder.Append(",\"entryCount\":");
                builder.Append(archive.EntryCount.ToString(CultureInfo.InvariantCulture));
                builder.Append(",\"headerKind\":");
                AppendJson(builder, archive.HeaderKind.ToString());
                builder.Append(",\"flags\":");
                AppendJson(builder, archive.Flags.ToString());
                builder.Append(",\"checksumVerified\":");
                builder.Append(archive.ChecksumVerified ? "true" : "false");
                builder.Append('}');
            }

            builder.Append("],\"entries\":[");
            MixVirtualEntry[] entries = mount.Entries
                .OrderBy(entry => entry, MixVirtualEntryReportComparer.Instance)
                .ToArray();
            for (int index = 0; index < entries.Length; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                AppendEntry(builder, entries[index]);
            }

            builder.Append("],\"diagnostics\":[");
            MixMountDiagnostic[] diagnostics = mount.Diagnostics
                .OrderBy(diagnostic => diagnostic.Code)
                .ThenBy(
                    diagnostic => diagnostic.ArchivePath,
                    LogicalContentPathReportComparer.Instance)
                .ThenBy(diagnostic => diagnostic.EntryId?.Value ?? 0)
                .ToArray();
            for (int index = 0; index < diagnostics.Length; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                MixMountDiagnostic diagnostic = diagnostics[index];
                builder.Append("{\"severity\":");
                AppendJson(builder, diagnostic.Severity.ToString());
                builder.Append(",\"code\":");
                AppendJson(builder, diagnostic.Code.ToString());
                builder.Append(",\"archivePath\":");
                AppendOptionalPath(builder, diagnostic.ArchivePath);
                builder.Append(",\"entryId\":");
                AppendOptionalId(builder, diagnostic.EntryId);
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
                    builder.Append(diagnostic.FormatDiagnostic.AbsoluteOffset.ToString(
                        CultureInfo.InvariantCulture));
                    builder.Append(",\"entryIndex\":");
                    builder.Append(diagnostic.FormatDiagnostic.EntryIndex.ToString(
                        CultureInfo.InvariantCulture));
                    builder.Append(",\"entryId\":");
                    AppendOptionalId(builder, diagnostic.FormatDiagnostic.EntryId);
                    builder.Append(",\"field\":");
                    AppendJson(builder, diagnostic.FormatDiagnostic.FieldOrSection);
                    builder.Append('}');
                }

                builder.Append('}');
            }

            builder.Append("]}");
            return builder.ToString();
        }

        public static string SerializeResolvedCanonicalJson(
            MixMountedContentResolutionResult resolution)
        {
            if (resolution == null)
            {
                throw new ArgumentNullException(nameof(resolution));
            }

            if (!resolution.IsComplete || resolution.HasErrors ||
                !resolution.HasAuditedDigests ||
                resolution.Entries.Any(entry => !entry.IsResolved))
            {
                throw new InvalidOperationException(
                    "Resolved public MIX manifests require complete, unambiguous, audited input.");
            }

            var builder = new StringBuilder();
            builder.Append("{\"schemaVersion\":");
            builder.Append(SchemaVersion.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"manifestType\":\"resolved-mix-content\",\"files\":[");
            MixMountedPathResolution[] entries = resolution.Entries
                .OrderBy(entry => entry.LogicalPath, LogicalContentPathReportComparer.Instance)
                .ToArray();
            for (int entryIndex = 0; entryIndex < entries.Length; entryIndex++)
            {
                if (entryIndex != 0)
                {
                    builder.Append(',');
                }

                MixMountedPathResolution entry = entries[entryIndex];
                builder.Append("{\"logicalPath\":");
                AppendJson(builder, entry.LogicalPath.Value);
                builder.Append(",\"selectedSourceId\":");
                AppendJson(builder, entry.Selected.Source.Id);
                builder.Append(",\"provenance\":[");
                for (int candidateIndex = 0;
                     candidateIndex < entry.ProvenanceChain.Count;
                     candidateIndex++)
                {
                    if (candidateIndex != 0)
                    {
                        builder.Append(',');
                    }

                    MixMountedContentCandidate candidate = entry.ProvenanceChain[candidateIndex];
                    builder.Append("{\"disposition\":");
                    AppendJson(
                        builder,
                        ReferenceEquals(candidate, entry.Selected)
                            ? "selected"
                            : "overridden");
                    builder.Append(",\"source\":");
                    AppendSource(builder, candidate.Source);
                    builder.Append(",\"layerKind\":");
                    AppendJson(builder, candidate.Layer.Kind.ToString());
                    builder.Append(",\"layerPath\":");
                    AppendJson(builder, candidate.Layer.LayerPath.Value);
                    builder.Append(",\"layerPriority\":");
                    if (candidate.LayerPriority.HasValue)
                    {
                        builder.Append(candidate.LayerPriority.Value.ToString(
                            CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append("null");
                    }

                    builder.Append(",\"length\":");
                    builder.Append(candidate.Length.ToString(CultureInfo.InvariantCulture));
                    builder.Append(",\"sha256\":");
                    AppendJson(builder, candidate.Sha256);
                    builder.Append(",\"mixProvenance\":");
                    if (candidate.MixProvenance == null)
                    {
                        builder.Append("null");
                    }
                    else
                    {
                        AppendProvenance(builder, candidate.MixProvenance);
                    }

                    builder.Append('}');
                }

                builder.Append("]}");
            }

            builder.Append("]}");
            return builder.ToString();
        }

        private static void AppendEntry(StringBuilder builder, MixVirtualEntry entry)
        {
            builder.Append("{\"id\":");
            AppendJson(builder, entry.Id.ToString());
            builder.Append(",\"logicalName\":");
            AppendOptionalPath(builder, entry.LogicalName);
            builder.Append(",\"length\":");
            builder.Append(entry.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"sha256\":");
            AppendJson(builder, entry.Sha256);
            builder.Append(",\"isMountedArchive\":");
            builder.Append(entry.IsMountedArchive ? "true" : "false");
            builder.Append(",\"provenance\":");
            AppendProvenance(builder, entry.Provenance);
            builder.Append('}');
        }

        private static void AppendProvenance(
            StringBuilder builder,
            MixEntryProvenance provenance)
        {
            builder.Append("{\"directorySource\":");
            AppendSource(builder, provenance.Source);
            builder.Append(",\"rootArchive\":");
            AppendJson(builder, provenance.RootArchivePath.Value);
            builder.Append(",\"layers\":[");
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
                AppendOptionalPath(builder, step.ResolvedName);
                builder.Append('}');
            }

            builder.Append("]}");
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

        private static void AppendOptionalPath(
            StringBuilder builder,
            LogicalContentPath path)
        {
            if (path == null)
            {
                builder.Append("null");
            }
            else
            {
                AppendJson(builder, path.Value);
            }
        }

        private static void AppendOptionalId(
            StringBuilder builder,
            MixFileId? id)
        {
            if (!id.HasValue)
            {
                builder.Append("null");
            }
            else
            {
                AppendJson(builder, id.Value.ToString());
            }
        }

        private static void AppendJson(StringBuilder builder, string value)
        {
            ContentResolutionManifestSerializer.AppendJsonString(builder, value);
        }

        private static void EnsureCompleteMount(MixVirtualContentMountResult mount)
        {
            if (mount == null)
            {
                throw new ArgumentNullException(nameof(mount));
            }

            if (!mount.IsComplete)
            {
                throw new InvalidOperationException(
                    "An incomplete MIX mount cannot be serialized as public evidence.");
            }

            if (mount.IndexMode != MixMountIndexMode.ManifestAudit ||
                mount.Entries.Any(entry => !entry.HasSha256))
            {
                throw new InvalidOperationException(
                    "Public MIX manifests require an explicit manifest-audit mount.");
            }
        }

        private sealed class MixVirtualEntryReportComparer : IComparer<MixVirtualEntry>
        {
            public static readonly MixVirtualEntryReportComparer Instance =
                new MixVirtualEntryReportComparer();

            public int Compare(MixVirtualEntry left, MixVirtualEntry right)
            {
                if (ReferenceEquals(left, right))
                {
                    return 0;
                }

                if (ReferenceEquals(left, null))
                {
                    return -1;
                }

                if (ReferenceEquals(right, null))
                {
                    return 1;
                }

                int root = LogicalContentPathReportComparer.Instance.Compare(
                    left.Provenance.RootArchivePath,
                    right.Provenance.RootArchivePath);
                if (root != 0)
                {
                    return root;
                }

                int commonSteps = Math.Min(
                    left.Provenance.Steps.Count,
                    right.Provenance.Steps.Count);
                for (int index = 0; index < commonSteps; index++)
                {
                    MixArchiveProvenanceStep leftStep = left.Provenance.Steps[index];
                    MixArchiveProvenanceStep rightStep = right.Provenance.Steps[index];
                    int archive = LogicalContentPathReportComparer.Instance.Compare(
                        leftStep.ArchivePath,
                        rightStep.ArchivePath);
                    if (archive != 0)
                    {
                        return archive;
                    }

                    int id = leftStep.EntryId.CompareTo(rightStep.EntryId);
                    if (id != 0)
                    {
                        return id;
                    }
                }

                return left.Provenance.Steps.Count.CompareTo(
                    right.Provenance.Steps.Count);
            }
        }
    }
}
