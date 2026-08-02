using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RA2YR.Core.Content;

namespace RA2YR.Core.Formats.Mix.Interop
{
    public enum XccSyntheticInteropStage
    {
        PrepareInternalContract,
        ValidateStagedCreatedCandidate,
        ValidateStagedExtractionCandidates
    }

    public enum XccSyntheticInteropDiagnosticCode
    {
        InvalidCaseId,
        UnsafeCacheBoundary,
        CaseAlreadyExists,
        CaseMissing,
        RequiredInputMissing,
        RequiredInputRejected,
        ArchiveBuildFailed,
        ArchiveReadFailed,
        ArchiveMismatch,
        PayloadMismatch,
        AtomicPublishFailed,
        ManifestWriteFailed,
        CleanupFailed,
        PublishedArtifactMismatch,
        ExtractionBudgetExceeded,
        ExtractionChanged
    }

    public sealed class XccSyntheticInteropDiagnostic
    {
        internal XccSyntheticInteropDiagnostic(
            XccSyntheticInteropDiagnosticCode code,
            string message,
            string cacheRelativePath = null)
        {
            XccSyntheticInteropModelRules.RequireDefinedEnum(code, nameof(code));
            Code = code;
            Message = XccSyntheticInteropModelRules.RequireStaticPublicText(
                message,
                nameof(message),
                512);
            CacheRelativePath = cacheRelativePath == null
                ? null
                : LogicalContentPath.Parse(cacheRelativePath).Value;
        }

        public XccSyntheticInteropDiagnosticCode Code { get; }

        public string Message { get; }

        public string CacheRelativePath { get; }
    }

    public sealed class XccSyntheticInteropArtifact
    {
        internal XccSyntheticInteropArtifact(
            string role,
            string cacheRelativePath,
            long length,
            string sha256)
        {
            Role = XccSyntheticInteropModelRules.RequireStaticPublicText(
                role,
                nameof(role),
                96);
            CacheRelativePath = LogicalContentPath.Parse(
                cacheRelativePath ?? throw new ArgumentNullException(
                    nameof(cacheRelativePath))).Value;
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(length),
                    "An artifact length cannot be negative.");
            }

