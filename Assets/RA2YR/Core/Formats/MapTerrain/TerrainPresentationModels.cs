using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Formats.VxlHva;

namespace RA2YR.Core.Formats.MapTerrain
{
    public enum TerrainPresentationDiagnosticCode
    {
        InvalidProfile, ArithmeticOverflow, ProjectionUnavailable, CellBudgetExceeded,
        MissingPalette, UnresolvedTmp, UnsupportedTile, NoProgress
    }

    public sealed class TerrainPresentationDiagnostic
    {
        public TerrainPresentationDiagnostic(TerrainPresentationDiagnosticCode code, string stage, string message, int ordinal = -1)
        { Code = code; Stage = stage ?? throw new ArgumentNullException(nameof(stage)); Message = message ?? throw new ArgumentNullException(nameof(message)); Ordinal = ordinal; }
        public TerrainPresentationDiagnosticCode Code { get; }
        public string Stage { get; }
        public string Message { get; }
        public int Ordinal { get; }
    }

    public enum TerrainPresentationCompletionStatus { Succeeded, Failed }

    public sealed class TerrainPresentationExecutionState
    {
        internal TerrainPresentationExecutionState(TerrainPresentationCompletionStatus status, int suppressed)
        { CompletionStatus = status; SuppressedDiagnosticCount = suppressed; }
        public TerrainPresentationCompletionStatus CompletionStatus { get; }
        public int SuppressedDiagnosticCount { get; }
        public bool IsSuccess => CompletionStatus == TerrainPresentationCompletionStatus.Succeeded;
    }

    public enum TerrainProjectionRounding { ExactHalfAwayFromZero, Floor, Ceiling }
    public enum TerrainProjectionAxisOrder { XMinusY, YMinusX }

    public sealed class IsometricProjectionProfile
    {
        public IsometricProjectionProfile(
            long originX,
            long originY,
            long tileWidth,
            long tileHeight,
            long heightStep,
            TerrainProjectionAxisOrder axisOrder = TerrainProjectionAxisOrder.XMinusY,
            TerrainProjectionRounding rounding = TerrainProjectionRounding.ExactHalfAwayFromZero)
        {
            if (tileWidth <= 0 || tileHeight <= 0 || heightStep < 0) throw new ArgumentOutOfRangeException();
            if (!Enum.IsDefined(typeof(TerrainProjectionAxisOrder), axisOrder) || !Enum.IsDefined(typeof(TerrainProjectionRounding), rounding)) throw new ArgumentOutOfRangeException();
            OriginX = originX; OriginY = originY; TileWidth = tileWidth; TileHeight = tileHeight; HeightStep = heightStep; AxisOrder = axisOrder; Rounding = rounding;
        }
        public long OriginX { get; }
        public long OriginY { get; }
        public long TileWidth { get; }
        public long TileHeight { get; }
        public long HeightStep { get; }
        public TerrainProjectionAxisOrder AxisOrder { get; }
        public TerrainProjectionRounding Rounding { get; }

        /// <summary>
        /// Projects into an exact doubled-unit space. A tile with an odd
        /// TileHeight therefore keeps its half-unit center instead of being
        /// rounded before geometry is built.
        /// </summary>
        public IsometricFixedPoint ProjectFixed(long x, long y, long level, long heightRaw)
        {
            long first = AxisOrder == TerrainProjectionAxisOrder.XMinusY ? checked(x - y) : checked(y - x);
            long second = checked(x + y);
            long vertical = checked(second * TileHeight - checked((level + heightRaw) * HeightStep));
            return new IsometricFixedPoint(
                checked(OriginX * IsometricFixedPoint.UnitsPerLogicalUnit + checked(first * TileWidth)),
                checked(OriginY * IsometricFixedPoint.UnitsPerLogicalUnit + vertical));
        }

