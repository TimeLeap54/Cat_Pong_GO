using System;
using CatTennis.Rebuild.Cat;
using UnityEngine;

namespace CatTennis.Rebuild.Config
{
    [CreateAssetMenu(fileName = "PlayerControlConfig", menuName = "Cat Tennis/Phase 4/Player Control")]
    public sealed class PlayerControlConfig : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpSpeed = 7f;
        [SerializeField] private float gravityScale = 3f;
        [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.55f);
        [SerializeField] private float groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask groundMask = 1 << 10;
        [SerializeField] private int playerBodyLayer = 8;
        [SerializeField] private int ballLayer = 9;

        [Header("Normal Swing Ticks")]
        [SerializeField] private int normalStartupTicks = 3;
        [SerializeField] private int normalActiveTicks = 4;
        [SerializeField] private int normalRecoveryTicks = 6;

        [Header("Smash Ticks")]
        [SerializeField] private int smashStartupTicks = 4;
        [SerializeField] private int smashActiveTicks = 4;
        [SerializeField] private int smashRecoveryTicks = 8;

        [Header("Hit Zones")]
        [SerializeField] private Vector2 normalZoneCenter = new Vector2(0.8f, 0.8f);
        [SerializeField] private Vector2 normalZoneHalfSize = new Vector2(1.1f, 1.1f);
        [SerializeField] private Vector2 smashZoneCenter = new Vector2(0.25f, 1.25f);
        [SerializeField] private Vector2 smashZoneHalfSize = new Vector2(1.5f, 1.5f);

        public float MoveSpeed => moveSpeed;
        public float JumpSpeed => jumpSpeed;
        public float GravityScale => gravityScale;
        public Vector2 GroundCheckOffset => groundCheckOffset;
        public float GroundCheckRadius => groundCheckRadius;
        public LayerMask GroundMask => groundMask;
        public int PlayerBodyLayer => playerBodyLayer;
        public int BallLayer => ballLayer;

        public PlayerActionSettings CreateActionSettings()
        {
            return new PlayerActionSettings(
                normalStartupTicks, normalActiveTicks, normalRecoveryTicks,
                smashStartupTicks, smashActiveTicks, smashRecoveryTicks);
        }

        public HitZoneDefinition CreateNormalHitZone()
        {
            return new HitZoneDefinition(
                normalZoneCenter.x, normalZoneCenter.y,
                normalZoneHalfSize.x, normalZoneHalfSize.y, true);
        }

        public HitZoneDefinition CreateSmashHitZone()
        {
            return new HitZoneDefinition(
                smashZoneCenter.x, smashZoneCenter.y,
                smashZoneHalfSize.x, smashZoneHalfSize.y, false);
        }

        public void Configure(
            float newMoveSpeed,
            float newJumpSpeed,
            float newGravityScale,
            Vector2 newGroundOffset,
            float newGroundRadius,
            LayerMask newGroundMask,
            int newPlayerLayer,
            int newBallLayer)
        {
            moveSpeed = newMoveSpeed;
            jumpSpeed = newJumpSpeed;
            gravityScale = newGravityScale;
            groundCheckOffset = newGroundOffset;
            groundCheckRadius = newGroundRadius;
            groundMask = newGroundMask;
            playerBodyLayer = newPlayerLayer;
            ballLayer = newBallLayer;
        }

        public void ValidateOrThrow()
        {
            CreateActionSettings().Validate();
            CreateNormalHitZone();
            CreateSmashHitZone();
            if (!IsFinite(moveSpeed) || !IsFinite(jumpSpeed) || !IsFinite(gravityScale) ||
                !IsFinite(groundCheckOffset.x) || !IsFinite(groundCheckOffset.y) ||
                !IsFinite(groundCheckRadius) || moveSpeed <= 0f || jumpSpeed <= 0f ||
                gravityScale <= 0f || groundCheckRadius <= 0f ||
                playerBodyLayer < 0 || playerBodyLayer > 31 ||
                ballLayer < 0 || ballLayer > 31 || playerBodyLayer == ballLayer)
            {
                throw new InvalidOperationException("Player control configuration is invalid.");
            }
        }

        private void OnValidate()
        {
            try
            {
                ValidateOrThrow();
            }
            catch (Exception exception)
            {
                Debug.LogError($"PlayerControlConfig '{name}' is invalid: {exception.Message}", this);
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
