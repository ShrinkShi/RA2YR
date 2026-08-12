using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;
using RA2YR.Core.Formats.PackedMap;
using RA2YR.Core.Formats.Tmp;

namespace RA2YR.Core.Formats.MapTerrain
{
    internal enum MapTerrainDiagnosticCode
    {
        MissingIsoMap,
        MissingOverlay,
        MissingOverlayPartner,
        MissingPreview,
        MissingTheaterRegistry,
        MissingTmp,
        AmbiguousTheater,
        UnsupportedTheater,
        GlobalTileIdUnresolved,
        SubTileOutOfRange,
        SubTileEmpty,
        TmpParseFailed,
        InvalidIsoMapProfile,
        InvalidOverlayProfile,
        UnknownRampCandidate,
        UnknownTerrainCandidate,
        PaletteCandidateMissing,
        LatIncomplete,
        LatCycle,
        DiagnosticBudgetExceeded,
        ArithmeticOverflow
    }

    internal enum MapTerrainCompletionStatus { NotRun, Succeeded, Incomplete, Failed }
    internal enum MapTerrainAuthorityStatus { StructuralOnly, CandidateBindings, NotAuthoritative }
    internal enum MapTerrainOverlayIndexProfile { ExternalRowMajorCandidate, OfficialEditorTransposedComparison }
    internal enum MapTerrainIsoTileIdProfile { RawU32, LowU16, HighU16 }
    internal enum MapTerrainRampProfile { RawOnly, Candidate0Through20 }
    internal enum MapTerrainTypeProfile { RawOnly, CandidateConfigured }
    internal enum MapTerrainLatStatus { Complete, Missing, Ambiguous, Cycle }
    internal enum MapTerrainPaletteStatus { Selected, Missing, Ambiguous, Suppressed }

    internal sealed class MapTerrainExecutionState
    {
        private bool ran;
        private bool failed;
        private bool incomplete;
        private BinaryDiagnosticSeverity highest = BinaryDiagnosticSeverity.Info;
        private int suppressed;
        public MapTerrainCompletionStatus CompletionStatus => !ran ? MapTerrainCompletionStatus.NotRun : failed ? MapTerrainCompletionStatus.Failed : incomplete ? MapTerrainCompletionStatus.Incomplete : MapTerrainCompletionStatus.Succeeded;
        public bool HasFatalError => failed;
        public BinaryDiagnosticSeverity HighestObservedSeverity => highest;
        public int SuppressedDiagnosticCount => suppressed;
        internal void Run() { ran = true; }
        internal void Fail() { Run(); failed = true; highest = (BinaryDiagnosticSeverity)Math.Max((int)highest, (int)BinaryDiagnosticSeverity.Error); }
        internal void Incomplete() { Run(); incomplete = true; highest = (BinaryDiagnosticSeverity)Math.Max((int)highest, (int)BinaryDiagnosticSeverity.Warning); }
        internal void Observe(BinaryDiagnosticSeverity severity) { Run(); highest = (BinaryDiagnosticSeverity)Math.Max((int)highest, (int)severity); if (severity == BinaryDiagnosticSeverity.Error) failed = true; }
        internal void Merge(MapTerrainExecutionState other) { if (other == null) return; if (other.ran) Run(); if (other.failed) Fail(); else if (other.incomplete) Incomplete(); Observe(other.highest); AddSuppressed(other.suppressed); }
        internal void Suppress() { if (suppressed < int.MaxValue) suppressed++; }
        private void AddSuppressed(int count) { if (count <= 0) return; long value = (long)suppressed + count; suppressed = value >= int.MaxValue ? int.MaxValue : (int)value; }
    }

