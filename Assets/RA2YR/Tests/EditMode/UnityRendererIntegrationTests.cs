using System;
using NUnit.Framework;
using RA2YR.Core.Formats.MapTerrain;
using RA2YR.Presentation;
using RA2YR.UnityIntegration;
using UnityEngine;
using UnityEngine.Rendering;

namespace RA2YR.Tests.EditMode
{
    public sealed class UnityRendererIntegrationTests
    {
        private static ObjectVisualPresentationDescriptor Object(string id, long ordinal = 0, PresentationRenderPass pass = PresentationRenderPass.GroundObject)
        {
            return new ObjectVisualPresentationDescriptor(new VisualAssetId(id), PresentationObjectFamily.GroundActor, pass, PresentationElevationLayer.Ground, new PresentationAnchor(PresentationAnchorKind.LogicalGround, 1, 2, 0), new PresentationBounds(PresentationBoundsKind.Visual, -1, -1, 1, 1), new PresentationBounds(PresentationBoundsKind.ConservativeCulling, -2, -2, 2, 2), id, ordinal, 1, 2);
        }

        private static Material Material()
        {
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            return shader == null ? null : new Material(shader);
        }

        [Test] public void CacheHitReusesResource()
        { VisualAssetCache cache = new VisualAssetCache(2, false); Texture2D texture = new Texture2D(1, 1); VisualAssetCacheKey key = new VisualAssetCacheKey(new VisualAssetId("unit"), "provider", "pal", "v", VisualAssetRepresentationKind.IndexedTexture, 0, "none"); try { Assert.IsTrue(cache.Put(key, texture)); UnityEngine.Object resource; Assert.IsTrue(cache.TryGet(key, out resource)); Assert.AreSame(texture, resource); } finally { UnityEngine.Object.DestroyImmediate(texture); } }

        [Test] public void CacheMissDoesNotCreateResource()
        { VisualAssetCache cache = new VisualAssetCache(2, false); UnityEngine.Object resource; Assert.IsFalse(cache.TryGet(new VisualAssetCacheKey(new VisualAssetId("missing"), "", "", "", VisualAssetRepresentationKind.Placeholder, 0, ""), out resource)); Assert.IsNull(resource); }

        [Test] public void CacheEvictsOldestAtBoundedCapacity()
        { VisualAssetCache cache = new VisualAssetCache(1, false); Texture2D first = new Texture2D(1, 1); Texture2D second = new Texture2D(1, 1); try { Assert.IsTrue(cache.Put(new VisualAssetCacheKey(new VisualAssetId("a"), "", "", "", VisualAssetRepresentationKind.IndexedTexture, 0, ""), first)); Assert.IsTrue(cache.Put(new VisualAssetCacheKey(new VisualAssetId("b"), "", "", "", VisualAssetRepresentationKind.IndexedTexture, 0, ""), second)); Assert.AreEqual(1, cache.Count); Assert.AreEqual(1, cache.EvictionCount); } finally { UnityEngine.Object.DestroyImmediate(first); UnityEngine.Object.DestroyImmediate(second); } }

        [Test] public void ZeroCapacityCacheFailsClosed()
        { VisualAssetCache cache = new VisualAssetCache(0, false); Texture2D texture = new Texture2D(1, 1); try { Assert.IsFalse(cache.Put(new VisualAssetCacheKey(new VisualAssetId("unit"), "", "", "", VisualAssetRepresentationKind.Placeholder, 0, ""), texture)); Assert.AreEqual(0, cache.Count); } finally { UnityEngine.Object.DestroyImmediate(texture); } }

        [Test] public void CacheKeySeparatesPaletteAndRemapProfiles()
        { VisualAssetCacheKey a = new VisualAssetCacheKey(new VisualAssetId("unit"), "p", "pal-a", "v", VisualAssetRepresentationKind.IndexedTexture, 0, "red"); VisualAssetCacheKey b = new VisualAssetCacheKey(new VisualAssetId("unit"), "p", "pal-b", "v", VisualAssetRepresentationKind.IndexedTexture, 0, "blue"); Assert.AreNotEqual(a, b); }

