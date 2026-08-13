using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using RA2YR.Simulation;

namespace RA2YR.Tests.EditMode.Simulation
{
    public sealed class PathfindingMovementTests
    {
        private static MovementGraphCandidate LineGraph(bool middleBlocked = false, int required = 0)
        {
            PassabilityState middle = middleBlocked ? PassabilityState.Blocked : PassabilityState.Passable;
            var nodes = new[]
            {
                new MovementNode(0, new CellCoordinate(0, 0), MovementDomain.Ground, MovementLayer.Ground, PassabilityState.Passable, 0),
                new MovementNode(1, new CellCoordinate(1, 0), MovementDomain.Ground, MovementLayer.Ground, middle, 0),
                new MovementNode(2, new CellCoordinate(2, 0), MovementDomain.Ground, MovementLayer.Ground, PassabilityState.Passable, 0)
            };
            var edges = new[]
            {
                new MovementEdgeCandidate(0, 0, 1, required, 1),
                new MovementEdgeCandidate(1, 1, 2, required, 1)
            };
            return new MovementGraphCandidate(nodes, edges);
        }

        private static PathRequest Request(long id, long start = 0, long goal = 2, int capability = 0, PathSearchPolicy policy = null)
        { return new PathRequest(new PathRequestId(id), new EntityId((int)id, 1), start, goal, capability, 1, policy ?? new PathSearchPolicy()); }

        [Test]
        public void SimplePathIsDeterministicAndImmutable()
        {
            PathResult result = new DeterministicManagedPathfinder(LineGraph()).FindPath(Request(0));
            Assert.That(result.Status, Is.EqualTo(PathResultStatus.Succeeded));
            Assert.That(result.Nodes.ToArray(), Is.EqualTo(new long[] { 0, 1, 2 }));
            Assert.That(result.Nodes, Is.Not.TypeOf<long[]>());
        }

        [Test]
        public void StartEqualsGoalReturnsSingleNode()
        {
            PathResult result = new DeterministicManagedPathfinder(LineGraph()).FindPath(Request(0, 1, 1));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Nodes, Is.EqualTo(new long[] { 1 }));
        }

        [Test]
        public void BlockedNodeProducesNoRouteWithoutPromotion()
        {
            PathResult result = new DeterministicManagedPathfinder(LineGraph(true)).FindPath(Request(0));
            Assert.That(result.Status, Is.EqualTo(PathResultStatus.NoRoute));
            Assert.That(result.Diagnostics.Any(x => x.Code == PathDiagnosticCode.BlockedNode), Is.True);
        }

        [Test]
        public void MissingNodeFailsClosed()
        {
            PathResult result = new DeterministicManagedPathfinder(LineGraph()).FindPath(Request(0, 9, 2));
            Assert.That(result.Status, Is.EqualTo(PathResultStatus.InvalidRequest));
        }

        [Test]
        public void CapabilityMismatchDoesNotTraverseEdge()
        {
            PathResult result = new DeterministicManagedPathfinder(LineGraph(false, 4)).FindPath(Request(0, capability: 0));
            Assert.That(result.Status, Is.EqualTo(PathResultStatus.NoRoute));
            Assert.That(result.Diagnostics.Any(x => x.Code == PathDiagnosticCode.CapabilityMismatch), Is.True);
            Assert.That(new DeterministicManagedPathfinder(LineGraph(false, 4)).FindPath(Request(0, capability: 4)).IsSuccess, Is.True);
        }

        [Test]
        public void DynamicBlockerProducesBlockedResult()
        {
            SimulationOccupancy occupancy = new SimulationOccupancy();
            occupancy.TryAcquireDynamic(new DynamicOccupancy(new EntityId(7, 1), new CellCoordinate(1, 0)));
            PathResult result = new DeterministicManagedPathfinder(LineGraph(), occupancy).FindPath(Request(0));
            Assert.That(result.Status, Is.EqualTo(PathResultStatus.NoRoute));
        }

