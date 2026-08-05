using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RA2YR.Core.Content.ShpTs.Audit;

namespace RA2YR.Core.Content.ShpTs.Forensics
{
    internal static class ShpTsRleForensicSerializer
    {
        public static byte[] SerializeExternalManifestUtf8(
            ShpTsRleForensicAuditModel model,
            long maximumBytes)
        {
            if (model == null || maximumBytes <= 0)
            {
                throw new ArgumentException("The forensic manifest budget is invalid.");
            }

            byte[] bytes = new UTF8Encoding(false, true).GetBytes(
                Serialize(model, null, true));
            if (bytes.LongLength > maximumBytes)
            {
                throw new ShpTsRleForensicAuditException(
                    ShpTsRleForensicAuditFailureCode.ManifestBudgetExceeded,
                    "The forensic external manifest exceeds its explicit budget.");
            }
            return bytes;
        }

        public static string SerializeSanitizedSummary(
            ShpTsRleForensicAuditModel model,
            ShpTsAuditExternalManifestReference external)
        {
            if (model == null || external == null)
            {
                throw new ArgumentNullException(model == null ? nameof(model) : nameof(external));
            }
            return Serialize(model, external, false);
        }

        private static string Serialize(
            ShpTsRleForensicAuditModel model,
            ShpTsAuditExternalManifestReference external,
            bool includeRows)
        {
            var builder = new StringBuilder();
            builder.Append('{');
            Property(builder, "manifestType");
            Json(builder, includeRows
                ? "RA2YR.ShpTsRleForensicExternal"
                : "RA2YR.ShpTsRleForensicSanitized");
            builder.Append(",\"schemaVersion\":1");
            builder.Append(",\"baselineLogicalName\":\"YR1001_ProjectBaseline\"");
            builder.Append(",\"directoryFingerprint\":");
            Json(builder, model.DirectoryFingerprint);
            builder.Append(",\"inputCatalogSha256\":");
            Json(builder, model.InputCatalogSha256);
            builder.Append(",\"canonicalModelSha256\":");
            Json(builder, model.CanonicalModelSha256);
            builder.Append(",\"startedUtc\":");
            Json(builder, model.StartedUtc.ToString("O"));
            builder.Append(",\"completedUtc\":");
            Json(builder, model.CompletedUtc.ToString("O"));
            AppendStageA(builder, model.Records);
            AppendStageB(builder, model.Records, model.StageBExecuted);
            AppendHypotheses(builder, model);
            builder.Append(",\"decision\":");
            Json(builder, model.Decision.ToString());
            builder.Append(",\"productionRepairRecommended\":")
                .Append(model.ProductionRepairRecommended ? "true" : "false");
            builder.Append(",\"inputModesEquivalent\":")
                .Append(model.InputModesEquivalent ? "true" : "false");
            builder.Append(",\"diagnosticCounts\":{}");
            if (external != null)
            {
                builder.Append(",\"externalManifest\":{");
                Property(builder, "cacheRelativePath");
                Json(builder, external.CacheRelativePath);
                builder.Append(",\"length\":").Append(external.Length);
                builder.Append(",\"sha256\":");
                Json(builder, external.Sha256);
                builder.Append('}');
            }
            if (includeRows)
            {
                AppendExternalRecords(builder, model.Records);
            }
            builder.Append(",\"limitations\":[");
            Json(builder, "The production decoder remains strict and unchanged.");
            builder.Append(',');
            Json(builder, "Per-frame and per-row records exist only in this repository-external manifest.");
            builder.Append(',');
            Json(builder, "No original row bytes, command arrays, indices, pixels, images, Base64, hex dumps, or host paths are serialized.");
            builder.Append(',');
            Json(builder, "ProjectBaseline is patched development content, not a clean YR 1.001 corpus.");
            builder.Append("]}");
            return builder.ToString();
        }