        [Test] public void IndexedTextureUploadPreservesBoundedDimensions()
        { IndexedTextureResource resource = IndexedTextureFactory.Build(new byte[] { 0, 1, 2, 3 }, 2, 2, null, PaletteDisplayProfile.Unresolved); try { Assert.AreEqual(2, resource.Indexed.width); Assert.AreEqual(2, resource.Indexed.height); Assert.IsNull(resource.PaletteLookup); } finally { resource.Destroy(); } }

        [Test] public void PaletteLookupUsesExplicitDisplayProfile()
        { byte[] palette = new byte[768]; palette[0] = 63; IndexedTextureResource resource = IndexedTextureFactory.Build(new byte[] { 0 }, 1, 1, palette, PaletteDisplayProfile.ShiftLeftTwo); try { Assert.IsNotNull(resource.PaletteLookup); Assert.AreEqual(256, resource.PaletteLookup.width); Assert.AreEqual(1, resource.PaletteLookup.height); } finally { resource.Destroy(); } }

        [Test] public void UnresolvedPaletteProfileDoesNotInventLookup()
        { IndexedTextureResource resource = IndexedTextureFactory.Build(new byte[] { 0 }, 1, 1, new byte[768], PaletteDisplayProfile.Unresolved); try { Assert.IsNull(resource.PaletteLookup); } finally { resource.Destroy(); } }

        [Test] public void IndexedUploadRejectsLengthMismatchBeforeTextureAllocation()
        { Assert.Throws<ArgumentException>(() => IndexedTextureFactory.Build(new byte[] { 0 }, 2, 1, null, PaletteDisplayProfile.Unresolved)); }

        [Test] public void IndexedUploadRejectsInvalidRawPalette()
        { byte[] palette = new byte[768]; palette[0] = 64; Assert.Throws<ArgumentOutOfRangeException>(() => IndexedTextureFactory.Build(new byte[] { 0 }, 1, 1, palette, PaletteDisplayProfile.ShiftLeftTwo)); }

        [Test] public void UnsupportedSpriteFrameUsesExplicitPlaceholder()
        { IndexedSpriteUploadResult result = IndexedSpriteRenderer.Upload(false, null, 0, 0, null, PaletteDisplayProfile.Unresolved); try { Assert.IsTrue(result.IsPlaceholder); Assert.IsNotNull(result.Resource); StringAssert.Contains("UnsupportedVisual", result.Diagnostic); } finally { if (result.Resource != null) result.Resource.Destroy(); } }

        [Test] public void MaterialPolicyMapsTranslucentDepthState()
        { Material material = Material(); if (material == null) Assert.Ignore("No test shader available."); try { UnityMaterialPolicy.Apply(material, PresentationAlphaMode.Translucent, PresentationDepthTestMode.TestAndWrite); Assert.AreEqual(1f, material.GetFloat("_ZWrite")); Assert.AreEqual((int)BlendMode.SrcAlpha, material.GetInt("_SrcBlend")); } finally { UnityEngine.Object.DestroyImmediate(material); } }

        [Test] public void CameraAdapterZoomDoesNotChangeLogicalPan()
        { UnityIsometricCameraAdapter adapter = new UnityIsometricCameraAdapter(); adapter.Pan(new Vector2(3, 4)); adapter.SetZoom(20f); Assert.AreEqual(new Vector2(3, 4), adapter.LogicalPan); Assert.AreEqual(20f, adapter.Zoom); }

        [Test] public void CameraAdapterRejectsOutOfRangeZoom()
        { UnityIsometricCameraAdapter adapter = new UnityIsometricCameraAdapter(new UnityCameraAdapterPolicy(2f, 5f)); Assert.Throws<ArgumentOutOfRangeException>(() => adapter.SetZoom(1f)); Assert.Throws<ArgumentOutOfRangeException>(() => adapter.SetZoom(6f)); }

        [Test] public void CameraAdapterKeepsViewportAspectExplicit()
        { UnityIsometricCameraAdapter adapter = new UnityIsometricCameraAdapter(); adapter.SetViewportAspect(1.5f); Assert.AreEqual(1.5f, adapter.ViewportAspect); Assert.Throws<ArgumentOutOfRangeException>(() => adapter.SetViewportAspect(0f)); }

