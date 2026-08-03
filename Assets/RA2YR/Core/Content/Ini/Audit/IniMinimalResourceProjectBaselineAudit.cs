using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RA2YR.Core.Configuration.Ini.Resolution;
using RA2YR.Core.Configuration.Ini.Typed;

namespace RA2YR.Core.Content.Ini.Audit
{
    public sealed class IniMinimalResourceProjectBaselineAuditDelivery
    {
        internal IniMinimalResourceProjectBaselineAuditDelivery(string sanitizedSummary)
        {
            SanitizedSummary = sanitizedSummary ??
                throw new ArgumentNullException(nameof(sanitizedSummary));
            byte[] bytes = new UTF8Encoding(false, true).GetBytes(sanitizedSummary);
            SummaryUtf8Length = bytes.Length;
            using (SHA256 sha = SHA256.Create())
            {
                SummarySha256 = ToLowerHex(sha.ComputeHash(bytes));
            }
        }

        public string SanitizedSummary { get; }
        public int SummaryUtf8Length { get; }
        public string SummarySha256 { get; }

        private static string ToLowerHex(byte[] bytes)
        {
            return string.Concat(bytes.Select(value =>
                value.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }

    internal static class IniMinimalResourceProjectBaselineAudit
    {
        private const string RulesExpandSample = "rulesmd-expandmd01";
        private const string RulesLocalSample = "rulesmd-localmd";
        private const string ArtLocalSample = "artmd-localmd";

        public static IniMinimalResourceProjectBaselineAuditDelivery Build(
            IniProjectBaselineAuditModel model,
            IniProjectBaselineAuditDelivery physicalAudit)
        {
            if (model == null || physicalAudit == null)
            {
                throw new ArgumentNullException(model == null ? nameof(model) : nameof(physicalAudit));
            }

            IniGoldenSampleRecord rulesExpand = GetSample(model, RulesExpandSample);
            IniGoldenSampleRecord rulesLocal = GetSample(model, RulesLocalSample);
            IniGoldenSampleRecord artLocal = GetSample(model, ArtLocalSample);

            RulesAggregate expandRules = BuildRules(rulesExpand);
            RulesAggregate localRules = BuildRules(rulesLocal);
            ArtAggregate art = BuildArt(artLocal);
            string summary = Serialize(
                model,
                physicalAudit,
                expandRules,
                localRules,
                art);
            return new IniMinimalResourceProjectBaselineAuditDelivery(summary);
        }

        private static IniGoldenSampleRecord GetSample(
            IniProjectBaselineAuditModel model,
            string sampleId)
        {
            IniGoldenSampleRecord[] matches = model.Samples.Where(sample =>
                    string.Equals(
                        sample.Specification.SampleId,
                        sampleId,
                        StringComparison.Ordinal))
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "A fixed ProjectBaseline typed-view input is missing or ambiguous.");
            }

            return matches[0];
        }

        private static RulesAggregate BuildRules(IniGoldenSampleRecord sample)
        {
            IniResolutionResult resolution = ResolveSingle(sample);
            IniTypedViewResult<IniRulesResourceDocument> typed =
                IniMinimalResourceViewBuilder.BuildRules(
                    resolution,
                    IniTypedNameComparisonPolicy.OrdinalIgnoreCaseAscii);
            if (typed.Document == null || typed.Status == IniTypedViewStatus.Failed ||
                typed.Status == IniTypedViewStatus.Ambiguous)
            {
                throw new InvalidOperationException(
                    "A fixed ProjectBaseline Rules typed view failed closed.");
            }

            IniRulesRegistryEntry[] entries = typed.Document.Registries
                .SelectMany(registry => registry.Entries)
                .ToArray();
            int traceCount = entries.Count(entry => entry.Identifier.Value != null);
            int completeTraceCount = entries.Count(entry =>
                HasCompleteTrace(entry.Identifier.Value));
            return new RulesAggregate(
                sample.Specification.SampleId,
                typed.Status,
                typed.Document.Registries.Count(registry => registry.Entries.Count != 0),
                entries.Length,
                entries.Count(entry => entry.Identifier.Status == IniTypedValueStatus.Invalid),
                entries.Count(entry => entry.Identifier.Status == IniTypedValueStatus.Failed),
                completeTraceCount,
                traceCount,
                typed.Document.CanonicalModelSha256,
                CountDiagnostics(typed.Diagnostics));
        }

        private static ArtAggregate BuildArt(IniGoldenSampleRecord sample)
        {
            IniResolutionResult resolution = ResolveSingle(sample);
            IniTypedViewResult<IniArtResourceDocument> typed =
                IniMinimalResourceViewBuilder.BuildArt(
                    resolution,
                    IniTypedNameComparisonPolicy.OrdinalIgnoreCaseAscii,
                    IniBooleanCasePolicy.OrdinalIgnoreCaseAscii);
            if (typed.Document == null || typed.Status == IniTypedViewStatus.Failed ||
                typed.Status == IniTypedViewStatus.Ambiguous)
            {
                throw new InvalidOperationException(
                    "The fixed ProjectBaseline Art typed view failed closed.");
            }

            IniArtResourceField[] fields = typed.Document.Records
                .SelectMany(record => record.Fields)
                .ToArray();
            IniResourceReference[] references = typed.Document.Records
                .SelectMany(record => record.References.References)
                .ToArray();
            IniArtResourceField[] voxels = fields.Where(field =>
                field.Kind == IniArtFieldKind.Voxel).ToArray();
            int traceCount = fields.Count(field => field.Parsed?.Value != null);
            int completeTraceCount = fields.Count(field =>
                HasCompleteTrace(field.Parsed?.Value));

            return new ArtAggregate(
                sample.Specification.SampleId,
                typed.Status,
                typed.Document.Records.Count,
                CountPresent(fields, IniArtFieldKind.Image),
                CountPresent(fields, IniArtFieldKind.Cameo),
                CountPresent(fields, IniArtFieldKind.AltCameo),
                CountPresent(fields, IniArtFieldKind.Buildup),
                voxels.Count(field => field.Status == IniTypedValueStatus.Present &&
                    field.Parsed.Value.BooleanValue == true),
                voxels.Count(field => field.Status == IniTypedValueStatus.Present &&
                    field.Parsed.Value.BooleanValue == false),
                voxels.Count(field => field.Status == IniTypedValueStatus.Invalid),
                voxels.Count(field => field.Status == IniTypedValueStatus.Missing),
                fields.Count(field => field.Status == IniTypedValueStatus.Ambiguous),
                fields.Count(field => field.Status == IniTypedValueStatus.Invalid),
                fields.Count(field => field.Status == IniTypedValueStatus.Missing),
                references.Count(value =>
                    value.ExplicitExtension == IniExplicitResourceExtension.None),
                references.Count(value =>
                    value.ExplicitExtension == IniExplicitResourceExtension.Shp),
                references.Count(value =>
                    value.ExplicitExtension == IniExplicitResourceExtension.Vxl),
                references.Count(value =>
                    value.ExplicitExtension == IniExplicitResourceExtension.Pal),
                references.Count(value =>
                    value.ExplicitExtension == IniExplicitResourceExtension.Other),
                typed.Document.Records.Count(value =>
                    value.RouteCandidate == IniResourceRouteCandidate.Shp),
                typed.Document.Records.Count(value =>
                    value.RouteCandidate == IniResourceRouteCandidate.Vxl),
                typed.Document.Records.Count(value =>
                    value.RouteCandidate == IniResourceRouteCandidate.Unknown),
                completeTraceCount,
                traceCount,
                typed.Document.CanonicalModelSha256,
                CountDiagnostics(typed.Diagnostics));
        }

        private static int CountPresent(
            IEnumerable<IniArtResourceField> fields,
            IniArtFieldKind kind)
        {
            return fields.Count(field =>
                field.Kind == kind && field.Status == IniTypedValueStatus.Present);
        }

        private static bool HasCompleteTrace(IniTypedValue value)
        {
            return value != null && value.SourceTrace != null &&
                value.SourceTrace.Winner != null &&
                value.SourceTrace.Candidates.Count != 0 &&
                value.SourceTrace.Candidates.All(candidate =>
                    !string.IsNullOrEmpty(candidate.SourceId) &&
                    !string.IsNullOrEmpty(candidate.CandidateId) &&
                    candidate.LogicalChain.Count != 0);
        }

        private static IReadOnlyDictionary<string, int> CountDiagnostics(
            IEnumerable<IniTypedDiagnostic> diagnostics)
        {
            return diagnostics.GroupBy(value => value.Code.ToString(), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        }

        private static IniResolutionResult ResolveSingle(IniGoldenSampleRecord sample)
        {
            IniResolutionEvidence evidence = new IniResolutionEvidence(
                IniResolutionEvidenceLevel.ConfiguredForTesting,
                "wp02g2-project-baseline-explicit-policy");
            var policy = new IniResolutionPolicy(
                IniFileCompositionPolicy.SelectHighestPriorityDocument, evidence,
                IniNameComparisonPolicy.OrdinalIgnoreCaseAscii, evidence,
                IniDuplicateSectionPolicy.MergeSectionsInFileOrder, evidence,
                IniDuplicateKeyPolicy.LastKeyWins, evidence,
                IniInlineCommentPolicy.PreserveSemicolonInValue, evidence,
                IniWhitespaceReadPolicy.TrimAsciiSpaceAndTab, evidence,
                IniEmptyValuePolicy.OverridesEarlierValue, evidence);
            var layer = new IniLoadLayer(
                sample.Specification.SampleId + "-layer",
                sample.Document.Provenance.SourceId,
                IniLoadLayerKind.TestSource,
                sample.Document.Provenance.LogicalChain,
                0,
                evidence);
            var candidate = new IniCandidateDocument(
                sample.Specification.SampleId + "-candidate",
                layer.LayerId,
                sample.Specification.LogicalName,
                sample.Document);
            IniResolutionResult result = new IniRuntimeResolver().Resolve(
                new IniLoadPlan(
                    sample.Specification.SampleId + "-wp02g2-plan",
                    new[] { layer }),
                new[] { candidate },
                policy);
            if (!result.IsComplete)
            {
                throw new InvalidOperationException(
                    "A fixed single-document configured test resolution was not complete.");
            }

            return result;
        }

        private static string Serialize(
            IniProjectBaselineAuditModel model,
            IniProjectBaselineAuditDelivery physicalAudit,
            RulesAggregate rulesExpand,
            RulesAggregate rulesLocal,
            ArtAggregate art)
        {
            var builder = new StringBuilder();
            builder.Append("{\"schemaVersion\":1");
            builder.Append(",\"manifestType\":\"RA2YR.IniMinimalResourceProjectBaselineAuditSanitized\"");
            builder.Append(",\"baselineLogicalName\":\"YR1001_ProjectBaseline\"");
            builder.Append(",\"auditStatus\":\"Complete\",\"policyEvidence\":\"ConfiguredForTesting\"");
            builder.Append(",\"stockRuntimeWinnerSelected\":false");
            builder.Append(",\"sourceVersion\":");
            AppendJson(builder, model.Source.Version);
            builder.Append(",\"directoryFingerprint\":");
            AppendJson(builder, model.DirectoryFingerprint);
            builder.Append(",\"baseIniAuditExternalManifest\":{\"schemaVersion\":1,\"cacheRelativePath\":");
            AppendJson(builder, physicalAudit.ExternalManifestCacheRelativePath);
            builder.Append(",\"length\":");
            builder.Append(physicalAudit.ExternalManifestLength.ToString(
                CultureInfo.InvariantCulture));
            builder.Append(",\"sha256\":");
            AppendJson(builder, physicalAudit.ExternalManifestSha256);
            builder.Append('}');
            builder.Append(",\"startedUtc\":");
            AppendJson(builder, model.StartedUtc.ToString("O", CultureInfo.InvariantCulture));
            builder.Append(",\"completedUtc\":");
            AppendJson(builder, model.CompletedUtc.ToString("O", CultureInfo.InvariantCulture));
            builder.Append(",\"rulesCandidates\":[");
            AppendRules(builder, rulesExpand);
            builder.Append(',');
            AppendRules(builder, rulesLocal);
            builder.Append("],\"artCandidate\":");
            AppendArt(builder, art);
            builder.Append(",\"limitations\":[");
            AppendJson(builder,
                "The two rulesmd.ini candidates are evaluated separately under ConfiguredForTesting and no stock runtime winner is selected.");
            builder.Append(',');
            AppendJson(builder,
                "Only explicit resource references are parsed; stock Rules and Art semantics, defaults, fallback, SHP, VXL, rendering, and gameplay remain unimplemented.");
            builder.Append(',');
            AppendJson(builder,
                "Incomplete typed results remain incomplete when Opaque lines, inline semicolons, or duplicate resolution policies may affect a target.");
            builder.Append("]}");
            return builder.ToString();
        }

        private static void AppendRules(StringBuilder builder, RulesAggregate value)
        {
            builder.Append("{\"candidateRole\":");
            AppendJson(builder, value.CandidateRole);
            builder.Append(",\"typedStatus\":");
            AppendJson(builder, value.Status.ToString());
            AppendNumber(builder, "registryTypeCount", value.RegistryTypeCount);
            AppendNumber(builder, "registryEntryCount", value.RegistryEntryCount);
            AppendNumber(builder, "invalidIdentifierCount", value.InvalidIdentifierCount);
            AppendNumber(builder, "failedIdentifierCount", value.FailedIdentifierCount);
            AppendTraceCoverage(builder, value.CompleteTraceCount, value.TraceCount);
            builder.Append(",\"normalizedModelSha256\":");
            AppendJson(builder, value.ModelSha256);
            AppendDiagnostics(builder, value.DiagnosticCounts);
            builder.Append('}');
        }

        private static void AppendArt(StringBuilder builder, ArtAggregate value)
        {
            builder.Append("{\"candidateRole\":");
            AppendJson(builder, value.CandidateRole);
            builder.Append(",\"typedStatus\":");
            AppendJson(builder, value.Status.ToString());
            AppendNumber(builder, "recordCount", value.RecordCount);
            AppendNumber(builder, "explicitImageCount", value.ImageCount);
            AppendNumber(builder, "cameoReferenceCount", value.CameoCount);
            AppendNumber(builder, "altCameoReferenceCount", value.AltCameoCount);
            AppendNumber(builder, "buildupReferenceCount", value.BuildupCount);
            builder.Append(",\"voxel\":{");
            AppendNumberBody(builder, "yes", value.VoxelYes);
            AppendNumber(builder, "no", value.VoxelNo);
            AppendNumber(builder, "invalid", value.VoxelInvalid);
            AppendNumber(builder, "missing", value.VoxelMissing);
            builder.Append("},\"fieldStates\":{");
            AppendNumberBody(builder, "ambiguous", value.AmbiguousCount);
            AppendNumber(builder, "invalid", value.InvalidCount);
            AppendNumber(builder, "missing", value.MissingCount);
            builder.Append("},\"explicitExtensions\":{");
            AppendNumberBody(builder, "none", value.ExtensionNone);
            AppendNumber(builder, "shp", value.ExtensionShp);
            AppendNumber(builder, "vxl", value.ExtensionVxl);
            AppendNumber(builder, "pal", value.ExtensionPal);
            AppendNumber(builder, "other", value.ExtensionOther);
            builder.Append("},\"routeCandidates\":{");
            AppendNumberBody(builder, "shp", value.RouteShp);
            AppendNumber(builder, "vxl", value.RouteVxl);
            AppendNumber(builder, "unknown", value.RouteUnknown);
            builder.Append('}');
            AppendTraceCoverage(builder, value.CompleteTraceCount, value.TraceCount);
            builder.Append(",\"normalizedModelSha256\":");
            AppendJson(builder, value.ModelSha256);
            AppendDiagnostics(builder, value.DiagnosticCounts);
            builder.Append('}');
        }

        private static void AppendTraceCoverage(
            StringBuilder builder,
            int complete,
            int total)
        {
            builder.Append(",\"sourceTraceCoverage\":{\"complete\":");
            builder.Append(complete.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"total\":");
            builder.Append(total.ToString(CultureInfo.InvariantCulture));
            builder.Append('}');
        }

        private static void AppendDiagnostics(
            StringBuilder builder,
            IReadOnlyDictionary<string, int> counts)
        {
            builder.Append(",\"diagnosticCounts\":{");
            int index = 0;
            foreach (KeyValuePair<string, int> item in counts.OrderBy(
                         value => value.Key,
                         StringComparer.Ordinal))
            {
                if (index++ != 0)
                {
                    builder.Append(',');
                }

                AppendJson(builder, item.Key);
                builder.Append(':');
                builder.Append(item.Value.ToString(CultureInfo.InvariantCulture));
            }

            builder.Append('}');
        }

        private static void AppendNumber(StringBuilder builder, string name, int value)
        {
            builder.Append(',');
            AppendNumberBody(builder, name, value);
        }

        private static void AppendNumberBody(StringBuilder builder, string name, int value)
        {
            AppendJson(builder, name);
            builder.Append(':');
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AppendJson(StringBuilder builder, string value)
        {
            builder.Append('"');
            foreach (char character in value ?? string.Empty)
            {
                switch (character)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (character < 0x20 || character > 0x7e)
                        {
                            builder.Append("\\u");
                            builder.Append(((int)character).ToString(
                                "x4",
                                CultureInfo.InvariantCulture));
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

        private sealed class RulesAggregate
        {
            public RulesAggregate(
                string candidateRole,
                IniTypedViewStatus status,
                int registryTypeCount,
                int registryEntryCount,
                int invalidIdentifierCount,
                int failedIdentifierCount,
                int completeTraceCount,
                int traceCount,
                string modelSha256,
                IReadOnlyDictionary<string, int> diagnosticCounts)
            {
                CandidateRole = candidateRole;
                Status = status;
                RegistryTypeCount = registryTypeCount;
                RegistryEntryCount = registryEntryCount;
                InvalidIdentifierCount = invalidIdentifierCount;
                FailedIdentifierCount = failedIdentifierCount;
                CompleteTraceCount = completeTraceCount;
                TraceCount = traceCount;
                ModelSha256 = modelSha256;
                DiagnosticCounts = diagnosticCounts;
            }

            public string CandidateRole { get; }
            public IniTypedViewStatus Status { get; }
            public int RegistryTypeCount { get; }
            public int RegistryEntryCount { get; }
            public int InvalidIdentifierCount { get; }
            public int FailedIdentifierCount { get; }
            public int CompleteTraceCount { get; }
            public int TraceCount { get; }
            public string ModelSha256 { get; }
            public IReadOnlyDictionary<string, int> DiagnosticCounts { get; }
        }

        private sealed class ArtAggregate
        {
            public ArtAggregate(
                string candidateRole, IniTypedViewStatus status, int recordCount,
                int imageCount, int cameoCount, int altCameoCount, int buildupCount,
                int voxelYes, int voxelNo, int voxelInvalid, int voxelMissing,
                int ambiguousCount, int invalidCount, int missingCount,
                int extensionNone, int extensionShp, int extensionVxl,
                int extensionPal, int extensionOther,
                int routeShp, int routeVxl, int routeUnknown,
                int completeTraceCount, int traceCount, string modelSha256,
                IReadOnlyDictionary<string, int> diagnosticCounts)
            {
                CandidateRole = candidateRole; Status = status; RecordCount = recordCount;
                ImageCount = imageCount; CameoCount = cameoCount;
                AltCameoCount = altCameoCount; BuildupCount = buildupCount;
                VoxelYes = voxelYes; VoxelNo = voxelNo; VoxelInvalid = voxelInvalid;
                VoxelMissing = voxelMissing; AmbiguousCount = ambiguousCount;
                InvalidCount = invalidCount; MissingCount = missingCount;
                ExtensionNone = extensionNone; ExtensionShp = extensionShp;
                ExtensionVxl = extensionVxl; ExtensionPal = extensionPal;
                ExtensionOther = extensionOther; RouteShp = routeShp;
                RouteVxl = routeVxl; RouteUnknown = routeUnknown;
                CompleteTraceCount = completeTraceCount; TraceCount = traceCount;
                ModelSha256 = modelSha256; DiagnosticCounts = diagnosticCounts;
            }

            public string CandidateRole { get; }
            public IniTypedViewStatus Status { get; }
            public int RecordCount { get; }
            public int ImageCount { get; }
            public int CameoCount { get; }
            public int AltCameoCount { get; }
            public int BuildupCount { get; }
            public int VoxelYes { get; }
            public int VoxelNo { get; }
            public int VoxelInvalid { get; }
            public int VoxelMissing { get; }
            public int AmbiguousCount { get; }
            public int InvalidCount { get; }
            public int MissingCount { get; }
            public int ExtensionNone { get; }
            public int ExtensionShp { get; }
            public int ExtensionVxl { get; }
            public int ExtensionPal { get; }
            public int ExtensionOther { get; }
            public int RouteShp { get; }
            public int RouteVxl { get; }
            public int RouteUnknown { get; }
            public int CompleteTraceCount { get; }
            public int TraceCount { get; }
            public string ModelSha256 { get; }
            public IReadOnlyDictionary<string, int> DiagnosticCounts { get; }
        }
    }
}
