using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Binary.Seekable;
using RA2YR.Core.Content;
using RA2YR.Core.Content.Mix;
using RA2YR.Core.Formats.Mix;
using RA2YR.Core.Formats.Pal;
using RA2YR.Core.Formats.ShpTs;
using RA2YR.Core.Formats.VxlHva;
using RA2YR.Core.Formats.PackedMap;
using RA2YR.Presentation;
using RA2YR.Simulation;
using UnityEngine;

namespace RA2YR.UnityIntegration
{
    public enum HumanPlaytestVisualMode
    {
        SyntheticOnly,
        ExternalLegacyPreferred
    }

    public sealed class HumanPlaytestVisualProfile
    {
        public HumanPlaytestVisualProfile(
            HumanPlaytestVisualMode mode,
            string configurationPath,
            string sourceId = "YR1001_ProjectBaseline",
            PaletteDisplayProfile paletteProfile = PaletteDisplayProfile.ScaleToFullRangeRounded,
            int maxProbeEntries = 12000,
            long maxAssetBytes = 64L * 1024 * 1024,
            byte teamRangeStart = 16,
            byte teamRangeEnd = 31,
            int humanTeamOffset = 0,
            int enemyTeamOffset = 16)
        {
            if (!Enum.IsDefined(typeof(HumanPlaytestVisualMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
            if (string.IsNullOrWhiteSpace(configurationPath)) throw new ArgumentException("A configuration path is required.", nameof(configurationPath));
            if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("A source id is required.", nameof(sourceId));
            if (maxProbeEntries < 0 || maxAssetBytes < 0) throw new ArgumentOutOfRangeException();
            if (teamRangeStart > teamRangeEnd) throw new ArgumentOutOfRangeException(nameof(teamRangeStart));
            Mode = mode;
            ConfigurationPath = configurationPath;
            SourceId = sourceId;
            PaletteProfile = paletteProfile;
            MaxProbeEntries = maxProbeEntries;
            MaxAssetBytes = maxAssetBytes;
            TeamRangeStart = teamRangeStart;
            TeamRangeEnd = teamRangeEnd;
            HumanTeamOffset = humanTeamOffset;
            EnemyTeamOffset = enemyTeamOffset;
        }

        public HumanPlaytestVisualMode Mode { get; }
        public string ConfigurationPath { get; }
        public string SourceId { get; }
        public PaletteDisplayProfile PaletteProfile { get; }
        public int MaxProbeEntries { get; }
        public long MaxAssetBytes { get; }
        public byte TeamRangeStart { get; }
        public byte TeamRangeEnd { get; }
        public int HumanTeamOffset { get; }
        public int EnemyTeamOffset { get; }
    }

    public sealed class ExternalLegacyVisualStatus
    {
        internal ExternalLegacyVisualStatus(
            bool configured,
            bool sourceAvailable,
            bool ready,
            int shpAssets,
            int decodedFrames,
            int unsupportedFrames,
            int vxlAssets,
            int hvaAssets,
            int paletteAssets,
            int probeCount,
            string terrainSource,
            string message)
        {
            IsConfigured = configured;
            SourceAvailable = sourceAvailable;
            IsReady = ready;
            ShpAssetCount = shpAssets;
            ShpDecodedFrameCount = decodedFrames;
            ShpUnsupportedFrameCount = unsupportedFrames;
            VxlAssetCount = vxlAssets;
            HvaAssetCount = hvaAssets;
            PaletteAssetCount = paletteAssets;
            ProbedEntryCount = probeCount;
            TerrainSource = terrainSource ?? "SyntheticFallback";
            Message = message ?? string.Empty;
        }

        public bool IsConfigured { get; }
        public bool SourceAvailable { get; }
        public bool IsReady { get; }
        public int ShpAssetCount { get; }
        public int ShpDecodedFrameCount { get; }
        public int ShpUnsupportedFrameCount { get; }
        public int VxlAssetCount { get; }
        public int HvaAssetCount { get; }
        public int PaletteAssetCount { get; }
        public int ProbedEntryCount { get; }
        public string TerrainSource { get; }
        public string Message { get; }
    }

    /// <summary>
    /// Unity-side adapter for the existing bounded legacy readers. It discovers
    /// typed assets from the configured source without embedding names or bytes
    /// in the project. It never mutates SimulationWorld.
    /// </summary>
    public sealed class ExternalLegacyVisualProvider : IVisualAssetProvider, IDisposable
    {
        private static readonly string[] CandidateLogicalNames =
        {
            "conquer.mix", "cache.mix", "snowmd.mix", "cameo.mix",
            "yatech.shp", "engineer.shp", "cnoild.shp", "mouse.shp", "chronofd.shp", "e1icon.shp",
            "isotem.pal", "temperat.pal", "unittem.pal",
            "apoc.vxl", "grizzly.vxl", "mtnk.vxl", "ytnk.vxl",
            "apoc.hva", "grizzly.hva", "mtnk.hva", "ytnk.hva"
        };

        private sealed class ShpFrame
        {
            public int Width;
            public int Height;
            public byte[] Indices;
        }

        private readonly List<ShpFrame> shpFrames;
        private readonly List<VoxelRenderCell> voxelCells;
        private readonly byte[] paletteRaw;
        private readonly HumanPlaytestVisualProfile profile;
        private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private Mesh voxelMesh;
        private bool disposed;

        private ExternalLegacyVisualProvider(
            HumanPlaytestVisualProfile profile,
            ExternalLegacyVisualStatus status,
            IEnumerable<ShpFrame> frames,
            IEnumerable<VoxelRenderCell> cells,
            byte[] palette)
        {
            this.profile = profile;
            Status = status;
            shpFrames = new List<ShpFrame>(frames ?? Enumerable.Empty<ShpFrame>());
            voxelCells = new List<VoxelRenderCell>(cells ?? Enumerable.Empty<VoxelRenderCell>());
            paletteRaw = palette == null ? null : (byte[])palette.Clone();
        }

        public string ProviderId => "external-legacy";
        public ExternalLegacyVisualStatus Status { get; }
        public bool IsAvailable => Status != null && Status.IsReady && !disposed;

        public static ExternalLegacyVisualProvider Create(
            HumanPlaytestVisualProfile profile,
            string repositoryRoot)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (repositoryRoot == null) throw new ArgumentNullException(nameof(repositoryRoot));
            if (profile.Mode == HumanPlaytestVisualMode.SyntheticOnly)
                return new ExternalLegacyVisualProvider(profile, Unavailable(false, "SyntheticOnly"), null, null, null);

            try
            {
                ExternalContentConfigurationLoadResult loaded =
                    new ExternalContentConfigurationLoader().Load(profile.ConfigurationPath, repositoryRoot);
                ExternalContentConfiguration configuration = loaded.Configuration;
                ExternalContentSourceDescriptor source = configuration.Sources.FirstOrDefault(
                    value => value.Enabled && string.Equals(value.Id, profile.SourceId, StringComparison.Ordinal));
                if (source == null)
                    return new ExternalLegacyVisualProvider(profile, Unavailable(true, "Configured external source is unavailable."), null, null, null);

                ContentIndexResult index = new ContentIndexer().Build(configuration);
                ContentSourceIndex sourceIndex = index.Sources.FirstOrDefault(value => string.Equals(value.Source.Id, source.Id, StringComparison.Ordinal));
                if (!index.IsComplete || sourceIndex == null || !sourceIndex.IsComplete)
                    return new ExternalLegacyVisualProvider(profile, Unavailable(true, "External source indexing failed closed."), null, null, null);

                LogicalContentPath[] roots = sourceIndex.Files
                    .Where(value => value.LogicalPath.Value.EndsWith(".mix", StringComparison.OrdinalIgnoreCase))
                    .Select(value => value.LogicalPath)
                    .OrderBy(value => value, LogicalContentPathReportComparer.Instance)
                    .ToArray();
                MixNameCatalog names = new MixNameCatalog(
                    CandidateLogicalNames.Select(LogicalContentPath.Parse)
                        .Concat(sourceIndex.Files.Select(value => value.LogicalPath))
                        .Concat(roots)
                        .Distinct());
                var mounts = new List<MixVirtualContentMountResult>();
                var frames = new List<ShpFrame>();
                var cells = new List<VoxelRenderCell>();
                byte[] palette = null;
                int unsupported = 0;
                int shpAssets = 0;
                int vxlAssets = 0;
                int hvaAssets = 0;
                int paletteAssets = 0;
                int probed = 0;
                try
                {
                    foreach (LogicalContentPath root in roots)
                    {
                        MixVirtualContentMountResult mount = MixVirtualContentSource.MountDirectorySource(
                            sourceIndex,
                            new[] { root },
                            names,
                            MixArchiveCatalogAdapters.ReadWithCoreReader,
                            MixMountLimits.Default,
                            MixMountIndexMode.StructureOnly);
                        mounts.Add(mount);
                    }

                    IEnumerable<MixVirtualEntry> entries = mounts
                        .Where(value => value.IsComplete)
                        .SelectMany(value => value.Entries)
                        .Where(value => !value.IsMountedArchive && value.HasResolvedName && value.Length > 0 && value.Length <= profile.MaxAssetBytes)
                        .Where(value => IsCandidateExtension(Path.GetExtension(value.LogicalName.Value)))
                        .OrderBy(value => value.LogicalName == null ? string.Empty : value.LogicalName.Value, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(value => value.Id.Value);

                    foreach (MixVirtualEntry entry in entries)
                    {
                        if (probed >= profile.MaxProbeEntries) break;
                        probed = checked(probed + 1);
                        byte[] bytes;
                        try { bytes = PackedMapBoundedInput.ReadWindow(entry.PayloadWindow, "m6-external-visual", profile.MaxAssetBytes); }
                        catch (Exception exception) when (exception is BinaryReadException || exception is IOException || exception is ArgumentException || exception is InvalidOperationException || exception is OverflowException)
                        { continue; }

                        LogicalContentPath logical = entry.LogicalName ?? LogicalContentPath.Parse("external-entry/" + probed.ToString("D6"));
                        string extension = Path.GetExtension(logical.Value);
                        bool hintedShp = string.Equals(extension, ".shp", StringComparison.OrdinalIgnoreCase);
                        bool hintedVxl = string.Equals(extension, ".vxl", StringComparison.OrdinalIgnoreCase);
                        bool hintedHva = string.Equals(extension, ".hva", StringComparison.OrdinalIgnoreCase);
                        bool hintedPal = string.Equals(extension, ".pal", StringComparison.OrdinalIgnoreCase);

                        if (palette == null && (hintedPal || bytes.Length == 768))
                        {
                            PaletteParseResult parsedPalette = WestwoodPaletteReader.Read(
                                bytes,
                                new BinarySourceContext("m6-external-palette", source.Id, logical),
                                new PaletteSourceProvenance(source.Id, new[] { logical }));
                            if (parsedPalette.IsSuccess)
                            {
                                palette = ToPaletteBytes(parsedPalette.Palette);
                                paletteAssets = checked(paletteAssets + 1);
                            }
                        }

                        if (frames.Count == 0 || hintedShp)
                        {
                            ShpTsParseResult parsedShp = WestwoodShpTsReader.Read(
                                bytes,
                                new BinarySourceContext("m6-external-shp", source.Id, logical),
                                new ShpTsSourceProvenance(source.Id, new[] { logical }));
                            if (parsedShp.IsSuccess && parsedShp.Document != null)
                            {
                                shpAssets = checked(shpAssets + 1);
                                foreach (ShpTsFrameDescriptor descriptor in parsedShp.Document.Frames)
                                {
                                    ShpTsDecodeResult decoded = WestwoodShpTsDecoder.DecodeFrame(bytes, parsedShp.Document, descriptor.Index);
                                    if (decoded.IsSuccess && decoded.Frame != null)
                                    {
                                        frames.Add(new ShpFrame { Width = decoded.Frame.Width, Height = decoded.Frame.Height, Indices = decoded.Frame.GetIndicesCopy() });
                                        if (frames.Count >= 8) break;
                                    }
                                    else unsupported = checked(unsupported + 1);
                                }
                            }
                        }

                        if (cells.Count == 0 || hintedVxl)
                        {
                            VxlReadResult parsedVxl = WestwoodVxlReader.Read(bytes);
                            if (parsedVxl.IsSuccess && parsedVxl.Document != null)
                            {
                                vxlAssets = checked(vxlAssets + 1);
                                AppendVoxels(parsedVxl.Document, cells, 65536);
                            }
                        }

                        if (hintedHva || (hvaAssets == 0 && bytes.Length >= 24))
                        {
                            HvaReadResult parsedHva = WestwoodHvaReader.Read(bytes);
                            if (parsedHva.IsSuccess && parsedHva.Document != null) hvaAssets = checked(hvaAssets + 1);
                        }

                        if (frames.Count >= 8 && cells.Count != 0 && palette != null && hvaAssets != 0) break;
                    }
                }
                finally
                {
                    foreach (MixVirtualContentMountResult mount in mounts)
                    {
                        try { mount.Dispose(); } catch (Exception) { }
                    }
                }

                bool ready = frames.Count != 0 && palette != null;
                var status = new ExternalLegacyVisualStatus(
                    true,
                    true,
                    ready,
                    shpAssets,
                    frames.Count,
                    unsupported,
                    vxlAssets,
                    hvaAssets,
                    paletteAssets,
                    probed,
                    "SyntheticFallback",
                    ready ? "External legacy indexed visuals available." : "No safe external indexed visual and palette pair was resolved.");
                return new ExternalLegacyVisualProvider(profile, status, frames, cells, palette);
            }
            catch (Exception exception) when (exception is ContentConfigurationException || exception is IOException || exception is UnauthorizedAccessException || exception is InvalidOperationException || exception is ArgumentException)
            {
                return new ExternalLegacyVisualProvider(profile, Unavailable(true, "External legacy content preflight failed closed."), null, null, null);
            }
        }

        public VisualAssetProviderResult Resolve(VisualAssetId assetId)
        {
            if (!assetId.IsValid || !assetId.Value.StartsWith("external-legacy/playtest/", StringComparison.Ordinal))
                return new VisualAssetProviderResult(VisualAssetProviderResolutionStatus.Missing, ProviderId, assetId);
            return new VisualAssetProviderResult(
                IsAvailable ? VisualAssetProviderResolutionStatus.Resolved : VisualAssetProviderResolutionStatus.Missing,
                ProviderId,
                assetId);
        }

        public bool TryGetSprite(HumanPlaytestEntityKind kind, bool enemy, out Sprite sprite)
        {
            sprite = null;
            if (!IsAvailable || shpFrames.Count == 0 || paletteRaw == null) return false;
            string key = kind + ":" + (enemy ? "enemy" : "human");
            if (sprites.TryGetValue(key, out sprite) && sprite != null) return true;
            ShpFrame frame = shpFrames[0];
            if (frame.Width <= 0 || frame.Height <= 0 || frame.Width > 4096 || frame.Height > 4096) return false;
            long pixelCount = checked((long)frame.Width * frame.Height);
            if (pixelCount != frame.Indices.LongLength || pixelCount > 16L * 1024 * 1024) return false;
            var colors = new Color32[frame.Indices.Length];
            int offset = enemy ? profile.EnemyTeamOffset : profile.HumanTeamOffset;
            for (int index = 0; index < frame.Indices.Length; index++)
            {
                byte paletteIndex = Remap(frame.Indices[index], offset);
                int paletteOffset = paletteIndex * 3;
                byte alpha = frame.Indices[index] == 0 ? (byte)0 : (byte)255;
                colors[index] = new Color32(
                    Scale(paletteRaw[paletteOffset]),
                    Scale(paletteRaw[paletteOffset + 1]),
                    Scale(paletteRaw[paletteOffset + 2]),
                    alpha);
            }
            Texture2D texture = new Texture2D(frame.Width, frame.Height, TextureFormat.RGBA32, false)
            { name = "ExternalLegacyIndexedFrame", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(colors);
            texture.Apply(false, false);
            sprite = Sprite.Create(texture, new Rect(0f, 0f, frame.Width, frame.Height), new Vector2(0.5f, 0.5f), 32f);
            sprite.name = "ExternalLegacyIndexedSprite";
            sprites.Add(key, sprite);
            return true;
        }

        public bool TryGetVoxelMesh(out Mesh mesh)
        {
            mesh = null;
            if (!IsAvailable || voxelCells.Count == 0) return false;
            if (voxelMesh != null) { mesh = voxelMesh; return true; }
            VxlMeshBuildResult result = VxlExposedFaceMeshBuilder.Build(voxelCells, new VxlMeshBuildPolicy(65536));
            if (!result.IsSuccess || result.Mesh == null) return false;
            result.Mesh.name = "ExternalLegacyVxlExposedFaces";
            voxelMesh = result.Mesh;
            mesh = voxelMesh;
            return true;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (Sprite sprite in sprites.Values)
            {
                if (sprite == null) continue;
                Texture2D texture = sprite.texture;
                UnityEngine.Object.DestroyImmediate(sprite);
                if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            }
            sprites.Clear();
            if (voxelMesh != null) UnityEngine.Object.DestroyImmediate(voxelMesh);
            voxelMesh = null;
        }

        private byte Remap(byte value, int offset)
        {
            if (value < profile.TeamRangeStart || value > profile.TeamRangeEnd || offset == 0) return value;
            int candidate = value + offset;
            int width = profile.TeamRangeEnd - profile.TeamRangeStart + 1;
            while (candidate > profile.TeamRangeEnd) candidate -= width;
            while (candidate < profile.TeamRangeStart) candidate += width;
            return (byte)candidate;
        }

        private byte Scale(byte raw)
        {
            if (raw > 63) return raw;
            switch (profile.PaletteProfile)
            {
                case PaletteDisplayProfile.ShiftLeftTwo: return checked((byte)(raw << 2));
                case PaletteDisplayProfile.ReplicateHighBits: return checked((byte)((raw << 2) | (raw >> 4)));
                case PaletteDisplayProfile.XccScaleToFullRangeFloor: return checked((byte)(raw * 255 / 63));
                default: return checked((byte)((raw * 255 + 31) / 63));
            }
        }

        private static void AppendVoxels(VxlDocumentRaw document, List<VoxelRenderCell> cells, int max)
        {
            foreach (VxlSectionRaw section in document.Sections)
                foreach (VxlColumnRaw column in section.Columns)
                {
                    int z = 0;
                    foreach (VxlSpanChunkRaw chunk in column.Chunks)
                    {
                        z = checked(z + chunk.Skip);
                        foreach (VxlVoxelRaw voxel in chunk.Voxels)
                        {
                            if (cells.Count >= max) return;
                            cells.Add(new VoxelRenderCell(column.X, column.Y, z, voxel.ColorIndex));
                            z = checked(z + 1);
                        }
                    }
                }
        }

        private static byte[] ToPaletteBytes(WestwoodPalette palette)
        {
            var bytes = new byte[WestwoodPalette.FileLength];
            for (int index = 0; index < WestwoodPalette.ColorCount; index++)
            {
                PaletteColorRaw color = palette[index];
                bytes[index * 3] = color.Red;
                bytes[index * 3 + 1] = color.Green;
                bytes[index * 3 + 2] = color.Blue;
            }
            return bytes;
        }

        private static ExternalLegacyVisualStatus Unavailable(bool configured, string message)
        {
            return new ExternalLegacyVisualStatus(configured, false, false, 0, 0, 0, 0, 0, 0, 0, "SyntheticFallback", message);
        }

        private static bool IsCandidateExtension(string extension)
        {
            return string.Equals(extension, ".shp", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".pal", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".vxl", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".hva", StringComparison.OrdinalIgnoreCase);
        }
    }
}