        [Test] public void ExposedVoxelBuilderUsesBoundedSurfaceMesh()
        { VxlMeshBuildResult result = VxlExposedFaceMeshBuilder.Build(new[] { new VoxelRenderCell(0, 0, 0, 1) }); try { Assert.IsTrue(result.IsSuccess); Assert.AreEqual(24, result.Mesh.vertexCount); Assert.AreEqual(36, result.Mesh.triangles.Length); } finally { if (result.Mesh != null) UnityEngine.Object.DestroyImmediate(result.Mesh); } }

        [Test] public void ExposedVoxelBuilderOmitsInternalFaces()
        { VxlMeshBuildResult result = VxlExposedFaceMeshBuilder.Build(new[] { new VoxelRenderCell(0, 0, 0, 1), new VoxelRenderCell(1, 0, 0, 2) }); try { Assert.IsTrue(result.IsSuccess); Assert.AreEqual(40, result.Mesh.vertexCount); } finally { if (result.Mesh != null) UnityEngine.Object.DestroyImmediate(result.Mesh); } }

        [Test] public void ExposedVoxelBuilderStopsBeforeUnboundedAllocation()
        { VxlMeshBuildResult result = VxlExposedFaceMeshBuilder.Build(new[] { new VoxelRenderCell(0, 0, 0, 1), new VoxelRenderCell(1, 0, 0, 2) }, new VxlMeshBuildPolicy(1)); Assert.IsFalse(result.IsSuccess); Assert.IsNull(result.Mesh); }

        [Test]
        public void VxlPresentationNormalizesKnownTwoByThreeByFourModel()
        {
            var cells = new System.Collections.Generic.List<VoxelRenderCell>();
            for (int x = 0; x < 2; x++)
                for (int y = 0; y < 3; y++)
                    for (int z = 0; z < 4; z++)
                        cells.Add(new VoxelRenderCell(x, y, z, (byte)((x + y + z) % 2 + 1)));
            VxlPresentationBuildResult result = VxlExposedFaceMeshBuilder.Build(
                new[] { new VxlPresentationSectionInput("body", 0, cells, Matrix4x4.identity, true) },
                VxlPresentationTransformProfile.Default,
                TestDisplayPalette());
            try
            {
                Assert.IsTrue(result.IsSuccess, result.Diagnostic);
                Assert.AreEqual(1, result.Asset.Metrics.SectionCount);
                Assert.That(result.Asset.Metrics.Bounds.WidthCells, Is.LessThanOrEqualTo(1.5f));
                Assert.That(result.Asset.Metrics.Bounds.DepthCells, Is.LessThanOrEqualTo(1.5f));
                Assert.That(result.Asset.Metrics.Bounds.HeightCells, Is.LessThanOrEqualTo(1.5f));
                Assert.AreEqual(1, result.Asset.Metrics.HvaAppliedSectionCount);
            }
            finally { DestroyPresentation(result); }
        }

        [Test]
        public void VxlPresentationKeepsBodyAndTurretSectionsIndependent()
        {
            VxlPresentationBuildResult result = VxlExposedFaceMeshBuilder.Build(
                new[]
                {
                    new VxlPresentationSectionInput("body", 0, new[] { new VoxelRenderCell(0, 0, 0, 1) }, Matrix4x4.identity, true),
                    new VxlPresentationSectionInput("turret", 1, new[] { new VoxelRenderCell(1, 0, 0, 2) }, Matrix4x4.Translate(new Vector3(0f, 0f, 1f)), true)
                },
                VxlPresentationTransformProfile.Default,
                TestDisplayPalette());
            try
            {
                Assert.IsTrue(result.IsSuccess, result.Diagnostic);
                Assert.AreEqual(2, result.Asset.Sections.Count);
                Assert.AreEqual("body", result.Asset.Sections[0].SectionIdentity);
                Assert.AreEqual("turret", result.Asset.Sections[1].SectionIdentity);
                Assert.AreEqual(2, result.Asset.Metrics.HvaAppliedSectionCount);
            }
            finally { DestroyPresentation(result); }
        }

