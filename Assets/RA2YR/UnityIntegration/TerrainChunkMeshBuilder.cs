using System;
using System.Collections.Generic;
using RA2YR.Core.Formats.MapTerrain;
using UnityEngine;
using UnityEngine.Rendering;

namespace RA2YR.UnityIntegration
{
    public sealed class TerrainMeshBuildPolicy
    {
        public TerrainMeshBuildPolicy(int maxCells = 4096, int maxVertices = 16384, int maxIndices = 24576)
        {
            if (maxCells < 0 || maxVertices < 0 || maxIndices < 0) throw new ArgumentOutOfRangeException();
            MaxCells = maxCells;
            MaxVertices = maxVertices;
            MaxIndices = maxIndices;
        }

        public int MaxCells { get; }
        public int MaxVertices { get; }
        public int MaxIndices { get; }
    }

    public enum TerrainMeshBuildDiagnosticCode
    {
        InvalidInput,
        BudgetExceeded,
        ArithmeticOverflow,
        ProjectionUnavailable
    }

    public sealed class TerrainMeshBuildDiagnostic
    {
        public TerrainMeshBuildDiagnostic(TerrainMeshBuildDiagnosticCode code, string message)
        {
            Code = code;
            Message = message ?? string.Empty;
        }

        public TerrainMeshBuildDiagnosticCode Code { get; }
        public string Message { get; }
    }

    public sealed class TerrainChunkMeshBuildResult
    {
        internal TerrainChunkMeshBuildResult(Mesh mesh, IEnumerable<TerrainMeshBuildDiagnostic> diagnostics, bool isSuccess)
        {
            Mesh = mesh;
            Diagnostics = new List<TerrainMeshBuildDiagnostic>(diagnostics ?? new TerrainMeshBuildDiagnostic[0]).AsReadOnly();
            IsSuccess = isSuccess;
        }

        public Mesh Mesh { get; }
        public IReadOnlyList<TerrainMeshBuildDiagnostic> Diagnostics { get; }
        public bool IsSuccess { get; }
    }

    /// <summary>
    /// Unity-only adapter. A chunk is represented by one mesh; no tile GameObjects,
    /// palette conversion, materials, or simulation state are created here.
    /// </summary>
    public static class TerrainChunkMeshBuilder
    {
        public static TerrainChunkMeshBuildResult Build(
            TerrainChunkDescriptor chunk,
            IsometricProjectionProfile projection,
            TerrainMeshBuildPolicy policy = null)
        {
            policy = policy ?? new TerrainMeshBuildPolicy();
            var diagnostics = new List<TerrainMeshBuildDiagnostic>();
            if (chunk == null || projection == null)
                return Failure(diagnostics, TerrainMeshBuildDiagnosticCode.InvalidInput, "Chunk and projection are required.");
            if (chunk.Cells.Count > policy.MaxCells)
                return Failure(diagnostics, TerrainMeshBuildDiagnosticCode.BudgetExceeded, "Terrain cell budget exceeded.");
            int expectedVertices;
            int expectedIndices;
            try
            {
                expectedVertices = checked(chunk.Cells.Count * 4);
                expectedIndices = checked(chunk.Cells.Count * 6);
            }
            catch (OverflowException)
            {
                return Failure(diagnostics, TerrainMeshBuildDiagnosticCode.ArithmeticOverflow, "Mesh index arithmetic overflowed.");
            }
            if (expectedVertices > policy.MaxVertices || expectedIndices > policy.MaxIndices)
                return Failure(diagnostics, TerrainMeshBuildDiagnosticCode.BudgetExceeded, "Mesh vertex or index budget exceeded.");

            var vertices = new Vector3[expectedVertices];
            var triangles = new int[expectedIndices];
            try
            {
                for (int i = 0; i < chunk.Cells.Count; i++)
                {
                    TerrainTilePresentationDescriptor cell = chunk.Cells[i];
                    IsometricScreenPoint center = projection.Project(cell.GridX, cell.GridY, cell.LevelRaw, cell.TmpHeightRaw ?? 0);
                    float x = checked((float)center.X);
                    float y = checked((float)center.Y);
                    float halfWidth = checked((float)projection.TileWidth) * 0.5f;
                    float halfHeight = checked((float)projection.TileHeight) * 0.5f;
                    int vertex = checked(i * 4);
                    vertices[vertex] = new Vector3(x - halfWidth, 0f, y);
                    vertices[vertex + 1] = new Vector3(x, 0f, y + halfHeight);
                    vertices[vertex + 2] = new Vector3(x + halfWidth, 0f, y);
                    vertices[vertex + 3] = new Vector3(x, 0f, y - halfHeight);
                    int index = checked(i * 6);
                    triangles[index] = vertex;
                    triangles[index + 1] = vertex + 1;
                    triangles[index + 2] = vertex + 2;
                    triangles[index + 3] = vertex;
                    triangles[index + 4] = vertex + 2;
                    triangles[index + 5] = vertex + 3;
                }
            }
            catch (OverflowException)
            {
                return Failure(diagnostics, TerrainMeshBuildDiagnosticCode.ArithmeticOverflow, "Projection arithmetic overflowed.");
            }
            catch (InvalidOperationException)
            {
                return Failure(diagnostics, TerrainMeshBuildDiagnosticCode.ProjectionUnavailable, "The projection could not provide a screen position.");
            }

            var mesh = new Mesh { name = "TerrainChunk_" + chunk.StableIdentity };
            if (expectedVertices > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return new TerrainChunkMeshBuildResult(mesh, diagnostics, true);
        }

        private static TerrainChunkMeshBuildResult Failure(
            List<TerrainMeshBuildDiagnostic> diagnostics,
            TerrainMeshBuildDiagnosticCode code,
            string message)
        {
            diagnostics.Add(new TerrainMeshBuildDiagnostic(code, message));
            return new TerrainChunkMeshBuildResult(null, diagnostics, false);
        }
    }
}
