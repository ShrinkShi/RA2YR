using System;
using NUnit.Framework;
using RA2YR.Presentation;
using RA2YR.UnityIntegration;

namespace RA2YR.Tests.EditMode
{
    public sealed class PresentationObjectTests
    {
        private static ObjectVisualPresentationDescriptor Object(
            string id,
            long sourceOrdinal,
            long x = 0,
            long y = 0,
            PresentationObjectFamily family = PresentationObjectFamily.GroundActor,
            PresentationRenderPass pass = PresentationRenderPass.GroundObject,
            PresentationElevationLayer layer = PresentationElevationLayer.Ground,
            long level = 0,
            long height = 0,
            long parent = 0,
            int duplicate = 0,
            PresentationBounds? occupancy = null,
            PresentationBounds? foundation = null)
        {
            return new ObjectVisualPresentationDescriptor(
                new VisualAssetId(id), family, pass, layer,
                new PresentationAnchor(PresentationAnchorKind.LogicalGround, x, y),
                new PresentationBounds(PresentationBoundsKind.Visual, -1, -1, 1, 1),
                new PresentationBounds(PresentationBoundsKind.ConservativeCulling, -2, -2, 2, 2),
                id, sourceOrdinal, x, y, level, height, 0,
                null, occupancy, foundation, null, parent, 0, duplicate);
        }

        [Test]
        public void DescriptorKeepsAnchorAndBoundsAsSeparateContracts()
        {
            ObjectVisualPresentationDescriptor descriptor = Object("building", 4, 3, 5, PresentationObjectFamily.BuildingBody, PresentationRenderPass.Structure, PresentationElevationLayer.Ground, foundation: new PresentationBounds(PresentationBoundsKind.Foundation, 0, 0, 2, 2));
            Assert.AreEqual(PresentationAnchorKind.LogicalGround, descriptor.LogicalGroundAnchor.Kind);
            Assert.AreEqual(PresentationBoundsKind.Visual, descriptor.VisualBounds.Kind);
            Assert.IsTrue(descriptor.FoundationBounds.HasValue);
            Assert.IsFalse(descriptor.OccupancyBounds.HasValue);
        }

        [Test]
        public void FoundationIsNotInferredFromVisualBounds()
        {
            ObjectVisualPresentationDescriptor descriptor = Object("tree", 0);
            Assert.IsFalse(descriptor.FoundationBounds.HasValue);
            Assert.IsFalse(descriptor.OccupancyBounds.HasValue);
        }

        [Test]
        public void AircraftAndShadowUseExplicitElevationLayers()
        {
            ObjectVisualPresentationDescriptor aircraft = Object("air", 0, family: PresentationObjectFamily.Aircraft, pass: PresentationRenderPass.Aircraft, layer: PresentationElevationLayer.Air);
            ObjectVisualPresentationDescriptor shadow = Object("air-shadow", 1, family: PresentationObjectFamily.Shadow, pass: PresentationRenderPass.GroundShadow, layer: PresentationElevationLayer.Shadow);
            Assert.AreEqual(PresentationElevationLayer.Air, aircraft.ElevationLayer);
            Assert.AreEqual(PresentationElevationLayer.Shadow, shadow.ElevationLayer);
        }

        [Test]
        public void CameraDependentDepthPolicyIsRejected()
        {
            Assert.Throws<ArgumentException>(() => new ObjectVisualPresentationPolicy(cameraDependent: true));
        }

        [Test]
        public void DepthSortIsDeterministicAcrossInputOrder()
        {
            ObjectVisualPresentationDescriptor first = Object("first", 10, 5, 0);
            ObjectVisualPresentationDescriptor second = Object("second", 2, 1, 1);
            ObjectVisualPresentationResult a = ObjectVisualPresentationComposer.Compose(new[] { first, second });
            ObjectVisualPresentationResult b = ObjectVisualPresentationComposer.Compose(new[] { second, first });
            Assert.IsTrue(a.IsSuccess);
            Assert.IsTrue(b.IsSuccess);
            Assert.AreEqual(a.Entries[0].Descriptor.StableIdentity, b.Entries[0].Descriptor.StableIdentity);
            Assert.AreEqual(a.Entries[1].Descriptor.StableIdentity, b.Entries[1].Descriptor.StableIdentity);
        }