        [Test]
        public void VxlPresentationFrameZeroTransformChangesSectionLocation()
        {
            VxlPresentationBuildResult result = VxlExposedFaceMeshBuilder.Build(
                new[]
                {
                    new VxlPresentationSectionInput("body", 0, new[] { new VoxelRenderCell(0, 0, 0, 1) }, Matrix4x4.identity, true),
                    new VxlPresentationSectionInput("turret", 1, new[] { new VoxelRenderCell(0, 0, 0, 2) }, Matrix4x4.Translate(new Vector3(2f, 0f, 0f)), true)
                },
                VxlPresentationTransformProfile.Default,
                TestDisplayPalette());
            try
            {
                Assert.IsTrue(result.IsSuccess, result.Diagnostic);
                Assert.That(result.Asset.Sections[0].Bounds.RawMin.x, Is.Not.EqualTo(result.Asset.Sections[1].Bounds.RawMin.x));
            }
            finally { DestroyPresentation(result); }
        }

        [Test]
        public void VxlPresentationUsesExplicitRawAxisBasis()
        {
            Vector3 basis = VxlPresentationTransformProfile.Default.ToPresentationBasis(new Vector3(1f, 2f, 3f));
            Assert.AreEqual(new Vector3(1f, 3f, 2f), basis);
        }

        [Test]
        public void VxlPresentationPreservesPaletteColorVariation()
        {
            VxlPresentationBuildResult result = VxlExposedFaceMeshBuilder.Build(
                new[] { new VxlPresentationSectionInput("body", 0, new[] { new VoxelRenderCell(0, 0, 0, 1), new VoxelRenderCell(1, 0, 0, 2) }, Matrix4x4.identity, true) },
                VxlPresentationTransformProfile.Default,
                TestDisplayPalette());
            try
            {
                Assert.IsTrue(result.IsSuccess, result.Diagnostic);
                Assert.That(result.Asset.Metrics.DistinctColorCount, Is.GreaterThanOrEqualTo(2));
                Assert.That(result.Asset.Sections[0].Mesh.colors32, Has.Length.EqualTo(result.Asset.Sections[0].Mesh.vertexCount));
                Assert.That(result.Asset.Sections[0].Mesh.colors32, Has.Some.EqualTo(new Color32(255, 130, 65, 255)));
            }
            finally { DestroyPresentation(result); }
        }

        [Test]
        public void VxlPresentationScaleKeepsFortyVoxelModelWithinOneCellFootprint()
        {
            var cells = new System.Collections.Generic.List<VoxelRenderCell>();
            for (int x = 0; x < 40; x++) cells.Add(new VoxelRenderCell(x, 0, 0, (byte)(x % 2 + 1)));
            VxlPresentationBuildResult result = VxlExposedFaceMeshBuilder.Build(
                new[] { new VxlPresentationSectionInput("body", 0, cells, Matrix4x4.identity, true) },
                VxlPresentationTransformProfile.Default,
                TestDisplayPalette());
            try
            {
                Assert.IsTrue(result.IsSuccess, result.Diagnostic);
                Assert.That(result.Asset.Metrics.Bounds.WidthCells, Is.LessThanOrEqualTo(1.5f));
                Assert.That(result.Asset.Metrics.Bounds.WidthCells, Is.Not.EqualTo(40f));
            }
            finally { DestroyPresentation(result); }
        }

        [Test]
        public void VxlPresentationRejectsRawDimensionBudgetOverflow()
        {
            VxlPresentationTransformProfile profile = new VxlPresentationTransformProfile(maximumRawDimension: 64f);
            VxlPresentationBuildResult result = VxlExposedFaceMeshBuilder.Build(
                new[] { new VxlPresentationSectionInput("malformed", 0, new[] { new VoxelRenderCell(0, 0, 0, 1), new VoxelRenderCell(400, 0, 0, 2) }, Matrix4x4.identity, true) },
                profile,
                TestDisplayPalette());
            Assert.IsFalse(result.IsSuccess);
            Assert.IsNull(result.Asset);
        }

