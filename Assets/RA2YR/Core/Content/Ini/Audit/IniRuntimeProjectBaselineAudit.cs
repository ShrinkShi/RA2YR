using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RA2YR.Core.Configuration.Ini.Resolution;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Content.Ini.Audit
{
    public sealed class IniRuntimeProjectBaselineAuditDelivery
    {
        internal IniRuntimeProjectBaselineAuditDelivery(string sanitizedSummary)
        {
            if (string.IsNullOrWhiteSpace(sanitizedSummary))
            {
                throw new ArgumentException(
                    "A sanitized runtime INI audit summary is required.",
                    nameof(sanitizedSummary));
            }

            SanitizedSummary = sanitizedSummary;
            byte[] bytes = Encoding.UTF8.GetBytes(sanitizedSummary);
            SummaryUtf8Length = bytes.Length;
            using (SHA256 sha256 = SHA256.Create())
            {
                SummarySha256 = string.Concat(sha256.ComputeHash(bytes)
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        public string SanitizedSummary { get; }
        public int SummaryUtf8Length { get; }
        public string SummarySha256 { get; }
    }

    internal static class IniRuntimeProjectBaselineAuditSerializer
    {
        private const int MaximumSummaryBytes = 4 * 1024 * 1024;

        public static string SerializeSanitizedSummary(
            IniProjectBaselineAuditModel model,
            IniProjectBaselineAuditDelivery baseDelivery)
        {
            if (model == null || baseDelivery == null)
            {
                throw new ArgumentNullException(model == null ? nameof(model) : nameof(baseDelivery));
            }

            var builder = new StringBuilder(64 * 1024);
            builder.Append('{');
            AppendNumber(builder, "schemaVersion", 1, false);
            AppendString(builder, "manifestType", "RA2YR.IniRuntimeResolutionAuditSanitized", true);
            AppendString(builder, "baselineLogicalName", IniProjectBaselineAuditService.BaselineLogicalName, true);
            AppendString(builder, "auditStatus", "Complete", true);
            AppendString(builder, "directoryFingerprint", model.DirectoryFingerprint, true);
            AppendString(builder, "startedUtc", model.StartedUtc.ToString("O", CultureInfo.InvariantCulture), true);
            AppendString(builder, "completedUtc", model.CompletedUtc.ToString("O", CultureInfo.InvariantCulture), true);
            builder.Append(",\"sourceBoundary\":{");
            AppendBoolean(builder, "readOnly", true, false);
            AppendBoolean(builder, "looseIniUsed", false, true);
            AppendBoolean(builder, "handExtractedIniUsed", false, true);
            AppendBoolean(builder, "originalTextPublished", false, true);
            AppendString(builder, "baselineKind", "patched-project-baseline-not-clean-yr1001", true);
            builder.Append('}');

            builder.Append(",\"policyEvidence\":{");
            AppendPolicyEvidence(
                builder,
                "containerPrecedence",
                IniResolutionEvidenceLevel.ConfirmedByOfficialEditorSource,
                "ea-finalalert2-mission-editor-6abf0f5",
                "editor-search-order-only-runtime-unresolved",
                false);
            AppendPolicyEvidence(
                builder,
                "fileComposition",
                IniResolutionEvidenceLevel.Unresolved,
                "wp02g1-static-research",
                "rulesmd-and-soundmd-winners-unresolved",
                true);
            AppendPolicyEvidence(
                builder,
                "nameComparison",
                IniResolutionEvidenceLevel.Unresolved,
                "wp02g1-static-research",
                "explicit-policies-available-stock-runtime-unresolved",
                true);
            AppendPolicyEvidence(
                builder,
                "duplicateResolution",
                IniResolutionEvidenceLevel.Unresolved,
                "wp02g1-static-research",
                "editor-and-independent-implementations-disagree-as-runtime-proof",
                true);
            AppendPolicyEvidence(
                builder,
                "inlineCommentWhitespaceEmptyValue",
                IniResolutionEvidenceLevel.Unresolved,
                "wp02g1-static-research",
                "preserved-and-explicitly-configurable-runtime-unresolved",
                true);
            builder.Append('}');

            builder.Append(",\"candidateSets\":[");
            AppendCandidateSet(builder, "rulesmd.ini", GetCandidates(model, "rulesmd.ini"), false);
            AppendCandidateSet(builder, "soundmd.ini", GetCandidates(model, "soundmd.ini"), true);
            builder.Append(']');

            builder.Append(",\"syntaxAudits\":[");
            for (int index = 0; index < model.Samples.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                AppendSyntaxAudit(builder, model.Samples[index]);
            }

            builder.Append(']');
            builder.Append(",\"survey\":{");
            builder.Append("\"located\":[");
            for (int index = 0; index < model.SurveyCandidates.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                AppendSurveyCandidate(builder, model.SurveyCandidates[index]);
            }

            builder.Append("],\"notLocatedInMountedDirectoryAndMixSources\":[");
            for (int index = 0; index < model.UnresolvedSurveyNames.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                AppendJsonString(builder, model.UnresolvedSurveyNames[index].Value);
            }

            builder.Append("]}");
            builder.Append(",\"runtimeBoundary\":{");
            AppendBoolean(builder, "genericExplicitLoadPlanExecutable", true, false);
            AppendBoolean(builder, "perValueCandidateChainImplemented", true, true);
            AppendBoolean(builder, "projectBaselineRuntimeWinnerSelected", false, true);
            AppendBoolean(builder, "originalRuntimeComparisonPassed", false, true);
            AppendBoolean(builder, "typedRulesArtAiImplemented", false, true);
            AppendBoolean(builder, "blackBoxValidationRequired", true, true);
            AppendString(builder, "blackBoxAuthorization", "not-granted-not-executed", true);
            builder.Append('}');
            builder.Append(",\"baseIniAuditExternalManifest\":{");
            AppendString(
                builder,
                "cacheRelativePath",
                baseDelivery.ExternalManifestCacheRelativePath,
                false);
            AppendNumber(builder, "length", baseDelivery.ExternalManifestLength, true);
            AppendString(builder, "sha256", baseDelivery.ExternalManifestSha256, true);
            AppendBoolean(builder, "repositoryExternalCacheOnly", true, true);
            builder.Append("}}");

            string result = builder.ToString();
            if (Encoding.UTF8.GetByteCount(result) > MaximumSummaryBytes)
            {
                throw new InvalidOperationException(
                    "The sanitized runtime INI audit summary exceeded its fixed budget.");
            }

            return result;
        }

        private static IniSurveyCandidate[] GetCandidates(
            IniProjectBaselineAuditModel model,
            string logicalName)
        {
            var matches = model.SurveyCandidates.Where(candidate => string.Equals(
                    candidate.LogicalName.Value,
                    logicalName,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();
            matches.AddRange(model.Samples.Where(sample => string.Equals(
                    sample.Specification.LogicalName.Value,
                    logicalName,
                    StringComparison.OrdinalIgnoreCase))
                .Select(sample => new IniSurveyCandidate(
                    sample.Specification.LogicalName,
                    sample.Specification.ExpectedMixId,
                    sample.Provenance,
                    sample.Length,
                    sample.Sha256)));
            return matches
                .OrderBy(candidate => candidate.Provenance.RootArchive.Value,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(candidate => string.Join(
                    "/",
                    candidate.Provenance.Layers.Select(layer => layer.ResolvedName.Value)),
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static void AppendCandidateSet(
            StringBuilder builder,
            string logicalName,
            IReadOnlyList<IniSurveyCandidate> candidates,
            bool prefixComma)
        {
            if (prefixComma)
            {
                builder.Append(',');
            }

            builder.Append('{');
            AppendString(builder, "logicalName", logicalName, false);
            AppendNumber(builder, "candidateCount", candidates.Count, true);
            builder.Append(",\"candidates\":[");
            for (int index = 0; index < candidates.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                AppendSurveyCandidate(builder, candidates[index]);
            }

            builder.Append(']');
            builder.Append(",\"selectedWinner\":null");
            AppendString(builder, "winnerEvidence", "Unresolved", true);
            builder.Append('}');
        }

        private static void AppendSyntaxAudit(
            StringBuilder builder,
            IniGoldenSampleRecord sample)
        {
            IniRuntimeSyntaxAudit audit = IniRuntimeSyntaxAuditor.Analyze(sample.Document);
            builder.Append('{');
            AppendString(builder, "sampleId", sample.Specification.SampleId, false);
            AppendString(builder, "logicalName", sample.Specification.LogicalName.Value, true);
            AppendString(builder, "sha256", sample.Sha256, true);
            AppendProvenance(builder, sample.Provenance, true);
            builder.Append(",\"opaque\":{");
            AppendNumber(builder, "total", audit.OpaqueLineCount, false);
            AppendNumber(builder, "beforeFirstSection", audit.OpaqueBeforeSectionCount, true);
            AppendNumber(builder, "insideSection", audit.OpaqueInsideSectionCount, true);
            AppendNumber(builder, "afterSectionWithoutActiveOwner", audit.OpaqueAfterSectionCount, true);
            AppendNumber(builder, "containsEquals", audit.OpaqueContainsEqualsCount, true);
            AppendNumber(builder, "knownStructuralPunctuation", audit.OpaqueKnownPunctuationCount, true);
            AppendNumber(builder, "potentialRuntimeImpact", audit.OpaquePotentialRuntimeImpactCount, true);
            builder.Append(",\"reasonCounts\":{");
            AppendDictionary(builder, audit.OpaqueReasonCounts
                .OrderBy(value => value.Key.ToString(), StringComparer.Ordinal)
                .Select(value => new KeyValuePair<string, int>(value.Key.ToString(), value.Value)));
            builder.Append("},\"patternCounts\":{");
            AppendDictionary(builder, audit.OpaquePatternCounts
                .OrderBy(value => value.Key, StringComparer.Ordinal));
            builder.Append("}}");
            builder.Append(",\"inlineSemicolon\":{");
            AppendNumber(builder, "total", audit.InlineSemicolonLineCount, false);
            AppendNumber(builder, "valueStart", audit.SemicolonAtValueStartCount, true);
            AppendNumber(builder, "valueMiddle", audit.SemicolonInValueMiddleCount, true);
            AppendNumber(builder, "valueEnd", audit.SemicolonAtValueEndCount, true);
            builder.Append('}');
            AppendBoolean(builder, "mayAffectMinimalTypedView", audit.MayAffectMinimalTypedView, true);
            builder.Append('}');
        }

        private static void AppendSurveyCandidate(
            StringBuilder builder,
            IniSurveyCandidate candidate)
        {
            builder.Append('{');
            AppendString(builder, "logicalName", candidate.LogicalName.Value, false);
            AppendString(builder, "mixId", candidate.MixId.ToString(), true);
            AppendProvenance(builder, candidate.Provenance, true);
            AppendNumber(builder, "length", candidate.Length, true);
            AppendString(builder, "sha256", candidate.Sha256, true);
            builder.Append('}');
        }

        private static void AppendProvenance(
            StringBuilder builder,
            IniAuditProvenance provenance,
            bool prefixComma)
        {
            if (prefixComma)
            {
                builder.Append(',');
            }

            builder.Append("\"provenance\":{");
            AppendString(builder, "sourceId", provenance.SourceId, false);
            AppendString(builder, "rootArchive", provenance.RootArchive.Value, true);
            builder.Append(",\"layers\":[");
            for (int index = 0; index < provenance.Layers.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                IniAuditProvenanceLayer layer = provenance.Layers[index];
                builder.Append('{');
                AppendString(builder, "archive", layer.Archive.Value, false);
                AppendString(builder, "entryId", layer.EntryId.ToString(), true);
                AppendString(builder, "resolvedName", layer.ResolvedName.Value, true);
                builder.Append('}');
            }

            builder.Append("]}");
        }

        private static void AppendPolicyEvidence(
            StringBuilder builder,
            string name,
            IniResolutionEvidenceLevel level,
            string referenceId,
            string conclusion,
            bool prefixComma)
        {
            if (prefixComma)
            {
                builder.Append(',');
            }

            AppendJsonString(builder, name);
            builder.Append(":{");
            AppendString(builder, "level", level.ToString(), false);
            AppendString(builder, "referenceId", referenceId, true);
            AppendString(builder, "conclusion", conclusion, true);
            AppendBoolean(builder, "confirmsRuntime",
                level == IniResolutionEvidenceLevel.ConfirmedByOriginalRuntime ||
                level == IniResolutionEvidenceLevel.ConfirmedByProjectBaselineRuntime,
                true);
            builder.Append('}');
        }

        private static void AppendDictionary(
            StringBuilder builder,
            IEnumerable<KeyValuePair<string, int>> values)
        {
            bool first = true;
            foreach (KeyValuePair<string, int> value in values)
            {
                if (!first)
                {
                    builder.Append(',');
                }

                first = false;
                AppendJsonString(builder, value.Key);
                builder.Append(':');
                builder.Append(value.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void AppendString(
            StringBuilder builder,
            string name,
            string value,
            bool prefixComma)
        {
            if (prefixComma)
            {
                builder.Append(',');
            }

            AppendJsonString(builder, name);
            builder.Append(':');
            AppendJsonString(builder, value);
        }

        private static void AppendNumber(
            StringBuilder builder,
            string name,
            long value,
            bool prefixComma)
        {
            if (prefixComma)
            {
                builder.Append(',');
            }

            AppendJsonString(builder, name);
            builder.Append(':');
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AppendBoolean(
            StringBuilder builder,
            string name,
            bool value,
            bool prefixComma)
        {
            if (prefixComma)
            {
                builder.Append(',');
            }

            AppendJsonString(builder, name);
            builder.Append(value ? ":true" : ":false");
        }

        private static void AppendJsonString(StringBuilder builder, string value)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('"');
            foreach (char character in value)
            {
                switch (character)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\b':
                        builder.Append("\\b");
                        break;
                    case '\f':
                        builder.Append("\\f");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (character < 0x20)
                        {
                            builder.Append("\\u");
                            builder.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(character);
                        }

                        break;
                }
            }

            builder.Append('"');
        }
    }
}
