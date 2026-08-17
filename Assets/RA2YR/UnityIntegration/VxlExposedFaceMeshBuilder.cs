using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Formats.VxlHva;
using UnityEngine;
using UnityEngine.Rendering;

namespace RA2YR.UnityIntegration
{
    public readonly struct VoxelRenderCell
    {
        public VoxelRenderCell(int x, int y, int z, byte colorIndex)
            : this(x, y, z, colorIndex, 0)
        {
        }

        public VoxelRenderCell(int x, int y, int z, byte colorIndex, byte normalIndex)
        {
            X = x;
            Y = y;
            Z = z;
            ColorIndex = colorIndex;
            NormalIndex = normalIndex;
        }

        public int X { get; }
        public int Y { get; }
        public int Z { get; }
        public byte ColorIndex { get; }
        public byte NormalIndex { get; }
    }

    public enum VxlPresentationAxisBasis
    {
        RawXToWorldX_RawYToWorldZ_RawZToWorldY
    }

    public enum VxlPresentationPivotPolicy
    {
        BoundsCenter
    }

    public enum VxlNormalPresentationMode
    {
        /// <summary>
        /// Uses the exposed voxel face geometry for lighting. The raw
        /// NormalIndex and NormalType remain provenance, not a guessed
        /// Westwood table lookup.
        /// </summary>
        DerivedGeometryNormalPresentation,
        EvidenceBackedWestwoodNormalTable
    }

    public sealed class VxlPresentationTransformProfile
    {
        public VxlPresentationTransformProfile(
            float targetHorizontalFootprintCells = 0.85f,
            float targetDepthFootprintCells = 0.85f,
            float targetVerticalFootprintCells = 0.85f,
            float maximumHorizontalFootprintCells = 1.5f,
            float maximumDepthFootprintCells = 1.5f,
            float maximumVerticalFootprintCells = 1.5f,
            VxlPresentationAxisBasis axisBasis = VxlPresentationAxisBasis.RawXToWorldX_RawYToWorldZ_RawZToWorldY,
            VxlPresentationPivotPolicy pivotPolicy = VxlPresentationPivotPolicy.BoundsCenter,
            float maximumRawDimension = 256f,
            VxlNormalPresentationMode normalPresentationMode = VxlNormalPresentationMode.DerivedGeometryNormalPresentation)
        {
            if (targetHorizontalFootprintCells <= 0f || targetDepthFootprintCells <= 0f || targetVerticalFootprintCells <= 0f ||
                maximumHorizontalFootprintCells <= 0f || maximumDepthFootprintCells <= 0f || maximumVerticalFootprintCells <= 0f ||
                targetHorizontalFootprintCells > maximumHorizontalFootprintCells ||
                targetDepthFootprintCells > maximumDepthFootprintCells ||
                targetVerticalFootprintCells > maximumVerticalFootprintCells ||
                maximumRawDimension <= 0f)
                throw new ArgumentOutOfRangeException(nameof(targetHorizontalFootprintCells));
            if (!Enum.IsDefined(typeof(VxlPresentationAxisBasis), axisBasis) || !Enum.IsDefined(typeof(VxlPresentationPivotPolicy), pivotPolicy) ||
                !Enum.IsDefined(typeof(VxlNormalPresentationMode), normalPresentationMode))
                throw new ArgumentOutOfRangeException(nameof(axisBasis));

            TargetHorizontalFootprintCells = targetHorizontalFootprintCells;
            TargetDepthFootprintCells = targetDepthFootprintCells;
            TargetVerticalFootprintCells = targetVerticalFootprintCells;
            MaximumHorizontalFootprintCells = maximumHorizontalFootprintCells;
            MaximumDepthFootprintCells = maximumDepthFootprintCells;
            MaximumVerticalFootprintCells = maximumVerticalFootprintCells;
            AxisBasis = axisBasis;
            PivotPolicy = pivotPolicy;
            MaximumRawDimension = maximumRawDimension;
            NormalPresentationMode = normalPresentationMode;
        }

