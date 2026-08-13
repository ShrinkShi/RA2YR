using System.Collections.Generic;
using NUnit.Framework;
using RA2YR.Simulation;

namespace RA2YR.Tests.EditMode.Simulation
{
    public sealed class StructuresPlacementTests
    {
        private static StructureReadLimits Limits(int diagnostics = 32) => new StructureReadLimits(8, 64, diagnostics);
        private static StructureDefinitionRaw Def(int w = 2, int h = 2, int health = 100) => new StructureDefinitionRaw(0, "PowerPlant", w, h, health, 100, 0, "Player0");
        [Test] public void RawDefinitionPreservesFootprintHealthPowerAndOwner() { var d = Def(); Assert.That(d.TypeRaw, Is.EqualTo("PowerPlant")); Assert.That(d.Width, Is.EqualTo(2)); Assert.That(d.MaxHealth, Is.EqualTo(100)); Assert.That(d.PowerProduced, Is.EqualTo(100)); Assert.That(d.OwnerRaw, Is.EqualTo("Player0")); }
        [Test] public void PlacementBuildsExplicitRectangularFootprint() { var r = StructurePlacementResult.Evaluate(Def(), 1, 2, 8, 8, null, StructurePlacementProfile.ExplicitRectangularFootprint, Limits()); Assert.That(r.IsSuccess, Is.True); Assert.That(r.Footprint.Count, Is.EqualTo(4)); Assert.That(r.Footprint[0].X, Is.EqualTo(1)); }
        [Test] public void PlacementRejectsOutOfBoundsWithoutClamping() { var r = StructurePlacementResult.Evaluate(Def(), 7, 7, 8, 8, null, StructurePlacementProfile.ExplicitRectangularFootprint, Limits()); Assert.That(r.IsSuccess, Is.False); Assert.That(r.OriginX, Is.EqualTo(7)); Assert.That(r.OriginY, Is.EqualTo(7)); }
        [Test] public void PlacementRejectsOverlapCandidate() { var r = StructurePlacementResult.Evaluate(Def(), 1, 1, 8, 8, new[] { new StructureFootprintCell(2, 2) }, StructurePlacementProfile.ExplicitRectangularFootprint, Limits()); Assert.That(r.IsSuccess, Is.False); }
        [Test] public void InvalidPlacementPolicyFailsClosed() { var r = StructurePlacementResult.Evaluate(Def(), 0, 0, 8, 8, null, (StructurePlacementProfile)99, Limits(0)); Assert.That(r.IsSuccess, Is.False); Assert.That(r.Execution.SuppressedDiagnosticCount, Is.GreaterThan(0)); }
        [Test] public void InvalidDefinitionFailsClosed() { var r = StructurePlacementResult.Evaluate(Def(0, 2), 0, 0, 8, 8, null, StructurePlacementProfile.ExplicitRectangularFootprint, Limits()); Assert.That(r.IsSuccess, Is.False); }
        [Test] public void PowerProjectionIsCheckedAndDeterministic() { var p = new StructurePowerProjection(new[] { Def(), new StructureDefinitionRaw(1, "Ref", 1, 1, 50, 0, 40, "Player0") }); Assert.That(p.Produced, Is.EqualTo(100)); Assert.That(p.Consumed, Is.EqualTo(40)); Assert.That(p.LowPower, Is.False); }
        [Test] public void RepairCandidateRequiresDamage() { IReadOnlyList<StructureDiagnostic> d; var c = StructureInteractionAnalyzer.Analyze(StructureInteractionAction.RepairCandidate, new PlayerId(0), new PlayerId(0), 100, 100, false, false, Limits(), out d); Assert.That(c.Allowed, Is.False); Assert.That(d[0].Code, Is.EqualTo(StructureDiagnosticCode.RepairInvalid)); }
        [Test] public void CaptureCandidateRejectsSameOwner() { IReadOnlyList<StructureDiagnostic> d; var c = StructureInteractionAnalyzer.Analyze(StructureInteractionAction.CaptureCandidate, new PlayerId(0), new PlayerId(0), 50, 100, false, false, Limits(), out d); Assert.That(c.Allowed, Is.False); }
        [Test] public void DeployCandidateRejectsAlreadyDeployed() { IReadOnlyList<StructureDiagnostic> d; var c = StructureInteractionAnalyzer.Analyze(StructureInteractionAction.DeployCandidate, new PlayerId(0), new PlayerId(1), 50, 100, true, false, Limits(), out d); Assert.That(c.Allowed, Is.False); }
        [Test] public void ZeroDiagnosticBudgetStillFailsInvalidInteraction() { IReadOnlyList<StructureDiagnostic> d; var c = StructureInteractionAnalyzer.Analyze((StructureInteractionAction)99, new PlayerId(0), new PlayerId(1), 50, 100, false, false, Limits(0), out d); Assert.That(c.Allowed, Is.False); Assert.That(d.Count, Is.EqualTo(0)); }
        [Test] public void FootprintHashIsStableForSameCandidate() { var a = StructurePlacementResult.Evaluate(Def(), 1, 1, 8, 8, null, StructurePlacementProfile.ExplicitRectangularFootprint, Limits()); var b = StructurePlacementResult.Evaluate(Def(), 1, 1, 8, 8, null, StructurePlacementProfile.ExplicitRectangularFootprint, Limits()); Assert.That(a.CanonicalHash, Is.EqualTo(b.CanonicalHash)); }
        [Test] public void CoreStructureAssemblyHasNoUnityReferences() { Assert.That(typeof(StructurePlacementResult).Assembly.GetReferencedAssemblies(), Has.None.Matches<System.Reflection.AssemblyName>(x => x.Name == "UnityEngine" || x.Name == "UnityEditor")); }
    }
}
