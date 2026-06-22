using CatTennis.BallPhysics.Core;
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
        }

        public void Launch(Vector2 velocity)
        {
            Applier.Launch(velocity);
        }

        public void StopBall()
        {
            Applier.StopBall();
        }
    }
}
