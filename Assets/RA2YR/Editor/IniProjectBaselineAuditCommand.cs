using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RA2YR.Core.Content;
using RA2YR.Core.Content.Ini.Audit;
using UnityEditor;
using UnityEngine;

namespace RA2YR.Editor
{
    public static class IniProjectBaselineAuditCommand
    {
        private const int MaximumSummaryUtf8Bytes = 2 * 1024 * 1024;

        public static void Run()
        {
            try
            {
                string repositoryRoot = Path.GetFullPath(
                    Path.Combine(Application.dataPath, ".."));
                string configurationPath = GetRequiredPathArgument(
                    "-ra2yrExternalContentConfig");
                string summaryOutput = GetRequiredPathArgument("-ra2yrSummaryOutput");

                ValidateRegularInputFile(configurationPath);
                ValidateSummaryOutput(repositoryRoot, summaryOutput);
                ExternalContentConfiguration configuration =
                    new ExternalContentConfigurationLoader()
                        .Load(configurationPath, repositoryRoot)
                        .Configuration;
                ValidateBaselineConfiguration(configuration);

                IniProjectBaselineAuditDelivery delivery =
                    IniProjectBaselineAuditService.Run(configuration);
                ValidateSanitizedDelivery(
                    delivery,
                    configuration,
                    configurationPath,
                    repositoryRoot);
                WriteNewUtf8FileAtomically(summaryOutput, delivery.SanitizedSummaryJson);

                Debug.Log(
                    "WP-02F INI ProjectBaseline audit completed: status=" + delivery.Status +
                    ", documents=" + delivery.DocumentCount +
                    ", surveyCandidates=" + delivery.LocatedSurveyCandidateCount +
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
                if (string.Equals(arguments[index], name, StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(arguments[index + 1]))
                {
                    return arguments[index + 1];
                }
            }

            throw new ArgumentException("Missing required command-line argument: " + name);
        }

        private static void ValidateBaselineConfiguration(
            ExternalContentConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new InvalidOperationException(
                    "The controlled INI audit configuration did not load.");
            }

            ExternalContentSourceDescriptor[] enabled = configuration.Sources
                .Where(source => source.Enabled)
                .ToArray();
            if (enabled.Length != 1 ||
                !string.Equals(
                    enabled[0].Id,
                    IniProjectBaselineAuditService.BaselineLogicalName,
                    StringComparison.Ordinal) ||
                enabled[0].Kind != ContentSourceKind.Patched)
            {
                throw new InvalidOperationException(
                    "The controlled INI audit requires exactly one enabled patched " +
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
                    "The sanitized INI summary output must be a JSON file below TestResults.");
            }

            RejectExistingReparsePoint(resultsRoot);
            RejectExistingReparsePoint(Path.GetDirectoryName(output));
            if (File.Exists(output) || Directory.Exists(output))
            {
                throw new IOException("The sanitized INI summary output path already exists.");
            }
        }

        private static void ValidateRegularInputFile(string path)
        {
            RejectExistingReparsePoint(path);
            FileAttributes attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.Directory) != 0 ||
                (attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new IOException("A controlled INI audit input is not a regular file.");
            }
        }

        private static void RejectExistingReparsePoint(string path)
        {
            string reparsePointPath;
            if (RepositoryPathPolicy.ContainsExistingReparsePoint(
                    path,
                    out reparsePointPath))
            {
                throw new IOException("A controlled INI audit path traverses a reparse point.");
            }
        }

        private static void ValidateSanitizedDelivery(
            IniProjectBaselineAuditDelivery delivery,
            ExternalContentConfiguration configuration,
            string configurationPath,
            string repositoryRoot)
        {
            if (delivery == null || string.IsNullOrWhiteSpace(delivery.SanitizedSummaryJson) ||
                delivery.Status != IniProjectBaselineAuditStatus.Complete ||
                delivery.DocumentCount != 4 ||
                delivery.ExternalManifestLength <= 0 ||
                !IsLowerSha256(delivery.ExternalManifestSha256))
            {
                throw new InvalidOperationException(
                    "The INI audit returned an incomplete or inconsistent delivery.");
            }

            LogicalContentPath.Parse(delivery.ExternalManifestCacheRelativePath);
            byte[] utf8 = new UTF8Encoding(false, true).GetBytes(
                delivery.SanitizedSummaryJson);
            if (utf8.Length > MaximumSummaryUtf8Bytes)
            {
                throw new InvalidOperationException(
                    "The sanitized INI audit summary exceeds its delivery budget.");
            }

            var protectedPaths = new List<string>
            {
                repositoryRoot,
                configurationPath,
                configuration.CachePath
            };
            protectedPaths.AddRange(configuration.Sources.Select(source => source.RootPath));
            foreach (string protectedPath in protectedPaths)
            {
                RejectSensitivePath(delivery.SanitizedSummaryJson, protectedPath);
            }

            string summary = delivery.SanitizedSummaryJson;
            if (!summary.Contains(
                    "\"manifestType\":\"RA2YR.IniProjectBaselineAuditSanitized\"") ||
                !summary.Contains("\"baselineLogicalName\":\"YR1001_ProjectBaseline\"") ||
                !summary.Contains("\"samples\":[") ||
                !summary.Contains("\"byteIdentical\":true") ||
                summary.Contains("\"lineRecords\":[") ||
                summary.Contains("\"identityCacheRelativePath\":") ||
                summary.Contains("\"rawBytes\":") ||
                summary.Contains("\"sectionName\":") ||
                summary.Contains("\"keyName\":") ||
                summary.Contains("\"valueText\":"))
            {
                throw new InvalidOperationException(
                    "The sanitized INI summary does not match the approved delivery identity.");
            }
        }

        private static bool IsLowerSha256(string value)
        {
            if (value == null || value.Length != 64)
            {
                return false;
            }

            return value.All(character =>
                (character >= '0' && character <= '9') ||
                (character >= 'a' && character <= 'f'));
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
                    "The sanitized INI summary contains a protected host path.");
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
