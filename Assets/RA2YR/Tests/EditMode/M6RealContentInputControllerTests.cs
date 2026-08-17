using NUnit.Framework;
using UnityEngine;
using RA2YR.UnityIntegration;

namespace RA2YR.Tests.EditMode
{
    public sealed class M6RealContentInputControllerTests
    {
        private GameObject root;

        [SetUp]
        public void SetUp() { root = new GameObject("M6InputControllerTest"); }

        [TearDown]
        public void TearDown() { if (root != null) Object.DestroyImmediate(root); }

        [Test]
        public void LeftDragStateCommitsOnlyAfterExplicitEnd()
        {
            UnityRtsInputController controller = root.AddComponent<UnityRtsInputController>();
            controller.BeginLeft(new Vector2(100f, 100f));
            Assert.That(controller.State, Is.EqualTo(UnityRtsInputState.SelectionDrag));
            Assert.That(controller.UpdateLeft(new Vector2(101f, 101f)), Is.False);
            Assert.That(controller.UpdateLeft(new Vector2(120f, 130f)), Is.True);
            bool wasDrag;
            Assert.That(controller.EndLeft(new Vector2(120f, 130f), out wasDrag), Is.True);
            Assert.That(wasDrag, Is.True);
            Assert.That(controller.State, Is.EqualTo(UnityRtsInputState.Idle));
        }

        [Test]
        public void RightDragIsDistinctFromRightClickCancel()
        {
            UnityRtsInputController controller = root.AddComponent<UnityRtsInputController>();
            controller.BeginRightDrag(new Vector2(200f, 200f));
            Assert.That(controller.State, Is.EqualTo(UnityRtsInputState.CameraRightDrag));
            controller.UpdateRightDrag(new Vector2(260f, 200f));
            controller.EndRightDrag();
            Assert.That(controller.State, Is.EqualTo(UnityRtsInputState.Idle));
            controller.EnterCommandTarget(UnityRtsInputState.AttackMove);
            controller.Cancel();
            Assert.That(controller.State, Is.EqualTo(UnityRtsInputState.Idle));
        }

        [Test]
        public void CommandTargetStatesRejectUnknownState()
        {
            UnityRtsInputController controller = root.AddComponent<UnityRtsInputController>();
            Assert.Throws<System.ArgumentOutOfRangeException>(() => controller.EnterCommandTarget(UnityRtsInputState.Idle));
        }
    }
}
