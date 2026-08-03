using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Content.Ini.Audit;
using RA2YR.Core.Formats.Ini;
using RA2YR.Core.Formats.Mix;

namespace RA2YR.Tests.EditMode.Configuration.Ini.Typed
{
    public sealed class IniMinimalResourceAuditTests
    {
        [Test]
        public void SanitizedAggregateDoesNotPublishSyntheticObjectOrResourceNames()
        {
            IniProjectBaselineAuditModel model = Model(
                Sample(
                    "rulesmd-expandmd01",
                    "expandmd01.mix",
                    null,
                    "rulesmd.ini",
                    "[VehicleTypes]\n0=PRIVATE_RULES_EXPAND"),
                Sample(
                    "rulesmd-localmd",
                    "ra2md.mix",
                    "localmd.mix",
                    "rulesmd.ini",
                    "[VehicleTypes]\n0=PRIVATE_RULES_LOCAL"),
                Sample(
                    "artmd-localmd",
                    "ra2md.mix",
                    "localmd.mix",
                    "artmd.ini",
                    "[PRIVATE_SECTION]\nImage=PRIVATE_RESOURCE\nVoxel=yes"),
                Sample(
                    "ai-local",
                    "ra2.mix",
                    "local.mix",
                    "ai.ini",
                    "[AI]\nTask=PRIVATE_AI"));
            var physical = new IniProjectBaselineAuditDelivery(
                4,
                0,
                "{}",
                "wp02f/ini-audits/synthetic.json",
                2,
                new string('b', 64));

            IniMinimalResourceProjectBaselineAuditDelivery delivery =
                IniMinimalResourceProjectBaselineAudit.Build(model, physical);

            Assert.That(delivery.SanitizedSummary, Does.Contain(
                "\"stockRuntimeWinnerSelected\":false"));
            Assert.That(delivery.SanitizedSummary, Does.Contain(
                "\"rulesCandidates\":["));
            Assert.That(delivery.SanitizedSummary, Does.Contain(
                "\"artCandidate\":"));
            Assert.That(delivery.SanitizedSummary, Does.Not.Contain("PRIVATE_"));
            Assert.That(delivery.SanitizedSummary, Does.Not.Contain(
                model.Source.RootPath));
            Assert.That(delivery.SummarySha256, Has.Length.EqualTo(64));
        }

        private static IniProjectBaselineAuditModel Model(
            params IniGoldenSampleRecord[] samples)
        {
            var source = new ExternalContentSourceDescriptor(
                IniProjectBaselineAuditService.BaselineLogicalName,
                ContentSourceKind.Patched,
                Path.GetFullPath("SyntheticProjectBaseline"),
                300,
                "synthetic-project-baseline",
                true);
            return new IniProjectBaselineAuditModel(
                source,
                new string('c', 64),
                samples,
                Array.Empty<IniSurveyCandidate>(),
                Array.Empty<LogicalContentPath>(),
                new DateTime(2026, 8, 3, 8, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 8, 3, 8, 0, 1, DateTimeKind.Utc));
        }

        private static IniGoldenSampleRecord Sample(
            string sampleId,
            string root,
            string nested,
            string logicalName,
            string content)
        {
            LogicalContentPath rootPath = LogicalContentPath.Parse(root);
            LogicalContentPath nestedPath = nested == null
                ? null
                : LogicalContentPath.Parse(nested);
            LogicalContentPath logicalPath = LogicalContentPath.Parse(logicalName);
            LogicalContentPath[] chain = nestedPath == null
                ? new[] { rootPath, logicalPath }
                : new[] { rootPath, nestedPath, logicalPath };
            byte[] bytes = Encoding.ASCII.GetBytes(content);
            IniParseResult parsed = WestwoodIniReader.Read(
                bytes,
                new BinarySourceContext(
                    "wp02g2-audit-test",
                    IniProjectBaselineAuditService.BaselineLogicalName,
                    logicalPath),
                new IniSourceProvenance(
                    IniProjectBaselineAuditService.BaselineLogicalName,
                    chain));
            Assert.That(parsed.IsSuccess, Is.True);
            var specification = new IniGoldenSampleSpecification(
                sampleId,
                root,
                nested,
                logicalName,
                bytes.Length,
                new string('a', 64));
            IniAuditProvenanceLayer[] layers = nestedPath == null
                ? new[]
                {
                    new IniAuditProvenanceLayer(
                        rootPath,
                        MixFileId.ComputeCandidateId(logicalName),
                        logicalPath)
                }
                : new[]
                {
                    new IniAuditProvenanceLayer(
                        rootPath,
                        MixFileId.ComputeCandidateId(nested),
                        nestedPath),
                    new IniAuditProvenanceLayer(
                        LogicalContentPath.Parse(root + "/" + nested),
                        MixFileId.ComputeCandidateId(logicalName),
                        logicalPath)
                };
            IniLineAuditRecord[] lineRecords = parsed.Document.Lines
                .Select((line, index) => new IniLineAuditRecord(
                    line.Id,
                    line.AbsoluteOffset,
                    line.Content.Length,
                    line.Ending.Length,
                    line.EndingKind,
                    parsed.Document.Nodes[index].Kind,
                    parsed.Document.Nodes[index] is IniOpaqueNode opaque
                        ? (IniOpaqueReason?)opaque.Reason
                        : null,
                    new string('d', 64)))
                .ToArray();
            return new IniGoldenSampleRecord(
                specification,
                new IniAuditProvenance(
                    IniProjectBaselineAuditService.BaselineLogicalName,
                    rootPath,
                    layers),
                parsed.Document,
                bytes.Length,
                new string('a', 64),
                new string('a', 64),
                "wp02f/identity/" + sampleId + ".ini",
                true,
                parsed.Document.Lines.Count(line =>
                    line.EndingKind == IniLineEnding.CarriageReturnLineFeed),
                parsed.Document.Lines.Count(line =>
                    line.EndingKind == IniLineEnding.LineFeed),
                parsed.Document.Lines.Count(line =>
                    line.EndingKind == IniLineEnding.CarriageReturn),
                parsed.Document.Lines.Count(line => line.EndingKind == IniLineEnding.None),
                parsed.Document.Nodes.Count(node => node.Kind == IniNodeKind.Section),
                parsed.Document.Nodes.Count(node => node.Kind == IniNodeKind.KeyValue),
                parsed.Document.Nodes.Count(node => node.Kind == IniNodeKind.Comment),
                parsed.Document.Nodes.Count(node => node.Kind == IniNodeKind.Blank),
                parsed.Document.Nodes.Count(node => node.Kind == IniNodeKind.Opaque),
                0,
                0,
                parsed.Document.Lines.Max(line => line.Content.Length),
                new Dictionary<string, int>(),
                lineRecords);
        }
    }
}