        [Test]
        public void PassAndElevationPrecedePrimaryDepth()
        {
            ObjectVisualPresentationDescriptor ground = Object("ground", 0, 100, 100);
            ObjectVisualPresentationDescriptor air = Object("air", 1, 0, 0, PresentationObjectFamily.Aircraft, PresentationRenderPass.Aircraft, PresentationElevationLayer.Air);
            ObjectVisualPresentationResult result = ObjectVisualPresentationComposer.Compose(new[] { ground, air });
            Assert.AreEqual("ground", result.Entries[0].Descriptor.StableIdentity);
            Assert.AreEqual("air", result.Entries[1].Descriptor.StableIdentity);
        }

        [Test]
        public void SourceOrdinalBreaksExactDepthTie()
        {
            ObjectVisualPresentationResult result = ObjectVisualPresentationComposer.Compose(new[] { Object("late", 8), Object("early", 2) });
            Assert.AreEqual("early", result.Entries[0].Descriptor.StableIdentity);
        }

        [Test]
        public void DuplicatePreservePolicyKeepsBothAndWarns()
        {
            ObjectVisualPresentationResult result = ObjectVisualPresentationComposer.Compose(new[] { Object("same", 0, duplicate: 0), Object("same", 1, duplicate: 1) });
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(2, result.Entries.Count);
            Assert.IsTrue(Has(result, ObjectPresentationDiagnosticCode.DuplicateStableIdentity));
            Assert.IsFalse(result.Execution.HasFatalError);
        }

        [Test]
        public void DuplicateRejectPolicyFailsClosed()
        {
            ObjectVisualPresentationResult result = ObjectVisualPresentationComposer.Compose(new[] { Object("same", 0), Object("same", 1) }, new ObjectVisualPresentationPolicy(duplicates: PresentationDuplicateObjectPolicy.RejectAnyDuplicate));
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Execution.HasFatalError);
        }

        [Test]
        public void ZeroDiagnosticBudgetStillFailsForDuplicate()
        {
            ObjectVisualPresentationResult result = ObjectVisualPresentationComposer.Compose(new[] { Object("same", 0), Object("same", 1) }, new ObjectVisualPresentationPolicy(duplicates: PresentationDuplicateObjectPolicy.RejectAnyDuplicate, maxDiagnostics: 0));
            Assert.IsFalse(result.IsSuccess);
            Assert.Greater(result.Execution.SuppressedDiagnosticCount, 0);
        }

        [Test]
        public void ObjectBudgetStopsEnumeration()
        {
            ObjectVisualPresentationResult result = ObjectVisualPresentationComposer.Compose(new[] { Object("one", 0), Object("two", 1) }, new ObjectVisualPresentationPolicy(maxObjects: 1));
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(1, result.Entries.Count);
            Assert.IsTrue(Has(result, ObjectPresentationDiagnosticCode.CellBudgetExceeded));
        }