        [Test]
        public void UnknownAndTemporaryNodesAreNotTraversed()
        {
            MovementGraphCandidate graph = new MovementGraphCandidate(
                new[] { new MovementNode(0, new CellCoordinate(0, 0), MovementDomain.Ground, MovementLayer.Ground, PassabilityState.Passable, 0), new MovementNode(1, new CellCoordinate(1, 0), MovementDomain.Ground, MovementLayer.Ground, PassabilityState.Unknown, 0), new MovementNode(2, new CellCoordinate(2, 0), MovementDomain.Ground, MovementLayer.Ground, PassabilityState.Passable, 0) },
                new[] { new MovementEdgeCandidate(0, 0, 1, 0, 1), new MovementEdgeCandidate(1, 1, 2, 0, 1) });
            Assert.That(new DeterministicManagedPathfinder(graph).FindPath(Request(0)).Status, Is.EqualTo(PathResultStatus.NoRoute));
        }

        [Test]
        public void EqualCostRoutesUseStableNodeTieBreak()
        {
            var graph = new MovementGraphCandidate(
                new[] { new MovementNode(0, new CellCoordinate(0, 0), MovementDomain.Ground, MovementLayer.Ground, PassabilityState.Passable, 0), new MovementNode(1, new CellCoordinate(1, 0), MovementDomain.Ground, MovementLayer.Ground, PassabilityState.Passable, 0), new MovementNode(2, new CellCoordinate(1, 1), MovementDomain.Ground, MovementLayer.Ground, PassabilityState.Passable, 0), new MovementNode(3, new CellCoordinate(2, 0), MovementDomain.Ground, MovementLayer.Ground, PassabilityState.Passable, 0) },
                new[] { new MovementEdgeCandidate(0, 0, 2, 0, 1), new MovementEdgeCandidate(1, 0, 1, 0, 1), new MovementEdgeCandidate(2, 1, 3, 0, 1), new MovementEdgeCandidate(3, 2, 3, 0, 1) });
            PathResult result = new DeterministicManagedPathfinder(graph).FindPath(Request(0, 0, 3));
            Assert.That(result.Nodes, Is.EqualTo(new long[] { 0, 1, 3 }));
        }

        [Test]
        public void ExpansionAndRouteBudgetsFailClosed()
        {
            PathResult result = new DeterministicManagedPathfinder(LineGraph()).FindPath(Request(0, policy: new PathSearchPolicy(1, 4)));
            Assert.That(result.Status, Is.EqualTo(PathResultStatus.BudgetExceeded));
            PathResult route = new DeterministicManagedPathfinder(LineGraph()).FindPath(Request(0, policy: new PathSearchPolicy(10, 2)));
            Assert.That(route.Status, Is.EqualTo(PathResultStatus.BudgetExceeded));
        }

        [Test]
        public void CancellationIsStructured()
        {
            using (var source = new CancellationTokenSource())
            {
                source.Cancel();
                PathResult result = new DeterministicManagedPathfinder(LineGraph()).FindPath(Request(0), source.Token);
                Assert.That(result.Status, Is.EqualTo(PathResultStatus.Cancelled));
                Assert.That(result.Diagnostics.Any(x => x.Code == PathDiagnosticCode.Cancelled), Is.True);
            }
        }

        [Test]
        public void CacheStoresOnlySuccessfulResultsAndInvalidates()
        {
            var cache = new DeterministicPathCache();
            var finder = new DeterministicManagedPathfinder(LineGraph(), null, cache);
            PathRequest request = Request(0);
            Assert.That(finder.FindPath(request).IsSuccess, Is.True);
            PathResult cached;
            Assert.That(cache.TryGet(request, out cached), Is.True);
            cache.Invalidate(new[] { 1L });
            Assert.That(cache.TryGet(request, out cached), Is.False);
        }

