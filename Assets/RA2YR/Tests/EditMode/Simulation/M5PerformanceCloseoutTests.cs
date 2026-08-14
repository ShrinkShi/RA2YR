using System;
using System.Linq;
using NUnit.Framework;
using RA2YR.Simulation;

namespace RA2YR.Tests.EditMode.Simulation
{
    public sealed class M5PerformanceCloseoutTests
    {
        private static M5StressResult Run(int entities, M5StressWorkload workload)
        {
            return new M5PerformanceCloseoutHarness(new M5StressConfig(entities, 3, workload)).Run();
        }

        [TestCase(500)]
        [TestCase(1000)]
        [TestCase(2000)]
        public void HarvestingStressUsesBoundedAuthoritativeEconomy(int entities)
        {
            var result = Run(entities, M5StressWorkload.Harvesting);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Aggregate.Credits, Is.EqualTo((long)entities * 3));
            Assert.That(result.Aggregate.Cargo, Is.EqualTo((long)entities * 3));
            Assert.That(result.PeakProposalCount, Is.EqualTo(entities));
        }

        [TestCase(500)]
        [TestCase(1000)]
        [TestCase(2000)]
        public void ProductionStressParsesDescriptorsOnceAndUsesDirectFactoryLookups(int entities)
        {
            var result = Run(entities, M5StressWorkload.Production);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.DescriptorParseCount, Is.EqualTo(entities));
            Assert.That(result.FactoryLookupCount, Is.EqualTo((long)entities * 3));
            Assert.That(result.PrerequisiteEvaluationCount, Is.EqualTo((long)entities * 3));
            Assert.That(result.Aggregate.QueueProgress, Is.EqualTo((long)entities * 3));
        }

        [TestCase(500)]
        [TestCase(1000)]
        [TestCase(2000)]
        public void CombatStressUsesDeterministicTargetPairs(int entities)
        {
            var result = Run(entities, M5StressWorkload.Combat);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.TargetingCandidateCount, Is.EqualTo(0));
            Assert.That(result.OperationCount, Is.EqualTo((long)entities * 3));
            Assert.That(result.StateHash, Is.Not.Empty);
        }

        [TestCase(500)]
        [TestCase(1000)]
        [TestCase(2000)]
        public void OccupancyStressUsesBoundedSpatialQueries(int entities)
        {
            var result = Run(entities, M5StressWorkload.Occupancy);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.OccupancyQueryCount, Is.EqualTo((long)entities * 3));
            Assert.That(result.PeakProposalCount, Is.EqualTo(entities));
        }

        [TestCase(500)]
        [TestCase(1000)]
        [TestCase(2000)]
        public void TargetingStressScalesLinearlyByCandidateCount(int entities)
        {
            var result = Run(entities, M5StressWorkload.Targeting);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.TargetingCandidateCount, Is.EqualTo((long)entities * 3));
            Assert.That(result.OperationCount, Is.EqualTo(0));
        }

        [TestCase(500)]
        [TestCase(1000)]
        [TestCase(2000)]
        public void AutonomyStressCommitsReadOnlyProposalsDeterministically(int entities)
        {
            var result = Run(entities, M5StressWorkload.Autonomy);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.AutonomyProposalCount, Is.EqualTo((long)entities * 3));
            Assert.That(result.Aggregate.Power, Is.EqualTo((long)entities * 3));
        }

        [TestCase(500)]
        [TestCase(1000)]
        [TestCase(2000)]
        public void MixedStressComposesAllBoundedPhases(int entities)
        {
            var result = Run(entities, M5StressWorkload.Mixed);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.DescriptorParseCount, Is.EqualTo(entities));
            Assert.That(result.OccupancyQueryCount, Is.EqualTo((long)entities * 3));
            Assert.That(result.TargetingCandidateCount, Is.EqualTo((long)entities * 3));
            Assert.That(result.AutonomyProposalCount, Is.EqualTo((long)entities * 3));
            Assert.That(result.Aggregate.OwnerZero, Is.EqualTo(entities / 2 + entities % 2));
            Assert.That(result.Aggregate.OwnerOne, Is.EqualTo(entities / 2));
        }

        [Test]
        public void RepeatedRunsHaveIdenticalHashesAtStressTier()
        {
            var config = new M5StressConfig(2000, 3, M5StressWorkload.Mixed, seed: 41);
            var first = new M5PerformanceCloseoutHarness(config).Run();
            var second = new M5PerformanceCloseoutHarness(config).Run();
            Assert.That(first.StateHash, Is.EqualTo(second.StateHash));
            Assert.That(first.OperationCount, Is.EqualTo(second.OperationCount));
        }

        [Test]
        public void ConfigurationSeedIsPartOfCanonicalStateIdentity()
        {
            var first = new M5PerformanceCloseoutHarness(new M5StressConfig(500, 2, M5StressWorkload.Harvesting, seed: 1)).Run();
            var second = new M5PerformanceCloseoutHarness(new M5StressConfig(500, 2, M5StressWorkload.Harvesting, seed: 2)).Run();
            Assert.That(first.StateHash, Is.Not.EqualTo(second.StateHash));
        }

        [Test]
        public void OperationBudgetFailsClosedBeforeUnboundedWork()
        {
            var result = new M5PerformanceCloseoutHarness(new M5StressConfig(500, 3, M5StressWorkload.Mixed, maxOperations: 2, maxDiagnostics: 0)).Run();
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Execution.HasFatalError, Is.True);
            Assert.That(result.Execution.SuppressedDiagnosticCount, Is.GreaterThan(0));
            Assert.That(result.Diagnostics, Is.Empty);
        }

        [Test]
        public void InvalidWorkloadIsRejectedByConfiguration()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new M5StressConfig(10, 1, (M5StressWorkload)999));
        }

        [Test]
        public void NoUnityReferencesInPerformanceAssembly()
        {
            Assert.That(typeof(M5PerformanceCloseoutHarness).Assembly.GetReferencedAssemblies(), Has.None.Matches<System.Reflection.AssemblyName>(x => x.Name == "UnityEngine" || x.Name == "UnityEditor"));
        }

        [Test]
        public void AggregateHashChangesWhenWorkloadChanges()
        {
            var harvest = Run(500, M5StressWorkload.Harvesting);
            var production = Run(500, M5StressWorkload.Production);
            Assert.That(harvest.StateHash, Is.Not.EqualTo(production.StateHash));
            Assert.That(harvest.Diagnostics.Concat(production.Diagnostics), Is.Empty);
        }
    }
}