        private static void AppendStageA(
            StringBuilder builder,
            IReadOnlyList<ShpTsRleForensicFrameRecord> records)
        {
            ShpTsRleForensicRowScalar[] rows = records.Select(value => value.StageARow).ToArray();
            builder.Append(",\"stageA\":{");
            builder.Append("\"frameCount\":").Append(records.Count);
            builder.Append(",\"productionFailureCode\":\"RleOutputOverflow\"");
            builder.Append(",\"productionFailureRow\":0");
            builder.Append(",\"widthRange\":{");
            builder.Append("\"min\":").Append(records.Min(value => value.Width));
            builder.Append(",\"max\":").Append(records.Max(value => value.Width)).Append('}');
            builder.Append(",\"parity\":{");
            builder.Append("\"odd\":").Append(records.Count(value => (value.Width & 1) != 0));
            builder.Append(",\"even\":").Append(records.Count(value => (value.Width & 1) == 0)).Append('}');
            builder.Append(",\"commands\":{");
            builder.Append("\"total\":").Append(rows.Sum(value => value.CommandCount));
            builder.Append(",\"literal\":").Append(rows.Sum(value => value.LiteralCount));
            builder.Append(",\"zeroRun\":").Append(rows.Sum(value => value.ZeroRunCount));
            builder.Append(",\"zeroZero\":").Append(rows.Sum(value => value.ZeroZeroCount)).Append('}');
            AppendEnumCounts(builder, "extraSource", rows.Select(value => value.ExtraSource));
            AppendBooleanCounts(builder, "extraIsLastOutput", rows.Select(value => value.ExtraIsLastOutput));
            AppendBooleanCounts(builder, "extraFromLastCommand", rows.Select(value => value.ExtraFromLastCommand));
            AppendBooleanCounts(builder, "extraIsZero", rows.Select(value => value.ExtraIsZero));
            AppendBooleanCounts(builder, "ignoreOneExtraInputExact", rows.Select(value => value.IgnoreOneExtraInputExact));
            AppendEnumCounts(builder, "remainingAtWidth", rows.Select(value => value.RemainingClass));
            long[] threeOrMore = rows.Where(value =>
                    value.RemainingClass == ShpTsRleForensicRemainingClass.ThreeOrMore)
                .Select(value => value.RemainingBytesAtWidth)
                .ToArray();
            builder.Append(",\"remainingThreeOrMoreRange\":{");
            builder.Append("\"count\":").Append(threeOrMore.Length);
            builder.Append(",\"min\":").Append(threeOrMore.Length == 0 ? 0 : threeOrMore.Min());
            builder.Append(",\"max\":").Append(threeOrMore.Length == 0 ? 0 : threeOrMore.Max()).Append('}');
            AppendEnumCounts(builder, "finalCommand", rows.Select(value => value.FinalCommandKind));
            AppendBuckets(builder, "finalZeroRunCountBuckets", rows
                .Where(value => value.FinalCommandKind == ShpTsRleForensicCommandKind.ZeroRun)
                .Select(value => (long)value.FinalZeroRunCount));
            AppendBuckets(builder, "distanceBeforeFinalZeroRunBuckets", rows
                .Where(value => value.FinalCommandKind == ShpTsRleForensicCommandKind.ZeroRun)
                .Select(value => value.DistanceBeforeFinalZeroRun));
            builder.Append(",\"overshootExactlyOne\":")
                .Append(rows.Count(value => value.ExtraOvershoot == 1));
            AppendSemantic(builder, "strictMechanical", rows,
                value => value.MechanicalLengthClass,
                value => value.InputExact);
            AppendSemantic(builder, "openRaStyle", rows,
                value => value.OpenRaLengthClass,
                value => value.InputExact);
            AppendSemantic(builder, "xccZeroRunClip", rows,
                value => value.XccVisibleLengthClass,
                value => value.InputExact);
            AppendSemantic(builder, "lineLengthIncludingHeader", rows,
                value => value.MechanicalLengthClass,
                value => value.InputExact);
            AppendSemantic(builder, "lineLengthExcludingHeader", rows,
                value => value.NoHeaderLengthClass,
                value => !value.NoHeaderMalformed);
            AppendSemantic(builder, "finalTransparentGuardClassifier", rows,
                value => value.GuardClassifierLengthClass,
                value => value.InputExact);
            builder.Append(",\"categoryCross\":[");
            AppendCategoryStageA(builder, records);
            builder.Append(']');
            builder.Append(",\"parityCross\":[");
            AppendParity(builder, records, true);
            builder.Append(',');
            AppendParity(builder, records, false);
            builder.Append("]}");
        }

