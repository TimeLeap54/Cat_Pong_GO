using System;
using UnityEngine;

namespace CatTennis.Rebuild.Config
{
    [CreateAssetMenu(fileName = "MovementBalanceConfig", menuName = "Cat Tennis/Balance/Movement Balance")]
    public sealed class MovementBalanceConfig : ScriptableObject
    {
        [SerializeField] private float playerMoveSpeedMultiplier = 0.9f;
        [SerializeField] private float jumpVelocityMultiplier = 1.12f;
        [SerializeField] private float ballHorizontalSpeedMultiplier = 0.9f;
        [SerializeField] private float ballVerticalSpeedMultiplier = 0.95f;

        public float PlayerMoveSpeedMultiplier => playerMoveSpeedMultiplier;
        public float JumpVelocityMultiplier => jumpVelocityMultiplier;
        public float BallHorizontalSpeedMultiplier => ballHorizontalSpeedMultiplier;
        public float BallVerticalSpeedMultiplier => ballVerticalSpeedMultiplier;

        public static float PlayerMoveSpeedMultiplierOrDefault(MovementBalanceConfig config) =>
            config == null ? 1f : config.PlayerMoveSpeedMultiplier;

        public static float JumpVelocityMultiplierOrDefault(MovementBalanceConfig config) =>
            config == null ? 1f : config.JumpVelocityMultiplier;

        public static float BallHorizontalSpeedMultiplierOrDefault(MovementBalanceConfig config) =>
            config == null ? 1f : config.BallHorizontalSpeedMultiplier;

        public static float BallVerticalSpeedMultiplierOrDefault(MovementBalanceConfig config) =>
            config == null ? 1f : config.BallVerticalSpeedMultiplier;

        public void ValidateOrThrow()
        {
            if (!IsValidMultiplier(playerMoveSpeedMultiplier) ||
                !IsValidMultiplier(jumpVelocityMultiplier) ||
                !IsValidMultiplier(ballHorizontalSpeedMultiplier) ||
                !IsValidMultiplier(ballVerticalSpeedMultiplier))
            {
                throw new InvalidOperationException("Movement balance multipliers must be finite and greater than zero.");
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
                Debug.LogError($"MovementBalanceConfig '{name}' is invalid: {exception.Message}", this);
            }
        }

        private static bool IsValidMultiplier(float value) =>
            value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
