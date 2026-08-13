using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RA2YR.Simulation
{
    public enum MovementDomain { Ground, Water, AmphibiousComposite, BridgeDeck, UnderBridge, Air, Subterranean, Tunnel, SpecialExtension }
    public enum MovementLayer { Ground, UnderBridge, BridgeDeck, Air, Subterranean, Tunnel, Special }
    public enum PassabilityState { Passable, TraversableWithCost, Blocked, RequiresCapability, TemporarilyBlocked, DestructibleBlocker, Unknown }
    public enum TerrainCellDiagnosticCode { DuplicateCell, ConflictingDuplicateCell, OutOfDomain, InvalidSubTile, CoordinateOverflow, CellBudgetExceeded, NoProgress }
    public enum DuplicateCellPolicy { PreserveAllAndDiagnose, RejectAnyDuplicate, AllowByteIdenticalDuplicatesButDiagnose }

    public readonly struct CellCoordinate : IEquatable<CellCoordinate>, IComparable<CellCoordinate>
    {
        public CellCoordinate(int x, int y) { X = x; Y = y; }
        public int X { get; }
        public int Y { get; }
        public bool Equals(CellCoordinate other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is CellCoordinate && Equals((CellCoordinate)obj);
        public override int GetHashCode() => (X * 397) ^ Y;
        public int CompareTo(CellCoordinate other) { int x = X.CompareTo(other.X); return x != 0 ? x : Y.CompareTo(other.Y); }
        public override string ToString() => X + "," + Y;
        public static bool operator ==(CellCoordinate left, CellCoordinate right) => left.Equals(right);
        public static bool operator !=(CellCoordinate left, CellCoordinate right) => !left.Equals(right);
    }

    public sealed class TerrainCellDiagnostic
    {
        public TerrainCellDiagnostic(TerrainCellDiagnosticCode code, int sourceOrdinal, CellCoordinate? coordinate, string message)
        { Code = code; SourceOrdinal = sourceOrdinal; Coordinate = coordinate; Message = message ?? string.Empty; }
        public TerrainCellDiagnosticCode Code { get; }
        public int SourceOrdinal { get; }
        public CellCoordinate? Coordinate { get; }
        public string Message { get; }
    }

    public readonly struct TerrainCellInput
    {
        public TerrainCellInput(CellCoordinate coordinate, uint tileRaw, byte subTileRaw, byte levelRaw, byte rampRaw, PassabilityState passability = PassabilityState.Unknown, int sourceOrdinal = 0)
        { Coordinate = coordinate; TileRawU32 = tileRaw; SubTileRaw = subTileRaw; LevelRaw = levelRaw; RampRaw = rampRaw; Passability = passability; SourceOrdinal = sourceOrdinal; }
        public CellCoordinate Coordinate { get; }
        public uint TileRawU32 { get; }
        public byte SubTileRaw { get; }
        public byte LevelRaw { get; }
        public byte RampRaw { get; }
        public PassabilityState Passability { get; }
        public int SourceOrdinal { get; }
        public bool ByteIdentical(TerrainCellInput other) => Coordinate == other.Coordinate && TileRawU32 == other.TileRawU32 && SubTileRaw == other.SubTileRaw && LevelRaw == other.LevelRaw && RampRaw == other.RampRaw && Passability == other.Passability;
    }

    public sealed class TerrainTopologyPolicy
    {
        public TerrainTopologyPolicy(int width = 0, int height = 0, int maxCells = 100000, int maxDiagnostics = 4096, int maxSubTile = 255, DuplicateCellPolicy duplicatePolicy = DuplicateCellPolicy.PreserveAllAndDiagnose)
        {
            if (width < 0 || height < 0 || maxCells < 0 || maxDiagnostics < 0 || maxSubTile < 0) throw new ArgumentOutOfRangeException();
            Width = width; Height = height; MaxCells = maxCells; MaxDiagnostics = maxDiagnostics; MaxSubTile = maxSubTile; DuplicatePolicy = duplicatePolicy;
            if (!Enum.IsDefined(typeof(DuplicateCellPolicy), duplicatePolicy)) throw new ArgumentOutOfRangeException(nameof(duplicatePolicy));
        }
        public int Width { get; }
        public int Height { get; }
        public int MaxCells { get; }
        public int MaxDiagnostics { get; }
        public int MaxSubTile { get; }
        public DuplicateCellPolicy DuplicatePolicy { get; }
    }

    public sealed class TerrainTopologyDocument
    {
        internal TerrainTopologyDocument(IEnumerable<TerrainCellInput> cells, IEnumerable<IReadOnlyList<TerrainCellInput>> duplicates, IEnumerable<TerrainCellDiagnostic> diagnostics, bool success, bool sparse, bool dense)
        { Cells = new ReadOnlyCollection<TerrainCellInput>((cells ?? Enumerable.Empty<TerrainCellInput>()).ToList()); DuplicateGroups = new ReadOnlyCollection<IReadOnlyList<TerrainCellInput>>((duplicates ?? Enumerable.Empty<IReadOnlyList<TerrainCellInput>>()).ToList()); Diagnostics = new ReadOnlyCollection<TerrainCellDiagnostic>((diagnostics ?? Enumerable.Empty<TerrainCellDiagnostic>()).ToList()); IsSuccess = success; IsSparse = sparse; IsDense = dense; }
        public IReadOnlyList<TerrainCellInput> Cells { get; }
        public IReadOnlyList<IReadOnlyList<TerrainCellInput>> DuplicateGroups { get; }
        public IReadOnlyList<TerrainCellDiagnostic> Diagnostics { get; }
        public bool IsSuccess { get; }
        public bool IsSparse { get; }
        public bool IsDense { get; }
    }

    public static class TerrainTopologyBuilder
    {
        public static TerrainTopologyDocument Build(IEnumerable<TerrainCellInput> source, TerrainTopologyPolicy policy = null)
        {
            policy = policy ?? new TerrainTopologyPolicy();
            if (source == null) return new TerrainTopologyDocument(Array.Empty<TerrainCellInput>(), Array.Empty<IReadOnlyList<TerrainCellInput>>(), new[] { new TerrainCellDiagnostic(TerrainCellDiagnosticCode.NoProgress, 0, null, "Cell source is null.") }, false, false, false);
            var cells = new List<TerrainCellInput>(); var diagnostics = new List<TerrainCellDiagnostic>(); var groups = new List<IReadOnlyList<TerrainCellInput>>(); var byCoordinate = new Dictionary<CellCoordinate, List<TerrainCellInput>>(); bool success = true; int ordinal = 0;
            foreach (TerrainCellInput input in source)
            {
                if (cells.Count >= policy.MaxCells) { success = false; Add(diagnostics, policy, new TerrainCellDiagnostic(TerrainCellDiagnosticCode.CellBudgetExceeded, ordinal, input.Coordinate, "Cell budget exceeded.")); break; }
                TerrainCellInput cell = input.SourceOrdinal == 0 && ordinal != 0 ? new TerrainCellInput(input.Coordinate, input.TileRawU32, input.SubTileRaw, input.LevelRaw, input.RampRaw, input.Passability, ordinal) : input;
                cells.Add(cell); ordinal++;
                List<TerrainCellInput> group;
                if (!byCoordinate.TryGetValue(cell.Coordinate, out group)) { group = new List<TerrainCellInput>(); byCoordinate.Add(cell.Coordinate, group); }
                group.Add(cell);
                if (policy.Width > 0 && policy.Height > 0 && (cell.Coordinate.X < 0 || cell.Coordinate.Y < 0 || cell.Coordinate.X >= policy.Width || cell.Coordinate.Y >= policy.Height)) Add(diagnostics, policy, new TerrainCellDiagnostic(TerrainCellDiagnosticCode.OutOfDomain, cell.SourceOrdinal, cell.Coordinate, "Coordinate is outside the explicit domain."));
                if (cell.SubTileRaw > policy.MaxSubTile) { success = false; Add(diagnostics, policy, new TerrainCellDiagnostic(TerrainCellDiagnosticCode.InvalidSubTile, cell.SourceOrdinal, cell.Coordinate, "SubTile is outside the configured candidate range.")); }
            }
            foreach (KeyValuePair<CellCoordinate, List<TerrainCellInput>> pair in byCoordinate.OrderBy(p => p.Key)) if (pair.Value.Count > 1)
            {
                groups.Add(new ReadOnlyCollection<TerrainCellInput>(pair.Value.OrderBy(x => x.SourceOrdinal).ToList()));
                bool identical = pair.Value.Skip(1).All(x => x.ByteIdentical(pair.Value[0]));
                Add(diagnostics, policy, new TerrainCellDiagnostic(identical ? TerrainCellDiagnosticCode.DuplicateCell : TerrainCellDiagnosticCode.ConflictingDuplicateCell, pair.Value[0].SourceOrdinal, pair.Key, identical ? "Byte-identical duplicate cell." : "Conflicting duplicate cell."));
                if (policy.DuplicatePolicy == DuplicateCellPolicy.RejectAnyDuplicate || (policy.DuplicatePolicy == DuplicateCellPolicy.AllowByteIdenticalDuplicatesButDiagnose && !identical)) success = false;
            }
            bool dense = policy.Width > 0 && policy.Height > 0 && cells.Count == checked(policy.Width * policy.Height); bool sparse = policy.Width > 0 && policy.Height > 0 && cells.Count < checked(policy.Width * policy.Height);
            return new TerrainTopologyDocument(cells, groups, diagnostics, success, sparse, dense);
        }
        private static void Add(List<TerrainCellDiagnostic> list, TerrainTopologyPolicy policy, TerrainCellDiagnostic diagnostic) { if (list.Count < policy.MaxDiagnostics) list.Add(diagnostic); }
    }

    public readonly struct MovementCapabilityProfile
    {
        public MovementCapabilityProfile(string movementZoneRaw, string speedTypeRaw, string locomotorReferenceRaw)
        { MovementZoneRaw = movementZoneRaw ?? string.Empty; SpeedTypeRaw = speedTypeRaw ?? string.Empty; LocomotorReferenceRaw = locomotorReferenceRaw ?? string.Empty; }
        public string MovementZoneRaw { get; }
        public string SpeedTypeRaw { get; }
        public string LocomotorReferenceRaw { get; }
    }

    public readonly struct MovementNode : IComparable<MovementNode>
    {
        public MovementNode(long stableId, CellCoordinate cell, MovementDomain domain, MovementLayer layer, PassabilityState state, byte levelRaw)
        { StableId = stableId; Cell = cell; Domain = domain; Layer = layer; State = state; LevelRaw = levelRaw; }
        public long StableId { get; }
        public CellCoordinate Cell { get; }
        public MovementDomain Domain { get; }
        public MovementLayer Layer { get; }
        public PassabilityState State { get; }
        public byte LevelRaw { get; }
        public int CompareTo(MovementNode other) => StableId.CompareTo(other.StableId);
    }

    public readonly struct MovementEdgeCandidate : IComparable<MovementEdgeCandidate>
    {
        public MovementEdgeCandidate(long stableId, long sourceNode, long targetNode, int requiredCapabilities, int costCandidate)
        { StableId = stableId; SourceNode = sourceNode; TargetNode = targetNode; RequiredCapabilities = requiredCapabilities; CostCandidate = costCandidate; }
        public long StableId { get; }
        public long SourceNode { get; }
        public long TargetNode { get; }
        public int RequiredCapabilities { get; }
        public int CostCandidate { get; }
        public int CompareTo(MovementEdgeCandidate other) => StableId.CompareTo(other.StableId);
    }

    public sealed class MovementGraphCandidate
    {
        public MovementGraphCandidate(IEnumerable<MovementNode> nodes, IEnumerable<MovementEdgeCandidate> edges)
        { Nodes = new ReadOnlyCollection<MovementNode>((nodes ?? Enumerable.Empty<MovementNode>()).OrderBy(x => x.StableId).ToList()); Edges = new ReadOnlyCollection<MovementEdgeCandidate>((edges ?? Enumerable.Empty<MovementEdgeCandidate>()).OrderBy(x => x.StableId).ToList()); }
        public IReadOnlyList<MovementNode> Nodes { get; }
        public IReadOnlyList<MovementEdgeCandidate> Edges { get; }
    }

    public readonly struct StaticOccupancy
    {
        public StaticOccupancy(CellCoordinate cell, string sourceRole) { Cell = cell; SourceRole = sourceRole ?? string.Empty; }
        public CellCoordinate Cell { get; }
        public string SourceRole { get; }
    }
    public readonly struct FoundationOccupancy
    {
        public FoundationOccupancy(CellCoordinate origin, int width, int height, string sourceRole)
        { if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(); Origin = origin; Width = width; Height = height; SourceRole = sourceRole ?? string.Empty; }
        public CellCoordinate Origin { get; }
        public int Width { get; }
        public int Height { get; }
        public string SourceRole { get; }
        public IEnumerable<CellCoordinate> Cells() { for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++) yield return new CellCoordinate(checked(Origin.X + x), checked(Origin.Y + y)); }
    }
    public readonly struct Reservation
    {
        public Reservation(EntityId owner, CellCoordinate cell, long tick) { Owner = owner; Cell = cell; Tick = tick; }
        public EntityId Owner { get; }
        public CellCoordinate Cell { get; }
        public long Tick { get; }
    }
    public readonly struct DynamicOccupancy
    {
        public DynamicOccupancy(EntityId owner, CellCoordinate cell) { Owner = owner; Cell = cell; }
        public EntityId Owner { get; }
        public CellCoordinate Cell { get; }
    }

    public sealed class SimulationOccupancy
    {
        private readonly SortedSet<CellCoordinate> staticCells = new SortedSet<CellCoordinate>();
        private readonly SortedDictionary<CellCoordinate, EntityId> dynamicCells = new SortedDictionary<CellCoordinate, EntityId>();
        private readonly SortedDictionary<CellCoordinate, Reservation> reservations = new SortedDictionary<CellCoordinate, Reservation>();
        public IReadOnlyCollection<CellCoordinate> StaticCells => new ReadOnlyCollection<CellCoordinate>(staticCells.ToList());
        public bool AddStatic(StaticOccupancy occupancy) => staticCells.Add(occupancy.Cell);
        public bool AddFoundation(FoundationOccupancy foundation) { bool added = false; foreach (CellCoordinate cell in foundation.Cells()) added |= staticCells.Add(cell); return added; }
        public bool TryAcquireDynamic(DynamicOccupancy occupancy) { if (IsBlocked(occupancy.Cell, occupancy.Owner)) return false; dynamicCells[occupancy.Cell] = occupancy.Owner; return true; }
        public bool ReleaseDynamic(EntityId owner) { CellCoordinate? found = null; foreach (KeyValuePair<CellCoordinate, EntityId> pair in dynamicCells) if (pair.Value == owner) { found = pair.Key; break; } if (!found.HasValue) return false; dynamicCells.Remove(found.Value); return true; }
        public bool TryReserve(Reservation reservation) { if (IsBlocked(reservation.Cell, reservation.Owner)) return false; reservations[reservation.Cell] = reservation; return true; }
        public bool IsBlocked(CellCoordinate cell, EntityId requester) { EntityId occupant; Reservation reservation; return staticCells.Contains(cell) || (dynamicCells.TryGetValue(cell, out occupant) && occupant != requester) || (reservations.TryGetValue(cell, out reservation) && reservation.Owner != requester); }
        public bool TryMove(EntityId owner, CellCoordinate from, CellCoordinate to) { EntityId current; if (!dynamicCells.TryGetValue(from, out current) || current != owner || IsBlocked(to, owner)) return false; dynamicCells.Remove(from); dynamicCells[to] = owner; return true; }
    }

    public sealed class DeterministicSpatialIndex
    {
        private readonly SortedDictionary<CellCoordinate, SortedSet<EntityId>> cells = new SortedDictionary<CellCoordinate, SortedSet<EntityId>>();
        public int Count { get; private set; }
        public bool Insert(EntityId entity, CellCoordinate cell) { if (!entity.IsValid) throw new ArgumentException(nameof(entity)); SortedSet<EntityId> occupants; if (!cells.TryGetValue(cell, out occupants)) { occupants = new SortedSet<EntityId>(); cells.Add(cell, occupants); } if (!occupants.Add(entity)) return false; Count++; return true; }
        public bool Remove(EntityId entity, CellCoordinate cell) { SortedSet<EntityId> occupants; if (!cells.TryGetValue(cell, out occupants) || !occupants.Remove(entity)) return false; Count--; if (occupants.Count == 0) cells.Remove(cell); return true; }
        public bool Move(EntityId entity, CellCoordinate from, CellCoordinate to) { if (!Remove(entity, from)) return false; Insert(entity, to); return true; }
        public IReadOnlyList<EntityId> QueryNeighbors(CellCoordinate center, int radius) { if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius)); var result = new List<EntityId>(); for (int y = checked(center.Y - radius); y <= checked(center.Y + radius); y++) for (int x = checked(center.X - radius); x <= checked(center.X + radius); x++) { SortedSet<EntityId> occupants; if (cells.TryGetValue(new CellCoordinate(x, y), out occupants)) result.AddRange(occupants); } result.Sort(); return new ReadOnlyCollection<EntityId>(result); }
    }
}
