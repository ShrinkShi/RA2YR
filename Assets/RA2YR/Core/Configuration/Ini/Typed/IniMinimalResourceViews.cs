using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RA2YR.Core.Configuration.Ini.Typed
{
    internal enum IniRulesRegistryKind
    {
        AircraftTypes,
        BuildingTypes,
        InfantryTypes,
        VehicleTypes,
        Animations
    }

    internal sealed class IniRulesRegistryEntry
    {
        public IniRulesRegistryEntry(
            IniRulesRegistryKind registry,
            string originalOrdinalKey,
            int ordinal,
            IniTypedParseResult identifier)
        {
            if (!Enum.IsDefined(typeof(IniRulesRegistryKind), registry) || ordinal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(registry));
            }

            Registry = registry;
            OriginalOrdinalKey = originalOrdinalKey ??
                throw new ArgumentNullException(nameof(originalOrdinalKey));
            Ordinal = ordinal;
            Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
        }

        public IniRulesRegistryKind Registry { get; }
        public string OriginalOrdinalKey { get; }
        public int Ordinal { get; }
        public IniTypedParseResult Identifier { get; }
    }

    internal sealed class IniRulesRegistry
    {
        private readonly IReadOnlyList<IniRulesRegistryEntry> entries;

        public IniRulesRegistry(
            IniRulesRegistryKind kind,
            IEnumerable<IniRulesRegistryEntry> entries)
        {
            if (!Enum.IsDefined(typeof(IniRulesRegistryKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            IniRulesRegistryEntry[] values =
                (entries ?? throw new ArgumentNullException(nameof(entries))).ToArray();
            if (values.Any(value => value == null || value.Registry != kind))
            {
                throw new ArgumentException("Rules registry entries have inconsistent ownership.");
            }

            Kind = kind;
            this.entries = Array.AsReadOnly(values);
        }

        public IniRulesRegistryKind Kind { get; }
        public IReadOnlyList<IniRulesRegistryEntry> Entries => entries;
    }

    internal sealed class IniRulesResourceDocument
    {
        private readonly IReadOnlyList<IniRulesRegistry> registries;

        public IniRulesResourceDocument(IEnumerable<IniRulesRegistry> registries)
        {
            IniRulesRegistry[] values =
                (registries ?? throw new ArgumentNullException(nameof(registries))).ToArray();
            if (values.Length != Enum.GetValues(typeof(IniRulesRegistryKind)).Length ||
                values.Any(value => value == null) ||
                values.Select(value => value.Kind).Distinct().Count() != values.Length)
            {
                throw new ArgumentException("Every minimal Rules registry must appear exactly once.");
            }

            this.registries = Array.AsReadOnly(values);
            CanonicalModelSha256 = IniTypedModelHasher.ComputeRules(values);
        }

        public IReadOnlyList<IniRulesRegistry> Registries => registries;
        public string CanonicalModelSha256 { get; }
        public int EntryCount => registries.Sum(registry => registry.Entries.Count);
    }

    internal enum IniArtFieldKind
    {
        Image,
        Cameo,
        AltCameo,
        Voxel,
        Remapable,
        NewTheater,
        Palette,
        CustomPalette,
        Buildup,
        ShadowIndex
    }

    internal enum IniExplicitResourceExtension
    {
        None,
        Shp,
        Vxl,
        Pal,
        Other
    }

    internal enum IniResourceRouteCandidate
    {
        Shp,
        Vxl,
        Unknown
    }

    internal sealed class IniArtResourceField
    {
        private readonly IReadOnlyList<IniTypedParseResult> parsedCandidates;

        private IniArtResourceField(
            IniArtFieldKind kind,
            IniTypedValueStatus status,
            IniTypedParseResult parsed,
            IEnumerable<IniTypedParseResult> parsedCandidates)
        {
            IniTypedParseResult[] candidates =
                (parsedCandidates ?? throw new ArgumentNullException(nameof(parsedCandidates)))
                .ToArray();
            if (!Enum.IsDefined(typeof(IniArtFieldKind), kind) ||
                !Enum.IsDefined(typeof(IniTypedValueStatus), status) ||
                candidates.Any(candidate => candidate == null) ||
                (status == IniTypedValueStatus.Missing &&
                    (parsed != null || candidates.Length != 0)) ||
                (status == IniTypedValueStatus.Ambiguous &&
                    (parsed != null || candidates.Length < 2 ||
                     candidates.Any(candidate => candidate.Value == null))) ||
                (status != IniTypedValueStatus.Missing &&
                 status != IniTypedValueStatus.Ambiguous &&
                    (parsed == null || parsed.Status != status || candidates.Length != 0)))
            {
                throw new ArgumentException("The Art resource field is inconsistent.");
            }

            Kind = kind;
            Status = status;
            Parsed = parsed;
            this.parsedCandidates = Array.AsReadOnly(candidates);
        }

        public IniArtFieldKind Kind { get; }
        public IniTypedValueStatus Status { get; }
        public IniTypedParseResult Parsed { get; }
        public IReadOnlyList<IniTypedParseResult> ParsedCandidates => parsedCandidates;

        internal static IniArtResourceField Missing(IniArtFieldKind kind)
        {
            return new IniArtResourceField(
                kind,
                IniTypedValueStatus.Missing,
                null,
                Array.Empty<IniTypedParseResult>());
        }

        internal static IniArtResourceField FromParse(
            IniArtFieldKind kind,
            IniTypedParseResult parsed)
        {
            return new IniArtResourceField(
                kind,
                (parsed ?? throw new ArgumentNullException(nameof(parsed))).Status,
                parsed,
                Array.Empty<IniTypedParseResult>());
        }

        internal static IniArtResourceField Ambiguous(
            IniArtFieldKind kind,
            IEnumerable<IniTypedParseResult> candidates)
        {
            return new IniArtResourceField(
                kind,
                IniTypedValueStatus.Ambiguous,
                null,
                candidates);
        }
    }

    internal sealed class IniResourceReference
    {
        public IniResourceReference(
            IniArtFieldKind field,
            IniTypedValue value,
            IniExplicitResourceExtension extension)
        {
            if (!Enum.IsDefined(typeof(IniArtFieldKind), field) ||
                !Enum.IsDefined(typeof(IniExplicitResourceExtension), extension) ||
                value == null || value.Kind != IniTypedValueKind.AsciiIdentifier)
            {
                throw new ArgumentException("A resource reference requires an explicit identifier.");
            }

            Field = field;
            Value = value;
            ExplicitExtension = extension;
        }

        public IniArtFieldKind Field { get; }
        public IniTypedValue Value { get; }
        public IniExplicitResourceExtension ExplicitExtension { get; }
    }

    internal sealed class IniResourceReferenceSet
    {
        private readonly IReadOnlyList<IniResourceReference> references;

        public IniResourceReferenceSet(IEnumerable<IniResourceReference> references)
        {
            IniResourceReference[] values =
                (references ?? throw new ArgumentNullException(nameof(references))).ToArray();
            if (values.Any(value => value == null))
            {
                throw new ArgumentException("Resource reference sets cannot contain null.");
            }

            this.references = Array.AsReadOnly(values);
        }

        public IReadOnlyList<IniResourceReference> References => references;
    }

    internal sealed class IniArtResourceRecord
    {
        private readonly IReadOnlyList<IniArtResourceField> fields;

        public IniArtResourceRecord(
            string sectionIdentifier,
            IEnumerable<IniArtResourceField> fields)
        {
            SectionIdentifier = sectionIdentifier ??
                throw new ArgumentNullException(nameof(sectionIdentifier));
            IniArtResourceField[] values =
                (fields ?? throw new ArgumentNullException(nameof(fields))).ToArray();
            if (values.Length != Enum.GetValues(typeof(IniArtFieldKind)).Length ||
                values.Any(value => value == null) ||
                values.Select(value => value.Kind).Distinct().Count() != values.Length)
            {
                throw new ArgumentException("Every explicit Art field state must appear once.");
            }

            this.fields = Array.AsReadOnly(values);
            References = new IniResourceReferenceSet(values
                .Where(value => value.Status == IniTypedValueStatus.Present &&
                                value.Parsed.Value.Kind == IniTypedValueKind.AsciiIdentifier)
                .Select(value => new IniResourceReference(
                    value.Kind,
                    value.Parsed.Value,
                    DetectExplicitExtension(value.Parsed.Value.Identifier))));
            RouteCandidate = GetRouteCandidate(values);
        }

        public string SectionIdentifier { get; }
        public IReadOnlyList<IniArtResourceField> Fields => fields;
        public IniResourceReferenceSet References { get; }
        public IniResourceRouteCandidate RouteCandidate { get; }

        private static IniExplicitResourceExtension DetectExplicitExtension(string identifier)
        {
            int dot = identifier.LastIndexOf('.');
            if (dot < 0 || dot == identifier.Length - 1)
            {
                return IniExplicitResourceExtension.None;
            }

            string extension = identifier.Substring(dot + 1);
            if (string.Equals(extension, "shp", StringComparison.OrdinalIgnoreCase))
            {
                return IniExplicitResourceExtension.Shp;
            }

            if (string.Equals(extension, "vxl", StringComparison.OrdinalIgnoreCase))
            {
                return IniExplicitResourceExtension.Vxl;
            }

            if (string.Equals(extension, "pal", StringComparison.OrdinalIgnoreCase))
            {
                return IniExplicitResourceExtension.Pal;
            }

            return IniExplicitResourceExtension.Other;
        }

        private static IniResourceRouteCandidate GetRouteCandidate(
            IEnumerable<IniArtResourceField> fields)
        {
            IniArtResourceField voxel = fields.Single(field =>
                field.Kind == IniArtFieldKind.Voxel);
            if (voxel.Status != IniTypedValueStatus.Present ||
                !voxel.Parsed.Value.BooleanValue.HasValue)
            {
                return IniResourceRouteCandidate.Unknown;
            }

            return voxel.Parsed.Value.BooleanValue.Value
                ? IniResourceRouteCandidate.Vxl
                : IniResourceRouteCandidate.Shp;
        }
    }

    internal sealed class IniArtResourceDocument
    {
        private readonly IReadOnlyList<IniArtResourceRecord> records;

        public IniArtResourceDocument(IEnumerable<IniArtResourceRecord> records)
        {
            IniArtResourceRecord[] values =
                (records ?? throw new ArgumentNullException(nameof(records))).ToArray();
            if (values.Any(value => value == null))
            {
                throw new ArgumentException("Art resource documents cannot contain null records.");
            }

            this.records = Array.AsReadOnly(values);
            CanonicalModelSha256 = IniTypedModelHasher.ComputeArt(values);
        }

        public IReadOnlyList<IniArtResourceRecord> Records => records;
        public string CanonicalModelSha256 { get; }
    }

    internal static class IniTypedModelHasher
    {
        public static string ComputeRules(IEnumerable<IniRulesRegistry> registries)
        {
            using (SHA256 sha = SHA256.Create())
            {
                Append(sha, "RA2YR.INI.MINIMAL.RULES.V1");
                IniRulesRegistry[] values = registries.ToArray();
                Append(sha, values.Length.ToString(CultureInfo.InvariantCulture));
                foreach (IniRulesRegistry registry in values)
                {
                    Append(sha, registry.Kind.ToString());
                    Append(sha, registry.Entries.Count.ToString(
                        CultureInfo.InvariantCulture));
                    foreach (IniRulesRegistryEntry entry in registry.Entries)
                    {
                        Append(sha, entry.OriginalOrdinalKey);
                        Append(sha, entry.Ordinal.ToString(CultureInfo.InvariantCulture));
                        AppendParse(sha, entry.Identifier);
                    }
                }

                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return ToLowerHex(sha.Hash);
            }
        }

        public static string ComputeArt(IEnumerable<IniArtResourceRecord> records)
        {
            using (SHA256 sha = SHA256.Create())
            {
                Append(sha, "RA2YR.INI.MINIMAL.ART.V1");
                IniArtResourceRecord[] values = records.ToArray();
                Append(sha, values.Length.ToString(CultureInfo.InvariantCulture));
                foreach (IniArtResourceRecord record in values)
                {
                    Append(sha, record.SectionIdentifier);
                    foreach (IniArtResourceField field in record.Fields)
                    {
                        Append(sha, field.Kind.ToString());
                        Append(sha, field.Status.ToString());
                        if (field.Parsed != null)
                        {
                            AppendParse(sha, field.Parsed);
                        }
                        else if (field.Status == IniTypedValueStatus.Ambiguous)
                        {
                            Append(sha, field.ParsedCandidates.Count.ToString(
                                CultureInfo.InvariantCulture));
                            foreach (IniTypedParseResult candidate in field.ParsedCandidates)
                            {
                                AppendParse(sha, candidate);
                            }
                        }
                    }
                }

                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return ToLowerHex(sha.Hash);
            }
        }

        private static void AppendParse(SHA256 sha, IniTypedParseResult parsed)
        {
            Append(sha, parsed.Status.ToString());
            if (parsed.Value == null)
            {
                return;
            }

            Append(sha, parsed.Value.Kind.ToString());
            AppendBlob(sha, parsed.Value.CopyRawBytes());
            Append(sha, parsed.Value.SourceTrace.Candidates.Count.ToString(
                CultureInfo.InvariantCulture));
            foreach (IniValueSourceCandidateTrace trace in parsed.Value.SourceTrace.Candidates)
            {
                Append(sha, trace.CandidateId);
                Append(sha, trace.SourceId);
                Append(sha, trace.LogicalName.Value);
                Append(sha, trace.SectionPhysicalLineId.ToString(CultureInfo.InvariantCulture));
                Append(sha, trace.KeyPhysicalLineId.ToString(CultureInfo.InvariantCulture));
                Append(sha, trace.Disposition.ToString());
                Append(sha, trace.ContainsInlineSemicolon ? "true" : "false");
                Append(sha, trace.LogicalChain.Count.ToString(
                    CultureInfo.InvariantCulture));
                foreach (var path in trace.LogicalChain)
                {
                    Append(sha, path.Value);
                }
            }
        }

        private static void Append(SHA256 sha, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            uint length = checked((uint)bytes.Length);
            AppendBytes(sha, new[]
            {
                (byte)length,
                (byte)(length >> 8),
                (byte)(length >> 16),
                (byte)(length >> 24)
            });
            AppendBytes(sha, bytes);
        }

        private static void AppendBlob(SHA256 sha, byte[] bytes)
        {
            uint length = checked((uint)bytes.Length);
            AppendBytes(sha, new[]
            {
                (byte)length,
                (byte)(length >> 8),
                (byte)(length >> 16),
                (byte)(length >> 24)
            });
            AppendBytes(sha, bytes);
        }

        private static void AppendBytes(SHA256 sha, byte[] bytes)
        {
            sha.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
        }

        private static string ToLowerHex(byte[] bytes)
        {
            return string.Concat(bytes.Select(value =>
                value.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }
}
