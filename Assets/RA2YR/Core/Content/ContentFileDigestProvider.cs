using System;
using System.IO;
using System.Security.Cryptography;

namespace RA2YR.Core.Content
{
    internal interface IContentFileDigestProvider
    {
        string ComputeSha256(string absolutePath);
    }

    internal sealed class Sha256ContentFileDigestProvider : IContentFileDigestProvider
    {
        public string ComputeSha256(string absolutePath)
        {
            if (absolutePath == null)
            {
                throw new ArgumentNullException(nameof(absolutePath));
            }

            using (var stream = new FileStream(
                       absolutePath,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.Read,
                       64 * 1024,
                       FileOptions.SequentialScan))
            using (SHA256 sha256 = SHA256.Create())
            {
                return Sha256Utilities.ToLowerHex(sha256.ComputeHash(stream));
            }
        }
    }

    internal static class Sha256Utilities
    {
        private const string LowerHexCharacters = "0123456789abcdef";

        public static string ToLowerHex(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            var characters = new char[bytes.Length * 2];
            for (int index = 0; index < bytes.Length; index++)
            {
                byte value = bytes[index];
                characters[index * 2] = LowerHexCharacters[value >> 4];
                characters[index * 2 + 1] = LowerHexCharacters[value & 0x0f];
            }

            return new string(characters);
        }

        public static bool IsLowerSha256(string value)
        {
            if (value == null || value.Length != 64)
            {
                return false;
            }

            foreach (char character in value)
            {
                if ((character < '0' || character > '9') &&
                    (character < 'a' || character > 'f'))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
