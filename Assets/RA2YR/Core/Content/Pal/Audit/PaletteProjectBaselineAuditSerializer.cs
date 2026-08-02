using System;
using System.Globalization;
using System.Text;

namespace RA2YR.Core.Content.Pal.Audit
{
    internal static class PaletteProjectBaselineAuditSerializer
    {
        public const int SchemaVersion = 1;

        public static byte[] SerializeExternalManifestUtf8(
            PaletteProjectBaselineAuditModel model,
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

            string json = Serialize(model, null, true);
            byte[] bytes;
            try
            {
                bytes = new UTF8Encoding(false, true).GetBytes(json);
            }
            catch (EncoderFallbackException)
            {
                throw new PaletteProjectBaselineAuditException(
                    PaletteProjectBaselineAuditFailureCode.ManifestBudgetExceeded,
                    "The external palette manifest contains invalid Unicode.");
            }

            if (bytes.LongLength > maximumUtf8Bytes)
            {
                throw new PaletteProjectBaselineAuditException(
                    PaletteProjectBaselineAuditFailureCode.ManifestBudgetExceeded,
                    "The external palette manifest exceeds its explicit UTF-8 budget.");
            }

            return bytes;
        }

        public static string SerializeSanitizedSummary(
            PaletteProjectBaselineAuditModel model,
            PaletteAuditExternalManifestReference externalManifest)
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
            PaletteProjectBaselineAuditModel model,
            PaletteAuditExternalManifestReference externalManifest,
            bool includeRawColors)
        {
            var builder = new StringBuilder();
            builder.Append("{\"schemaVersion\":");
            builder.Append(SchemaVersion.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"manifestType\":");
            AppendJson(builder, includeRawColors
                ? "RA2YR.PaletteProjectBaselineAuditExternal"
                : "RA2YR.PaletteProjectBaselineAuditSanitized");
            builder.Append(",\"baselineLogicalName\":");
            AppendJson(builder, PaletteProjectBaselineAuditService.BaselineLogicalName);
            builder.Append(",\"auditStatus\":\"Complete\",\"sourceVersion\":");
            AppendJson(builder, model.Source.Version);
            builder.Append(",\"directoryFingerprint\":");
            AppendJson(builder, model.DirectoryFingerprint);
            builder.Append(",\"startedUtc\":");
            AppendJson(builder, model.StartedUtc.ToString("O", CultureInfo.InvariantCulture));
            builder.Append(",\"completedUtc\":");
            AppendJson(builder, model.CompletedUtc.ToString("O", CultureInfo.InvariantCulture));
            if (!includeRawColors)
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

            builder.Append(",\"palettes\":[");
            for (int index = 0; index < model.Palettes.Count; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                AppendPalette(builder, model.Palettes[index], includeRawColors);
            }

            builder.Append(']');
            if (!includeRawColors)
            {
                builder.Append(",\"limitations\":[");
                AppendJson(builder,
                    "YR1001_ProjectBaseline includes official map, music, and compatibility updates and is not a clean YR 1.001 installation.");
                builder.Append(',');
                AppendJson(builder,
                    "Clean YR 1.001 original comparison and visual rendering validation have not been performed.");
                builder.Append(',');
                AppendJson(builder,
                    "XccScaleToFullRangeFloor is recorded as a reference strategy and is not claimed as the original or default renderer behavior.");
                builder.Append(']');
            }

            builder.Append('}');
            return builder.ToString();
        }

        private static void AppendPalette(
            StringBuilder builder,
            PaletteGoldenSampleRecord palette,
            bool includeRawColors)
        {
            builder.Append("{\"logicalName\":");
            AppendJson(builder, palette.Specification.LogicalName.Value);
            builder.Append(",\"mixId\":");
            AppendJson(builder, palette.Specification.ExpectedMixId.ToString());
            builder.Append(",\"provenance\":{\"sourceId\":");
            AppendJson(builder, palette.Provenance.SourceId);
            builder.Append(",\"rootArchive\":");
            AppendJson(builder, palette.Provenance.RootArchive.Value);
            builder.Append(",\"layers\":[");
            for (int index = 0; index < palette.Provenance.Layers.Count; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                PaletteGoldenProvenanceLayer layer = palette.Provenance.Layers[index];
                builder.Append("{\"archive\":");
                AppendJson(builder, layer.Archive.Value);
                builder.Append(",\"entryId\":");
                AppendJson(builder, layer.EntryId.ToString());
                builder.Append(",\"resolvedName\":");
                AppendJson(builder, layer.ResolvedName.Value);
                builder.Append('}');
            }

            builder.Append("]},\"length\":");
            builder.Append(palette.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"sha256\":");
            AppendJson(builder, palette.Sha256);
            builder.Append(",\"colorCount\":");
            builder.Append(palette.ColorCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"rawChannelMin\":");
            builder.Append(palette.RawChannelMin.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"rawChannelMax\":");
            builder.Append(palette.RawChannelMax.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"invalidChannelCount\":");
            builder.Append(palette.InvalidChannelCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"distinctColorCount\":");
            builder.Append(palette.DistinctColorCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"normalizedModelSha256\":");
            AppendJson(builder, palette.NormalizedModelSha256);
            builder.Append(",\"displayConversionStrategy\":");
            AppendJson(builder, palette.DisplayConversionStrategy);
            builder.Append(",\"diagnosticCount\":");
            builder.Append(palette.DiagnosticCount.ToString(CultureInfo.InvariantCulture));
            if (includeRawColors)
            {
                byte[] raw = palette.GetRawRgbTripletsCopy();
                builder.Append(",\"rawColors\":[");
                for (int index = 0; index < palette.ColorCount; index++)
                {
                    if (index != 0)
                    {
                        builder.Append(',');
                    }

                    int offset = checked(index * 3);
                    builder.Append("{\"index\":");
                    builder.Append(index.ToString(CultureInfo.InvariantCulture));
                    builder.Append(",\"r\":");
                    builder.Append(raw[offset].ToString(CultureInfo.InvariantCulture));
                    builder.Append(",\"g\":");
                    builder.Append(raw[offset + 1].ToString(CultureInfo.InvariantCulture));
                    builder.Append(",\"b\":");
                    builder.Append(raw[offset + 2].ToString(CultureInfo.InvariantCulture));
                    builder.Append('}');
                }

                builder.Append(']');
            }

            builder.Append('}');
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
                        if (char.IsControl(character))
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