    internal sealed class MapTerrainDiagnostic
    {
        internal MapTerrainDiagnostic(BinaryDiagnosticSeverity severity, MapTerrainDiagnosticCode code, string stage, string message, BinarySourceContext source, IEnumerable<IniSourceProvenance> provenance)
        { Severity = severity; Code = code; Stage = BinaryDiagnosticLabel.Validate(stage, nameof(stage)); Message = message ?? throw new ArgumentNullException(nameof(message)); Source = source ?? throw new ArgumentNullException(nameof(source)); var p = (provenance ?? throw new ArgumentNullException(nameof(provenance))).ToArray(); if (p.Length == 0 || p.Any(x => x == null)) throw new ArgumentException("Provenance is required.", nameof(provenance)); Provenance = Array.AsReadOnly(p); }
        public BinaryDiagnosticSeverity Severity { get; }
        public MapTerrainDiagnosticCode Code { get; }
        public string Stage { get; }
        public string Message { get; }
        public BinarySourceContext Source { get; }
        public IReadOnlyList<IniSourceProvenance> Provenance { get; }
    }

    internal sealed class MapTerrainReadLimits
    {
        public MapTerrainReadLimits(int maxCells = 1_000_000, int maxDiagnostics = 4096, int maxLatEdges = 100_000)
        { if (maxCells < 0 || maxDiagnostics < 0 || maxLatEdges < 0) throw new ArgumentOutOfRangeException(); MaxCells = maxCells; MaxDiagnostics = maxDiagnostics; MaxLatEdges = maxLatEdges; }
        public int MaxCells { get; }
        public int MaxDiagnostics { get; }
        public int MaxLatEdges { get; }
    }

    internal sealed class MapTerrainBindingPolicy
    {
        public MapTerrainBindingPolicy(MapTerrainIsoTileIdProfile tileIdProfile = MapTerrainIsoTileIdProfile.RawU32,
            MapTerrainOverlayIndexProfile overlayProfile = MapTerrainOverlayIndexProfile.ExternalRowMajorCandidate,
            MapTerrainRampProfile rampProfile = MapTerrainRampProfile.RawOnly,
            MapTerrainTypeProfile terrainProfile = MapTerrainTypeProfile.RawOnly,
            bool requireOverlay = false, bool requirePreview = false, MapTerrainReadLimits limits = null)
        { if (!Enum.IsDefined(typeof(MapTerrainIsoTileIdProfile), tileIdProfile) || !Enum.IsDefined(typeof(MapTerrainOverlayIndexProfile), overlayProfile) || !Enum.IsDefined(typeof(MapTerrainRampProfile), rampProfile) || !Enum.IsDefined(typeof(MapTerrainTypeProfile), terrainProfile)) throw new ArgumentOutOfRangeException(); TileIdProfile = tileIdProfile; OverlayProfile = overlayProfile; RampProfile = rampProfile; TerrainProfile = terrainProfile; RequireOverlay = requireOverlay; RequirePreview = requirePreview; Limits = limits ?? new MapTerrainReadLimits(); }
        public MapTerrainIsoTileIdProfile TileIdProfile { get; }
        public MapTerrainOverlayIndexProfile OverlayProfile { get; }
        public MapTerrainRampProfile RampProfile { get; }
        public MapTerrainTypeProfile TerrainProfile { get; }
        public bool RequireOverlay { get; }
        public bool RequirePreview { get; }
        public MapTerrainReadLimits Limits { get; }
    }

    internal sealed class MapTerrainOverlayRawBinding
    {
        internal MapTerrainOverlayRawBinding(int index, byte? typeRaw, byte? dataRaw, MapTerrainOverlayIndexProfile profile) { Index = index; TypeRaw = typeRaw; DataRaw = dataRaw; Profile = profile; }
        public int Index { get; }
        public byte? TypeRaw { get; }
        public byte? DataRaw { get; }
        public MapTerrainOverlayIndexProfile Profile { get; }
        public bool IsComplete => TypeRaw.HasValue && DataRaw.HasValue;
    }

