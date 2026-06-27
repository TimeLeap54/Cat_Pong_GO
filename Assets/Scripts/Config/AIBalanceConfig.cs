using System;
using UnityEngine;

namespace CatTennis.Rebuild.Config
{
    [CreateAssetMenu(fileName = "AIBalanceConfig", menuName = "Cat Tennis/AI Balance")]
    public sealed class AIBalanceConfig : ScriptableObject
    {
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float deceleration = 16f;
        [SerializeField] private float reactionDelay = 0.16f;
        [SerializeField] private float predictionStep = 0.02f;
        [SerializeField] private float predictionHorizon = 2.5f;
        [SerializeField] private float courtMinX = 0.65f;
        [SerializeField] private float courtMaxX = 7.8f;
        [SerializeField] private float homeX = 5f;
        [SerializeField] private float swingLeadTime = 0.14f;
        [SerializeField] private float jumpLeadTime = 0.35f;
        [SerializeField] private float jumpHeightThreshold = 2.1f;
        [SerializeField] private float jumpSpeed = 7f;
        [SerializeField] private float safeWeight = 55f;
        [SerializeField] private float deepWeight = 20f;
        [SerializeField] private float dropWeight = 10f;
        [SerializeField] private float lobWeight = 15f;

        public float MoveSpeed => moveSpeed; public float Acceleration => acceleration;
        public float Deceleration => deceleration; public float ReactionDelay => reactionDelay;
        public float PredictionStep => predictionStep; public float PredictionHorizon => predictionHorizon;
        public float CourtMinX => courtMinX; public float CourtMaxX => courtMaxX;
        public float HomeX => homeX; public float SwingLeadTime => swingLeadTime;
        public float JumpLeadTime => jumpLeadTime; public float JumpHeightThreshold => jumpHeightThreshold;
        public float JumpSpeed => jumpSpeed;
        public float SafeWeight=>safeWeight; public float DeepWeight=>deepWeight;
        public float DropWeight=>dropWeight; public float LobWeight=>lobWeight;

        public void Configure(float speed, float reaction, float safeHome,
            float safeShot=55f,float deepShot=20f,float dropShot=10f,float lobShot=15f)
        { moveSpeed=speed;reactionDelay=reaction;homeX=safeHome;safeWeight=safeShot;
          deepWeight=deepShot;dropWeight=dropShot;lobWeight=lobShot; }
        public void ValidateOrThrow()
        {
            if (moveSpeed <= 0f || acceleration <= 0f || deceleration <= 0f || reactionDelay < 0f ||
                predictionStep <= 0f || predictionHorizon <= predictionStep || courtMinX >= courtMaxX ||
                homeX < courtMinX || homeX > courtMaxX || swingLeadTime <= 0f || jumpSpeed <= 0f)
                throw new InvalidOperationException("AI balance configuration is invalid.");
            if(safeWeight<0f||deepWeight<0f||dropWeight<0f||lobWeight<0f||
               safeWeight+deepWeight+dropWeight+lobWeight<=0f)
                throw new InvalidOperationException("AI shot weights are invalid.");
        }
        private void OnValidate() { try { ValidateOrThrow(); } catch (Exception e) { Debug.LogError(e.Message, this); } }
    }
}
