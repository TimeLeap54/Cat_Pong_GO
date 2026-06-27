using CatTennis.Rebuild.Ball;
using CatTennis.Rebuild.Cat;
using CatTennis.Rebuild.Config;
using CatTennis.Rebuild.State;
using UnityEngine;

namespace CatTennis.Rebuild.Flow
{
    public sealed class OpponentServeFlowController : MonoBehaviour
    {
        [SerializeField] private BallController ball;
        [SerializeField] private OpponentAIController opponent;
        [SerializeField] private ShotBalanceConfig shotConfig;

        private float resetDelayRemaining;
        private bool isWaitingForToss;
        private Vector2 resetPosition;

        public bool IsWaitingForToss => isWaitingForToss;

        public void Configure(BallController ballController, OpponentAIController opponentController, ShotBalanceConfig shots)
        {
            ball = ballController;
            opponent = opponentController;
            shotConfig = shots;
        }

        public void BeginServe(Vector2 ballResetPosition)
        {
            resetPosition = ballResetPosition;
            resetDelayRemaining = 0.6f; // 0.6초 대기 후 토스
            isWaitingForToss = true;
            ball.ResetBall(new Vector2(0f, -100f));
            ball.SetPlayMode(BallPlayMode.Inactive);
        }

        private void FixedUpdate()
        {
            if (!isWaitingForToss) return;

            resetDelayRemaining -= Time.fixedDeltaTime;
            if (resetDelayRemaining <= 0f)
            {
                isWaitingForToss = false;

                // AI의 서브 시작 위치 기준 기획자가 비주얼에 맞춘 TossOffset 에셋 값을 적용 (AI 방향 -1 적용)
                Vector2 tossPos = resetPosition + new Vector2(
                    shotConfig.TossOffset.x * -1f,
                    shotConfig.TossOffset.y
                );
                ball.ResetBall(tossPos);
                ball.SetPlayMode(BallPlayMode.ServeToss);
                ball.Launch(new Vector2(0f, shotConfig.TossSpeed)); // 수직 토스 속도
            }
        }
    }
}