        private static void AppendStageB(
            StringBuilder builder,
            IReadOnlyList<ShpTsRleForensicFrameRecord> records,
            bool executed)
        {
            ShpTsRleForensicFrameAnalysis[] frames = records
                .Where(value => value.StageB != null)
                .Select(value => value.StageB)
                .ToArray();
            ShpTsRleForensicRowScalar[] rows = frames.SelectMany(value => value.Rows).ToArray();
            builder.Append(",\"stageB\":{");
            builder.Append("\"executed\":").Append(executed ? "true" : "false");
            builder.Append(",\"analyzedRows\":").Append(rows.Length);
            builder.Append(",\"mechanicalWidth\":")
                .Append(rows.Count(value => value.MechanicalLengthClass == ShpTsRleForensicLengthClass.Width));
            builder.Append(",\"mechanicalWidthPlusOne\":")
                .Append(rows.Count(value => value.MechanicalLengthClass == ShpTsRleForensicLengthClass.WidthPlusOne));
            builder.Append(",\"mechanicalOther\":")
                .Append(rows.Count(value => value.MechanicalLengthClass != ShpTsRleForensicLengthClass.Width &&
                    value.MechanicalLengthClass != ShpTsRleForensicLengthClass.WidthPlusOne));
            builder.Append(",\"extraFromFinalZeroRun\":")
                .Append(rows.Count(value => value.ExtraSource == ShpTsRleForensicExtraSource.ZeroRun &&
                    value.ExtraFromLastCommand));
            builder.Append(",\"extraIsZero\":")
                .Append(rows.Count(value => value.ExtraSource != ShpTsRleForensicExtraSource.None &&
                    value.ExtraIsZero));
            builder.Append(",\"literalOverflowRows\":")
                .Append(rows.Count(value => value.LiteralOverflow));
            builder.Append(",\"zeroZeroRows\":")
                .Append(rows.Count(value => value.ZeroZeroCount != 0));
            builder.Append(",\"malformedRows\":")
                .Append(frames.Count(value => !value.IsSuccess));
            builder.Append(",\"ignoreOneExtraInputExact\":")
                .Append(rows.Count(value => value.IgnoreOneExtraInputExact));
            builder.Append(",\"framesAllRowsGuardPattern\":")
                .Append(frames.Count(value => value.IsSuccess && value.Rows.Count != 0 &&
                    value.Rows.All(row => row.GuardPattern)));
            builder.Append(",\"framesMixedPattern\":")
                .Append(frames.Count(value => value.IsSuccess &&
                    value.Rows.Any(row => row.GuardPattern) &&
                    value.Rows.Any(row => !row.GuardPattern)));
            builder.Append(",\"categoryRows\":[");
            bool first = true;
            foreach (IGrouping<ShpTsRleForensicCategory, ShpTsRleForensicFrameRecord> group in
                     records.GroupBy(value => value.Category).OrderBy(value => value.Key))
            {
                if (!first)
                {
                    builder.Append(',');
                }
                first = false;
                ShpTsRleForensicRowScalar[] categoryRows = group
                    .Where(value => value.StageB != null)
                    .SelectMany(value => value.StageB.Rows)
                    .ToArray();
                builder.Append('{');
                Property(builder, "category");
                Json(builder, group.Key.ToString());
                builder.Append(",\"rows\":").Append(categoryRows.Length);
                builder.Append(",\"width\":")
                    .Append(categoryRows.Count(value => value.MechanicalLengthClass == ShpTsRleForensicLengthClass.Width));
                builder.Append(",\"widthPlusOne\":")
                    .Append(categoryRows.Count(value => value.MechanicalLengthClass == ShpTsRleForensicLengthClass.WidthPlusOne));
                builder.Append(",\"other\":")
                    .Append(categoryRows.Count(value => value.MechanicalLengthClass != ShpTsRleForensicLengthClass.Width &&
                        value.MechanicalLengthClass != ShpTsRleForensicLengthClass.WidthPlusOne));
                builder.Append('}');
            }
            builder.Append("]}");
        }