        public static VxlPresentationTransformProfile Default { get; } = new VxlPresentationTransformProfile();
        public float TargetHorizontalFootprintCells { get; }
        public float TargetDepthFootprintCells { get; }
        public float TargetVerticalFootprintCells { get; }
        public float MaximumHorizontalFootprintCells { get; }
        public float MaximumDepthFootprintCells { get; }
        public float MaximumVerticalFootprintCells { get; }
        public VxlPresentationAxisBasis AxisBasis { get; }
        public VxlPresentationPivotPolicy PivotPolicy { get; }
        public float MaximumRawDimension { get; }
        public VxlNormalPresentationMode NormalPresentationMode { get; }

        public Vector3 ToPresentationBasis(Vector3 raw)
        {
            switch (AxisBasis)
            {
                case VxlPresentationAxisBasis.RawXToWorldX_RawYToWorldZ_RawZToWorldY:
                    return new Vector3(raw.x, raw.z, raw.y);
                default:
                    throw new InvalidOperationException("The VXL presentation axis basis is not supported.");
            }
        }
    }

    public sealed class VxlMeshBuildPolicy
    {
        public VxlMeshBuildPolicy(int maxVoxels = 65536, int maxVertices = 262144, int maxTriangles = 131072)
        {
            if (maxVoxels < 0 || maxVertices < 0 || maxTriangles < 0) throw new ArgumentOutOfRangeException();
            MaxVoxels = maxVoxels;
            MaxVertices = maxVertices;
            MaxTriangles = maxTriangles;
        }

        public int MaxVoxels { get; }
        public int MaxVertices { get; }
        public int MaxTriangles { get; }
    }

    public sealed class VxlPresentationSectionInput
    {
        public VxlPresentationSectionInput(
            string sectionIdentity,
            int sectionOrdinal,
            IEnumerable<VoxelRenderCell> cells,
            Matrix4x4 frameTransform,
            bool hvaApplied,
            byte normalTypeRaw = 0)
        {
            if (string.IsNullOrWhiteSpace(sectionIdentity)) throw new ArgumentException("A section identity is required.", nameof(sectionIdentity));
            if (sectionOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(sectionOrdinal));
            SectionIdentity = sectionIdentity;
            SectionOrdinal = sectionOrdinal;
            Cells = Array.AsReadOnly((cells ?? throw new ArgumentNullException(nameof(cells))).ToArray());
            FrameTransform = frameTransform;
            HvaApplied = hvaApplied;
            NormalTypeRaw = normalTypeRaw;
        }

        public string SectionIdentity { get; }
        public int SectionOrdinal { get; }
        public IReadOnlyList<VoxelRenderCell> Cells { get; }
        public Matrix4x4 FrameTransform { get; }
        public bool HvaApplied { get; }
        public byte NormalTypeRaw { get; }
    }

    public sealed class VxlPresentationBounds
    {
        internal VxlPresentationBounds(Vector3 rawMin, Vector3 rawMax, Vector3 localMin, Vector3 localMax, Vector3 pivotRaw, Vector3 pivotLocal)
        {
            RawMin = rawMin;
            RawMax = rawMax;
            LocalMin = localMin;
            LocalMax = localMax;
            PivotRaw = pivotRaw;
            PivotLocal = pivotLocal;
        }

        public Vector3 RawMin { get; }
        public Vector3 RawMax { get; }
        public Vector3 LocalMin { get; }
        public Vector3 LocalMax { get; }
        public Vector3 PivotRaw { get; }
        public Vector3 PivotLocal { get; }
        public Vector3 RawDimensions => RawMax - RawMin;
        public Vector3 LocalDimensions => LocalMax - LocalMin;
        public float WidthCells => LocalDimensions.x;
        public float HeightCells => LocalDimensions.y;
        public float DepthCells => LocalDimensions.z;
    }

    public sealed class VxlPresentationSectionMesh
    {
        internal VxlPresentationSectionMesh(string sectionIdentity, int sectionOrdinal, Mesh mesh, VxlPresentationBounds bounds, bool hvaApplied, int distinctColorCount, byte normalTypeRaw, VxlNormalPresentationMode normalPresentationMode)
        {
            SectionIdentity = sectionIdentity;
            SectionOrdinal = sectionOrdinal;
            Mesh = mesh;
            Bounds = bounds;
            HvaApplied = hvaApplied;
            DistinctColorCount = distinctColorCount;
            NormalTypeRaw = normalTypeRaw;
            NormalPresentationMode = normalPresentationMode;
        }

