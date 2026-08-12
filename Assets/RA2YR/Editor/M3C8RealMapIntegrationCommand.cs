using System;
using System.IO;
using System.Linq;
using System.Text;
using RA2YR.Core.Content;
using RA2YR.Core.Content.MapTerrain.Audit;
using UnityEditor;
using UnityEngine;

namespace RA2YR.Editor
{
    public static class M3C8RealMapIntegrationCommand
    {
        public static void Run()
        {
            try
            {
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string config = Required("-ra2yrExternalContentConfig");
                string output = Required("-ra2yrSummaryOutput");
                ExternalContentConfiguration loaded = new ExternalContentConfigurationLoader().Load(config, root).Configuration;
                ExternalContentSourceDescriptor[] enabledSources = loaded.Sources.Where(s => s.Enabled).ToArray();
                if (enabledSources.Length != 1 || enabledSources[0].Id != "YR1001_ProjectBaseline" || enabledSources[0].Kind != ContentSourceKind.Patched)
                    throw new InvalidOperationException("M3-C8 requires exactly one enabled ProjectBaseline source.");
                M3C8RealMapIntegrationDelivery delivery = M3C8RealMapIntegrationService.Run(loaded);
                string results = Path.GetFullPath(Path.Combine(root, "TestResults"));
                string normalized = Path.GetFullPath(output);
                string configPath = Path.GetFullPath(config);
                string sourceRoot = enabledSources[0].RootPath;
                if (!normalized.StartsWith(results.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                    File.Exists(normalized) ||
                    !normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                    delivery.SanitizedSummaryJson.Contains(configPath, StringComparison.OrdinalIgnoreCase) ||
                    delivery.SanitizedSummaryJson.Contains(sourceRoot, StringComparison.OrdinalIgnoreCase) ||
                    delivery.SanitizedSummaryJson.Contains("\\", StringComparison.Ordinal))
                    throw new InvalidOperationException("M3-C8 summary output must be a new JSON below TestResults.");
                if (delivery.SanitizedSummaryJson.Contains("\"records\":[") || delivery.SanitizedSummaryJson.Contains("\"pixels\":[") || delivery.SanitizedSummaryJson.Contains("\"filename\":"))
                    throw new InvalidOperationException("M3-C8 summary contains forbidden detail.");
                Directory.CreateDirectory(Path.GetDirectoryName(normalized));
                File.WriteAllText(normalized, delivery.SanitizedSummaryJson, new UTF8Encoding(false));
                Debug.Log("M3-C8 real-map integration completed: status=" + delivery.Status + ", candidates=" + delivery.MapCandidateCount + ", unresolved=" + delivery.TerrainUnresolvedCount);
                EditorApplication.Exit(0);
            }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static string Required(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int index = 0; index + 1 < args.Length; index++)
                if (args[index] == name && !string.IsNullOrWhiteSpace(args[index + 1])) return Path.GetFullPath(args[index + 1]);
            throw new ArgumentException("Missing command-line argument: " + name);
        }
    }
}
