using System;
using System.IO;
using System.Linq;
using System.Text;
using RA2YR.Core.Content;
using UnityEditor;
using UnityEngine;

namespace RA2YR.Editor
{
    public static class ContentBaselineManifestCommand
    {
        private const string ExpectedBaselineName = "YR1001_ProjectBaseline";

        public static void Run()
        {
            try
            {
                string repositoryRoot = Path.GetFullPath(
                    Path.Combine(Application.dataPath, ".."));
                string configurationPath = GetRequiredPathArgument(
                    "-ra2yrExternalContentConfig");
                string baselineName = GetRequiredValueArgument("-ra2yrBaselineSourceId");
                string summaryOutput = GetRequiredPathArgument("-ra2yrSummaryOutput");
                if (!string.Equals(
                        baselineName,
                        ExpectedBaselineName,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "This command only produces the approved YR1001_ProjectBaseline summary.");
                }

                ValidateSummaryOutput(repositoryRoot, summaryOutput);
                ExternalContentConfiguration configuration =
                    new ExternalContentConfigurationLoader()
                        .Load(configurationPath, repositoryRoot)
                        .Configuration;
                ExternalContentSourceDescriptor[] enabledSources = configuration.Sources
                    .Where(source => source.Enabled)
                    .ToArray();
                if (enabledSources.Length != 1 ||
                    !string.Equals(
                        enabledSources[0].Id,
                        baselineName,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "The controlled ProjectBaseline command requires exactly one enabled " +
                        "source with the approved baseline identity.");
                }

                DateTime startedUtc = DateTime.UtcNow;
                ContentIndexResult index = new ContentIndexer().Build(configuration);
                DateTime completedUtc = DateTime.UtcNow;
                ContentResolutionResult resolution = new ContentResolver().Resolve(index);
                if (!resolution.IsComplete)
                {
                    string codes = string.Join(
                        ",",
                        resolution.Diagnostics
                            .Select(item => item.Code.ToString())
                            .Distinct()
                            .OrderBy(value => value, StringComparer.Ordinal));
                    throw new InvalidOperationException(
                        "Baseline indexing or resolution was incomplete. Diagnostic codes: " +
                        codes);
                }

                string[] approvedRepresentatives =
                {
                    "ra2md.mix",
                    "langmd.mix",
                    "MAPSMD03.MIX",
                    "expandmd01.mix",
                    "ddraw.dll"
                };
                ValidateApprovedRepresentatives(
                    resolution,
                    baselineName,
                    approvedRepresentatives);

                ExternalManifestWriteResult manifest =
                    new ExternalContentManifestWriter().Write(
                        configuration,
                        resolution,
                        baselineName);
                ContentBaselineSummary summary = ContentBaselineSummaryBuilder.Build(
                    baselineName,
                    resolution,
                    manifest,
                    startedUtc,
                    completedUtc,
                    approvedRepresentatives,
                    new[]
                    {
                        "YR1001_ProjectBaseline includes the official map add-on, music " +
                        "pack, and Windows compatibility patch; it is not a clean YR 1.001 baseline.",
                        "Directory-level indexing only; MIX payloads were not parsed.",
                        "YR 1.001 original behavior comparison is not complete."
                    },
                    "未在当前已挂载的目录型内容源中发现；MIX 内容尚未解析。");
                WriteNewUtf8File(
                    summaryOutput,
                    ContentBaselineSummarySerializer.SerializeJson(summary));

                Debug.Log(
                    "WP-02A baseline index completed: files=" + summary.TotalFileCount +
                    ", bytes=" + summary.TotalBytes +
                    ", manifestSha256=" + summary.ManifestSha256);
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
                !string.Equals(Path.GetExtension(output), ".json", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The sanitized summary output must be a JSON file below TestResults.");
            }

            RejectExistingReparsePoint(resultsRoot);
            RejectExistingReparsePoint(Path.GetDirectoryName(output));
            if (File.Exists(output) || Directory.Exists(output))
            {
                throw new IOException("The summary output path already exists.");
            }
        }

        private static void RejectExistingReparsePoint(string path)
        {
            string reparsePointPath;
            if (RepositoryPathPolicy.ContainsExistingReparsePoint(
                    path,
                    out reparsePointPath))
            {
                throw new IOException("The summary output path traverses a reparse point.");
            }
        }

        private static void ValidateApprovedRepresentatives(
            ContentResolutionResult resolution,
            string baselineName,
            string[] approvedPaths)
        {
            foreach (string approvedPath in approvedPaths)
            {
                LogicalContentPath logicalPath = LogicalContentPath.Parse(approvedPath);
                ContentPathResolution entry = resolution.Entries.SingleOrDefault(
                    candidate => candidate.LogicalPath.Equals(logicalPath));
                if (entry == null || entry.Selected == null ||
                    !string.Equals(
                        entry.Selected.Source.Id,
                        baselineName,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "An approved representative is not selected from the baseline: " +
                        approvedPath);
                }
            }
        }

        private static void WriteNewUtf8File(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            RejectExistingReparsePoint(Path.GetDirectoryName(path));
            byte[] bytes = new UTF8Encoding(false, true).GetBytes(content);
            using (var stream = new FileStream(
                       path,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();
            }
        }
    }
}