        public string SectionIdentity { get; }
        public int SectionOrdinal { get; }
        public Mesh Mesh { get; }
        public VxlPresentationBounds Bounds { get; }
        public bool HvaApplied { get; }
        public int DistinctColorCount { get; }
        public byte NormalTypeRaw { get; }
        public VxlNormalPresentationMode NormalPresentationMode { get; }
    }

    public sealed class VxlPresentationMetrics
    {
        internal VxlPresentationMetrics(VxlPresentationBounds bounds, int sectionCount, int hvaAppliedSectionCount, int vertexCount, int triangleCount, int distinctColorCount)
        {
            Bounds = bounds;
            SectionCount = sectionCount;
            HvaAppliedSectionCount = hvaAppliedSectionCount;
            VertexCount = vertexCount;
            TriangleCount = triangleCount;
            DistinctColorCount = distinctColorCount;
        }

        public VxlPresentationBounds Bounds { get; }
        public int SectionCount { get; }
        public int HvaAppliedSectionCount { get; }
        public int VertexCount { get; }
        public int TriangleCount { get; }
        public int DistinctColorCount { get; }

        public bool IsFiniteAndBounded(VxlPresentationTransformProfile profile)
        {
            if (Bounds == null || profile == null || SectionCount <= 0 || VertexCount <= 0 || TriangleCount <= 0 || DistinctColorCount < 2)
                return false;
            return IsFinite(Bounds.LocalMin) && IsFinite(Bounds.LocalMax) &&
                   Bounds.WidthCells > 0f && Bounds.HeightCells > 0f && Bounds.DepthCells > 0f &&
                   Bounds.WidthCells <= profile.MaximumHorizontalFootprintCells &&
                   Bounds.DepthCells <= profile.MaximumDepthFootprintCells &&
                   Bounds.HeightCells <= profile.MaximumVerticalFootprintCells;
        }

        private static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public sealed class VxlPresentationAsset
    {
        internal VxlPresentationAsset(IEnumerable<VxlPresentationSectionMesh> sections, VxlPresentationMetrics metrics)
        {
            Sections = Array.AsReadOnly((sections ?? throw new ArgumentNullException(nameof(sections))).ToArray());
            Metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        }

        public IReadOnlyList<VxlPresentationSectionMesh> Sections { get; }
        public VxlPresentationMetrics Metrics { get; }
    }

    public sealed class VxlPresentationBuildResult
    {
        internal VxlPresentationBuildResult(VxlPresentationAsset asset, bool success, string diagnostic)
        {
            Asset = asset;
            IsSuccess = success;
            Diagnostic = diagnostic;
        }

        public VxlPresentationAsset Asset { get; }
        public bool IsSuccess { get; }
        public string Diagnostic { get; }
    }

    public sealed class VxlMeshBuildResult
    {
        internal VxlMeshBuildResult(Mesh mesh, bool success, string diagnostic)
        {
            Mesh = mesh;
            IsSuccess = success;
            Diagnostic = diagnostic;
        }

        public Mesh Mesh { get; }
        public bool IsSuccess { get; }
        public string Diagnostic { get; }
    }

    public static class VxlExposedFaceMeshBuilder
    {
        private static readonly int[,] Directions =
        {
            { 1, 0, 0 }, { -1, 0, 0 }, { 0, 1, 0 },
            { 0, -1, 0 }, { 0, 0, 1 }, { 0, 0, -1 }
        };

        public static VxlMeshBuildResult Build(IReadOnlyList<VoxelRenderCell> cells, VxlMeshBuildPolicy policy = null)
        {
            policy = policy ?? new VxlMeshBuildPolicy();
            if (cells == null) return new VxlMeshBuildResult(null, false, "Voxel cells are required.");
            if (cells.Count > policy.MaxVoxels) return new VxlMeshBuildResult(null, false, "Voxel budget exceeded.");
            Mesh mesh;
            string diagnostic;
            try { mesh = BuildRawMesh(cells, Vector3.zero, Matrix4x4.identity, 1f, null, policy, out diagnostic); }
            catch (InvalidOperationException error) { return new VxlMeshBuildResult(null, false, error.Message); }
            return new VxlMeshBuildResult(mesh, mesh != null, diagnostic);
        }

