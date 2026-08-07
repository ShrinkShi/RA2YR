using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using RA2YR.Core.Formats.ShpTs;

namespace RA2YR.Core.Content.ShpTs.Audit
{
    internal static class ShpTsProjectBaselineAuditSerializer
    {
        public const int SchemaVersion = 1;

        public static byte[] SerializeExternalManifestUtf8(
            ShpTsProjectBaselineAuditModel model,
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
                bytes = new UTF8Encoding(false, true).GetBytes(Serialize(model, null, true));
            }
            catch (EncoderFallbackException)
            {
                throw Failure("The external SHP manifest contains invalid Unicode.");
            }

            if (bytes.LongLength > maximumUtf8Bytes)
            {
                throw Failure("The external SHP manifest exceeds its UTF-8 budget.");
            }

            return bytes;
        }

        public static string SerializeSanitizedSummary(
            ShpTsProjectBaselineAuditModel model,
            ShpTsAuditExternalManifestReference externalManifest)
        {
            if (model == null || externalManifest == null)
            {
                throw new ArgumentNullException(model == null
                    ? nameof(model)
                    : nameof(externalManifest));
            }

            return Serialize(model, externalManifest, false);
        }

        private static string Serialize(
            ShpTsProjectBaselineAuditModel model,
            ShpTsAuditExternalManifestReference external,
            bool includeFrames)
        {
            var builder = new StringBuilder();
            builder.Append("{\"schemaVersion\":");
            Number(builder, SchemaVersion);
            builder.Append(",\"manifestType\":");
            Json(builder, includeFrames
                ? "RA2YR.ShpTsProjectBaselineAuditExternal"
                : "RA2YR.ShpTsProjectBaselineAuditSanitized");
            builder.Append(",\"baselineLogicalName\":");
            Json(builder, ShpTsProjectBaselineAuditService.BaselineLogicalName);
            builder.Append(",\"auditStatus\":");
            Json(builder, GetStatus(model).ToString());
            builder.Append(",\"sourceVersion\":");
            Json(builder, model.Source.Version);
            builder.Append(",\"directoryFingerprint\":");
            Json(builder, model.DirectoryFingerprint);
            builder.Append(",\"startedUtc\":");
            Json(builder, model.StartedUtc.ToString("O", CultureInfo.InvariantCulture));
            builder.Append(",\"completedUtc\":");
            Json(builder, model.CompletedUtc.ToString("O", CultureInfo.InvariantCulture));
            if (!includeFrames)
            {
                builder.Append(",\"externalManifest\":{\"cacheRelativePath\":");
                Json(builder, external.CacheRelativePath);
                builder.Append(",\"length\":");
                Number(builder, external.Length);
                builder.Append(",\"sha256\":");
                Json(builder, external.Sha256);
                builder.Append('}');
            }

            builder.Append(",\"samples\":[");
            for (int index = 0; index < model.Samples.Count; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                AppendSample(builder, model.Samples[index], includeFrames);
            }

            builder.Append(']');
            if (!includeFrames)
            {
                builder.Append(",\"limitations\":[");
                Json(builder,
                    "SelectionBasis reports evidence provenance and does not claim stock runtime precedence.");
                builder.Append(',');
                Json(builder,
                    "Flags 2, unknown flags, and 00 00 command semantics remain unresolved when observed.");
                builder.Append(',');
                Json(builder,
                    "RLE-Zero decoding remains strict; ProjectBaseline rows that exceed their declared local width are recorded as decode failures and do not promote compatibility.");
                builder.Append(',');
                Json(builder,
                    "00 00 counts cover only commands reached before strict termination; each failing ProjectBaseline RLE frame stopped on row zero before later rows could be audited.");
                builder.Append(',');
                Json(builder,
                    "Palette binding, RGBA rendering, remap, shadow pairing, writer, and visual comparison are not implemented.");
                builder.Append(']');
            }

            builder.Append('}');
            return builder.ToString();
        }

