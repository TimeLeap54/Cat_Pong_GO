using CatTennis.BallPhysics.Core;
using CatTennis.Rebuild.Shot;
using UnityEngine;

namespace CatTennis.Rebuild.Ball
{
    /// <summary>Exposes lifecycle commands while delegating all calculations.</summary>
    [RequireComponent(typeof(BallPhysicsApplier))]
    public sealed class BallController : MonoBehaviour
    {
        [SerializeField] private BallPhysicsApplier physicsApplier;

        public BallSnapshot CurrentSnapshot => Applier.CurrentSnapshot;
        public BallStepResult LastStepResult => Applier.LastStepResult;
        public BallPlayMode PlayMode { get; private set; } = BallPlayMode.Inactive;
        public ShotIntent LastShotIntent { get; private set; } = ShotIntent.Undefined;
        public bool LastShotWasKillSmash { get; private set; } = false;

        private BallPhysicsApplier Applier
        {
            get
            {
                if (physicsApplier == null)
                {
                    physicsApplier = GetComponent<BallPhysicsApplier>();
                }

                return physicsApplier;
            }
        }

        public void ResetBall(Vector2 position)
        {
            Applier.ResetBall(position);
            PlayMode = BallPlayMode.Inactive;
            LastShotIntent = ShotIntent.Undefined;
            LastShotWasKillSmash = false;
        }

        public void Launch(Vector2 velocity)
        {
            Applier.Launch(velocity);
        }

        public void StopBall()
        {
            Applier.StopBall();
            PlayMode = BallPlayMode.Inactive;
            LastShotIntent = ShotIntent.Undefined;
            LastShotWasKillSmash = false;
        }

        public void SetPlayMode(BallPlayMode mode)
        {
            PlayMode = mode;
        }

        public void SetLastShotIntent(ShotIntent intent, bool isKillSmash = false)
        {
            LastShotIntent = intent;
            LastShotWasKillSmash = isKillSmash;
        }
    }
}
