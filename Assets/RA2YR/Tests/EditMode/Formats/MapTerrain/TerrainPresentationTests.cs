using System;
using NUnit.Framework;
using RA2YR.Core.Formats.MapTerrain;
using RA2YR.Core.Formats.VxlHva;
using RA2YR.UnityIntegration;
using UnityEngine;

namespace RA2YR.Tests.EditMode.Formats.MapTerrain
{
    public sealed class TerrainPresentationTests
    {
        private static TerrainTilePresentationDescriptor Cell(long x, long y, long ordinal, PaletteBindingDescriptor? palette = null)
        {
            return new TerrainTilePresentationDescriptor(x, y, 42, 3, 2, null, null, null, 1, 7, palette, ordinal);
        }

        [Test]
        public void ProjectionUsesExplicitXMinusYProfile()
        {
            var profile = new IsometricProjectionProfile(10, 20, 2, 4, 1);
            IsometricScreenPoint point = profile.Project(3, 1, 0, 0);
            Assert.AreEqual(12, point.X);
            Assert.AreEqual(28, point.Y);
        }

        [Test]
        public void ProjectionAxisOrderRemainsExplicit()
        {
            var profile = new IsometricProjectionProfile(0, 0, 2, 2, 0, TerrainProjectionAxisOrder.YMinusX);
            IsometricScreenPoint point = profile.Project(1, 3, 0, 0);
            Assert.AreEqual(2, point.X);
            Assert.AreEqual(4, point.Y);
        }

        [Test]
        public void ProjectionInverseOnlyAcceptsExactCandidate()
        {
            var profile = new IsometricProjectionProfile(0, 0, 2, 2, 0);
            IsometricScreenPoint point = profile.Project(4, 2, 0, 0);
            IsometricGridPoint candidate;
            Assert.IsTrue(profile.TryInverse(point, 0, 0, out candidate));
            Assert.AreEqual(4, candidate.X);
            Assert.AreEqual(2, candidate.Y);
        }

        [Test]
        public void ProjectionRoundingIsExplicit()
        {
            var floor = new IsometricProjectionProfile(0, 0, 3, 3, 0, TerrainProjectionAxisOrder.XMinusY, TerrainProjectionRounding.Floor);
            var ceiling = new IsometricProjectionProfile(0, 0, 3, 3, 0, TerrainProjectionAxisOrder.XMinusY, TerrainProjectionRounding.Ceiling);
            Assert.AreEqual(1, floor.Project(1, 0, 0, 0).X);
            Assert.AreEqual(2, ceiling.Project(1, 0, 0, 0).X);
        }

        [Test]
        public void ProjectionCheckedArithmeticDoesNotWrap()
        {
            var profile = new IsometricProjectionProfile(0, 0, long.MaxValue, 2, 0);
            Assert.Throws<OverflowException>(() => profile.Project(2, 0, 0, 0));
        }

        [Test]
        public void FixedProjectionPreservesOddTileHalfUnits()
        {
            var profile = new IsometricProjectionProfile(0, 0, 2, 1, 0);
            Assert.AreEqual(new IsometricFixedPoint(0, 0), profile.ProjectFixed(0, 0, 0, 0));
            Assert.AreEqual(new IsometricFixedPoint(2, 1), profile.ProjectFixed(1, 0, 0, 0));
            Assert.AreEqual(new IsometricFixedPoint(-2, 1), profile.ProjectFixed(0, 1, 0, 0));
        }

        [Test]
        public void AdjacentOddHeightDiamondsShareExactEdges()
        {
            var profile = new IsometricProjectionProfile(0, 0, 2, 1, 0);
            TerrainPresentationBuildResult composed = TerrainPresentationComposer.Build(new[] { Cell(0, 0, 0), Cell(1, 0, 1), Cell(0, 1, 2) });
            TerrainChunkMeshBuildResult result = TerrainChunkMeshBuilder.Build(composed.Chunks[0], profile);
            try
            {
                Assert.IsTrue(result.IsSuccess, result.Diagnostics.Count == 0 ? string.Empty : result.Diagnostics[0].Message);
                Vector3[] vertices = result.Mesh.vertices;
                Assert.AreEqual(vertices[1], vertices[4]);
                Assert.AreEqual(vertices[2], vertices[7]);
                Assert.AreEqual(vertices[0], vertices[11]);
                Assert.AreEqual(vertices[1], vertices[10]);
            }
            finally { if (result.Mesh != null) UnityEngine.Object.DestroyImmediate(result.Mesh); }
        }

