using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;

namespace RA2YR.Core.Formats.Csf
{
    internal readonly struct CsfLanguageCode : IEquatable<CsfLanguageCode>
    {
        public CsfLanguageCode(uint rawValue)
        {
            RawValue = rawValue;
        }

        public uint RawValue { get; }

        public bool Equals(CsfLanguageCode other)
        {
            return RawValue == other.RawValue;
        }

        public override bool Equals(object obj)
        {
            return obj is CsfLanguageCode other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked((int)RawValue);
        }

        public static bool operator ==(CsfLanguageCode left, CsfLanguageCode right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CsfLanguageCode left, CsfLanguageCode right)
        {
            return !left.Equals(right);
        }
    }

    internal sealed class CsfHeader
    {
        internal CsfHeader(
            uint signature,
            uint version,
            uint declaredLabelCount,
            uint declaredValueCount,
            uint reserved,
            CsfLanguageCode language)
        {
            Signature = signature;
            Version = version;
            DeclaredLabelCount = declaredLabelCount;
            DeclaredValueCount = declaredValueCount;
            Reserved = reserved;
            Language = language;
        }

        public uint Signature { get; }

        public uint Version { get; }

        public uint DeclaredLabelCount { get; }

        public uint DeclaredValueCount { get; }

        public uint Reserved { get; }

        public CsfLanguageCode Language { get; }
    }

    internal sealed class CsfText : IEquatable<CsfText>
    {
        internal CsfText(string codeUnits)
        {
            CodeUnits = codeUnits ?? throw new ArgumentNullException(nameof(codeUnits));
        }

        public string CodeUnits { get; }

        public int Length => CodeUnits.Length;

        public ushort this[int index] => CodeUnits[index];

        public bool Equals(CsfText other)
        {
            return other != null && string.Equals(
                CodeUnits,
                other.CodeUnits,
                StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CsfText);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(CodeUnits);
        }
    }

    internal enum CsfValueKind : byte
    {
        Normal = 0,
        Extended = 1
    }

    internal sealed class CsfValue
    {
        internal CsfValue(
            CsfValueKind kind,
            CsfText text,
            string extraText)
        {
            if (!Enum.IsDefined(typeof(CsfValueKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if ((kind == CsfValueKind.Normal) != (extraText == null))
            {
                throw new ArgumentException(
                    "Only an extended CSF value carries an additional ASCII string.",
                    nameof(extraText));
            }

            if (extraText != null && extraText.Any(character => character > 0x7f))
            {
                throw new ArgumentException(
                    "A CSF extended string must contain only confirmed ASCII bytes.",
                    nameof(extraText));
            }

            Kind = kind;
            Text = text ?? throw new ArgumentNullException(nameof(text));
            ExtraText = extraText;
        }

        public CsfValueKind Kind { get; }

        public CsfText Text { get; }

        public string ExtraText { get; }

        public bool HasExtraText => Kind == CsfValueKind.Extended;
    }

    internal sealed class CsfLabel
    {
        private readonly CsfValue[] values;
        private readonly IReadOnlyList<CsfValue> valueView;

        internal CsfLabel(string name, IEnumerable<CsfValue> values)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Any(character => character > 0x7f))
            {
                throw new ArgumentException(
                    "A CSF label must contain only confirmed ASCII bytes.",
                    nameof(name));
            }

            CsfValue[] valueArray =
                (values ?? throw new ArgumentNullException(nameof(values))).ToArray();
            if (valueArray.Any(value => value == null))
            {
                throw new ArgumentException(
                    "A CSF label cannot contain null values.",
                    nameof(values));
            }

            Name = name;
            this.values = valueArray;
            valueView = Array.AsReadOnly(this.values);
        }

        public string Name { get; }

        public IReadOnlyList<CsfValue> Values => valueView;

        public CsfValue this[int index] => values[index];
    }

    internal sealed class CsfSourceProvenance
    {
        private readonly IReadOnlyList<LogicalContentPath> logicalChain;

        public CsfSourceProvenance(
            string sourceId,
            IEnumerable<LogicalContentPath> logicalChain)
        {
            SourceId = BinaryDiagnosticLabel.Validate(sourceId, nameof(sourceId));
            LogicalContentPath[] chain =
                (logicalChain ?? throw new ArgumentNullException(nameof(logicalChain))).ToArray();
            if (chain.Length == 0 || chain.Any(path => path == null))
            {
                throw new ArgumentException(
                    "CSF provenance requires a nonempty logical chain.",
                    nameof(logicalChain));
            }

            this.logicalChain = Array.AsReadOnly(chain);
        }

        public string SourceId { get; }

        public IReadOnlyList<LogicalContentPath> LogicalChain => logicalChain;
    }

    internal sealed class CsfDocument
    {
        private readonly CsfLabel[] labels;
        private readonly IReadOnlyList<CsfLabel> labelView;

        internal CsfDocument(
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            CsfHeader header,
            IEnumerable<CsfLabel> labels)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
            Header = header ?? throw new ArgumentNullException(nameof(header));
            CsfLabel[] labelArray =
                (labels ?? throw new ArgumentNullException(nameof(labels))).ToArray();
            if (labelArray.Any(label => label == null))
            {
                throw new ArgumentException(
                    "A CSF document cannot contain null labels.",
                    nameof(labels));
            }

            this.labels = labelArray;
            labelView = Array.AsReadOnly(this.labels);
            CanonicalModelSha256 = CsfCanonicalModelHasher.Compute(this);
        }

        public BinarySourceContext Source { get; }

        public CsfSourceProvenance Provenance { get; }

        public CsfHeader Header { get; }

        public IReadOnlyList<CsfLabel> Labels => labelView;

        public CsfLabel this[int index] => labels[index];

        public string CanonicalModelSha256 { get; }

        public IReadOnlyList<CsfLabel> FindLabelsByExactOrdinalName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return Array.AsReadOnly(labels
                .Where(label => string.Equals(
                    label.Name,
                    name,
                    StringComparison.Ordinal))
                .ToArray());
        }
    }
}
