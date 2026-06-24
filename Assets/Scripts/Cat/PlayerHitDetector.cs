using System;
using CatTennis.BallPhysics.Core;
using CatTennis.Rebuild.Ball;
using CatTennis.Rebuild.Config;
using CatTennis.Rebuild.Shot;
using CatTennis.Rebuild.State;
using UnityEngine;

namespace CatTennis.Rebuild.Cat
{
    /// <summary>Creates one deterministic request from one active swing and explicit zone test.</summary>
    public sealed class PlayerHitDetector : MonoBehaviour
    {
        [SerializeField] private BallController ballController;
        [SerializeField] private PlayerControlConfig config;

        private readonly PlayerHitZoneModel zoneModel = new PlayerHitZoneModel();
        private long consumedSwingId;

        public event Action<ShotRequest> OnShotRequested;

        public void Initialize(BallController ball, PlayerControlConfig playerConfig)
        {
            ballController = ball;
            config = playerConfig;
            config?.ValidateOrThrow();
            consumedSwingId = 0;
        }

        public bool Evaluate(
            PlayerActionFrame action,
            Vector2 playerPosition,
            int facingDirection)
        {
            if (!action.IsHitActive || action.SwingId <= 0 ||
                action.SwingId == consumedSwingId || ballController == null || config == null)
            {
                return false;
            }

            BallSnapshot ball = ballController.CurrentSnapshot;
            if (!ball.IsActive)
            {
                return false;
            }

            HitZoneDefinition zone = action.SwingKind == SwingKind.Smash
                ? config.CreateSmashHitZone()
                : config.CreateNormalHitZone();
            if (!zoneModel.Contains(
                    zone,
                    playerPosition.x,
                    playerPosition.y,
                    facingDirection,
                    ball.PositionX,
                    ball.PositionY))
            {
                return false;
            }

            ShotIntent intent = action.SwingKind == SwingKind.Smash
                ? ShotIntent.Smash
                : ShotIntent.SafeReturn;
            ShotRequest request = new ShotRequest(
                action.SwingId,
                ball.StepIndex,
                HitterType.Player,
                intent,
                ball,
                playerPosition.x,
                playerPosition.y,
                ball.PositionX,
                ball.PositionY,
                facingDirection);

            consumedSwingId = action.SwingId;
            OnShotRequested?.Invoke(request);
            return true;
        }
    }
}