    internal sealed class MapTerrainResourceBinding
    {
        internal MapTerrainResourceBinding(long globalTileId, int tileSetIndex, int localOrdinal, TmpAssetResolutionTrace asset, TmpDocument document, int? subTile, bool empty)
        { GlobalTileId = globalTileId; TileSetIndex = tileSetIndex; LocalOrdinal = localOrdinal; Asset = asset; Document = document; SubTileRaw = subTile; IsEmptySubTile = empty; }
        public long GlobalTileId { get; }
        public int TileSetIndex { get; }
        public int LocalOrdinal { get; }
        public TmpAssetResolutionTrace Asset { get; }
        public TmpDocument Document { get; }
        public int? SubTileRaw { get; }
        public bool IsEmptySubTile { get; }
        public bool IsResolved => Asset != null && Asset.Selected != null && Document != null && Document.IsSuccess;
    }

    internal sealed class MapTerrainLatCandidateGraph
    {
        internal MapTerrainLatCandidateGraph(IEnumerable<string> sourceTileSets, IEnumerable<string> transitionTileSets, IEnumerable<string> baseTileSets, MapTerrainLatStatus status, IEnumerable<MapTerrainDiagnostic> diagnostics)
        { SourceTileSets = Array.AsReadOnly((sourceTileSets ?? Enumerable.Empty<string>()).OrderBy(x => x, StringComparer.Ordinal).ToArray()); TransitionTileSets = Array.AsReadOnly((transitionTileSets ?? Enumerable.Empty<string>()).OrderBy(x => x, StringComparer.Ordinal).ToArray()); BaseTileSets = Array.AsReadOnly((baseTileSets ?? Enumerable.Empty<string>()).OrderBy(x => x, StringComparer.Ordinal).ToArray()); Status = status; Diagnostics = Array.AsReadOnly((diagnostics ?? Enumerable.Empty<MapTerrainDiagnostic>()).ToArray()); }
        public IReadOnlyList<string> SourceTileSets { get; }
        public IReadOnlyList<string> TransitionTileSets { get; }
        public IReadOnlyList<string> BaseTileSets { get; }
        public MapTerrainLatStatus Status { get; }
        public IReadOnlyList<MapTerrainDiagnostic> Diagnostics { get; }
    }

    internal sealed class MapTerrainPaletteBinding
    {
        internal MapTerrainPaletteBinding(string role, string logicalCandidate, string providerId, MapTerrainPaletteStatus status, IEnumerable<string> suppressedCandidates, IEnumerable<IniSourceProvenance> provenance)
        { Role = role ?? throw new ArgumentNullException(nameof(role)); LogicalCandidate = logicalCandidate; ProviderId = providerId; Status = status; SuppressedCandidates = Array.AsReadOnly((suppressedCandidates ?? Enumerable.Empty<string>()).OrderBy(x => x, StringComparer.Ordinal).ToArray()); var p = (provenance ?? throw new ArgumentNullException(nameof(provenance))).ToArray(); if (p.Length == 0 || p.Any(x => x == null)) throw new ArgumentException("Palette provenance is required.", nameof(provenance)); Provenance = Array.AsReadOnly(p); }
        public string Role { get; }
        public string LogicalCandidate { get; }
        public string ProviderId { get; }
        public MapTerrainPaletteStatus Status { get; }
        public IReadOnlyList<string> SuppressedCandidates { get; }
        public IReadOnlyList<IniSourceProvenance> Provenance { get; }
    }

    internal sealed class MapTerrainCellBinding
    {
        internal MapTerrainCellBinding(IsoMapPack5RecordRaw record, long? globalTileId, MapTerrainResourceBinding resource, MapTerrainOverlayRawBinding overlay, byte? heightRaw, byte? rampRaw, byte? terrainRaw, IEnumerable<MapTerrainDiagnostic> diagnostics)
        { Record = record ?? throw new ArgumentNullException(nameof(record)); GlobalTileId = globalTileId; Resource = resource; Overlay = overlay; IsoLevelRaw = record.LevelRaw; TmpHeightRaw = heightRaw; TmpRampTypeRaw = rampRaw; TmpTerrainTypeRaw = terrainRaw; Diagnostics = Array.AsReadOnly((diagnostics ?? Enumerable.Empty<MapTerrainDiagnostic>()).ToArray()); }
        public IsoMapPack5RecordRaw Record { get; }
        public long? GlobalTileId { get; }
        public MapTerrainResourceBinding Resource { get; }
        public MapTerrainOverlayRawBinding Overlay { get; }
        public byte IsoLevelRaw { get; }
        public byte? TmpHeightRaw { get; }
        public byte? TmpRampTypeRaw { get; }
        public byte? TmpTerrainTypeRaw { get; }
        public IReadOnlyList<MapTerrainDiagnostic> Diagnostics { get; }
        public bool IsFullyBound => Resource != null && Resource.IsResolved;
    }

