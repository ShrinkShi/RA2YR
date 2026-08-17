using System;
using UnityEngine;

namespace RA2YR.UnityIntegration
{
    public enum UnityRtsInputState
    {
        Idle,
        SelectionDrag,
        CommandTarget,
        CameraRightDrag,
        AttackMove,
        ForceFire,
        ForceMove,
        Canceled
    }

    /// <summary>
    /// Small stateful seam for RA2-style mouse interaction. The controller owns
    /// pointer state only; simulation commands remain in the client gateway.
    /// </summary>
    public sealed class UnityRtsInputController : MonoBehaviour
    {
        [SerializeField] private float dragThresholdPixels = 6f;
        public UnityRtsInputState State { get; private set; } = UnityRtsInputState.Idle;
        public Vector2 DragStartScreen { get; private set; }
        public Vector2 DragCurrentScreen { get; private set; }
        public bool IsDragging => State == UnityRtsInputState.SelectionDrag;
        public bool HasCommandTarget => State == UnityRtsInputState.CommandTarget || State == UnityRtsInputState.AttackMove || State == UnityRtsInputState.ForceFire || State == UnityRtsInputState.ForceMove;
        public float DragThresholdPixels => Mathf.Max(1f, dragThresholdPixels);

        public void BeginLeft(Vector2 screenPoint)
        {
            DragStartScreen = DragCurrentScreen = screenPoint;
            State = UnityRtsInputState.SelectionDrag;
        }

        public bool UpdateLeft(Vector2 screenPoint)
        {
            DragCurrentScreen = screenPoint;
            return IsDragging && Vector2.Distance(DragStartScreen, DragCurrentScreen) >= DragThresholdPixels;
        }

        public bool EndLeft(Vector2 screenPoint, out bool wasDrag)
        {
            DragCurrentScreen = screenPoint;
            wasDrag = IsDragging && Vector2.Distance(DragStartScreen, DragCurrentScreen) >= DragThresholdPixels;
            if (!IsDragging) return false;
            State = UnityRtsInputState.Idle;
            return true;
        }

        public void BeginRightDrag(Vector2 screenPoint)
        {
            DragStartScreen = DragCurrentScreen = screenPoint;
            State = UnityRtsInputState.CameraRightDrag;
        }

        public Vector2 UpdateRightDrag(Vector2 screenPoint)
        {
            Vector2 delta = screenPoint - DragCurrentScreen;
            DragCurrentScreen = screenPoint;
            return delta;
        }

        public void EndRightDrag()
        {
            if (State == UnityRtsInputState.CameraRightDrag) State = UnityRtsInputState.Idle;
        }

        public void EnterCommandTarget(UnityRtsInputState commandState = UnityRtsInputState.CommandTarget)
        {
            if (commandState != UnityRtsInputState.CommandTarget && commandState != UnityRtsInputState.AttackMove && commandState != UnityRtsInputState.ForceFire && commandState != UnityRtsInputState.ForceMove)
                throw new ArgumentOutOfRangeException(nameof(commandState));
            State = commandState;
        }

        public void Cancel()
        {
            State = UnityRtsInputState.Canceled;
            State = UnityRtsInputState.Idle;
            DragStartScreen = DragCurrentScreen = default(Vector2);
        }
    }
}
