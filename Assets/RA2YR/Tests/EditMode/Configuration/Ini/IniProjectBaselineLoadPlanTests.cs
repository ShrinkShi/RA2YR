using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Configuration.Ini.Resolution;
using RA2YR.Core.Configuration.Ini.Typed;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Tests.EditMode.Configuration.Ini
{
    public sealed class IniProjectBaselineLoadPlanTests
    {
        private const string SourceId = "YR1001_ProjectBaseline";

        [Test]
        public void BuildsExplicitLowToHighProjectBaselineLayerOrder()
        {
            IniProjectBaselineLoadPlanBuildResult result = Build(
                Input("loose", "[S]\nK=loose", "rulesmd.ini"),
                Input("expand99", "[S]\nK=99", "expandmd99.mix", "rulesmd.ini"),
                Input("ra2", "[S]\nK=ra2", "ra2.mix", "local.mix", "rulesmd.ini"),
                Input("expand01", "[S]\nK=01", "expandmd01.mix", "rulesmd.ini"),
                Input("ra2md", "[S]\nK=md", "ra2md.mix", "localmd.mix", "rulesmd.ini"));

            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.Plan.Layers.Select(layer => layer.LayerId), Is.EqualTo(new[]
            {
                "projectbaseline-ra2",
                "projectbaseline-ra2md",
                "projectbaseline-expandmd01",
                "projectbaseline-expandmd99",
                "projectbaseline-loose"
            }));
            Assert.That(result.Plan.Layers.Select(layer => layer.Priority),
                Is.EqualTo(new int?[] { 100, 200, 301, 399, 1000 }));
        }

        [Test]
        public void MissingExpandNumbersAreLegalAndHigherNumberOverrides()
        {
            IniProjectBaselineLoadPlanBuildResult build = Build(
                Input("expand01", "[S]\nK=one\nInherited=low", "expandmd01.mix", "rulesmd.ini"),
                Input("expand07", "[S]\nK=seven\nAdded=high", "expandmd07.mix", "rulesmd.ini"));

            IniResolutionResult result = Resolve(build);

            Assert.That(Value(result, "s", "k"), Is.EqualTo("seven"));
            Assert.That(Value(result, "s", "inherited"), Is.EqualTo("low"));
            Assert.That(Value(result, "s", "added"), Is.EqualTo("high"));
        }

        [Test]
        public void FullCompositionOverridesPerKeyAndRetainsLowerUniqueValues()
        {
            IniProjectBaselineLoadPlanBuildResult build = Build(
                Input("ra2", "[S]\nBaseOnly=1\nShared=ra2", "ra2.mix", "local.mix", "rulesmd.ini"),
                Input("ra2md", "[S]\nMdOnly=2\nShared=ra2md", "ra2md.mix", "localmd.mix", "rulesmd.ini"),
                Input("expand", "[S]\nExpandOnly=3\nShared=expand", "expandmd01.mix", "rulesmd.ini"),
                Input("loose", "[S]\nLooseOnly=4\nShared=loose", "rulesmd.ini"));

            IniResolutionResult result = Resolve(build);

            Assert.That(result.Status, Is.EqualTo(IniResolutionStatus.Complete));
            Assert.That(Value(result, "s", "baseonly"), Is.EqualTo("1"));
            Assert.That(Value(result, "s", "mdonly"), Is.EqualTo("2"));
            Assert.That(Value(result, "s", "expandonly"), Is.EqualTo("3"));
            Assert.That(Value(result, "s", "looseonly"), Is.EqualTo("4"));
            Assert.That(Value(result, "s", "shared"), Is.EqualTo("loose"));
        }

        [Test]
        public void InputEnumerationOrderDoesNotChangePlanResolutionOrTrace()
        {
            IniProjectBaselineDocumentInput[] inputs =
            {
                Input("ra2md", "[S]\nK=base", "ra2md.mix", "localmd.mix", "rulesmd.ini"),
                Input("expand07", "[S]\nK=seven", "expandmd07.mix", "rulesmd.ini"),
                Input("expand01", "[S]\nK=one", "expandmd01.mix", "rulesmd.ini")
            };
            IniResolutionResult forward = Resolve(Build(inputs));
            IniResolutionResult reverse = Resolve(Build(inputs.Reverse().ToArray()));

            Assert.That(Value(reverse, "s", "k"), Is.EqualTo(Value(forward, "s", "k")));
            Assert.That(reverse.Trace.DocumentCandidates.Select(value => value.CandidateId),
                Is.EqualTo(forward.Trace.DocumentCandidates.Select(value => value.CandidateId)));
            Assert.That(GetValue(reverse, "s", "k").CandidateChain.Select(value =>
                    value.Document.LayerId),
                Is.EqualTo(GetValue(forward, "s", "k").CandidateChain.Select(value =>
                    value.Document.LayerId)));
        }

        [Test]
        public void ResolvedValueRetainsWinnerAndEveryOverriddenLayerProvenance()
        {
            IniResolutionResult result = Resolve(Build(
                Input("ra2", "[S]\nK=ra2", "ra2.mix", "local.mix", "rulesmd.ini"),
                Input("ra2md", "[S]\nK=md", "ra2md.mix", "localmd.mix", "rulesmd.ini"),
                Input("expand", "[S]\nK=expand", "expandmd01.mix", "rulesmd.ini")));

            IniResolvedValue value = GetValue(result, "s", "k");

            Assert.That(value.Winner.Document.LayerId,
                Is.EqualTo("projectbaseline-expandmd01"));
            Assert.That(value.CandidateChain.Select(candidate => candidate.Document.LayerId),
                Is.EqualTo(new[]
                {
                    "projectbaseline-expandmd01",
                    "projectbaseline-ra2md",
                    "projectbaseline-ra2"
                }));
            Assert.That(value.CandidateChain.Select(candidate =>
                    string.Join("/", candidate.Document.Document.Provenance.LogicalChain.Select(
                        path => path.Value))),
                Is.EqualTo(new[]
                {
                    "expandmd01.mix/rulesmd.ini",
                    "ra2md.mix/localmd.mix/rulesmd.ini",
                    "ra2.mix/local.mix/rulesmd.ini"
                }));
            Assert.That(value.CandidateChain.Skip(1).All(candidate =>
                candidate.Disposition == IniValueCandidateDisposition.OverriddenByFileComposition),
                Is.True);
        }

        [Test]
        public void TypedSourceTraceRetainsEveryDocumentLayer()
        {
            IniResolvedValue value = GetValue(Resolve(Build(
                Input("ra2md", "[S]\nK=base", "ra2md.mix", "localmd.mix", "rulesmd.ini"),
                Input("expand", "[S]\nK=expand", "expandmd01.mix", "rulesmd.ini"))),
                "s",
                "k");

            IniTypedParseResult parsed = IniTypedScalarParser.ParseRaw(value);

            Assert.That(parsed.Status, Is.EqualTo(IniTypedValueStatus.Present));
            Assert.That(parsed.Value.SourceTrace.Candidates.Select(candidate => candidate.LayerId),
                Is.EqualTo(new[]
                {
                    "projectbaseline-expandmd01",
                    "projectbaseline-ra2md"
                }));
        }

        [Test]
        public void DuplicateExpandNumberFailsWithStructuredDiagnostic()
        {
            IniProjectBaselineLoadPlanBuildResult result = Build(
                Input("first", "[S]\nK=1", "expandmd01.mix", "rulesmd.ini"),
                Input("second", "[S]\nK=2", "EXPANDMD01.MIX", "rulesmd.ini"));

            AssertFailed(result, IniResolutionDiagnosticCode.DuplicateExpandArchiveNumber);
        }

        [TestCase("expandmd00.mix")]
        [TestCase("expandmd1.mix")]
        [TestCase("expandmd100.mix")]
        [TestCase("expandmdxx.mix")]
        [TestCase("expandmd07.mixing")]
        public void InvalidExpandNumberFailsWithStructuredDiagnostic(string archiveName)
        {
            IniProjectBaselineLoadPlanBuildResult result = Build(
                Input("invalid", "[S]\nK=1", archiveName, "rulesmd.ini"));

            AssertFailed(result, IniResolutionDiagnosticCode.InvalidExpandArchiveNumber);
        }

        [Test]
        public void UnsupportedArchiveFailsWithoutBecomingFallback()
        {
            IniProjectBaselineLoadPlanBuildResult result = Build(
                Input("external", "[S]\nK=1", "finalalert2.mix", "rulesmd.ini"));

            AssertFailed(result, IniResolutionDiagnosticCode.UnsupportedProjectBaselineLayer);
        }

        [Test]
        public void NonProjectBaselineSourceFailsWithoutHostPathDisclosure()
        {
            IniProjectBaselineDocumentInput input = InputFromSource(
                "external",
                "[S]\nK=1",
                "ReferenceTools",
                "expandmd01.mix",
                "rulesmd.ini");

            IniProjectBaselineLoadPlanBuildResult result =
                IniProjectBaselineLoadPlanBuilder.Build(
                    "project-baseline-tests",
                    SourceId,
                    new[] { input });

            AssertFailed(result, IniResolutionDiagnosticCode.ProjectBaselineSourceRejected);
            Assert.That(result.Diagnostics.All(value =>
                !value.Message.Contains(":\\") &&
                (value.LogicalPath == null || !value.LogicalPath.Value.Contains("\\"))), Is.True);
        }

        [Test]
        public void DuplicateBaseLayerFailsRatherThanUsingCandidateIdentity()
        {
            IniProjectBaselineLoadPlanBuildResult result = Build(
                Input("first", "[S]\nK=1", "ra2md.mix", "localmd.mix", "rulesmd.ini"),
                Input("second", "[S]\nK=2", "RA2MD.MIX", "other.mix", "rulesmd.ini"));

            AssertFailed(result, IniResolutionDiagnosticCode.DuplicateProjectBaselineLayer);
        }

        [Test]
        public void ProjectBaselineEvidenceConfiguresPolicyButDoesNotConfirmOriginalRuntime()
        {
            IniResolutionEvidence evidence =
                IniProjectBaselineLoadPlanBuilder.CreateProjectBaselineEvidence();
            IniResolutionPolicy policy = ProjectPolicy();

            Assert.That(evidence.Level,
                Is.EqualTo(IniResolutionEvidenceLevel.ConfiguredForProjectBaseline));
            Assert.That(evidence.ConfiguresProjectBaseline, Is.True);
            Assert.That(evidence.ConfirmsRuntime, Is.False);
            Assert.That(policy.FileComposition,
                Is.EqualTo(IniFileCompositionPolicy.OverlayDocumentsLowToHigh));
            Assert.That(policy.FileCompositionEvidence.ConfiguresProjectBaseline, Is.True);
            Assert.That(policy.NameComparisonEvidence.Level,
                Is.EqualTo(IniResolutionEvidenceLevel.ConfiguredForTesting));
        }

        private static IniProjectBaselineLoadPlanBuildResult Build(
            params IniProjectBaselineDocumentInput[] inputs)
        {
            return IniProjectBaselineLoadPlanBuilder.Build(
                "project-baseline-tests",
                SourceId,
                inputs);
        }

        private static IniResolutionResult Resolve(
            IniProjectBaselineLoadPlanBuildResult build)
        {
            Assert.That(build.IsComplete, Is.True,
                string.Join(",", build.Diagnostics.Select(value => value.Code)));
            return new IniRuntimeResolver().Resolve(
                build.Plan,
                build.Candidates,
                ProjectPolicy());
        }

        private static IniResolutionPolicy ProjectPolicy()
        {
            IniResolutionEvidence testing = new IniResolutionEvidence(
                IniResolutionEvidenceLevel.ConfiguredForTesting,
                "project-baseline-composition-synthetic-intradocument-policy");
            return IniProjectBaselineLoadPlanBuilder.CreateResolutionPolicy(
                IniNameComparisonPolicy.OrdinalIgnoreCaseAscii,
                testing,
                IniDuplicateSectionPolicy.MergeSectionsInFileOrder,
                testing,
                IniDuplicateKeyPolicy.LastKeyWins,
                testing,
                IniInlineCommentPolicy.PreserveSemicolonInValue,
                testing,
                IniWhitespaceReadPolicy.Preserve,
                testing,
                IniEmptyValuePolicy.OverridesEarlierValue,
                testing);
        }

        private static IniProjectBaselineDocumentInput Input(
            string candidateId,
            string content,
            params string[] chain)
        {
            return InputFromSource(candidateId, content, SourceId, chain);
        }

        private static IniProjectBaselineDocumentInput InputFromSource(
            string candidateId,
            string content,
            string sourceId,
            params string[] chain)
        {
            LogicalContentPath[] paths = chain.Select(LogicalContentPath.Parse).ToArray();
            LogicalContentPath logicalName = paths[paths.Length - 1];
            IniParseResult parsed = WestwoodIniReader.Read(
                Encoding.ASCII.GetBytes(content),
                new BinarySourceContext(
                    "project-baseline-composition-tests",
                    sourceId,
                    logicalName),
                new IniSourceProvenance(sourceId, paths));
            Assert.That(parsed.IsSuccess, Is.True);
            return new IniProjectBaselineDocumentInput(candidateId, logicalName, parsed.Document);
        }

        private static IniResolvedValue GetValue(
            IniResolutionResult result,
            string section,
            string key)
        {
            Assert.That(result.Status, Is.EqualTo(IniResolutionStatus.Complete),
                string.Join(",", result.Diagnostics.Select(value => value.Code)));
            return result.Sections.Single(value => value.Name == section)
                .Values.Single(value => value.KeyName == key);
        }

        private static string Value(
            IniResolutionResult result,
            string section,
            string key)
        {
            return Encoding.ASCII.GetString(
                GetValue(result, section, key).Winner.CopyEffectiveValueBytes());
        }

        private static void AssertFailed(
            IniProjectBaselineLoadPlanBuildResult result,
            IniResolutionDiagnosticCode code)
        {
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.Candidates, Is.Empty);
            Assert.That(result.Diagnostics.Any(value => value.Code == code), Is.True,
                string.Join(",", result.Diagnostics.Select(value => value.Code)));
        }
    }
}
