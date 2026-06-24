using System;
using CatTennis.Rebuild.Cat;
using CatTennis.Rebuild.Config;
using CatTennis.Rebuild.Shot;
using CatTennis.Rebuild.State;
using UnityEngine;

namespace CatTennis.Rebuild.Flow
{
    /// <summary>Resolves valid requests and forwards authored velocity to point-loop wiring.</summary>
    public sealed class PlayerShotEventBridge : MonoBehaviour
    {
        [SerializeField] private PlayerHitDetector hitDetector;
        [SerializeField] private PointLoopEventBridge pointLoopBridge;
        [SerializeField] private ShotBalanceConfig shotConfig;

        private readonly ShotModel shotModel = new ShotModel();
        private bool subscribed;

        private void OnEnable()
        {
            RequireReferences();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(
            PlayerHitDetector detector,
            PointLoopEventBridge pointLoop,
            ShotBalanceConfig balanceConfig)
        {
            hitDetector = detector;
            pointLoopBridge = pointLoop;
            shotConfig = balanceConfig;
            RequireReferences();
            Subscribe();
        }

        private void HandleShotRequested(ShotRequest request)
        {
            ShotResult result = shotModel.Resolve(request, shotConfig.CreateSettings());
            pointLoopBridge.TrySubmitHit(
                request.Hitter,
                result.BallStepIndex,
                new Vector2(result.VelocityX, result.VelocityY));
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            hitDetector.OnShotRequested += HandleShotRequested;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (hitDetector != null)
            {
                hitDetector.OnShotRequested -= HandleShotRequested;
            }

            subscribed = false;
        }

        private void RequireReferences()
        {
            if (hitDetector == null || pointLoopBridge == null || shotConfig == null)
            {
                throw new InvalidOperationException("PlayerShotEventBridge references are incomplete.");
            }

            shotConfig.ValidateOrThrow();
        }
    }
}
