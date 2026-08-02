using System;
using System.IO;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Binary.Seekable;

namespace RA2YR.Core.Formats.Csf
{
    internal static class WestwoodCsfReader
    {
        internal const uint FileSignature = 0x43534620u;
        internal const uint SupportedVersion = 3u;
        internal const uint LabelMarker = 0x4c424c20u;
        internal const uint NormalValueMarker = 0x53545220u;
        internal const uint ExtendedValueMarker = 0x53545257u;
        internal const int HeaderLength = 24;

        private const int WindowReadChunkSize = 81920;
        private const long FixedModelAllocationEstimate = 8192;
        private const long ReferenceStorageEstimate = 16;
        private const long LabelObjectEstimate = 128;
        private const long ValueObjectEstimate = 128;
        private const long TextObjectEstimate = 64;

        public static CsfParseResult Read(
            ReadOnlyMemory<byte> input,
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            CsfReadLimits limits = null,
            long absoluteStartOffset = 0)
        {
            ValidateContext(source, provenance);
            return ReadMemoryCore(
                input,
                source,
                provenance,
                limits ?? CsfReadLimits.Default,
                absoluteStartOffset,
                0);
        }

        public static CsfParseResult Read(
            Stream stream,
            long inputLength,
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            CsfReadLimits limits = null,
            bool leaveOpen = false,
            long absoluteStartOffset = 0)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            ValidateContext(source, provenance);
            BinaryReadSession session = null;
            try
            {
                CsfReadLimits effectiveLimits = limits ?? CsfReadLimits.Default;
                session = BinaryReadSession.FromStream(
                    stream,
                    inputLength,
                    source,
                    effectiveLimits.ToBinaryLimits(),
                    leaveOpen,
                    absoluteStartOffset);
                return ParseSession(session, source, provenance, effectiveLimits);
            }
            catch (CsfReadException exception)
            {
                return CsfParseResult.Failure(exception.Diagnostic);
            }
            catch (BinaryReadException exception)
            {
                return CsfParseResult.Failure(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    -1,
                    -1,
                    null));
            }
            finally
            {
                session?.Dispose();
            }
        }

        public static CsfParseResult ReadSeekable(
            Stream stream,
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            CsfReadLimits limits = null,
            bool leaveOpen = false)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            ValidateContext(source, provenance);
            BinaryReadSession session = null;
            try
            {
                CsfReadLimits effectiveLimits = limits ?? CsfReadLimits.Default;
                session = BinaryReadSession.FromSeekableStream(
                    stream,
                    source,
                    effectiveLimits.ToBinaryLimits(),
                    leaveOpen);
                return ParseSession(session, source, provenance, effectiveLimits);
            }
            catch (CsfReadException exception)
            {
                return CsfParseResult.Failure(exception.Diagnostic);
            }
            catch (BinaryReadException exception)
            {
                return CsfParseResult.Failure(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    -1,
                    -1,
                    null));
            }
            finally
            {
                session?.Dispose();
            }
        }

        public static CsfParseResult Read(
            ReadOnlyDataWindow window,
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            CsfReadLimits limits = null)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            ValidateContext(source, provenance);
            CsfReadLimits effectiveLimits = limits ?? CsfReadLimits.Default;
            if (window.Length > effectiveLimits.MaxInputBytes)
            {
                return CsfParseResult.Failure(CreateDirectFailure(
                    CsfDiagnosticCode.InputBudgetExceeded,
                    source,
                    provenance,
                    window.AbsoluteStartOffset,
                    window.Length,
                    window.Length,
                    "csf-input",
                    -1,
                    -1,
                    null,
                    "The bounded CSF window exceeds its explicit input budget.",
                    BinaryDiagnosticCode.InputBudgetExceeded));
            }

            if (window.Length > int.MaxValue)
            {
                return CsfParseResult.Failure(CreateDirectFailure(
                    CsfDiagnosticCode.InvalidLength,
                    source,
                    provenance,
                    window.AbsoluteStartOffset,
                    window.Length,
                    window.Length,
                    "csf-input",
                    -1,
                    -1,
                    null,
                    "The bounded CSF window length cannot be represented safely.",
                    BinaryDiagnosticCode.InvalidLength));
            }

            if (window.Length > effectiveLimits.MaxAllocatedBytes)
            {
                return CsfParseResult.Failure(CreateDirectFailure(
                    CsfDiagnosticCode.AllocationBudgetExceeded,
                    source,
                    provenance,
                    window.AbsoluteStartOffset,
                    window.Length,
                    effectiveLimits.MaxAllocatedBytes,
                    "csf-window-snapshot",
                    -1,
                    -1,
                    null,
                    "The bounded CSF window exceeds its snapshot allocation budget.",
                    BinaryDiagnosticCode.AllocationBudgetExceeded));
            }

            if (window.Length > 0 && effectiveLimits.MaxSingleReadBytes == 0)
            {
                return CsfParseResult.Failure(CreateDirectFailure(
                    CsfDiagnosticCode.ReadBudgetExceeded,
                    source,
                    provenance,
                    window.AbsoluteStartOffset,
                    1,
                    window.Length,
                    "csf-window-input",
                    -1,
                    -1,
                    null,
                    "The bounded CSF window cannot be read within a zero-byte operation budget.",
                    BinaryDiagnosticCode.ReadBudgetExceeded));
            }

            byte[] snapshot;
            try
            {
                snapshot = new byte[checked((int)window.Length)];
                int offset = 0;
                int maximumChunk = checked((int)Math.Min(
                    WindowReadChunkSize,
                    effectiveLimits.MaxSingleReadBytes));
                while (offset < snapshot.Length)
                {
                    int count = Math.Min(maximumChunk, snapshot.Length - offset);
                    window.ReadExactly(
                        offset,
                        snapshot,
                        offset,
                        count,
                        "csf-window-input");
                    offset = checked(offset + count);
                }
            }
            catch (BinaryReadException exception)
            {
                return CsfParseResult.Failure(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    -1,
                    -1,
                    null));
            }
            catch (OutOfMemoryException)
            {
                return CsfParseResult.Failure(CreateDirectFailure(
                    CsfDiagnosticCode.ReadFailure,
                    source,
                    provenance,
                    window.AbsoluteStartOffset,
                    window.Length,
                    window.Length,
                    "csf-window-snapshot",
                    -1,
                    -1,
                    null,
                    "The validated CSF window snapshot could not be allocated.",
                    BinaryDiagnosticCode.ReadFailure));
            }

            return ReadMemoryCore(
                snapshot,
                source,
                provenance,
                effectiveLimits,
                window.AbsoluteStartOffset,
                snapshot.LongLength);
        }

        private static CsfParseResult ReadMemoryCore(
            ReadOnlyMemory<byte> input,
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            CsfReadLimits limits,
            long absoluteStartOffset,
            long initialAllocation)
        {
            BinaryReadSession session = null;
            try
            {
                session = BinaryReadSession.FromMemory(
                    input,
                    source,
                    limits.ToBinaryLimits(),
                    absoluteStartOffset);
                if (initialAllocation != 0)
                {
                    session.ReserveAllocation(
                        initialAllocation,
                        absoluteStartOffset,
                        input.Length,
                        "csf-window-snapshot");
                }

                return ParseSession(session, source, provenance, limits);
            }
            catch (CsfReadException exception)
            {
                return CsfParseResult.Failure(exception.Diagnostic);
            }
            catch (BinaryReadException exception)
            {
                return CsfParseResult.Failure(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    -1,
                    -1,
                    null));
            }
            finally
            {
                session?.Dispose();
            }
        }

        private static CsfParseResult ParseSession(
            BinaryReadSession session,
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            CsfReadLimits limits)
        {
            BoundedBinaryReader reader = session.Root;
            ReserveAllocation(
                session,
                reader,
                FixedModelAllocationEstimate,
                source,
                provenance,
                -1,
                -1,
                "csf-fixed-model");

            long signatureOffset = reader.AbsoluteOffset;
            uint signature = ReadUInt32(
                reader, source, provenance, -1, -1, "csf-signature", null);
            if (signature != FileSignature)
            {
                throw Failure(
                    CsfDiagnosticCode.InvalidSignature,
                    source,
                    provenance,
                    signatureOffset,
                    4,
                    reader.RemainingLength,
                    "csf-signature",
                    -1,
                    -1,
                    signature,
                    "The CSF file signature is invalid.");
            }

            long versionOffset = reader.AbsoluteOffset;
            uint version = ReadUInt32(
                reader, source, provenance, -1, -1, "csf-version", null);
            if (version != SupportedVersion)
            {
                throw Failure(
                    CsfDiagnosticCode.UnsupportedVersion,
                    source,
                    provenance,
                    versionOffset,
                    4,
                    reader.RemainingLength,
                    "csf-version",
                    -1,
                    -1,
                    null,
                    "The CSF version is not supported by this strict reader.");
            }

            long labelCountOffset = reader.AbsoluteOffset;
            uint declaredLabelCount = ReadUInt32(
                reader, source, provenance, -1, -1, "csf-label-count", null);
            long valueCountOffset = reader.AbsoluteOffset;
            uint declaredValueCount = ReadUInt32(
                reader, source, provenance, -1, -1, "csf-value-count", null);
            uint reserved = ReadUInt32(
                reader, source, provenance, -1, -1, "csf-reserved", null);
            uint rawLanguage = ReadUInt32(
                reader, source, provenance, -1, -1, "csf-language", null);

            if (declaredLabelCount > limits.MaxLabels)
            {
                throw Failure(
                    CsfDiagnosticCode.LabelBudgetExceeded,
                    source,
                    provenance,
                    labelCountOffset,
                    declaredLabelCount,
                    reader.RemainingLength,
                    "csf-label-count",
                    -1,
                    -1,
                    null,
                    "The declared CSF label count exceeds its explicit budget.");
            }

            if (declaredValueCount > limits.MaxTotalValues)
            {
                throw Failure(
                    CsfDiagnosticCode.TotalValueBudgetExceeded,
                    source,
                    provenance,
                    valueCountOffset,
                    declaredValueCount,
                    reader.RemainingLength,
                    "csf-value-count",
                    -1,
                    -1,
                    null,
                    "The declared CSF value count exceeds its explicit budget.");
            }

            ReserveRecords(
                reader,
                declaredLabelCount,
                source,
                provenance,
                -1,
                -1,
                "csf-label-records");
            int labelCount = ConvertToInt(
                declaredLabelCount,
                reader,
                source,
                provenance,
                -1,
                -1,
                "csf-label-count");
            ReserveAllocation(
                session,
                reader,
                CheckedAllocation(
                    labelCount,
                    ReferenceStorageEstimate,
                    reader,
                    source,
                    provenance,
                    -1,
                    -1,
                    "csf-label-storage"),
                source,
                provenance,
                -1,
                -1,
                "csf-label-storage");

            var header = new CsfHeader(
                signature,
                version,
                declaredLabelCount,
                declaredValueCount,
                reserved,
                new CsfLanguageCode(rawLanguage));
            var labels = new CsfLabel[labelCount];
            long actualValueCount = 0;
            long cumulativeCodeUnits = 0;
            for (int labelIndex = 0; labelIndex < labels.Length; labelIndex++)
            {
                long markerOffset = reader.AbsoluteOffset;
                uint marker = ReadUInt32(
                    reader,
                    source,
                    provenance,
                    labelIndex,
                    -1,
                    "csf-label-marker",
                    null);
                if (marker != LabelMarker)
                {
                    throw Failure(
                        CsfDiagnosticCode.InvalidLabelMarker,
                        source,
                        provenance,
                        markerOffset,
                        4,
                        reader.RemainingLength,
                        "csf-label-marker",
                        labelIndex,
                        -1,
                        marker,
                        "The CSF label record marker is invalid.");
                }

                long valuesOffset = reader.AbsoluteOffset;
                uint declaredValuesForLabel = ReadUInt32(
                    reader,
                    source,
                    provenance,
                    labelIndex,
                    -1,
                    "csf-label-value-count",
                    marker);
                if (declaredValuesForLabel > limits.MaxValuesPerLabel)
                {
                    throw Failure(
                        CsfDiagnosticCode.ValuesPerLabelBudgetExceeded,
                        source,
                        provenance,
                        valuesOffset,
                        declaredValuesForLabel,
                        reader.RemainingLength,
                        "csf-label-value-count",
                        labelIndex,
                        -1,
                        marker,
                        "The per-label CSF value count exceeds its explicit budget.");
                }

                actualValueCount = CheckedAdd(
                    actualValueCount,
                    declaredValuesForLabel,
                    reader,
                    source,
                    provenance,
                    labelIndex,
                    -1,
                    "csf-total-value-count");
                if (actualValueCount > declaredValueCount)
                {
                    throw Failure(
                        CsfDiagnosticCode.DeclaredValueCountMismatch,
                        source,
                        provenance,
                        valuesOffset,
                        declaredValuesForLabel,
                        reader.RemainingLength,
                        "csf-label-value-count",
                        labelIndex,
                        -1,
                        marker,
                        "The sum of per-label values exceeds the declared CSF total.");
                }

                uint labelLength = ReadUInt32(
                    reader,
                    source,
                    provenance,
                    labelIndex,
                    -1,
                    "csf-label-name-length",
                    marker);
                string name = ReadAscii(
                    reader,
                    session,
                    labelLength,
                    limits.MaxLabelNameBytes,
                    CsfDiagnosticCode.LabelNameBudgetExceeded,
                    source,
                    provenance,
                    labelIndex,
                    -1,
                    "csf-label-name",
                    marker);

                ReserveRecords(
                    reader,
                    declaredValuesForLabel,
                    source,
                    provenance,
                    labelIndex,
                    -1,
                    "csf-value-records");
                int valueCount = ConvertToInt(
                    declaredValuesForLabel,
                    reader,
                    source,
                    provenance,
                    labelIndex,
                    -1,
                    "csf-label-value-count");
                ReserveAllocation(
                    session,
                    reader,
                    CheckedAllocation(
                        valueCount,
                        ReferenceStorageEstimate,
                        reader,
                        source,
                        provenance,
                        labelIndex,
                        -1,
                        "csf-value-storage"),
                    source,
                    provenance,
                    labelIndex,
                    -1,
                    "csf-value-storage");
                var values = new CsfValue[valueCount];
                for (int valueIndex = 0; valueIndex < values.Length; valueIndex++)
                {
                    values[valueIndex] = ReadValue(
                        reader,
                        session,
                        limits,
                        source,
                        provenance,
                        labelIndex,
                        valueIndex,
                        ref cumulativeCodeUnits);
                }

                ReserveAllocation(
                    session,
                    reader,
                    LabelObjectEstimate,
                    source,
                    provenance,
                    labelIndex,
                    -1,
                    "csf-label-model");
                labels[labelIndex] = new CsfLabel(name, values);
            }

            if (reader.RemainingLength >= 4)
            {
                uint trailingMarker = PeekUInt32(
                    reader,
                    source,
                    provenance,
                    labels.Length,
                    -1,
                    "csf-label-marker");
                if (trailingMarker == LabelMarker)
                {
                    throw Failure(
                        CsfDiagnosticCode.DeclaredLabelCountMismatch,
                        source,
                        provenance,
                        reader.AbsoluteOffset,
                        4,
                        reader.RemainingLength,
                        "csf-label-count",
                        labels.Length,
                        -1,
                        trailingMarker,
                        "The CSF contains another label after its declared label count.");
                }
            }

            if (actualValueCount != declaredValueCount)
            {
                throw Failure(
                    CsfDiagnosticCode.DeclaredValueCountMismatch,
                    source,
                    provenance,
                    reader.AbsoluteOffset,
                    declaredValueCount,
                    reader.RemainingLength,
                    "csf-value-count",
                    labels.Length - 1,
                    -1,
                    null,
                    "The parsed CSF value count does not match its declaration.");
            }

            BinaryParseCompletion completion = reader.Complete(
                TrailingDataPolicy.RequireFullyConsumed,
                "csf-trailing-data");
            if (!completion.IsComplete)
            {
                BinaryDiagnostic diagnostic = completion.Diagnostics.First(
                    item => item.Severity == BinaryDiagnosticSeverity.Error);
                return CsfParseResult.Failure(MapBinaryFailure(
                    diagnostic,
                    source,
                    provenance,
                    -1,
                    -1,
                    null));
            }

            return CsfParseResult.Success(new CsfDocument(
                source,
                provenance,
                header,
                labels));
        }

        private static CsfValue ReadValue(
            BoundedBinaryReader reader,
            BinaryReadSession session,
            CsfReadLimits limits,
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            int labelIndex,
            int valueIndex,
            ref long cumulativeCodeUnits)
        {
            long markerOffset = reader.AbsoluteOffset;
            uint marker = ReadUInt32(
                reader,
                source,
                provenance,
                labelIndex,
                valueIndex,
                "csf-value-marker",
                null);
            CsfValueKind kind;
            if (marker == NormalValueMarker)
            {
                kind = CsfValueKind.Normal;
            }
            else if (marker == ExtendedValueMarker)
            {
                kind = CsfValueKind.Extended;
            }
            else
            {
                throw Failure(
                    CsfDiagnosticCode.InvalidValueMarker,
                    source,
                    provenance,
                    markerOffset,
                    4,
                    reader.RemainingLength,
                    "csf-value-marker",
                    labelIndex,
                    valueIndex,
                    marker,
                    "The CSF value record marker is invalid.");
            }

            uint mainLength = ReadUInt32(
                reader,
                source,
                provenance,
                labelIndex,
                valueIndex,
                "csf-main-text-length",
                marker);
            if (mainLength > limits.MaxMainTextCodeUnits)
            {
                throw Failure(
                    CsfDiagnosticCode.MainTextBudgetExceeded,
                    source,
                    provenance,
                    checked(reader.AbsoluteOffset - 4),
                    mainLength,
                    reader.RemainingLength,
                    "csf-main-text-length",
                    labelIndex,
                    valueIndex,
                    marker,
                    "The CSF main-text length exceeds its explicit code-unit budget.");
            }

            long mainByteLength = CheckedAllocation(
                mainLength,
                2,
                reader,
                source,
                provenance,
                labelIndex,
                valueIndex,
                "csf-main-text");
            EnsureRemaining(
                reader,
                mainByteLength,
                source,
                provenance,
                labelIndex,
                valueIndex,
                "csf-main-text",
                marker);

            cumulativeCodeUnits = CheckedAdd(
                cumulativeCodeUnits,
                mainLength,
                reader,
                source,
                provenance,
                labelIndex,
                valueIndex,
                "csf-cumulative-code-units");
            if (cumulativeCodeUnits > limits.MaxCumulativeUtf16CodeUnits)
            {
                throw Failure(
                    CsfDiagnosticCode.CumulativeCodeUnitBudgetExceeded,
                    source,
                    provenance,
                    reader.AbsoluteOffset,
                    mainLength,
                    reader.RemainingLength,
                    "csf-cumulative-code-units",
                    labelIndex,
                    valueIndex,
                    marker,
                    "The cumulative CSF UTF-16 code-unit budget would be exceeded.");
            }

            CsfText text = ReadMainText(
                reader,
                session,
                mainLength,
                source,
                provenance,
                labelIndex,
                valueIndex,
                marker);
            string extraText = null;
            if (kind == CsfValueKind.Extended)
            {
                uint extraLength = ReadUInt32(
                    reader,
                    source,
                    provenance,
                    labelIndex,
                    valueIndex,
                    "csf-extra-text-length",
                    marker);
                extraText = ReadAscii(
                    reader,
                    session,
                    extraLength,
                    limits.MaxExtraTextBytes,
                    CsfDiagnosticCode.ExtraTextBudgetExceeded,
                    source,
                    provenance,
                    labelIndex,
                    valueIndex,
                    "csf-extra-text",
                    marker);
            }

            ReserveAllocation(
                session,
                reader,
                ValueObjectEstimate,
                source,
                provenance,
                labelIndex,
                valueIndex,
                "csf-value-model");
            return new CsfValue(kind, text, extraText);
        }

        private static CsfText ReadMainText(
            BoundedBinaryReader reader,
            BinaryReadSession session,
            uint length,
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            int labelIndex,
            int valueIndex,
            uint marker)
        {
            ValidateStringLength(
                reader,
                length,
                source,
                provenance,
                labelIndex,
                valueIndex,
                "csf-main-text-length");
            int count = ConvertToInt(
                length,
                reader,
                source,
                provenance,
                labelIndex,
                valueIndex,
                "csf-main-text-length");
            long allocation = CheckedAllocation(
                count,
                4,
                reader,
                source,
                provenance,
                labelIndex,
                valueIndex,
                "csf-main-text-model");
            allocation = CheckedAdd(
                allocation,
                TextObjectEstimate,
                reader,
                source,
                provenance,
                labelIndex,
                valueIndex,
                "csf-main-text-model");
            ReserveAllocation(
                session,
                reader,
                allocation,
                source,
                provenance,
                labelIndex,
                valueIndex,
                "csf-main-text-model");

            char[] codeUnits;
            try
            {
                codeUnits = new char[count];
            }
            catch (OutOfMemoryException)
            {
                throw Failure(
                    CsfDiagnosticCode.ReadFailure,
                    source,
                    provenance,
                    reader.AbsoluteOffset,
                    length,
                    reader.RemainingLength,
                    "csf-main-text-model",
                    labelIndex,
                    valueIndex,
                    marker,
                    "The validated CSF main-text model could not be allocated.");
            }

            for (int index = 0; index < codeUnits.Length; index++)
            {
                ushort stored = ReadUInt16(
                    reader,
                    source,
                    provenance,
                    labelIndex,
                    valueIndex,
                    "csf-main-text",
                    marker);
                codeUnits[index] = unchecked((char)(stored ^ 0xffff));
            }

            return new CsfText(new string(codeUnits));
        }

        private static string ReadAscii(
            BoundedBinaryReader reader,
            BinaryReadSession session,
            uint length,
            long formatLimit,
            CsfDiagnosticCode budgetCode,
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            int labelIndex,
            int valueIndex,
            string field,
            uint? marker)
        {
            if (length > formatLimit)
            {
                throw Failure(
                    budgetCode,
                    source,
                    provenance,
                    reader.AbsoluteOffset,
                    length,
                    reader.RemainingLength,
                    field,
                    labelIndex,
                    valueIndex,
                    marker,
                    "The declared CSF ASCII field exceeds its explicit format budget.");
            }

            EnsureRemaining(
                reader,
                length,
                source,
                provenance,
                labelIndex,
                valueIndex,
                field,
                marker);

            ValidateStringLength(
                reader,
                length,
                source,
                provenance,
                labelIndex,
                valueIndex,
                field);
            int count = ConvertToInt(
                length,
                reader,
                source,
                provenance,
                labelIndex,
                valueIndex,
                field);
            long allocation = CheckedAllocation(
                count,
                4,
                reader,
                source,
                provenance,
                labelIndex,
                valueIndex,
                field);
            allocation = CheckedAdd(
                allocation,
                TextObjectEstimate,
                reader,
                source,
                provenance,
                labelIndex,
                valueIndex,
                field);
            ReserveAllocation(
                session,
                reader,
                allocation,
                source,
                provenance,
                labelIndex,
                valueIndex,
                field);

            char[] characters;
            try
            {
                characters = new char[count];
            }
            catch (OutOfMemoryException)
            {
                throw Failure(
                    CsfDiagnosticCode.ReadFailure,
                    source,
                    provenance,
                    reader.AbsoluteOffset,
                    length,
                    reader.RemainingLength,
                    field,
                        labelIndex,
                        valueIndex,
                        marker,
                        "The validated CSF ASCII model could not be allocated.");
            }

            for (int index = 0; index < characters.Length; index++)
            {
                long byteOffset = reader.AbsoluteOffset;
                byte value = ReadUInt8(
                    reader,
                    source,
                    provenance,
                    labelIndex,
                    valueIndex,
                    field,
                    null);
                if (value > 0x7f)
                {
                    throw Failure(
                        CsfDiagnosticCode.InvalidAsciiByte,
                        source,
                        provenance,
                        byteOffset,
                        1,
                        reader.RemainingLength,
                        field,
                        labelIndex,
                        valueIndex,
                        marker,
                        "A CSF ASCII field contains a byte outside the confirmed range.");
                }

                characters[index] = (char)value;
            }

            return new string(characters);
        }

        private static byte ReadUInt8(
            BoundedBinaryReader reader,
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            int labelIndex,
            int valueIndex,
            string field,
            uint? marker)
        {
            try
            {
                return reader.ReadUInt8(field);
            }
            catch (BinaryReadException exception)
            {
                throw new CsfReadException(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    labelIndex,
                    valueIndex,
                    marker));
            }
        }

        private static ushort ReadUInt16(
            BoundedBinaryReader reader,
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            int labelIndex,
            int valueIndex,
            string field,
            uint? marker)
        {
            try
            {
                return reader.ReadUInt16(field);
            }
            catch (BinaryReadException exception)
            {
                throw new CsfReadException(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    labelIndex,
                    valueIndex,
                    marker));
            }
        }

        private static uint ReadUInt32(
            BoundedBinaryReader reader,
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            int labelIndex,
            int valueIndex,
            string field,
            uint? marker)
        {
            try
            {
                return reader.ReadUInt32(field);
            }
            catch (BinaryReadException exception)
            {
                throw new CsfReadException(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    labelIndex,
                    valueIndex,
                    marker));
            }
        }

        private static uint PeekUInt32(
            BoundedBinaryReader reader,
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            int labelIndex,
            int valueIndex,
            string field)
        {
            try
            {
                byte[] bytes = reader.PeekBytes(4, field);
                return (uint)bytes[0] |
                       ((uint)bytes[1] << 8) |
                       ((uint)bytes[2] << 16) |
                       ((uint)bytes[3] << 24);
            }
            catch (BinaryReadException exception)
            {
                throw new CsfReadException(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    labelIndex,
                    valueIndex,
                    null));
            }
        }

        private static void EnsureRemaining(
            BoundedBinaryReader reader,
            long requestedLength,
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            int labelIndex,
            int valueIndex,
            string field,
            uint? marker)
        {
            if (requestedLength > reader.RemainingLength)
            {
                throw Failure(
                    CsfDiagnosticCode.UnexpectedEndOfInput,
                    source,
                    provenance,
                    reader.AbsoluteOffset,
                    requestedLength,
                    reader.RemainingLength,
                    field,
                    labelIndex,
                    valueIndex,
                    marker,
                    "The declared CSF field extends beyond the bounded input.",
                    BinaryDiagnosticCode.UnexpectedEndOfInput);
            }
        }

        private static void ReserveAllocation(
            BinaryReadSession session,
            BoundedBinaryReader reader,
            long byteCount,
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            int labelIndex,
            int valueIndex,
            string field)
        {
            try
            {
                session.ReserveAllocation(
                    byteCount,
                    reader.AbsoluteOffset,
                    reader.RemainingLength,
                    field);
            }
            catch (BinaryReadException exception)
            {
                throw new CsfReadException(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    labelIndex,
                    valueIndex,
                    null));
            }
        }

        private static void ReserveRecords(
            BoundedBinaryReader reader,
            long count,
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            int labelIndex,
            int valueIndex,
            string field)
        {
            try
            {
                reader.ReserveRecords(count, field);
            }
            catch (BinaryReadException exception)
            {
                throw new CsfReadException(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    labelIndex,
                    valueIndex,
                    null));
            }
        }

        private static void ValidateStringLength(
            BoundedBinaryReader reader,
            long length,
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            int labelIndex,
            int valueIndex,
            string field)
        {
            try
            {
                reader.ValidateStringLength(length, field);
            }
            catch (BinaryReadException exception)
            {
                throw new CsfReadException(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    labelIndex,
                    valueIndex,
                    null));
            }
        }

        private static int ConvertToInt(
            long value,
            BoundedBinaryReader reader,
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            int labelIndex,
            int valueIndex,
            string field)
        {
            try
            {
                return checked((int)value);
            }
            catch (OverflowException)
            {
                throw Failure(
                    CsfDiagnosticCode.ArithmeticOverflow,
                    source,
                    provenance,
                    reader.AbsoluteOffset,
                    value,
                    reader.RemainingLength,
                    field,
                    labelIndex,
                    valueIndex,
                    null,
                    "A validated CSF length cannot be represented by this reader.",
                    BinaryDiagnosticCode.ArithmeticOverflow);
            }
        }

        private static long CheckedAllocation(
            long count,
            long bytesPerItem,
            BoundedBinaryReader reader,
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            int labelIndex,
            int valueIndex,
            string field)
        {
            try
            {
                return checked(count * bytesPerItem);
            }
            catch (OverflowException)
            {
                throw Failure(
                    CsfDiagnosticCode.ArithmeticOverflow,
                    source,
                    provenance,
                    reader.AbsoluteOffset,
                    count,
                    reader.RemainingLength,
                    field,
                    labelIndex,
                    valueIndex,
                    null,
                    "CSF allocation accounting overflowed.",
                    BinaryDiagnosticCode.ArithmeticOverflow);
            }
        }

        private static long CheckedAdd(
            long left,
            long right,
            BoundedBinaryReader reader,
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            int labelIndex,
            int valueIndex,
            string field)
        {
            try
            {
                return checked(left + right);
            }
            catch (OverflowException)
            {
                throw Failure(
                    CsfDiagnosticCode.ArithmeticOverflow,
                    source,
                    provenance,
                    reader.AbsoluteOffset,
                    right,
                    reader.RemainingLength,
                    field,
                    labelIndex,
                    valueIndex,
                    null,
                    "CSF cumulative accounting overflowed.",
                    BinaryDiagnosticCode.ArithmeticOverflow);
            }
        }

        private static void ValidateContext(
            BinarySourceContext source,
            CsfSourceProvenance provenance)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (provenance == null)
            {
                throw new ArgumentNullException(nameof(provenance));
            }

            if (!string.Equals(
                    source.LogicalSourceId,
                    provenance.SourceId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "CSF provenance must identify the binary source.",
                    nameof(provenance));
            }
        }

        private static CsfDiagnostic MapBinaryFailure(
            BinaryDiagnostic diagnostic,
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            int labelIndex,
            int valueIndex,
            uint? marker)
        {
            return new CsfDiagnostic(
                MapCode(diagnostic.Code),
                source,
                provenance,
                diagnostic.AbsoluteOffset,
                diagnostic.RequestedLength,
                diagnostic.RemainingLength,
                diagnostic.FieldOrSection,
                labelIndex,
                valueIndex,
                marker,
                diagnostic.Message,
                diagnostic.Code);
        }

        private static CsfReadException Failure(
            CsfDiagnosticCode code,
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            long absoluteOffset,
            long requestedLength,
            long remainingLength,
            string field,
            int labelIndex,
            int valueIndex,
            uint? marker,
            string message,
            BinaryDiagnosticCode? binaryCode = null)
        {
            return new CsfReadException(CreateDirectFailure(
                code,
                source,
                provenance,
                absoluteOffset,
                requestedLength,
                remainingLength,
                field,
                labelIndex,
                valueIndex,
                marker,
                message,
                binaryCode));
        }

        private static CsfDiagnostic CreateDirectFailure(
            CsfDiagnosticCode code,
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            long absoluteOffset,
            long requestedLength,
            long remainingLength,
            string field,
            int labelIndex,
            int valueIndex,
            uint? marker,
            string message,
            BinaryDiagnosticCode? binaryCode = null)
        {
            return new CsfDiagnostic(
                code,
                source,
                provenance,
                absoluteOffset,
                requestedLength,
                remainingLength,
                field,
                labelIndex,
                valueIndex,
                marker,
                message,
                binaryCode);
        }

        private static CsfDiagnosticCode MapCode(BinaryDiagnosticCode code)
        {
            switch (code)
            {
                case BinaryDiagnosticCode.UnexpectedEndOfInput:
                    return CsfDiagnosticCode.UnexpectedEndOfInput;
                case BinaryDiagnosticCode.InvalidLength:
                    return CsfDiagnosticCode.InvalidLength;
                case BinaryDiagnosticCode.TrailingData:
                    return CsfDiagnosticCode.UnexpectedTrailingData;
                case BinaryDiagnosticCode.InputBudgetExceeded:
                    return CsfDiagnosticCode.InputBudgetExceeded;
                case BinaryDiagnosticCode.ReadBudgetExceeded:
                    return CsfDiagnosticCode.ReadBudgetExceeded;
                case BinaryDiagnosticCode.AllocationBudgetExceeded:
                    return CsfDiagnosticCode.AllocationBudgetExceeded;
                case BinaryDiagnosticCode.RecordBudgetExceeded:
                    return CsfDiagnosticCode.RecordBudgetExceeded;
                case BinaryDiagnosticCode.StringBudgetExceeded:
                    return CsfDiagnosticCode.StringBudgetExceeded;
                case BinaryDiagnosticCode.ArithmeticOverflow:
                    return CsfDiagnosticCode.ArithmeticOverflow;
                case BinaryDiagnosticCode.UnsupportedSeekOperation:
                    return CsfDiagnosticCode.UnsupportedSeekOperation;
                case BinaryDiagnosticCode.ReadFailure:
                    return CsfDiagnosticCode.ReadFailure;
                default:
                    return CsfDiagnosticCode.BinaryReadFailure;
            }
        }
    }
}