        /// <summary>
        /// Builds presentation meshes from a display-space palette table. The
        /// caller must convert the authoritative PAL raw 0..63 channels through
        /// the configured display profile before entering this Unity boundary.
        /// </summary>
        public static VxlPresentationBuildResult Build(IReadOnlyList<VxlPresentationSectionInput> sections, VxlPresentationTransformProfile profile, byte[] displayPalette, VxlMeshBuildPolicy policy = null)
        {
            policy = policy ?? new VxlMeshBuildPolicy();
            if (sections == null || sections.Count == 0) return Failure("At least one VXL section is required.");
            if (profile == null) return Failure("A VXL presentation transform profile is required.");
            if (profile.NormalPresentationMode != VxlNormalPresentationMode.DerivedGeometryNormalPresentation)
                return Failure("The requested Westwood normal table is not available in this presentation build.");
            if (displayPalette == null || displayPalette.Length < 256 * 3) return Failure("A complete VXL display palette is required.");

            int totalCells = 0;
            var transformedBounds = new PresentationPointBounds();
            var sectionPoints = new List<SectionPointSet>(sections.Count);
            for (int sectionIndex = 0; sectionIndex < sections.Count; sectionIndex++)
            {
                VxlPresentationSectionInput section = sections[sectionIndex];
                if (section == null || section.Cells.Count == 0) return Failure("A VXL section has no voxels.");
                totalCells = checked(totalCells + section.Cells.Count);
                if (totalCells > policy.MaxVoxels) return Failure("Voxel budget exceeded.");
                var points = new SectionPointSet();
                foreach (VoxelRenderCell cell in section.Cells)
                {
                    for (int corner = 0; corner < 8; corner++)
                    {
                        Vector3 raw = new Vector3(cell.X + ((corner & 1) == 0 ? 0f : 1f), cell.Y + ((corner & 2) == 0 ? 0f : 1f), cell.Z + ((corner & 4) == 0 ? 0f : 1f));
                        Vector3 transformed = section.FrameTransform.MultiplyPoint3x4(raw);
                        points.Add(transformed);
                        transformedBounds.Add(transformed);
                    }
                }
                sectionPoints.Add(points);
            }

            if (!transformedBounds.IsValid) return Failure("VXL bounds are empty or non-finite.");
            Vector3 pivot = profile.PivotPolicy == VxlPresentationPivotPolicy.BoundsCenter ? (transformedBounds.Min + transformedBounds.Max) * 0.5f : Vector3.zero;
            Vector3 rawDimensions = transformedBounds.Max - transformedBounds.Min;
            if (!IsFinite(rawDimensions.x) || !IsFinite(rawDimensions.y) || !IsFinite(rawDimensions.z) ||
                rawDimensions.x > profile.MaximumRawDimension || rawDimensions.y > profile.MaximumRawDimension || rawDimensions.z > profile.MaximumRawDimension)
                return Failure("VXL raw presentation dimension exceeds its configured budget.");
            Vector3 basisDimensions = profile.ToPresentationBasis(rawDimensions);
            float scale = Mathf.Min(
                profile.TargetHorizontalFootprintCells / Mathf.Max(basisDimensions.x, 0.0001f),
                profile.TargetVerticalFootprintCells / Mathf.Max(basisDimensions.y, 0.0001f),
                profile.TargetDepthFootprintCells / Mathf.Max(basisDimensions.z, 0.0001f));
            if (!IsFinite(scale) || scale <= 0f) return Failure("VXL presentation scale is invalid.");

            var builtSections = new List<VxlPresentationSectionMesh>(sections.Count);
            int vertexCount = 0;
            int triangleCount = 0;
            int hvaCount = 0;
            var allColors = new HashSet<Color32>();
            for (int sectionIndex = 0; sectionIndex < sections.Count; sectionIndex++)
            {
                VxlPresentationSectionInput section = sections[sectionIndex];
                SectionPointSet points = sectionPoints[sectionIndex];
                Vector3 localMin = Vector3.positiveInfinity;
                Vector3 localMax = Vector3.negativeInfinity;
                foreach (Vector3 transformed in points.Points)
                {
                    Vector3 local = profile.ToPresentationBasis(transformed - pivot) * scale;
                    Include(ref localMin, ref localMax, local);
                }
                VxlPresentationBounds bounds = new VxlPresentationBounds(points.Min, points.Max, localMin, localMax, pivot, Vector3.zero);
                Mesh mesh;
                string diagnostic;
                try { mesh = BuildRawMesh(section.Cells, pivot, section.FrameTransform, scale, profile, policy, out diagnostic, displayPalette, allColors); }
                catch (InvalidOperationException error) { DestroyMeshes(builtSections); return Failure(error.Message); }
                if (mesh == null)
                {
                    DestroyMeshes(builtSections);
                    return Failure(diagnostic ?? "VXL section mesh generation failed.");
                }
                vertexCount = checked(vertexCount + mesh.vertexCount);
                triangleCount = checked(triangleCount + mesh.triangles.Length / 3);
                if (vertexCount > policy.MaxVertices || triangleCount > policy.MaxTriangles)
                {
                    UnityEngine.Object.DestroyImmediate(mesh);
                    DestroyMeshes(builtSections);
                    return Failure("VXL mesh budget exceeded.");
                }
                if (section.HvaApplied) hvaCount++;
                int distinctColors = CountDistinctColors(mesh.colors32);
                builtSections.Add(new VxlPresentationSectionMesh(section.SectionIdentity, section.SectionOrdinal, mesh, bounds, section.HvaApplied, distinctColors, section.NormalTypeRaw, profile.NormalPresentationMode));
            }

            VxlPresentationBounds totalBounds = new VxlPresentationBounds(
                transformedBounds.Min,
                transformedBounds.Max,
                profile.ToPresentationBasis(transformedBounds.Min - pivot) * scale,
                profile.ToPresentationBasis(transformedBounds.Max - pivot) * scale,
                pivot,
                Vector3.zero);
            VxlPresentationMetrics metrics = new VxlPresentationMetrics(totalBounds, builtSections.Count, hvaCount, vertexCount, triangleCount, allColors.Count);
            if (!metrics.IsFiniteAndBounded(profile))
            {
                DestroyMeshes(builtSections);
                return Failure("VXL presentation bounds or palette-color sanity gate failed.");
            }
            return new VxlPresentationBuildResult(new VxlPresentationAsset(builtSections, metrics), true, null);
        }

