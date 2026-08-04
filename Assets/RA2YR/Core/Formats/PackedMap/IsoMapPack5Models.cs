using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Formats.PackedMap
{
    internal enum IsoMapDiagnosticCode
    {
        InputBudgetExceeded,
        RecordBudgetExceeded,
        IncompleteRecord,
        UnexpectedTrailingBytes,
        InvalidFourZeroTrailer,
        TrailingBudgetExceeded,
        CoordinateBudgetExceeded,
        DuplicateCoordinate,
        ConflictingDuplicateCoordinate,
        OutOfDomainCoordinate,
        CoordinateArithmeticOverflow,
        PackedStageFailure,
        WrongCodec,
        BackendUnavailable,
        NoProgress,
        BinaryReadFailure
    }

    internal sealed class IsoMapDiagnostic
    {
        internal IsoMapDiagnostic(
            BinaryDiagnosticSeverity severity,
            IsoMapDiagnosticCode code,
            BinarySourceContext source,
            IEnumerable<IniSourceProvenance> provenance,
            long absoluteOffset,
            int recordOrdinal,
            IsoMapCoordinateKey? coordinate,
            string stage,
            string message,
            BinaryDiagnosticCode? binaryCode = null)
        {
            if (recordOrdinal < -1) throw new ArgumentOutOfRangeException(nameof(recordOrdinal));
            Severity = severity;
            Code = code;
            Source = source ?? throw new ArgumentNullException(nameof(source));
            IniSourceProvenance[] chain = (provenance ?? throw new ArgumentNullException(nameof(provenance))).ToArray();
            if (chain.Length == 0 || chain.Any(item => item == null)) throw new ArgumentException("IsoMap provenance is required.", nameof(provenance));
            Provenance = Array.AsReadOnly(chain);
            AbsoluteOffset = absoluteOffset;
            RecordOrdinal = recordOrdinal;
            Coordinate = coordinate;
            Stage = BinaryDiagnosticLabel.Validate(stage, nameof(stage));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            BinaryCode = binaryCode;
        }

        public BinaryDiagnosticSeverity Severity { get; }
        public IsoMapDiagnosticCode Code { get; }
        public BinarySourceContext Source { get; }
        public IReadOnlyList<IniSourceProvenance> Provenance { get; }
        public long AbsoluteOffset { get; }
        public int RecordOrdinal { get; }
        public IsoMapCoordinateKey? Coordinate { get; }
        public string Stage { get; }
        public string Message { get; }
        public BinaryDiagnosticCode? BinaryCode { get; }
    }

    internal enum IsoMapPack5TrailingPolicy
    {
        RejectAnyRemainder,
        PreserveRemainderWithDiagnostic,
        AllowExactFourZeroTrailer
    }

    internal enum IsoMapTrailingClassification
    {
        None,
        RejectedRemainder,
        PreservedRemainder,
        ExactFourZeroTrailer
    }

    internal sealed class IsoMapPack5TrailingData
    {
        internal IsoMapPack5TrailingData(long absoluteOffset, byte[] bytes, IsoMapTrailingClassification classification)
        {
            if (absoluteOffset < 0) throw new ArgumentOutOfRangeException(nameof(absoluteOffset));
            AbsoluteOffset = absoluteOffset;
            Bytes = (byte[])(bytes ?? throw new ArgumentNullException(nameof(bytes))).Clone();
            Classification = classification;
        }

        public long AbsoluteOffset { get; }
        public byte[] Bytes { get; }
        public IsoMapTrailingClassification Classification { get; }
    }

    internal sealed class IsoMapPack5ReadLimits
    {
        public IsoMapPack5ReadLimits(
            long maxInputBytes = 64 * 1024 * 1024,
            int maxRecords = 1_000_000,
            int maxTrailingBytes = 4096,
            int maxCoordinateEntries = 1_000_000,
            int maxDuplicateGroups = 100_000,
            int maxDiagnostics = 4096)
        {
            if (maxInputBytes < 0 || maxRecords < 0 || maxTrailingBytes < 0 || maxCoordinateEntries < 0 || maxDuplicateGroups < 0 || maxDiagnostics < 0)
                throw new ArgumentOutOfRangeException();
            MaxInputBytes = maxInputBytes;
            MaxRecords = maxRecords;
            MaxTrailingBytes = maxTrailingBytes;
            MaxCoordinateEntries = maxCoordinateEntries;
            MaxDuplicateGroups = maxDuplicateGroups;
            MaxDiagnostics = maxDiagnostics;
        }

        public long MaxInputBytes { get; }
        public int MaxRecords { get; }
        public int MaxTrailingBytes { get; }
        public int MaxCoordinateEntries { get; }
        public int MaxDuplicateGroups { get; }
        public int MaxDiagnostics { get; }
    }

    internal sealed class IsoMapPack5RecordRaw
    {
        private readonly byte[] raw;

        internal IsoMapPack5RecordRaw(
            int sourceOrdinal,
            long sourceOffset,
            byte[] rawBytes,
            IEnumerable<IniSourceProvenance> provenance)
        {
            if (sourceOrdinal < 0 || sourceOffset < 0) throw new ArgumentOutOfRangeException();
            raw = (byte[])(rawBytes ?? throw new ArgumentNullException(nameof(rawBytes))).Clone();
            if (raw.Length != 11) throw new ArgumentException("IsoMapPack5 records are exactly 11 bytes.", nameof(rawBytes));
            IniSourceProvenance[] chain = (provenance ?? throw new ArgumentNullException(nameof(provenance))).ToArray();
            if (chain.Length == 0 || chain.Any(item => item == null)) throw new ArgumentException("Record provenance is required.", nameof(provenance));
            SourceOrdinal = sourceOrdinal;
            SourceOffset = sourceOffset;
            Provenance = Array.AsReadOnly(chain);
        }

        public int SourceOrdinal { get; }
        public long SourceOffset { get; }
        public ushort XRawU16LittleEndian => (ushort)(raw[0] | (raw[1] << 8));
        public ushort YRawU16LittleEndian => (ushort)(raw[2] | (raw[3] << 8));
        public uint TileRawU32LittleEndian => (uint)(raw[4] | (raw[5] << 8) | (raw[6] << 16) | (raw[7] << 24));
        public ushort TileLowU16LittleEndian => (ushort)(raw[4] | (raw[5] << 8));
        public ushort TileHighU16LittleEndian => (ushort)(raw[6] | (raw[7] << 8));
        public byte SubTileRaw => raw[8];
        public byte LevelRaw => raw[9];
        public byte TailRaw => raw[10];
        public IReadOnlyList<IniSourceProvenance> Provenance { get; }

        public byte[] GetRawBytesCopy()
        {
            return (byte[])raw.Clone();
        }
    }

    internal sealed class IsoMapPack5RecordReadResult
    {
        internal IsoMapPack5RecordReadResult(
            IEnumerable<IsoMapPack5RecordRaw> records,
            IsoMapPack5TrailingData trailing,
            IEnumerable<IsoMapDiagnostic> diagnostics)
        {
            Records = Array.AsReadOnly((records ?? throw new ArgumentNullException(nameof(records))).ToArray());
            Trailing = trailing;
            Diagnostics = Array.AsReadOnly((diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray());
        }

        public IReadOnlyList<IsoMapPack5RecordRaw> Records { get; }
        public IsoMapPack5TrailingData Trailing { get; }
        public IReadOnlyList<IsoMapDiagnostic> Diagnostics { get; }
        public bool IsSuccess => Diagnostics.All(item => item.Severity != BinaryDiagnosticSeverity.Error);
    }

    internal readonly struct IsoMapCoordinateKey : IEquatable<IsoMapCoordinateKey>
    {
        internal IsoMapCoordinateKey(ushort xRaw, ushort yRaw)
        {
            XRaw = xRaw;
            YRaw = yRaw;
        }

        public ushort XRaw { get; }
        public ushort YRaw { get; }

        public bool Equals(IsoMapCoordinateKey other) => XRaw == other.XRaw && YRaw == other.YRaw;
        public override bool Equals(object obj) => obj is IsoMapCoordinateKey && Equals((IsoMapCoordinateKey)obj);
        public override int GetHashCode() => unchecked((XRaw * 397) ^ YRaw);
    }

    internal enum IsoMapCoordinateDuplicatePolicy
    {
        PreserveAllAndDiagnose,
        RejectAnyDuplicate,
        AllowByteIdenticalDuplicatesButDiagnose
    }

    internal enum IsoMapCoordinateAxisOrder
    {
        XThenY,
        YThenX
    }

    internal enum IsoMapCoordinateSignednessCandidate
    {
        RawUnsigned,
        Signed16Candidate
    }

    internal sealed class IsoMapCoordinateValidationProfile
    {
        public IsoMapCoordinateValidationProfile(
            IsoMapCoordinateAxisOrder axisOrder = IsoMapCoordinateAxisOrder.XThenY,
            IsoMapCoordinateSignednessCandidate signedness = IsoMapCoordinateSignednessCandidate.RawUnsigned,
            int? width = null,
            int? height = null,
            bool configuredDenseCountCandidate = false)
        {
            if (width.HasValue && width.Value < 0 || height.HasValue && height.Value < 0) throw new ArgumentOutOfRangeException();
            AxisOrder = axisOrder;
            Signedness = signedness;
            Width = width;
            Height = height;
            ConfiguredDenseCountCandidate = configuredDenseCountCandidate;
        }

        public IsoMapCoordinateAxisOrder AxisOrder { get; }
        public IsoMapCoordinateSignednessCandidate Signedness { get; }
        public int? Width { get; }
        public int? Height { get; }
        public bool ConfiguredDenseCountCandidate { get; }
    }

    internal sealed class IsoMapCoordinateOccurrence
    {
        internal IsoMapCoordinateOccurrence(
            IsoMapCoordinateKey key,
            int sourceOrdinal,
            int firstOccurrenceOrdinal,
            bool outOfDomainCandidate,
            IsoMapPack5RecordRaw record)
        {
            Key = key;
            SourceOrdinal = sourceOrdinal;
            FirstOccurrenceOrdinal = firstOccurrenceOrdinal;
            OutOfDomainCandidate = outOfDomainCandidate;
            Record = record ?? throw new ArgumentNullException(nameof(record));
        }

        public IsoMapCoordinateKey Key { get; }
        public int SourceOrdinal { get; }
        public int FirstOccurrenceOrdinal { get; }
        public bool OutOfDomainCandidate { get; }
        public IsoMapPack5RecordRaw Record { get; }
    }

    internal sealed class IsoMapCoordinateDuplicateGroup
    {
        internal IsoMapCoordinateDuplicateGroup(IsoMapCoordinateKey key, IEnumerable<IsoMapCoordinateOccurrence> occurrences, bool conflicting)
        {
            Key = key;
            Occurrences = Array.AsReadOnly((occurrences ?? throw new ArgumentNullException(nameof(occurrences))).ToArray());
            ConflictingPayload = conflicting;
        }

        public IsoMapCoordinateKey Key { get; }
        public IReadOnlyList<IsoMapCoordinateOccurrence> Occurrences { get; }
        public bool ConflictingPayload { get; }
    }

    internal sealed class IsoMapCoordinateIndex
    {
        internal IsoMapCoordinateIndex(IEnumerable<IsoMapCoordinateOccurrence> occurrences, IEnumerable<IsoMapCoordinateDuplicateGroup> duplicateGroups)
        {
            Occurrences = Array.AsReadOnly((occurrences ?? throw new ArgumentNullException(nameof(occurrences))).ToArray());
            DuplicateGroups = Array.AsReadOnly((duplicateGroups ?? throw new ArgumentNullException(nameof(duplicateGroups))).ToArray());
        }

        public IReadOnlyList<IsoMapCoordinateOccurrence> Occurrences { get; }
        public IReadOnlyList<IsoMapCoordinateDuplicateGroup> DuplicateGroups { get; }
    }

    internal sealed class IsoMapCoordinateAnalysis
    {
        internal IsoMapCoordinateAnalysis(IsoMapCoordinateIndex index, IEnumerable<IsoMapDiagnostic> diagnostics, bool denseCountCandidate)
        {
            Index = index ?? throw new ArgumentNullException(nameof(index));
            Diagnostics = Array.AsReadOnly((diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray());
            DenseCountCandidate = denseCountCandidate;
        }

        public IsoMapCoordinateIndex Index { get; }
        public IReadOnlyList<IsoMapDiagnostic> Diagnostics { get; }
        public bool DenseCountCandidate { get; }
        public bool IsSuccess => Diagnostics.All(item => item.Severity != BinaryDiagnosticSeverity.Error);
    }

    internal sealed class IsoMapPack5PackedReadPolicy
    {
        public IsoMapPack5PackedReadPolicy(
            PackedSectionDecodePolicy packedPolicy,
            IsoMapPack5TrailingPolicy trailingPolicy = IsoMapPack5TrailingPolicy.RejectAnyRemainder,
            IsoMapCoordinateDuplicatePolicy duplicatePolicy = IsoMapCoordinateDuplicatePolicy.PreserveAllAndDiagnose,
            IsoMapCoordinateValidationProfile coordinateProfile = null,
            IsoMapPack5ReadLimits limits = null)
        {
            PackedPolicy = packedPolicy ?? throw new ArgumentNullException(nameof(packedPolicy));
            TrailingPolicy = trailingPolicy;
            DuplicatePolicy = duplicatePolicy;
            CoordinateProfile = coordinateProfile;
            Limits = limits ?? new IsoMapPack5ReadLimits();
        }

        public PackedSectionDecodePolicy PackedPolicy { get; }
        public IsoMapPack5TrailingPolicy TrailingPolicy { get; }
        public IsoMapCoordinateDuplicatePolicy DuplicatePolicy { get; }
        public IsoMapCoordinateValidationProfile CoordinateProfile { get; }
        public IsoMapPack5ReadLimits Limits { get; }
    }

    internal sealed class IsoMapPack5PackedReadResult
    {
        internal IsoMapPack5PackedReadResult(PackedSectionDecodeResult packed, IsoMapPack5RecordReadResult records, IsoMapCoordinateAnalysis coordinates, IEnumerable<IsoMapDiagnostic> diagnostics)
        {
            Packed = packed;
            Records = records;
            Coordinates = coordinates;
            Diagnostics = Array.AsReadOnly((diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray());
        }

        public PackedSectionDecodeResult Packed { get; }
        public IsoMapPack5RecordReadResult Records { get; }
        public IsoMapCoordinateAnalysis Coordinates { get; }
        public IReadOnlyList<IsoMapDiagnostic> Diagnostics { get; }
        public bool IsSuccess => Packed != null && Packed.IsSuccess && Records != null && Records.IsSuccess && (Coordinates == null || Coordinates.IsSuccess) && Diagnostics.All(item => item.Severity != BinaryDiagnosticSeverity.Error);
    }
}
