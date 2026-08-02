using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RA2YR.Core.Binary;

namespace RA2YR.Core.Formats.Pal
{
    internal sealed class WestwoodPalette
    {
        private const string CanonicalHashDomain = "RA2YR.PAL.RAW.V1\0";

        private readonly PaletteColorRaw[] colors;
        private readonly IReadOnlyList<PaletteColorRaw> colorView;

        internal WestwoodPalette(
            BinarySourceContext source,
            PaletteSourceProvenance provenance,
            IEnumerable<PaletteColorRaw> colors)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
            PaletteColorRaw[] colorArray =
                (colors ?? throw new ArgumentNullException(nameof(colors))).ToArray();
            if (colorArray.Length != ColorCount)
            {
                throw new ArgumentException(
                    "A Westwood palette requires exactly 256 colors.",
                    nameof(colors));
            }

            this.colors = colorArray;
            colorView = Array.AsReadOnly(this.colors);

            byte minimum = PaletteColorRaw.MaximumChannelValue;
            byte maximum = 0;
            var distinct = new HashSet<int>();
            foreach (PaletteColorRaw color in this.colors)
            {
                minimum = Math.Min(minimum, Math.Min(color.Red, Math.Min(color.Green, color.Blue)));
                maximum = Math.Max(maximum, Math.Max(color.Red, Math.Max(color.Green, color.Blue)));
                distinct.Add(color.GetHashCode());
            }

            MinimumRawChannel = minimum;
            MaximumRawChannel = maximum;
            DistinctColorCount = distinct.Count;
            CanonicalModelSha256 = ComputeCanonicalSha256(this.colors);
        }

        public const int ColorCount = 256;

        public const int ChannelsPerColor = 3;

        public const int FileLength = ColorCount * ChannelsPerColor;

        public BinarySourceContext Source { get; }

        public PaletteSourceProvenance Provenance { get; }

        public IReadOnlyList<PaletteColorRaw> Colors => colorView;

        public PaletteColorRaw this[int index] => colors[index];

        public byte MinimumRawChannel { get; }

        public byte MaximumRawChannel { get; }

        public int DistinctColorCount { get; }

        public string CanonicalModelSha256 { get; }

        private static string ComputeCanonicalSha256(PaletteColorRaw[] paletteColors)
        {
            byte[] domain = Encoding.ASCII.GetBytes(CanonicalHashDomain);
            byte[] canonical = new byte[checked(
                domain.Length + 4 + ColorCount * (2 + ChannelsPerColor))];
            Buffer.BlockCopy(domain, 0, canonical, 0, domain.Length);
            canonical[domain.Length] = 0x00;
            canonical[domain.Length + 1] = 0x01;
            canonical[domain.Length + 2] = 0x00;
            canonical[domain.Length + 3] = 0x00;
            int offset = domain.Length + 4;
            for (int index = 0; index < paletteColors.Length; index++)
            {
                PaletteColorRaw color = paletteColors[index];
                canonical[offset++] = checked((byte)(index & 0xff));
                canonical[offset++] = checked((byte)(index >> 8));
                canonical[offset++] = color.Red;
                canonical[offset++] = color.Green;
                canonical[offset++] = color.Blue;
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                return BitConverter.ToString(sha256.ComputeHash(canonical))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }
    }
}