        public static IReadOnlyList<VxlPresentationSectionInput> CreateSectionInputs(VxlDocumentRaw document, HvaDocumentRaw hva, VxlHvaBindingResult binding, int maxVoxels = 65536)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (maxVoxels < 0) throw new ArgumentOutOfRangeException(nameof(maxVoxels));
            var values = new List<VxlPresentationSectionInput>();
            int total = 0;
            foreach (VxlSectionRaw section in document.Sections)
            {
                var cells = new List<VoxelRenderCell>();
                foreach (VxlColumnRaw column in section.Columns)
                {
                    int z = 0;
                    foreach (VxlSpanChunkRaw chunk in column.Chunks)
                    {
                        z = checked(z + chunk.Skip);
                        foreach (VxlVoxelRaw voxel in chunk.Voxels)
                        {
                            total = checked(total + 1);
                            if (total > maxVoxels) throw new InvalidOperationException("VXL voxel budget exceeded.");
                            cells.Add(new VoxelRenderCell(column.X, column.Y, z, voxel.ColorIndex, voxel.NormalIndex));
                            z = checked(z + 1);
                        }
                    }
                }
                Matrix4x4 transform = Matrix4x4.identity;
                bool hvaApplied = false;
                if (hva != null && binding != null)
                {
                    VxlHvaBinding matching = binding.Bindings.FirstOrDefault(value => value.VxlSectionOrdinal == section.Header.Ordinal);
                    if (matching != null)
                    {
                        HvaRawTransform3x4 raw = hva.GetCandidate(0, matching.HvaSectionOrdinal, HvaTransformRecordOrder.FrameMajor);
                        transform = ToMatrix(raw);
                        hvaApplied = true;
                    }
                }
                values.Add(new VxlPresentationSectionInput(
                    string.IsNullOrWhiteSpace(section.Header.NameCandidate) ? "section-" + section.Header.Ordinal : section.Header.NameCandidate,
                    section.Header.Ordinal,
                    cells,
                    transform,
                    hvaApplied,
                    section.Tailer.NormalTypeRaw));
            }
            return values.AsReadOnly();
        }