        private static ShpTsProjectBaselineAuditStatus GetStatus(
            ShpTsProjectBaselineAuditModel model)
        {
            int failed = model.Samples.Sum(value => value.FailedFrameCount);
            if (failed != 0)
            {
                return ShpTsProjectBaselineAuditStatus.CompleteWithDecodeFailures;
            }

            return model.Samples.Sum(value => value.UnresolvedFrameCount) == 0
                ? ShpTsProjectBaselineAuditStatus.Complete
                : ShpTsProjectBaselineAuditStatus.CompleteWithUnresolvedFrames;
        }

        private static void AppendSample(
            StringBuilder builder,
            ShpTsGoldenSampleRecord sample,
            bool includeFrames)
        {
            ShpTsFrameDescriptor[] descriptors = sample.Directory.Frames.ToArray();
            ShpTsAuditFrameRecord[] decoded = sample.Frames.ToArray();
            builder.Append("{\"sampleId\":");
            Json(builder, sample.Specification.SampleId);
            builder.Append(",\"logicalRole\":");
            Json(builder, sample.Specification.LogicalRole);
            builder.Append(",\"selectionBasis\":");
            Json(builder, sample.Specification.SelectionBasis.ToString());
            if (includeFrames)
            {
                builder.Append(",\"logicalName\":");
                Json(builder, sample.Specification.LogicalName.Value);
            }

            builder.Append(",\"mixId\":");
            Json(builder, sample.Specification.ExpectedMixId.ToString());
            builder.Append(",\"provenance\":{\"rootArchive\":");
            Json(builder, sample.Specification.RootArchive.Value);
            builder.Append(",\"layers\":[");
            for (int index = 0; index < sample.ProvenanceLayers.Count; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                ShpTsAuditProvenanceLayer layer = sample.ProvenanceLayers[index];
                builder.Append("{\"archive\":");
                Json(builder, layer.Archive.Value);
                builder.Append(",\"entryId\":");
                Json(builder, layer.EntryId.ToString());
                builder.Append(",\"resolvedName\":");
                Json(builder, includeFrames ? layer.ResolvedName.Value : "resolved");
                builder.Append('}');
            }

            builder.Append("]},\"length\":");
            Number(builder, sample.Length);
            builder.Append(",\"sha256\":");
            Json(builder, sample.Sha256);
            builder.Append(",\"frameCount\":");
            Number(builder, sample.Directory.Header.FrameCountRaw);
            builder.Append(",\"canvas\":{\"width\":");
            Number(builder, sample.Directory.Header.CanvasWidthRaw);
            builder.Append(",\"height\":");
            Number(builder, sample.Directory.Header.CanvasHeightRaw);
            builder.Append("},\"frameRectangleRange\":{");
            AppendRange(builder, "xRaw", descriptors.Select(value => (long)value.XRaw));
            builder.Append(',');
            AppendRange(builder, "yRaw", descriptors.Select(value => (long)value.YRaw));
            builder.Append(',');
            AppendRange(builder, "widthRaw", descriptors.Select(value => (long)value.WidthRaw));
            builder.Append(',');
            AppendRange(builder, "heightRaw", descriptors.Select(value => (long)value.HeightRaw));
            builder.Append("},\"flags\":{");
            AppendCount(builder, "raw0", descriptors.Count(value => value.RawFlags == 0));
            builder.Append(',');
            AppendCount(builder, "raw1", descriptors.Count(value => value.RawFlags == 1));
            builder.Append(',');
            AppendCount(builder, "raw2", descriptors.Count(value => value.RawFlags == 2));
            builder.Append(',');
            AppendCount(builder, "rle3", descriptors.Count(value => value.RawFlags == 3));
            builder.Append(',');
            AppendCount(builder, "unknown", descriptors.Count(value => value.RawFlags > 3));
            builder.Append("},\"emptyFrameCount\":");
            Number(builder, descriptors.Count(value => value.IsCanonicalEmpty));
            builder.Append(",\"reservedNonZeroCount\":");
            Number(builder, descriptors.Count(value => value.ReservedRaw != 0));
            builder.Append(",\"coordinateHighBitCount\":");
            Number(builder, descriptors.Count(value =>
                (value.XRaw & 0x8000) != 0 || (value.YRaw & 0x8000) != 0));
            builder.Append(",\"offsetAggregation\":{");
            AppendCount(builder, "unalignedCount", descriptors.Count(value =>
                !value.IsCanonicalEmpty && (value.DataOffsetRaw & 7u) != 0));
            builder.Append(',');
            AppendCount(builder, "duplicateOffsetFrameCount", descriptors
                .Where(value => !value.IsCanonicalEmpty)
                .GroupBy(value => value.DataOffsetRaw)
                .Where(group => group.Count() > 1)
                .Sum(group => group.Count()));
            builder.Append(',');
            AppendCount(builder, "descendingOffsetCount", CountDescending(descriptors));
            builder.Append("},\"paddingAggregation\":{");
            AppendCount(builder, "frameCount", decoded.Count(value => value.PaddingBytes != 0));
            builder.Append(',');
            builder.Append("\"totalBytes\":");
            Number(builder, decoded.Sum(value => value.PaddingBytes));
            builder.Append(',');
            AppendRange(builder, "bytes", decoded.Select(value => value.PaddingBytes));
            builder.Append("},\"decodedIndexRange\":{");
            ShpTsAuditFrameRecord[] pixelFrames = decoded.Where(value => value.PixelCount != 0)
                .ToArray();
            builder.Append("\"min\":");
            Number(builder, pixelFrames.Length == 0
                ? 0
                : pixelFrames.Min(value => value.MinimumIndex));
            builder.Append(",\"max\":");
            Number(builder, pixelFrames.Length == 0
                ? 0
                : pixelFrames.Max(value => value.MaximumIndex));
            builder.Append("},\"decodedPixelCount\":");
            Number(builder, decoded.Sum(value => (long)value.PixelCount));
            builder.Append(",\"zeroZeroUnresolvedCount\":");
            Number(builder, sample.Diagnostics.Count(value =>
                value.Code == ShpTsDiagnosticCode.ZeroOutputCommandSemanticsUnresolved));
            builder.Append(",\"unresolvedFrameCount\":");
            Number(builder, sample.UnresolvedFrameCount);
            builder.Append(",\"failedFrameCount\":");
            Number(builder, sample.FailedFrameCount);
            builder.Append(",\"directoryModelSha256\":");
            Json(builder, sample.Directory.CanonicalDirectoryModelSha256);
            builder.Append(",\"decodedModelSha256\":");
            Json(builder, sample.Decoded.CanonicalDecodedModelSha256);
            builder.Append(",\"memoryStreamMixWindowEquivalent\":true");
            builder.Append(",\"diagnosticCounts\":");
            AppendDiagnosticCounts(builder, sample.Diagnostics);
            if (includeFrames)
            {
                builder.Append(",\"frames\":[");
                for (int index = 0; index < sample.Frames.Count; index++)
                {
                    if (index != 0)
                    {
                        builder.Append(',');
                    }

                    AppendFrame(builder, sample.Frames[index]);
                }

                builder.Append(']');
            }

            builder.Append('}');
        }

