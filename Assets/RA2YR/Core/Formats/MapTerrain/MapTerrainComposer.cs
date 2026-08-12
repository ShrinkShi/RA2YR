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
    internal sealed class MapTerrainComposer
    {
        public MapTerrainBindingResult Compose(
            IsoMapPack5PackedReadResult isoMap,
            OverlayPackedDocumentReadResult overlay,
            PreviewPackReadResult preview,
            TheaterProfileDescriptor theater,
            TheaterTileRegistry registry,
            ITmpAssetProvider assetProvider,
            Func<TmpAssetCandidate, TmpDocument> tmpLoader,
            MapTerrainBindingPolicy policy = null)
        {
            policy = policy ?? new MapTerrainBindingPolicy();
            var execution = new MapTerrainExecutionState();
            var diagnostics = new List<MapTerrainDiagnostic>();
            BinarySourceContext source = SyntheticSource();
            IniSourceProvenance provenance = SyntheticProvenance();
            if (isoMap == null || !isoMap.IsSuccess)
            {
                Add(diagnostics, execution, policy.Limits, Error(MapTerrainDiagnosticCode.MissingIsoMap, "isomap", "IsoMapPack5 is missing or incomplete.", source, provenance));
                return Build(policy, theater, isoMap, overlay, preview, registry, Array.Empty<MapTerrainCellBinding>(), diagnostics, execution);
            }
            if (theater == null)
            {
                Add(diagnostics, execution, policy.Limits, Error(MapTerrainDiagnosticCode.UnsupportedTheater, "theater", "Theater selection must be explicit.", source, provenance));
                return Build(policy, theater, isoMap, overlay, preview, registry, Array.Empty<MapTerrainCellBinding>(), diagnostics, execution);
            }
            if (registry == null || !registry.IsSuccess)
                AddIncomplete(diagnostics, execution, policy.Limits, Warning(MapTerrainDiagnosticCode.MissingTheaterRegistry, "registry", "Theater TileSet registry is missing or incomplete.", source, provenance));
            if (policy.RequireOverlay && (overlay == null || !overlay.IsSuccess))
                Add(diagnostics, execution, policy.Limits, Error(MapTerrainDiagnosticCode.MissingOverlay, "overlay", "Required Overlay raw arrays are missing or incomplete.", source, provenance));
            if (policy.RequirePreview && (preview == null || !preview.IsSuccess))
                Add(diagnostics, execution, policy.Limits, Error(MapTerrainDiagnosticCode.MissingPreview, "preview", "Required Preview document is missing or incomplete.", source, provenance));
            if (preview == null || !preview.IsSuccess)
                AddIncomplete(diagnostics, execution, policy.Limits, Warning(MapTerrainDiagnosticCode.MissingPreview, "preview", "Preview is optional and unavailable; terrain composition remains independent.", source, provenance));

            if (registry == null || !registry.IsSuccess)
                return Build(policy, theater, isoMap, overlay, preview, registry, Array.Empty<MapTerrainCellBinding>(), diagnostics, execution);

            var cells = new List<MapTerrainCellBinding>();
            OverlayRawIndexedView overlayView = overlay == null ? null : overlay.CreateIndexedView();
            foreach (IsoMapCoordinateOccurrence occurrence in isoMap.Coordinates.Index.Occurrences.OrderBy(x => x.SourceOrdinal))
            {
                if (cells.Count >= policy.Limits.MaxCells)
                {
                    Add(diagnostics, execution, policy.Limits, Error(MapTerrainDiagnosticCode.DiagnosticBudgetExceeded, "cells", "Map terrain cell budget exceeded.", source, provenance));
                    break;
                }
                IsoMapPack5RecordRaw record = occurrence.Record;
                long tileId;
                try { tileId = SelectTileId(record, policy.TileIdProfile); }
                catch (ArgumentOutOfRangeException)
                {
                    Add(diagnostics, execution, policy.Limits, Error(MapTerrainDiagnosticCode.InvalidIsoMapProfile, "isomap", "Unknown IsoMap tile-id interpretation profile.", source, record.Provenance));
                    continue;
                }
                TheaterTileIdRange range;
                int localOrdinal;
                if (!registry.TryResolveGlobalTileId(tileId, out range, out localOrdinal))
                {
                    AddIncomplete(diagnostics, execution, policy.Limits, Warning(MapTerrainDiagnosticCode.GlobalTileIdUnresolved, "registry", "Raw IsoMap tile candidate does not resolve to a TileSet range.", source, record.Provenance));
                    cells.Add(new MapTerrainCellBinding(record, tileId, null, null, null, null, null, Array.Empty<MapTerrainDiagnostic>()));
                    continue;
                }
                TmpAssetResolutionTrace asset = assetProvider == null ? null : TmpAssetResolver.Resolve(registry, theater, range.TileSetIndex, localOrdinal, assetProvider);
                TmpDocument document = asset == null || asset.Selected == null || tmpLoader == null ? null : tmpLoader(asset.Selected);
                var cellDiagnostics = new List<MapTerrainDiagnostic>();
                if (asset == null || asset.Selected == null)
                    AddIncomplete(cellDiagnostics, execution, policy.Limits, Warning(MapTerrainDiagnosticCode.MissingTmp, "tmp", "No explicit TMP asset candidate resolved.", source, record.Provenance));
                else if (document == null || !document.IsSuccess)
                    AddIncomplete(cellDiagnostics, execution, policy.Limits, Warning(MapTerrainDiagnosticCode.TmpParseFailed, "tmp", "Selected TMP candidate did not produce a complete raw document.", source, record.Provenance));
                int? subTile = record.SubTileRaw;
                bool slotInRange = document != null && subTile.GetValueOrDefault() < document.OffsetTable.Count;
                bool empty = slotInRange && document.OffsetTable[subTile.Value].IsEmptyCandidate;
                TmpCellRaw tmpCell = slotInRange ? document.Cells.FirstOrDefault(c => c.SlotOrdinal == subTile.Value) : null;
                if (document != null && !slotInRange)
                    Add(cellDiagnostics, execution, policy.Limits, Error(MapTerrainDiagnosticCode.SubTileOutOfRange, "subtile", "SubTile raw value is outside the TMP slot range.", source, record.Provenance));
                else if (empty)
                    Add(cellDiagnostics, execution, policy.Limits, Warning(MapTerrainDiagnosticCode.SubTileEmpty, "subtile", "SubTile points at an empty TMP slot candidate.", source, record.Provenance));
                OverlayRawCellPair pair = default(OverlayRawCellPair);
                MapTerrainOverlayRawBinding binding = null;
                if (overlayView != null)
                {
                    int index;
                    switch (policy.OverlayProfile)
                    {
                        case MapTerrainOverlayIndexProfile.ExternalRowMajorCandidate:
                            index = checked(record.XRawU16LittleEndian + OverlayStorageProfiles.StorageSideLength * record.YRawU16LittleEndian);
                            break;
                        case MapTerrainOverlayIndexProfile.OfficialEditorTransposedComparison:
                            index = checked(record.YRawU16LittleEndian + OverlayStorageProfiles.StorageSideLength * record.XRawU16LittleEndian);
                            break;
                        default:
                            Add(cellDiagnostics, execution, policy.Limits, Error(MapTerrainDiagnosticCode.InvalidOverlayProfile, "overlay", "Unknown Overlay index profile.", source, record.Provenance));
                            index = -1;
                            break;
                    }
                    if (overlayView.TryGetPairAtIndex(index, out pair)) binding = new MapTerrainOverlayRawBinding(index, pair.TypeRaw, pair.DataRaw, policy.OverlayProfile);
                }
                byte? height = tmpCell == null ? (byte?)null : tmpCell.Header.HeightRaw;
                byte? ramp = tmpCell == null ? (byte?)null : tmpCell.Header.RampTypeRaw;
                byte? terrainRaw = tmpCell == null ? (byte?)null : tmpCell.Header.TerrainTypeRaw;
                if (ramp.HasValue && policy.RampProfile == MapTerrainRampProfile.Candidate0Through20 && ramp.Value > 20)
                    Add(cellDiagnostics, execution, policy.Limits, Warning(MapTerrainDiagnosticCode.UnknownRampCandidate, "ramp", "Ramp raw value is outside the explicit candidate profile.", source, record.Provenance));
                if (terrainRaw.HasValue && policy.TerrainProfile == MapTerrainTypeProfile.CandidateConfigured && terrainRaw.Value > 31)
                    Add(cellDiagnostics, execution, policy.Limits, Warning(MapTerrainDiagnosticCode.UnknownTerrainCandidate, "terrain", "Terrain raw value is outside the configured candidate profile.", source, record.Provenance));
                cells.Add(new MapTerrainCellBinding(record, tileId, new MapTerrainResourceBinding(tileId, range.TileSetIndex, localOrdinal, asset, document, subTile, empty), binding, height, ramp, terrainRaw, cellDiagnostics));
            }
            if (overlay == null || !overlay.IsSuccess) AddIncomplete(diagnostics, execution, policy.Limits, Warning(MapTerrainDiagnosticCode.MissingOverlay, "overlay", "Overlay raw binding is unavailable; no automatic profile was selected.", source, provenance));
            if (registry != null && registry.IsSuccess) execution.Run();
            return Build(policy, theater, isoMap, overlay, preview, registry, cells, diagnostics, execution);
        }

        private static long SelectTileId(IsoMapPack5RecordRaw record, MapTerrainIsoTileIdProfile profile)
        {
            switch (profile)
            {
                case MapTerrainIsoTileIdProfile.RawU32: return record.TileRawU32LittleEndian;
                case MapTerrainIsoTileIdProfile.LowU16: return record.TileLowU16LittleEndian;
                case MapTerrainIsoTileIdProfile.HighU16: return record.TileHighU16LittleEndian;
                default: throw new ArgumentOutOfRangeException(nameof(profile));
            }
        }

        private static MapTerrainBindingResult Build(MapTerrainBindingPolicy policy, TheaterProfileDescriptor theater, IsoMapPack5PackedReadResult isoMap, OverlayPackedDocumentReadResult overlay, PreviewPackReadResult preview, TheaterTileRegistry registry, IEnumerable<MapTerrainCellBinding> cells, IEnumerable<MapTerrainDiagnostic> diagnostics, MapTerrainExecutionState execution)
        {
            var all = diagnostics.ToList();
            string hash = Hash(cells);
            var document = new MapTerrainDocument(policy, theater, isoMap, overlay, preview, registry, cells, all, execution, hash);
            return new MapTerrainBindingResult(document, all, execution);
        }

        private static string Hash(IEnumerable<MapTerrainCellBinding> cells)
        {
            var b = new StringBuilder();
            foreach (MapTerrainCellBinding cell in cells.OrderBy(c => c.Record.SourceOrdinal)) b.Append(cell.Record.SourceOrdinal).Append(':').Append(cell.GlobalTileId).Append(':').Append(cell.Record.SubTileRaw).Append(':').Append(cell.IsoLevelRaw).Append(':').Append(cell.TmpHeightRaw).Append(':').Append(cell.TmpRampTypeRaw).Append(':').Append(cell.TmpTerrainTypeRaw).Append('|');
            using (SHA256 sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(b.ToString()))).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static void Add(IList<MapTerrainDiagnostic> list, MapTerrainExecutionState execution, MapTerrainReadLimits limits, MapTerrainDiagnostic diagnostic)
        { execution.Observe(diagnostic.Severity); if (list.Count < limits.MaxDiagnostics) list.Add(diagnostic); else execution.Suppress(); }
        private static void AddIncomplete(IList<MapTerrainDiagnostic> list, MapTerrainExecutionState execution, MapTerrainReadLimits limits, MapTerrainDiagnostic diagnostic)
        { execution.Incomplete(); if (list.Count < limits.MaxDiagnostics) list.Add(diagnostic); else execution.Suppress(); }
        private static MapTerrainDiagnostic Error(MapTerrainDiagnosticCode code, string stage, string message, BinarySourceContext source, IEnumerable<IniSourceProvenance> provenance) => new MapTerrainDiagnostic(BinaryDiagnosticSeverity.Error, code, stage, message, source, provenance);
        private static MapTerrainDiagnostic Warning(MapTerrainDiagnosticCode code, string stage, string message, BinarySourceContext source, IEnumerable<IniSourceProvenance> provenance) => new MapTerrainDiagnostic(BinaryDiagnosticSeverity.Warning, code, stage, message, source, provenance);
        private static MapTerrainDiagnostic Error(MapTerrainDiagnosticCode code, string stage, string message, BinarySourceContext source, IniSourceProvenance provenance) => Error(code, stage, message, source, new[] { provenance });
        private static MapTerrainDiagnostic Warning(MapTerrainDiagnosticCode code, string stage, string message, BinarySourceContext source, IniSourceProvenance provenance) => Warning(code, stage, message, source, new[] { provenance });
        private static BinarySourceContext SyntheticSource() => new BinarySourceContext("map-terrain-composer", "map-terrain", LogicalContentPath.Parse("map-terrain"));
        private static IniSourceProvenance SyntheticProvenance() => new IniSourceProvenance("map-terrain", new[] { LogicalContentPath.Parse("map-terrain") });
    }
}
