using System;
using UnityEngine;

namespace CatTennis.Rebuild.Flow
{
    /// <summary>Guards execution order without interpreting tennis rules.</summary>
    public sealed class PointLifecycleController : MonoBehaviour
    {
        public PointLoopState State { get; private set; } = PointLoopState.Uninitialized;

        public bool AllowsRallyEvents => State == PointLoopState.RallyActive;

        public void Initialize()
        {
            if (State != PointLoopState.Uninitialized)
            {
                throw new InvalidOperationException("Point lifecycle is already initialized.");
            }

            State = PointLoopState.StartingPoint;
        }

        public bool TryBeginNextPoint()
        {
            if (State != PointLoopState.ResetPending)
            {
                return false;
            }

            State = PointLoopState.StartingPoint;
            return true;
        }

        public bool TryActivateRally()
        {
            if (State != PointLoopState.StartingPoint)
            {
                return false;
            }

            State = PointLoopState.RallyActive;
            return true;
        }

        public bool TryBeginReset()
        {
            if (State != PointLoopState.RallyActive)
            {
                return false;
            }

            State = PointLoopState.ResetPending;
            return true;
        }

        public void MarkMatchEnded()
        {
            if (State != PointLoopState.ResetPending)
            {
                throw new InvalidOperationException("Match can end only while resolving a point.");
            }

            State = PointLoopState.MatchEnded;
        }

        public void BeginRetry()
        {
            if (State == PointLoopState.Uninitialized)
            {
                throw new InvalidOperationException("Point lifecycle is not initialized.");
            }

            State = PointLoopState.ResetPending;
        }

        public void ResetForSceneReload()
        {
            State = PointLoopState.Uninitialized;
        }
    }
}