        private static void AppendFrame(
            StringBuilder builder,
            ShpTsAuditFrameRecord frame)
        {
            ShpTsFrameDescriptor descriptor = frame.Descriptor;
            builder.Append("{\"index\":");
            Number(builder, descriptor.Index);
            builder.Append(",\"descriptorOffsetRelative\":");
            Number(builder, 8L + descriptor.Index * 24L);
            builder.Append(",\"xRaw\":");
            Number(builder, descriptor.XRaw);
            builder.Append(",\"yRaw\":");
            Number(builder, descriptor.YRaw);
            builder.Append(",\"widthRaw\":");
            Number(builder, descriptor.WidthRaw);
            builder.Append(",\"heightRaw\":");
            Number(builder, descriptor.HeightRaw);
            builder.Append(",\"rawFlags\":");
            Number(builder, descriptor.RawFlags);
            builder.Append(",\"reservedRaw\":");
            Number(builder, descriptor.ReservedRaw);
            builder.Append(",\"dataOffsetRaw\":");
            Number(builder, descriptor.DataOffsetRaw);
            builder.Append(",\"decodeStatus\":");
            Json(builder, frame.DecodeStatus);
            builder.Append(",\"bytesConsumed\":");
            Number(builder, frame.BytesConsumed);
            builder.Append(",\"paddingBytes\":");
            Number(builder, frame.PaddingBytes);
            builder.Append(",\"pixelCount\":");
            Number(builder, frame.PixelCount);
            builder.Append(",\"minimumIndex\":");
            Number(builder, frame.MinimumIndex);
            builder.Append(",\"maximumIndex\":");
            Number(builder, frame.MaximumIndex);
            builder.Append(",\"diagnostics\":[");
            for (int index = 0; index < frame.Diagnostics.Count; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                ShpTsDiagnostic diagnostic = frame.Diagnostics[index];
                builder.Append("{\"code\":");
                Json(builder, diagnostic.Code.ToString());
                builder.Append(",\"rowIndex\":");
                Number(builder, diagnostic.RowIndex);
                builder.Append(",\"offsetRelativeToEntry\":");
                Number(builder, diagnostic.AbsoluteOffset -
                    (frame.Descriptor.DataAbsoluteOffset - frame.Descriptor.DataOffsetRaw));
                builder.Append(",\"requestedLength\":");
                Number(builder, diagnostic.RequestedLength);
                builder.Append(",\"remainingLength\":");
                Number(builder, diagnostic.RemainingLength);
                builder.Append(",\"message\":");
                Json(builder, diagnostic.Message);
                builder.Append('}');
            }

            builder.Append("]}");
        }