        [Test]
        public void RawPaletteDisplayProfileUsesExactSixBitConversion()
        {
            byte[] raw = new byte[256 * 3];
            raw[0] = 32;
            raw[1] = 16;
            raw[2] = 63;
            byte[] display = PaletteDisplayProfileConversion.ToDisplayBytes(raw, PaletteDisplayProfile.ScaleToFullRangeRounded);
            Assert.AreEqual(130, display[0]);
            Assert.AreEqual(65, display[1]);
            Assert.AreEqual(255, display[2]);
            Assert.AreEqual(255, PaletteDisplayProfileConversion.ConvertChannel(63, PaletteDisplayProfile.ScaleToFullRangeRounded));
        }

        [Test]
        public void VxlPresentationUsesConvertedRawPaletteColor()
        {
            byte[] raw = new byte[256 * 3];
            int offset = 7 * 3;
            raw[offset] = 32;
            raw[offset + 1] = 16;
            raw[offset + 2] = 63;
            byte[] display = PaletteDisplayProfileConversion.ToDisplayBytes(raw, PaletteDisplayProfile.ScaleToFullRangeRounded);
            VxlPresentationBuildResult result = VxlExposedFaceMeshBuilder.Build(
                new[] { new VxlPresentationSectionInput("body", 0, new[] { new VoxelRenderCell(0, 0, 0, 7), new VoxelRenderCell(1, 0, 0, 8) }, Matrix4x4.identity, true) },
                VxlPresentationTransformProfile.Default,
                display);
            try
            {
                Assert.IsTrue(result.IsSuccess, result.Diagnostic);
                Assert.That(result.Asset.Sections[0].Mesh.colors32, Has.Some.EqualTo(new Color32(130, 65, 255, 255)));
            }
            finally { DestroyPresentation(result); }
        }

        private static byte[] TestDisplayPalette()
        {
            byte[] raw = new byte[768];
            raw[3] = 63;
            raw[4] = 32;
            raw[5] = 16;
            raw[6] = 16;
            raw[7] = 55;
            raw[8] = 40;
            return PaletteDisplayProfileConversion.ToDisplayBytes(raw, PaletteDisplayProfile.ScaleToFullRangeRounded);
        }

        private static void DestroyPresentation(VxlPresentationBuildResult result)
        {
            if (result == null || result.Asset == null) return;
            foreach (VxlPresentationSectionMesh section in result.Asset.Sections)
                if (section.Mesh != null) UnityEngine.Object.DestroyImmediate(section.Mesh);
        }

