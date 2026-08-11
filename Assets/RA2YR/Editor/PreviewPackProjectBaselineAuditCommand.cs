using System;
using System.IO;
using System.Linq;
using System.Text;
using RA2YR.Core.Content;
using RA2YR.Core.Content.PackedMap.Audit;
using UnityEditor;
using UnityEngine;

namespace RA2YR.Editor
{
    public static class PreviewPackProjectBaselineAuditCommand
    {
        private const int MaximumSummaryUtf8Bytes = 1024 * 1024;

        public static void Run()
        {
            try
            {
                string repositoryRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string configurationPath = RequiredPath("-ra2yrExternalContentConfig");
                string summaryOutput = RequiredPath("-ra2yrSummaryOutput");
                ValidateRegularFile(configurationPath);
                ValidateSummaryOutput(repositoryRoot, summaryOutput);

                ExternalContentConfiguration configuration = new ExternalContentConfigurationLoader()
                    .Load(configurationPath, repositoryRoot).Configuration;
                ExternalContentSourceDescriptor[] enabled = configuration.Sources.Where(source => source.Enabled).ToArray();
                if (enabled.Length != 1 ||
                    !string.Equals(enabled[0].Id, PreviewPackProjectBaselineAuditService.BaselineLogicalName, StringComparison.Ordinal) ||
                    enabled[0].Kind != ContentSourceKind.Patched)
                    throw new InvalidOperationException("The controlled PreviewPack audit requires exactly one enabled patched ProjectBaseline source.");

                PreviewPackProjectBaselineAuditDelivery delivery = PreviewPackProjectBaselineAuditService.Run(configuration);
                ValidateSanitizedSummary(delivery, configuration, configurationPath, repositoryRoot);
                WriteUtf8Atomically(summaryOutput, delivery.SanitizedSummaryJson);
                Debug.Log("M3-C5 PreviewPack ProjectBaseline audit completed: status=" + delivery.Status +
                          ", candidates=" + delivery.CandidateEntryCount +
                          ", exact=" + delivery.ExactDecodedCount +
                          ", failed=" + delivery.FailedCount +
                          ", aggregateSha256=" + delivery.AggregateSha256);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static string RequiredPath(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index + 1 < arguments.Length; index++)
                if (string.Equals(arguments[index], name, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(arguments[index + 1]))
                    return Path.GetFullPath(arguments[index + 1]);
            throw new ArgumentException("Missing required command-line argument: " + name);
        }

        private static void ValidateRegularFile(string path)
        {
            string reparsePath;
            if (RepositoryPathPolicy.ContainsExistingReparsePoint(path, out reparsePath))
                throw new IOException("A controlled audit input traverses a reparse point.");
            FileAttributes attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.Directory) != 0 || (attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("The audit configuration is not a regular file.");
        }

        private static void ValidateSummaryOutput(string repositoryRoot, string output)
        {
            string resultsRoot = Path.GetFullPath(Path.Combine(repositoryRoot, "TestResults"));
            string normalizedOutput = Path.GetFullPath(output);
            string prefix = resultsRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!normalizedOutput.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetExtension(normalizedOutput), ".json", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The sanitized PreviewPack summary must be a JSON file below TestResults.");
            if (File.Exists(normalizedOutput) || Directory.Exists(normalizedOutput))
                throw new IOException("The sanitized summary output path already exists.");
            string reparsePath;
            if (RepositoryPathPolicy.ContainsExistingReparsePoint(resultsRoot, out reparsePath) ||
                RepositoryPathPolicy.ContainsExistingReparsePoint(Path.GetDirectoryName(normalizedOutput), out reparsePath))
                throw new IOException("The sanitized summary path traverses a reparse point.");
        }

        private static void ValidateSanitizedSummary(
            PreviewPackProjectBaselineAuditDelivery delivery,
            ExternalContentConfiguration configuration,
            string configurationPath,
            string repositoryRoot)
        {
            if (delivery == null || string.IsNullOrWhiteSpace(delivery.SanitizedSummaryJson))
                throw new InvalidOperationException("The PreviewPack audit did not produce a sanitized summary.");
            byte[] utf8 = new UTF8Encoding(false, true).GetBytes(delivery.SanitizedSummaryJson);
            if (utf8.Length > MaximumSummaryUtf8Bytes)
                throw new InvalidOperationException("The sanitized PreviewPack summary exceeds its delivery budget.");
            string[] protectedPaths = new[] { repositoryRoot, configurationPath, configuration.CachePath }
                .Concat(configuration.Sources.Select(source => source.RootPath)).ToArray();
            foreach (string protectedPath in protectedPaths)
            {
                string full = Path.GetFullPath(protectedPath);
                string alternate = full.Replace(Path.DirectorySeparatorChar, '/');
                string escaped = full.Replace("\\", "\\\\");
                if (delivery.SanitizedSummaryJson.IndexOf(full, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    delivery.SanitizedSummaryJson.IndexOf(alternate, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    delivery.SanitizedSummaryJson.IndexOf(escaped, StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new InvalidOperationException("The sanitized summary contains a protected host path.");
            }
            if (!delivery.SanitizedSummaryJson.Contains("\"manifestType\":\"RA2YR.PreviewPackProjectBaselineAuditSanitized\"") ||
                delivery.SanitizedSummaryJson.Contains("\"payload\":") ||
                delivery.SanitizedSummaryJson.Contains("\"pixels\":") ||
                delivery.SanitizedSummaryJson.Contains("\"fileName\":") ||
                delivery.SanitizedSummaryJson.Contains("\"path\":"))
                throw new InvalidOperationException("The sanitized PreviewPack summary contains forbidden detail.");
        }

        private static void WriteUtf8Atomically(string path, string content)
        {
            string directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);
            string temp = Path.Combine(directory, "." + Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.WriteThrough))
                {
                    byte[] bytes = new UTF8Encoding(false, true).GetBytes(content);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                File.Move(temp, path);
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }
    }
}