        private static int CountDescending(IReadOnlyList<ShpTsFrameDescriptor> values)
        {
            uint previous = 0;
            bool havePrevious = false;
            int count = 0;
            foreach (ShpTsFrameDescriptor value in values.Where(item => !item.IsCanonicalEmpty))
            {
                if (havePrevious && value.DataOffsetRaw < previous)
                {
                    count++;
                }

                previous = value.DataOffsetRaw;
                havePrevious = true;
            }

            return count;
        }

        private static void AppendDiagnosticCounts(
            StringBuilder builder,
            IEnumerable<ShpTsDiagnostic> diagnostics)
        {
            builder.Append('{');
            int index = 0;
            foreach (IGrouping<string, ShpTsDiagnostic> group in diagnostics
                         .GroupBy(value => value.Code.ToString(), StringComparer.Ordinal)
                         .OrderBy(value => value.Key, StringComparer.Ordinal))
            {
                if (index++ != 0)
                {
                    builder.Append(',');
                }

                Json(builder, group.Key);
                builder.Append(':');
                Number(builder, group.Count());
            }

            builder.Append('}');
        }

        private static void AppendRange(
            StringBuilder builder,
            string name,
            IEnumerable<long> values)
        {
            long[] array = values.ToArray();
            Json(builder, name);
            builder.Append(":{\"min\":");
            Number(builder, array.Length == 0 ? 0 : array.Min());
            builder.Append(",\"max\":");
            Number(builder, array.Length == 0 ? 0 : array.Max());
            builder.Append('}');
        }

        private static void AppendCount(StringBuilder builder, string name, int value)
        {
            Json(builder, name);
            builder.Append(':');
            Number(builder, value);
        }

        private static void Number(StringBuilder builder, long value)
        {
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void Json(StringBuilder builder, string value)
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
                            builder.Append(((int)character).ToString(
                                "x4", CultureInfo.InvariantCulture));
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

        private static ShpTsProjectBaselineAuditException Failure(string message)
        {
            return new ShpTsProjectBaselineAuditException(
                ShpTsProjectBaselineAuditFailureCode.ManifestBudgetExceeded,
                message);
        }
    }
}
