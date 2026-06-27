using CatTennis.Rebuild.Ball;
using CatTennis.Rebuild.Cat;
using CatTennis.Rebuild.Config;
using CatTennis.Rebuild.State;
using UnityEngine;

namespace CatTennis.Rebuild.Flow
{
    public sealed class ServeFlowController : MonoBehaviour
    {
        [SerializeField] private BallController ball;
        [SerializeField] private PlayerCatController player;
        [SerializeField] private PlayerHitDetector hitDetector;
        [SerializeField] private ShotBalanceConfig shotConfig;

        private Vector2 resetPosition;
        private long lastHandledSwing;
        private bool subscribed;
        private float startupDelayRemaining = 0.5f;

        public ServeFlowState State { get; private set; } = ServeFlowState.WaitingForToss;

        public void Configure(BallController ballController, PlayerCatController playerController,
            PlayerHitDetector detector, ShotBalanceConfig shots)
        {
            ball = ballController; player = playerController; hitDetector = detector;
            shotConfig = shots;
            Subscribe();
        }

        public void BeginServe(Vector2 ballResetPosition)
        {
            enabled = true;
            resetPosition = ballResetPosition;
            lastHandledSwing = 0;
            State = ServeFlowState.WaitingForToss;
            hitDetector.RallyHitsEnabled = false;
            ball.ResetBall(new Vector2(0f, -100f));
            ball.SetPlayMode(BallPlayMode.Inactive);
            startupDelayRemaining = 0.5f;
        }

        private void OnEnable() => Subscribe();
        private void OnDisable()
        {
            if (subscribed && player != null) player.OnActionApplied -= HandleAction;
            subscribed = false;
        }

        private void FixedUpdate()
        {
            if (startupDelayRemaining > 0f)
            {
                startupDelayRemaining -= Time.fixedDeltaTime;
            }

            if (State == ServeFlowState.TossInFlight)
            {
                if (ball.PlayMode == BallPlayMode.Rally)
                {
                    State = ServeFlowState.ServeLaunched;
                    hitDetector.RallyHitsEnabled = true;
                }
            }
            else if (State == ServeFlowState.ServeLaunched &&
                     player.CurrentAction.SwingState == SwingState.Ready)
            {
                hitDetector.RallyHitsEnabled = true;
            }
        }

        private void HandleAction(PlayerActionFrame action, Vector2 playerPosition)
        {
            if (startupDelayRemaining > 0f)
            {
                return;
            }

            bool startup = action.SwingState == SwingState.NormalStartup ||
                           action.SwingState == SwingState.SmashStartup;
            
            if (!startup || action.SwingId == lastHandledSwing)
            {
                return;
            }

            lastHandledSwing = action.SwingId;

            if (State == ServeFlowState.WaitingForToss)
            {
                if (action.SwingKind != SwingKind.Normal) return; 
                
                hitDetector.ConsumeSwing(action.SwingId);
                Vector2 tossPosition = playerPosition + new Vector2(
                    shotConfig.TossOffset.x * player.FacingDirection, shotConfig.TossOffset.y);
                ball.ResetBall(tossPosition);
                ball.SetPlayMode(BallPlayMode.ServeToss);
                ball.Launch(new Vector2(0f, shotConfig.TossSpeed));
                State = ServeFlowState.TossInFlight;
            }
        }

        private void Subscribe()
        {
            if (subscribed || player == null) return;
            player.OnActionApplied += HandleAction;
            subscribed = true;
        }
    }
}
