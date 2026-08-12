using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RA2YR.Core.Content.Mix;

namespace RA2YR.Core.Content.MapTerrain.Audit
{
    /// <summary>Read-only, aggregate-only map candidate traversal for the configured patched source.</summary>
    public static class MapTerrainProjectBaselineAuditService
    {
        public const string BaselineLogicalName = "YR1001_ProjectBaseline";

        public static MapTerrainProjectBaselineAuditDelivery Run(ExternalContentConfiguration configuration, int maxRoots = 1024, int maxEntries = 500000)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            ContentIndexResult index = new ContentIndexer().Build(configuration);
            if (!index.IsComplete || index.HasErrors) throw new InvalidOperationException("The configured ProjectBaseline index is incomplete.");
            ContentSourceIndex source = index.Sources.Single(s => s.Source.Id == BaselineLogicalName && s.IsComplete);
            LogicalContentPath[] roots = source.Files.Where(f => f.LogicalPath.Value.EndsWith(".mix", StringComparison.OrdinalIgnoreCase)).Select(f => f.LogicalPath).OrderBy(x => x, LogicalContentPathReportComparer.Instance).ToArray();
            if (roots.Length > maxRoots) throw new InvalidOperationException("Map terrain audit root budget exceeded.");
            string before = source.Fingerprint;
            var names = new MixNameCatalog(source.Files.Select(f => f.LogicalPath).Concat(roots).Distinct());
            var mounts = new List<MixVirtualContentMountResult>();
            int mounted = 0, candidates = 0, failures = 0;
            try
            {
                foreach (LogicalContentPath root in roots)
                {
                    MixVirtualContentMountResult mount = MixVirtualContentSource.MountDirectorySource(source, new[] { root }, names, MixArchiveCatalogAdapters.ReadWithCoreReader, MixMountLimits.Default, MixMountIndexMode.ManifestAudit);
                    mounts.Add(mount);
                    if (!mount.IsComplete || mount.Diagnostics.Any(d => d.Severity == MixMountDiagnosticSeverity.Error)) { failures++; continue; }
                    mounted = checked(mounted + mount.Entries.Count);
                    if (mounted > maxEntries) throw new InvalidOperationException("Map terrain audit entry budget exceeded.");
                    candidates = checked(candidates + mount.Entries.Count(e => !e.IsMountedArchive && e.HasResolvedName && string.Equals(Path.GetExtension(e.LogicalName.Value), ".map", StringComparison.OrdinalIgnoreCase)));
                }
                ContentSourceIndex after = new ContentIndexer().Build(configuration).Sources.Single(s => s.Source.Id == BaselineLogicalName && s.IsComplete);
                if (!string.Equals(before, after.Fingerprint, StringComparison.Ordinal)) throw new InvalidOperationException("The ProjectBaseline source changed during map terrain audit.");
                // Candidate discovery is implemented; full map-to-terrain binding is intentionally not
                // executed by this audit until a map-specific reader is available. Never call that
                // state Complete when candidates are present.
                if (candidates > 0) failures = checked(failures + candidates);
                string hash = Hash(roots.Length, mounted, candidates, failures, before);
                MapTerrainProjectBaselineAuditStatus status = candidates == 0 ? MapTerrainProjectBaselineAuditStatus.CompleteWithNoCandidates : MapTerrainProjectBaselineAuditStatus.CompleteWithFailures;
                string summary = Serialize(status, roots.Length, mounted, candidates, failures, before, after.Fingerprint, hash);
                return new MapTerrainProjectBaselineAuditDelivery(status, roots.Length, mounted, candidates, 0, candidates, failures, before, after.Fingerprint, hash, summary);
            }
            finally { foreach (MixVirtualContentMountResult mount in mounts.AsEnumerable().Reverse()) mount.Dispose(); }
        }

        private static string Hash(int roots, int mounted, int candidates, int failures, string fingerprint)
        { using (SHA256 sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(roots + "|" + mounted + "|" + candidates + "|" + failures + "|" + fingerprint))).Replace("-", string.Empty).ToLowerInvariant(); }
        private static string Serialize(MapTerrainProjectBaselineAuditStatus status, int roots, int mounted, int candidates, int failures, string before, string after, string hash)
        { return "{\"manifestType\":\"RA2YR.MapTerrainProjectBaselineAuditSanitized\",\"auditVersion\":\"m3c7-v1\",\"status\":\"" + status + "\",\"rootArchiveCount\":" + roots + ",\"mountedEntryCount\":" + mounted + ",\"mapCandidateCount\":" + candidates + ",\"parsedCandidateCount\":0,\"incompleteBindingCount\":" + candidates + ",\"failureCount\":" + failures + ",\"sourceFingerprintBefore\":\"" + before + "\",\"sourceFingerprintAfter\":\"" + after + "\",\"aggregateSha256\":\"" + hash + "\",\"originalRuntimeCompatibility\":\"NotConfirmed\"}"; }
    }
}
