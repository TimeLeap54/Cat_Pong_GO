using System.Collections.Generic;
using CatTennis.Rebuild.Audio;
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
        [SerializeField] private RallyAiBalanceConfig rallyAiConfig;
        private readonly ShotModel model = new ShotModel();
        private readonly HashSet<ulong> consumedSwingIds = new HashSet<ulong>();
        private int playerSmashCount;
        private int opponentSmashCount;

        public void Configure(PointLoopEventBridge bridge, ShotBalanceConfig shots,
            BallPhysicsConfig physics, CourtGeometryConfig court, RallyAiBalanceConfig rallyAi = null)
        { pointBridge = bridge; shotConfig = shots; physicsConfig = physics; courtConfig = court; rallyAiConfig = rallyAi; }

        public bool TryExecute(HitContact contact)
        {
            ulong key = ((ulong)contact.Hitter << 60) | ((ulong)contact.SwingId & 0x0fffffffffffffffUL);
            if (contact.PointId != pointBridge.CurrentPointId || consumedSwingIds.Contains(key)) return false;

            bool isKillSmash = false;
            if (contact.Intent == ShotIntent.Smash)
            {
                if (contact.Hitter == HitterType.Player) isKillSmash = playerSmashCount >= 2;
                else if (contact.Hitter == HitterType.Opponent) isKillSmash = opponentSmashCount >= 2;
            }

            ShotRequest baseRequest = contact.ToShotRequest();
            ShotRequest request = new ShotRequest(
                baseRequest.IntentSnapshot,
                baseRequest.BallStepIndex,
                baseRequest.Hitter,
                baseRequest.BallSnapshot,
                baseRequest.OriginX,
                baseRequest.OriginY,
                baseRequest.IsServeToss,
                baseRequest.IsCounteringSmash,
                baseRequest.HitHeightRatio,
                isKillSmash
            );

            ShotResult result = model.Resolve(request,
                shotConfig.CreateSettings(physicsConfig, courtConfig),
                pointBridge.RallyHitCount, rallyAiConfig, pointBridge.IsRallyMode, pointBridge.IsVolley);
            if (!result.IsValid) return false;
            bool launched = pointBridge.TrySubmitHit(contact.Hitter, contact.BallStepIndex,
                new Vector2(result.VelocityX, result.VelocityY));
            if (launched)
            {
                consumedSwingIds.Add(key);
                if (pointBridge.Ball != null)
                {
                    pointBridge.Ball.SetLastShotIntent(contact.Intent, isKillSmash);
                }

                AudioEventRouter.NotifyShotExecuted(contact.Intent);

                if (contact.Intent == ShotIntent.Smash)
                {
                    if (contact.Hitter == HitterType.Player) playerSmashCount++;
                    else if (contact.Hitter == HitterType.Opponent) opponentSmashCount++;

                    int currentCount = contact.Hitter == HitterType.Player ? playerSmashCount : opponentSmashCount;
                    bool fellBack = isKillSmash && result.FlightTime > 0.50f;
                    Debug.Log($"[Smash System] Hitter: {contact.Hitter}, Consecutive Smash Count: {currentCount}, IsKillSmash: {isKillSmash}, FellBackToNormal: {fellBack}");
                }
                else
                {
                    if (contact.Hitter == HitterType.Player) playerSmashCount = 0;
                    else if (contact.Hitter == HitterType.Opponent) opponentSmashCount = 0;
                }
            }
            return launched;
        }

        public void ResetPoint()
        {
            consumedSwingIds.Clear();
            playerSmashCount = 0;
            opponentSmashCount = 0;
        }
    }
}
