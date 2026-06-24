using System;
using CatTennis.Rebuild.State;
using UnityEngine;

namespace CatTennis.Rebuild.Config
{
    /// <summary>Fixed Phase 3 harness values; no trajectory calculations belong here.</summary>
    [CreateAssetMenu(fileName = "Phase3PointLoopConfig", menuName = "Cat Tennis/Phase 3/Point Loop")]
    public sealed class Phase3PointLoopConfig : ScriptableObject
    {
        [SerializeField] private int targetScore = 5;
        [SerializeField] private float resetDelay = 0.5f;
        [SerializeField] private HitterType server = HitterType.Player;
        [SerializeField] private Vector2 resetPosition = new Vector2(-4f, 1f);
        [SerializeField] private Vector2 fixedTestServeVelocity = new Vector2(8f, 6f);
        [SerializeField] private Vector2 playerResetPosition = new Vector2(-4f, 0.75f);

        public int TargetScore => targetScore;
        public float ResetDelay => resetDelay;
        public HitterType Server => server;
        public Vector2 ResetPosition => resetPosition;
        public Vector2 FixedTestServeVelocity => fixedTestServeVelocity;
        public Vector2 PlayerResetPosition => playerResetPosition;

        public void Configure(
            int newTargetScore,
            float newResetDelay,
            HitterType newServer,
            Vector2 newResetPosition,
            Vector2 newFixedTestServeVelocity)
        {
            targetScore = newTargetScore;
            resetDelay = newResetDelay;
            server = newServer;
            resetPosition = newResetPosition;
            fixedTestServeVelocity = newFixedTestServeVelocity;
        }

        public void SetPlayerResetPosition(Vector2 position)
        {
            playerResetPosition = position;
        }

        public void ValidateOrThrow()
        {
            if (targetScore <= 0 || resetDelay < 0f || !IsFinite(resetDelay))
            {
                throw new InvalidOperationException("Target score and reset delay are invalid.");
            }

            if (server != HitterType.Player && server != HitterType.Opponent)
            {
                throw new InvalidOperationException("A valid Phase 3 server is required.");
            }

            if (!IsFinite(resetPosition.x) || !IsFinite(resetPosition.y) ||
                !IsFinite(fixedTestServeVelocity.x) || !IsFinite(fixedTestServeVelocity.y))
            {
                throw new InvalidOperationException("Point loop vectors must be finite.");
            }


            if (!IsFinite(playerResetPosition.x) || !IsFinite(playerResetPosition.y))
            {
                throw new InvalidOperationException("Player reset position must be finite.");
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
                Debug.LogError($"Phase3PointLoopConfig '{name}' is invalid: {exception.Message}", this);
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
