using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RA2YR.Core.Binary;
using RA2YR.Core.Configuration.Ini.Resolution;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Formats.Tmp
{
    internal enum TmpDiagnosticCode
    {
        InputBudgetExceeded, InvalidFileHeader, InvalidGridDimensions, OffsetTableTruncated,
        NegativeOffset, OffsetInsideHeader, OffsetInsideOffsetTable, OffsetOutsideWindow,
        CellHeaderTruncated, OffsetArithmeticOverflow, DuplicateCellOffset, PlaneOverlap,
        PlaneOutsideWindow, PlaneTruncated, InvalidDimensions, DimensionBudgetExceeded,
        DiamondProfileMismatch, DiamondLengthOverflow, ExtraAreaOverflow, UnknownFlags,
        TrailingBytes, DamagedDataUnresolved, DeclaredSequentialDisagreement, InvalidPlanePolicy,
        MissingGeneral, InvalidTileSetSection, TileSetGap, DuplicateTileSetIndex,
        MissingRequiredValue, InvalidInteger, NegativeTilesInSet, ZeroTilesInSet,
        GlobalIdOverflow, GlobalIdBudgetExceeded, SpecialRoleOutOfRange, MissingTmpAsset,
        AmbiguousAssetCandidate, CaseCollision, FallbackCandidate, VariationCandidate,
        InvalidProfile, MissingControlDocument, MissingProvider, InvalidTileOrdinal,
        GlobalTileIdOutOfRange, NoProgress, ArithmeticOverflow, DiagnosticBudgetExceeded,
        SourceFailure
    }

    internal enum TmpCompletionStatus { Succeeded, Failed }

    internal sealed class TmpExecutionState
    {
        private BinaryDiagnosticSeverity highest = BinaryDiagnosticSeverity.Info;
        private bool failed;
        private int suppressed;

        public TmpCompletionStatus CompletionStatus => failed ? TmpCompletionStatus.Failed : TmpCompletionStatus.Succeeded;
        public bool HasFatalError => failed;
        public BinaryDiagnosticSeverity HighestObservedSeverity => highest;
        public int SuppressedDiagnosticCount => suppressed;

        internal void Observe(BinaryDiagnosticSeverity severity)
        {
            if ((int)severity > (int)highest) highest = severity;
            if (severity == BinaryDiagnosticSeverity.Error) failed = true;
        }

        internal void Fail()
        {
            failed = true;
            highest = BinaryDiagnosticSeverity.Error;
        }

        internal void Suppress(int count = 1)
        {
            if (count <= 0 || suppressed == int.MaxValue) return;
            long total = (long)suppressed + count;
            suppressed = total >= int.MaxValue ? int.MaxValue : (int)total;
        }

        internal void Merge(TmpExecutionState child)
        {
            if (child == null) return;
            Observe(child.HighestObservedSeverity);
            if (child.HasFatalError) Fail();
            Suppress(child.SuppressedDiagnosticCount);
        }
    }

    internal sealed class TmpDiagnostic
    {
        internal TmpDiagnostic(BinaryDiagnosticSeverity severity, TmpDiagnosticCode code, BinarySourceContext source,
            IniSourceProvenance provenance, long absoluteOffset, int cellOrdinal, string stage, string message)
        {
            Severity = severity;
            Code = code;
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
            AbsoluteOffset = absoluteOffset;
            CellOrdinal = cellOrdinal;
            Stage = BinaryDiagnosticLabel.Validate(stage, nameof(stage));
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public BinaryDiagnosticSeverity Severity { get; }
        public TmpDiagnosticCode Code { get; }
        public BinarySourceContext Source { get; }
        public IniSourceProvenance Provenance { get; }
        public long AbsoluteOffset { get; }
        public int CellOrdinal { get; }
        public string Stage { get; }
        public string Message { get; }
    }

    internal sealed class TmpDiagnosticCollector
    {
        private readonly int budget;
        private readonly List<TmpDiagnostic> diagnostics = new List<TmpDiagnostic>();
        internal readonly TmpExecutionState Execution = new TmpExecutionState();

        internal TmpDiagnosticCollector(int budget)
        {
            if (budget < 0) throw new ArgumentOutOfRangeException(nameof(budget));
            this.budget = budget;
        }

        internal IReadOnlyList<TmpDiagnostic> Diagnostics => diagnostics;

        internal void Add(TmpDiagnostic diagnostic, bool fatal)
        {
            if (diagnostic == null) throw new ArgumentNullException(nameof(diagnostic));
            if (fatal || diagnostic.Severity == BinaryDiagnosticSeverity.Error) Execution.Fail();
            else Execution.Observe(diagnostic.Severity);
            if (diagnostics.Count < budget) diagnostics.Add(diagnostic);
            else Execution.Suppress();
        }

        internal void Fail(TmpDiagnostic diagnostic) => Add(diagnostic, true);
    }

    internal sealed class TmpReadLimits
    {
        public TmpReadLimits(long maxInputBytes = 64 * 1024 * 1024, int maxCellSlots = 1_000_000,
            int maxDiagnostics = 4096, int maxDistinctPlaneWindows = 1_000_000,
            int maxTemplateWidth = 4096, int maxTemplateHeight = 4096,
            int maxTileWidth = 4096, int maxTileHeight = 4096, long maxDiamondPlaneBytes = 16 * 1024 * 1024,
            int maxExtraWidth = 4096, int maxExtraHeight = 4096, long maxExtraArea = 16 * 1024 * 1024)
        {
            if (maxInputBytes < 0 || maxCellSlots < 0 || maxDiagnostics < 0 || maxDistinctPlaneWindows < 0 ||
                maxTemplateWidth < 0 || maxTemplateHeight < 0 || maxTileWidth < 0 || maxTileHeight < 0 ||
                maxDiamondPlaneBytes < 0 || maxExtraWidth < 0 || maxExtraHeight < 0 || maxExtraArea < 0)
                throw new ArgumentOutOfRangeException();
            MaxInputBytes = maxInputBytes; MaxCellSlots = maxCellSlots; MaxDiagnostics = maxDiagnostics;
            MaxDistinctPlaneWindows = maxDistinctPlaneWindows; MaxTemplateWidth = maxTemplateWidth;
            MaxTemplateHeight = maxTemplateHeight; MaxTileWidth = maxTileWidth; MaxTileHeight = maxTileHeight;
            MaxDiamondPlaneBytes = maxDiamondPlaneBytes; MaxExtraWidth = maxExtraWidth;
            MaxExtraHeight = maxExtraHeight; MaxExtraArea = maxExtraArea;
        }
        public long MaxInputBytes { get; }
        public int MaxCellSlots { get; }
        public int MaxDiagnostics { get; }
        public int MaxDistinctPlaneWindows { get; }
        public int MaxTemplateWidth { get; }
        public int MaxTemplateHeight { get; }
        public int MaxTileWidth { get; }
        public int MaxTileHeight { get; }
        public long MaxDiamondPlaneBytes { get; }
        public int MaxExtraWidth { get; }
        public int MaxExtraHeight { get; }
        public long MaxExtraArea { get; }
    }

    internal enum TmpPlaneLayoutPolicy { DeclaredOffsets, SequentialWithZ, SequentialWithoutZ }

    internal sealed class TmpReadPolicy
    {
        public TmpReadPolicy(int tileWidth = 60, int tileHeight = 30,
            TmpPlaneLayoutPolicy planeLayout = TmpPlaneLayoutPolicy.DeclaredOffsets,
            TmpReadLimits limits = null)
        {
            if (tileWidth <= 0 || tileHeight <= 0) throw new ArgumentOutOfRangeException(nameof(tileWidth));
            if (!Enum.IsDefined(typeof(TmpPlaneLayoutPolicy), planeLayout)) throw new ArgumentOutOfRangeException(nameof(planeLayout));
            TileWidth = tileWidth; TileHeight = tileHeight; PlaneLayout = planeLayout; Limits = limits ?? new TmpReadLimits();
        }
        public int TileWidth { get; }
        public int TileHeight { get; }
        public TmpPlaneLayoutPolicy PlaneLayout { get; }
        public TmpReadLimits Limits { get; }
    }

    internal sealed class TmpFileHeaderRaw
    {
        internal TmpFileHeaderRaw(uint field0, uint field1, uint field2, uint field3)
        { Field0Raw = field0; Field1Raw = field1; Field2Raw = field2; Field3Raw = field3; }
        public uint Field0Raw { get; }
        public uint Field1Raw { get; }
        public uint Field2Raw { get; }
        public uint Field3Raw { get; }
        public uint BlocksXRaw => Field0Raw;
        public uint BlocksYRaw => Field1Raw;
        public uint ReservedField2Raw => Field2Raw;
        public uint ReservedField3Raw => Field3Raw;
    }

    internal sealed class TmpCellOffsetEntry
    {
        internal TmpCellOffsetEntry(int slot, int raw, bool empty) { SlotOrdinal = slot; OffsetRawI32 = raw; IsEmptyCandidate = empty; }
        public int SlotOrdinal { get; }
        public int OffsetRawI32 { get; }
        public uint OffsetRawU32 => unchecked((uint)OffsetRawI32);
        public bool IsEmptyCandidate { get; }
    }

    internal sealed class TmpCellHeaderRaw
    {
        private readonly byte[] raw;
        internal TmpCellHeaderRaw(long sourceOffset, byte[] bytes)
        {
            if (sourceOffset < 0) throw new ArgumentOutOfRangeException(nameof(sourceOffset));
            raw = (bytes ?? throw new ArgumentNullException(nameof(bytes))).ToArray();
            if (raw.Length != 52) throw new ArgumentException("TMP cell headers are exactly 52 bytes.", nameof(bytes));
            SourceOffset = sourceOffset;
        }
        public long SourceOffset { get; }
        public int XRawI32 => ReadI32(0); public int YRawI32 => ReadI32(4);
        public uint ExtraColorOffsetRawU32 => ReadU32(8); public uint DiamondDepthOffsetRawU32 => ReadU32(12);
        public uint ExtraDepthOffsetRawU32 => ReadU32(16); public int ExtraXRawI32 => ReadI32(20);
        public int ExtraYRawI32 => ReadI32(24); public uint ExtraWidthRawU32 => ReadU32(28);
        public uint ExtraHeightRawU32 => ReadU32(32); public uint FlagsRawU32 => ReadU32(36);
        public byte HeightRaw => raw[40]; public byte TerrainTypeRaw => raw[41]; public byte RampTypeRaw => raw[42];
        public byte RadarLeftComponent0Raw => raw[43]; public byte RadarLeftComponent1Raw => raw[44]; public byte RadarLeftComponent2Raw => raw[45];
        public byte RadarRightComponent0Raw => raw[46]; public byte RadarRightComponent1Raw => raw[47]; public byte RadarRightComponent2Raw => raw[48];
        public byte Trailing0Raw => raw[49]; public byte Trailing1Raw => raw[50]; public byte Trailing2Raw => raw[51];
        public bool HasExtraDataCandidate => (FlagsRawU32 & 1u) != 0;
        public bool HasZDataCandidate => (FlagsRawU32 & 2u) != 0;
        public bool HasDamagedDataCandidate => (FlagsRawU32 & 4u) != 0;
        public uint UnknownFlagsRaw => FlagsRawU32 & ~7u;
        public byte[] GetRawBytesCopy() => raw.ToArray();
        private int ReadI32(int offset) => unchecked((int)ReadU32(offset));
        private uint ReadU32(int offset) => (uint)(raw[offset] | (raw[offset + 1] << 8) | (raw[offset + 2] << 16) | (raw[offset + 3] << 24));
    }

    internal sealed class TmpPlaneWindow
    {
        internal TmpPlaneWindow(string kind, long relativeOffset, long length, byte[] bytes, bool present, string status)
        { Kind = kind; RelativeOffset = relativeOffset; Length = length; bytes = bytes ?? Array.Empty<byte>(); this.bytes = bytes.ToArray(); Present = present; Status = status; }
        private readonly byte[] bytes;
        public string Kind { get; } public long RelativeOffset { get; } public long Length { get; }
        public bool Present { get; } public string Status { get; }
        public byte[] GetBytesCopy() => bytes.ToArray();
    }

    internal sealed class TmpCellPlaneDirectory
    {
        internal TmpCellPlaneDirectory(TmpPlaneLayoutPolicy policy, IEnumerable<TmpPlaneWindow> planes, IEnumerable<TmpDiagnostic> diagnostics)
        { Policy = policy; Planes = Array.AsReadOnly((planes ?? throw new ArgumentNullException(nameof(planes))).ToArray()); Diagnostics = Array.AsReadOnly((diagnostics ?? Enumerable.Empty<TmpDiagnostic>()).ToArray()); }
        public TmpPlaneLayoutPolicy Policy { get; }
        public IReadOnlyList<TmpPlaneWindow> Planes { get; }
        public IReadOnlyList<TmpDiagnostic> Diagnostics { get; }
        public TmpPlaneWindow Find(string kind) => Planes.FirstOrDefault(p => string.Equals(p.Kind, kind, StringComparison.Ordinal));
    }

    internal sealed class TmpCellRaw
    {
        internal TmpCellRaw(int slot, long offset, TmpCellHeaderRaw header, TmpCellPlaneDirectory directory)
        { SlotOrdinal = slot; SourceOffset = offset; Header = header ?? throw new ArgumentNullException(nameof(header)); PlaneDirectory = directory ?? throw new ArgumentNullException(nameof(directory)); }
        public int SlotOrdinal { get; } public long SourceOffset { get; }
        public TmpCellHeaderRaw Header { get; } public TmpCellPlaneDirectory PlaneDirectory { get; }
    }

    internal sealed class TmpDocument
    {
        internal TmpDocument(TmpFileHeaderRaw header, IEnumerable<TmpCellOffsetEntry> offsets, IEnumerable<TmpCellRaw> cells,
            IEnumerable<TmpDiagnostic> diagnostics, TmpExecutionState execution, long consumedBytes, string sha256)
        { Header = header ?? throw new ArgumentNullException(nameof(header)); OffsetTable = Array.AsReadOnly((offsets ?? throw new ArgumentNullException(nameof(offsets))).ToArray()); Cells = Array.AsReadOnly((cells ?? throw new ArgumentNullException(nameof(cells))).ToArray()); Diagnostics = Array.AsReadOnly((diagnostics ?? Enumerable.Empty<TmpDiagnostic>()).ToArray()); Execution = execution ?? new TmpExecutionState(); ConsumedBytes = consumedBytes; CanonicalSha256 = sha256 ?? string.Empty; }
        public TmpFileHeaderRaw Header { get; } public IReadOnlyList<TmpCellOffsetEntry> OffsetTable { get; }
        public IReadOnlyList<TmpCellRaw> Cells { get; } public IReadOnlyList<TmpDiagnostic> Diagnostics { get; }
        public TmpExecutionState Execution { get; } public long ConsumedBytes { get; } public string CanonicalSha256 { get; }
        public bool IsSuccess => Execution.CompletionStatus == TmpCompletionStatus.Succeeded;
        public int EmptySlotCount => OffsetTable.Count(e => e.IsEmptyCandidate);
    }

    internal enum TheaterKind { Temperate, Snow, Urban, NewUrban, Desert, Lunar }

    internal sealed class TheaterProfileDescriptor
    {
        internal TheaterProfileDescriptor(TheaterKind kind, string id, string extension, IEnumerable<string> controlNames,
            IEnumerable<string> fallbackExtensions, string variationPolicy, string isoPaletteRole)
        { Kind = kind; Id = id; PrimaryTmpExtension = extension; ControlIniLogicalNames = Array.AsReadOnly((controlNames ?? throw new ArgumentNullException(nameof(controlNames))).ToArray()); OptionalFallbackTmpExtensions = Array.AsReadOnly((fallbackExtensions ?? Enumerable.Empty<string>()).ToArray()); VariationPolicyId = variationPolicy; IsoPaletteLogicalRole = isoPaletteRole; }
        public TheaterKind Kind { get; } public string Id { get; } public string PrimaryTmpExtension { get; }
        public IReadOnlyList<string> ControlIniLogicalNames { get; } public IReadOnlyList<string> OptionalFallbackTmpExtensions { get; }
        public string VariationPolicyId { get; } public string IsoPaletteLogicalRole { get; }
    }

    internal static class TheaterProfiles
    {
        public static IReadOnlyList<TheaterProfileDescriptor> All { get; } = new[]
        {
            new TheaterProfileDescriptor(TheaterKind.Temperate, "Temperate", ".tem", new[]{"temperat.ini","temperatmd.ini"}, Array.Empty<string>(), "BaseAndAThroughF", "isotem.pal"),
            new TheaterProfileDescriptor(TheaterKind.Snow, "Snow", ".sno", new[]{"snow.ini","snowmd.ini"}, Array.Empty<string>(), "BaseAndAThroughF", "isosno.pal"),
            new TheaterProfileDescriptor(TheaterKind.Urban, "Urban", ".urb", new[]{"urban.ini","urbanmd.ini"}, Array.Empty<string>(), "BaseAndAThroughF", "isourb.pal"),
            new TheaterProfileDescriptor(TheaterKind.NewUrban, "NewUrban", ".ubn", new[]{"urbannmd.ini","newurban.ini"}, new[]{".urb"}, "BaseAndAThroughF", "isoubn.pal"),
            new TheaterProfileDescriptor(TheaterKind.Desert, "Desert", ".des", new[]{"desertmd.ini"}, Array.Empty<string>(), "BaseAndAThroughF", "isodes.pal"),
            new TheaterProfileDescriptor(TheaterKind.Lunar, "Lunar", ".lun", new[]{"lunarmd.ini"}, Array.Empty<string>(), "BaseAndAThroughF", "isolun.pal")
        };
        public static TheaterProfileDescriptor Get(TheaterKind kind) => All.First(p => p.Kind == kind);
    }

    internal sealed class TheaterValueView
    {
        internal TheaterValueView(string section, string key, string raw, int? integer, IniSourceProvenance provenance, int sectionLine, int keyLine, IEnumerable<IniResolvedValueCandidate> candidates)
        { Section = section; Key = key; Raw = raw; IntegerCandidate = integer; Provenance = provenance; SectionPhysicalLineId = sectionLine; KeyPhysicalLineId = keyLine; Candidates = Array.AsReadOnly((candidates ?? Enumerable.Empty<IniResolvedValueCandidate>()).ToArray()); }
        public string Section { get; } public string Key { get; } public string Raw { get; } public int? IntegerCandidate { get; }
        public IniSourceProvenance Provenance { get; } public int SectionPhysicalLineId { get; } public int KeyPhysicalLineId { get; }
        public IReadOnlyList<IniResolvedValueCandidate> Candidates { get; }
    }

    internal sealed class TheaterControlDocument
    {
        internal TheaterControlDocument(TheaterProfileDescriptor profile, TheaterValueView general, IEnumerable<TheaterValueView> values, IEnumerable<TmpDiagnostic> diagnostics, TmpExecutionState execution)
        { Profile = profile ?? throw new ArgumentNullException(nameof(profile)); General = general; Values = Array.AsReadOnly((values ?? throw new ArgumentNullException(nameof(values))).ToArray()); Diagnostics = Array.AsReadOnly((diagnostics ?? Enumerable.Empty<TmpDiagnostic>()).ToArray()); Execution = execution ?? new TmpExecutionState(); }
        public TheaterProfileDescriptor Profile { get; } public TheaterValueView General { get; }
        public IReadOnlyList<TheaterValueView> Values { get; } public IReadOnlyList<TmpDiagnostic> Diagnostics { get; }
        public TmpExecutionState Execution { get; } public bool IsSuccess => !Execution.HasFatalError;
        public IReadOnlyList<TheaterValueView> Find(string section, string key) => Values.Where(v => string.Equals(v.Section, section, StringComparison.OrdinalIgnoreCase) && string.Equals(v.Key, key, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    internal sealed class TheaterTileSetDescriptor
    {
        internal TheaterTileSetDescriptor(int index, string rawSection, string fileName, int? tilesInSet, TheaterValueView sectionProvenance, IEnumerable<TheaterValueView> values)
        { Index = index; RawSectionName = rawSection; FileNameRaw = fileName; TilesInSetRaw = tilesInSet; SectionProvenance = sectionProvenance; Values = Array.AsReadOnly((values ?? Enumerable.Empty<TheaterValueView>()).ToArray()); }
        public int Index { get; } public string RawSectionName { get; } public string FileNameRaw { get; }
        public int? TilesInSetRaw { get; } public TheaterValueView SectionProvenance { get; } public IReadOnlyList<TheaterValueView> Values { get; }
    }

    internal sealed class TheaterTileIdRange
    {
        internal TheaterTileIdRange(int tileSetIndex, long start, int count, long end, TheaterTileSetDescriptor descriptor)
        { TileSetIndex = tileSetIndex; StartInclusive = start; Count = count; EndExclusive = end; Descriptor = descriptor; }
        public int TileSetIndex { get; } public long StartInclusive { get; } public int Count { get; } public long EndExclusive { get; }
        public TheaterTileSetDescriptor Descriptor { get; }
    }

    internal sealed class TheaterSpecialRoleBinding
    {
        internal TheaterSpecialRoleBinding(string role, string raw, int? index, TheaterTileIdRange range) { Role = role; Raw = raw; Index = index; Range = range; }
        public string Role { get; } public string Raw { get; } public int? Index { get; } public TheaterTileIdRange Range { get; }
    }

    internal sealed class TheaterTileRegistry
    {
        internal TheaterTileRegistry(TheaterControlDocument document, IEnumerable<TheaterTileSetDescriptor> sets, IEnumerable<TheaterTileIdRange> ranges, IEnumerable<TheaterSpecialRoleBinding> roles, IEnumerable<TmpDiagnostic> diagnostics, TmpExecutionState execution, string hash)
        { Document = document; TileSets = Array.AsReadOnly((sets ?? throw new ArgumentNullException(nameof(sets))).ToArray()); IdRanges = Array.AsReadOnly((ranges ?? throw new ArgumentNullException(nameof(ranges))).ToArray()); SpecialRoles = Array.AsReadOnly((roles ?? throw new ArgumentNullException(nameof(roles))).ToArray()); Diagnostics = Array.AsReadOnly((diagnostics ?? Enumerable.Empty<TmpDiagnostic>()).ToArray()); Execution = execution ?? new TmpExecutionState(); CanonicalHash = hash ?? string.Empty; }
        public TheaterControlDocument Document { get; } public IReadOnlyList<TheaterTileSetDescriptor> TileSets { get; }
        public IReadOnlyList<TheaterTileIdRange> IdRanges { get; } public IReadOnlyList<TheaterSpecialRoleBinding> SpecialRoles { get; }
        public IReadOnlyList<TmpDiagnostic> Diagnostics { get; } public TmpExecutionState Execution { get; }
        public string CanonicalHash { get; } public bool IsSuccess => !Execution.HasFatalError;
        public TheaterTileIdRange FindRange(long globalTileId) => IdRanges.FirstOrDefault(r => globalTileId >= r.StartInclusive && globalTileId < r.EndExclusive);
        public bool TryResolveGlobalTileId(long globalTileId, out TheaterTileIdRange range, out int localOrdinal)
        { range = FindRange(globalTileId); if (range == null) { localOrdinal = -1; return false; } localOrdinal = checked((int)(globalTileId - range.StartInclusive)); return true; }
    }

    internal enum TmpVariationPolicy { BaseOnly, BaseAndAThroughF }
    internal enum TmpFallbackExtensionPolicy { Disabled, ExplicitNewUrbanEditorCandidate }

    internal sealed class TmpAssetResolutionPolicy
    {
        public TmpAssetResolutionPolicy(TmpVariationPolicy variation = TmpVariationPolicy.BaseAndAThroughF, TmpFallbackExtensionPolicy fallback = TmpFallbackExtensionPolicy.Disabled)
        { if (!Enum.IsDefined(typeof(TmpVariationPolicy), variation) || !Enum.IsDefined(typeof(TmpFallbackExtensionPolicy), fallback)) throw new ArgumentOutOfRangeException(); Variation = variation; Fallback = fallback; }
        public TmpVariationPolicy Variation { get; } public TmpFallbackExtensionPolicy Fallback { get; }
    }

    internal sealed class TmpAssetProviderCandidate
    {
        internal TmpAssetProviderCandidate(string logicalName, string providerId, bool exists, IniSourceProvenance provenance)
        { LogicalName = BinaryDiagnosticLabel.Validate(logicalName, nameof(logicalName)); ProviderId = BinaryDiagnosticLabel.Validate(providerId, nameof(providerId)); Exists = exists; Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance)); }
        public string LogicalName { get; } public string ProviderId { get; } public bool Exists { get; } public IniSourceProvenance Provenance { get; }
    }

    internal interface ITmpAssetProvider
    {
        IReadOnlyList<TmpAssetProviderCandidate> ResolveCandidates(string logicalName);
    }

    internal sealed class TmpAssetCandidate
    {
        internal TmpAssetCandidate(string logicalName, string extension, string variation, int tileSetIndex, int localOrdinal, long globalTileId, TmpAssetProviderCandidate provider)
        { LogicalName = logicalName; Extension = extension; Variation = variation; TileSetIndex = tileSetIndex; LocalOrdinal = localOrdinal; GlobalTileId = globalTileId; Provider = provider; }
        public string LogicalName { get; } public string Extension { get; } public string Variation { get; }
        public int TileSetIndex { get; } public int LocalOrdinal { get; } public long GlobalTileId { get; }
        public TmpAssetProviderCandidate Provider { get; } public bool IsPresent => Provider != null && Provider.Exists;
    }

    internal sealed class TmpAssetResolutionTrace
    {
        internal TmpAssetResolutionTrace(TheaterProfileDescriptor profile, int tileSetIndex, int localOrdinal, long globalTileId, IEnumerable<TmpAssetCandidate> candidates, TmpAssetCandidate selected, IEnumerable<TmpAssetCandidate> suppressed, IEnumerable<TmpDiagnostic> diagnostics, TmpExecutionState execution)
        { Profile = profile; TileSetIndex = tileSetIndex; LocalOrdinal = localOrdinal; GlobalTileId = globalTileId; Candidates = Array.AsReadOnly((candidates ?? Enumerable.Empty<TmpAssetCandidate>()).ToArray()); Selected = selected; Suppressed = Array.AsReadOnly((suppressed ?? Enumerable.Empty<TmpAssetCandidate>()).ToArray()); Diagnostics = Array.AsReadOnly((diagnostics ?? Enumerable.Empty<TmpDiagnostic>()).ToArray()); Execution = execution ?? new TmpExecutionState(); }
        public TheaterProfileDescriptor Profile { get; } public int TileSetIndex { get; } public int LocalOrdinal { get; } public long GlobalTileId { get; }
        public IReadOnlyList<TmpAssetCandidate> Candidates { get; } public TmpAssetCandidate Selected { get; } public IReadOnlyList<TmpAssetCandidate> Suppressed { get; }
        public IReadOnlyList<TmpDiagnostic> Diagnostics { get; } public TmpExecutionState Execution { get; }
        public bool IsSuccess => !Execution.HasFatalError;
    }
}
