using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RA2YR.Core.Content
{
    public static class ContentResolutionManifestSerializer
    {
        public const int SchemaVersion = 1;

        public static string SerializeCanonicalJson(ContentResolutionResult resolution)
        {
            EnsureSerializable(resolution);

            var builder = new StringBuilder();
            builder.Append("{\"schemaVersion\":");
            builder.Append(SchemaVersion.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"manifestType\":\"resolved-directory-content\"");
            builder.Append(",\"pathSemantics\":\"ordinal-ignore-case\"");
            builder.Append(",\"sources\":[");

            ContentResolutionSource[] sources = resolution.Sources
                .OrderByDescending(source => source.Priority)
                .ThenBy(source => source.Id, StringComparer.Ordinal)
                .ToArray();
            for (int sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
            {
                if (sourceIndex != 0)
                {
                    builder.Append(',');
                }

                ContentResolutionSource source = sources[sourceIndex];
                builder.Append("{\"id\":");
                AppendJsonString(builder, source.Id);
                builder.Append(",\"kind\":");
                AppendJsonString(builder, source.Kind.ToString());
                builder.Append(",\"version\":");
                AppendJsonString(builder, source.Version);
                builder.Append(",\"priority\":");
                builder.Append(source.Priority.ToString(CultureInfo.InvariantCulture));
                builder.Append(",\"fingerprint\":");
                AppendJsonString(builder, source.Fingerprint);
                builder.Append(",\"fileCount\":");
                builder.Append(source.FileCount.ToString(CultureInfo.InvariantCulture));
                builder.Append(",\"totalBytes\":");
                builder.Append(source.TotalBytes.ToString(CultureInfo.InvariantCulture));
                builder.Append('}');
            }

            builder.Append("],\"files\":[");
            ContentPathResolution[] entries = resolution.Entries
                .OrderBy(entry => entry.LogicalPath, LogicalContentPathReportComparer.Instance)
                .ToArray();
            for (int entryIndex = 0; entryIndex < entries.Length; entryIndex++)
            {
                if (entryIndex != 0)
                {
                    builder.Append(',');
                }

                ContentPathResolution entry = entries[entryIndex];
                builder.Append("{\"logicalPath\":");
                AppendJsonString(builder, entry.LogicalPath.Value);
                builder.Append(",\"selected\":");
                AppendCandidate(builder, entry.Selected, "selected");
                builder.Append(",\"provenance\":[");
                for (int candidateIndex = 0;
                     candidateIndex < entry.ProvenanceChain.Count;
                     candidateIndex++)
                {
                    if (candidateIndex != 0)
                    {
                        builder.Append(',');
                    }

                    ContentProvenanceCandidate candidate =
                        entry.ProvenanceChain[candidateIndex];
                    AppendCandidate(
                        builder,
                        candidate,
                        ReferenceEquals(candidate, entry.Selected)
                            ? "selected"
                            : "overridden");
                }

                builder.Append("]}");
            }

            builder.Append("],\"diagnostics\":[]}");
            return builder.ToString();
        }

        public static byte[] SerializeCanonicalUtf8(ContentResolutionResult resolution)
        {
            return new UTF8Encoding(false, true).GetBytes(
                SerializeCanonicalJson(resolution));
        }

        public static string ComputeCanonicalSha256(ContentResolutionResult resolution)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return Sha256Utilities.ToLowerHex(
                    sha256.ComputeHash(SerializeCanonicalUtf8(resolution)));
            }
        }

        private static void EnsureSerializable(ContentResolutionResult resolution)
        {
            if (resolution == null)
            {
                throw new ArgumentNullException(nameof(resolution));
            }

            if (!resolution.IsComplete || resolution.HasErrors ||
                resolution.Entries.Any(entry => !entry.IsResolved))
            {
                throw new InvalidOperationException(
                    "A resolution manifest cannot be produced from an incomplete, " +
                    "ambiguous, or erroneous result.");
            }
        }

        private static void AppendCandidate(
            StringBuilder builder,
            ContentProvenanceCandidate candidate,
            string disposition)
        {
            if (candidate == null)
            {
                throw new InvalidOperationException(
                    "A complete resolution entry must have a selected candidate.");
            }

            builder.Append("{\"sourceId\":");
            AppendJsonString(builder, candidate.Source.Id);
            builder.Append(",\"sourceRelativePath\":");
            AppendJsonString(builder, candidate.SourceRelativePath);
            builder.Append(",\"sourceVersion\":");
            AppendJsonString(builder, candidate.Source.Version);
            builder.Append(",\"priority\":");
            builder.Append(candidate.Source.Priority.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"length\":");
            builder.Append(candidate.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"sha256\":");
            AppendJsonString(builder, candidate.Sha256);
            builder.Append(",\"disposition\":");
            AppendJsonString(builder, disposition);
            builder.Append('}');
        }

        internal static void AppendJsonString(StringBuilder builder, string value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

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
                    case '\b':
                        builder.Append("\\b");
                        break;
                    case '\f':
                        builder.Append("\\f");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (character < 0x20 || character > 0x7e)
                        {
                            builder.Append("\\u");
                            builder.Append(((int)character).ToString(
                                "x4", CultureInfo.InvariantCulture));
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
    }
}
