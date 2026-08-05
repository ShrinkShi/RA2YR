using System;
using System.Collections.Generic;
using System.Linq;

namespace RA2YR.Core.Content.ShpTs.Forensics
{
    internal enum ShpTsRleForensicCommandKind
    {
        None,
        Literal,
        ZeroRun,
        ZeroZero,
        DanglingZero
    }

    internal enum ShpTsRleForensicExtraSource
    {
        None,
        Literal,
        ZeroRun,
        Malformed
    }

    internal enum ShpTsRleForensicLengthClass
    {
        BelowWidth,
        Width,
        WidthPlusOne,
        AboveWidthPlusOne,
        Malformed
    }

    internal enum ShpTsRleForensicRemainingClass
    {
        NotReached,
        End,
        OneByte,
        TwoBytes,
        ThreeOrMore,
        IncompleteCommand
    }

    internal enum ShpTsRleForensicFailureCode
    {
        None,
        InputLengthMismatch,
        WindowStartMismatch,
        FrameIndexOutOfRange,
        FrameIsNotNonEmptyFlags3,
        RowHeaderTruncated,
        LineLengthTooSmall,
        LineLengthBudgetExceeded,
        RowPayloadTruncated,
        CommandBudgetExceeded,
        DanglingZero,
        ArithmeticOverflow,
        ReadFailure
    }

    internal sealed class ShpTsRleForensicLimits
    {
        public ShpTsRleForensicLimits(
            int maxRowsPerFrame,
            int maxLineBytes,
            int maxCommandsPerRow,
            long maxCommandsPerFrame)
        {
            if (maxRowsPerFrame <= 0 || maxLineBytes < 2 ||
                maxCommandsPerRow <= 0 || maxCommandsPerFrame <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxRowsPerFrame));
            }