            Length = length;
            Sha256 = Sha256Utilities.IsLowerSha256(sha256)
                ? sha256
                : throw new ArgumentException(
                    "A canonical lowercase SHA-256 value is required.",
                    nameof(sha256));
        }

        public string Role { get; }

        public string CacheRelativePath { get; }

        public long Length { get; }

        public string Sha256 { get; }
    }

    public sealed class XccSyntheticInteropResult
    {
        private XccSyntheticInteropResult(
            XccSyntheticInteropStage stage,
            string caseId,
            IEnumerable<XccSyntheticInteropArtifact> artifacts,
            IEnumerable<XccSyntheticInteropDiagnostic> diagnostics)
        {
            XccSyntheticInteropModelRules.RequireDefinedEnum(stage, nameof(stage));

            XccSyntheticInteropArtifact[] artifactArray =
                XccSyntheticInteropModelRules.CopyWithoutNullElements(
                    artifacts,
                    nameof(artifacts));
            XccSyntheticInteropDiagnostic[] diagnosticArray =
                XccSyntheticInteropModelRules.CopyWithoutNullElements(
                    diagnostics,
                    nameof(diagnostics));

            if (diagnosticArray.Length == 0)
            {
                if (!XccSyntheticInteropModelRules.IsValidCaseId(caseId))
                {
                    throw new ArgumentException(
                        "A successful result requires a canonical safe case id.",
                        nameof(caseId));
                }

                if (artifactArray.Length == 0)
                {
                    throw new ArgumentException(
                        "A successful result requires at least one artifact.",
                        nameof(artifacts));
                }
            }
            else
            {
                if (artifactArray.Length != 0)
                {
                    throw new ArgumentException(
                        "A failed result cannot expose artifacts.",
                        nameof(artifacts));
                }

                if (caseId != null &&
                    !XccSyntheticInteropModelRules.IsValidCaseId(caseId))
                {
                    throw new ArgumentException(
                        "A reported case id must be canonical and safe.",
                        nameof(caseId));
                }
            }

            Stage = stage;
            CaseId = caseId;
            Artifacts = new ReadOnlyCollection<XccSyntheticInteropArtifact>(
                artifactArray);
            Diagnostics = new ReadOnlyCollection<XccSyntheticInteropDiagnostic>(
                diagnosticArray);
        }

        public bool IsSuccess => Diagnostics.Count == 0;

        public bool IsRealXccExecutionEvidence => false;

        public XccSyntheticInteropStage Stage { get; }

        public string CaseId { get; }

        public IReadOnlyList<XccSyntheticInteropArtifact> Artifacts { get; }

        public IReadOnlyList<XccSyntheticInteropDiagnostic> Diagnostics { get; }

        internal static XccSyntheticInteropResult Success(
            XccSyntheticInteropStage stage,
            string caseId,
            IEnumerable<XccSyntheticInteropArtifact> artifacts)
        {
            return new XccSyntheticInteropResult(
                stage,
                caseId,
                artifacts,
                Array.Empty<XccSyntheticInteropDiagnostic>());
        }

        internal static XccSyntheticInteropResult Failure(
            XccSyntheticInteropStage stage,
            string caseId,
            XccSyntheticInteropDiagnostic diagnostic)
        {
            return Failure(
                stage,
                caseId,
                new[] { diagnostic ?? throw new ArgumentNullException(nameof(diagnostic)) });
        }

        internal static XccSyntheticInteropResult Failure(
            XccSyntheticInteropStage stage,
            string caseId,
            IEnumerable<XccSyntheticInteropDiagnostic> diagnostics)
        {
            return new XccSyntheticInteropResult(
                stage,
                XccSyntheticInteropModelRules.IsValidCaseId(caseId)
                    ? caseId
                    : null,
                Array.Empty<XccSyntheticInteropArtifact>(),
                diagnostics);
        }
    }

    internal static class XccSyntheticInteropModelRules
    {
        public static void RequireDefinedEnum<T>(T value, string parameterName)
            where T : struct
        {
            if (!Enum.IsDefined(typeof(T), value))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "A defined enumeration value is required.");
            }
        }

        public static string RequireStaticPublicText(
            string value,
            string parameterName,
            int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Static public text cannot be empty.",
                    parameterName);
            }

            if (value.Length > maximumLength ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Static public text is not in canonical form.",
                    parameterName);
            }

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (char.IsControl(character) || character == '/' ||
                    character == '\\' || character == ':')
                {
                    throw new ArgumentException(
                        "Static public text cannot contain path material or control characters.",
                        parameterName);
                }

                if (char.IsHighSurrogate(character))
                {
                    if (index + 1 >= value.Length ||
                        !char.IsLowSurrogate(value[index + 1]))
                    {
                        throw new ArgumentException(
                            "Static public text contains invalid UTF-16.",
                            parameterName);
                    }

                    int scalar = char.ConvertToUtf32(character, value[index + 1]);
                    if (IsUnicodeNoncharacter(scalar))
                    {
                        throw new ArgumentException(
                            "Static public text contains an unsafe Unicode value.",
                            parameterName);
                    }

                    index++;
                }
                else if (char.IsLowSurrogate(character) ||
                         IsUnicodeNoncharacter(character))
                {
                    throw new ArgumentException(
                        "Static public text contains an unsafe Unicode value.",
                        parameterName);
                }
            }

            return value;
        }

        public static T[] CopyWithoutNullElements<T>(
            IEnumerable<T> values,
            string parameterName)
            where T : class
        {
            if (values == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            T[] result = values.ToArray();
            if (result.Any(value => value == null))
            {
                throw new ArgumentException(
                    "Collections cannot contain null elements.",
                    parameterName);
            }

            return result;
        }

        public static bool IsValidCaseId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 64 ||
                value[0] < 'a' || value[0] > 'z')
            {
                return false;
            }

            for (int index = 1; index < value.Length; index++)
            {
                char character = value[index];
                if ((character < 'a' || character > 'z') &&
                    (character < '0' || character > '9') &&
                    character != '-' && character != '_')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsUnicodeNoncharacter(int scalar)
        {
            return (scalar >= 0xfdd0 && scalar <= 0xfdef) ||
                   (scalar & 0xffff) == 0xfffe ||
                   (scalar & 0xffff) == 0xffff;
        }
    }
}
