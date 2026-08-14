using System;
using System.Collections.Generic;
using UnityEngine;

namespace RA2YR.UnityIntegration
{
    public readonly struct VoxelRenderCell
    {
        public VoxelRenderCell(int x, int y, int z, byte colorIndex) { X = x; Y = y; Z = z; ColorIndex = colorIndex; }
        public int X { get; } public int Y { get; } public int Z { get; } public byte ColorIndex { get; }
    }

    public sealed class VxlMeshBuildPolicy
    {
        public VxlMeshBuildPolicy(int maxVoxels = 65536) { if (maxVoxels < 0) throw new ArgumentOutOfRangeException(nameof(maxVoxels)); MaxVoxels = maxVoxels; }
        public int MaxVoxels { get; }
    }

    public sealed class VxlMeshBuildResult
    {
        internal VxlMeshBuildResult(Mesh mesh, bool success, string diagnostic) { Mesh = mesh; IsSuccess = success; Diagnostic = diagnostic; }
        public Mesh Mesh { get; } public bool IsSuccess { get; } public string Diagnostic { get; }
    }

    public static class VxlExposedFaceMeshBuilder
    {
        private static readonly int[,] Directions = { { 1, 0, 0 }, { -1, 0, 0 }, { 0, 1, 0 }, { 0, -1, 0 }, { 0, 0, 1 }, { 0, 0, -1 } };
        public static VxlMeshBuildResult Build(IReadOnlyList<VoxelRenderCell> cells, VxlMeshBuildPolicy policy = null)
        {
            policy = policy ?? new VxlMeshBuildPolicy();
            if (cells == null) return new VxlMeshBuildResult(null, false, "Voxel cells are required.");
            if (cells.Count > policy.MaxVoxels) return new VxlMeshBuildResult(null, false, "Voxel budget exceeded.");
            var occupied = new HashSet<Vector3Int>(); foreach (VoxelRenderCell cell in cells) occupied.Add(new Vector3Int(cell.X, cell.Y, cell.Z));
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (int i = 0; i < cells.Count; i++)
            {
                VoxelRenderCell cell = cells[i];
                for (int face = 0; face < 6; face++)
                {
                    Vector3Int neighbor = new Vector3Int(cell.X + Directions[face, 0], cell.Y + Directions[face, 1], cell.Z + Directions[face, 2]);
                    if (occupied.Contains(neighbor)) continue;
                    AddFace(vertices, triangles, cell.X, cell.Y, cell.Z, face);
                }
            }
            Mesh mesh = new Mesh { name = "SyntheticVxlExposedFaces" }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds(); return new VxlMeshBuildResult(mesh, true, null);
        }
        private static void AddFace(List<Vector3> vertices, List<int> triangles, int x, int y, int z, int face)
        {
            Vector3[] corners = new Vector3[4]; float x0 = x, x1 = x + 1f, y0 = y, y1 = y + 1f, z0 = z, z1 = z + 1f;
            switch (face)
            {
                case 0: corners = new[] { new Vector3(x1, y0, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), new Vector3(x1, y0, z1) }; break;
                case 1: corners = new[] { new Vector3(x0, y0, z1), new Vector3(x0, y1, z1), new Vector3(x0, y1, z0), new Vector3(x0, y0, z0) }; break;
                case 2: corners = new[] { new Vector3(x0, y1, z0), new Vector3(x0, y1, z1), new Vector3(x1, y1, z1), new Vector3(x1, y1, z0) }; break;
                case 3: corners = new[] { new Vector3(x0, y0, z1), new Vector3(x0, y0, z0), new Vector3(x1, y0, z0), new Vector3(x1, y0, z1) }; break;
                case 4: corners = new[] { new Vector3(x0, y0, z1), new Vector3(x1, y0, z1), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1) }; break;
                default: corners = new[] { new Vector3(x1, y0, z0), new Vector3(x0, y0, z0), new Vector3(x0, y1, z0), new Vector3(x1, y1, z0) }; break;
            }
            int start = vertices.Count; vertices.AddRange(corners); triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2); triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
        }
    }
}