        [Test]
        public void FourByFourOddHeightTerrainHasAllCellsAndNoAdjacencyGaps()
        {
            var cells = new System.Collections.Generic.List<TerrainTilePresentationDescriptor>();
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    cells.Add(Cell(x, y, y * 4 + x));
            TerrainPresentationBuildResult composed = TerrainPresentationComposer.Build(cells);
            TerrainChunkMeshBuildResult result = TerrainChunkMeshBuilder.Build(composed.Chunks[0], new IsometricProjectionProfile(0, 0, 2, 1, 0));
            try
            {
                Assert.IsTrue(result.IsSuccess);
                Assert.AreEqual(16 * 4, result.Mesh.vertexCount);
                Assert.IsTrue(result.Mesh.bounds.size.x > 0f && result.Mesh.bounds.size.z > 0f);
                Vector3[] vertices = result.Mesh.vertices;
                for (int index = 0; index < vertices.Length; index++)
                    Assert.IsFalse(float.IsNaN(vertices[index].x) || float.IsInfinity(vertices[index].x) || float.IsNaN(vertices[index].z) || float.IsInfinity(vertices[index].z));
            }
            finally { if (result.Mesh != null) UnityEngine.Object.DestroyImmediate(result.Mesh); }
        }

        [Test]
        public void NearestInverseUsesTheSameFixedProjectionContract()
        {
            var profile = new IsometricProjectionProfile(8, 9, 2, 1, 0);
            IsometricFixedPoint fixedPoint = profile.ProjectFixed(3, 2, 0, 0);
            IsometricGridPoint candidate;
            Assert.IsTrue(profile.TryInverseNearest(fixedPoint.LogicalX + 0.12, fixedPoint.LogicalY - 0.12, 0, 0, out candidate));
            Assert.AreEqual(3, candidate.X);
            Assert.AreEqual(2, candidate.Y);
        }

        [Test]
        public void DescriptorRetainsRawTmpAndTileFields()
        {
            TerrainTilePresentationDescriptor descriptor = new TerrainTilePresentationDescriptor(4, 5, 0x10203040, 0xab, 0xcd, 0xef, 0x11, 0x22, 3, 9, null, 17);
            Assert.AreEqual(4, descriptor.GridX);
            Assert.AreEqual(5, descriptor.GridY);
            Assert.AreEqual(0x10203040, descriptor.TileLogicalIdentity);
            Assert.AreEqual(0xab, descriptor.SubTileRaw);
            Assert.AreEqual(0xcd, descriptor.LevelRaw);
            Assert.AreEqual(0xef, descriptor.TmpHeightRaw);
            Assert.AreEqual(3, descriptor.TileSetIndex);
            Assert.AreEqual(9, descriptor.LocalTileOrdinal);
        }

        [Test]
        public void UnresolvedPaletteDoesNotFallback()
        {
            var descriptor = Cell(0, 0, 0, new PaletteBindingDescriptor("iso", PaletteConversionProfile.Unresolved));
            Assert.IsFalse(descriptor.IsPaletteBound);
        }

        [Test]
        public void ComposerUsesStableChunkIdentityAndOrdering()
        {
            var policy = new TerrainPresentationPolicy(16, 16, 10);
            TerrainPresentationBuildResult result = TerrainPresentationComposer.Build(new[] { Cell(16, 0, 1), Cell(0, 16, 2), Cell(0, 0, 0) }, policy);
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(3, result.Chunks.Count);
            Assert.AreEqual("0:0:16:16", result.Chunks[0].StableIdentity);
            Assert.AreEqual("1:0:16:16", result.Chunks[1].StableIdentity);
            Assert.AreEqual("0:1:16:16", result.Chunks[2].StableIdentity);
        }

        [Test]
        public void ComposerFailsClosedAtCellBudget()
        {
            TerrainPresentationBuildResult result = TerrainPresentationComposer.Build(new[] { Cell(0, 0, 0), Cell(1, 0, 1) }, new TerrainPresentationPolicy(maxCells: 1));
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(TerrainPresentationDiagnosticCode.CellBudgetExceeded, result.Diagnostics[0].Code);
        }

        [Test]
        public void ComposerStopsOnNullSourceDescriptor()
        {
            TerrainPresentationBuildResult result = TerrainPresentationComposer.Build(new TerrainTilePresentationDescriptor[] { null, Cell(0, 0, 1) });
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, result.Chunks.Count);
        }

