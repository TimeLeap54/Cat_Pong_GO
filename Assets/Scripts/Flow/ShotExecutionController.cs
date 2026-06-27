using System.Collections.Generic;
using CatTennis.Rebuild.Cat;
using CatTennis.Rebuild.Config;
using CatTennis.Rebuild.Shot;
using CatTennis.Rebuild.State;
using UnityEngine;

namespace CatTennis.Rebuild.Flow
{
    public sealed class ShotExecutionController : MonoBehaviour
    {
        [SerializeField] private PointLoopEventBridge pointBridge;
        [SerializeField] private ShotBalanceConfig shotConfig;
        [SerializeField] private BallPhysicsConfig physicsConfig;
        [SerializeField] private CourtGeometryConfig courtConfig;
        private readonly ShotModel model = new ShotModel();
        private readonly HashSet<ulong> consumedSwingIds = new HashSet<ulong>();

        public void Configure(PointLoopEventBridge bridge, ShotBalanceConfig shots,
            BallPhysicsConfig physics, CourtGeometryConfig court)
        { pointBridge = bridge; shotConfig = shots; physicsConfig = physics; courtConfig = court; }

        public bool TryExecute(HitContact contact)
        {
            ulong key = ((ulong)contact.Hitter << 60) | ((ulong)contact.SwingId & 0x0fffffffffffffffUL);
            if (contact.PointId != pointBridge.CurrentPointId || consumedSwingIds.Contains(key)) return false;
            ShotResult result = model.Resolve(contact.ToShotRequest(),
                shotConfig.CreateSettings(physicsConfig, courtConfig));
            if (!result.IsValid) return false;
            bool launched = pointBridge.TrySubmitHit(contact.Hitter, contact.BallStepIndex,
                new Vector2(result.VelocityX, result.VelocityY));
            if (launched)
            {
                consumedSwingIds.Add(key);
                if (pointBridge.Ball != null)
                {
                    pointBridge.Ball.SetLastShotIntent(contact.Intent);
                }
            }
            return launched;
        }

        public void ResetPoint() => consumedSwingIds.Clear();
    }
}
