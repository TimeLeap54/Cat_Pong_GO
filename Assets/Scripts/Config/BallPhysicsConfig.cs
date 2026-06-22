using CatTennis.BallPhysics.Core;
using UnityEngine;

namespace CatTennis.Rebuild.Config
{
    /// <summary>Unity authoring adapter for deterministic ball physics settings.</summary>
    [CreateAssetMenu(fileName = "BallPhysicsConfig", menuName = "Cat Tennis/Physics/Ball Physics Config")]
    public sealed class BallPhysicsConfig : ScriptableObject
    {
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float ballRadius = 0.15f;
        [SerializeField] private float groundRestitution = 0.7f;
        [SerializeField] private float horizontalBounceRetention = 1f;
        [SerializeField] private float maxHorizontalSpeed = 30f;
        [SerializeField] private float maxRiseSpeed = 30f;
        [SerializeField] private float maxFallSpeed = 30f;
        [SerializeField] private float minBounceSpeed = 0.1f;
        [SerializeField] private float groundSkin = 0.01f;

        public BallPhysicsSettings CreateSettings()
        {
            return new BallPhysicsSettings(
                gravity,
                ballRadius,
                groundRestitution,
                horizontalBounceRetention,
                maxHorizontalSpeed,
                maxRiseSpeed,
                maxFallSpeed,
                minBounceSpeed,
                groundSkin);
        }

        private void OnValidate()
        {
            string warning = GetValidationWarning();
            if (!string.IsNullOrEmpty(warning))
            {
                Debug.LogWarning($"BallPhysicsConfig '{name}' is invalid: {warning}", this);
            }
        }

        private string GetValidationWarning()
        {
            if (!AllFinite())
            {
                return "all values must be finite";
            }

            if (gravity >= 0f)
            {
                return "gravity must be negative";
            }

            if (ballRadius <= 0f)
            {
                return "ball radius must be positive";
            }

            if (groundRestitution < 0f || groundRestitution > 1f ||
                horizontalBounceRetention < 0f || horizontalBounceRetention > 1f)
            {
                return "retention values must be between zero and one";
            }

            if (maxHorizontalSpeed <= 0f || maxRiseSpeed <= 0f || maxFallSpeed <= 0f)
            {
                return "speed limits must be positive";
            }

            if (minBounceSpeed < 0f)
            {
                return "minimum bounce speed cannot be negative";
            }

            return groundSkin < 0f || groundSkin >= ballRadius
                ? "ground skin must be non-negative and smaller than ball radius"
                : string.Empty;
        }

        private bool AllFinite()
        {
            return IsFinite(gravity) &&
                   IsFinite(ballRadius) &&
                   IsFinite(groundRestitution) &&
                   IsFinite(horizontalBounceRetention) &&
                   IsFinite(maxHorizontalSpeed) &&
                   IsFinite(maxRiseSpeed) &&
                   IsFinite(maxFallSpeed) &&
                   IsFinite(minBounceSpeed) &&
                   IsFinite(groundSkin);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
