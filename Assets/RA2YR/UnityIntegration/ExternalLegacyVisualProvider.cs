using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Binary.Seekable;
using RA2YR.Core.Configuration.Ini.Resolution;
using RA2YR.Core.Configuration.Ini.Typed;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;
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
            int enemyTeamOffset = 16,
            HumanPlaytestVisualRoleProfile roleProfile = null)
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
            RoleProfile = roleProfile ?? HumanPlaytestVisualRoleProfile.CreateDefault();
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
        public HumanPlaytestVisualRoleProfile RoleProfile { get; }
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
            int visualRolesTotal,
            int visualRolesResolvedExternal,
            int visualRolesFallback,
            int shpRolesResolved,
            int vxlRolesResolved,
            int hvaBindingsResolved,
            int paletteBindingsResolved,
            int humanUnitsExternal,
            int humanStructuresExternal,
            int enemyUnitsExternal,
            int enemyStructuresExternal,
            int unresolvedRoles,
            bool sourceFingerprintStable,
            HumanPlaytestRemapProfile remapProfile,
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
            VisualRolesTotal = visualRolesTotal;
            VisualRolesResolvedExternal = visualRolesResolvedExternal;
            VisualRolesFallback = visualRolesFallback;
            ShpRolesResolved = shpRolesResolved;
            VxlRolesResolved = vxlRolesResolved;
            HvaBindingsResolved = hvaBindingsResolved;
            PaletteBindingsResolved = paletteBindingsResolved;
            HumanUnitsExternal = humanUnitsExternal;
            HumanStructuresExternal = humanStructuresExternal;
            EnemyUnitsExternal = enemyUnitsExternal;
            EnemyStructuresExternal = enemyStructuresExternal;
            UnresolvedRoles = unresolvedRoles;
            SourceFingerprintStable = sourceFingerprintStable;
            RemapProfile = remapProfile;
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
        public int VisualRolesTotal { get; }
        public int VisualRolesResolvedExternal { get; }
        public int VisualRolesFallback { get; }
        public int ShpRolesResolved { get; }
        public int VxlRolesResolved { get; }
        public int HvaBindingsResolved { get; }
        public int PaletteBindingsResolved { get; }
        public int HumanUnitsExternal { get; }
        public int HumanStructuresExternal { get; }
        public int EnemyUnitsExternal { get; }
        public int EnemyStructuresExternal { get; }
        public int UnresolvedRoles { get; }
        public bool SourceFingerprintStable { get; }
        public HumanPlaytestRemapProfile RemapProfile { get; }
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
        private sealed class ShpFrame
        {
            public int Width;
            public int Height;
            public byte[] Indices;
        }

        private sealed class DecodedVisualAsset
        {
            public ResolvedLegacyVisual Binding;
            public ShpFrame Shp;
            public List<VoxelRenderCell> Voxels;
            public byte[] Palette;
        }

        private readonly IReadOnlyDictionary<HumanPlaytestVisualRole, DecodedVisualAsset> visualAssets;
        private readonly HumanPlaytestVisualProfile profile;
        private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>(StringComparer.Ordinal);
        private bool disposed;

        private ExternalLegacyVisualProvider(
            HumanPlaytestVisualProfile profile,
            ExternalLegacyVisualStatus status,
            IEnumerable<KeyValuePair<HumanPlaytestVisualRole, DecodedVisualAsset>> assets)
        {
            this.profile = profile;
            Status = status;
            visualAssets = new Dictionary<HumanPlaytestVisualRole, DecodedVisualAsset>(
                assets ?? Enumerable.Empty<KeyValuePair<HumanPlaytestVisualRole, DecodedVisualAsset>>());
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
                return new ExternalLegacyVisualProvider(profile, Unavailable(profile, false, "SyntheticOnly"), null);

            try
            {
                ExternalContentConfigurationLoadResult loaded =
                    new ExternalContentConfigurationLoader().Load(profile.ConfigurationPath, repositoryRoot);
                ExternalContentConfiguration configuration = loaded.Configuration;
                ExternalContentSourceDescriptor source = configuration.Sources.FirstOrDefault(
                    value => value.Enabled && string.Equals(value.Id, profile.SourceId, StringComparison.Ordinal));
                if (source == null)
                    return new ExternalLegacyVisualProvider(profile, Unavailable(profile, true, "Configured external source is unavailable."), null);

                ContentIndexResult index = new ContentIndexer().Build(configuration);
                ContentSourceIndex sourceIndex = index.Sources.FirstOrDefault(value => string.Equals(value.Source.Id, source.Id, StringComparison.Ordinal));
                if (!index.IsComplete || sourceIndex == null || !sourceIndex.IsComplete)
                    return new ExternalLegacyVisualProvider(profile, Unavailable(profile, true, "External source indexing failed closed."), null);

                LogicalContentPath[] roots = sourceIndex.Files
                    .Where(value => value.LogicalPath.Value.EndsWith(".mix", StringComparison.OrdinalIgnoreCase))
                    .Select(value => value.LogicalPath)
                    .OrderBy(value => value, LogicalContentPathReportComparer.Instance)
                    .ToArray();
                MixNameCatalog names = new MixNameCatalog(
                    new[] { "rulesmd.ini", "artmd.ini", "rules.ini", "art.ini" }
                        .Select(LogicalContentPath.Parse)
                        .Concat(sourceIndex.Files.Select(value => value.LogicalPath))
                        .Concat(roots)
                        .Distinct());
                var mounts = new List<MixVirtualContentMountResult>();
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
                        .Where(value => !value.IsMountedArchive && value.Length > 0 && value.Length <= profile.MaxAssetBytes)
                        .OrderBy(value => value.Provenance.RootArchivePath.Value, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(value => value.Id.Value);

                    MixVirtualEntry[] entryArray = entries.Take(profile.MaxProbeEntries).ToArray();
                    probed = entryArray.Length;
                    IniResolutionResult rules = ResolveTypedIni(entryArray, source.Id, "rulesmd.ini", profile.MaxAssetBytes);
                    IniResolutionResult art = ResolveTypedIni(entryArray, source.Id, "artmd.ini", profile.MaxAssetBytes);
                    if (rules == null) rules = ResolveTypedIni(entryArray, source.Id, "rules.ini", profile.MaxAssetBytes);
                    if (art == null) art = ResolveTypedIni(entryArray, source.Id, "art.ini", profile.MaxAssetBytes);
                    IniRulesResourceDocument typedRules = rules != null && rules.IsComplete
                        ? IniMinimalResourceViewBuilder.BuildRules(rules, IniTypedNameComparisonPolicy.OrdinalIgnoreCaseAscii, IniTypedViewLimits.Default).Document
                        : null;
                    IniArtResourceDocument typedArt = art != null && art.IsComplete
                        ? IniMinimalResourceViewBuilder.BuildArt(art, IniTypedNameComparisonPolicy.OrdinalIgnoreCaseAscii, IniBooleanCasePolicy.OrdinalIgnoreCaseAscii, IniTypedViewLimits.Default).Document
                        : null;
                    HumanPlaytestRoleDescriptor[] descriptors = BuildRoleDescriptors(profile.RoleProfile, typedRules, typedArt);
                    HumanPlaytestAssetAvailability[] availability = BuildAvailability(descriptors, profile, entryArray);
                    HumanPlaytestRoleResolutionResult resolution = HumanPlaytestVisualRoleResolver.Resolve(profile.RoleProfile, descriptors, availability);
                    Dictionary<HumanPlaytestVisualRole, DecodedVisualAsset> decoded = DecodeResolvedAssets(resolution, entryArray, source.Id, profile);
                    bool fingerprintStable = IsSourceFingerprintStable(configuration, source.Id, sourceIndex.Fingerprint);
                    ExternalLegacyVisualStatus status = CreateStatus(profile, resolution, decoded, probed, fingerprintStable);
                    return new ExternalLegacyVisualProvider(profile, status, decoded);
                }
                finally
                {
                    foreach (MixVirtualContentMountResult mount in mounts)
                    {
                        try { mount.Dispose(); } catch (Exception) { }
                    }
                }

            }
            catch (Exception exception) when (exception is ContentConfigurationException || exception is IOException || exception is UnauthorizedAccessException || exception is InvalidOperationException || exception is ArgumentException || exception is OverflowException)
            {
                return new ExternalLegacyVisualProvider(profile, Unavailable(profile, true, "External legacy content preflight failed closed."), null);
            }
        }

        public VisualAssetProviderResult Resolve(VisualAssetId assetId)
        {
            if (!assetId.IsValid || !assetId.Value.StartsWith("external-legacy/playtest/", StringComparison.Ordinal))
                return new VisualAssetProviderResult(VisualAssetProviderResolutionStatus.Missing, ProviderId, assetId);
            bool known = visualAssets.Values.Any(value => value.Binding.VisualAssetId == assetId.Value);
            return new VisualAssetProviderResult(
                IsAvailable && known ? VisualAssetProviderResolutionStatus.Resolved : VisualAssetProviderResolutionStatus.Missing,
                ProviderId,
                assetId);
        }

        public bool TryGetResolvedVisual(HumanPlaytestVisualRole role, out ResolvedLegacyVisual visual)
        {
            visual = null;
            DecodedVisualAsset asset;
            if (!IsAvailable || !visualAssets.TryGetValue(role, out asset) || asset == null || asset.Binding == null) return false;
            visual = asset.Binding;
            return true;
        }

        public bool TryGetSprite(HumanPlaytestVisualRole role, out Sprite sprite)
        {
            sprite = null;
            DecodedVisualAsset asset;
            if (!IsAvailable || !visualAssets.TryGetValue(role, out asset) || asset == null || asset.Shp == null || asset.Palette == null) return false;
            string key = asset.Binding.VisualAssetId + ":" + profile.RoleProfile.RemapProfile;
            if (sprites.TryGetValue(key, out sprite) && sprite != null) return true;
            ShpFrame frame = asset.Shp;
            if (frame.Width <= 0 || frame.Height <= 0 || frame.Width > 4096 || frame.Height > 4096) return false;
            long pixelCount = checked((long)frame.Width * frame.Height);
            if (pixelCount != frame.Indices.LongLength || pixelCount > 16L * 1024 * 1024) return false;
            var colors = new Color32[frame.Indices.Length];
            int offset = profile.RoleProfile.RemapProfile == HumanPlaytestRemapProfile.ImplementationSpecificConfigured
                ? (role == HumanPlaytestVisualRole.EnemyBasicUnit || role == HumanPlaytestVisualRole.EnemyBase || role == HumanPlaytestVisualRole.EnemyFactory
                    ? profile.EnemyTeamOffset : profile.HumanTeamOffset)
                : 0;
            for (int index = 0; index < frame.Indices.Length; index++)
            {
                byte paletteIndex = Remap(frame.Indices[index], offset);
                int paletteOffset = paletteIndex * 3;
                byte alpha = frame.Indices[index] == 0 ? (byte)0 : (byte)255;
                colors[index] = new Color32(
                    Scale(asset.Palette[paletteOffset]),
                    Scale(asset.Palette[paletteOffset + 1]),
                    Scale(asset.Palette[paletteOffset + 2]),
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

        public bool TryGetVoxelMesh(HumanPlaytestVisualRole role, out Mesh mesh)
        {
            mesh = null;
            DecodedVisualAsset asset;
            if (!IsAvailable || !visualAssets.TryGetValue(role, out asset) || asset == null || asset.Voxels == null || asset.Voxels.Count == 0) return false;
            string key = asset.Binding.VisualAssetId + ":" + asset.Binding.Format;
            if (meshes.TryGetValue(key, out mesh) && mesh != null) return true;
            VxlMeshBuildResult result = VxlExposedFaceMeshBuilder.Build(asset.Voxels, new VxlMeshBuildPolicy(65536));
            if (!result.IsSuccess || result.Mesh == null) return false;
            result.Mesh.name = "ExternalLegacyVxlExposedFaces_" + role;
            meshes[key] = result.Mesh;
            mesh = result.Mesh;
            return true;
        }

        public bool TryGetSprite(HumanPlaytestEntityKind kind, bool enemy, out Sprite sprite)
        {
            return TryGetSprite(RoleFor(kind, enemy), out sprite);
        }

        public bool TryGetVoxelMesh(out Mesh mesh)
        {
            return TryGetVoxelMesh(HumanPlaytestVisualRole.HumanBase, out mesh);
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
            foreach (Mesh mesh in meshes.Values)
            {
                if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
            }
            meshes.Clear();
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

        private static HumanPlaytestVisualRole RoleFor(HumanPlaytestEntityKind kind, bool enemy)
        {
            switch (kind)
            {
                case HumanPlaytestEntityKind.Harvester: return HumanPlaytestVisualRole.HumanHarvester;
                case HumanPlaytestEntityKind.MainBase: return enemy ? HumanPlaytestVisualRole.EnemyBase : HumanPlaytestVisualRole.HumanBase;
                case HumanPlaytestEntityKind.Refinery: return HumanPlaytestVisualRole.HumanRefinery;
                case HumanPlaytestEntityKind.Factory: return enemy ? HumanPlaytestVisualRole.EnemyFactory : HumanPlaytestVisualRole.HumanFactory;
                case HumanPlaytestEntityKind.Power: return HumanPlaytestVisualRole.HumanPower;
                default: return enemy ? HumanPlaytestVisualRole.EnemyBasicUnit : HumanPlaytestVisualRole.HumanBasicUnit;
            }
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

        private static IniResolutionResult ResolveTypedIni(
            IReadOnlyList<MixVirtualEntry> entries,
            string sourceId,
            string logicalName,
            long maxBytes)
        {
            LogicalContentPath target = LogicalContentPath.Parse(logicalName);
            MixFileId id = MixFileId.ComputeCandidateId(logicalName);
            MixVirtualEntry[] matches = entries
                .Where(value => value.Id == id && !value.IsMountedArchive)
                .OrderBy(value => value.Provenance.RootArchivePath.Value, StringComparer.OrdinalIgnoreCase)
                .ThenBy(value => value.Id.Value)
                .Take(8)
                .ToArray();
            var inputs = new List<IniProjectBaselineDocumentInput>();
            int ordinal = 0;
            foreach (MixVirtualEntry entry in matches)
            {
                try
                {
                    byte[] bytes = PackedMapBoundedInput.ReadWindow(entry.PayloadWindow, "m6-typed-ini", maxBytes);
                    var chain = new List<LogicalContentPath> { entry.Provenance.RootArchivePath };
                    foreach (MixArchiveProvenanceStep step in entry.Provenance.Steps)
                    {
                        if (step.ResolvedName != null && !chain.Contains(step.ResolvedName)) chain.Add(step.ResolvedName);
                    }
                    if (!chain.Contains(target)) chain.Add(target);
                    IniParseResult parsed = WestwoodIniReader.Read(
                        bytes,
                        new BinarySourceContext("m6-typed-ini", sourceId, target),
                        new IniSourceProvenance(sourceId, chain),
                        IniReadLimits.Default);
                    if (!parsed.IsSuccess || parsed.Document == null) continue;
                    inputs.Add(new IniProjectBaselineDocumentInput(
                        "m6-typed-" + logicalName + "-" + ordinal.ToString("D4"),
                        target,
                        parsed.Document));
                    ordinal = checked(ordinal + 1);
                }
                catch (Exception exception) when (exception is BinaryReadException || exception is IOException || exception is ArgumentException || exception is InvalidOperationException || exception is OverflowException)
                {
                }
            }
            if (inputs.Count == 0) return null;
            IniProjectBaselineLoadPlanBuildResult built = IniProjectBaselineLoadPlanBuilder.Build(
                "m6-typed-" + logicalName,
                sourceId,
                inputs,
                128);
            if (!built.IsComplete) return null;
            IniResolutionEvidence configured = new IniResolutionEvidence(
                IniResolutionEvidenceLevel.ConfiguredForProjectBaseline,
                "m6-human-playtest-typed-visual-routing");
            IniResolutionPolicy policy = IniProjectBaselineLoadPlanBuilder.CreateResolutionPolicy(
                IniNameComparisonPolicy.OrdinalIgnoreCaseAscii,
                configured,
                IniDuplicateSectionPolicy.MergeSectionsInFileOrder,
                configured,
                IniDuplicateKeyPolicy.LastKeyWins,
                configured,
                IniInlineCommentPolicy.PreserveSemicolonInValue,
                configured,
                IniWhitespaceReadPolicy.Preserve,
                configured,
                IniEmptyValuePolicy.OverridesEarlierValue,
                configured);
            return new IniRuntimeResolver().Resolve(built.Plan, built.Candidates, policy);
        }

        private static HumanPlaytestRoleDescriptor[] BuildRoleDescriptors(
            HumanPlaytestVisualRoleProfile profile,
            IniRulesResourceDocument rules,
            IniArtResourceDocument art)
        {
            if (rules == null || art == null) return Array.Empty<HumanPlaytestRoleDescriptor>();
            var values = new List<HumanPlaytestRoleDescriptor>();
            foreach (HumanPlaytestVisualRoleBinding binding in profile.Bindings.OrderBy(value => value.Role))
            {
                IniRulesRegistryKind registry = ToRulesRegistry(binding.Registry);
                IniRulesRegistry typedRegistry = rules.Registries.FirstOrDefault(value => value.Kind == registry);
                IniRulesRegistryEntry rule = typedRegistry == null ? null : typedRegistry.Entries.FirstOrDefault(value =>
                    value.Identifier.Status == IniTypedValueStatus.Present &&
                    string.Equals(value.Identifier.Value.Identifier, binding.TypeId, StringComparison.OrdinalIgnoreCase));
                IniArtResourceRecord artRecord = art.Records.FirstOrDefault(value => string.Equals(value.SectionIdentifier, binding.TypeId, StringComparison.OrdinalIgnoreCase));
                if (rule == null || artRecord == null) continue;
                IniArtResourceField image = artRecord.Fields.FirstOrDefault(value => value.Kind == IniArtFieldKind.Image);
                IniArtResourceField voxel = artRecord.Fields.FirstOrDefault(value => value.Kind == IniArtFieldKind.Voxel);
                IniArtResourceField palette = artRecord.Fields.FirstOrDefault(value => value.Kind == IniArtFieldKind.Palette);
                if (image == null || image.Status != IniTypedValueStatus.Present || image.Parsed == null || image.Parsed.Value == null) continue;
                bool isVoxel = voxel != null && voxel.Status == IniTypedValueStatus.Present && voxel.Parsed.Value.BooleanValue == true;
                string paletteName = palette != null && palette.Status == IniTypedValueStatus.Present && palette.Parsed.Value != null
                    ? palette.Parsed.Value.Identifier
                    : null;
                values.Add(new HumanPlaytestRoleDescriptor(binding.Role, binding.TypeId, image.Parsed.Value.Identifier, isVoxel, paletteName));
            }
            return values.ToArray();
        }

        private static IniRulesRegistryKind ToRulesRegistry(HumanPlaytestRulesRegistry registry)
        {
            switch (registry)
            {
                case HumanPlaytestRulesRegistry.AircraftTypes: return IniRulesRegistryKind.AircraftTypes;
                case HumanPlaytestRulesRegistry.BuildingTypes: return IniRulesRegistryKind.BuildingTypes;
                case HumanPlaytestRulesRegistry.VehicleTypes: return IniRulesRegistryKind.VehicleTypes;
                default: return IniRulesRegistryKind.InfantryTypes;
            }
        }

        private static HumanPlaytestAssetAvailability[] BuildAvailability(
            IEnumerable<HumanPlaytestRoleDescriptor> descriptors,
            HumanPlaytestVisualProfile profile,
            IReadOnlyList<MixVirtualEntry> entries)
        {
            var values = new List<HumanPlaytestAssetAvailability>();
            foreach (HumanPlaytestRoleDescriptor descriptor in descriptors)
            {
                string extension = descriptor.Voxel ? ".vxl" : ".shp";
                string image = EnsureExtension(descriptor.ImageLogicalName, extension);
                MixVirtualEntry imageEntry = FindEntry(entries, image);
                string baseName = image.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
                    ? image.Substring(0, image.Length - extension.Length)
                    : image;
                bool hasHva = descriptor.Voxel && FindEntry(entries, baseName + ".hva") != null;
                string palette = descriptor.PaletteLogicalName ?? profile.RoleProfile.DefaultPaletteLogicalName;
                values.Add(new HumanPlaytestAssetAvailability(image, !descriptor.Voxel && imageEntry != null, descriptor.Voxel && imageEntry != null, hasHva, false));
                values.Add(new HumanPlaytestAssetAvailability(palette, false, false, false, FindEntry(entries, palette) != null));
            }
            return values.GroupBy(value => value.LogicalName, StringComparer.OrdinalIgnoreCase).Select(group =>
            {
                bool shp = group.Any(value => value.HasShp);
                bool vxl = group.Any(value => value.HasVxl);
                bool hva = group.Any(value => value.HasHva);
                bool pal = group.Any(value => value.HasPalette);
                return new HumanPlaytestAssetAvailability(group.Key, shp, vxl, hva, pal);
            }).ToArray();
        }

        private static Dictionary<HumanPlaytestVisualRole, DecodedVisualAsset> DecodeResolvedAssets(
            HumanPlaytestRoleResolutionResult resolution,
            IReadOnlyList<MixVirtualEntry> entries,
            string sourceId,
            HumanPlaytestVisualProfile profile)
        {
            var values = new Dictionary<HumanPlaytestVisualRole, DecodedVisualAsset>();
            foreach (ResolvedLegacyVisual binding in resolution.Resolved.OrderBy(value => value.Role))
            {
                MixVirtualEntry imageEntry = FindEntry(entries, binding.ImageLogicalName);
                MixVirtualEntry paletteEntry = FindEntry(entries, binding.PaletteLogicalName);
                if (imageEntry == null || paletteEntry == null) continue;
                try
                {
                    byte[] imageBytes = PackedMapBoundedInput.ReadWindow(imageEntry.PayloadWindow, "m6-visual-image", profile.MaxAssetBytes);
                    byte[] paletteBytes = PackedMapBoundedInput.ReadWindow(paletteEntry.PayloadWindow, "m6-visual-palette", profile.MaxAssetBytes);
                    PaletteParseResult parsedPalette = WestwoodPaletteReader.Read(
                        paletteBytes,
                        new BinarySourceContext("m6-visual-palette", sourceId, LogicalContentPath.Parse(binding.PaletteLogicalName)),
                        new PaletteSourceProvenance(sourceId, new[] { LogicalContentPath.Parse(binding.PaletteLogicalName) }));
                    if (!parsedPalette.IsSuccess || parsedPalette.Palette == null) continue;
                    var asset = new DecodedVisualAsset { Binding = binding, Palette = ToPaletteBytes(parsedPalette.Palette) };
                    if (binding.Format == HumanPlaytestVisualFormat.Shp)
                    {
                        ShpTsParseResult parsed = WestwoodShpTsReader.Read(
                            imageBytes,
                            new BinarySourceContext("m6-visual-shp", sourceId, LogicalContentPath.Parse(binding.ImageLogicalName)),
                            new ShpTsSourceProvenance(sourceId, new[] { LogicalContentPath.Parse(binding.ImageLogicalName) }));
                        if (!parsed.IsSuccess || parsed.Document == null || parsed.Document.Frames.Count == 0) continue;
                        ShpTsDecodeResult decoded = WestwoodShpTsDecoder.DecodeFrame(imageBytes, parsed.Document, parsed.Document.Frames[0].Index);
                        if (!decoded.IsSuccess || decoded.Frame == null) continue;
                        asset.Shp = new ShpFrame { Width = decoded.Frame.Width, Height = decoded.Frame.Height, Indices = decoded.Frame.GetIndicesCopy() };
                    }
                    else
                    {
                        VxlReadResult parsed = WestwoodVxlReader.Read(imageBytes);
                        if (!parsed.IsSuccess || parsed.Document == null) continue;
                        asset.Voxels = new List<VoxelRenderCell>();
                        AppendVoxels(parsed.Document, asset.Voxels, 65536);
                        if (asset.Voxels.Count == 0) continue;
                    }
                    values[binding.Role] = asset;
                }
                catch (Exception exception) when (exception is BinaryReadException || exception is IOException || exception is ArgumentException || exception is InvalidOperationException || exception is OverflowException)
                {
                }
            }
            return values;
        }

        private static ExternalLegacyVisualStatus CreateStatus(
            HumanPlaytestVisualProfile profile,
            HumanPlaytestRoleResolutionResult resolution,
            IReadOnlyDictionary<HumanPlaytestVisualRole, DecodedVisualAsset> decoded,
            int probed,
            bool fingerprintStable)
        {
            int total = profile.RoleProfile.Bindings.Count;
            int resolved = decoded.Count;
            int shp = decoded.Values.Count(value => value.Binding.Format == HumanPlaytestVisualFormat.Shp);
            int vxl = decoded.Values.Count(value => value.Binding.Format != HumanPlaytestVisualFormat.Shp);
            int hva = decoded.Values.Count(value => value.Binding.HvaBound);
            int humanUnits = decoded.Keys.Count(value => value == HumanPlaytestVisualRole.HumanBasicUnit || value == HumanPlaytestVisualRole.HumanHarvester);
            int humanStructures = decoded.Keys.Count(value => value == HumanPlaytestVisualRole.HumanBase || value == HumanPlaytestVisualRole.HumanRefinery || value == HumanPlaytestVisualRole.HumanFactory || value == HumanPlaytestVisualRole.HumanPower);
            int enemyUnits = decoded.Keys.Count(value => value == HumanPlaytestVisualRole.EnemyBasicUnit);
            int enemyStructures = decoded.Keys.Count(value => value == HumanPlaytestVisualRole.EnemyBase || value == HumanPlaytestVisualRole.EnemyFactory);
            return new ExternalLegacyVisualStatus(
                true,
                probed > 0,
                resolved > 0,
                shp,
                shp,
                resolution.Diagnostics.Count(value => value.Code == HumanPlaytestRoleDiagnosticCode.MissingVisualAsset),
                vxl,
                hva,
                decoded.Count,
                probed,
                total,
                resolved,
                total - resolved,
                shp,
                vxl,
                hva,
                decoded.Count,
                humanUnits,
                humanStructures,
                enemyUnits,
                enemyStructures,
                total - resolved,
                fingerprintStable,
                profile.RoleProfile.RemapProfile,
                "SyntheticFallback",
                resolved > 0 ? "Typed Rules to Art visual roles resolved." : "No safe typed external visual role was resolved.");
        }

        private static bool IsSourceFingerprintStable(ExternalContentConfiguration configuration, string sourceId, string expected)
        {
            try
            {
                ContentIndexResult index = new ContentIndexer().Build(configuration);
                ContentSourceIndex source = index.Sources.FirstOrDefault(value => string.Equals(value.Source.Id, sourceId, StringComparison.Ordinal));
                return source != null && source.IsComplete && string.Equals(source.Fingerprint, expected, StringComparison.Ordinal);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is InvalidOperationException || exception is ArgumentException)
            {
                return false;
            }
        }

        private static MixVirtualEntry FindEntry(IReadOnlyList<MixVirtualEntry> entries, string logicalName)
        {
            if (string.IsNullOrWhiteSpace(logicalName)) return null;
            MixFileId id = MixFileId.ComputeCandidateId(logicalName);
            return entries.Where(value => !value.IsMountedArchive && value.Id == id)
                .OrderBy(value => value.Provenance.RootArchivePath.Value, StringComparer.OrdinalIgnoreCase)
                .ThenBy(value => value.Id.Value)
                .FirstOrDefault();
        }

        private static string EnsureExtension(string name, string extension)
        {
            return name.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? name : name + extension;
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

        private static ExternalLegacyVisualStatus Unavailable(HumanPlaytestVisualProfile profile, bool configured, string message)
        {
            return new ExternalLegacyVisualStatus(
                configured, false, false, 0, 0, 0, 0, 0, 0, 0,
                profile == null || profile.RoleProfile == null ? 0 : profile.RoleProfile.Bindings.Count,
                0,
                profile == null || profile.RoleProfile == null ? 0 : profile.RoleProfile.Bindings.Count,
                0, 0, 0, 0, 0, 0, 0, 0,
                profile == null || profile.RoleProfile == null ? 0 : profile.RoleProfile.Bindings.Count,
                false,
                profile == null || profile.RoleProfile == null ? HumanPlaytestRemapProfile.SourcePaletteOnly : profile.RoleProfile.RemapProfile,
                "SyntheticFallback",
                message);
        }
    }
}
