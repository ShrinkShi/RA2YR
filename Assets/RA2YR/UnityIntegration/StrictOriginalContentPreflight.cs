using System;
using System.IO;
using System.Linq;
using RA2YR.Core.Content;

namespace RA2YR.UnityIntegration
{
    public enum StrictOriginalContentPreflightStatus
    {
        NotRun,
        MissingConfiguration,
        IndexFailed,
        MissingRequiredCategory,
        PresentationRouteIncomplete,
        Ready
    }

    public sealed class StrictOriginalContentPreflightResult
    {
        internal StrictOriginalContentPreflightResult(StrictOriginalContentPreflightStatus status, int mixRoots, int nestedMix, int palette, int shp, int vxl, int hva, int tmp, int maps, bool providerReady, bool terrainBound, bool resourceBound, string message)
        {
            Status = status;
            MixRootCount = mixRoots;
            NestedMixCount = nestedMix;
            PaletteCount = palette;
            ShpCount = shp;
            VxlCount = vxl;
            HvaCount = hva;
            TmpCount = tmp;
            MapCount = maps;
            ProviderReady = providerReady;
            TerrainPresentationBound = terrainBound;
            ResourcePresentationBound = resourceBound;
            Message = message ?? string.Empty;
        }
        public StrictOriginalContentPreflightStatus Status { get; }
        public int MixRootCount { get; }
        public int NestedMixCount { get; }
        public int PaletteCount { get; }
        public int ShpCount { get; }
        public int VxlCount { get; }
        public int HvaCount { get; }
        public int TmpCount { get; }
        public int MapCount { get; }
        public bool ProviderReady { get; }
        public bool TerrainPresentationBound { get; }
        public bool ResourcePresentationBound { get; }
        public string Message { get; }
        public bool IsReady => Status == StrictOriginalContentPreflightStatus.Ready;
    }

    /// <summary>
    /// Validates the configured source and presentation capabilities before a
    /// human strict-content session starts. It never searches outside the
    /// explicit configuration and never publishes a physical path or payload.
    /// </summary>
    public static class StrictOriginalContentPreflight
    {
        public static StrictOriginalContentPreflightResult Run(string configurationPath, string repositoryRoot, ExternalLegacyVisualStatus visualStatus, bool terrainPresentationBound, bool resourcePresentationBound)
        {
            if (string.IsNullOrWhiteSpace(configurationPath) || !File.Exists(configurationPath))
                return new StrictOriginalContentPreflightResult(StrictOriginalContentPreflightStatus.MissingConfiguration, 0, 0, 0, 0, 0, 0, 0, 0, visualStatus != null && visualStatus.IsStrictRealContentReady, terrainPresentationBound, resourcePresentationBound, "The configured original-content manifest is missing.");
            try
            {
                ExternalContentConfigurationLoadResult loaded = new ExternalContentConfigurationLoader().Load(configurationPath, repositoryRoot);
                ExternalContentSourceDescriptor source = loaded.Configuration.Sources.FirstOrDefault(x => x.Enabled && x.Id == "YR1001_ProjectBaseline");
                if (source == null) return new StrictOriginalContentPreflightResult(StrictOriginalContentPreflightStatus.IndexFailed, 0, 0, 0, 0, 0, 0, 0, 0, false, terrainPresentationBound, resourcePresentationBound, "The authoritative configured original-content source is unavailable.");
                ContentIndexResult index = new ContentIndexer().Build(loaded.Configuration);
                ContentSourceIndex sourceIndex = index.Sources.FirstOrDefault(x => x.Source.Id == source.Id);
                if (!index.IsComplete || sourceIndex == null || !sourceIndex.IsComplete)
                    return new StrictOriginalContentPreflightResult(StrictOriginalContentPreflightStatus.IndexFailed, 0, 0, 0, 0, 0, 0, 0, 0, false, terrainPresentationBound, resourcePresentationBound, "The configured original-content index is incomplete.");
                var files = sourceIndex.Files.Select(x => x.LogicalPath.Value).ToArray();
                ExternalVisualRouteDiagnostics route = visualStatus == null ? null : visualStatus.RouteDiagnostics;
                int mix = files.Count(x => x.EndsWith(".mix", StringComparison.OrdinalIgnoreCase));
                int pal = files.Count(x => x.EndsWith(".pal", StringComparison.OrdinalIgnoreCase));
                int shp = files.Count(x => x.EndsWith(".shp", StringComparison.OrdinalIgnoreCase));
                int vxl = files.Count(x => x.EndsWith(".vxl", StringComparison.OrdinalIgnoreCase));
                int hva = files.Count(x => x.EndsWith(".hva", StringComparison.OrdinalIgnoreCase));
                int tmp = files.Count(x => x.EndsWith(".tem", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".sno", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".urb", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".des", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".lun", StringComparison.OrdinalIgnoreCase));
                int maps = files.Count(x => x.EndsWith(".map", StringComparison.OrdinalIgnoreCase));
                // The source index is intentionally a bounded manifest of the
                // configured root. Nested MIX entries are proven by the visual
                // route, not by assuming that they are loose files in the root.
                if (route != null)
                {
                    mix = Math.Max(mix, route.RootMixCount);
                    pal = Math.Max(pal, route.PaletteVfsMatches);
                    shp = Math.Max(shp, route.ShpDecodeSuccess + route.ShpDecodeFailed);
                    vxl = Math.Max(vxl, route.VxlDecodeSuccess + route.VxlDecodeFailed);
                    hva = Math.Max(hva, route.HvaBindSuccess + route.HvaBindFailed);
                }
                bool categories = mix > 0 && pal > 0 && (shp > 0 || vxl > 0) && hva > 0;
                bool providerReady = visualStatus != null && visualStatus.IsStrictRealContentReady;
                StrictOriginalContentPreflightStatus status = !categories ? StrictOriginalContentPreflightStatus.MissingRequiredCategory : !providerReady ? StrictOriginalContentPreflightStatus.PresentationRouteIncomplete : terrainPresentationBound && resourcePresentationBound ? StrictOriginalContentPreflightStatus.Ready : StrictOriginalContentPreflightStatus.PresentationRouteIncomplete;
                string message = status == StrictOriginalContentPreflightStatus.Ready ? "Strict original-content preflight completed." : "Strict original-content preflight requires real terrain and resource presentation bindings; synthetic fallback is disabled.";
                return new StrictOriginalContentPreflightResult(status, mix, route == null ? 0 : route.MountedArchiveCount, pal, shp, vxl, hva, tmp, maps, providerReady, terrainPresentationBound, resourcePresentationBound, message);
            }
            catch (Exception exception) when (exception is IOException || exception is InvalidOperationException || exception is ArgumentException)
            {
                return new StrictOriginalContentPreflightResult(StrictOriginalContentPreflightStatus.IndexFailed, 0, 0, 0, 0, 0, 0, 0, 0, false, terrainPresentationBound, resourcePresentationBound, "The configured original-content preflight failed closed: " + exception.GetType().Name + ".");
            }
        }
    }
}
