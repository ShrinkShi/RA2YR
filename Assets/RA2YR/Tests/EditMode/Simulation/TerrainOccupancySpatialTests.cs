using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RA2YR.Simulation;

namespace RA2YR.Tests.EditMode.Simulation
{
    public sealed class TerrainOccupancySpatialTests
    {
        private static TerrainCellInput Cell(int x, int y, uint tile = 1, byte subTile = 0, byte level = 0, byte ramp = 0, PassabilityState state = PassabilityState.Unknown, int ordinal = 0)
        { return new TerrainCellInput(new CellCoordinate(x, y), tile, subTile, level, ramp, state, ordinal); }

        [Test]
        public void SparseAndDenseTopologyRemainExplicit()
        {
            TerrainTopologyDocument sparse = TerrainTopologyBuilder.Build(new[] { Cell(0, 0) }, new TerrainTopologyPolicy(2, 2));
            TerrainTopologyDocument dense = TerrainTopologyBuilder.Build(new[] { Cell(0, 0), Cell(1, 0), Cell(0, 1), Cell(1, 1) }, new TerrainTopologyPolicy(2, 2));
            Assert.That(sparse.IsSparse, Is.True);
            Assert.That(sparse.IsDense, Is.False);
            Assert.That(dense.IsDense, Is.True);
        }

        [Test]
        public void HugeDomainDoesNotOverflowDenseClassification()
        {
            TerrainTopologyDocument result = TerrainTopologyBuilder.Build(new[] { Cell(0, 0) }, new TerrainTopologyPolicy(int.MaxValue, int.MaxValue));
            Assert.That(result.IsSparse, Is.True);
            Assert.That(result.IsDense, Is.False);
        }

        [Test]
        public void DuplicateCellsPreserveSourceOrderAndConflict()
        {
            TerrainTopologyDocument result = TerrainTopologyBuilder.Build(new[] { Cell(2, 3, 1, ordinal: 10), Cell(2, 3, 2, ordinal: 11) });
            Assert.That(result.Cells.Count, Is.EqualTo(2));
            Assert.That(result.DuplicateGroups.Count, Is.EqualTo(1));
            Assert.That(result.DuplicateGroups[0].Select(x => x.TileRawU32).ToArray(), Is.EqualTo(new uint[] { 1, 2 }));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(TerrainCellDiagnosticCode.ConflictingDuplicateCell));
        }

        [Test]
        public void DuplicatePoliciesFailOnlyWhenExplicitlyConfigured()
        {
            TerrainCellInput[] cells = { Cell(0, 0), Cell(0, 0) };
            Assert.That(TerrainTopologyBuilder.Build(cells).IsSuccess, Is.True);
            Assert.That(TerrainTopologyBuilder.Build(cells, new TerrainTopologyPolicy(duplicatePolicy: DuplicateCellPolicy.RejectAnyDuplicate)).IsSuccess, Is.False);
            Assert.That(TerrainTopologyBuilder.Build(new[] { Cell(0, 0, 1), Cell(0, 0, 2) }, new TerrainTopologyPolicy(duplicatePolicy: DuplicateCellPolicy.AllowByteIdenticalDuplicatesButDiagnose)).IsSuccess, Is.False);
        }

        [Test]
        public void UnknownPassabilityIsNotPromoted()
        { Assert.That(Cell(1, 1).Passability, Is.EqualTo(PassabilityState.Unknown)); }

