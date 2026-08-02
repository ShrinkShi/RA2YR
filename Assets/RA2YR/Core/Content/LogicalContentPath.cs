using System;
using System.Collections.Generic;

namespace RA2YR.Core.Content
{
    /// <summary>
    /// A repository-independent content name using YR/Windows path semantics.
    /// </summary>
    public sealed class LogicalContentPath :
        IEquatable<LogicalContentPath>,
        IComparable<LogicalContentPath>
    {
        private const int MaximumPathCharacters = 32767;
        private const int MaximumSegmentCharacters = 255;

        private static readonly HashSet<string> ReservedDeviceNames =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CON", "PRN", "AUX", "NUL",
                "COM1", "COM2", "COM3", "COM4", "COM5",
                "COM6", "COM7", "COM8", "COM9",
                "LPT1", "LPT2", "LPT3", "LPT4", "LPT5",
                "LPT6", "LPT7", "LPT8", "LPT9"
            };

        private LogicalContentPath(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static LogicalContentPath Parse(string value)
        {
            LogicalContentPath path;
            string reason;
            if (!TryParse(value, out path, out reason))
            {
                throw new ArgumentException(reason, nameof(value));
            }

            return path;
        }

        public static bool TryParse(
            string value,
            out LogicalContentPath path,
            out string failureReason)
        {
            path = null;
            failureReason = null;
            if (string.IsNullOrEmpty(value))
            {
                failureReason = "A logical content path cannot be empty.";
                return false;
            }

            if (value.Length > MaximumPathCharacters)
            {
                failureReason = "The logical content path is too long.";
                return false;
            }

            if (value[0] == '/' || value[value.Length - 1] == '/' ||
                value.IndexOf('\\') >= 0 || value.IndexOf(':') >= 0)
            {
                failureReason =
                    "A logical content path must be relative and use '/' separators.";
                return false;
            }

            if (!HasSafelyRepresentableCharacters(value, out failureReason))
            {
                return false;
            }

            string[] segments = value.Split('/');
            foreach (string segment in segments)
            {
                if (segment.Length == 0 || segment == "." || segment == "..")
                {
                    failureReason =
                        "Empty, current-directory, and parent-directory segments are forbidden.";
                    return false;
                }

                if (segment.Length > MaximumSegmentCharacters)
                {
                    failureReason = "A logical content path segment is too long.";
                    return false;
                }

                if (segment[segment.Length - 1] == '.' ||
                    segment[segment.Length - 1] == ' ')
                {
                    failureReason =
                        "Segments ending in a dot or space are not stable Windows names.";
                    return false;
                }

                int extensionIndex = segment.IndexOf('.');
                string deviceStem = extensionIndex < 0
                    ? segment
                    : segment.Substring(0, extensionIndex);
                deviceStem = deviceStem.TrimEnd(' ', '.');
                if (ReservedDeviceNames.Contains(deviceStem))
                {
                    failureReason = "Reserved Windows device names are forbidden.";
                    return false;
                }
            }

            path = new LogicalContentPath(value);
            return true;
        }

        public bool Equals(LogicalContentPath other)
        {
            return other != null && StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as LogicalContentPath);
        }

        public int CompareTo(LogicalContentPath other)
        {
            if (ReferenceEquals(other, null))
            {
                return 1;
            }

            return StringComparer.OrdinalIgnoreCase.Compare(Value, other.Value);
        }

        public override int GetHashCode()
        {
            // Fold the complete string so supplementary-plane case pairs are
            // handled before applying a deterministic, non-randomized hash.
            string folded = Value.ToUpperInvariant();
            unchecked
            {
                uint hash = 2166136261;
                foreach (char character in folded)
                {
                    hash ^= character;
                    hash *= 16777619;
                }

                return (int)hash;
            }
        }

        public override string ToString()
        {
            return Value;
        }

        private static bool HasSafelyRepresentableCharacters(
            string value,
            out string failureReason)
        {
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (char.IsControl(character) ||
                    character == '<' || character == '>' || character == '"' ||
                    character == '|' || character == '?' || character == '*')
                {
                    failureReason =
                        "The logical content path contains a Windows-unsafe character.";
                    return false;
                }

                int scalarValue;
                if (char.IsHighSurrogate(character))
                {
                    if (index + 1 >= value.Length ||
                        !char.IsLowSurrogate(value[index + 1]))
                    {
                        failureReason = "The logical content path contains invalid UTF-16.";
                        return false;
                    }

                    scalarValue = char.ConvertToUtf32(character, value[index + 1]);
                    index++;
                }
                else if (char.IsLowSurrogate(character))
                {
                    failureReason =
                        "The logical content path contains an unsafe Unicode value.";
                    return false;
                }
                else
                {
                    scalarValue = character;
                }

                if ((scalarValue >= 0xfdd0 && scalarValue <= 0xfdef) ||
                    (scalarValue & 0xffff) == 0xfffe ||
                    (scalarValue & 0xffff) == 0xffff)
                {
                    failureReason =
                        "The logical content path contains a Unicode noncharacter.";
                    return false;
                }
            }

            failureReason = null;
            return true;
        }
    }

    internal sealed class LogicalContentPathReportComparer :
        IComparer<LogicalContentPath>
    {
        public static readonly LogicalContentPathReportComparer Instance =
            new LogicalContentPathReportComparer();

        public int Compare(LogicalContentPath left, LogicalContentPath right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (ReferenceEquals(left, null))
            {
                return -1;
            }

            if (ReferenceEquals(right, null))
            {
                return 1;
            }

            int logical = StringComparer.OrdinalIgnoreCase.Compare(left.Value, right.Value);
            return logical != 0
                ? logical
                : StringComparer.Ordinal.Compare(left.Value, right.Value);
        }
    }
}