            MaxRowsPerFrame = maxRowsPerFrame;
            MaxLineBytes = maxLineBytes;
            MaxCommandsPerRow = maxCommandsPerRow;
            MaxCommandsPerFrame = maxCommandsPerFrame;
        }

        public static ShpTsRleForensicLimits Default { get; } =
            new ShpTsRleForensicLimits(4096, 1024 * 1024, 1024 * 1024, 16L * 1024 * 1024);

        public int MaxRowsPerFrame { get; }
        public int MaxLineBytes { get; }
        public int MaxCommandsPerRow { get; }
        public long MaxCommandsPerFrame { get; }
    }

    internal sealed class ShpTsRleForensicRowScalar
    {
        internal ShpTsRleForensicRowScalar(
            int rowIndex,
            ushort width,
            ushort lineLengthIncludingHeader,
            long commandCount,
            long literalCount,
            long zeroRunCount,
            long zeroZeroCount,
            long mechanicalOutputLength,
            long xccVisibleOutputLength,
            long noHeaderOutputLength,
            bool noHeaderMalformed,
            ShpTsRleForensicExtraSource extraSource,
            bool extraFromLastCommand,
            bool extraIsLastOutput,
            bool extraIsZero,
            bool ignoreOneExtraInputExact,
            ShpTsRleForensicCommandKind finalCommandKind,
            int finalZeroRunCount,
            long distanceBeforeFinalZeroRun,
            long extraOvershoot,
            long remainingBytesAtWidth,
            ShpTsRleForensicRemainingClass remainingClass,
            bool inputExact,
            bool literalOverflow,
            bool guardPattern)
        {
            if (rowIndex < 0 || commandCount < 0 || literalCount < 0 ||
                zeroRunCount < 0 || zeroZeroCount < 0 ||
                mechanicalOutputLength < 0 || xccVisibleOutputLength < 0 ||
                noHeaderOutputLength < 0 || remainingBytesAtWidth < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(rowIndex));
            }

            RowIndex = rowIndex;
            Width = width;
            LineLengthIncludingHeader = lineLengthIncludingHeader;
            CommandCount = commandCount;
            LiteralCount = literalCount;
            ZeroRunCount = zeroRunCount;
            ZeroZeroCount = zeroZeroCount;
            MechanicalOutputLength = mechanicalOutputLength;
            XccVisibleOutputLength = xccVisibleOutputLength;
            NoHeaderOutputLength = noHeaderOutputLength;
            NoHeaderMalformed = noHeaderMalformed;
            ExtraSource = extraSource;
            ExtraFromLastCommand = extraFromLastCommand;
            ExtraIsLastOutput = extraIsLastOutput;
            ExtraIsZero = extraIsZero;
            IgnoreOneExtraInputExact = ignoreOneExtraInputExact;
            FinalCommandKind = finalCommandKind;
            FinalZeroRunCount = finalZeroRunCount;
            DistanceBeforeFinalZeroRun = distanceBeforeFinalZeroRun;
            ExtraOvershoot = extraOvershoot;
            RemainingBytesAtWidth = remainingBytesAtWidth;
            RemainingClass = remainingClass;
            InputExact = inputExact;
            LiteralOverflow = literalOverflow;
            GuardPattern = guardPattern;
        }

        public int RowIndex { get; }
        public ushort Width { get; }
        public ushort LineLengthIncludingHeader { get; }
        public long CommandCount { get; }
        public long LiteralCount { get; }
        public long ZeroRunCount { get; }
        public long ZeroZeroCount { get; }
        public long MechanicalOutputLength { get; }
        public long OpenRaStyleOutputLength => MechanicalOutputLength;
        public long XccVisibleOutputLength { get; }
        public long XccMechanicalOutputLength => MechanicalOutputLength;
        public long NoHeaderOutputLength { get; }
        public bool NoHeaderMalformed { get; }
        public ShpTsRleForensicExtraSource ExtraSource { get; }
        public bool ExtraFromLastCommand { get; }
        public bool ExtraIsLastOutput { get; }
        public bool ExtraIsZero { get; }
        public bool IgnoreOneExtraInputExact { get; }
        public ShpTsRleForensicCommandKind FinalCommandKind { get; }
        public int FinalZeroRunCount { get; }
        public long DistanceBeforeFinalZeroRun { get; }
        public long ExtraOvershoot { get; }
        public long RemainingBytesAtWidth { get; }
        public ShpTsRleForensicRemainingClass RemainingClass { get; }
        public bool InputExact { get; }
        public bool LiteralOverflow { get; }
        public bool GuardPattern { get; }

        public ShpTsRleForensicLengthClass MechanicalLengthClass =>
            ClassifyLength(MechanicalOutputLength, Width, false);

        public ShpTsRleForensicLengthClass OpenRaLengthClass => MechanicalLengthClass;

        public ShpTsRleForensicLengthClass XccVisibleLengthClass =>
            ClassifyLength(XccVisibleOutputLength, Width, false);

        public ShpTsRleForensicLengthClass NoHeaderLengthClass =>
            ClassifyLength(NoHeaderOutputLength, Width, NoHeaderMalformed);

        public ShpTsRleForensicLengthClass GuardClassifierLengthClass =>
            ClassifyLength(GuardPattern ? Width : MechanicalOutputLength, Width, false);

        internal string CanonicalScalar()
        {
            return string.Join("|", new[]
            {
                RowIndex.ToString(), Width.ToString(), LineLengthIncludingHeader.ToString(),
                CommandCount.ToString(), LiteralCount.ToString(), ZeroRunCount.ToString(),
                ZeroZeroCount.ToString(), MechanicalOutputLength.ToString(),
                XccVisibleOutputLength.ToString(), NoHeaderOutputLength.ToString(),
                NoHeaderMalformed ? "1" : "0", ExtraSource.ToString(),
                ExtraFromLastCommand ? "1" : "0", ExtraIsLastOutput ? "1" : "0",
                ExtraIsZero ? "1" : "0", IgnoreOneExtraInputExact ? "1" : "0",
                FinalCommandKind.ToString(), FinalZeroRunCount.ToString(),
                DistanceBeforeFinalZeroRun.ToString(), ExtraOvershoot.ToString(),
                RemainingBytesAtWidth.ToString(), RemainingClass.ToString(),
                InputExact ? "1" : "0", LiteralOverflow ? "1" : "0",
                GuardPattern ? "1" : "0"
            });
        }

        private static ShpTsRleForensicLengthClass ClassifyLength(
            long output,
            ushort width,
            bool malformed)
        {
            if (malformed)
            {
                return ShpTsRleForensicLengthClass.Malformed;
            }

            if (output < width)
            {
                return ShpTsRleForensicLengthClass.BelowWidth;
            }

            if (output == width)
            {
                return ShpTsRleForensicLengthClass.Width;
            }

            return output == checked((long)width + 1)
                ? ShpTsRleForensicLengthClass.WidthPlusOne
                : ShpTsRleForensicLengthClass.AboveWidthPlusOne;
        }
    }

    internal sealed class ShpTsRleForensicFrameAnalysis
    {
        private readonly IReadOnlyList<ShpTsRleForensicRowScalar> rows;

        private ShpTsRleForensicFrameAnalysis(
            int frameIndex,
            ushort width,
            ushort height,
            IEnumerable<ShpTsRleForensicRowScalar> rows,
            ShpTsRleForensicFailureCode failureCode,
            int failureRowIndex,
            long failureAbsoluteOffset)
        {
            ShpTsRleForensicRowScalar[] values = (rows ??
                throw new ArgumentNullException(nameof(rows))).ToArray();
            if (frameIndex < 0 || values.Any(value => value == null) ||
                (failureCode == ShpTsRleForensicFailureCode.None) != (failureRowIndex < 0))
            {
                throw new ArgumentException("The forensic frame result is inconsistent.");
            }

            FrameIndex = frameIndex;
            Width = width;
            Height = height;
            this.rows = Array.AsReadOnly(values);
            FailureCode = failureCode;
            FailureRowIndex = failureRowIndex;
            FailureAbsoluteOffset = failureAbsoluteOffset;
        }

        public bool IsSuccess => FailureCode == ShpTsRleForensicFailureCode.None;
        public int FrameIndex { get; }
        public ushort Width { get; }
        public ushort Height { get; }
        public IReadOnlyList<ShpTsRleForensicRowScalar> Rows => rows;
        public ShpTsRleForensicFailureCode FailureCode { get; }
        public int FailureRowIndex { get; }
        public long FailureAbsoluteOffset { get; }

        internal static ShpTsRleForensicFrameAnalysis Success(
            int frameIndex,
            ushort width,
            ushort height,
            IEnumerable<ShpTsRleForensicRowScalar> rows)
        {
            return new ShpTsRleForensicFrameAnalysis(
                frameIndex,
                width,
                height,
                rows,
                ShpTsRleForensicFailureCode.None,
                -1,
                -1);
        }

        internal static ShpTsRleForensicFrameAnalysis Failure(
            int frameIndex,
            ushort width,
            ushort height,
            IEnumerable<ShpTsRleForensicRowScalar> rows,
            ShpTsRleForensicFailureCode code,
            int rowIndex,
            long absoluteOffset)
        {
            if (code == ShpTsRleForensicFailureCode.None || rowIndex < 0)
            {
                throw new ArgumentException("A forensic failure requires a code and row.");
            }

            return new ShpTsRleForensicFrameAnalysis(
                frameIndex,
                width,
                height,
                rows,
                code,
                rowIndex,
                absoluteOffset);
        }

        internal string CanonicalScalar()
        {
            return string.Join("\n", new[]
            {
                FrameIndex + "|" + Width + "|" + Height + "|" + FailureCode + "|" +
                    FailureRowIndex + "|" + FailureAbsoluteOffset,
                string.Join("\n", rows.Select(value => value.CanonicalScalar()))
            });
        }
    }
}
