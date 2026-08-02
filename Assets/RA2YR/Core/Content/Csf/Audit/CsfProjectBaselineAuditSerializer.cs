using System;
using System.Globalization;
using System.Text;
using RA2YR.Core.Formats.Csf;

namespace RA2YR.Core.Content.Csf.Audit
{
    internal static class CsfProjectBaselineAuditSerializer
    {
        public const int SchemaVersion = 1;

        public static byte[] SerializeExternalManifestUtf8(
            CsfProjectBaselineAuditModel model,
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
                throw new CsfProjectBaselineAuditException(
                    CsfProjectBaselineAuditFailureCode.ManifestBudgetExceeded,
                    "The external CSF manifest contains invalid Unicode.");
            }

            if (bytes.LongLength > maximumUtf8Bytes)
            {
                throw new CsfProjectBaselineAuditException(
                    CsfProjectBaselineAuditFailureCode.ManifestBudgetExceeded,
                    "The external CSF manifest exceeds its explicit UTF-8 budget.");
            }

            return bytes;
        }

        public static string SerializeSanitizedSummary(
            CsfProjectBaselineAuditModel model,
            CsfAuditExternalManifestReference externalManifest)
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
            CsfProjectBaselineAuditModel model,
            CsfAuditExternalManifestReference externalManifest,
            bool includeRecords)
        {
            var builder = new StringBuilder();
            builder.Append("{\"schemaVersion\":");
            builder.Append(SchemaVersion.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"manifestType\":");
            AppendJson(builder, includeRecords
                ? "RA2YR.CsfProjectBaselineAuditExternal"
                : "RA2YR.CsfProjectBaselineAuditSanitized");
            builder.Append(",\"baselineLogicalName\":");
            AppendJson(builder, CsfProjectBaselineAuditService.BaselineLogicalName);
            builder.Append(",\"auditStatus\":\"Complete\",\"sourceVersion\":");
            AppendJson(builder, model.Source.Version);
            builder.Append(",\"directoryFingerprint\":");
            AppendJson(builder, model.DirectoryFingerprint);
            builder.Append(",\"startedUtc\":");
            AppendJson(builder, model.StartedUtc.ToString("O", CultureInfo.InvariantCulture));
            builder.Append(",\"completedUtc\":");
            AppendJson(builder, model.CompletedUtc.ToString("O", CultureInfo.InvariantCulture));
            if (!includeRecords)
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

            builder.Append(",\"csf\":");
            AppendSample(builder, model.Sample, includeRecords);
            if (!includeRecords)
            {
                builder.Append(",\"limitations\":[");
                AppendJson(builder,
                    "YR1001_ProjectBaseline includes official map, music, and compatibility updates and is not a clean YR 1.001 installation.");
                builder.Append(',');
                AppendJson(builder,
                    "Clean YR 1.001 original comparison and CSF write or roundtrip validation have not been performed.");
                builder.Append(',');
                AppendJson(builder,
                    "Runtime label lookup, duplicate-label precedence, language fallback, UI rendering, and XCC GUI observation remain unimplemented.");
                builder.Append(']');
            }

            builder.Append('}');
            return builder.ToString();
        }

        private static void AppendSample(
            StringBuilder builder,
            CsfGoldenSampleRecord sample,
            bool includeRecords)
        {
            CsfHeader header = sample.Document.Header;
            builder.Append("{\"logicalName\":");
            AppendJson(builder, sample.Specification.LogicalName.Value);
            builder.Append(",\"mixId\":");
            AppendJson(builder, sample.Specification.ExpectedMixId.ToString());
            builder.Append(",\"provenance\":{\"sourceId\":");
            AppendJson(builder, sample.Provenance.SourceId);
            builder.Append(",\"rootArchive\":");
            AppendJson(builder, sample.Provenance.RootArchive.Value);
            builder.Append(",\"layers\":[");
            for (int index = 0; index < sample.Provenance.Layers.Count; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                CsfGoldenProvenanceLayer layer = sample.Provenance.Layers[index];
                builder.Append("{\"archive\":");
                AppendJson(builder, layer.Archive.Value);
                builder.Append(",\"entryId\":");
                AppendJson(builder, layer.EntryId.ToString());
                builder.Append(",\"resolvedName\":");
                AppendJson(builder, layer.ResolvedName.Value);
                builder.Append('}');
            }

            builder.Append("]},\"length\":");
            builder.Append(sample.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"sha256\":");
            AppendJson(builder, sample.Sha256);
            builder.Append(",\"formatVersion\":");
            builder.Append(header.Version.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"rawLanguageCode\":");
            builder.Append(header.Language.RawValue.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"labelRecordCount\":");
            builder.Append(sample.Document.Labels.Count.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"totalValueCount\":");
            builder.Append(sample.TotalValueCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"normalValueCount\":");
            builder.Append(sample.NormalValueCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"extendedValueCount\":");
            builder.Append(sample.ExtendedValueCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"emptyValueCount\":");
            builder.Append(sample.EmptyValueCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"duplicateLabelCount\":");
            builder.Append(sample.DuplicateLabelCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"maximumValuesPerLabel\":");
            builder.Append(sample.MaximumValuesPerLabel.ToString(CultureInfo.InvariantCulture));
            AppendRange(
                builder,
                "labelNameLength",
                sample.MinimumLabelNameLength,
                sample.MaximumLabelNameLength,
                "ascii-bytes");
            AppendRange(
                builder,
                "mainTextLength",
                sample.MinimumMainTextLength,
                sample.MaximumMainTextLength,
                "utf16-code-units");
            AppendRange(
                builder,
                "extendedTextLength",
                sample.MinimumExtendedTextLength,
                sample.MaximumExtendedTextLength,
                "ascii-bytes");
            builder.Append(",\"normalizedModelSha256\":");
            AppendJson(builder, sample.Document.CanonicalModelSha256);
            builder.Append(",\"diagnosticCount\":");
            builder.Append(sample.DiagnosticCount.ToString(CultureInfo.InvariantCulture));
            if (includeRecords)
            {
                builder.Append(",\"records\":[");
                AppendRecords(builder, sample.Document);
                builder.Append(']');
            }

            builder.Append('}');
        }

        private static void AppendRange(
            StringBuilder builder,
            string name,
            int minimum,
            int maximum,
            string unit)
        {
            builder.Append(",\"");
            builder.Append(name);
            builder.Append("\":{\"minimum\":");
            builder.Append(minimum.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"maximum\":");
            builder.Append(maximum.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"unit\":");
            AppendJson(builder, unit);
            builder.Append('}');
        }

        private static void AppendRecords(StringBuilder builder, CsfDocument document)
        {
            for (int labelIndex = 0; labelIndex < document.Labels.Count; labelIndex++)
            {
                if (labelIndex != 0)
                {
                    builder.Append(',');
                }

                CsfLabel label = document.Labels[labelIndex];
                builder.Append("{\"index\":");
                builder.Append(labelIndex.ToString(CultureInfo.InvariantCulture));
                builder.Append(",\"name\":");
                AppendJson(builder, label.Name);
                builder.Append(",\"values\":[");
                for (int valueIndex = 0; valueIndex < label.Values.Count; valueIndex++)
                {
                    if (valueIndex != 0)
                    {
                        builder.Append(',');
                    }

                    CsfValue value = label.Values[valueIndex];
                    builder.Append("{\"index\":");
                    builder.Append(valueIndex.ToString(CultureInfo.InvariantCulture));
                    builder.Append(",\"kind\":");
                    AppendJson(builder, value.Kind == CsfValueKind.Normal
                        ? "Normal"
                        : "Extended");
                    builder.Append(",\"mainText\":");
                    AppendJson(builder, value.Text.CodeUnits);
                    if (value.Kind == CsfValueKind.Extended)
                    {
                        builder.Append(",\"extraText\":");
                        AppendJson(builder, value.ExtraText);
                    }

                    builder.Append('}');
                }

                builder.Append("]}");
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
    }
}
