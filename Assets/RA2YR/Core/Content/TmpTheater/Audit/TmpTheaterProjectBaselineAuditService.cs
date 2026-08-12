using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RA2YR.Core.Binary;
using RA2YR.Core.Content.Mix;
using RA2YR.Core.Formats.Ini;
using RA2YR.Core.Formats.PackedMap;
using RA2YR.Core.Formats.Mix;
using RA2YR.Core.Formats.Tmp;

namespace RA2YR.Core.Content.TmpTheater.Audit
{
    /// <summary>Read-only aggregate audit for the configured patched development source.</summary>
    public static class TmpTheaterProjectBaselineAuditService
    {
        public const string BaselineLogicalName = "YR1001_ProjectBaseline";

        public static TmpTheaterProjectBaselineAuditDelivery Run(ExternalContentConfiguration configuration, int maxEntries = 500000)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            ExternalContentSourceDescriptor source = configuration.Sources.Single(s => s.Enabled && s.Id == BaselineLogicalName && s.Kind == ContentSourceKind.Patched);
            ContentIndexResult index = new ContentIndexer().Build(configuration);
            if (!index.IsComplete || index.HasErrors) throw new InvalidOperationException("The configured ProjectBaseline index is incomplete.");
            ContentSourceIndex sourceIndex = index.Sources.Single(s => s.Source.Id == BaselineLogicalName && s.IsComplete);
            string before = sourceIndex.Fingerprint;
            LogicalContentPath[] roots = sourceIndex.Files.Where(f => f.LogicalPath.Value.EndsWith(".mix", StringComparison.OrdinalIgnoreCase)).Select(f => f.LogicalPath).OrderBy(x => x, LogicalContentPathReportComparer.Instance).ToArray();
            var names = new MixNameCatalog(sourceIndex.Files.Select(f => f.LogicalPath).Concat(roots).Distinct());
            var aggregates = TheaterProfiles.All.ToDictionary(p => p.Kind, p => new TmpTheaterProfileAuditAggregate(p.Id));
            var hash = SHA256.Create();
            int mountedEntries = 0, candidates = 0, successes = 0, failures = 0, header48 = 0, header52 = 0, declared = 0, withZ = 0, withoutZ = 0, equivalent = 0;
            var mounts = new List<MixVirtualContentMountResult>();
            try
            {
                foreach (LogicalContentPath root in roots)
                {
                    MixVirtualContentMountResult mount = MixVirtualContentSource.MountDirectorySource(sourceIndex, new[] { root }, names, MixArchiveCatalogAdapters.ReadWithCoreReader, MixMountLimits.Default, MixMountIndexMode.ManifestAudit);
                    mounts.Add(mount);
                    if (!mount.IsComplete || mount.Diagnostics.Any(d => d.Severity == MixMountDiagnosticSeverity.Error)) { failures++; continue; }
                    mountedEntries = checked(mountedEntries + mount.Entries.Count);
                    if (mountedEntries > maxEntries) throw new InvalidOperationException("TMP audit entry budget exceeded.");
                    foreach (MixVirtualEntry entry in mount.Entries.Where(e => !e.IsMountedArchive && e.HasResolvedName).OrderBy(e => e.LogicalName.Value, StringComparer.OrdinalIgnoreCase))
                    {
                        string extension = Path.GetExtension(entry.LogicalName.Value).ToLowerInvariant();
                        TheaterProfileDescriptor profile = TheaterProfiles.All.FirstOrDefault(p => string.Equals(p.PrimaryTmpExtension, extension, StringComparison.OrdinalIgnoreCase));
                        if (profile == null && extension != ".ini") continue;
                        if (extension == ".ini") { InspectControl(entry, source, aggregates); continue; }
                        TmpTheaterProfileAuditAggregate aggregate = aggregates[profile.Kind];
                        aggregate.TmpCandidateCount++; candidates++;
                        TmpDocument doc = Read(entry, source, profile, TmpPlaneLayoutPolicy.DeclaredOffsets);
                        if (doc.IsSuccess) { aggregate.ValidTmpCount++; successes++; aggregate.DeclaredSuccessCount++; declared++; }
                        else { aggregate.InvalidTmpCount++; failures++; }
                        aggregate.CellCount = checked(aggregate.CellCount + doc.Cells.Count);
                        aggregate.EmptySlotCount = checked(aggregate.EmptySlotCount + doc.EmptySlotCount);
                        foreach (TmpCellRaw cell in doc.Cells)
                        {
                            aggregate.Header48CandidateCount++; aggregate.Header52ProductionCount++;
                            header48++; header52++;
                            if (cell.Header.UnknownFlagsRaw != 0) aggregate.UnknownFlagCellCount++;
                            if (cell.PlaneDirectory.Diagnostics.Any(d => d.Code == TmpDiagnosticCode.TrailingBytes)) aggregate.TrailingCellCount++;
                        }
                        if (doc.IsSuccess) AppendHash(hash, doc.CanonicalSha256);
                        TmpDocument z = Read(entry, source, profile, TmpPlaneLayoutPolicy.SequentialWithZ); if (z.IsSuccess) { aggregate.SequentialWithZSuccessCount++; withZ++; }
                        TmpDocument noZ = Read(entry, source, profile, TmpPlaneLayoutPolicy.SequentialWithoutZ); if (noZ.IsSuccess) { aggregate.SequentialWithoutZSuccessCount++; withoutZ++; }
                        if (doc.IsSuccess && TryConfirmMemoryStreamEquivalence(entry, source, profile, doc)) equivalent++;
                    }
                }
                ContentSourceIndex after = new ContentIndexer().Build(configuration).Sources.Single(s => s.Source.Id == BaselineLogicalName && s.IsComplete);
                if (!string.Equals(before, after.Fingerprint, StringComparison.Ordinal)) throw new IOException("ProjectBaseline changed during TMP audit.");
                string digest = FinalizeHash(hash);
                TmpTheaterAuditStatus status = candidates == 0 ? (failures == 0 ? TmpTheaterAuditStatus.CompleteWithNoCandidates : TmpTheaterAuditStatus.CompleteWithFailures) : failures == 0 ? TmpTheaterAuditStatus.Complete : TmpTheaterAuditStatus.CompleteWithFailures;
                return new TmpTheaterProjectBaselineAuditDelivery(status, before, after.Fingerprint, aggregates.Values, roots.Length, mountedEntries, candidates, successes, failures, header48, header52, declared, withZ, withoutZ, equivalent, digest, Serialize(status, before, after.Fingerprint, aggregates.Values, roots.Length, mountedEntries, candidates, successes, failures, header48, header52, declared, withZ, withoutZ, equivalent, digest));
            }
            finally
            {
                foreach (MixVirtualContentMountResult mount in mounts.AsEnumerable().Reverse()) mount.Dispose();
                hash.Dispose();
            }
        }