    internal sealed class MapTerrainDocument
    {
        internal MapTerrainDocument(MapTerrainBindingPolicy policy, TheaterProfileDescriptor theater, IsoMapPack5PackedReadResult isoMap, OverlayPackedDocumentReadResult overlay, PreviewPackReadResult preview, TheaterTileRegistry registry, IEnumerable<MapTerrainCellBinding> cells, IEnumerable<MapTerrainDiagnostic> diagnostics, MapTerrainExecutionState execution, string hash, MapTerrainLatCandidateGraph lat = null, MapTerrainPaletteBinding palette = null)
        { Policy = policy ?? throw new ArgumentNullException(nameof(policy)); Theater = theater; IsoMap = isoMap; Overlay = overlay; Preview = preview; Registry = registry; Cells = Array.AsReadOnly((cells ?? Enumerable.Empty<MapTerrainCellBinding>()).OrderBy(c => c.Record.SourceOrdinal).ToArray()); Diagnostics = Array.AsReadOnly((diagnostics ?? Enumerable.Empty<MapTerrainDiagnostic>()).ToArray()); Execution = execution ?? throw new ArgumentNullException(nameof(execution)); CanonicalHash = hash ?? string.Empty; Lat = lat; Palette = palette; }
        public MapTerrainBindingPolicy Policy { get; }
        public TheaterProfileDescriptor Theater { get; }
        public IsoMapPack5PackedReadResult IsoMap { get; }
        public OverlayPackedDocumentReadResult Overlay { get; }
        public PreviewPackReadResult Preview { get; }
        public TheaterTileRegistry Registry { get; }
        public IReadOnlyList<MapTerrainCellBinding> Cells { get; }
        public IReadOnlyList<MapTerrainDiagnostic> Diagnostics { get; }
        public MapTerrainExecutionState Execution { get; }
        public MapTerrainCompletionStatus CompletionStatus => Execution.CompletionStatus;
        public MapTerrainAuthorityStatus AuthorityStatus => CompletionStatus == MapTerrainCompletionStatus.Succeeded ? MapTerrainAuthorityStatus.CandidateBindings : MapTerrainAuthorityStatus.StructuralOnly;
        public bool IsFullyBound => CompletionStatus == MapTerrainCompletionStatus.Succeeded && Cells.All(c => c.IsFullyBound);
        public string CanonicalHash { get; }
        public MapTerrainLatCandidateGraph Lat { get; }
        public MapTerrainPaletteBinding Palette { get; }
    }

    internal sealed class MapTerrainBindingResult
    {
        internal MapTerrainBindingResult(MapTerrainDocument document, IEnumerable<MapTerrainDiagnostic> diagnostics, MapTerrainExecutionState execution)
        { Document = document; Diagnostics = Array.AsReadOnly((diagnostics ?? Enumerable.Empty<MapTerrainDiagnostic>()).ToArray()); Execution = execution ?? throw new ArgumentNullException(nameof(execution)); }
        public MapTerrainDocument Document { get; }
        public IReadOnlyList<MapTerrainDiagnostic> Diagnostics { get; }
        public MapTerrainExecutionState Execution { get; }
        public MapTerrainCompletionStatus CompletionStatus => Execution.CompletionStatus;
        public bool IsSuccess => CompletionStatus == MapTerrainCompletionStatus.Succeeded;
    }
}