        public IsometricScreenPoint Project(long x, long y, long level, long heightRaw)
        {
            long first = AxisOrder == TerrainProjectionAxisOrder.XMinusY ? checked(x - y) : checked(y - x);
            long second = checked(x + y);
            long screenX = checked(OriginX + Divide(checked(first * TileWidth), 2));
            long vertical = checked(second * TileHeight - checked((level + heightRaw) * HeightStep));
            long screenY = checked(OriginY + Divide(vertical, 2));
            return new IsometricScreenPoint(screenX, screenY);
        }
        public bool TryInverse(IsometricScreenPoint point, long level, long heightRaw, out IsometricGridPoint candidate)
        {
            candidate = default(IsometricGridPoint);
            long centeredX = checked((point.X - OriginX) * 2 / TileWidth);
            long centeredY = checked((point.Y - OriginY + checked((level + heightRaw) * HeightStep)) * 2 / TileHeight);
            long first = checked((centeredX + centeredY) / 2);
            long second = checked(centeredY - first);
            if (AxisOrder == TerrainProjectionAxisOrder.XMinusY) candidate = new IsometricGridPoint(first, second);
            else candidate = new IsometricGridPoint(second, first);
            return Project(candidate.X, candidate.Y, level, heightRaw).Equals(point);
        }

        /// <summary>
        /// Returns the nearest grid coordinate for a presentation-space
        /// position. This is intentionally separate from TryInverse, which
        /// remains an exact integer-point inverse contract.
        /// </summary>
        public bool TryInverseNearest(double screenX, double screenY, long level, long heightRaw, out IsometricGridPoint candidate)
        {
            candidate = default(IsometricGridPoint);
            if (double.IsNaN(screenX) || double.IsInfinity(screenX) || double.IsNaN(screenY) || double.IsInfinity(screenY))
                return false;
            double fixedX = screenX * IsometricFixedPoint.UnitsPerLogicalUnit;
            double fixedY = screenY * IsometricFixedPoint.UnitsPerLogicalUnit;
            if (fixedX < long.MinValue || fixedX > long.MaxValue || fixedY < long.MinValue || fixedY > long.MaxValue)
                return false;
            long centeredX = checked((long)Math.Round(fixedX, MidpointRounding.AwayFromZero)) - checked(OriginX * IsometricFixedPoint.UnitsPerLogicalUnit);
            long centeredY = checked((long)Math.Round(fixedY, MidpointRounding.AwayFromZero)) - checked(OriginY * IsometricFixedPoint.UnitsPerLogicalUnit);
            centeredY = checked(centeredY + checked((level + heightRaw) * HeightStep));
            long first = NearestDivide(centeredX, TileWidth);
            long second = NearestDivide(centeredY, TileHeight);
            if (AxisOrder == TerrainProjectionAxisOrder.XMinusY)
                candidate = new IsometricGridPoint(checked((first + second) / 2), checked((second - first) / 2));
            else
                candidate = new IsometricGridPoint(checked((second - first) / 2), checked((first + second) / 2));
            return true;
        }

        private static long NearestDivide(long value, long divisor)
        {
            if (value % divisor == 0) return value / divisor;
            return value >= 0 ? checked((value + divisor / 2) / divisor) : checked((value - divisor / 2) / divisor);
        }
        private long Divide(long value, long divisor)
        {
            if (value % divisor == 0) return value / divisor;
            switch (Rounding)
            {
                case TerrainProjectionRounding.Floor: return (long)Math.Floor((double)value / divisor);
                case TerrainProjectionRounding.Ceiling: return (long)Math.Ceiling((double)value / divisor);
                default: return value >= 0 ? checked((value + divisor / 2) / divisor) : checked((value - divisor / 2) / divisor);
            }
        }
    }