        private static void InspectControl(MixVirtualEntry entry, ExternalContentSourceDescriptor source, IDictionary<TheaterKind, TmpTheaterProfileAuditAggregate> aggregates)
        {
            string name = entry.LogicalName.Value;
            TheaterProfileDescriptor profile = TheaterProfiles.All.FirstOrDefault(p => p.ControlIniLogicalNames.Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase) || string.Equals(n, Path.GetFileName(name), StringComparison.OrdinalIgnoreCase)));
            if (profile != null) aggregates[profile.Kind].ControlDocumentCount++;
        }

        private static TmpDocument Read(MixVirtualEntry entry, ExternalContentSourceDescriptor source, TheaterProfileDescriptor profile, TmpPlaneLayoutPolicy layout)
        {
            var provenance = new IniSourceProvenance(source.Id, new[] { entry.Provenance.RootArchivePath });
            var binary = new BinarySourceContext("m3c6-tmp-baseline-audit", source.Id, LogicalContentPath.Parse("tmp-audit-entry"));
            return TmpRawReader.Read(entry.PayloadWindow, binary, provenance, new TmpReadPolicy(60, 30, layout));
        }

        private static bool TryConfirmMemoryStreamEquivalence(MixVirtualEntry entry, ExternalContentSourceDescriptor source,
            TheaterProfileDescriptor profile, TmpDocument windowDocument)
        {
            try
            {
                byte[] bytes = PackedMapBoundedInput.ReadWindow(entry.PayloadWindow, "m3c6-tmp-equivalence", 64 * 1024 * 1024);
                var provenance = new IniSourceProvenance(source.Id, new[] { entry.Provenance.RootArchivePath });
                var binary = new BinarySourceContext("m3c6-tmp-baseline-audit", source.Id, LogicalContentPath.Parse("tmp-audit-entry"));
                var policy = new TmpReadPolicy(60, 30, TmpPlaneLayoutPolicy.DeclaredOffsets);
                TmpDocument memory = TmpRawReader.Read(bytes, binary, provenance, policy, entry.PayloadWindow.AbsoluteStartOffset);
                using (var stream = new MemoryStream(bytes, writable: false))
                {
                    TmpDocument streamed = TmpRawReader.Read(stream, bytes.Length, binary, provenance, policy, entry.PayloadWindow.AbsoluteStartOffset);
                    return windowDocument.IsSuccess == memory.IsSuccess && memory.IsSuccess == streamed.IsSuccess &&
                        string.Equals(memory.CanonicalSha256, streamed.CanonicalSha256, StringComparison.Ordinal) &&
                        memory.ConsumedBytes == streamed.ConsumedBytes;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void AppendHash(HashAlgorithm hash, string value)
        { byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty); hash.TransformBlock(bytes, 0, bytes.Length, bytes, 0); }
        private static string FinalizeHash(HashAlgorithm hash) { hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0); return BitConverter.ToString(hash.Hash).Replace("-", string.Empty).ToLowerInvariant(); }

        private static string Serialize(TmpTheaterAuditStatus status, string before, string after, IEnumerable<TmpTheaterProfileAuditAggregate> profiles, int roots, int mounted, int candidates, int success, int failures, int header48, int header52, int declared, int withZ, int withoutZ, int equivalent, string hash)
        {
            var b = new StringBuilder(); b.Append('{').Append("\"manifestType\":\"RA2YR.TmpTheaterProjectBaselineAuditSanitized\""); b.Append(",\"auditVersion\":\"m3c6-v1\""); b.Append(",\"contentRole\":\"patched-development-content-source\""); b.Append(",\"status\":\"").Append(status).Append('"'); b.Append(",\"rootArchiveCount\":").Append(roots).Append(",\"mountedEntryCount\":").Append(mounted).Append(",\"tmpCandidateCount\":").Append(candidates).Append(",\"validTmpCount\":").Append(success).Append(",\"invalidTmpCount\":").Append(failures).Append(",\"header48CandidateCount\":").Append(header48).Append(",\"header52ProductionCount\":").Append(header52).Append(",\"declaredOffsetSuccessCount\":").Append(declared).Append(",\"sequentialWithZSuccessCount\":").Append(withZ).Append(",\"sequentialWithoutZSuccessCount\":").Append(withoutZ).Append(",\"memoryStreamEquivalentCount\":").Append(equivalent); b.Append(",\"theaterProfiles\":{"); bool first = true; foreach (TmpTheaterProfileAuditAggregate p in profiles.OrderBy(p => p.Profile, StringComparer.Ordinal)) { if (!first) b.Append(','); first = false; b.Append('"').Append(p.Profile).Append("\":{"); b.Append("\"controlDocuments\":").Append(p.ControlDocumentCount).Append(",\"tmpCandidates\":").Append(p.TmpCandidateCount).Append(",\"valid\":").Append(p.ValidTmpCount).Append(",\"invalid\":").Append(p.InvalidTmpCount).Append(",\"cells\":").Append(p.CellCount).Append(",\"emptySlots\":").Append(p.EmptySlotCount).Append('}'); } b.Append('}'); b.Append(",\"sourceFingerprintBefore\":\"").Append(before).Append("\",\"sourceFingerprintAfter\":\"").Append(after).Append("\",\"aggregateSha256\":\"").Append(hash).Append("\",\"originalRuntimeCompatibility\":\"NotConfirmed\"}"); return b.ToString();
        }
    }
}
