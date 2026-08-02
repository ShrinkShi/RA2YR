using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Mix.Interop;
using UnityEditor;
using UnityEngine;

namespace RA2YR.Editor
{
    public static class XccSyntheticInteropCommand
    {
        private const string CommandResultRootRelative =
            "wp02c/xcc-interop-command-results";
        private const int MaximumResultUtf8Bytes = 1024 * 1024;

        public static void Run()
        {
            try
            {
                string repositoryRoot = Path.GetFullPath(
                    Path.Combine(Application.dataPath, ".."));
                string configurationPath = GetRequiredPathArgument(
                    "-ra2yrExternalContentConfig");
                string modeValue = GetRequiredValueArgument("-ra2yrXccInteropMode");
                string caseId = GetRequiredValueArgument("-ra2yrXccInteropCaseId");
                string resultOutput = GetRequiredPathArgument(
                    "-ra2yrXccInteropResultOutput");

                XccSyntheticInteropStage stage = GetExpectedStage(modeValue);
                ValidateCaseId(caseId);
                ExternalContentConfiguration configuration =
                    new ExternalContentConfigurationLoader()
                        .Load(configurationPath, repositoryRoot)
                        .Configuration;
                ValidateResultOutput(configuration, resultOutput);

                var service = new XccSyntheticInteropService();
                XccSyntheticInteropResult result = RunStage(
                    service,
                    configuration,
                    modeValue,
                    caseId);
                ValidateResult(result, stage, caseId, configuration, configurationPath);
                string json = SerializeResult(result, modeValue);
                RejectSensitivePaths(json, configuration, configurationPath);
                WriteNewUtf8FileAtomically(resultOutput, json);

                Debug.Log(
                    "WP-02C XCC synthetic interop command completed: stage=" + result.Stage +
                    ", success=" + result.IsSuccess +
                    ", artifacts=" + result.Artifacts.Count +
                    ", diagnostics=" + result.Diagnostics.Count);
                EditorApplication.Exit(result.IsSuccess ? 0 : 1);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static XccSyntheticInteropResult RunStage(
            XccSyntheticInteropService service,
            ExternalContentConfiguration configuration,
            string mode,
            string caseId)
        {
            switch (mode)
            {
                case "Prepare":
                    return service.PrepareInternalContract(configuration, caseId);
                case "VerifyXccCreated":
                    return service.ValidateStagedCreatedCandidate(configuration, caseId);
                case "VerifyXccExtractions":
                    return service.ValidateStagedExtractionCandidates(configuration, caseId);
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        private static XccSyntheticInteropStage GetExpectedStage(string value)
        {
            switch (value)
            {
                case "Prepare":
                    return XccSyntheticInteropStage.PrepareInternalContract;
                case "VerifyXccCreated":
                    return XccSyntheticInteropStage.ValidateStagedCreatedCandidate;
                case "VerifyXccExtractions":
                    return XccSyntheticInteropStage.ValidateStagedExtractionCandidates;
                default:
                    throw new ArgumentException(
                        "The XCC synthetic interop mode is not supported.",
                        nameof(value));
            }
        }

        private static void ValidateCaseId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 64 ||
                value[0] < 'a' || value[0] > 'z')
            {
                throw new ArgumentException(
                    "The XCC synthetic interop case id is not canonical.",
                    nameof(value));
            }

            for (int index = 1; index < value.Length; index++)
            {
                char character = value[index];
                if ((character < 'a' || character > 'z') &&
                    (character < '0' || character > '9') &&
                    character != '-' && character != '_')
                {
                    throw new ArgumentException(
                        "The XCC synthetic interop case id is not canonical.",
                        nameof(value));
                }
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

        private static void ValidateResultOutput(
            ExternalContentConfiguration configuration,
            string resultOutput)
        {
            string cacheRoot = Path.GetFullPath(configuration.CachePath);
            string resultRoot = Path.GetFullPath(Path.Combine(
                cacheRoot,
                CommandResultRootRelative.Replace(
                    '/',
                    Path.DirectorySeparatorChar)));
            string output = Path.GetFullPath(resultOutput);
            string outputDirectory = Path.GetDirectoryName(output);
            string parentDirectory = Path.GetDirectoryName(outputDirectory);
            if (!string.Equals(
                    Path.GetFileName(output),
                    "result.json",
                    StringComparison.Ordinal) ||
                !string.Equals(
                    parentDirectory,
                    resultRoot,
                    PathComparison) ||
                !IsValidRunDirectoryName(Path.GetFileName(outputDirectory)) ||
                !RepositoryPathPolicy.IsInsideOrEqual(output, resultRoot) ||
                !RepositoryPathPolicy.IsInsideOrEqual(resultRoot, cacheRoot))
            {
                throw new InvalidOperationException(
                    "The interop command result must use its fixed external-cache location.");
            }

            RejectExistingReparsePoint(cacheRoot);
            RejectExistingReparsePoint(resultRoot);
            RejectExistingReparsePoint(outputDirectory);
            if (File.Exists(output) || Directory.Exists(output))
            {
                throw new IOException("The interop command result already exists.");
            }
        }

        private static bool IsValidRunDirectoryName(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 96 ||
                !value.StartsWith("run-", StringComparison.Ordinal))
            {
                return false;
            }

            for (int index = 4; index < value.Length; index++)
            {
                char character = value[index];
                if ((character < 'a' || character > 'z') &&
                    (character < '0' || character > '9') &&
                    character != '-')
                {
                    return false;
                }
            }

            return true;
        }

        private static void ValidateResult(
            XccSyntheticInteropResult result,
            XccSyntheticInteropStage expectedStage,
            string expectedCaseId,
            ExternalContentConfiguration configuration,
            string configurationPath)
        {
            if (result == null || result.Stage != expectedStage ||
                !string.Equals(result.CaseId, expectedCaseId, StringComparison.Ordinal) ||
                result.IsRealXccExecutionEvidence ||
                result.IsSuccess != (result.Diagnostics.Count == 0) ||
                (!result.IsSuccess && result.Artifacts.Count != 0))
            {
                throw new InvalidOperationException(
                    "The XCC synthetic interop service returned an inconsistent result.");
            }

            foreach (XccSyntheticInteropArtifact artifact in result.Artifacts)
            {
                if (artifact == null || string.IsNullOrWhiteSpace(artifact.Role) ||
                    artifact.Length < 0 || !IsSha256(artifact.Sha256))
                {
                    throw new InvalidOperationException(
                        "The XCC synthetic interop result contains an invalid artifact.");
                }

                ValidateCanonicalLogicalPath(artifact.CacheRelativePath);
            }

            foreach (XccSyntheticInteropDiagnostic diagnostic in result.Diagnostics)
            {
                if (diagnostic == null ||
                    !Enum.IsDefined(typeof(XccSyntheticInteropDiagnosticCode), diagnostic.Code) ||
                    string.IsNullOrWhiteSpace(diagnostic.Message))
                {
                    throw new InvalidOperationException(
                        "The XCC synthetic interop result contains an invalid diagnostic.");
                }

                if (diagnostic.CacheRelativePath != null)
                {
                    ValidateCanonicalLogicalPath(diagnostic.CacheRelativePath);
                }

                RejectSensitiveValue(diagnostic.Message, configuration.RepositoryRoot);
                RejectSensitiveValue(diagnostic.Message, configurationPath);
                RejectSensitiveValue(diagnostic.Message, configuration.CachePath);
                foreach (ExternalContentSourceDescriptor source in configuration.Sources)
                {
                    RejectSensitiveValue(diagnostic.Message, source.RootPath);
                }
            }
        }

        private static void ValidateCanonicalLogicalPath(string value)
        {
            LogicalContentPath path = LogicalContentPath.Parse(value);
            if (!string.Equals(path.Value, value, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "An interop result path is not in canonical logical form.");
            }
        }

        private static bool IsSha256(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 64)
            {
                return false;
            }

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if ((character < '0' || character > '9') &&
                    (character < 'a' || character > 'f') &&
                    (character < 'A' || character > 'F'))
                {
                    return false;
                }
            }

            return true;
        }

        private static string SerializeResult(
            XccSyntheticInteropResult result,
            string mode)
        {
            var builder = new StringBuilder();
            builder.Append("{\"schemaVersion\":1,\"synthetic\":true,\"mode\":");
            AppendJson(builder, mode);
            builder.Append(",\"stage\":");
            AppendJson(builder, result.Stage.ToString());
            builder.Append(",\"caseId\":");
            AppendJson(builder, result.CaseId);
            builder.Append(",\"realXccExecutionEvidence\":");
            builder.Append(result.IsRealXccExecutionEvidence ? "true" : "false");
            builder.Append(",\"success\":");
            builder.Append(result.IsSuccess ? "true" : "false");
            builder.Append(",\"artifactCount\":");
            builder.Append(result.Artifacts.Count.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"diagnosticCount\":");
            builder.Append(result.Diagnostics.Count.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"artifacts\":[");

            XccSyntheticInteropArtifact[] artifacts = result.Artifacts
                .OrderBy(item => item.CacheRelativePath, StringComparer.Ordinal)
                .ThenBy(item => item.Role, StringComparer.Ordinal)
                .ToArray();
            for (int index = 0; index < artifacts.Length; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                XccSyntheticInteropArtifact artifact = artifacts[index];
                builder.Append("{\"role\":");
                AppendJson(builder, artifact.Role);
                builder.Append(",\"cacheRelativePath\":");
                AppendJson(builder, artifact.CacheRelativePath);
                builder.Append(",\"length\":");
                builder.Append(artifact.Length.ToString(CultureInfo.InvariantCulture));
                builder.Append(",\"sha256\":");
                AppendJson(builder, artifact.Sha256);
                builder.Append('}');
            }

            builder.Append("],\"diagnostics\":[");
            XccSyntheticInteropDiagnostic[] diagnostics = result.Diagnostics
                .OrderBy(item => item.Code)
                .ThenBy(item => item.CacheRelativePath, StringComparer.Ordinal)
                .ThenBy(item => item.Message, StringComparer.Ordinal)
                .ToArray();
            for (int index = 0; index < diagnostics.Length; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                XccSyntheticInteropDiagnostic diagnostic = diagnostics[index];
                builder.Append("{\"code\":");
                AppendJson(builder, diagnostic.Code.ToString());
                builder.Append(",\"message\":");
                AppendJson(builder, diagnostic.Message);
                builder.Append(",\"cacheRelativePath\":");
                if (diagnostic.CacheRelativePath == null)
                {
                    builder.Append("null");
                }
                else
                {
                    AppendJson(builder, diagnostic.CacheRelativePath);
                }

                builder.Append('}');
            }

            builder.Append("]}");
            byte[] utf8 = new UTF8Encoding(false, true).GetBytes(builder.ToString());
            if (utf8.Length > MaximumResultUtf8Bytes)
            {
                throw new InvalidOperationException(
                    "The sanitized interop command result exceeds its delivery budget.");
            }

            return builder.ToString();
        }

        private static void AppendJson(StringBuilder builder, string value)
        {
            builder.Append('"');
            foreach (char character in value)
            {
                switch (character)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    default:
                        if (character < 0x20)
                        {
                            builder.Append("\\u");
                            builder.Append(((int)character).ToString(
                                "x4",
                                CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(character);
                        }

                        break;
                }
            }

            builder.Append('"');
        }

        private static void RejectSensitivePaths(
            string json,
            ExternalContentConfiguration configuration,
            string configurationPath)
        {
            RejectSensitiveValue(json, configuration.RepositoryRoot);
            RejectSensitiveValue(json, configurationPath);
            RejectSensitiveValue(json, configuration.CachePath);
            foreach (ExternalContentSourceDescriptor source in configuration.Sources)
            {
                RejectSensitiveValue(json, source.RootPath);
            }
        }

        private static void RejectSensitiveValue(string value, string protectedPath)
        {
            if (string.IsNullOrEmpty(protectedPath))
            {
                return;
            }

            string fullPath = Path.GetFullPath(protectedPath);
            string slashPath = fullPath.Replace(Path.DirectorySeparatorChar, '/');
            string escapedPath = fullPath.Replace("\\", "\\\\");
            if (value.IndexOf(fullPath, StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf(slashPath, StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf(escapedPath, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new InvalidOperationException(
                    "A sanitized interop result contains a protected host path.");
            }
        }

        private static void RejectExistingReparsePoint(string path)
        {
            string reparsePointPath;
            if (RepositoryPathPolicy.ContainsExistingReparsePoint(
                    path,
                    out reparsePointPath))
            {
                throw new IOException(
                    "An XCC synthetic interop command path traverses a reparse point.");
            }
        }

        private static void WriteNewUtf8FileAtomically(string path, string content)
        {
            string directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);
            RejectExistingReparsePoint(directory);
            string temporaryPath = Path.Combine(
                directory,
                ".result." + Guid.NewGuid().ToString("N") + ".tmp");
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

        private static StringComparison PathComparison =>
            Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
    }
}
