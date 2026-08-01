using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RA2YR.Core.Content
{
    internal static class ContentSourceFingerprint
    {
        public static string Compute(
            ExternalContentSourceDescriptor source,
            IEnumerable<ContentFileRecord> files)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            using (var canonicalData = new MemoryStream())
            {
                using (var writer = new BinaryWriter(
                           canonicalData,
                           new UTF8Encoding(false, true),
                           true))
                {
                    writer.Write(1);
                    writer.Write(source.Id);
                    writer.Write(source.Kind.ToString());
                    writer.Write(source.Priority);
                    writer.Write(source.Version);

                    foreach (ContentFileRecord file in files.OrderBy(
                                 item => item.RelativePath,
                                 StringComparer.Ordinal))
                    {
                        writer.Write(file.RelativePath);
                        writer.Write(file.Length);
                        writer.Write(file.Sha256);
                    }
                }

                canonicalData.Position = 0;
                using (SHA256 sha256 = SHA256.Create())
                {
                    return Sha256Utilities.ToLowerHex(sha256.ComputeHash(canonicalData));
                }
            }
        }
    }
}
