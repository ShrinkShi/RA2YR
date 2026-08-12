using System;
using System.IO;
using System.Linq;
using System.Text;
using RA2YR.Core.Content;
using RA2YR.Core.Content.TmpTheater.Audit;
using UnityEditor;
using UnityEngine;

namespace RA2YR.Editor
{
    public static class TmpTheaterProjectBaselineAuditCommand
    {
        public static void Run()
        {
            try
            {
                string repositoryRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string configurationPath = RequiredPath("-ra2yrExternalContentConfig");
                string summaryPath = RequiredPath("-ra2yrSummaryOutput");
                ExternalContentConfiguration configuration = new ExternalContentConfigurationLoader().Load(configurationPath, repositoryRoot).Configuration;
                TmpTheaterProjectBaselineAuditDelivery delivery = TmpTheaterProjectBaselineAuditService.Run(configuration);
                ValidateSummary(delivery, repositoryRoot, configurationPath);
                WriteUtf8Atomically(summaryPath, delivery.SanitizedSummaryJson);
                Debug.Log("M3-C6 TMP/theater ProjectBaseline audit completed: status=" + delivery.Status + ", candidates=" + delivery.CandidateCount + ", failures=" + delivery.FailureCount + ", aggregateSha256=" + delivery.AggregateSha256);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static string RequiredPath(string argument)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (string.Equals(args[i], argument, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(args[i + 1])) return Path.GetFullPath(args[i + 1]);
            throw new ArgumentException("Missing required command-line argument: " + argument);
        }

        private static void ValidateSummary(TmpTheaterProjectBaselineAuditDelivery delivery, string repositoryRoot, string configurationPath)
        {
            if (delivery == null || string.IsNullOrWhiteSpace(delivery.SanitizedSummaryJson)) throw new InvalidOperationException("TMP/theater audit did not produce a summary.");
            if (delivery.SanitizedSummaryJson.Length > 1024 * 1024) throw new InvalidOperationException("TMP/theater audit summary exceeds its budget.");
            string[] protectedValues = { repositoryRoot, configurationPath };
            foreach (string value in protectedValues) if (delivery.SanitizedSummaryJson.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0) throw new InvalidOperationException("TMP/theater summary contains a protected path.");
            if (!delivery.SanitizedSummaryJson.Contains("\"manifestType\":\"RA2YR.TmpTheaterProjectBaselineAuditSanitized\"") || delivery.SanitizedSummaryJson.Contains("\"bytes\"") || delivery.SanitizedSummaryJson.Contains("\"pixels\"")) throw new InvalidOperationException("TMP/theater summary is not sanitized.");
        }

        private static void WriteUtf8Atomically(string path, string content)
        {
            string directory = Path.GetDirectoryName(path); Directory.CreateDirectory(directory); string temp = Path.Combine(directory, "." + Path.GetFileName(path) + ".tmp");
            try { File.WriteAllText(temp, content, new UTF8Encoding(false)); File.Move(temp, path); } finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }
}
