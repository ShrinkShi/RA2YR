using System;
using System.Globalization;
using System.Linq;
using System.Text;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Content.Ini.Audit
{
    internal static class IniProjectBaselineAuditSerializer
    {
        public const int SchemaVersion = 1;

        public static byte[] SerializeExternalManifestUtf8(
            IniProjectBaselineAuditModel model,
            long maximumUtf8Bytes)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (maximumUtf8Bytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumUtf8Bytes));
            }

            byte[] bytes;
            try
            {
                bytes = new UTF8Encoding(false, true).GetBytes(
                    Serialize(model, null, true));
            }
            catch (EncoderFallbackException)
            {
                throw Failure("The external INI manifest contains invalid Unicode.");
            }

            if (bytes.LongLength > maximumUtf8Bytes)
            {
                throw Failure("The external INI manifest exceeds its explicit UTF-8 budget.");
            }

            return bytes;
        }

        public static string SerializeSanitizedSummary(
            IniProjectBaselineAuditModel model,
            IniAuditExternalManifestReference externalManifest)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (externalManifest == null)
            {
                throw new ArgumentNullException(nameof(externalManifest));
            }

            return Serialize(model, externalManifest, false);
        }

        private static string Serialize(
            IniProjectBaselineAuditModel model,
            IniAuditExternalManifestReference externalManifest,
            bool includeLineRecords)
        {
            var builder = new StringBuilder();
            builder.Append("{\"schemaVersion\":");
            builder.Append(SchemaVersion.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"manifestType\":");
            AppendJson(builder, includeLineRecords
                ? "RA2YR.IniProjectBaselineAuditExternal"
                : "RA2YR.IniProjectBaselineAuditSanitized");
            builder.Append(",\"baselineLogicalName\":");
            AppendJson(builder, IniProjectBaselineAuditService.BaselineLogicalName);
            builder.Append(",\"auditStatus\":\"Complete\",\"sourceVersion\":");
            AppendJson(builder, model.Source.Version);
            builder.Append(",\"directoryFingerprint\":");
            AppendJson(builder, model.DirectoryFingerprint);
            builder.Append(",\"startedUtc\":");
            AppendJson(builder, model.StartedUtc.ToString("O", CultureInfo.InvariantCulture));
            builder.Append(",\"completedUtc\":");
            AppendJson(builder, model.CompletedUtc.ToString("O", CultureInfo.InvariantCulture));
            if (!includeLineRecords)
            {
                builder.Append(",\"externalManifest\":{\"schemaVersion\":");
                builder.Append(SchemaVersion.ToString(CultureInfo.InvariantCulture));
                builder.Append(",\"cacheRelativePath\":");
                AppendJson(builder, externalManifest.CacheRelativePath);
                builder.Append(",\"length\":");
                builder.Append(externalManifest.Length.ToString(CultureInfo.InvariantCulture));
                builder.Append(",\"sha256\":");
                AppendJson(builder, externalManifest.Sha256);
                builder.Append('}');
            }

            builder.Append(",\"samples\":[");
            for (int index = 0; index < model.Samples.Count; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                AppendSample(builder, model.Samples[index], includeLineRecords);
            }

            builder.Append("],\"survey\":{\"located\":[");
            for (int index = 0; index < model.SurveyCandidates.Count; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                AppendSurveyCandidate(builder, model.SurveyCandidates[index]);
            }

            builder.Append("],\"notLocatedInMountedDirectoryAndMixSources\":[");
            for (int index = 0; index < model.UnresolvedSurveyNames.Count; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                AppendJson(builder, model.UnresolvedSurveyNames[index].Value);
            }

            builder.Append("]}");
            if (!includeLineRecords)
            {
                builder.Append(",\"limitations\":[");
                AppendJson(builder,
                    "Only unmodified byte-identical identity writing is validated; semantic editing and original writer behavior are not implemented.");
                builder.Append(',');
                AppendJson(builder,
                    "Both rulesmd.ini candidates remain independent; no archive-layer winner or runtime precedence is selected.");
                builder.Append(',');
                AppendJson(builder,
                    "No-BOM single-byte text remains raw bytes; a ProjectBaseline code page and runtime decoding policy are unresolved.");
                builder.Append(',');
                AppendJson(builder,
                    "Rules, Art, AI, theater semantics, map overrides, FinalAlert 2 edited roundtrip, and gameplay integration are unimplemented.");
                builder.Append(']');
            }

            builder.Append('}');
            return builder.ToString();
        }

        private static void AppendSample(
            StringBuilder builder,
            IniGoldenSampleRecord sample,
            bool includeLineRecords)
        {
            builder.Append("{\"sampleId\":");
            AppendJson(builder, sample.Specification.SampleId);
            builder.Append(",\"logicalName\":");
            AppendJson(builder, sample.Specification.LogicalName.Value);
            builder.Append(",\"mixId\":");
            AppendJson(builder, sample.Specification.ExpectedMixId.ToString());
            AppendProvenance(builder, sample.Provenance);
            builder.Append(",\"length\":");
            builder.Append(sample.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"sha256\":");
            AppendJson(builder, sample.Sha256);
            builder.Append(",\"bom\":");
            AppendJson(builder, BomName(sample.Document.ByteOrderMarkKind));
            builder.Append(",\"encodingObservation\":");
            AppendJson(builder, EncodingObservation(sample.Document.PhysicalEncoding));
            builder.Append(",\"completeness\":");
            AppendJson(builder, sample.Document.Completeness.ToString());
            builder.Append(",\"lineCount\":");
            builder.Append(sample.Document.Lines.Count.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"lineEndings\":{\"crlf\":");
            builder.Append(sample.CrlfCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"lf\":");
            builder.Append(sample.LfCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"cr\":");
            builder.Append(sample.CrCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"none\":");
            builder.Append(sample.NoEndingCount.ToString(CultureInfo.InvariantCulture));
            builder.Append("},\"nodes\":{\"section\":");
            builder.Append(sample.SectionCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"keyValue\":");
            builder.Append(sample.KeyValueCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"comment\":");
            builder.Append(sample.CommentCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"blank\":");
            builder.Append(sample.BlankCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"opaque\":");
            builder.Append(sample.OpaqueCount.ToString(CultureInfo.InvariantCulture));
            builder.Append("},\"duplicateSectionsRawExact\":");
            builder.Append(sample.DuplicateSectionCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"duplicateKeysRawExactWithinPhysicalSection\":");
            builder.Append(sample.DuplicateKeyCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"maximumLineBytes\":");
            builder.Append(sample.MaximumLineLength.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"canonicalModelSha256\":");
            AppendJson(builder, sample.Document.CanonicalModelSha256);
            builder.Append(",\"identityOutputSha256\":");
            AppendJson(builder, sample.IdentitySha256);
            builder.Append(",\"byteIdentical\":");
            builder.Append(sample.ByteIdentical ? "true" : "false");
            if (includeLineRecords)
            {
                builder.Append(",\"identityCacheRelativePath\":");
                AppendJson(builder, sample.IdentityCacheRelativePath);
            }

            builder.Append(",\"diagnosticCounts\":{");
            int diagnosticIndex = 0;
            foreach (var diagnostic in sample.DiagnosticCounts.OrderBy(
                         item => item.Key,
                         StringComparer.Ordinal))
            {
                if (diagnosticIndex++ != 0)
                {
                    builder.Append(',');
                }

                AppendJson(builder, diagnostic.Key);
                builder.Append(':');
                builder.Append(diagnostic.Value.ToString(CultureInfo.InvariantCulture));
            }

            builder.Append('}');
            if (includeLineRecords)
            {
                builder.Append(",\"lineRecords\":[");
                for (int index = 0; index < sample.LineRecords.Count; index++)
                {
                    if (index != 0)
                    {
                        builder.Append(',');
                    }

                    IniLineAuditRecord record = sample.LineRecords[index];
                    builder.Append("{\"lineId\":");
                    builder.Append(record.LineId.ToString(CultureInfo.InvariantCulture));
                    builder.Append(",\"absoluteOffset\":");
                    builder.Append(record.AbsoluteOffset.ToString(CultureInfo.InvariantCulture));
                    builder.Append(",\"contentLength\":");
                    builder.Append(record.ContentLength.ToString(CultureInfo.InvariantCulture));
                    builder.Append(",\"endingLength\":");
                    builder.Append(record.EndingLength.ToString(CultureInfo.InvariantCulture));
                    builder.Append(",\"ending\":");
                    AppendJson(builder, LineEndingName(record.Ending));
                    builder.Append(",\"nodeKind\":");
                    AppendJson(builder, record.NodeKind.ToString());
                    builder.Append(",\"opaqueReason\":");
                    if (record.OpaqueReason.HasValue)
                    {
                        AppendJson(builder, record.OpaqueReason.Value.ToString());
                    }
                    else
                    {
                        builder.Append("null");
                    }

                    builder.Append(",\"rawLineSha256\":");
                    AppendJson(builder, record.RawLineSha256);
                    builder.Append('}');
                }

                builder.Append(']');
            }

            builder.Append('}');
        }

        private static void AppendSurveyCandidate(
            StringBuilder builder,
            IniSurveyCandidate candidate)
        {
            builder.Append("{\"logicalName\":");
            AppendJson(builder, candidate.LogicalName.Value);
            builder.Append(",\"mixId\":");
            AppendJson(builder, candidate.MixId.ToString());
            AppendProvenance(builder, candidate.Provenance);
            builder.Append(",\"length\":");
            builder.Append(candidate.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"sha256\":");
            AppendJson(builder, candidate.Sha256);
            builder.Append('}');
        }

        private static void AppendProvenance(
            StringBuilder builder,
            IniAuditProvenance provenance)
        {
            builder.Append(",\"provenance\":{\"sourceId\":");
            AppendJson(builder, provenance.SourceId);
            builder.Append(",\"rootArchive\":");
            AppendJson(builder, provenance.RootArchive.Value);
            builder.Append(",\"layers\":[");
            for (int index = 0; index < provenance.Layers.Count; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                IniAuditProvenanceLayer layer = provenance.Layers[index];
                builder.Append("{\"archive\":");
                AppendJson(builder, layer.Archive.Value);
                builder.Append(",\"entryId\":");
                AppendJson(builder, layer.EntryId.ToString());
                builder.Append(",\"resolvedName\":");
                AppendJson(builder, layer.ResolvedName.Value);
                builder.Append('}');
            }

            builder.Append("]}");
        }

        private static string BomName(IniByteOrderMarkKind value)
        {
            switch (value)
            {
                case IniByteOrderMarkKind.None: return "none";
                case IniByteOrderMarkKind.Utf8: return "utf8";
                case IniByteOrderMarkKind.Utf16LittleEndian: return "utf16le";
                case IniByteOrderMarkKind.Utf16BigEndian: return "utf16be";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        private static string EncodingObservation(IniPhysicalEncodingKind value)
        {
            switch (value)
            {
                case IniPhysicalEncodingKind.RawSingleByte:
                    return "raw-single-byte-bom-absent-code-page-unresolved";
                case IniPhysicalEncodingKind.Utf8WithBom:
                    return "utf8-explicit-bom";
                case IniPhysicalEncodingKind.Utf16LittleEndianWithBom:
                    return "utf16le-explicit-bom";
                case IniPhysicalEncodingKind.Utf16BigEndianWithBom:
                    return "utf16be-explicit-bom";
                default:
                    throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        private static string LineEndingName(IniLineEnding value)
        {
            switch (value)
            {
                case IniLineEnding.None: return "none";
                case IniLineEnding.CarriageReturnLineFeed: return "crlf";
                case IniLineEnding.LineFeed: return "lf";
                case IniLineEnding.CarriageReturn: return "cr";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        private static void AppendJson(StringBuilder builder, string value)
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
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
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

        private static IniProjectBaselineAuditException Failure(string message)
        {
            return new IniProjectBaselineAuditException(
                IniProjectBaselineAuditFailureCode.ManifestBudgetExceeded,
                message);
        }
    }
}
