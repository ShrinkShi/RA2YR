using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RA2YR.Core.Content;
using RA2YR.Core.Content.Mix.Audit;
using UnityEditor;
using UnityEngine;

namespace RA2YR.Editor
{
    public static class MixBaselineAuditCommand
    {
        private const int MaximumSummaryUtf8Bytes = 8 * 1024 * 1024;

        public static void Run()
        {
            try
            {
                string repositoryRoot = Path.GetFullPath(
                    Path.Combine(Application.dataPath, ".."));
                string configurationPath = GetRequiredPathArgument(
                    "-ra2yrExternalContentConfig");
                string xccDatabasePath = GetRequiredPathArgument(
                    "-ra2yrXccGlobalNameDatabase");
                string summaryOutput = GetRequiredPathArgument("-ra2yrSummaryOutput");

                ValidateSummaryOutput(repositoryRoot, summaryOutput);
                ValidateRegularInputFile(xccDatabasePath);
                ExternalContentConfiguration configuration =
                    new ExternalContentConfigurationLoader()
                        .Load(configurationPath, repositoryRoot)
                        .Configuration;
                ValidateBaselineConfiguration(configuration);

                MixBaselineAuditDelivery delivery = MixBaselineAuditService.Run(
                    configuration,
                    xccDatabasePath);
                ValidateSanitizedDelivery(
                    delivery,
                    configuration,
                    configurationPath,
                    xccDatabasePath,
                    repositoryRoot);
                WriteNewUtf8FileAtomically(summaryOutput, delivery.SanitizedSummaryJson);

                Debug.Log(
                    "WP-02C MIX baseline audit completed: status=" + delivery.Status +
                    ", roots=" + delivery.RootArchiveCount +
                    ", parsed=" + delivery.ParsedRootArchiveCount +
                    ", failed=" + delivery.FailedRootArchiveCount +
                    ", externalManifestSha256=" + delivery.ExternalManifestSha256);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static string GetRequiredPathArgument(string name)
        {
            return Path.GetFullPath(GetRequiredValueArgument(name));
        }

        private static string GetRequiredValueArgument(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], name, StringComparison.Ordinal))
                {
                    string value = arguments[index + 1];
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }

            throw new ArgumentException("Missing required command-line argument: " + name);
        }

        private static void ValidateBaselineConfiguration(
            ExternalContentConfiguration configuration)
        {
            ExternalContentSourceDescriptor[] enabled = configuration.Sources
                .Where(source => source.Enabled)
                .ToArray();
            if (enabled.Length != 1 ||
                !string.Equals(
                    enabled[0].Id,
                    MixBaselineAuditService.BaselineLogicalName,
                    StringComparison.Ordinal) ||
                enabled[0].Kind != ContentSourceKind.Patched)
            {
                throw new InvalidOperationException(
                    "The controlled MIX audit requires exactly one enabled patched " +
                    "YR1001_ProjectBaseline source.");
            }
        }

        private static void ValidateSummaryOutput(
            string repositoryRoot,
            string summaryOutput)
        {
            string resultsRoot = Path.GetFullPath(
                Path.Combine(repositoryRoot, "TestResults"));
            string output = Path.GetFullPath(summaryOutput);
            string prefix = resultsRoot.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            if (!output.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    Path.GetExtension(output),
                    ".json",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The sanitized summary output must be a JSON file below TestResults.");
            }

            RejectExistingReparsePoint(resultsRoot);
            RejectExistingReparsePoint(Path.GetDirectoryName(output));
            if (File.Exists(output) || Directory.Exists(output))
            {
                throw new IOException("The sanitized summary output path already exists.");
            }
        }

        private static void ValidateRegularInputFile(string path)
        {
            RejectExistingReparsePoint(path);
            FileAttributes attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.Directory) != 0 ||
                (attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new IOException("A controlled MIX audit input is not a regular file.");
            }
        }

        private static void RejectExistingReparsePoint(string path)
        {
            string reparsePointPath;
            if (RepositoryPathPolicy.ContainsExistingReparsePoint(
                    path,
                    out reparsePointPath))
            {
                throw new IOException("A controlled MIX audit path traverses a reparse point.");
            }
        }

        private static void ValidateSanitizedDelivery(
            MixBaselineAuditDelivery delivery,
            ExternalContentConfiguration configuration,
            string configurationPath,
            string xccDatabasePath,
            string repositoryRoot)
        {
            if (delivery == null || string.IsNullOrWhiteSpace(delivery.SanitizedSummaryJson))
            {
                throw new InvalidOperationException(
                    "The MIX audit did not produce a sanitized summary.");
            }

            if (delivery.RootArchiveCount <= 0 ||
                delivery.ParsedRootArchiveCount < 0 ||
                delivery.FailedRootArchiveCount < 0 ||
                delivery.ParsedRootArchiveCount + delivery.FailedRootArchiveCount !=
                    delivery.RootArchiveCount)
            {
                throw new InvalidOperationException(
                    "The MIX audit returned inconsistent root archive counts.");
            }

            byte[] utf8 = new UTF8Encoding(false, true).GetBytes(
                delivery.SanitizedSummaryJson);
            if (utf8.Length > MaximumSummaryUtf8Bytes)
            {
                throw new InvalidOperationException(
                    "The sanitized MIX audit summary exceeds its delivery budget.");
            }

            var protectedPaths = new List<string>
            {
                repositoryRoot,
                configurationPath,
                configuration.CachePath,
                xccDatabasePath
            };
            protectedPaths.AddRange(configuration.Sources.Select(source => source.RootPath));
            foreach (string protectedPath in protectedPaths)
            {
                RejectSensitivePath(delivery.SanitizedSummaryJson, protectedPath);
            }

            if (!delivery.SanitizedSummaryJson.Contains(
                    "\"baselineLogicalName\":\"YR1001_ProjectBaseline\""))
            {
                throw new InvalidOperationException(
                    "The sanitized summary does not identify the approved baseline.");
            }
        }

        private static void RejectSensitivePath(string summary, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string fullPath = Path.GetFullPath(path);
            string alternate = fullPath.Replace(Path.DirectorySeparatorChar, '/');
            string jsonEscaped = fullPath.Replace("\\", "\\\\");
            if (summary.IndexOf(fullPath, StringComparison.OrdinalIgnoreCase) >= 0 ||
                summary.IndexOf(jsonEscaped, StringComparison.OrdinalIgnoreCase) >= 0 ||
                summary.IndexOf(alternate, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new InvalidOperationException(
                    "The sanitized summary contains a protected host path.");
            }
        }

        private static void WriteNewUtf8FileAtomically(string path, string content)
        {
            string directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);
            RejectExistingReparsePoint(directory);
            string temporaryPath = Path.Combine(
                directory,
                "." + Path.GetFileName(path) + "." +
                Guid.NewGuid().ToString("N") + ".tmp");
            byte[] bytes = new UTF8Encoding(false, true).GetBytes(content);

            try
            {
                using (var stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    64 * 1024,
                    FileOptions.WriteThrough))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                RejectExistingReparsePoint(directory);
                File.Move(temporaryPath, path);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }
    }
}