    public readonly struct IsometricScreenPoint : IEquatable<IsometricScreenPoint>
    {
        public IsometricScreenPoint(long x, long y) { X = x; Y = y; }
        public long X { get; } public long Y { get; }
        public bool Equals(IsometricScreenPoint other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is IsometricScreenPoint other && Equals(other);
        public override int GetHashCode() => X.GetHashCode() ^ (Y.GetHashCode() * 397);
    }

    public readonly struct IsometricFixedPoint : IEquatable<IsometricFixedPoint>
    {
        public const long UnitsPerLogicalUnit = 2;

        public IsometricFixedPoint(long x, long y) { X = x; Y = y; }
        public long X { get; }
        public long Y { get; }
        public double LogicalX => (double)X / UnitsPerLogicalUnit;
        public double LogicalY => (double)Y / UnitsPerLogicalUnit;
        public bool Equals(IsometricFixedPoint other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is IsometricFixedPoint other && Equals(other);
        public override int GetHashCode() => X.GetHashCode() ^ (Y.GetHashCode() * 397);
    }

    public readonly struct IsometricGridPoint
    {
        public IsometricGridPoint(long x, long y) { X = x; Y = y; }
        public long X { get; } public long Y { get; }
    }

    public sealed class TerrainTilePresentationDescriptor
    {
        public TerrainTilePresentationDescriptor(
            long gridX,
            long gridY,
            long tileLogicalIdentity,
            byte subTileRaw,
            byte levelRaw,
            byte? tmpHeightRaw,
            byte? tmpRampRaw,
            byte? tmpTerrainRaw,
            int tileSetIndex,
            int localTileOrdinal,
            PaletteBindingDescriptor? palette,
            long sourceOrdinal)
        {
            if (gridX < 0 || gridY < 0 || sourceOrdinal < 0) throw new ArgumentOutOfRangeException();
            if (tileSetIndex < 0 || localTileOrdinal < 0) throw new ArgumentOutOfRangeException();
            GridX = gridX; GridY = gridY; TileLogicalIdentity = tileLogicalIdentity; SubTileRaw = subTileRaw; LevelRaw = levelRaw; TmpHeightRaw = tmpHeightRaw; TmpRampRaw = tmpRampRaw; TmpTerrainRaw = tmpTerrainRaw; TileSetIndex = tileSetIndex; LocalTileOrdinal = localTileOrdinal; Palette = palette; SourceOrdinal = sourceOrdinal;
        }
        public TerrainTilePresentationDescriptor(
            long tileLogicalIdentity,
            byte subTileRaw,
            byte levelRaw,
            byte? tmpHeightRaw,
            byte? tmpRampRaw,
            byte? tmpTerrainRaw,
            int tileSetIndex,
            int localTileOrdinal,
            PaletteBindingDescriptor? palette,
            long sourceOrdinal)
            : this(sourceOrdinal, 0, tileLogicalIdentity, subTileRaw, levelRaw, tmpHeightRaw, tmpRampRaw, tmpTerrainRaw, tileSetIndex, localTileOrdinal, palette, sourceOrdinal) { }
        public long GridX { get; }
        public long GridY { get; }
        public long TileLogicalIdentity { get; }
        public byte SubTileRaw { get; }
        public byte LevelRaw { get; }
        public byte? TmpHeightRaw { get; }
        public byte? TmpRampRaw { get; }
        public byte? TmpTerrainRaw { get; }
        public int TileSetIndex { get; }
        public int LocalTileOrdinal { get; }
        public PaletteBindingDescriptor? Palette { get; }
        public long SourceOrdinal { get; }
        public bool IsPaletteBound => Palette.HasValue && Palette.Value.ConversionProfile != PaletteConversionProfile.Unresolved;
    }

    public sealed class TerrainChunkDescriptor
    {
        internal TerrainChunkDescriptor(int chunkX, int chunkY, int width, int height, IEnumerable<TerrainTilePresentationDescriptor> cells)
        { ChunkX = chunkX; ChunkY = chunkY; Width = width; Height = height; Cells = Array.AsReadOnly((cells ?? Enumerable.Empty<TerrainTilePresentationDescriptor>()).OrderBy(c => c.SourceOrdinal).ToArray()); }
        public int ChunkX { get; } public int ChunkY { get; } public int Width { get; } public int Height { get; }
        public IReadOnlyList<TerrainTilePresentationDescriptor> Cells { get; }
        public string StableIdentity => ChunkX + ":" + ChunkY + ":" + Width + ":" + Height;
    }

    public sealed class TerrainPresentationPolicy
    {
        public TerrainPresentationPolicy(int chunkWidth = 16, int chunkHeight = 16, int maxCells = 1_000_000, int maxDiagnostics = 256)
        { if (chunkWidth <= 0 || chunkHeight <= 0 || maxCells < 0 || maxDiagnostics < 0) throw new ArgumentOutOfRangeException(); ChunkWidth = chunkWidth; ChunkHeight = chunkHeight; MaxCells = maxCells; MaxDiagnostics = maxDiagnostics; }
        public int ChunkWidth { get; } public int ChunkHeight { get; } public int MaxCells { get; } public int MaxDiagnostics { get; }
    }

    public sealed class TerrainPresentationBuildResult
    {
        internal TerrainPresentationBuildResult(IEnumerable<TerrainChunkDescriptor> chunks, IEnumerable<TerrainPresentationDiagnostic> diagnostics, TerrainPresentationExecutionState execution)
        { Chunks = Array.AsReadOnly((chunks ?? Enumerable.Empty<TerrainChunkDescriptor>()).ToArray()); Diagnostics = Array.AsReadOnly((diagnostics ?? Enumerable.Empty<TerrainPresentationDiagnostic>()).ToArray()); Execution = execution; }
        public IReadOnlyList<TerrainChunkDescriptor> Chunks { get; }
        public IReadOnlyList<TerrainPresentationDiagnostic> Diagnostics { get; }
        public TerrainPresentationExecutionState Execution { get; }
        public bool IsSuccess => Execution.IsSuccess;
    }

    public static class TerrainPresentationComposer
    {
        public static TerrainPresentationBuildResult Build(IEnumerable<TerrainTilePresentationDescriptor> source, TerrainPresentationPolicy policy = null)
        {
            policy = policy ?? new TerrainPresentationPolicy();
            var diagnostics = new List<TerrainPresentationDiagnostic>(); bool failed = false; int suppressed = 0; int count = 0;
            if (source == null) { diagnostics.Add(new TerrainPresentationDiagnostic(TerrainPresentationDiagnosticCode.InvalidProfile, "source", "Terrain source is required.")); return new TerrainPresentationBuildResult(null, diagnostics, new TerrainPresentationExecutionState(TerrainPresentationCompletionStatus.Failed, 0)); }
            var grouped = new Dictionary<Tuple<int, int>, List<TerrainTilePresentationDescriptor>>();
            foreach (TerrainTilePresentationDescriptor cell in source)
            {
                if (cell == null) { failed = true; Add(diagnostics, policy, ref suppressed, new TerrainPresentationDiagnostic(TerrainPresentationDiagnosticCode.UnsupportedTile, "source", "Null terrain descriptor is not accepted.")); break; }
                if (count++ >= policy.MaxCells) { failed = true; Add(diagnostics, policy, ref suppressed, new TerrainPresentationDiagnostic(TerrainPresentationDiagnosticCode.CellBudgetExceeded, "cells", "Terrain cell budget exceeded.")); break; }
                int chunkX = checked((int)(cell.GridX / policy.ChunkWidth)); int chunkY = checked((int)(cell.GridY / policy.ChunkHeight));
                var key = Tuple.Create(chunkX, chunkY); List<TerrainTilePresentationDescriptor> list; if (!grouped.TryGetValue(key, out list)) { list = new List<TerrainTilePresentationDescriptor>(); grouped.Add(key, list); } list.Add(cell);
            }
            var chunks = grouped.OrderBy(p => p.Key.Item2).ThenBy(p => p.Key.Item1).Select(p => new TerrainChunkDescriptor(p.Key.Item1, p.Key.Item2, policy.ChunkWidth, policy.ChunkHeight, p.Value)).ToArray();
            return new TerrainPresentationBuildResult(chunks, diagnostics, new TerrainPresentationExecutionState(failed ? TerrainPresentationCompletionStatus.Failed : TerrainPresentationCompletionStatus.Succeeded, suppressed));
        }
        private static void Add(List<TerrainPresentationDiagnostic> list, TerrainPresentationPolicy policy, ref int suppressed, TerrainPresentationDiagnostic d) { if (list.Count < policy.MaxDiagnostics) list.Add(d); else if (suppressed < int.MaxValue) suppressed++; }
    }
}
