using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Configuration.Ini.Resolution
{
    internal enum IniOpaqueAuditPosition
    {
        BeforeFirstStructuredSection,
        InsideStructuredSection,
        AfterStructuredSectionWithoutActiveOwner
    }

    internal enum IniOpaqueLeadingClass
    {
        Empty,
        KnownStructuralPunctuation,
        OtherAsciiPunctuation,
        AsciiAlphanumeric,
        NonAsciiOrInvalid
    }

    internal enum IniInlineSemicolonPosition
    {
        FirstNonWhitespaceValueUnit,
        Middle,
        LastNonWhitespaceValueUnit
    }

    internal sealed class IniRuntimeSyntaxAudit
    {
        internal IniRuntimeSyntaxAudit(
            int opaqueLineCount,
            int opaqueBeforeSectionCount,
            int opaqueInsideSectionCount,
            int opaqueAfterSectionCount,
            int opaqueContainsEqualsCount,
            int opaqueKnownPunctuationCount,
            int opaquePotentialRuntimeImpactCount,
            int inlineSemicolonLineCount,
            int semicolonAtValueStartCount,
            int semicolonInValueMiddleCount,
            int semicolonAtValueEndCount,
            IReadOnlyDictionary<IniOpaqueReason, int> opaqueReasonCounts,
            IReadOnlyDictionary<string, int> opaquePatternCounts)
        {
            int[] counts =
            {
                opaqueLineCount,
                opaqueBeforeSectionCount,
                opaqueInsideSectionCount,
                opaqueAfterSectionCount,
                opaqueContainsEqualsCount,
                opaqueKnownPunctuationCount,
                opaquePotentialRuntimeImpactCount,
                inlineSemicolonLineCount,
                semicolonAtValueStartCount,
                semicolonInValueMiddleCount,
                semicolonAtValueEndCount
            };
            if (counts.Any(value => value < 0) ||
                checked(opaqueBeforeSectionCount + opaqueInsideSectionCount +
                        opaqueAfterSectionCount) != opaqueLineCount ||
                checked(semicolonAtValueStartCount + semicolonInValueMiddleCount +
                        semicolonAtValueEndCount) != inlineSemicolonLineCount)
            {
                throw new ArgumentException("Runtime syntax audit counts are inconsistent.");
            }

            OpaqueLineCount = opaqueLineCount;
            OpaqueBeforeSectionCount = opaqueBeforeSectionCount;
            OpaqueInsideSectionCount = opaqueInsideSectionCount;
            OpaqueAfterSectionCount = opaqueAfterSectionCount;
            OpaqueContainsEqualsCount = opaqueContainsEqualsCount;
            OpaqueKnownPunctuationCount = opaqueKnownPunctuationCount;
            OpaquePotentialRuntimeImpactCount = opaquePotentialRuntimeImpactCount;
            InlineSemicolonLineCount = inlineSemicolonLineCount;
            SemicolonAtValueStartCount = semicolonAtValueStartCount;
            SemicolonInValueMiddleCount = semicolonInValueMiddleCount;
            SemicolonAtValueEndCount = semicolonAtValueEndCount;
            OpaqueReasonCounts = Copy(opaqueReasonCounts);
            OpaquePatternCounts = Copy(opaquePatternCounts);
        }

        public int OpaqueLineCount { get; }
        public int OpaqueBeforeSectionCount { get; }
        public int OpaqueInsideSectionCount { get; }
        public int OpaqueAfterSectionCount { get; }
        public int OpaqueContainsEqualsCount { get; }
        public int OpaqueKnownPunctuationCount { get; }
        public int OpaquePotentialRuntimeImpactCount { get; }
        public int InlineSemicolonLineCount { get; }
        public int SemicolonAtValueStartCount { get; }
        public int SemicolonInValueMiddleCount { get; }
        public int SemicolonAtValueEndCount { get; }
        public IReadOnlyDictionary<IniOpaqueReason, int> OpaqueReasonCounts { get; }
        public IReadOnlyDictionary<string, int> OpaquePatternCounts { get; }

        public bool MayAffectMinimalTypedView => OpaquePotentialRuntimeImpactCount > 0;

        private static IReadOnlyDictionary<TKey, int> Copy<TKey>(
            IReadOnlyDictionary<TKey, int> source)
        {
            if (source == null || source.Any(value => value.Value < 0))
            {
                throw new ArgumentException("Audit dictionaries must contain valid counts.");
            }

            return new System.Collections.ObjectModel.ReadOnlyDictionary<TKey, int>(
                source.ToDictionary(value => value.Key, value => value.Value));
        }
    }

    internal static class IniRuntimeSyntaxAuditor
    {
        private static readonly HashSet<int> KnownStructuralPunctuation =
            new HashSet<int> { '[', ']', '=', ';', '#', '$', '@', '!' };

        public static IniRuntimeSyntaxAudit Analyze(IniRawDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            int width = document.PhysicalEncoding ==
                            IniPhysicalEncodingKind.Utf16LittleEndianWithBom ||
                        document.PhysicalEncoding ==
                            IniPhysicalEncodingKind.Utf16BigEndianWithBom
                ? 2
                : 1;
            bool seenStructuredSection = false;
            bool activeStructuredSection = false;
            int opaqueBefore = 0;
            int opaqueInside = 0;
            int opaqueAfter = 0;
            int opaqueEquals = 0;
            int opaqueKnownPunctuation = 0;
            int opaquePotentialImpact = 0;
            int semicolonStart = 0;
            int semicolonMiddle = 0;
            int semicolonEnd = 0;
            var reasons = new Dictionary<IniOpaqueReason, int>();
            var patterns = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (IniNode node in document.Nodes)
            {
                if (node is IniSectionNode)
                {
                    seenStructuredSection = true;
                    activeStructuredSection = true;
                    continue;
                }

                var opaque = node as IniOpaqueNode;
                if (opaque != null)
                {
                    IniOpaqueAuditPosition position = !seenStructuredSection
                        ? IniOpaqueAuditPosition.BeforeFirstStructuredSection
                        : activeStructuredSection
                            ? IniOpaqueAuditPosition.InsideStructuredSection
                            : IniOpaqueAuditPosition.AfterStructuredSectionWithoutActiveOwner;
                    if (position == IniOpaqueAuditPosition.BeforeFirstStructuredSection)
                    {
                        opaqueBefore = checked(opaqueBefore + 1);
                    }
                    else if (position == IniOpaqueAuditPosition.InsideStructuredSection)
                    {
                        opaqueInside = checked(opaqueInside + 1);
                    }
                    else
                    {
                        opaqueAfter = checked(opaqueAfter + 1);
                    }

                    byte[] bytes = opaque.Line.Content.ToArray();
                    bool hasEquals = FindAsciiUnit(bytes, width, '=') >= 0;
                    IniOpaqueLeadingClass leading = ClassifyLeading(bytes, width);
                    if (hasEquals)
                    {
                        opaqueEquals = checked(opaqueEquals + 1);
                    }

                    if (leading == IniOpaqueLeadingClass.KnownStructuralPunctuation)
                    {
                        opaqueKnownPunctuation = checked(opaqueKnownPunctuation + 1);
                    }

                    bool potentialImpact = position ==
                                               IniOpaqueAuditPosition.InsideStructuredSection ||
                                           hasEquals ||
                                           leading == IniOpaqueLeadingClass.KnownStructuralPunctuation;
                    if (potentialImpact)
                    {
                        opaquePotentialImpact = checked(opaquePotentialImpact + 1);
                    }

                    Increment(reasons, opaque.Reason);
                    string pattern = opaque.Reason + "|" + position + "|equals=" +
                                     (hasEquals ? "yes" : "no") + "|leading=" + leading;
                    Increment(patterns, pattern);

                    if (FirstNonWhitespaceAsciiUnit(bytes, width) == '[')
                    {
                        activeStructuredSection = false;
                    }

                    continue;
                }

                var key = node as IniKeyValueNode;
                if (key == null)
                {
                    continue;
                }

                byte[] beforeValue = key.WhitespaceAfterEquals.ToArray();
                byte[] value = key.Value.ToArray();
                var combined = new byte[checked(beforeValue.Length + value.Length)];
                Buffer.BlockCopy(beforeValue, 0, combined, 0, beforeValue.Length);
                Buffer.BlockCopy(value, 0, combined, beforeValue.Length, value.Length);
                int semicolon = FindAsciiUnit(combined, width, ';');
                if (semicolon < 0)
                {
                    continue;
                }

                int first = FirstNonWhitespaceOffset(combined, width);
                int last = LastNonWhitespaceOffset(combined, width);
                if (semicolon == first)
                {
                    semicolonStart = checked(semicolonStart + 1);
                }
                else if (semicolon == last)
                {
                    semicolonEnd = checked(semicolonEnd + 1);
                }
                else
                {
                    semicolonMiddle = checked(semicolonMiddle + 1);
                }
            }

            return new IniRuntimeSyntaxAudit(
                checked(opaqueBefore + opaqueInside + opaqueAfter),
                opaqueBefore,
                opaqueInside,
                opaqueAfter,
                opaqueEquals,
                opaqueKnownPunctuation,
                opaquePotentialImpact,
                checked(semicolonStart + semicolonMiddle + semicolonEnd),
                semicolonStart,
                semicolonMiddle,
                semicolonEnd,
                reasons,
                patterns);
        }

        private static IniOpaqueLeadingClass ClassifyLeading(byte[] bytes, int width)
        {
            int value = FirstNonWhitespaceAsciiUnit(bytes, width);
            if (value < 0)
            {
                return bytes.Length == 0
                    ? IniOpaqueLeadingClass.Empty
                    : IniOpaqueLeadingClass.NonAsciiOrInvalid;
            }

            if (KnownStructuralPunctuation.Contains(value))
            {
                return IniOpaqueLeadingClass.KnownStructuralPunctuation;
            }

            if (value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' ||
                value >= '0' && value <= '9')
            {
                return IniOpaqueLeadingClass.AsciiAlphanumeric;
            }

            return value >= 0x21 && value <= 0x7e
                ? IniOpaqueLeadingClass.OtherAsciiPunctuation
                : IniOpaqueLeadingClass.NonAsciiOrInvalid;
        }

        private static int FirstNonWhitespaceAsciiUnit(byte[] bytes, int width)
        {
            int offset = FirstNonWhitespaceOffset(bytes, width);
            return offset < 0 ? -1 : ReadAsciiUnit(bytes, offset, width);
        }

        private static int FirstNonWhitespaceOffset(byte[] bytes, int width)
        {
            for (int offset = 0; offset <= bytes.Length - width; offset += width)
            {
                int value = ReadAsciiUnit(bytes, offset, width);
                if (value != ' ' && value != '\t')
                {
                    return offset;
                }
            }

            return -1;
        }

        private static int LastNonWhitespaceOffset(byte[] bytes, int width)
        {
            for (int offset = bytes.Length - width; offset >= 0; offset -= width)
            {
                int value = ReadAsciiUnit(bytes, offset, width);
                if (value != ' ' && value != '\t')
                {
                    return offset;
                }
            }

            return -1;
        }

        private static int FindAsciiUnit(byte[] bytes, int width, int expected)
        {
            for (int offset = 0; offset <= bytes.Length - width; offset += width)
            {
                if (ReadAsciiUnit(bytes, offset, width) == expected)
                {
                    return offset;
                }
            }

            return -1;
        }

        private static int ReadAsciiUnit(byte[] bytes, int offset, int width)
        {
            if (width == 1)
            {
                return bytes[offset] <= 0x7f ? bytes[offset] : -1;
            }

            if (bytes[offset + 1] == 0 && bytes[offset] <= 0x7f)
            {
                return bytes[offset];
            }

            if (bytes[offset] == 0 && bytes[offset + 1] <= 0x7f)
            {
                return bytes[offset + 1];
            }

            return -1;
        }

        private static void Increment<TKey>(IDictionary<TKey, int> counts, TKey key)
        {
            int value;
            counts.TryGetValue(key, out value);
            counts[key] = checked(value + 1);
        }
    }
}
