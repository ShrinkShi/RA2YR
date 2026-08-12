using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RA2YR.Core.Content.PackedMap.Audit;
using RA2YR.Core.Content.TmpTheater.Audit;

namespace RA2YR.Core.Content.MapTerrain.Audit
{
    /// <summary>Runs the existing bounded audits as one read-only M3 vertical integration observation.</summary>
    public static class M3C8RealMapIntegrationService
    {
        public static M3C8RealMapIntegrationDelivery Run(ExternalContentConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            IsoMapPack5ProjectBaselineAuditDelivery iso = IsoMapPack5ProjectBaselineAuditService.Run(configuration);
            PreviewPackProjectBaselineAuditDelivery preview = PreviewPackProjectBaselineAuditService.Run(configuration);
            TmpTheaterProjectBaselineAuditDelivery theater = TmpTheaterProjectBaselineAuditService.Run(configuration);
            MapTerrainProjectBaselineAuditDelivery terrain = MapTerrainProjectBaselineAuditService.Run(configuration);

            int mapCandidates = checked(Math.Max(iso.CandidateSectionCount, preview.CandidateEntryCount));
            int unresolved = checked(mapCandidates - terrain.MapCandidateCount);
            int diagnostics = checked(iso.DiagnosticCount + preview.DiagnosticCount + theater.FailureCount + terrain.FailureCount);
            bool sourceStable = string.Equals(iso.SourceFingerprint, iso.SourceFingerprintAfter, StringComparison.Ordinal) &&
                string.Equals(preview.SourceFingerprint, preview.SourceFingerprintAfter, StringComparison.Ordinal) &&
                string.Equals(theater.SourceFingerprintBefore, theater.SourceFingerprintAfter, StringComparison.Ordinal) &&
                string.Equals(terrain.SourceFingerprintBefore, terrain.SourceFingerprintAfter, StringComparison.Ordinal);
            if (!sourceStable) throw new InvalidOperationException("The ProjectBaseline fingerprint changed during M3-C8 integration.");

            int failures = checked(iso.FailedSectionCount + preview.FailedCount + theater.FailureCount + terrain.FailureCount);
            M3C8AuditStatus status = mapCandidates == 0
                ? (failures == 0 ? M3C8AuditStatus.CompleteWithNoCandidates : M3C8AuditStatus.CompleteWithFailures)
                : (failures == 0 && unresolved == 0 ? M3C8AuditStatus.Complete : M3C8AuditStatus.CompleteWithFailures);
            string hash = Hash(iso.AggregateSha256, preview.AggregateSha256, theater.AggregateSha256, terrain.AggregateSha256, mapCandidates, unresolved, status);
            string summary = Serialize(status, iso, preview, theater, terrain, mapCandidates, unresolved, diagnostics, hash);
            return new M3C8RealMapIntegrationDelivery(status, iso.SourceFingerprint, iso.SourceFingerprintAfter,
                iso.RootArchiveCount, iso.MountedEntryCount, mapCandidates, iso.CandidateSectionCount,
                iso.SuccessfulSectionCount, iso.FailedSectionCount, preview.CandidateEntryCount,
                preview.ExactDecodedCount, preview.FailedCount, theater.CandidateCount, theater.SuccessCount,
                theater.FailureCount, 0, 0, unresolved, diagnostics, hash, summary);
        }

        private static string Hash(string iso, string preview, string theater, string terrain, int candidates, int unresolved, M3C8AuditStatus status)
        {
            string value = string.Join("|", iso, preview, theater, terrain, candidates, unresolved, status);
            using (SHA256 sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static string Serialize(M3C8AuditStatus status, IsoMapPack5ProjectBaselineAuditDelivery iso,
            PreviewPackProjectBaselineAuditDelivery preview, TmpTheaterProjectBaselineAuditDelivery theater,
            MapTerrainProjectBaselineAuditDelivery terrain, int candidates, int unresolved, int diagnostics, string hash)
        {
            return "{\"manifestType\":\"RA2YR.M3C8RealMapIntegrationSanitized\",\"status\":\"" + status +
                "\",\"rootArchiveCount\":" + iso.RootArchiveCount + ",\"mountedEntryCount\":" + iso.MountedEntryCount +
                ",\"mapCandidateCount\":" + candidates + ",\"isoMapCandidateCount\":" + iso.CandidateSectionCount +
                ",\"isoMapSuccessCount\":" + iso.SuccessfulSectionCount + ",\"isoMapFailureCount\":" + iso.FailedSectionCount +
                ",\"previewCandidateCount\":" + preview.CandidateEntryCount + ",\"previewExactCount\":" + preview.ExactDecodedCount +
                ",\"previewFailureCount\":" + preview.FailedCount + ",\"theaterCandidateCount\":" + theater.CandidateCount +
                ",\"theaterSuccessCount\":" + theater.SuccessCount + ",\"theaterFailureCount\":" + theater.FailureCount +
                ",\"terrainFullyBoundCount\":0,\"terrainPartiallyBoundCount\":0,\"terrainUnresolvedCount\":" + unresolved +
                ",\"diagnosticCount\":" + diagnostics + ",\"aggregateSha256\":\"" + hash +
                "\",\"originalRuntimeCompatibility\":\"NotConfirmed\",\"cleanYROne001Comparison\":\"Unresolved\"}";
        }
    }
}
