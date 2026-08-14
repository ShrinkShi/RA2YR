using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RA2YR.Core.Formats.VxlHva
{
    public enum LegacyVisualDiagnosticSeverity { Warning, Error }

    public enum LegacyVisualDiagnosticCode
    {
        InputBudgetExceeded, AllocationBudgetExceeded, TruncatedHeader, TruncatedRecord,
        InvalidMagic, InvalidLength, ArithmeticOverflow, UnexpectedTrailingData,
        SectionCountMismatch, ZeroSectionCountUnconfirmed, InvalidSectionName,
        DuplicateSectionName, InvalidSpanDirectory, InvalidSpanRange, InconsistentEmptyColumn,
        SpanDataTruncated, SpanCommandOverflow, SpanVoxelBudgetExceeded, InvalidDuplicateCount,
        InvalidNormalMode, InvalidNormalIndex, UnknownNormalMode, InvalidFiniteValue,
        HvaCountBudgetExceeded, HvaNameTableTruncated, HvaTransformTruncated,
        HvaTrailingData, DuplicateHvaSectionName, CaseOnlySectionNameConflict,
        AmbiguousBinding, MissingBinding, UnboundSection, WrongInput, NoProgress
    }

    public sealed class LegacyVisualDiagnostic
    {
        public LegacyVisualDiagnostic(
            LegacyVisualDiagnosticCode code,
            LegacyVisualDiagnosticSeverity severity,
            long offset,
            string stage,
            string message,
            int ordinal = -1)
        {
            Code = code;
            Severity = severity;
            Offset = offset;
            Stage = stage ?? throw new ArgumentNullException(nameof(stage));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Ordinal = ordinal;
        }

        public LegacyVisualDiagnosticCode Code { get; }
        public LegacyVisualDiagnosticSeverity Severity { get; }
        public long Offset { get; }
        public string Stage { get; }
        public string Message { get; }
        public int Ordinal { get; }
    }

    public enum LegacyVisualCompletionStatus { Succeeded, Failed }

    public sealed class LegacyVisualExecutionState
    {
        internal LegacyVisualExecutionState(
            LegacyVisualCompletionStatus status,
            bool hasFatalError,
            LegacyVisualDiagnosticSeverity highestSeverity,
            int suppressedDiagnosticCount)
        {
            CompletionStatus = status;
            HasFatalError = hasFatalError;
            HighestSeverity = highestSeverity;
            SuppressedDiagnosticCount = suppressedDiagnosticCount;
        }

        public LegacyVisualCompletionStatus CompletionStatus { get; }
        public bool HasFatalError { get; }
        public LegacyVisualDiagnosticSeverity HighestSeverity { get; }
        public int SuppressedDiagnosticCount { get; }
        public bool IsSuccess => CompletionStatus == LegacyVisualCompletionStatus.Succeeded;
    }

    internal sealed class LegacyVisualDiagnosticCollector
    {
        private readonly int max;
        private readonly List<LegacyVisualDiagnostic> diagnostics = new List<LegacyVisualDiagnostic>();
        private bool fatal;
        private LegacyVisualDiagnosticSeverity highest;
        private int suppressed;

        public LegacyVisualDiagnosticCollector(int maxDiagnostics)
        {
            if (maxDiagnostics < 0) throw new ArgumentOutOfRangeException(nameof(maxDiagnostics));
            max = maxDiagnostics;
        }

        public IReadOnlyList<LegacyVisualDiagnostic> Diagnostics => diagnostics;
        public bool Fatal => fatal;
        public LegacyVisualDiagnosticSeverity Highest => highest;
        public int Suppressed => suppressed;

        public void Add(LegacyVisualDiagnostic diagnostic)
        {
            if (diagnostic == null) throw new ArgumentNullException(nameof(diagnostic));
            if (diagnostic.Severity == LegacyVisualDiagnosticSeverity.Error) fatal = true;
            if (diagnostic.Severity > highest) highest = diagnostic.Severity;
            if (diagnostics.Count < max) diagnostics.Add(diagnostic);
            else if (suppressed < int.MaxValue) suppressed++;
        }

        public LegacyVisualExecutionState Complete()
        {
            return new LegacyVisualExecutionState(
                fatal ? LegacyVisualCompletionStatus.Failed : LegacyVisualCompletionStatus.Succeeded,
                fatal, highest, suppressed);
        }
    }

    public sealed class VxlHvaReadLimits
    {
        public VxlHvaReadLimits(
            long maxInputBytes = 16 * 1024 * 1024,
            int maxSections = 1024,
            long maxBodyBytes = 16 * 1024 * 1024,
            long maxColumns = 4 * 1024 * 1024,
            long maxStoredVoxels = 16 * 1024 * 1024,
            long maxDiagnostics = 256)
        {
            if (maxInputBytes < 0 || maxBodyBytes < 0 || maxColumns < 0 || maxStoredVoxels < 0 || maxDiagnostics < 0)
                throw new ArgumentOutOfRangeException();
            if (maxSections < 0) throw new ArgumentOutOfRangeException(nameof(maxSections));
            MaxInputBytes = maxInputBytes;
            MaxSections = maxSections;
            MaxBodyBytes = maxBodyBytes;
            MaxColumns = maxColumns;
            MaxStoredVoxels = maxStoredVoxels;
            MaxDiagnostics = maxDiagnostics;
        }

        public static VxlHvaReadLimits Default { get; } = new VxlHvaReadLimits();
        public long MaxInputBytes { get; }
        public int MaxSections { get; }
        public long MaxBodyBytes { get; }
        public long MaxColumns { get; }
        public long MaxStoredVoxels { get; }
        public long MaxDiagnostics { get; }
    }

    public readonly struct PaletteBindingDescriptor : IEquatable<PaletteBindingDescriptor>
    {
        public PaletteBindingDescriptor(string logicalPaletteId, PaletteConversionProfile conversionProfile)
        {
            if (string.IsNullOrWhiteSpace(logicalPaletteId)) throw new ArgumentException("Palette identity is required.", nameof(logicalPaletteId));
            LogicalPaletteId = logicalPaletteId;
            ConversionProfile = conversionProfile;
        }
        public string LogicalPaletteId { get; }
        public PaletteConversionProfile ConversionProfile { get; }
        public bool Equals(PaletteBindingDescriptor other) => string.Equals(LogicalPaletteId, other.LogicalPaletteId, StringComparison.Ordinal) && ConversionProfile == other.ConversionProfile;
        public override bool Equals(object obj) => obj is PaletteBindingDescriptor other && Equals(other);
        public override int GetHashCode() => (LogicalPaletteId ?? string.Empty).GetHashCode() ^ (int)ConversionProfile;
    }

    public enum PaletteConversionProfile
    {
        Unresolved,
        ShiftLeftTwo,
        ReplicateHighBits,
        ScaleToFullRangeRounded,
        XccScaleToFullRangeFloor
    }

    public readonly struct TeamRemapDescriptor : IEquatable<TeamRemapDescriptor>
    {
        public TeamRemapDescriptor(byte startRaw, byte endRaw)
        {
            StartRaw = startRaw;
            EndRaw = endRaw;
        }
        public byte StartRaw { get; }
        public byte EndRaw { get; }
        public bool IsReversed => StartRaw > EndRaw;
        public bool Equals(TeamRemapDescriptor other) => StartRaw == other.StartRaw && EndRaw == other.EndRaw;
        public override bool Equals(object obj) => obj is TeamRemapDescriptor other && Equals(other);
        public override int GetHashCode() => StartRaw | (EndRaw << 8);
    }

    public sealed class IndexedImageDescriptor
    {
        private readonly byte[] indices;
        private readonly IReadOnlyList<LegacyVisualDiagnostic> diagnostics;
        public IndexedImageDescriptor(
            int width,
            int height,
            IEnumerable<byte> indexBuffer,
            PaletteBindingDescriptor palette,
            bool? transparentIndexZero,
            TeamRemapDescriptor? teamRemap,
            IEnumerable<LegacyVisualDiagnostic> diagnostics = null)
        {
            if (width < 0 || height < 0) throw new ArgumentOutOfRangeException();
            indices = (indexBuffer ?? throw new ArgumentNullException(nameof(indexBuffer))).ToArray();
            if (indices.LongLength != checked((long)width * height)) throw new ArgumentException("Index buffer length does not match dimensions.", nameof(indexBuffer));
            Width = width; Height = height; Palette = palette; TransparentIndexZero = transparentIndexZero; TeamRemap = teamRemap;
            this.diagnostics = Array.AsReadOnly((diagnostics ?? Enumerable.Empty<LegacyVisualDiagnostic>()).ToArray());
        }
        public int Width { get; }
        public int Height { get; }
        public PaletteBindingDescriptor Palette { get; }
        public bool? TransparentIndexZero { get; }
        public TeamRemapDescriptor? TeamRemap { get; }
        public IReadOnlyList<LegacyVisualDiagnostic> Diagnostics => diagnostics;
        public ReadOnlyMemory<byte> Indices => new ReadOnlyMemory<byte>(indices);
        public byte[] GetIndicesCopy() => (byte[])indices.Clone();
    }

    public sealed class VxlHeaderRaw
    {
        internal VxlHeaderRaw(byte[] fileType, uint paletteCount, uint sectionHeaderCount, uint sectionTailerCount, uint bodySize, byte remapStart, byte remapEnd, byte[] palette)
        {
            FileTypeRaw = Array.AsReadOnly((byte[])fileType.Clone()); PaletteRaw = Array.AsReadOnly((byte[])palette.Clone());
            PaletteCountRaw = paletteCount; SectionHeaderCountRaw = sectionHeaderCount; SectionTailerCountRaw = sectionTailerCount; BodySizeRaw = bodySize; StartPaletteRemapRaw = remapStart; EndPaletteRemapRaw = remapEnd;
        }
        public IReadOnlyList<byte> FileTypeRaw { get; }
        public uint PaletteCountRaw { get; }
        public uint SectionHeaderCountRaw { get; }
        public uint SectionTailerCountRaw { get; }
        public uint BodySizeRaw { get; }
        public byte StartPaletteRemapRaw { get; }
        public byte EndPaletteRemapRaw { get; }
        public IReadOnlyList<byte> PaletteRaw { get; }
    }

    public sealed class VxlSectionHeaderRaw
    {
        internal VxlSectionHeaderRaw(byte[] name, uint number, uint unknown1, uint unknown2, int ordinal)
        { NameRaw = Array.AsReadOnly((byte[])name.Clone()); SectionNumberRaw = number; Unknown1Raw = unknown1; Unknown2Raw = unknown2; Ordinal = ordinal; }
        public IReadOnlyList<byte> NameRaw { get; }
        public uint SectionNumberRaw { get; }
        public uint Unknown1Raw { get; }
        public uint Unknown2Raw { get; }
        public int Ordinal { get; }
        public string NameCandidate => LegacyVisualRawName.Decode(NameRaw);
    }

    public sealed class VxlSectionTailerRaw
    {
        internal VxlSectionTailerRaw(uint start, uint end, uint data, uint scale, uint[] transform, uint[] min, uint[] max, byte x, byte y, byte z, byte normal, int ordinal)
        { SpanStartOffsetRaw = start; SpanEndOffsetRaw = end; SpanDataOffsetRaw = data; ScaleRawBits = scale; TransformRawBits = Array.AsReadOnly((uint[])transform.Clone()); MinBoundsRawBits = Array.AsReadOnly((uint[])min.Clone()); MaxBoundsRawBits = Array.AsReadOnly((uint[])max.Clone()); SizeXRaw = x; SizeYRaw = y; SizeZRaw = z; NormalTypeRaw = normal; Ordinal = ordinal; }
        public uint SpanStartOffsetRaw { get; }
        public uint SpanEndOffsetRaw { get; }
        public uint SpanDataOffsetRaw { get; }
        public uint ScaleRawBits { get; }
        public IReadOnlyList<uint> TransformRawBits { get; }
        public IReadOnlyList<uint> MinBoundsRawBits { get; }
        public IReadOnlyList<uint> MaxBoundsRawBits { get; }
        public byte SizeXRaw { get; }
        public byte SizeYRaw { get; }
        public byte SizeZRaw { get; }
        public byte NormalTypeRaw { get; }
        public int Ordinal { get; }
    }

    public readonly struct VxlVoxelRaw
    {
        public VxlVoxelRaw(byte colorIndex, byte normalIndex) { ColorIndex = colorIndex; NormalIndex = normalIndex; }
        public byte ColorIndex { get; }
        public byte NormalIndex { get; }
    }

    public sealed class VxlSpanChunkRaw
    {
        internal VxlSpanChunkRaw(byte skip, byte count, IEnumerable<VxlVoxelRaw> voxels, byte duplicate)
        { Skip = skip; Count = count; Voxels = Array.AsReadOnly((voxels ?? Enumerable.Empty<VxlVoxelRaw>()).ToArray()); DuplicateCountRaw = duplicate; }
        public byte Skip { get; }
        public byte Count { get; }
        public IReadOnlyList<VxlVoxelRaw> Voxels { get; }
        public byte DuplicateCountRaw { get; }
    }

    public sealed class VxlColumnRaw
    {
        internal VxlColumnRaw(int index, int x, int y, int start, int end, IEnumerable<VxlSpanChunkRaw> chunks)
        { ColumnIndex = index; X = x; Y = y; StartOffsetRaw = start; EndOffsetRaw = end; Chunks = Array.AsReadOnly((chunks ?? Enumerable.Empty<VxlSpanChunkRaw>()).ToArray()); }
        public int ColumnIndex { get; }
        public int X { get; }
        public int Y { get; }
        public int StartOffsetRaw { get; }
        public int EndOffsetRaw { get; }
        public IReadOnlyList<VxlSpanChunkRaw> Chunks { get; }
        public bool IsEmpty => StartOffsetRaw == -1 && EndOffsetRaw == -1;
    }

    public sealed class VxlSectionRaw
    {
        internal VxlSectionRaw(VxlSectionHeaderRaw header, VxlSectionTailerRaw tailer, IEnumerable<VxlColumnRaw> columns)
        { Header = header; Tailer = tailer; Columns = Array.AsReadOnly((columns ?? Enumerable.Empty<VxlColumnRaw>()).ToArray()); }
        public VxlSectionHeaderRaw Header { get; }
        public VxlSectionTailerRaw Tailer { get; }
        public IReadOnlyList<VxlColumnRaw> Columns { get; }
    }

    public sealed class VxlDocumentRaw
    {
        internal VxlDocumentRaw(VxlHeaderRaw header, IEnumerable<VxlSectionRaw> sections, string hash)
        { Header = header; Sections = Array.AsReadOnly((sections ?? Enumerable.Empty<VxlSectionRaw>()).ToArray()); CanonicalSha256 = hash; }
        public VxlHeaderRaw Header { get; }
        public IReadOnlyList<VxlSectionRaw> Sections { get; }
        public string CanonicalSha256 { get; }
    }

    public sealed class VxlReadResult
    {
        internal VxlReadResult(VxlDocumentRaw document, IEnumerable<LegacyVisualDiagnostic> diagnostics, LegacyVisualExecutionState execution)
        { Document = document; Diagnostics = Array.AsReadOnly((diagnostics ?? Enumerable.Empty<LegacyVisualDiagnostic>()).ToArray()); Execution = execution; }
        public VxlDocumentRaw Document { get; }
        public IReadOnlyList<LegacyVisualDiagnostic> Diagnostics { get; }
        public LegacyVisualExecutionState Execution { get; }
        public bool IsSuccess => Execution.IsSuccess && Document != null;
    }

    public sealed class HvaHeaderRaw
    {
        internal HvaHeaderRaw(byte[] label, uint frames, uint sections) { LabelRaw = Array.AsReadOnly((byte[])label.Clone()); FrameCountRaw = frames; SectionCountRaw = sections; }
        public IReadOnlyList<byte> LabelRaw { get; }
        public uint FrameCountRaw { get; }
        public uint SectionCountRaw { get; }
    }

    public sealed class HvaSectionNameRaw
    {
        internal HvaSectionNameRaw(byte[] name, int ordinal) { NameRaw = Array.AsReadOnly((byte[])name.Clone()); Ordinal = ordinal; }
        public IReadOnlyList<byte> NameRaw { get; }
        public int Ordinal { get; }
        public string NameCandidate => LegacyVisualRawName.Decode(NameRaw);
    }

    public sealed class HvaRawTransform3x4
    {
        internal HvaRawTransform3x4(uint[] bits, int ordinal) { RawBits = Array.AsReadOnly((uint[])bits.Clone()); RecordOrdinal = ordinal; }
        public IReadOnlyList<uint> RawBits { get; }
        public int RecordOrdinal { get; }
    }

    public enum HvaTransformRecordOrder { Unresolved, FrameMajor, SectionMajor }

    public sealed class HvaDocumentRaw
    {
        internal HvaDocumentRaw(HvaHeaderRaw header, IEnumerable<HvaSectionNameRaw> names, IEnumerable<HvaRawTransform3x4> transforms, string hash)
        { Header = header; SectionNames = Array.AsReadOnly((names ?? Enumerable.Empty<HvaSectionNameRaw>()).ToArray()); Transforms = Array.AsReadOnly((transforms ?? Enumerable.Empty<HvaRawTransform3x4>()).ToArray()); CanonicalSha256 = hash; }
        public HvaHeaderRaw Header { get; }
        public IReadOnlyList<HvaSectionNameRaw> SectionNames { get; }
        public IReadOnlyList<HvaRawTransform3x4> Transforms { get; }
        public string CanonicalSha256 { get; }
        public HvaRawTransform3x4 GetCandidate(int frame, int section, HvaTransformRecordOrder order)
        {
            if (frame < 0 || section < 0 || frame >= Header.FrameCountRaw || section >= Header.SectionCountRaw) throw new ArgumentOutOfRangeException();
            long index = order == HvaTransformRecordOrder.FrameMajor ? (long)frame * Header.SectionCountRaw + section : order == HvaTransformRecordOrder.SectionMajor ? (long)section * Header.FrameCountRaw + frame : -1;
            if (index < 0 || index >= Transforms.Count) throw new ArgumentException("An explicit HVA record order is required.", nameof(order));
            return Transforms[(int)index];
        }
    }

    public sealed class HvaReadResult
    {
        internal HvaReadResult(HvaDocumentRaw document, IEnumerable<LegacyVisualDiagnostic> diagnostics, LegacyVisualExecutionState execution)
        { Document = document; Diagnostics = Array.AsReadOnly((diagnostics ?? Enumerable.Empty<LegacyVisualDiagnostic>()).ToArray()); Execution = execution; }
        public HvaDocumentRaw Document { get; }
        public IReadOnlyList<LegacyVisualDiagnostic> Diagnostics { get; }
        public LegacyVisualExecutionState Execution { get; }
        public bool IsSuccess => Execution.IsSuccess && Document != null;
    }

    public enum VxlHvaBindingStatus { Complete, Incomplete, Ambiguous, NotAttempted }

    public sealed class VxlHvaBinding
    {
        internal VxlHvaBinding(int vxlOrdinal, int hvaOrdinal, string name) { VxlSectionOrdinal = vxlOrdinal; HvaSectionOrdinal = hvaOrdinal; Name = name; }
        public int VxlSectionOrdinal { get; }
        public int HvaSectionOrdinal { get; }
        public string Name { get; }
    }

    public sealed class VxlHvaBindingResult
    {
        internal VxlHvaBindingResult(VxlHvaBindingStatus status, IEnumerable<VxlHvaBinding> bindings, IEnumerable<int> unboundVxl, IEnumerable<int> unboundHva, IEnumerable<LegacyVisualDiagnostic> diagnostics, LegacyVisualExecutionState execution)
        { Status = status; Bindings = Array.AsReadOnly((bindings ?? Enumerable.Empty<VxlHvaBinding>()).ToArray()); UnboundVxlSections = Array.AsReadOnly((unboundVxl ?? Enumerable.Empty<int>()).ToArray()); UnboundHvaSections = Array.AsReadOnly((unboundHva ?? Enumerable.Empty<int>()).ToArray()); Diagnostics = Array.AsReadOnly((diagnostics ?? Enumerable.Empty<LegacyVisualDiagnostic>()).ToArray()); Execution = execution; }
        public VxlHvaBindingStatus Status { get; }
        public IReadOnlyList<VxlHvaBinding> Bindings { get; }
        public IReadOnlyList<int> UnboundVxlSections { get; }
        public IReadOnlyList<int> UnboundHvaSections { get; }
        public IReadOnlyList<LegacyVisualDiagnostic> Diagnostics { get; }
        public LegacyVisualExecutionState Execution { get; }
        public bool IsSuccess => Execution.IsSuccess && Status == VxlHvaBindingStatus.Complete;
    }

    internal static class LegacyVisualRawName
    {
        public static string Decode(IReadOnlyList<byte> raw)
        {
            int length = 0;
            while (length < raw.Count && raw[length] != 0) length++;
            if (length == 0) return string.Empty;
            var chars = new char[length];
            for (int i = 0; i < length; i++) chars[i] = raw[i] >= 32 && raw[i] <= 126 ? (char)raw[i] : '\ufffd';
            return new string(chars);
        }
    }

    internal static class LegacyVisualHash
    {
        public static string Sha256(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