        [Test]
        public void NullDescriptorFailsWithoutContinuing()
        {
            ObjectVisualPresentationResult result = ObjectVisualPresentationComposer.Compose(new ObjectVisualPresentationDescriptor[] { null, Object("never", 99) });
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, result.Entries.Count);
            Assert.IsTrue(Has(result, ObjectPresentationDiagnosticCode.NullDescriptor));
        }

        [Test]
        public void DepthArithmeticOverflowFailsClosed()
        {
            ObjectVisualPresentationResult result = ObjectVisualPresentationComposer.Compose(new[] { Object("overflow", 0, long.MaxValue, 1) });
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(Has(result, ObjectPresentationDiagnosticCode.DepthComponentOverflow));
        }

        [Test]
        public void AttachmentRequiresPresentParent()
        {
            ObjectVisualPresentationResult result = ObjectVisualPresentationComposer.Compose(new[] { Object("turret", 4, parent: 99, family: PresentationObjectFamily.Attachment) });
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(Has(result, ObjectPresentationDiagnosticCode.MissingAttachmentParent));
        }

        [Test]
        public void AttachmentCanUseExplicitParentSourceOrdinal()
        {
            ObjectVisualPresentationResult result = ObjectVisualPresentationComposer.Compose(new[] { Object("body", 99), Object("turret", 4, parent: 99, family: PresentationObjectFamily.Attachment) });
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(2, result.Entries.Count);
        }

        [Test]
        public void OccupancyBoundsRemainSeparateFromVisualBounds()
        {
            var occupancy = new PresentationBounds(PresentationBoundsKind.Occupancy, 0, 0, 1, 1);
            ObjectVisualPresentationDescriptor descriptor = Object("factory", 0, occupancy: occupancy);
            Assert.IsTrue(descriptor.OccupancyBounds.HasValue);
            Assert.AreEqual(PresentationBoundsKind.Occupancy, descriptor.OccupancyBounds.Value.Kind);
            Assert.AreNotEqual(descriptor.VisualBounds, descriptor.OccupancyBounds.Value);
        }

        [Test]
        public void DrawAdapterEmitsOrderedCommandsWithoutGameObjects()
        {
            ObjectVisualPresentationResult presentation = ObjectVisualPresentationComposer.Compose(new[] { Object("a", 0), Object("b", 1) });
            ObjectVisualDrawCommandResult result = ObjectVisualDrawCommandBuilder.Build(presentation);
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(2, result.Commands.Count);
            Assert.AreEqual("a", result.Commands[0].StableIdentity);
        }

        [Test]
        public void DrawAdapterFailsClosedForFailedPresentation()
        {
            ObjectVisualPresentationResult presentation = ObjectVisualPresentationComposer.Compose(new ObjectVisualPresentationDescriptor[] { null });
            ObjectVisualDrawCommandResult result = ObjectVisualDrawCommandBuilder.Build(presentation);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, result.Commands.Count);
        }

        [Test]
        public void PresentationAssemblyIsUnityFree()
        {
            Assert.AreEqual("RA2YR.Presentation", typeof(ObjectVisualPresentationDescriptor).Assembly.GetName().Name);
            Assert.IsFalse(typeof(ObjectVisualPresentationDescriptor).Assembly.FullName.Contains("UnityEngine"));
        }

        [Test]
        public void ExplicitZAdjustIsRetainedInDepthKey()
        {
            ObjectVisualPresentationDescriptor descriptor = new ObjectVisualPresentationDescriptor(new VisualAssetId("z"), PresentationObjectFamily.GroundActor, PresentationRenderPass.GroundObject, PresentationElevationLayer.Ground, new PresentationAnchor(PresentationAnchorKind.LogicalGround, 0, 0), new PresentationBounds(PresentationBoundsKind.Visual, 0, 0, 1, 1), new PresentationBounds(PresentationBoundsKind.ConservativeCulling, 0, 0, 1, 1), "z", 0, 0, 0, explicitZAdjust: 7);
            ObjectVisualPresentationResult result = ObjectVisualPresentationComposer.Compose(new[] { descriptor });
            Assert.AreEqual(7, result.Entries[0].DepthKey.ExplicitZAdjust);
        }

        private static bool Has(ObjectVisualPresentationResult result, ObjectPresentationDiagnosticCode code)
        {
            foreach (ObjectPresentationDiagnostic diagnostic in result.Diagnostics) if (diagnostic.Code == code) return true;
            return false;
        }
    }
}
