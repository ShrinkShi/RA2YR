using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Configuration.Ini.Resolution
{
    internal sealed class IniRuntimeResolver
    {
        public IniResolutionResult Resolve(
            IniLoadPlan plan,
            IEnumerable<IniCandidateDocument> candidates,
            IniResolutionPolicy policy,
            IniResolutionLimits limits = null)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            limits = limits ?? IniResolutionLimits.Default;
            var diagnostics = new DiagnosticCollector(limits.MaxDiagnostics);
            IniCandidateDocument[] input = candidates.ToArray();
            if (input.Length == 0)
            {
                diagnostics.AddError(
                    IniResolutionDiagnosticCode.EmptyCandidateSet,
                    "INI resolution requires at least one candidate document.");
                return Failed(Array.Empty<IniCandidateDocument>(), diagnostics);
            }

            if (input.Any(candidate => candidate == null))
            {
                throw new ArgumentException("INI candidates cannot contain null.", nameof(candidates));
            }

            if (input.Length > limits.MaxDocuments)
            {
                diagnostics.AddError(
                    IniResolutionDiagnosticCode.DocumentBudgetExceeded,
                    "The INI candidate document budget was exceeded.");
                return Failed(input, diagnostics);
            }

            if (plan.Layers.Count > limits.MaxLayers)
            {
                diagnostics.AddError(
                    IniResolutionDiagnosticCode.LayerBudgetExceeded,
                    "The INI load layer budget was exceeded.");
                return Failed(input, diagnostics);
            }

            if (input.Select(candidate => candidate.CandidateId)
                .Distinct(StringComparer.Ordinal).Count() != input.Length)
            {
                diagnostics.AddError(
                    IniResolutionDiagnosticCode.DuplicateCandidateId,
                    "INI candidate ids must be unique.");
                return Failed(input, diagnostics);
            }

            var layerById = plan.Layers.ToDictionary(
                layer => layer.LayerId,
                StringComparer.Ordinal);
            var bound = new List<BoundDocument>();
            foreach (IniCandidateDocument candidate in input)
            {
                IniLoadLayer layer;
                if (!layerById.TryGetValue(candidate.LayerId, out layer))
                {
                    diagnostics.AddError(
                        IniResolutionDiagnosticCode.CandidateLayerMissing,
                        "An INI candidate refers to a layer absent from the load plan.",
                        candidate.LogicalName,
                        candidate.CandidateId);
                    continue;
                }

                if (!HasCompleteMatchingProvenance(candidate, layer))
                {
                    diagnostics.AddError(
                        IniResolutionDiagnosticCode.IncompleteProvenance,
                        "An INI candidate does not match its complete logical source chain.",
                        candidate.LogicalName,
                        candidate.CandidateId);
                    continue;
                }

                if (!candidate.Document.Source.LogicalPath.Equals(candidate.LogicalName) ||
                    !layer.LogicalChain[layer.LogicalChain.Count - 1].Equals(candidate.LogicalName))
                {
                    diagnostics.AddError(
                        IniResolutionDiagnosticCode.LogicalNameMismatch,
                        "An INI candidate logical name disagrees with its source context.",
                        candidate.LogicalName,
                        candidate.CandidateId);
                    continue;
                }

                bound.Add(new BoundDocument(candidate, layer));
            }

            if (diagnostics.HasErrors)
            {
                return Failed(SortDocuments(bound).Select(value => value.Candidate), diagnostics);
            }

            LogicalContentPath logicalName = bound[0].Candidate.LogicalName;
            if (bound.Any(value => !value.Candidate.LogicalName.Equals(logicalName)))
            {
                diagnostics.AddError(
                    IniResolutionDiagnosticCode.MultipleLogicalNames,
                    "One INI resolution operation may resolve only one logical file name.");
                return Failed(SortDocuments(bound).Select(value => value.Candidate), diagnostics);
            }

            BoundDocument[] orderedDocuments = SortDocuments(bound).ToArray();
            bool policyAmbiguous = ValidateDocumentPolicies(
                orderedDocuments,
                policy,
                diagnostics,
                logicalName);
            if (policy.NameComparison == IniNameComparisonPolicy.Unresolved)
            {
                diagnostics.AddError(
                    IniResolutionDiagnosticCode.UnresolvedNameComparison,
                    "The INI section and key comparison policy is unresolved.",
                    logicalName);
                policyAmbiguous = true;
            }

            if (policyAmbiguous &&
                policy.NameComparison == IniNameComparisonPolicy.Unresolved)
            {
                return Ambiguous(
                    orderedDocuments.Select(value => value.Candidate),
                    Array.Empty<IniResolvedValueCandidate>(),
                    diagnostics);
            }

            BoundDocument selectedDocument = SelectDocumentForWholeFile(
                orderedDocuments,
                policy.FileComposition);
            var working = new List<WorkingValueCandidate>();
            foreach (BoundDocument document in orderedDocuments)
            {
                ExtractDocumentCandidates(document, policy, working, diagnostics, limits);
                if (diagnostics.HasFatalBudgetError)
                {
                    return Failed(
                        orderedDocuments.Select(value => value.Candidate),
                        diagnostics,
                        working);
                }
            }

            var resolvedValues = new List<IniResolvedValue>();
            var traceValues = new List<IniResolvedValueCandidate>();
            foreach (IGrouping<ValueIdentity, WorkingValueCandidate> group in working
                .GroupBy(value => new ValueIdentity(value.NormalizedSection, value.NormalizedKey))
                .OrderBy(value => value.Key.Section, StringComparer.Ordinal)
                .ThenBy(value => value.Key.Key, StringComparer.Ordinal))
            {
                if (resolvedValues.Count >= limits.MaxResolvedValues)
                {
                    diagnostics.AddBudgetError(
                        IniResolutionDiagnosticCode.ResolvedValueBudgetExceeded,
                        "The resolved INI value budget was exceeded.");
                    return Failed(
                        orderedDocuments.Select(value => value.Candidate),
                        diagnostics,
                        working);
                }

                ResolveValueGroup(
                    group.Key,
                    group.ToArray(),
                    orderedDocuments,
                    selectedDocument,
                    policy,
                    diagnostics,
                    resolvedValues,
                    traceValues);
            }

            if (policyAmbiguous || diagnostics.HasErrors)
            {
                foreach (WorkingValueCandidate candidate in working.Where(value =>
                    value.Disposition == IniValueCandidateDisposition.Candidate))
                {
                    candidate.Disposition = IniValueCandidateDisposition.Ambiguous;
                }

                traceValues = CreateTraceValues(working).ToList();
                return Ambiguous(
                    orderedDocuments.Select(value => value.Candidate),
                    traceValues,
                    diagnostics,
                    resolvedValues);
            }

            IniResolvedSection[] sections = resolvedValues
                .GroupBy(value => value.SectionName, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => new IniResolvedSection(
                    group.Key,
                    group.OrderBy(value => value.KeyName, StringComparer.Ordinal)))
                .ToArray();
            return IniResolutionResult.Create(
                IniResolutionStatus.Complete,
                sections,
                new IniResolutionTrace(
                    orderedDocuments.Select(value => value.Candidate),
                    traceValues),
                diagnostics.Values);
        }

        private static bool HasCompleteMatchingProvenance(
            IniCandidateDocument candidate,
            IniLoadLayer layer)
        {
            IniSourceProvenance provenance = candidate.Document.Provenance;
            return string.Equals(
                       provenance.SourceId,
                       layer.SourceId,
                       StringComparison.OrdinalIgnoreCase) &&
                   provenance.LogicalChain.Count == layer.LogicalChain.Count &&
                   provenance.LogicalChain.Zip(
                       layer.LogicalChain,
                       (left, right) => left.Equals(right)).All(value => value);
        }

        private static IEnumerable<BoundDocument> SortDocuments(
            IEnumerable<BoundDocument> documents)
        {
            return documents
                .OrderByDescending(value => value.Layer.Priority.HasValue)
                .ThenByDescending(value => value.Layer.Priority ?? int.MinValue)
                .ThenBy(value => ChainKey(value.Layer.LogicalChain), StringComparer.OrdinalIgnoreCase)
                .ThenBy(value => value.Candidate.CandidateId, StringComparer.Ordinal);
        }

        private static string ChainKey(IEnumerable<LogicalContentPath> chain)
        {
            return string.Join("/", chain.Select(path => path.Value));
        }

        private static bool ValidateDocumentPolicies(
            IReadOnlyList<BoundDocument> documents,
            IniResolutionPolicy policy,
            DiagnosticCollector diagnostics,
            LogicalContentPath logicalName)
        {
            if (documents.Count <= 1)
            {
                return false;
            }

            bool ambiguous = false;
            if (policy.FileComposition == IniFileCompositionPolicy.Unresolved)
            {
                diagnostics.AddError(
                    IniResolutionDiagnosticCode.UnresolvedFileComposition,
                    "Multiple INI documents exist but file composition is unresolved.",
                    logicalName);
                ambiguous = true;
            }

            if (documents.Any(value => !value.Layer.Priority.HasValue))
            {
                diagnostics.AddError(
                    IniResolutionDiagnosticCode.MissingLayerPriority,
                    "Multiple INI candidates require explicit layer priorities.",
                    logicalName);
                ambiguous = true;
            }

            if (!documents.All(value => value.Layer.Priority.HasValue))
            {
                return ambiguous;
            }

            bool equalRelevantPriority;
            if (policy.FileComposition ==
                IniFileCompositionPolicy.SelectHighestPriorityDocument)
            {
                int highest = documents.Max(value => value.Layer.Priority.Value);
                equalRelevantPriority = documents.Count(value =>
                    value.Layer.Priority.Value == highest) > 1;
            }
            else
            {
                equalRelevantPriority = documents
                    .GroupBy(value => value.Layer.Priority.Value)
                    .Any(group => group.Count() > 1);
            }

            if (equalRelevantPriority)
            {
                diagnostics.AddError(
                    IniResolutionDiagnosticCode.EqualLayerPriority,
                    "Relevant INI candidates share an explicit priority; identity is not a tiebreaker.",
                    logicalName);
                ambiguous = true;
            }

            return ambiguous;
        }

        private static BoundDocument SelectDocumentForWholeFile(
            IReadOnlyList<BoundDocument> orderedDocuments,
            IniFileCompositionPolicy composition)
        {
            return composition == IniFileCompositionPolicy.SelectHighestPriorityDocument
                ? orderedDocuments.FirstOrDefault()
                : null;
        }

        private static void ExtractDocumentCandidates(
            BoundDocument document,
            IniResolutionPolicy policy,
            ICollection<WorkingValueCandidate> output,
            DiagnosticCollector diagnostics,
            IniResolutionLimits limits)
        {
            IniRawDocument raw = document.Candidate.Document;
            var sections = raw.Nodes.OfType<IniSectionNode>()
                .ToDictionary(section => section.PhysicalLineId);
            foreach (IniNode node in raw.Nodes)
            {
                var opaque = node as IniOpaqueNode;
                if (opaque != null)
                {
                    diagnostics.AddWarning(
                        IniResolutionDiagnosticCode.OpaqueNodeNotExecuted,
                        "An opaque INI node was preserved but not executed.",
                        document.Candidate.LogicalName,
                        document.Candidate.CandidateId,
                        opaque.PhysicalLineId);
                    continue;
                }

                var key = node as IniKeyValueNode;
                if (key == null)
                {
                    continue;
                }

                if (output.Count >= limits.MaxValueCandidates)
                {
                    diagnostics.AddBudgetError(
                        IniResolutionDiagnosticCode.ValueCandidateBudgetExceeded,
                        "The INI value candidate budget was exceeded.");
                    return;
                }

                IniSectionNode section;
                if (!sections.TryGetValue(key.ContainingSectionLineId, out section))
                {
                    diagnostics.AddError(
                        IniResolutionDiagnosticCode.IncompleteProvenance,
                        "An INI value node has no stable physical section owner.",
                        document.Candidate.LogicalName,
                        document.Candidate.CandidateId,
                        key.PhysicalLineId);
                    continue;
                }

                string sectionName;
                string keyName;
                if (!TryDecodeAsciiName(section.Name, raw.PhysicalEncoding, out sectionName) ||
                    !TryDecodeAsciiName(key.Key, raw.PhysicalEncoding, out keyName))
                {
                    diagnostics.AddError(
                        IniResolutionDiagnosticCode.NonAsciiRuntimeName,
                        "A runtime INI name is outside the explicit raw-ASCII comparison boundary.",
                        document.Candidate.LogicalName,
                        document.Candidate.CandidateId,
                        key.PhysicalLineId);
                    continue;
                }

                ValueTransform transform = TransformValue(
                    key,
                    raw.PhysicalEncoding,
                    policy,
                    diagnostics,
                    document.Candidate);
                output.Add(new WorkingValueCandidate(
                    document,
                    key.ContainingSectionLineId,
                    key.PhysicalLineId,
                    NormalizeName(sectionName, policy.NameComparison),
                    NormalizeName(keyName, policy.NameComparison),
                    transform.Bytes,
                    transform.IsEmpty,
                    transform.ContainsSemicolon));
            }
        }

        private static ValueTransform TransformValue(
            IniKeyValueNode key,
            IniPhysicalEncodingKind encoding,
            IniResolutionPolicy policy,
            DiagnosticCollector diagnostics,
            IniCandidateDocument document)
        {
            byte[] leading = key.WhitespaceAfterEquals.ToArray();
            byte[] value = key.Value.ToArray();
            var combined = new byte[checked(leading.Length + value.Length)];
            Buffer.BlockCopy(leading, 0, combined, 0, leading.Length);
            Buffer.BlockCopy(value, 0, combined, leading.Length, value.Length);
            int width = GetUnitWidth(encoding);
            bool hasSemicolon = FindAsciiUnit(combined, width, (byte)';') >= 0;
            if (hasSemicolon)
            {
                if (policy.InlineComments == IniInlineCommentPolicy.SemicolonStartsComment)
                {
                    int semicolon = FindAsciiUnit(combined, width, (byte)';');
                    Array.Resize(ref combined, semicolon);
                }
                else if (policy.InlineComments == IniInlineCommentPolicy.Unresolved)
                {
                    diagnostics.AddError(
                        IniResolutionDiagnosticCode.UnresolvedInlineComment,
                        "An inline semicolon is present but its runtime policy is unresolved.",
                        document.LogicalName,
                        document.CandidateId,
                        key.PhysicalLineId);
                }
            }

            if (policy.Whitespace == IniWhitespaceReadPolicy.TrimAsciiSpaceAndTab)
            {
                combined = TrimAsciiWhitespace(combined, width);
            }
            else if (policy.Whitespace == IniWhitespaceReadPolicy.Unresolved &&
                     HasEdgeAsciiWhitespace(combined, width))
            {
                diagnostics.AddError(
                    IniResolutionDiagnosticCode.UnresolvedWhitespace,
                    "A value has edge whitespace but its runtime policy is unresolved.",
                    document.LogicalName,
                    document.CandidateId,
                    key.PhysicalLineId);
            }

            return new ValueTransform(combined, combined.Length == 0, hasSemicolon);
        }

        private static void ResolveValueGroup(
            ValueIdentity identity,
            WorkingValueCandidate[] values,
            IReadOnlyList<BoundDocument> orderedDocuments,
            BoundDocument selectedDocument,
            IniResolutionPolicy policy,
            DiagnosticCollector diagnostics,
            ICollection<IniResolvedValue> resolvedValues,
            ICollection<IniResolvedValueCandidate> traceValues)
        {
            var documentWinners = new List<WorkingValueCandidate>();
            bool groupAmbiguous = false;
            foreach (BoundDocument document in orderedDocuments)
            {
                WorkingValueCandidate[] documentValues = values
                    .Where(value => value.Document == document)
                    .OrderBy(value => value.SectionLineId)
                    .ThenBy(value => value.KeyLineId)
                    .ToArray();
                if (documentValues.Length == 0)
                {
                    continue;
                }

                WorkingValueCandidate[] eligible = ApplyDuplicateSectionPolicy(
                    documentValues,
                    policy.DuplicateSections,
                    diagnostics,
                    ref groupAmbiguous);
                eligible = ApplyEmptyValuePolicy(
                    eligible,
                    policy.EmptyValues,
                    diagnostics,
                    ref groupAmbiguous);
                WorkingValueCandidate winner = ApplyDuplicateKeyPolicy(
                    eligible,
                    policy.DuplicateKeys,
                    diagnostics,
                    ref groupAmbiguous);
                if (winner != null)
                {
                    documentWinners.Add(winner);
                }
            }

            WorkingValueCandidate finalWinner = null;
            if (!groupAmbiguous)
            {
                if (policy.FileComposition ==
                    IniFileCompositionPolicy.SelectHighestPriorityDocument)
                {
                    finalWinner = documentWinners.FirstOrDefault(value =>
                        value.Document == selectedDocument);
                }
                else if (policy.FileComposition ==
                         IniFileCompositionPolicy.OverlayDocumentsLowToHigh)
                {
                    finalWinner = documentWinners
                        .OrderByDescending(value => value.Document.Layer.Priority.HasValue)
                        .ThenByDescending(value =>
                            value.Document.Layer.Priority ?? int.MinValue)
                        .ThenBy(value => value.Document.Candidate.CandidateId, StringComparer.Ordinal)
                        .FirstOrDefault();
                }
                else if (orderedDocuments.Count == 1)
                {
                    finalWinner = documentWinners.SingleOrDefault();
                }
            }

            foreach (WorkingValueCandidate documentWinner in documentWinners)
            {
                if (documentWinner != finalWinner)
                {
                    documentWinner.Disposition = groupAmbiguous
                        ? IniValueCandidateDisposition.Ambiguous
                        : IniValueCandidateDisposition.OverriddenByFileComposition;
                }
            }

            if (groupAmbiguous || finalWinner == null)
            {
                foreach (WorkingValueCandidate value in values.Where(candidate =>
                    candidate.Disposition == IniValueCandidateDisposition.Candidate))
                {
                    value.Disposition = IniValueCandidateDisposition.Ambiguous;
                }

                foreach (IniResolvedValueCandidate candidate in CreateTraceValues(values))
                {
                    traceValues.Add(candidate);
                }

                return;
            }

            finalWinner.Disposition = IniValueCandidateDisposition.Winner;
            IniResolvedValueCandidate[] immutableChain = CreateTraceValues(values).ToArray();
            foreach (IniResolvedValueCandidate candidate in immutableChain)
            {
                traceValues.Add(candidate);
            }

            IniResolvedValueCandidate immutableWinner = immutableChain.Single(value =>
                value.Disposition == IniValueCandidateDisposition.Winner);
            resolvedValues.Add(new IniResolvedValue(
                identity.Section,
                identity.Key,
                immutableWinner,
                immutableChain));
        }

        private static WorkingValueCandidate[] ApplyDuplicateSectionPolicy(
            WorkingValueCandidate[] values,
            IniDuplicateSectionPolicy policy,
            DiagnosticCollector diagnostics,
            ref bool ambiguous)
        {
            int[] sectionIds = values.Select(value => value.SectionLineId).Distinct().ToArray();
            if (sectionIds.Length <= 1 || policy == IniDuplicateSectionPolicy.MergeSectionsInFileOrder)
            {
                return values;
            }

            if (policy == IniDuplicateSectionPolicy.Unresolved)
            {
                diagnostics.AddError(
                    IniResolutionDiagnosticCode.UnresolvedDuplicateSection,
                    "A duplicate physical section requires an explicit runtime policy.",
                    values[0].Document.Candidate.LogicalName,
                    values[0].Document.Candidate.CandidateId);
                ambiguous = true;
                return values;
            }

            int selected = policy == IniDuplicateSectionPolicy.FirstSectionWins
                ? sectionIds.Min()
                : sectionIds.Max();
            foreach (WorkingValueCandidate value in values.Where(value =>
                value.SectionLineId != selected))
            {
                value.Disposition = IniValueCandidateDisposition.SuppressedByDuplicateSection;
            }

            return values.Where(value => value.SectionLineId == selected).ToArray();
        }

        private static WorkingValueCandidate[] ApplyEmptyValuePolicy(
            WorkingValueCandidate[] values,
            IniEmptyValuePolicy policy,
            DiagnosticCollector diagnostics,
            ref bool ambiguous)
        {
            if (!values.Any(value => value.IsEmpty))
            {
                return values;
            }

            if (policy == IniEmptyValuePolicy.Unresolved)
            {
                diagnostics.AddError(
                    IniResolutionDiagnosticCode.UnresolvedEmptyValue,
                    "An empty INI value requires an explicit override policy.",
                    values[0].Document.Candidate.LogicalName,
                    values[0].Document.Candidate.CandidateId);
                ambiguous = true;
                return values;
            }

            if (policy == IniEmptyValuePolicy.DoesNotOverrideEarlierValue)
            {
                foreach (WorkingValueCandidate value in values.Where(value => value.IsEmpty))
                {
                    value.Disposition = IniValueCandidateDisposition.SuppressedByEmptyValuePolicy;
                }

                return values.Where(value => !value.IsEmpty).ToArray();
            }

            return values;
        }

        private static WorkingValueCandidate ApplyDuplicateKeyPolicy(
            WorkingValueCandidate[] values,
            IniDuplicateKeyPolicy policy,
            DiagnosticCollector diagnostics,
            ref bool ambiguous)
        {
            if (values.Length == 0)
            {
                return null;
            }

            if (values.Length == 1)
            {
                return values[0];
            }

            if (policy == IniDuplicateKeyPolicy.Unresolved)
            {
                diagnostics.AddError(
                    IniResolutionDiagnosticCode.UnresolvedDuplicateKey,
                    "A duplicate INI key requires an explicit runtime policy.",
                    values[0].Document.Candidate.LogicalName,
                    values[0].Document.Candidate.CandidateId);
                ambiguous = true;
                return null;
            }

            WorkingValueCandidate selected = policy == IniDuplicateKeyPolicy.FirstKeyWins
                ? values.OrderBy(value => value.KeyLineId).First()
                : values.OrderByDescending(value => value.KeyLineId).First();
            foreach (WorkingValueCandidate value in values.Where(value => value != selected))
            {
                value.Disposition = IniValueCandidateDisposition.SuppressedByDuplicateKey;
            }

            return selected;
        }

        private static IReadOnlyList<IniResolvedValueCandidate> CreateTraceValues(
            IEnumerable<WorkingValueCandidate> values)
        {
            return Array.AsReadOnly(values
                .OrderByDescending(value => value.Document.Layer.Priority.HasValue)
                .ThenByDescending(value => value.Document.Layer.Priority ?? int.MinValue)
                .ThenBy(value => ChainKey(value.Document.Layer.LogicalChain),
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(value => value.Document.Candidate.CandidateId, StringComparer.Ordinal)
                .ThenBy(value => value.SectionLineId)
                .ThenBy(value => value.KeyLineId)
                .Select(value => new IniResolvedValueCandidate(
                    value.Document.Candidate,
                    value.SectionLineId,
                    value.KeyLineId,
                    value.Disposition,
                    value.EffectiveValueBytes,
                    value.IsEmpty,
                    value.ContainsSemicolon))
                .ToArray());
        }

        private static bool TryDecodeAsciiName(
            IniRawSlice slice,
            IniPhysicalEncodingKind encoding,
            out string value)
        {
            byte[] bytes = slice.ToArray();
            int width = GetUnitWidth(encoding);
            if (bytes.Length % width != 0)
            {
                value = null;
                return false;
            }

            var characters = new char[bytes.Length / width];
            for (int index = 0; index < characters.Length; index++)
            {
                int offset = index * width;
                byte ascii;
                if (width == 1)
                {
                    ascii = bytes[offset];
                }
                else if (encoding == IniPhysicalEncodingKind.Utf16LittleEndianWithBom)
                {
                    if (bytes[offset + 1] != 0)
                    {
                        value = null;
                        return false;
                    }

                    ascii = bytes[offset];
                }
                else
                {
                    if (bytes[offset] != 0)
                    {
                        value = null;
                        return false;
                    }

                    ascii = bytes[offset + 1];
                }

                if (ascii > 0x7f || ascii == 0)
                {
                    value = null;
                    return false;
                }

                characters[index] = (char)ascii;
            }

            value = new string(characters);
            return true;
        }

        private static string NormalizeName(string value, IniNameComparisonPolicy policy)
        {
            if (policy == IniNameComparisonPolicy.OrdinalRawAscii)
            {
                return value;
            }

            var normalized = value.ToCharArray();
            for (int index = 0; index < normalized.Length; index++)
            {
                if (normalized[index] >= 'A' && normalized[index] <= 'Z')
                {
                    normalized[index] = (char)(normalized[index] + ('a' - 'A'));
                }
            }

            return new string(normalized);
        }

        private static int GetUnitWidth(IniPhysicalEncodingKind encoding)
        {
            return encoding == IniPhysicalEncodingKind.Utf16LittleEndianWithBom ||
                   encoding == IniPhysicalEncodingKind.Utf16BigEndianWithBom
                ? 2
                : 1;
        }

        private static int FindAsciiUnit(byte[] bytes, int width, byte value)
        {
            for (int offset = 0; offset <= bytes.Length - width; offset += width)
            {
                if (width == 1 ? bytes[offset] == value :
                    (bytes[offset] == value && bytes[offset + 1] == 0) ||
                    (bytes[offset] == 0 && bytes[offset + 1] == value))
                {
                    return offset;
                }
            }

            return -1;
        }

        private static byte[] TrimAsciiWhitespace(byte[] bytes, int width)
        {
            int start = 0;
            int end = bytes.Length;
            while (start < end && IsAsciiWhitespaceUnit(bytes, start, width))
            {
                start += width;
            }

            while (end > start && IsAsciiWhitespaceUnit(bytes, end - width, width))
            {
                end -= width;
            }

            var result = new byte[end - start];
            Buffer.BlockCopy(bytes, start, result, 0, result.Length);
            return result;
        }

        private static bool HasEdgeAsciiWhitespace(byte[] bytes, int width)
        {
            return bytes.Length > 0 &&
                   (IsAsciiWhitespaceUnit(bytes, 0, width) ||
                    IsAsciiWhitespaceUnit(bytes, bytes.Length - width, width));
        }

        private static bool IsAsciiWhitespaceUnit(byte[] bytes, int offset, int width)
        {
            if (width == 1)
            {
                return bytes[offset] == (byte)' ' || bytes[offset] == (byte)'\t';
            }

            return (bytes[offset] == (byte)' ' || bytes[offset] == (byte)'\t') &&
                       bytes[offset + 1] == 0 ||
                   bytes[offset] == 0 &&
                       (bytes[offset + 1] == (byte)' ' || bytes[offset + 1] == (byte)'\t');
        }

        private static IniResolutionResult Failed(
            IEnumerable<IniCandidateDocument> documents,
            DiagnosticCollector diagnostics,
            IEnumerable<WorkingValueCandidate> working = null)
        {
            return IniResolutionResult.Create(
                IniResolutionStatus.Failed,
                Array.Empty<IniResolvedSection>(),
                new IniResolutionTrace(
                    documents,
                    working == null
                        ? Array.Empty<IniResolvedValueCandidate>()
                        : CreateTraceValues(working)),
                diagnostics.Values);
        }

        private static IniResolutionResult Ambiguous(
            IEnumerable<IniCandidateDocument> documents,
            IEnumerable<IniResolvedValueCandidate> values,
            DiagnosticCollector diagnostics,
            IEnumerable<IniResolvedValue> resolvedValues = null)
        {
            IniResolvedSection[] sections = (resolvedValues ??
                    Enumerable.Empty<IniResolvedValue>())
                .GroupBy(value => value.SectionName, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => new IniResolvedSection(group.Key, group))
                .ToArray();
            return IniResolutionResult.Create(
                IniResolutionStatus.Ambiguous,
                sections,
                new IniResolutionTrace(documents, values),
                diagnostics.Values);
        }

        private sealed class BoundDocument
        {
            public BoundDocument(IniCandidateDocument candidate, IniLoadLayer layer)
            {
                Candidate = candidate;
                Layer = layer;
            }

            public IniCandidateDocument Candidate { get; }
            public IniLoadLayer Layer { get; }
        }

        private sealed class WorkingValueCandidate
        {
            public WorkingValueCandidate(
                BoundDocument document,
                int sectionLineId,
                int keyLineId,
                string normalizedSection,
                string normalizedKey,
                byte[] effectiveValueBytes,
                bool isEmpty,
                bool containsSemicolon)
            {
                Document = document;
                SectionLineId = sectionLineId;
                KeyLineId = keyLineId;
                NormalizedSection = normalizedSection;
                NormalizedKey = normalizedKey;
                EffectiveValueBytes = effectiveValueBytes;
                IsEmpty = isEmpty;
                ContainsSemicolon = containsSemicolon;
                Disposition = IniValueCandidateDisposition.Candidate;
            }

            public BoundDocument Document { get; }
            public int SectionLineId { get; }
            public int KeyLineId { get; }
            public string NormalizedSection { get; }
            public string NormalizedKey { get; }
            public byte[] EffectiveValueBytes { get; }
            public bool IsEmpty { get; }
            public bool ContainsSemicolon { get; }
            public IniValueCandidateDisposition Disposition { get; set; }
        }

        private readonly struct ValueIdentity : IEquatable<ValueIdentity>
        {
            public ValueIdentity(string section, string key)
            {
                Section = section;
                Key = key;
            }

            public string Section { get; }
            public string Key { get; }

            public bool Equals(ValueIdentity other)
            {
                return string.Equals(Section, other.Section, StringComparison.Ordinal) &&
                       string.Equals(Key, other.Key, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is ValueIdentity other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (StringComparer.Ordinal.GetHashCode(Section) * 397) ^
                           StringComparer.Ordinal.GetHashCode(Key);
                }
            }
        }

        private readonly struct ValueTransform
        {
            public ValueTransform(byte[] bytes, bool isEmpty, bool containsSemicolon)
            {
                Bytes = bytes;
                IsEmpty = isEmpty;
                ContainsSemicolon = containsSemicolon;
            }

            public byte[] Bytes { get; }
            public bool IsEmpty { get; }
            public bool ContainsSemicolon { get; }
        }

        private sealed class DiagnosticCollector
        {
            private readonly int maximum;
            private readonly List<IniResolutionDiagnostic> values =
                new List<IniResolutionDiagnostic>();

            public DiagnosticCollector(int maximum)
            {
                this.maximum = maximum;
            }

            public IReadOnlyList<IniResolutionDiagnostic> Values => values;

            public bool HasErrors => values.Any(value =>
                value.Severity == IniResolutionDiagnosticSeverity.Error);

            public bool HasFatalBudgetError => values.Any(value =>
                value.Code == IniResolutionDiagnosticCode.ValueCandidateBudgetExceeded ||
                value.Code == IniResolutionDiagnosticCode.DiagnosticBudgetExceeded);

            public void AddWarning(
                IniResolutionDiagnosticCode code,
                string message,
                LogicalContentPath path = null,
                string candidateId = null,
                int? lineId = null)
            {
                Add(new IniResolutionDiagnostic(
                    code,
                    IniResolutionDiagnosticSeverity.Warning,
                    message,
                    path,
                    candidateId,
                    lineId));
            }

            public void AddError(
                IniResolutionDiagnosticCode code,
                string message,
                LogicalContentPath path = null,
                string candidateId = null,
                int? lineId = null)
            {
                Add(new IniResolutionDiagnostic(
                    code,
                    IniResolutionDiagnosticSeverity.Error,
                    message,
                    path,
                    candidateId,
                    lineId));
            }

            public void AddBudgetError(
                IniResolutionDiagnosticCode code,
                string message)
            {
                AddError(code, message);
            }

            private void Add(IniResolutionDiagnostic diagnostic)
            {
                if (values.Count < maximum)
                {
                    values.Add(diagnostic);
                    return;
                }

                if (values.Any(value =>
                    value.Code == IniResolutionDiagnosticCode.DiagnosticBudgetExceeded))
                {
                    return;
                }

                if (values.Count > 0)
                {
                    values.RemoveAt(values.Count - 1);
                }

                values.Add(new IniResolutionDiagnostic(
                    IniResolutionDiagnosticCode.DiagnosticBudgetExceeded,
                    IniResolutionDiagnosticSeverity.Error,
                    "The INI resolution diagnostic budget was exceeded."));
            }
        }
    }
}
