using System;
using UnityEngine;

namespace CatTennis.Rebuild.Config
{
    /// <summary>Authoritative Phase 3 court and world ranges.</summary>
    [CreateAssetMenu(fileName = "CourtGeometryConfig", menuName = "Cat Tennis/Phase 3/Court Geometry")]
    public sealed class CourtGeometryConfig : ScriptableObject
    {
        [SerializeField] private float worldMinX = -10f;
        [SerializeField] private float worldMaxX = 10f;
        [SerializeField] private float groundY;
        [SerializeField] private float playerCourtMinX = -8f;
        [SerializeField] private float playerCourtMaxX = -0.5f;
        [SerializeField] private float opponentCourtMinX = 0.5f;
        [SerializeField] private float opponentCourtMaxX = 8f;
        [SerializeField] private float lineTolerance = 0.01f;
        [SerializeField] private bool useKillPlane = true;
        [SerializeField] private float killPlaneY = -5f;

        public float WorldMinX => worldMinX;
        public float WorldMaxX => worldMaxX;
        public float GroundY => groundY;
        public float PlayerCourtMinX => playerCourtMinX;
        public float PlayerCourtMaxX => playerCourtMaxX;
        public float OpponentCourtMinX => opponentCourtMinX;
        public float OpponentCourtMaxX => opponentCourtMaxX;
        public float LineTolerance => lineTolerance;
        public bool UseKillPlane => useKillPlane;
        public float KillPlaneY => killPlaneY;

        public void Configure(
            float newWorldMinX,
            float newWorldMaxX,
            float newPlayerCourtMinX,
            float newPlayerCourtMaxX,
            float newOpponentCourtMinX,
            float newOpponentCourtMaxX,
            float newLineTolerance,
            bool newUseKillPlane = true,
            float newKillPlaneY = -5f,
            float newGroundY = 0f)
        {
            worldMinX = newWorldMinX;
            worldMaxX = newWorldMaxX;
            playerCourtMinX = newPlayerCourtMinX;
            playerCourtMaxX = newPlayerCourtMaxX;
            opponentCourtMinX = newOpponentCourtMinX;
            opponentCourtMaxX = newOpponentCourtMaxX;
            lineTolerance = newLineTolerance;
            useKillPlane = newUseKillPlane;
            killPlaneY = newKillPlaneY;
            groundY = newGroundY;
        }

        public void ValidateOrThrow()
        {
            if (!AllFinite())
            {
                throw new InvalidOperationException("Court geometry values must be finite.");
            }

            if (lineTolerance < 0f || worldMinX >= worldMaxX)
            {
                throw new InvalidOperationException("Court tolerance and world range are invalid.");
            }

            if (playerCourtMinX > playerCourtMaxX ||
                opponentCourtMinX > opponentCourtMaxX)
            {
                throw new InvalidOperationException("Court minimums must not exceed maximums.");
            }

            if (playerCourtMinX < worldMinX || opponentCourtMaxX > worldMaxX)
            {
                throw new InvalidOperationException("Court ranges must remain inside world bounds.");
            }

            if (playerCourtMaxX + lineTolerance >= opponentCourtMinX - lineTolerance)
            {
                throw new InvalidOperationException(
                    "Tolerant court ranges must not overlap; the neutral gap is required.");
            }
        }

        private void OnValidate()
        {
            try
            {
                ValidateOrThrow();
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError($"CourtGeometryConfig '{name}' is invalid: {exception.Message}", this);
            }
        }

        private bool AllFinite()
        {
            return IsFinite(worldMinX) && IsFinite(worldMaxX) &&
                   IsFinite(playerCourtMinX) && IsFinite(playerCourtMaxX) &&
                   IsFinite(opponentCourtMinX) && IsFinite(opponentCourtMaxX) &&
                   IsFinite(lineTolerance) && IsFinite(killPlaneY) && IsFinite(groundY);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