        private static VxlPresentationBuildResult Failure(string diagnostic) => new VxlPresentationBuildResult(null, false, diagnostic ?? "VXL presentation build failed.");

        private static Mesh BuildRawMesh(IReadOnlyList<VoxelRenderCell> cells, Vector3 pivot, Matrix4x4 frameTransform, float scale, VxlPresentationTransformProfile profile, VxlMeshBuildPolicy policy, out string diagnostic, byte[] displayPalette = null, HashSet<Color32> aggregateColors = null)
        {
            diagnostic = null;
            var occupied = new HashSet<Vector3Int>();
            foreach (VoxelRenderCell cell in cells) occupied.Add(new Vector3Int(cell.X, cell.Y, cell.Z));
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var colors = new List<Color32>();
            var normals = new List<Vector3>();
            for (int i = 0; i < cells.Count; i++)
            {
                VoxelRenderCell cell = cells[i];
                for (int face = 0; face < 6; face++)
                {
                    Vector3Int neighbor = new Vector3Int(cell.X + Directions[face, 0], cell.Y + Directions[face, 1], cell.Z + Directions[face, 2]);
                    if (occupied.Contains(neighbor)) continue;
                    Color32 color = displayPalette == null ? new Color32(255, 255, 255, 255) : DisplayPaletteColor(displayPalette, cell.ColorIndex);
                    AddFace(vertices, triangles, colors, normals, cell.X, cell.Y, cell.Z, face, pivot, frameTransform, scale, profile, color);
                    aggregateColors?.Add(color);
                    if (vertices.Count > policy.MaxVertices || triangles.Count / 3 > policy.MaxTriangles)
                    {
                        diagnostic = "VXL mesh budget exceeded.";
                        return null;
                    }
                }
            }
            if (vertices.Count == 0)
            {
                diagnostic = "VXL presentation contains no exposed faces.";
                return null;
            }
            Mesh mesh = new Mesh { name = "ExternalLegacyVxlSection" };
            if (vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetColors(colors);
            mesh.SetNormals(normals);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddFace(List<Vector3> vertices, List<int> triangles, List<Color32> colors, List<Vector3> normals, int x, int y, int z, int face, Vector3 pivot, Matrix4x4 frameTransform, float scale, VxlPresentationTransformProfile profile, Color32 color)
        {
            float x0 = x, x1 = x + 1f, y0 = y, y1 = y + 1f, z0 = z, z1 = z + 1f;
            Vector3[] corners;
            switch (face)
            {
                case 0: corners = new[] { new Vector3(x1, y0, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), new Vector3(x1, y0, z1) }; break;
                case 1: corners = new[] { new Vector3(x0, y0, z1), new Vector3(x0, y1, z1), new Vector3(x0, y1, z0), new Vector3(x0, y0, z0) }; break;
                case 2: corners = new[] { new Vector3(x0, y1, z0), new Vector3(x0, y1, z1), new Vector3(x1, y1, z1), new Vector3(x1, y1, z0) }; break;
                case 3: corners = new[] { new Vector3(x0, y0, z1), new Vector3(x0, y0, z0), new Vector3(x1, y0, z0), new Vector3(x1, y0, z1) }; break;
                case 4: corners = new[] { new Vector3(x0, y0, z1), new Vector3(x1, y0, z1), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1) }; break;
                default: corners = new[] { new Vector3(x1, y0, z0), new Vector3(x0, y0, z0), new Vector3(x0, y1, z0), new Vector3(x1, y1, z0) }; break;
            }
            int start = vertices.Count;
            Vector3 rawNormal = new Vector3(Directions[face, 0], Directions[face, 1], Directions[face, 2]);
            Vector3 transformedNormal = frameTransform.inverse.transpose.MultiplyVector(rawNormal);
            if (profile != null) transformedNormal = profile.ToPresentationBasis(transformedNormal);
            float normalLength = transformedNormal.magnitude;
            if (float.IsNaN(normalLength) || float.IsInfinity(normalLength) || normalLength <= 0.000001f)
                throw new InvalidOperationException("VXL normal presentation produced a non-finite or zero normal.");
            transformedNormal /= normalLength;
            foreach (Vector3 corner in corners)
            {
                Vector3 transformed = frameTransform.MultiplyPoint3x4(corner);
                Vector3 local = profile == null ? (transformed - pivot) * scale : profile.ToPresentationBasis(transformed - pivot) * scale;
                vertices.Add(local);
                colors.Add(color);
                normals.Add(transformedNormal);
            }
            // The configured raw-to-world basis swaps two axes and therefore
            // has negative handedness. Reverse winding so outward normals and
            // back-face culling remain consistent after that explicit basis.
            bool reverseWinding = profile != null && profile.AxisBasis == VxlPresentationAxisBasis.RawXToWorldX_RawYToWorldZ_RawZToWorldY;
            if (reverseWinding)
            {
                triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
                triangles.Add(start); triangles.Add(start + 3); triangles.Add(start + 2);
            }
            else
            {
                triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
            }
        }

        private static Color32 DisplayPaletteColor(byte[] displayPalette, byte index)
        {
            int offset = index * 3;
            return new Color32(displayPalette[offset], displayPalette[offset + 1], displayPalette[offset + 2], 255);
        }

        private static int CountDistinctColors(Color32[] colors) => colors == null ? 0 : new HashSet<Color32>(colors).Count;

        private static void Include(ref Vector3 min, ref Vector3 max, Vector3 value)
        {
            min = Vector3.Min(min, value);
            max = Vector3.Max(max, value);
        }

        private static void DestroyMeshes(IEnumerable<VxlPresentationSectionMesh> sections)
        {
            foreach (VxlPresentationSectionMesh section in sections)
                if (section.Mesh != null) UnityEngine.Object.DestroyImmediate(section.Mesh);
        }

        private static Matrix4x4 ToMatrix(HvaRawTransform3x4 raw)
        {
            if (raw == null || raw.RawBits.Count != 12) throw new InvalidOperationException("HVA frame transform is incomplete.");
            float[] values = raw.RawBits.Select(value => BitConverter.Int32BitsToSingle(unchecked((int)value))).ToArray();
            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.m00 = values[0]; matrix.m01 = values[1]; matrix.m02 = values[2]; matrix.m03 = values[3];
            matrix.m10 = values[4]; matrix.m11 = values[5]; matrix.m12 = values[6]; matrix.m13 = values[7];
            matrix.m20 = values[8]; matrix.m21 = values[9]; matrix.m22 = values[10]; matrix.m23 = values[11];
            return matrix;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private sealed class SectionPointSet
        {
            public List<Vector3> Points { get; } = new List<Vector3>();
            public Vector3 Min { get; private set; } = Vector3.positiveInfinity;
            public Vector3 Max { get; private set; } = Vector3.negativeInfinity;
            public void Add(Vector3 point)
            {
                Points.Add(point);
                Min = Vector3.Min(Min, point);
                Max = Vector3.Max(Max, point);
            }
        }

        private sealed class PresentationPointBounds
        {
            public Vector3 Min = Vector3.positiveInfinity;
            public Vector3 Max = Vector3.negativeInfinity;
            public bool IsValid { get; private set; }
            public void Add(Vector3 point)
            {
                if (!IsFinite(point)) return;
                Min = Vector3.Min(Min, point);
                Max = Vector3.Max(Max, point);
                IsValid = true;
            }

            private static bool IsFinite(Vector3 value)
            {
                return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
                       !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
                       !float.IsNaN(value.z) && !float.IsInfinity(value.z);
            }
        }
    }
}