        [Test]
        public void MovementControllerUsesReservationAndArrival()
        {
            SimulationOccupancy occupancy = new SimulationOccupancy();
            EntityId entity = new EntityId(0, 1);
            occupancy.TryAcquireDynamic(new DynamicOccupancy(entity, new CellCoordinate(0, 0)));
            var state = new MovementRouteState(entity, new long[] { 0, 1, 2 });
            var controller = new DeterministicMovementController(LineGraph(), occupancy);
            Assert.That(controller.Advance(ref state), Is.EqualTo(MovementAdvanceStatus.Advanced));
            Assert.That(controller.Advance(ref state), Is.EqualTo(MovementAdvanceStatus.Arrived));
            Assert.That(state.IsComplete, Is.True);
        }

        [Test]
        public void ReservationConflictBlocksAdvanceWithoutReleasingSource()
        {
            SimulationOccupancy occupancy = new SimulationOccupancy();
            EntityId entity = new EntityId(0, 1);
            occupancy.TryAcquireDynamic(new DynamicOccupancy(entity, new CellCoordinate(0, 0)));
            occupancy.TryReserve(new Reservation(new EntityId(9, 1), new CellCoordinate(1, 0), 0));
            var state = new MovementRouteState(entity, new long[] { 0, 1 });
            Assert.That(new DeterministicMovementController(LineGraph(), occupancy).Advance(ref state), Is.EqualTo(MovementAdvanceStatus.ReservationConflict));
            Assert.That(occupancy.IsBlocked(new CellCoordinate(0, 0), entity), Is.False);
        }

        [Test]
        public void LocalAvoidanceYieldIsStable()
        {
            var avoidance = new DeterministicLocalAvoidance();
            CellCoordinate result = avoidance.YieldDestination(new CellCoordinate(0, 0), new CellCoordinate(1, 0), new[] { new CellCoordinate(1, 0) });
            Assert.That(result, Is.EqualTo(new CellCoordinate(0, 1)));
        }

        [Test]
        public void ProposalOrderIsStableRegardlessOfInputPermutation()
        {
            var avoidance = new DeterministicLocalAvoidance();
            var left = avoidance.Order(new[] { new MovementProposal(new EntityId(2, 1), new CellCoordinate(1, 0), 1, 0), new MovementProposal(new EntityId(1, 1), new CellCoordinate(1, 0), 1, 1) });
            var right = avoidance.Order(new[] { new MovementProposal(new EntityId(1, 1), new CellCoordinate(1, 0), 1, 1), new MovementProposal(new EntityId(2, 1), new CellCoordinate(1, 0), 1, 0) });
            Assert.That(left.Select(x => x.Entity).ToArray(), Is.EqualTo(right.Select(x => x.Entity).ToArray()));
        }

        [Test]
        public void BatchBudgetLimitsRequestsAndIsDeterministic()
        {
            var batch = new DeterministicPathBatch(new DeterministicManagedPathfinder(LineGraph()), new PathTickBudget(2, 100));
            var results = batch.Evaluate(Enumerable.Range(0, 3).Select(i => Request(i)).ToArray());
            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results[0].IsSuccess, Is.True);
            Assert.That(results[2].Status, Is.EqualTo(PathResultStatus.BudgetExceeded));
        }

        [Test]
        public void OneHundredSyntheticRequestsRemainStable()
        {
            var batch = new DeterministicPathBatch(new DeterministicManagedPathfinder(LineGraph()), new PathTickBudget(100, 10000));
            var results = batch.Evaluate(Enumerable.Range(0, 100).Select(i => Request(i)).Reverse());
            Assert.That(results.Count, Is.EqualTo(100));
            Assert.That(results.All(x => x.IsSuccess), Is.True);
            Assert.That(results.Select(x => x.Request.Id.Value).ToArray(), Is.EqualTo(Enumerable.Range(0, 100).Select(i => (long)i).ToArray()));
        }

        [Test]
        public void InvalidPolicyIsRejectedBeforeSearch()
        { Assert.Throws<ArgumentOutOfRangeException>(() => new PathSearchPolicy(0)); }

        [Test]
        public void NoRouteResultHasNoPartialNodes()
        {
            PathResult result = new DeterministicManagedPathfinder(LineGraph(true)).FindPath(Request(0));
            Assert.That(result.Nodes, Is.Empty);
        }
    }
}
