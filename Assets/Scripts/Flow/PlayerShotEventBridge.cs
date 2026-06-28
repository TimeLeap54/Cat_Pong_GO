using System;
using CatTennis.Rebuild.Cat;
using CatTennis.Rebuild.Shot;
using CatTennis.Rebuild.State;
using UnityEngine;

namespace CatTennis.Rebuild.Flow
{
    /// <summary>Resolves valid requests and forwards authored velocity to point-loop wiring.</summary>
    public sealed class PlayerShotEventBridge : MonoBehaviour
    {
        [SerializeField] private PlayerHitDetector hitDetector;
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
            ShotExecutionController executor)
        {
            hitDetector = detector;
            shotExecutionController = executor;
            RequireReferences();
            Subscribe();
        }

        private void HandleShotRequested(ShotRequest request)
        {
            HitContact contact = new HitContact(request.PointId, request.SwingId,
                request.BallStepIndex, request.Hitter, request.Intent,
                new Vector2(request.OriginX, request.OriginY), request.BallSnapshot,
                request.FacingDirection, request.InputTick,
                request.IsServeToss, request.IsCounteringSmash, request.HitHeightRatio,
                request.IsCounteringKillSmash);
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
            if (hitDetector == null || shotExecutionController == null)
            {
                throw new InvalidOperationException("PlayerShotEventBridge references are incomplete.");
            }
        }
    }
}
