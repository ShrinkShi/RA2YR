using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RA2YR.Core.Content
{
    public enum ContentSourceKind
    {
        Clean,
        Unpacked,
        Patched,
        Overlay,
        Other
    }

    public sealed class ExternalContentSourceDescriptor
    {
        public ExternalContentSourceDescriptor(
            string id,
            ContentSourceKind kind,
            string rootPath,
            int priority,
            string version,
            bool enabled)
        {
            if (!ContentConfigurationValueRules.IsValidSourceId(id))
            {
                throw new ArgumentException("The source id is not valid.", nameof(id));
            }

            if (!Enum.IsDefined(typeof(ContentSourceKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (string.IsNullOrWhiteSpace(rootPath) || !Path.IsPathRooted(rootPath))
            {
                throw new ArgumentException(
                    "An absolute source root path is required.",
                    nameof(rootPath));
            }

            if (!ContentConfigurationValueRules.IsValidVersion(version))
            {
                throw new ArgumentException("The source version is not valid.", nameof(version));
            }

            Id = id;
            Kind = kind;
            RootPath = RepositoryPathPolicy.NormalizeAbsolutePath(rootPath);
            Priority = priority;
            Version = version;
            Enabled = enabled;
        }

        public string Id { get; }

        public ContentSourceKind Kind { get; }

        public string RootPath { get; }

        public int Priority { get; }

        public string Version { get; }

        public bool Enabled { get; }
    }

    public sealed class ExternalContentConfiguration
    {
        public ExternalContentConfiguration(
            int schemaVersion,
            string configurationPath,
            string repositoryRoot,
            string cachePath,
            IEnumerable<ExternalContentSourceDescriptor> sources)
        {
            if (schemaVersion != ExternalContentConfigurationLoader.SupportedSchemaVersion)
            {
                throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            }

            ValidateAbsolutePath(configurationPath, nameof(configurationPath));
            ValidateAbsolutePath(repositoryRoot, nameof(repositoryRoot));
            ValidateAbsolutePath(cachePath, nameof(cachePath));
            ExternalContentSourceDescriptor[] sourceArray =
                (sources ?? throw new ArgumentNullException(nameof(sources))).ToArray();
            if (sourceArray.Any(source => source == null))
            {
                throw new ArgumentException("Sources may not contain null.", nameof(sources));
            }

            if (sourceArray
                .GroupBy(source => source.Id, StringComparer.OrdinalIgnoreCase)
                .Any(group => group.Count() != 1))
            {
                throw new ArgumentException("Source ids must be unique.", nameof(sources));
            }

            if (sourceArray.Length == 0)
            {
                throw new ArgumentException("At least one source is required.", nameof(sources));
            }

            if (!sourceArray.Any(source => source.Enabled))
            {
                throw new ArgumentException("At least one source must be enabled.", nameof(sources));
            }

            string normalizedConfigurationPath =
                RepositoryPathPolicy.NormalizeAbsolutePath(configurationPath);
            string normalizedRepositoryRoot =
                RepositoryPathPolicy.NormalizeAbsolutePath(repositoryRoot);
            string normalizedCachePath =
                RepositoryPathPolicy.NormalizeAbsolutePath(cachePath);
            if (!Directory.Exists(normalizedRepositoryRoot))
            {
                throw new ArgumentException(
                    "The repository root must exist and be a directory.",
                    nameof(repositoryRoot));
            }

            if (RepositoryPathPolicy.OverlapsRepository(
                normalizedCachePath,
                normalizedRepositoryRoot))
            {
                throw new ArgumentException(
                    "The cache path must not overlap the repository.",
                    nameof(cachePath));
            }

            for (int index = 0; index < sourceArray.Length; index++)
            {
                ExternalContentSourceDescriptor source = sourceArray[index];
                if (RepositoryPathPolicy.OverlapsRepository(
                        source.RootPath,
                        normalizedRepositoryRoot))
                {
                    throw new ArgumentException(
                        "A source path must not overlap the repository.",
                        nameof(sources));
                }

                if (RepositoryPathPolicy.OverlapsRepository(
                        source.RootPath,
                        normalizedCachePath))
                {
                    throw new ArgumentException(
                        "A source path must not overlap the cache path.",
                        nameof(sources));
                }

                for (int otherIndex = index + 1; otherIndex < sourceArray.Length; otherIndex++)
                {
                    if (RepositoryPathPolicy.OverlapsRepository(
                            source.RootPath,
                            sourceArray[otherIndex].RootPath))
                    {
                        throw new ArgumentException(
                            "Source paths must not contain one another.",
                            nameof(sources));
                    }
                }
            }

            SchemaVersion = schemaVersion;
            ConfigurationPath = normalizedConfigurationPath;
            RepositoryRoot = normalizedRepositoryRoot;
            CachePath = normalizedCachePath;
            Sources = Array.AsReadOnly(sourceArray);
        }

        public int SchemaVersion { get; }

        public string ConfigurationPath { get; }

        public string RepositoryRoot { get; }

        public string CachePath { get; }

        public IReadOnlyList<ExternalContentSourceDescriptor> Sources { get; }

        private static void ValidateAbsolutePath(string path, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path))
            {
                throw new ArgumentException("An absolute path is required.", parameterName);
            }
        }
    }

    public sealed class ExternalContentConfigurationLoadResult
    {
        public ExternalContentConfigurationLoadResult(
            ExternalContentConfiguration configuration,
            IEnumerable<ContentDiagnostic> diagnostics)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Diagnostics = Array.AsReadOnly(
                (diagnostics ?? Enumerable.Empty<ContentDiagnostic>()).ToArray());
        }

        public ExternalContentConfiguration Configuration { get; }

        public IReadOnlyList<ContentDiagnostic> Diagnostics { get; }
    }

    internal static class ContentConfigurationValueRules
    {
        public static bool IsValidSourceId(string value)
        {
            if (string.IsNullOrEmpty(value) ||
                value.Length > 64 ||
                !IsAsciiLetterOrDigit(value[0]))
            {
                return false;
            }

            for (int index = 1; index < value.Length; index++)
            {
                char character = value[index];
                if (!IsAsciiLetterOrDigit(character) &&
                    character != '.' &&
                    character != '_' &&
                    character != '-')
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsValidVersion(string value)
        {
            return value != null &&
                   value.Length <= 256 &&
                   !value.Any(char.IsControl) &&
                   !Path.IsPathRooted(value);
        }

        private static bool IsAsciiLetterOrDigit(char value)
        {
            return (value >= 'A' && value <= 'Z') ||
                   (value >= 'a' && value <= 'z') ||
                   (value >= '0' && value <= '9');
        }
    }
}