        private static void AppendHypotheses(
            StringBuilder builder,
            ShpTsRleForensicAuditModel model)
        {
            ShpTsRleForensicRowScalar[] stageA = model.Records
                .Select(value => value.StageARow).ToArray();
            bool allFinalZeroRun = stageA.All(value => value.GuardPattern);
            bool anyLiteral = stageA.Any(value =>
                value.ExtraSource == ShpTsRleForensicExtraSource.Literal);
            bool noHeaderStable = stageA.All(value => !value.NoHeaderMalformed &&
                value.NoHeaderLengthClass == ShpTsRleForensicLengthClass.WidthPlusOne);
            ShpTsRleForensicRowScalar[] stageB = model.Records
                .Where(value => value.StageB != null)
                .SelectMany(value => value.StageB.Rows)
                .ToArray();
            bool stageBAllGuard = stageB.Length != 0 &&
                stageB.All(value => value.GuardPattern);
            bool stageBMixed = stageB.Any(value => value.GuardPattern) &&
                stageB.Any(value => !value.GuardPattern);
            builder.Append(",\"hypotheses\":{");
            AppendHypothesis(builder, "H1", stageBMixed
                ? "final-zero-run-boundary-explains-only-width-plus-one-rows"
                : allFinalZeroRun
                    ? "narrowed-to-final-zero-run-boundary"
                : "unresolved-command-semantics", true);
            AppendHypothesis(builder, "H2", stageB.Any(value =>
                    value.MechanicalLengthClass == ShpTsRleForensicLengthClass.Width)
                ? "rejected-width-is-not-universally-inclusive"
                : "reduced-by-raw-width-and-rle-only-conflict", false);
            AppendHypothesis(builder, "H3", anyLiteral
                ? "supported-by-literal-extra"
                : "rejected-extra-is-not-literal", false);
            AppendHypothesis(builder, "H4", noHeaderStable
                ? "not-rejected-by-no-header-classification"
                : "reduced-no-header-interpretation-is-unstable", false);
            AppendHypothesis(builder, "H5", allFinalZeroRun
                ? "reduced-to-final-run-not-general-count-offset"
                : "unresolved", false);
            AppendHypothesis(builder, "H6", stageBAllGuard
                ? "supported-across-all-analyzed-rows"
                : stageBMixed
                    ? "supported-for-width-plus-one-rows-but-not-universal"
                    : allFinalZeroRun
                        ? "supported-by-stage-a-await-stage-b"
                : "rejected-by-stage-a", false);
            AppendHypothesis(builder, "H7", stageBAllGuard
                ? "supported-aggregately-await-independent-runtime-evidence"
                : stageBMixed
                    ? "present-but-not-a-universal-row-contract"
                    : allFinalZeroRun
                        ? "supported-by-transparent-extra-await-independent-runtime-evidence"
                : "rejected-by-stage-a", false);
            AppendHypothesis(builder, "H8", stageBMixed
                ? "reduced-to-two-mechanical-row-width-classes-without-second-field"
                : allFinalZeroRun
                    ? "reduced-extra-is-command-local-not-independent-span"
                : "unresolved", false);
            AppendHypothesis(builder, "H9", "rejected-row-payload-remains-within-declared-line", false);
            AppendHypothesis(builder, "H10", "addressed-by-independent-analyzer-and-fixtures", false);
            builder.Append('}');
        }

        private static void AppendHypothesis(
            StringBuilder builder,
            string id,
            string status,
            bool first)
        {
            if (!first)
            {
                builder.Append(',');
            }
            Property(builder, id);
            Json(builder, status);
        }

