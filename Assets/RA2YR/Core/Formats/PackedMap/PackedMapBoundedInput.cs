using System;
using System.IO;
using RA2YR.Core.Binary;
using RA2YR.Core.Binary.Seekable;

namespace RA2YR.Core.Formats.PackedMap
{
    internal static class PackedMapBoundedInput
    {
        internal static byte[] ReadWindow(ReadOnlyDataWindow window, string field, long maxInputBytes = long.MaxValue)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            if (maxInputBytes < 0) throw new ArgumentOutOfRangeException(nameof(maxInputBytes));
            if (window.Length > maxInputBytes) throw new ArgumentOutOfRangeException(nameof(window), "The input window exceeds the configured byte budget.");
            if (window.Length > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(window));
            byte[] bytes = new byte[(int)window.Length];
            if (bytes.Length != 0) window.ReadExactly(0, bytes, 0, bytes.Length, field);
            return bytes;
        }

        internal static byte[] ReadStream(Stream stream, long length, BinarySourceContext source, long maxInputBytes = long.MaxValue)
        {
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            if (maxInputBytes < 0) throw new ArgumentOutOfRangeException(nameof(maxInputBytes));
            if (length > maxInputBytes) throw new ArgumentOutOfRangeException(nameof(length), "The input window exceeds the configured byte budget.");
            using (BinaryReadSession session = BinaryReadSession.FromStream(stream, length, source, leaveOpen: true))
            {
                byte[] bytes = session.Root.ReadBytes(length, "packed-input");
                session.Root.Complete(TrailingDataPolicy.RequireFullyConsumed, "packed-input");
                return bytes;
            }
        }
    }
}
