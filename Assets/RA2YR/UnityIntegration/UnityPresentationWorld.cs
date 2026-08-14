using System;
using System.Collections.Generic;
using RA2YR.Core.Formats.MapTerrain;
using RA2YR.Presentation;
using UnityEngine;

namespace RA2YR.UnityIntegration
{
    public sealed class UnityPresentationWorldPolicy
    {
        public UnityPresentationWorldPolicy(int maxSubmissions = 65536, int maxTerrainChunks = 4096)
        { if (maxSubmissions < 0 || maxTerrainChunks < 0) throw new ArgumentOutOfRangeException(); MaxSubmissions = maxSubmissions; MaxTerrainChunks = maxTerrainChunks; }
        public int MaxSubmissions { get; } public int MaxTerrainChunks { get; }
    }

    public sealed class UnityRenderDiagnostic
    {
        internal UnityRenderDiagnostic(string code, string message) { Code = code; Message = message; }
        public string Code { get; } public string Message { get; }
    }

    public sealed class UnityPresentationApplyResult
    {
        internal UnityPresentationApplyResult(bool success, int submissions, IEnumerable<UnityRenderDiagnostic> diagnostics)
        { IsSuccess = success; SubmissionCount = submissions; Diagnostics = new List<UnityRenderDiagnostic>(diagnostics ?? new UnityRenderDiagnostic[0]).AsReadOnly(); }
        public bool IsSuccess { get; } public int SubmissionCount { get; } public IReadOnlyList<UnityRenderDiagnostic> Diagnostics { get; }
    }

    public sealed class UnityPresentationWorld : MonoBehaviour
    {
        private readonly Dictionary<string, GameObject> submissions = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private readonly Dictionary<string, GameObject> chunks = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private readonly Dictionary<string, Material> effectMaterials = new Dictionary<string, Material>(StringComparer.Ordinal);
        private Material sharedMaterial;
        public int LastSubmissionCount { get; private set; }
        public UnityPresentationWorldPolicy Policy { get; private set; }

        public static UnityPresentationWorld CreateSynthetic(string name = "SyntheticPresentationWorld")
        {
            GameObject root = new GameObject(name); UnityPresentationWorld world = root.AddComponent<UnityPresentationWorld>(); world.Policy = new UnityPresentationWorldPolicy(); return world;
        }

        public void Configure(UnityPresentationWorldPolicy policy, Material material = null)
        { Policy = policy ?? throw new ArgumentNullException(nameof(policy)); sharedMaterial = material; }

        public UnityPresentationApplyResult Apply(ObjectVisualDrawCommandResult objects, EffectPresentationResult effects)
        {
            if (Policy == null) Policy = new UnityPresentationWorldPolicy();
            var diagnostics = new List<UnityRenderDiagnostic>();
            if (objects != null && !objects.IsSuccess) return Failure(diagnostics, "ObjectPresentationFailed");
            if (effects != null && !effects.IsSuccess) return Failure(diagnostics, "EffectPresentationFailed");
            int count = 0;
            if (objects != null)
            {
                foreach (ObjectVisualDrawCommand command in objects.Commands)
                {
                    if (count >= Policy.MaxSubmissions) return Failure(diagnostics, "SubmissionBudgetExceeded");
                    if (!TryMap(command.DepthKey, out int order)) return Failure(diagnostics, "DepthMappingOverflow");
                    GameObject target = GetOrCreate(command.StableIdentity, "Object"); target.transform.localPosition = command.LogicalAnchor; SetOrder(target, order); count++;
                }
            }
            if (effects != null)
            {
                foreach (EffectPresentationEntry entry in effects.Entries)
                {
                    if (!entry.IsVisuallySubmitted) continue;
                    if (count >= Policy.MaxSubmissions) return Failure(diagnostics, "SubmissionBudgetExceeded");
                    if (!TryMap(entry.DepthKey, out int order)) return Failure(diagnostics, "DepthMappingOverflow");
                    GameObject target = GetOrCreateEffect(entry.Descriptor); target.transform.localPosition = new Vector3(entry.Descriptor.Anchor.X, 0f, entry.Descriptor.Anchor.Y); SetOrder(target, order); count++;
                }
            }
            LastSubmissionCount = count; return new UnityPresentationApplyResult(true, count, diagnostics);
        }

