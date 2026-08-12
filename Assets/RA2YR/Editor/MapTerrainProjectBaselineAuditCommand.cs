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
    public static class MapTerrainProjectBaselineAuditCommand
    {
        public static void Run()
        {
            try
            {
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string config = Required("-ra2yrExternalContentConfig");
                string output = Required("-ra2yrSummaryOutput");
                ExternalContentConfiguration loaded = new ExternalContentConfigurationLoader().Load(config, root).Configuration;
                if (loaded.Sources.Count(s => s.Enabled) != 1 || loaded.Sources.Single(s => s.Enabled).Id != MapTerrainProjectBaselineAuditService.BaselineLogicalName) throw new InvalidOperationException("Map terrain audit requires exactly one enabled ProjectBaseline source.");
                MapTerrainProjectBaselineAuditDelivery delivery = MapTerrainProjectBaselineAuditService.Run(loaded);
                string results = Path.GetFullPath(Path.Combine(root, "TestResults"));
                string normalized = Path.GetFullPath(output);
                if (!normalized.StartsWith(results.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || File.Exists(normalized) || Directory.Exists(normalized) || !normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Map terrain summary output must be a new JSON below TestResults.");
                if (delivery.SanitizedSummaryJson.Contains("\"records\":[") || delivery.SanitizedSummaryJson.Contains("\"pixels\":[") || delivery.SanitizedSummaryJson.Contains("\"filename\":")) throw new InvalidOperationException("Map terrain summary contains forbidden detail.");
                Directory.CreateDirectory(Path.GetDirectoryName(normalized));
                File.WriteAllText(normalized, delivery.SanitizedSummaryJson, new UTF8Encoding(false));
                Debug.Log("M3-C7 map terrain audit completed: status=" + delivery.Status + ", maps=" + delivery.MapCandidateCount + ", failures=" + delivery.FailureCount);
                EditorApplication.Exit(0);
            }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }
        private static string Required(string name)
        { string[] args = Environment.GetCommandLineArgs(); for (int i = 0; i + 1 < args.Length; i++) if (args[i] == name && !string.IsNullOrWhiteSpace(args[i + 1])) return Path.GetFullPath(args[i + 1]); throw new ArgumentException("Missing command-line argument: " + name); }
    }
}