        private static void AppendExternalRecords(
            StringBuilder builder,
            IReadOnlyList<ShpTsRleForensicFrameRecord> records)
        {
            builder.Append(",\"records\":[");
            for (int index = 0; index < records.Count; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }
                ShpTsRleForensicFrameRecord record = records[index];
                builder.Append('{');
                Property(builder, "sampleId");
                Json(builder, record.SampleId);
                builder.Append(",\"category\":");
                Json(builder, record.Category.ToString());
                builder.Append(",\"frameIndex\":").Append(record.FrameIndex);
                builder.Append(",\"width\":").Append(record.Width);
                builder.Append(",\"height\":").Append(record.Height);
                builder.Append(",\"stageA\":");
                AppendRow(builder, record.StageARow);
                builder.Append(",\"stageB\":[");
                if (record.StageB != null)
                {
                    for (int row = 0; row < record.StageB.Rows.Count; row++)
                    {
                        if (row != 0)
                        {
                            builder.Append(',');
                        }
                        AppendRow(builder, record.StageB.Rows[row]);
                    }
                }
                builder.Append("]}");
            }
            builder.Append(']');
        }

        private static void AppendRow(StringBuilder builder, ShpTsRleForensicRowScalar row)
        {
            builder.Append('{');
            builder.Append("\"rowIndex\":").Append(row.RowIndex);
            builder.Append(",\"lineLengthIncludingHeader\":").Append(row.LineLengthIncludingHeader);
            builder.Append(",\"commandCount\":").Append(row.CommandCount);
            builder.Append(",\"literalCount\":").Append(row.LiteralCount);
            builder.Append(",\"zeroRunCount\":").Append(row.ZeroRunCount);
            builder.Append(",\"zeroZeroCount\":").Append(row.ZeroZeroCount);
            builder.Append(",\"mechanicalOutputLength\":").Append(row.MechanicalOutputLength);
            builder.Append(",\"extraSource\":");
            Json(builder, row.ExtraSource.ToString());
            builder.Append(",\"extraFromLastCommand\":").Append(row.ExtraFromLastCommand ? "true" : "false");
            builder.Append(",\"extraIsLastOutput\":").Append(row.ExtraIsLastOutput ? "true" : "false");
            builder.Append(",\"extraIsZero\":").Append(row.ExtraIsZero ? "true" : "false");
            builder.Append(",\"ignoreOneExtraInputExact\":").Append(row.IgnoreOneExtraInputExact ? "true" : "false");
            builder.Append(",\"finalCommand\":");
            Json(builder, row.FinalCommandKind.ToString());
            builder.Append(",\"finalZeroRunCount\":").Append(row.FinalZeroRunCount);
            builder.Append(",\"distanceBeforeFinalZeroRun\":").Append(row.DistanceBeforeFinalZeroRun);
            builder.Append(",\"extraOvershoot\":").Append(row.ExtraOvershoot);
            builder.Append(",\"remainingBytesAtWidth\":").Append(row.RemainingBytesAtWidth);
            builder.Append(",\"remainingClass\":");
            Json(builder, row.RemainingClass.ToString());
            builder.Append(",\"guardPattern\":").Append(row.GuardPattern ? "true" : "false");
            builder.Append('}');
        }

        private static void AppendCategoryStageA(
            StringBuilder builder,
            IEnumerable<ShpTsRleForensicFrameRecord> records)
        {
            bool first = true;
            foreach (IGrouping<ShpTsRleForensicCategory, ShpTsRleForensicFrameRecord> group in
                     records.GroupBy(value => value.Category).OrderBy(value => value.Key))
            {
                if (!first)
                {
                    builder.Append(',');
                }
                first = false;
                builder.Append('{');
                Property(builder, "category");
                Json(builder, group.Key.ToString());
                builder.Append(",\"frames\":").Append(group.Count());
                builder.Append(",\"zeroRunExtra\":")
                    .Append(group.Count(value => value.StageARow.ExtraSource == ShpTsRleForensicExtraSource.ZeroRun));
                builder.Append(",\"literalExtra\":")
                    .Append(group.Count(value => value.StageARow.ExtraSource == ShpTsRleForensicExtraSource.Literal));
                builder.Append('}');
            }
        }