        public TerrainChunkMeshBuildResult ApplyTerrainChunk(TerrainChunkDescriptor chunk, IsometricProjectionProfile projection, TerrainMeshBuildPolicy policy = null)
        {
            if (chunk == null || projection == null) throw new ArgumentNullException();
            if (!chunks.ContainsKey(chunk.StableIdentity) && chunks.Count >= (Policy ?? new UnityPresentationWorldPolicy()).MaxTerrainChunks) throw new InvalidOperationException("Terrain chunk budget exceeded.");
            TerrainChunkMeshBuildResult result = TerrainChunkMeshBuilder.Build(chunk, projection, policy);
            if (!result.IsSuccess) return result;
            GameObject target;
            if (!chunks.TryGetValue(chunk.StableIdentity, out target)) { target = new GameObject("TerrainChunk_" + chunk.StableIdentity); target.transform.SetParent(transform, false); target.AddComponent<MeshFilter>(); target.AddComponent<MeshRenderer>(); chunks.Add(chunk.StableIdentity, target); }
            target.GetComponent<MeshFilter>().sharedMesh = result.Mesh; return result;
        }

        public void ClearSubmissions()
        { foreach (GameObject target in submissions.Values) DestroyObject(target); submissions.Clear(); LastSubmissionCount = 0; }
        private GameObject GetOrCreate(string identity, string prefix)
        { GameObject target; if (submissions.TryGetValue(identity, out target) && target != null) { target.SetActive(true); return target; } target = new GameObject(prefix + "_" + identity); target.transform.SetParent(transform, false); if (sharedMaterial != null) target.AddComponent<MeshRenderer>().sharedMaterial = sharedMaterial; submissions[identity] = target; return target; }
        private GameObject GetOrCreateEffect(EffectPresentationDescriptor descriptor)
        {
            GameObject target = GetOrCreate(descriptor.StableIdentity, "Effect");
            string profile = ((int)descriptor.AlphaMode).ToString() + ":" + ((int)descriptor.DepthTestMode).ToString();
            Material material;
            if (!effectMaterials.TryGetValue(profile, out material))
            {
                Shader shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
                if (shader != null)
                {
                    material = sharedMaterial == null ? new Material(shader) : new Material(sharedMaterial);
                    UnityMaterialPolicy.Apply(material, descriptor.AlphaMode, descriptor.DepthTestMode);
                    effectMaterials.Add(profile, material);
                }
            }
            if (material != null)
            {
                MeshRenderer renderer = target.GetComponent<MeshRenderer>() ?? target.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
            }
            return target;
        }
        private void SetOrder(GameObject target, int order) { Renderer renderer = target.GetComponent<Renderer>(); if (renderer != null) renderer.sortingOrder = order; }
        private static bool TryMap(RenderDepthKey key, out int order) { order = 0; if (key.PrimaryDepth < int.MinValue || key.PrimaryDepth > int.MaxValue) return false; order = checked((int)key.PrimaryDepth); return true; }
        private static bool TryMap(EffectDepthKey key, out int order) { order = 0; if (key.Primary < int.MinValue || key.Primary > int.MaxValue) return false; order = checked((int)key.Primary); return true; }
        private UnityPresentationApplyResult Failure(List<UnityRenderDiagnostic> diagnostics, string code) { diagnostics.Add(new UnityRenderDiagnostic(code, code)); return new UnityPresentationApplyResult(false, 0, diagnostics); }
        private static void DestroyObject(UnityEngine.Object target) { if (target == null) return; if (Application.isPlaying) UnityEngine.Object.Destroy(target); else UnityEngine.Object.DestroyImmediate(target); }
        private void OnDestroy()
        {
            foreach (GameObject target in submissions.Values) DestroyObject(target);
            foreach (GameObject target in chunks.Values)
            {
                if (target != null && target.GetComponent<MeshFilter>() != null && target.GetComponent<MeshFilter>().sharedMesh != null) DestroyObject(target.GetComponent<MeshFilter>().sharedMesh);
                DestroyObject(target);
            }
            foreach (Material material in effectMaterials.Values) DestroyObject(material);
        }
    }
}
