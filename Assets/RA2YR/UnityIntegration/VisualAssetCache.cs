using System;
using System.Collections.Generic;
using RA2YR.Presentation;
using UnityEngine;
using UnityEngine.Rendering;

namespace RA2YR.UnityIntegration
{
    public enum VisualAssetRepresentationKind
    {
        IndexedTexture,
        PaletteLookup,
        TerrainMesh,
        VoxelMesh,
        Placeholder
    }

    public readonly struct VisualAssetCacheKey : IEquatable<VisualAssetCacheKey>
    {
        public VisualAssetCacheKey(VisualAssetId assetId, string providerId, string paletteProfile, string variant, VisualAssetRepresentationKind representation, int frame, string remapProfile)
        {
            if (!assetId.IsValid) throw new ArgumentException("A valid visual asset identity is required.", nameof(assetId));
            AssetId = assetId; ProviderId = providerId ?? string.Empty; PaletteProfile = paletteProfile ?? string.Empty; Variant = variant ?? string.Empty; Representation = representation; Frame = frame; RemapProfile = remapProfile ?? string.Empty;
        }
        public VisualAssetId AssetId { get; }
        public string ProviderId { get; }
        public string PaletteProfile { get; }
        public string Variant { get; }
        public VisualAssetRepresentationKind Representation { get; }
        public int Frame { get; }
        public string RemapProfile { get; }
        public bool Equals(VisualAssetCacheKey other) => AssetId.Equals(other.AssetId) && string.Equals(ProviderId, other.ProviderId, StringComparison.Ordinal) && string.Equals(PaletteProfile, other.PaletteProfile, StringComparison.Ordinal) && string.Equals(Variant, other.Variant, StringComparison.Ordinal) && Representation == other.Representation && Frame == other.Frame && string.Equals(RemapProfile, other.RemapProfile, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is VisualAssetCacheKey && Equals((VisualAssetCacheKey)obj);
        public override int GetHashCode() { unchecked { int value = AssetId.GetHashCode(); value = value * 397 ^ StringComparer.Ordinal.GetHashCode(ProviderId); value = value * 397 ^ StringComparer.Ordinal.GetHashCode(PaletteProfile); value = value * 397 ^ StringComparer.Ordinal.GetHashCode(Variant); value = value * 397 ^ (int)Representation; value = value * 397 ^ Frame; return value * 397 ^ StringComparer.Ordinal.GetHashCode(RemapProfile); } }
    }

    public sealed class VisualAssetCache
    {
        private sealed class Entry { public UnityEngine.Object Resource; public LinkedListNode<VisualAssetCacheKey> Node; }
        private readonly Dictionary<VisualAssetCacheKey, Entry> entries = new Dictionary<VisualAssetCacheKey, Entry>();
        private readonly LinkedList<VisualAssetCacheKey> order = new LinkedList<VisualAssetCacheKey>();
        private readonly bool destroyEvicted;
        public VisualAssetCache(int capacity = 256, bool destroyEvicted = true)
        { if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity)); Capacity = capacity; this.destroyEvicted = destroyEvicted; }
        public int Capacity { get; }
        public int Count => entries.Count;
        public int EvictionCount { get; private set; }
        public bool TryGet(VisualAssetCacheKey key, out UnityEngine.Object resource)
        {
            Entry entry;
            if (!entries.TryGetValue(key, out entry)) { resource = null; return false; }
            Touch(key, entry); resource = entry.Resource; return resource != null;
        }
        public bool Put(VisualAssetCacheKey key, UnityEngine.Object resource)
        {
            if (resource == null || Capacity == 0) return false;
            Entry existing;
            if (entries.TryGetValue(key, out existing)) { existing.Resource = resource; Touch(key, existing); return true; }
            while (entries.Count >= Capacity) EvictOldest();
            LinkedListNode<VisualAssetCacheKey> node = order.AddLast(key);
            entries.Add(key, new Entry { Resource = resource, Node = node });
            return true;
        }
        public bool Remove(VisualAssetCacheKey key)
        {
            Entry entry;
            if (!entries.TryGetValue(key, out entry)) return false;
            entries.Remove(key); order.Remove(entry.Node); DestroyIfOwned(entry.Resource); return true;
        }
        public void Clear()
        { foreach (Entry entry in entries.Values) DestroyIfOwned(entry.Resource); entries.Clear(); order.Clear(); }
        private void Touch(VisualAssetCacheKey key, Entry entry) { order.Remove(entry.Node); entry.Node = order.AddLast(key); }
        private void EvictOldest()
        { LinkedListNode<VisualAssetCacheKey> node = order.First; if (node == null) return; Entry entry = entries[node.Value]; entries.Remove(node.Value); order.Remove(node); DestroyIfOwned(entry.Resource); EvictionCount++; }
        private void DestroyIfOwned(UnityEngine.Object resource) { if (destroyEvicted && resource != null) UnityEngine.Object.DestroyImmediate(resource); }
    }

    public enum PaletteDisplayProfile { Unresolved, ShiftLeftTwo, ReplicateHighBits, ScaleToFullRangeRounded, XccScaleToFullRangeFloor }

    public sealed class IndexedTextureUploadPolicy
    {
        public IndexedTextureUploadPolicy(int maxWidth = 4096, int maxHeight = 4096, long maxPixels = 16 * 1024 * 1024)
        { if (maxWidth < 0 || maxHeight < 0 || maxPixels < 0) throw new ArgumentOutOfRangeException(); MaxWidth = maxWidth; MaxHeight = maxHeight; MaxPixels = maxPixels; }
        public int MaxWidth { get; } public int MaxHeight { get; } public long MaxPixels { get; }
    }

    public sealed class IndexedTextureResource
    {
        internal IndexedTextureResource(Texture2D indexed, Texture2D palette) { Indexed = indexed; PaletteLookup = palette; }
        public Texture2D Indexed { get; }
        public Texture2D PaletteLookup { get; }
        public void Destroy() { if (Indexed != null) UnityEngine.Object.DestroyImmediate(Indexed); if (PaletteLookup != null) UnityEngine.Object.DestroyImmediate(PaletteLookup); }
    }

    public static class IndexedTextureFactory
    {
        public static IndexedTextureResource Build(byte[] indices, int width, int height, byte[] paletteRaw, PaletteDisplayProfile profile, IndexedTextureUploadPolicy policy = null)
        {
            policy = policy ?? new IndexedTextureUploadPolicy();
            if (indices == null) throw new ArgumentNullException(nameof(indices));
            if (width <= 0 || height <= 0 || width > policy.MaxWidth || height > policy.MaxHeight) throw new ArgumentOutOfRangeException();
            long pixels = checked((long)width * height);
            if (pixels > policy.MaxPixels || pixels != indices.Length) throw new ArgumentException("Indexed pixel data does not match the bounded dimensions.", nameof(indices));
            Texture2D indexed = null; Texture2D palette = null;
            try
            {
                indexed = new Texture2D(width, height, TextureFormat.R8, false, true) { name = "SyntheticIndexedSource", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                indexed.LoadRawTextureData(indices); indexed.Apply(false, false);
                if (profile != PaletteDisplayProfile.Unresolved)
                {
                    if (paletteRaw == null || paletteRaw.Length != 256 * 3) throw new ArgumentException("A 256-color palette is required for a resolved lookup profile.", nameof(paletteRaw));
                    Color32[] colors = new Color32[256];
                    for (int i = 0; i < colors.Length; i++) colors[i] = new Color32(Convert(paletteRaw[i * 3], profile), Convert(paletteRaw[i * 3 + 1], profile), Convert(paletteRaw[i * 3 + 2], profile), 255);
                    palette = new Texture2D(256, 1, TextureFormat.RGBA32, false, true) { name = "SyntheticPaletteLookup", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                    palette.SetPixels32(colors); palette.Apply(false, false);
                }
                return new IndexedTextureResource(indexed, palette);
            }
            catch { if (indexed != null) UnityEngine.Object.DestroyImmediate(indexed); if (palette != null) UnityEngine.Object.DestroyImmediate(palette); throw; }
        }
        private static byte Convert(byte raw, PaletteDisplayProfile profile)
        {
            if (raw > 63) throw new ArgumentOutOfRangeException(nameof(raw));
            switch (profile)
            {
                case PaletteDisplayProfile.ShiftLeftTwo: return checked((byte)(raw << 2));
                case PaletteDisplayProfile.ReplicateHighBits: return checked((byte)((raw << 2) | (raw >> 4)));
                case PaletteDisplayProfile.ScaleToFullRangeRounded: return checked((byte)((raw * 255 + 31) / 63));
                case PaletteDisplayProfile.XccScaleToFullRangeFloor: return checked((byte)(raw * 255 / 63));
                default: throw new ArgumentOutOfRangeException(nameof(profile));
            }
        }
    }

    public sealed class IndexedSpriteUploadResult
    {
        internal IndexedSpriteUploadResult(IndexedTextureResource resource, bool placeholder, string diagnostic) { Resource = resource; IsPlaceholder = placeholder; Diagnostic = diagnostic; }
        public IndexedTextureResource Resource { get; }
        public bool IsPlaceholder { get; }
        public string Diagnostic { get; }
    }

    public static class IndexedSpriteRenderer
    {
        public static IndexedSpriteUploadResult Upload(bool decoderSucceeded, byte[] indices, int width, int height, byte[] paletteRaw, PaletteDisplayProfile profile)
        {
            if (decoderSucceeded) return new IndexedSpriteUploadResult(IndexedTextureFactory.Build(indices, width, height, paletteRaw, profile), false, null);
            // Generated magenta marker is a repo-safe placeholder, never a compatibility success.
            IndexedTextureResource placeholder = IndexedTextureFactory.Build(new byte[] { 255 }, 1, 1, null, PaletteDisplayProfile.Unresolved);
            return new IndexedSpriteUploadResult(placeholder, true, "UnsupportedVisual: strict legacy frame was not renderable.");
        }
    }

    public static class UnityMaterialPolicy
    {
        public static void Apply(Material material, PresentationAlphaMode alpha, PresentationDepthTestMode depth)
        {
            if (material == null) throw new ArgumentNullException(nameof(material));
            material.SetFloat("_ZWrite", depth == PresentationDepthTestMode.TestAndWrite ? 1f : 0f);
            material.SetFloat("_ZTest", depth == PresentationDepthTestMode.Disabled ? (float)CompareFunction.Disabled : (float)CompareFunction.LessEqual);
            material.SetFloat("_Surface", alpha == PresentationAlphaMode.Translucent ? 1f : 0f);
            material.SetInt("_SrcBlend", alpha == PresentationAlphaMode.Translucent ? (int)BlendMode.SrcAlpha : (int)BlendMode.One);
            material.SetInt("_DstBlend", alpha == PresentationAlphaMode.Translucent ? (int)BlendMode.OneMinusSrcAlpha : (int)BlendMode.Zero);
        }
    }
}
