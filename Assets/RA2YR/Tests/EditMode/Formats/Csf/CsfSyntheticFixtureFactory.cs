using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Formats.Csf;

namespace RA2YR.Tests.EditMode.Formats.Csf
{
    internal sealed class SyntheticCsfValue
    {
        public SyntheticCsfValue(string text, string extraText = null)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            ExtraText = extraText;
        }

        public string Text { get; }

        public string ExtraText { get; }
    }

    internal sealed class SyntheticCsfLabel
    {
        public SyntheticCsfLabel(string name, params SyntheticCsfValue[] values)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public string Name { get; }

        public IReadOnlyList<SyntheticCsfValue> Values { get; }
    }

    internal static class CsfSyntheticFixtureFactory
    {
        public static SyntheticCsfLabel Label(
            string name,
            params SyntheticCsfValue[] values)
        {
            return new SyntheticCsfLabel(name, values);
        }

        public static SyntheticCsfValue Normal(string text)
        {
            return new SyntheticCsfValue(text);
        }

        public static SyntheticCsfValue Extended(string text, string extraText)
        {
            return new SyntheticCsfValue(text, extraText);
        }

        public static byte[] Build(
            IEnumerable<SyntheticCsfLabel> labels,
            uint signature = WestwoodCsfReader.FileSignature,
            uint version = WestwoodCsfReader.SupportedVersion,
            uint reserved = 0,
            uint language = 0,
            uint? declaredLabelCount = null,
            uint? declaredValueCount = null)
        {
            SyntheticCsfLabel[] labelArray =
                (labels ?? throw new ArgumentNullException(nameof(labels))).ToArray();
            var bytes = new List<byte>();
            WriteUInt32(bytes, signature);
            WriteUInt32(bytes, version);
            WriteUInt32(bytes, declaredLabelCount ?? checked((uint)labelArray.Length));
            WriteUInt32(bytes, declaredValueCount ?? checked((uint)labelArray.Sum(
                label => label.Values.Count)));
            WriteUInt32(bytes, reserved);
            WriteUInt32(bytes, language);
            foreach (SyntheticCsfLabel label in labelArray)
            {
                WriteUInt32(bytes, WestwoodCsfReader.LabelMarker);
                WriteUInt32(bytes, checked((uint)label.Values.Count));
                WriteAscii(bytes, label.Name);
                foreach (SyntheticCsfValue value in label.Values)
                {
                    bool extended = value.ExtraText != null;
                    WriteUInt32(bytes, extended
                        ? WestwoodCsfReader.ExtendedValueMarker
                        : WestwoodCsfReader.NormalValueMarker);
                    WriteUInt32(bytes, checked((uint)value.Text.Length));
                    foreach (char codeUnit in value.Text)
                    {
                        WriteUInt16(bytes, unchecked((ushort)(codeUnit ^ 0xffff)));
                    }

                    if (extended)
                    {
                        WriteAscii(bytes, value.ExtraText);
                    }
                }
            }

            return bytes.ToArray();
        }

        public static void WriteUInt32(byte[] bytes, int offset, uint value)
        {
            bytes[offset] = checked((byte)(value & 0xff));
            bytes[offset + 1] = checked((byte)((value >> 8) & 0xff));
            bytes[offset + 2] = checked((byte)((value >> 16) & 0xff));
            bytes[offset + 3] = checked((byte)((value >> 24) & 0xff));
        }

        public static int FindUInt32(byte[] bytes, uint value, int startOffset = 0)
        {
            for (int index = startOffset; index <= bytes.Length - 4; index++)
            {
                if (bytes[index] == (byte)(value & 0xff) &&
                    bytes[index + 1] == (byte)((value >> 8) & 0xff) &&
                    bytes[index + 2] == (byte)((value >> 16) & 0xff) &&
                    bytes[index + 3] == (byte)((value >> 24) & 0xff))
                {
                    return index;
                }
            }

            return -1;
        }

        private static void WriteAscii(List<byte> bytes, string value)
        {
            WriteUInt32(bytes, checked((uint)value.Length));
            foreach (char character in value)
            {
                bytes.Add(checked((byte)character));
            }
        }

        private static void WriteUInt16(List<byte> bytes, ushort value)
        {
            bytes.Add(checked((byte)(value & 0xff)));
            bytes.Add(checked((byte)(value >> 8)));
        }

        private static void WriteUInt32(List<byte> bytes, uint value)
        {
            bytes.Add(checked((byte)(value & 0xff)));
            bytes.Add(checked((byte)((value >> 8) & 0xff)));
            bytes.Add(checked((byte)((value >> 16) & 0xff)));
            bytes.Add(checked((byte)((value >> 24) & 0xff)));
        }
    }
}
