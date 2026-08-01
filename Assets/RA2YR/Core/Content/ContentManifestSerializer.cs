using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RA2YR.Core.Content
{
    public static class ContentManifestSerializer
    {
        public const int SchemaVersion = 1;

        public static string SerializeCanonicalJson(ContentIndexResult index)
        {
            if (index == null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            if (!index.IsComplete || index.HasErrors ||
                index.Sources.Any(source => !source.IsComplete))
            {
                throw new InvalidOperationException(
                    "A canonical manifest cannot be produced from an incomplete or erroneous index.");
            }

            var builder = new StringBuilder();
            builder.Append("{\"schemaVersion\":");
            builder.Append(SchemaVersion.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"sources\":[");

            IReadOnlyList<ContentSourceIndex> sources = index.Sources
                .OrderByDescending(source => source.Source.Priority)
                .ThenBy(source => source.Source.Id, StringComparer.Ordinal)
                .ToArray();
            for (int sourceIndex = 0; sourceIndex < sources.Count; sourceIndex++)
            {
                if (sourceIndex != 0)
                {
                    builder.Append(',');
                }

                ContentSourceIndex source = sources[sourceIndex];
                builder.Append("{\"id\":");
                AppendJsonString(builder, source.Source.Id);
                builder.Append(",\"kind\":");
                AppendJsonString(builder, source.Source.Kind.ToString());
                builder.Append(",\"priority\":");
                builder.Append(source.Source.Priority.ToString(CultureInfo.InvariantCulture));
                builder.Append(",\"version\":");
                AppendJsonString(builder, source.Source.Version);
                builder.Append(",\"fingerprint\":");
                AppendJsonString(builder, source.Fingerprint);
                builder.Append(",\"files\":[");

                IReadOnlyList<ContentFileRecord> files = source.Files
                    .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
                    .ToArray();
                for (int fileIndex = 0; fileIndex < files.Count; fileIndex++)
                {
                    if (fileIndex != 0)
                    {
                        builder.Append(',');
                    }

                    ContentFileRecord file = files[fileIndex];
                    builder.Append("{\"path\":");
                    AppendJsonString(builder, file.RelativePath);
                    builder.Append(",\"length\":");
                    builder.Append(file.Length.ToString(CultureInfo.InvariantCulture));
                    builder.Append(",\"sha256\":");
                    AppendJsonString(builder, file.Sha256);
                    builder.Append('}');
                }

                builder.Append("]}");
            }

            builder.Append("]}");
            return builder.ToString();
        }

        public static string ComputeCanonicalSha256(ContentIndexResult index)
        {
            byte[] bytes = new UTF8Encoding(false, true).GetBytes(
                SerializeCanonicalJson(index));
            using (SHA256 sha256 = SHA256.Create())
            {
                return Sha256Utilities.ToLowerHex(sha256.ComputeHash(bytes));
            }
        }

        private static void AppendJsonString(StringBuilder builder, string value)
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
                            builder.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
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
