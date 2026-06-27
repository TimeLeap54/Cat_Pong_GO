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
        [SerializeField] private BallPhysicsConfig ballPhysicsConfig;
        [SerializeField] private CourtGeometryConfig courtGeometryConfig;
        [SerializeField] private ShotExecutionController shotExecutionController;

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
            ShotBalanceConfig balanceConfig,
            BallPhysicsConfig physicsConfig = null,
            CourtGeometryConfig geometryConfig = null,
            ShotExecutionController executor = null)
        {
            hitDetector = detector;
            pointLoopBridge = pointLoop;
            shotConfig = balanceConfig;
            if (physicsConfig != null) ballPhysicsConfig = physicsConfig;
            if (geometryConfig != null) courtGeometryConfig = geometryConfig;
            if (executor != null) shotExecutionController = executor;
            RequireReferences();
            Subscribe();
        }

        private void HandleShotRequested(ShotRequest request)
        {
            HitContact contact = new HitContact(request.PointId, request.SwingId,
                request.BallStepIndex, request.Hitter, request.Intent,
                new Vector2(request.OriginX, request.OriginY), request.BallSnapshot,
                request.FacingDirection, request.InputTick);
            shotExecutionController.TryExecute(contact);
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
            if (hitDetector == null || pointLoopBridge == null || shotConfig == null ||
                ballPhysicsConfig == null || courtGeometryConfig == null)
            {
                throw new InvalidOperationException("PlayerShotEventBridge references are incomplete.");
            }

            shotConfig.ValidateOrThrow();
            if (shotExecutionController == null)
                throw new InvalidOperationException("ShotExecutionController is required.");
        }
    }
}