        [Test] public void EffectMaterialPolicyIsAppliedToSubmission()
        { Shader shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color"); if (shader == null) Assert.Ignore("No test shader available."); Material material = new Material(shader); EffectPresentationDescriptor descriptor = new EffectPresentationDescriptor("effect", new VisualAssetId("effect"), PresentationEffectKind.Explosion, PresentationElevationLayer.Ground, new PresentationAnchor(PresentationAnchorKind.RenderPivot, 0, 0), new PresentationBounds(PresentationBoundsKind.Visual, 0, 0, 1, 1), new PresentationBounds(PresentationBoundsKind.ConservativeCulling, 0, 0, 1, 1), PresentationAlphaMode.Translucent, PresentationDepthTestMode.TestAndWrite, PresentationVisibilityState.Visible, 0); UnityPresentationWorld world = UnityPresentationWorld.CreateSynthetic(); try { world.Configure(new UnityPresentationWorldPolicy(), material); UnityPresentationApplyResult result = world.Apply(null, EffectPresentationComposer.Compose(new[] { descriptor }, null)); Assert.IsTrue(result.IsSuccess); Assert.AreEqual(1, result.SubmissionCount); Assert.IsNotNull(world.transform.GetChild(0).GetComponent<Renderer>().sharedMaterial); Assert.AreEqual(1f, world.transform.GetChild(0).GetComponent<Renderer>().sharedMaterial.GetFloat("_ZWrite")); } finally { UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(world.gameObject); } }

        [Test] public void WorldDoesNotCreateOneGameObjectPerVoxel()
        { VxlMeshBuildResult result = VxlExposedFaceMeshBuilder.Build(new[] { new VoxelRenderCell(0, 0, 0, 1), new VoxelRenderCell(1, 0, 0, 2) }); try { GameObject root = new GameObject("voxel-root"); try { MeshFilter filter = root.AddComponent<MeshFilter>(); filter.sharedMesh = result.Mesh; Assert.IsNotNull(root.GetComponent<MeshFilter>()); Assert.AreEqual(0, root.transform.childCount); } finally { UnityEngine.Object.DestroyImmediate(root); } } finally { if (result.Mesh != null) UnityEngine.Object.DestroyImmediate(result.Mesh); } }

        [Test] public void ObjectDrawBuilderProducesOrderedCommand()
        { ObjectVisualPresentationResult presentation = ObjectVisualPresentationComposer.Compose(new[] { Object("unit") }); ObjectVisualDrawCommandResult result = ObjectVisualDrawCommandBuilder.Build(presentation); Assert.IsTrue(result.IsSuccess); Assert.AreEqual(1, result.Commands.Count); Assert.AreEqual("unit", result.Commands[0].StableIdentity); }

        [Test] public void WorldAppliesObjectSubmissionWithoutSimulationMutation()
        { UnityPresentationWorld world = UnityPresentationWorld.CreateSynthetic(); try { ObjectVisualDrawCommandResult draw = ObjectVisualDrawCommandBuilder.Build(ObjectVisualPresentationComposer.Compose(new[] { Object("unit") })); UnityPresentationApplyResult result = world.Apply(draw, null); Assert.IsTrue(result.IsSuccess); Assert.AreEqual(1, result.SubmissionCount); Assert.AreEqual(1, world.transform.childCount); } finally { UnityEngine.Object.DestroyImmediate(world.gameObject); } }

        [Test] public void FoggedEffectRemainsLogicalButIsNotSubmittedToWorld()
        { EffectPresentationDescriptor descriptor = new EffectPresentationDescriptor("fog", new VisualAssetId("fog"), PresentationEffectKind.Fire, PresentationElevationLayer.Ground, new PresentationAnchor(PresentationAnchorKind.RenderPivot, 0, 0), new PresentationBounds(PresentationBoundsKind.Visual, 0, 0, 1, 1), new PresentationBounds(PresentationBoundsKind.ConservativeCulling, 0, 0, 1, 1), PresentationAlphaMode.Translucent, PresentationDepthTestMode.TestOnly, PresentationVisibilityState.Fogged, 0); EffectPresentationResult effect = EffectPresentationComposer.Compose(new[] { descriptor }, null); UnityPresentationWorld world = UnityPresentationWorld.CreateSynthetic(); try { UnityPresentationApplyResult result = world.Apply(null, effect); Assert.IsTrue(result.IsSuccess); Assert.AreEqual(0, result.SubmissionCount); } finally { UnityEngine.Object.DestroyImmediate(world.gameObject); } }

        [Test] public void WorldRejectsFailedPresentationBeforeCreatingObjects()
        { ObjectVisualDrawCommandResult failed = ObjectVisualDrawCommandBuilder.Build(ObjectVisualPresentationComposer.Compose(new ObjectVisualPresentationDescriptor[] { null })); UnityPresentationWorld world = UnityPresentationWorld.CreateSynthetic(); try { UnityPresentationApplyResult result = world.Apply(failed, null); Assert.IsFalse(result.IsSuccess); Assert.AreEqual(0, world.transform.childCount); } finally { UnityEngine.Object.DestroyImmediate(world.gameObject); } }

        [Test] public void TerrainChunkUsesOneMeshObject()
        { TerrainPresentationBuildResult composed = TerrainPresentationComposer.Build(new[] { new TerrainTilePresentationDescriptor(0, 0, 1, 0, 0, null, null, null, 0, 0, null, 0), new TerrainTilePresentationDescriptor(1, 0, 2, 0, 0, null, null, null, 0, 1, null, 1) }); UnityPresentationWorld world = UnityPresentationWorld.CreateSynthetic(); try { TerrainChunkMeshBuildResult result = world.ApplyTerrainChunk(composed.Chunks[0], new IsometricProjectionProfile(0, 0, 2, 2, 0)); Assert.IsTrue(result.IsSuccess); Assert.AreEqual(1, world.transform.childCount); Assert.AreEqual(8, result.Mesh.vertexCount); } finally { UnityEngine.Object.DestroyImmediate(world.gameObject); } }

        [Test] public void TerrainChunkBudgetIsExplicit()
        { UnityPresentationWorld world = UnityPresentationWorld.CreateSynthetic(); world.Configure(new UnityPresentationWorldPolicy(maxTerrainChunks: 0)); try { TerrainPresentationBuildResult composed = TerrainPresentationComposer.Build(new[] { new TerrainTilePresentationDescriptor(0, 0, 1, 0, 0, null, null, null, 0, 0, null, 0) }); Assert.Throws<InvalidOperationException>(() => world.ApplyTerrainChunk(composed.Chunks[0], new IsometricProjectionProfile(0, 0, 2, 2, 0))); } finally { UnityEngine.Object.DestroyImmediate(world.gameObject); } }

        [Test] public void WorldReusesObjectSubmissionIdentity()
        { UnityPresentationWorld world = UnityPresentationWorld.CreateSynthetic(); try { ObjectVisualDrawCommandResult draw = ObjectVisualDrawCommandBuilder.Build(ObjectVisualPresentationComposer.Compose(new[] { Object("unit") })); world.Apply(draw, null); world.Apply(draw, null); Assert.AreEqual(1, world.transform.childCount); } finally { UnityEngine.Object.DestroyImmediate(world.gameObject); } }

        [Test] public void WorldClearRemovesSubmissionReferences()
        { UnityPresentationWorld world = UnityPresentationWorld.CreateSynthetic(); try { ObjectVisualDrawCommandResult draw = ObjectVisualDrawCommandBuilder.Build(ObjectVisualPresentationComposer.Compose(new[] { Object("unit") })); world.Apply(draw, null); world.ClearSubmissions(); Assert.AreEqual(0, world.LastSubmissionCount); } finally { UnityEngine.Object.DestroyImmediate(world.gameObject); } }

        [Test] public void WorldDepthMappingRejectsLongPrimary()
        { ObjectVisualPresentationDescriptor descriptor = Object("far"); ObjectVisualPresentationDescriptor impossible = new ObjectVisualPresentationDescriptor(descriptor.VisualAssetId, descriptor.Family, descriptor.RenderPass, descriptor.ElevationLayer, descriptor.LogicalGroundAnchor, descriptor.VisualBounds, descriptor.ConservativeCullingBounds, descriptor.StableIdentity, 0, 1, 2, long.MaxValue); ObjectVisualDrawCommandResult draw = ObjectVisualDrawCommandBuilder.Build(ObjectVisualPresentationComposer.Compose(new[] { impossible })); UnityPresentationWorld world = UnityPresentationWorld.CreateSynthetic(); try { UnityPresentationApplyResult result = world.Apply(draw, null); Assert.IsFalse(result.IsSuccess); } finally { UnityEngine.Object.DestroyImmediate(world.gameObject); } }

        [Test] public void PresentationAssemblyRemainsUnityFree()
        { Assert.IsFalse(typeof(EffectPresentationDescriptor).Assembly.FullName.Contains("UnityEngine")); }

        [Test] public void NoPerTileGameObjectContractIsPreserved()
        { TerrainPresentationBuildResult composed = TerrainPresentationComposer.Build(new[] { new TerrainTilePresentationDescriptor(0, 0, 1, 0, 0, null, null, null, 0, 0, null, 0), new TerrainTilePresentationDescriptor(1, 0, 2, 0, 0, null, null, null, 0, 1, null, 1) }); TerrainChunkMeshBuildResult result = TerrainChunkMeshBuilder.Build(composed.Chunks[0], new IsometricProjectionProfile(0, 0, 2, 2, 0)); try { Assert.AreEqual(1, result.Mesh.subMeshCount); } finally { UnityEngine.Object.DestroyImmediate(result.Mesh); } }
    }
}