        private static void AppendParity(
            StringBuilder builder,
            IEnumerable<ShpTsRleForensicFrameRecord> records,
            bool odd)
        {
            ShpTsRleForensicFrameRecord[] values = records
                .Where(value => ((value.Width & 1) != 0) == odd).ToArray();
            builder.Append('{');
            Property(builder, "parity");
            Json(builder, odd ? "Odd" : "Even");
            builder.Append(",\"frames\":").Append(values.Length);
            builder.Append(",\"zeroRunExtra\":")
                .Append(values.Count(value => value.StageARow.ExtraSource == ShpTsRleForensicExtraSource.ZeroRun));
            builder.Append(",\"literalExtra\":")
                .Append(values.Count(value => value.StageARow.ExtraSource == ShpTsRleForensicExtraSource.Literal));
            builder.Append('}');
        }

        private static void AppendSemantic(
            StringBuilder builder,
            string name,
            IEnumerable<ShpTsRleForensicRowScalar> rows,
            Func<ShpTsRleForensicRowScalar, ShpTsRleForensicLengthClass> classify,
            Func<ShpTsRleForensicRowScalar, bool> exact)
        {
            ShpTsRleForensicRowScalar[] values = rows.ToArray();
            builder.Append(',');
            Property(builder, name);
            builder.Append('{');
            builder.Append("\"width\":")
                .Append(values.Count(value => classify(value) == ShpTsRleForensicLengthClass.Width));
            builder.Append(",\"widthPlusOne\":")
                .Append(values.Count(value => classify(value) == ShpTsRleForensicLengthClass.WidthPlusOne));
            builder.Append(",\"other\":")
                .Append(values.Count(value => classify(value) != ShpTsRleForensicLengthClass.Width &&
                    classify(value) != ShpTsRleForensicLengthClass.WidthPlusOne &&
                    classify(value) != ShpTsRleForensicLengthClass.Malformed));
            builder.Append(",\"inputExact\":").Append(values.Count(exact));
            builder.Append(",\"malformed\":")
                .Append(values.Count(value => classify(value) == ShpTsRleForensicLengthClass.Malformed));
            builder.Append('}');
        }

        private static void AppendBuckets(
            StringBuilder builder,
            string name,
            IEnumerable<long> values)
        {
            long[] array = values.ToArray();
            builder.Append(',');
            Property(builder, name);
            builder.Append('{');
            builder.Append("\"one\":").Append(array.Count(value => value == 1));
            builder.Append(",\"two\":").Append(array.Count(value => value == 2));
            builder.Append(",\"threeToFour\":").Append(array.Count(value => value >= 3 && value <= 4));
            builder.Append(",\"fiveToEight\":").Append(array.Count(value => value >= 5 && value <= 8));
            builder.Append(",\"nineToSixteen\":").Append(array.Count(value => value >= 9 && value <= 16));
            builder.Append(",\"seventeenPlus\":").Append(array.Count(value => value >= 17));
            builder.Append(",\"zeroOrNegative\":").Append(array.Count(value => value <= 0));
            builder.Append('}');
        }

        private static void AppendBooleanCounts(
            StringBuilder builder,
            string name,
            IEnumerable<bool> values)
        {
            bool[] array = values.ToArray();
            builder.Append(',');
            Property(builder, name);
            builder.Append("{\"true\":").Append(array.Count(value => value));
            builder.Append(",\"false\":").Append(array.Count(value => !value)).Append('}');
        }

        private static void AppendEnumCounts<T>(
            StringBuilder builder,
            string name,
            IEnumerable<T> values)
        {
            T[] array = values.ToArray();
            builder.Append(',');
            Property(builder, name);
            builder.Append('{');
            T[] distinct = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
            for (int index = 0; index < distinct.Length; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }
                Property(builder, distinct[index].ToString());
                builder.Append(array.Count(value => EqualityComparer<T>.Default.Equals(
                    value,
                    distinct[index])));
            }
            builder.Append('}');
        }

        private static void Property(StringBuilder builder, string name)
        {
            Json(builder, name);
            builder.Append(':');
        }

        private static void Json(StringBuilder builder, string value)
        {
            builder.Append('"');
            foreach (char character in value ?? string.Empty)
            {
                switch (character)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '"': builder.Append("\\\""); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (character < 0x20)
                        {
                            builder.Append("\\u").Append(((int)character).ToString("x4"));
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