        [Test]
        public void OutOfDomainAndInvalidSubTileAreDiagnosed()
        {
            TerrainTopologyDocument result = TerrainTopologyBuilder.Build(new[] { Cell(3, -1, subTile: 9) }, new TerrainTopologyPolicy(2, 2, maxSubTile: 3));
            Assert.That(result.Diagnostics.Select(x => x.Code), Does.Contain(TerrainCellDiagnosticCode.OutOfDomain));
            Assert.That(result.Diagnostics.Select(x => x.Code), Does.Contain(TerrainCellDiagnosticCode.InvalidSubTile));
            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void CellBudgetStopsLazyEnumeration()
        {
            int moved = 0;
            IEnumerable<TerrainCellInput> Source()
            {
                for (int i = 0; i < 10; i++) { moved++; yield return Cell(i, 0); }
            }
            TerrainTopologyDocument result = TerrainTopologyBuilder.Build(Source(), new TerrainTopologyPolicy(maxCells: 2));
            Assert.That(result.Cells.Count, Is.EqualTo(2));
            Assert.That(moved, Is.EqualTo(3));
            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void NullSourceFailsClosed()
        { Assert.That(TerrainTopologyBuilder.Build(null).IsSuccess, Is.False); }

        [Test]
        public void MovementNodesAndEdgesHaveStableOrdering()
        {
            MovementGraphCandidate graph = new MovementGraphCandidate(
                new[] { new MovementNode(2, new CellCoordinate(1, 0), MovementDomain.Ground, MovementLayer.Ground, PassabilityState.Unknown, 0), new MovementNode(1, new CellCoordinate(0, 0), MovementDomain.Ground, MovementLayer.Ground, PassabilityState.Passable, 0) },
                new[] { new MovementEdgeCandidate(2, 2, 1, 0, 5), new MovementEdgeCandidate(1, 1, 2, 0, 4) });
            Assert.That(graph.Nodes[0].StableId, Is.EqualTo(1));
            Assert.That(graph.Edges[0].StableId, Is.EqualTo(1));
        }

        [Test]
        public void FoundationOccupancyCoversCheckedRectangle()
        {
            FoundationOccupancy foundation = new FoundationOccupancy(new CellCoordinate(2, 3), 2, 2, "synthetic");
            Assert.That(foundation.Cells().ToArray(), Is.EqualTo(new[] { new CellCoordinate(2, 3), new CellCoordinate(3, 3), new CellCoordinate(2, 4), new CellCoordinate(3, 4) }));
        }

        [Test]
        public void OccupancyRejectsStaticDynamicAndReservationCollisions()
        {
            SimulationOccupancy occupancy = new SimulationOccupancy();
            EntityId first = new EntityId(0, 1);
            EntityId second = new EntityId(1, 1);
            Assert.That(occupancy.AddStatic(new StaticOccupancy(new CellCoordinate(0, 0), "wall")), Is.True);
            Assert.That(occupancy.TryAcquireDynamic(new DynamicOccupancy(first, new CellCoordinate(0, 0))), Is.False);
            Assert.That(occupancy.TryReserve(new Reservation(second, new CellCoordinate(1, 0), 4)), Is.True);
            Assert.That(occupancy.TryAcquireDynamic(new DynamicOccupancy(first, new CellCoordinate(1, 0))), Is.False);
        }

        [Test]
        public void OccupancyMoveIsAtomicOnBlockedDestination()
        {
            SimulationOccupancy occupancy = new SimulationOccupancy();
            EntityId first = new EntityId(0, 1);
            Assert.That(occupancy.TryAcquireDynamic(new DynamicOccupancy(first, new CellCoordinate(0, 0))), Is.True);
            occupancy.AddStatic(new StaticOccupancy(new CellCoordinate(1, 0), "blocker"));
            Assert.That(occupancy.TryMove(first, new CellCoordinate(0, 0), new CellCoordinate(1, 0)), Is.False);
            Assert.That(occupancy.IsBlocked(new CellCoordinate(0, 0), first), Is.False);
        }

        [Test]
        public void SpatialIndexInsertRemoveMoveAndDeterministicNeighbors()
        {
            DeterministicSpatialIndex index = new DeterministicSpatialIndex();
            EntityId first = new EntityId(2, 1); EntityId second = new EntityId(1, 1);
            Assert.That(index.Insert(first, new CellCoordinate(0, 0)), Is.True);
            Assert.That(index.Insert(second, new CellCoordinate(1, 0)), Is.True);
            Assert.That(index.QueryNeighbors(new CellCoordinate(0, 0), 1).ToArray(), Is.EqualTo(new[] { second, first }));
            Assert.That(index.Move(first, new CellCoordinate(0, 0), new CellCoordinate(2, 0)), Is.True);
            Assert.That(index.Remove(second, new CellCoordinate(1, 0)), Is.True);
            Assert.That(index.Count, Is.EqualTo(1));
        }

        [Test]
        public void SpatialIndexAllowsMultipleEntitiesPerCellWithStableEntityOrder()
        {
            DeterministicSpatialIndex index = new DeterministicSpatialIndex();
            index.Insert(new EntityId(3, 1), new CellCoordinate(0, 0));
            index.Insert(new EntityId(1, 1), new CellCoordinate(0, 0));
            Assert.That(index.QueryNeighbors(new CellCoordinate(0, 0), 0).Select(x => x.Index).ToArray(), Is.EqualTo(new[] { 1, 3 }));
        }

        [Test]
        public void SpatialQueryRejectsCoordinateOverflowAndResultBudget()
        {
            DeterministicSpatialIndex index = new DeterministicSpatialIndex(1);
            index.Insert(new EntityId(0, 1), new CellCoordinate(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => index.QueryNeighbors(new CellCoordinate(int.MaxValue, 0), 1));
            index.Insert(new EntityId(1, 1), new CellCoordinate(0, 0));
            Assert.Throws<InvalidOperationException>(() => index.QueryNeighbors(new CellCoordinate(0, 0), 0));
        }

        [Test]
        public void ExplicitCapabilityProfilePreservesRawCandidates()
        {
            MovementCapabilityProfile profile = new MovementCapabilityProfile("Subterrannean", "FloatBeach", "raw-locomotor");
            Assert.That(profile.MovementZoneRaw, Is.EqualTo("Subterrannean"));
            Assert.That(profile.SpeedTypeRaw, Is.EqualTo("FloatBeach"));
        }

        [Test]
        public void CoordinateEqualityAndOrderingAreStable()
        {
            var values = new[] { new CellCoordinate(1, 2), new CellCoordinate(0, 4), new CellCoordinate(1, 1) }.OrderBy(x => x).ToArray();
            Assert.That(values, Is.EqualTo(new[] { new CellCoordinate(0, 4), new CellCoordinate(1, 1), new CellCoordinate(1, 2) }));
        }

        [Test]
        public void InvalidFoundationDimensionsAreRejected()
        { Assert.Throws<ArgumentOutOfRangeException>(() => new FoundationOccupancy(new CellCoordinate(0, 0), 0, 1, "bad")); }

        [Test]
        public void GraphDoesNotInferPathfinding()
        {
            MovementGraphCandidate graph = new MovementGraphCandidate(new[] { new MovementNode(1, new CellCoordinate(0, 0), MovementDomain.Ground, MovementLayer.Ground, PassabilityState.Passable, 0) }, Array.Empty<MovementEdgeCandidate>());
            Assert.That(graph.Edges, Is.Empty);
        }
    }
}
