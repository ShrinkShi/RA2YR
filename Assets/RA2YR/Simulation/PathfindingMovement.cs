using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace RA2YR.Simulation
{
    public readonly struct PathRequestId : IEquatable<PathRequestId>, IComparable<PathRequestId>
    {
        public PathRequestId(long value) { Value = value; }
        public long Value { get; }
        public bool Equals(PathRequestId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is PathRequestId && Equals((PathRequestId)obj);
        public override int GetHashCode() => Value.GetHashCode();
        public int CompareTo(PathRequestId other) => Value.CompareTo(other.Value);
    }

    public enum PathResultStatus { Succeeded, NoRoute, Blocked, Cancelled, BudgetExceeded, InvalidRequest }
    public enum PathDiagnosticCode { InvalidRequest, MissingNode, CapabilityMismatch, BlockedNode, NegativeCost, ExpansionBudgetExceeded, RouteBudgetExceeded, Cancelled, NoRoute, CacheInvalidated }

    public sealed class PathDiagnostic
    {
        public PathDiagnostic(PathDiagnosticCode code, long nodeId, string message) { Code = code; NodeId = nodeId; Message = message ?? string.Empty; }
        public PathDiagnosticCode Code { get; }
        public long NodeId { get; }
        public string Message { get; }
    }

    public sealed class PathSearchPolicy
    {
        public PathSearchPolicy(int maxExpansions = 10000, int maxRouteNodes = 4096, int maxDiagnostics = 256)
        { if (maxExpansions <= 0 || maxRouteNodes <= 0 || maxDiagnostics < 0) throw new ArgumentOutOfRangeException(); MaxExpansions = maxExpansions; MaxRouteNodes = maxRouteNodes; MaxDiagnostics = maxDiagnostics; }
        public int MaxExpansions { get; }
        public int MaxRouteNodes { get; }
        public int MaxDiagnostics { get; }
    }

    public readonly struct PathRequest
    {
        public PathRequest(PathRequestId id, EntityId entity, long startNode, long goalNode, int capabilityMask, long requestTick, PathSearchPolicy policy)
        { Id = id; Entity = entity; StartNode = startNode; GoalNode = goalNode; CapabilityMask = capabilityMask; RequestTick = requestTick; Policy = policy ?? throw new ArgumentNullException(nameof(policy)); }
        public PathRequestId Id { get; }
        public EntityId Entity { get; }
        public long StartNode { get; }
        public long GoalNode { get; }
        public int CapabilityMask { get; }
        public long RequestTick { get; }
        public PathSearchPolicy Policy { get; }
    }

    public sealed class PathResult
    {
        internal PathResult(PathRequest request, PathResultStatus status, IEnumerable<long> nodes, int expansions, IEnumerable<PathDiagnostic> diagnostics)
        { Request = request; Status = status; Nodes = new ReadOnlyCollection<long>((nodes ?? Enumerable.Empty<long>()).ToList()); Expansions = expansions; Diagnostics = new ReadOnlyCollection<PathDiagnostic>((diagnostics ?? Enumerable.Empty<PathDiagnostic>()).ToList()); }
        public PathRequest Request { get; }
        public PathResultStatus Status { get; }
        public bool IsSuccess => Status == PathResultStatus.Succeeded;
        public IReadOnlyList<long> Nodes { get; }
        public int Expansions { get; }
        public IReadOnlyList<PathDiagnostic> Diagnostics { get; }
    }

    public interface IPathCache
    {
        bool TryGet(PathRequest request, out PathResult result);
        void Store(PathRequest request, PathResult result);
        void Invalidate(IEnumerable<long> dirtyNodes);
    }

    public sealed class DeterministicPathCache : IPathCache
    {
        private readonly Dictionary<string, PathResult> entries = new Dictionary<string, PathResult>(StringComparer.Ordinal);
        public bool TryGet(PathRequest request, out PathResult result) => entries.TryGetValue(Key(request), out result);
        public void Store(PathRequest request, PathResult result) { if (result != null && result.IsSuccess) entries[Key(request)] = result; }
        public void Invalidate(IEnumerable<long> dirtyNodes) { entries.Clear(); }
        private static string Key(PathRequest request) => request.Id.Value + ":" + request.Entity.Index + ":" + request.Entity.Generation + ":" + request.StartNode + ":" + request.GoalNode + ":" + request.CapabilityMask + ":" + request.RequestTick;
    }

    public sealed class DeterministicManagedPathfinder
    {
        private readonly MovementGraphCandidate graph;
        private readonly Dictionary<long, MovementNode> nodes;
        private readonly Dictionary<long, List<MovementEdgeCandidate>> edges;
        private readonly SimulationOccupancy occupancy;
        private readonly IPathCache cache;

        public DeterministicManagedPathfinder(MovementGraphCandidate graph, SimulationOccupancy occupancy = null, IPathCache cache = null)
        {
            this.graph = graph ?? throw new ArgumentNullException(nameof(graph));
            nodes = graph.Nodes.ToDictionary(x => x.StableId);
            edges = new Dictionary<long, List<MovementEdgeCandidate>>();
            foreach (MovementEdgeCandidate edge in graph.Edges.OrderBy(x => x.StableId)) { List<MovementEdgeCandidate> list; if (!edges.TryGetValue(edge.SourceNode, out list)) { list = new List<MovementEdgeCandidate>(); edges.Add(edge.SourceNode, list); } list.Add(edge); }
            this.occupancy = occupancy;
            this.cache = cache;
        }

        public PathResult FindPath(PathRequest request, CancellationToken cancellation = default(CancellationToken))
        {
            var diagnostics = new List<PathDiagnostic>();
            if (!request.Entity.IsValid || !nodes.ContainsKey(request.StartNode) || !nodes.ContainsKey(request.GoalNode)) return Fail(request, PathResultStatus.InvalidRequest, diagnostics, new PathDiagnostic(PathDiagnosticCode.InvalidRequest, request.StartNode, "Entity or node is invalid."));
            PathResult cached; if (cache != null && cache.TryGet(request, out cached)) return cached;
            if (!IsTraversable(nodes[request.StartNode]) || !IsTraversable(nodes[request.GoalNode])) return Fail(request, PathResultStatus.Blocked, diagnostics, new PathDiagnostic(PathDiagnosticCode.BlockedNode, request.StartNode, "Start or goal is not traversable."));
            if (occupancy != null && (occupancy.IsBlocked(nodes[request.StartNode].Cell, request.Entity) || occupancy.IsBlocked(nodes[request.GoalNode].Cell, request.Entity))) return Fail(request, PathResultStatus.Blocked, diagnostics, new PathDiagnostic(PathDiagnosticCode.BlockedNode, request.StartNode, "Start or goal is occupied."));
            if (request.StartNode == request.GoalNode) return Succeed(request, new[] { request.StartNode }, 0, diagnostics);
            var open = new List<SearchEntry>(); var g = new Dictionary<long, int>(); var cameFrom = new Dictionary<long, long>(); var closed = new HashSet<long>();
            g[request.StartNode] = 0; open.Add(new SearchEntry(request.StartNode, 0, Heuristic(nodes[request.StartNode], nodes[request.GoalNode]), 0)); int expansions = 0; long sequence = 1;
            while (open.Count > 0)
            {
                if (cancellation.IsCancellationRequested) return Fail(request, PathResultStatus.Cancelled, diagnostics, new PathDiagnostic(PathDiagnosticCode.Cancelled, 0, "Path request cancelled."));
                open.Sort(SearchEntry.Compare); SearchEntry current = open[0]; open.RemoveAt(0); if (closed.Contains(current.NodeId)) continue; closed.Add(current.NodeId);
                if (++expansions > request.Policy.MaxExpansions) return Fail(request, PathResultStatus.BudgetExceeded, diagnostics, new PathDiagnostic(PathDiagnosticCode.ExpansionBudgetExceeded, current.NodeId, "Expansion budget exceeded."));
                if (current.NodeId == request.GoalNode) { PathResult result = BuildSuccess(request, cameFrom, current.NodeId, expansions, diagnostics); if (cache != null) cache.Store(request, result); return result; }
                List<MovementEdgeCandidate> outgoing; if (!edges.TryGetValue(current.NodeId, out outgoing)) continue;
                foreach (MovementEdgeCandidate edge in outgoing.OrderBy(x => x.StableId))
                {
                    MovementNode target; if (!nodes.TryGetValue(edge.TargetNode, out target)) { Add(diagnostics, request.Policy, new PathDiagnostic(PathDiagnosticCode.MissingNode, edge.TargetNode, "Edge target is missing.")); continue; }
                    if (!IsTraversable(target)) { Add(diagnostics, request.Policy, new PathDiagnostic(PathDiagnosticCode.BlockedNode, target.StableId, "Target node is not traversable.")); continue; }
                    if ((edge.RequiredCapabilities & ~request.CapabilityMask) != 0) { Add(diagnostics, request.Policy, new PathDiagnostic(PathDiagnosticCode.CapabilityMismatch, edge.TargetNode, "Required capability is unavailable.")); continue; }
                    if (edge.CostCandidate < 0) { Add(diagnostics, request.Policy, new PathDiagnostic(PathDiagnosticCode.NegativeCost, edge.StableId, "Negative edge cost is invalid.")); continue; }
                    if (occupancy != null && occupancy.IsBlocked(target.Cell, request.Entity)) { Add(diagnostics, request.Policy, new PathDiagnostic(PathDiagnosticCode.BlockedNode, target.StableId, "Target node is dynamically occupied.")); continue; }
                    if (closed.Contains(target.StableId)) continue;
                    int nextG; try { nextG = checked(current.G + edge.CostCandidate); } catch (OverflowException) { continue; }
                    int oldG; if (g.TryGetValue(target.StableId, out oldG) && nextG >= oldG) continue;
                    g[target.StableId] = nextG; cameFrom[target.StableId] = current.NodeId; open.Add(new SearchEntry(target.StableId, nextG, Heuristic(target, nodes[request.GoalNode]), sequence++));
                }
            }
            return Fail(request, PathResultStatus.NoRoute, diagnostics, new PathDiagnostic(PathDiagnosticCode.NoRoute, request.GoalNode, "No route was found."));
        }

        private static int Heuristic(MovementNode left, MovementNode right) => Math.Abs(left.Cell.X - right.Cell.X) + Math.Abs(left.Cell.Y - right.Cell.Y);
        private static bool IsTraversable(MovementNode node)
        {
            return node.State == PassabilityState.Passable || node.State == PassabilityState.TraversableWithCost || node.State == PassabilityState.RequiresCapability;
        }
        private static PathResult BuildSuccess(PathRequest request, Dictionary<long, long> parents, long goal, int expansions, List<PathDiagnostic> diagnostics)
        { var route = new List<long>(); long current = goal; route.Add(current); while (parents.ContainsKey(current)) { current = parents[current]; route.Add(current); if (route.Count > request.Policy.MaxRouteNodes) return Fail(request, PathResultStatus.BudgetExceeded, diagnostics, new PathDiagnostic(PathDiagnosticCode.RouteBudgetExceeded, current, "Route budget exceeded.")); } route.Reverse(); return Succeed(request, route, expansions, diagnostics); }
        private static PathResult Succeed(PathRequest request, IEnumerable<long> nodes, int expansions, IEnumerable<PathDiagnostic> diagnostics) => new PathResult(request, PathResultStatus.Succeeded, nodes, expansions, diagnostics);
        private static PathResult Fail(PathRequest request, PathResultStatus status, List<PathDiagnostic> diagnostics, PathDiagnostic diagnostic) { Add(diagnostics, request.Policy, diagnostic); return new PathResult(request, status, Array.Empty<long>(), 0, diagnostics); }
        private static void Add(List<PathDiagnostic> diagnostics, PathSearchPolicy policy, PathDiagnostic diagnostic) { if (diagnostics.Count < policy.MaxDiagnostics) diagnostics.Add(diagnostic); }

        private readonly struct SearchEntry
        {
            public SearchEntry(long nodeId, int g, int h, long sequence) { NodeId = nodeId; G = g; H = h; Sequence = sequence; }
            public long NodeId { get; }
            public int G { get; }
            public int H { get; }
            public long Sequence { get; }
            public int F => checked(G + H);
            public static int Compare(SearchEntry left, SearchEntry right) { int f = left.F.CompareTo(right.F); if (f != 0) return f; int g = left.G.CompareTo(right.G); if (g != 0) return g; int node = left.NodeId.CompareTo(right.NodeId); return node != 0 ? node : left.Sequence.CompareTo(right.Sequence); }
        }
    }

    public enum MovementAdvanceStatus { Advanced, Arrived, Blocked, InvalidRoute, ReservationConflict }
    public readonly struct MovementRouteState
    {
        public MovementRouteState(EntityId entity, IEnumerable<long> route, int nextIndex = 0) { Entity = entity; Route = new ReadOnlyCollection<long>((route ?? Enumerable.Empty<long>()).ToList()); NextIndex = nextIndex; }
        public EntityId Entity { get; }
        public IReadOnlyList<long> Route { get; }
        public int NextIndex { get; }
        public bool IsComplete => NextIndex >= Route.Count - 1;
        public MovementRouteState Advance() => new MovementRouteState(Entity, Route, NextIndex + 1);
    }

    public sealed class DeterministicMovementController
    {
        private readonly MovementGraphCandidate graph;
        private readonly SimulationOccupancy occupancy;
        public DeterministicMovementController(MovementGraphCandidate graph, SimulationOccupancy occupancy) { this.graph = graph ?? throw new ArgumentNullException(nameof(graph)); this.occupancy = occupancy ?? throw new ArgumentNullException(nameof(occupancy)); }
        public MovementAdvanceStatus Advance(ref MovementRouteState state)
        {
            if (!state.Entity.IsValid || state.Route.Count == 0 || state.NextIndex < 0 || state.NextIndex >= state.Route.Count) return MovementAdvanceStatus.InvalidRoute;
            if (state.IsComplete) return MovementAdvanceStatus.Arrived;
            MovementNode from; MovementNode to; if (!TryNode(state.Route[state.NextIndex], out from) || !TryNode(state.Route[state.NextIndex + 1], out to)) return MovementAdvanceStatus.InvalidRoute;
            if (!occupancy.TryReserve(new Reservation(state.Entity, to.Cell, 0))) return MovementAdvanceStatus.ReservationConflict;
            if (!occupancy.TryMove(state.Entity, from.Cell, to.Cell)) return MovementAdvanceStatus.Blocked;
            state = state.Advance(); return state.IsComplete ? MovementAdvanceStatus.Arrived : MovementAdvanceStatus.Advanced;
        }
        private bool TryNode(long id, out MovementNode node)
        {
            foreach (MovementNode candidate in graph.Nodes) if (candidate.StableId == id) { node = candidate; return true; }
            node = default(MovementNode); return false;
        }
    }

    public readonly struct MovementProposal : IComparable<MovementProposal>
    {
        public MovementProposal(EntityId entity, CellCoordinate destination, int priority, long sequence) { Entity = entity; Destination = destination; Priority = priority; Sequence = sequence; }
        public EntityId Entity { get; }
        public CellCoordinate Destination { get; }
        public int Priority { get; }
        public long Sequence { get; }
        public int CompareTo(MovementProposal other) { int p = other.Priority.CompareTo(Priority); if (p != 0) return p; int e = Entity.CompareTo(other.Entity); return e != 0 ? e : Sequence.CompareTo(other.Sequence); }
    }

    public interface ITacticalMovementEvaluator
    {
        MovementProposal Evaluate(SimulationReadSnapshot snapshot, EntityId entity, CellCoordinate destination, int priority, long sequence);
    }

    public sealed class DeterministicLocalAvoidance
    {
        public IReadOnlyList<MovementProposal> Order(IEnumerable<MovementProposal> proposals) { var copy = (proposals ?? Enumerable.Empty<MovementProposal>()).ToList(); copy.Sort(); return new ReadOnlyCollection<MovementProposal>(copy); }
        public CellCoordinate YieldDestination(CellCoordinate current, CellCoordinate desired, IEnumerable<CellCoordinate> blockedCandidates) { var blocked = new HashSet<CellCoordinate>(blockedCandidates ?? Enumerable.Empty<CellCoordinate>()); if (!blocked.Contains(desired)) return desired; foreach (CellCoordinate candidate in new[] { new CellCoordinate(current.X + 1, current.Y), new CellCoordinate(current.X, current.Y + 1), new CellCoordinate(current.X - 1, current.Y), new CellCoordinate(current.X, current.Y - 1) }) if (!blocked.Contains(candidate)) return candidate; return current; }
    }

    public sealed class PathTickBudget
    {
        private readonly int maxRequests;
        private readonly int maxExpansions;
        public PathTickBudget(int maxRequests, int maxExpansions)
        { if (maxRequests <= 0 || maxExpansions <= 0) throw new ArgumentOutOfRangeException(); this.maxRequests = maxRequests; this.maxExpansions = maxExpansions; }
        public int RequestsStarted { get; private set; }
        public int ExpansionsConsumed { get; private set; }
        public bool TryStartRequest() { if (RequestsStarted >= maxRequests) return false; RequestsStarted++; return true; }
        public bool TryConsumeExpansions(int count) { if (count < 0 || count > maxExpansions - ExpansionsConsumed) return false; ExpansionsConsumed += count; return true; }
        public void Reset() { RequestsStarted = 0; ExpansionsConsumed = 0; }
    }

    public sealed class DeterministicPathBatch
    {
        private readonly DeterministicManagedPathfinder pathfinder;
        private readonly PathTickBudget budget;
        public DeterministicPathBatch(DeterministicManagedPathfinder pathfinder, PathTickBudget budget)
        { this.pathfinder = pathfinder ?? throw new ArgumentNullException(nameof(pathfinder)); this.budget = budget ?? throw new ArgumentNullException(nameof(budget)); }
        public IReadOnlyList<PathResult> Evaluate(IEnumerable<PathRequest> requests, CancellationToken cancellation = default(CancellationToken))
        {
            var results = new List<PathResult>();
            foreach (PathRequest request in (requests ?? Enumerable.Empty<PathRequest>()).OrderBy(x => x.Id))
            {
                if (cancellation.IsCancellationRequested) break;
                if (!budget.TryStartRequest()) { results.Add(new PathResult(request, PathResultStatus.BudgetExceeded, Array.Empty<long>(), 0, new[] { new PathDiagnostic(PathDiagnosticCode.ExpansionBudgetExceeded, 0, "Per-tick request budget exceeded.") })); continue; }
                PathResult result = pathfinder.FindPath(request, cancellation);
                if (!budget.TryConsumeExpansions(result.Expansions)) { results.Add(new PathResult(request, PathResultStatus.BudgetExceeded, Array.Empty<long>(), result.Expansions, new[] { new PathDiagnostic(PathDiagnosticCode.ExpansionBudgetExceeded, 0, "Per-tick expansion budget exceeded.") })); continue; }
                results.Add(result);
            }
            return new ReadOnlyCollection<PathResult>(results);
        }
    }
}