        [Test]
        public void ComposerDiagnosticBudgetSuppressesWithoutFailOpen()
        {
            TerrainPresentationBuildResult result = TerrainPresentationComposer.Build(new TerrainTilePresentationDescriptor[] { null }, new TerrainPresentationPolicy(maxDiagnostics: 0));
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(1, result.Execution.SuppressedDiagnosticCount);
        }

        [Test]
        public void ChunkCellsAreOrderedBySourceOrdinal()
        {
            TerrainPresentationBuildResult result = TerrainPresentationComposer.Build(new[] { Cell(0, 0, 2), Cell(0, 0, 0), Cell(0, 0, 1) });
            Assert.AreEqual(0, result.Chunks[0].Cells[0].SourceOrdinal);
            Assert.AreEqual(1, result.Chunks[0].Cells[1].SourceOrdinal);
            Assert.AreEqual(2, result.Chunks[0].Cells[2].SourceOrdinal);
        }

        [Test]
        public void MeshBuilderCreatesOneMeshForManyCells()
        {
            TerrainPresentationBuildResult composed = TerrainPresentationComposer.Build(new[] { Cell(0, 0, 0), Cell(1, 0, 1) });
            var profile = new IsometricProjectionProfile(0, 0, 2, 2, 1);
            TerrainChunkMeshBuildResult result = TerrainChunkMeshBuilder.Build(composed.Chunks[0], profile);
            try
            {
                Assert.IsTrue(result.IsSuccess);
                Assert.IsNotNull(result.Mesh);
                Assert.AreEqual(8, result.Mesh.vertexCount);
                Assert.AreEqual(12, result.Mesh.triangles.Length);
            }
            finally
            {
                if (result.Mesh != null) UnityEngine.Object.DestroyImmediate(result.Mesh);
            }
        }

        [Test]
        public void MeshBuilderRejectsCellBudgetBeforeAllocation()
        {
            TerrainPresentationBuildResult composed = TerrainPresentationComposer.Build(new[] { Cell(0, 0, 0), Cell(1, 0, 1) });
            TerrainChunkMeshBuildResult result = TerrainChunkMeshBuilder.Build(composed.Chunks[0], new IsometricProjectionProfile(0, 0, 2, 2, 0), new TerrainMeshBuildPolicy(maxCells: 1));
            Assert.IsFalse(result.IsSuccess);
            Assert.IsNull(result.Mesh);
            Assert.AreEqual(TerrainMeshBuildDiagnosticCode.BudgetExceeded, result.Diagnostics[0].Code);
        }

        [Test]
        public void MeshBuilderDoesNotCreateTileGameObjects()
        {
            TerrainPresentationBuildResult composed = TerrainPresentationComposer.Build(new[] { Cell(0, 0, 0) });
            TerrainChunkMeshBuildResult result = TerrainChunkMeshBuilder.Build(composed.Chunks[0], new IsometricProjectionProfile(0, 0, 2, 2, 0));
            try { Assert.IsNotNull(result.Mesh); Assert.AreEqual(1, result.Mesh.subMeshCount); }
            finally { if (result.Mesh != null) UnityEngine.Object.DestroyImmediate(result.Mesh); }
        }

        [Test]
        public void CoreProjectionAssemblyHasNoUnityDependency()
        {
            Assert.IsTrue(typeof(IsometricProjectionProfile).Assembly.GetName().Name == "RA2YR.Core");
            Assert.IsFalse(typeof(IsometricProjectionProfile).Assembly.FullName.Contains("UnityEngine"));
        }

        [Test]
        public void ProfileRejectsInvalidDimensions()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new IsometricProjectionProfile(0, 0, 0, 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new IsometricProjectionProfile(0, 0, 1, 1, -1));
        }

        [Test]
        public void MeshBuilderKeepsRawTileSemanticsOutOfGeometryContract()
        {
            TerrainTilePresentationDescriptor descriptor = new TerrainTilePresentationDescriptor(0, 0, 7, 0x55, 0x66, 0x77, null, null, 0, 0, null, 0);
            Assert.AreEqual(0x55, descriptor.SubTileRaw);
            Assert.AreEqual(0x66, descriptor.LevelRaw);
            Assert.AreEqual(0x77, descriptor.TmpHeightRaw);
        }
    }
}
