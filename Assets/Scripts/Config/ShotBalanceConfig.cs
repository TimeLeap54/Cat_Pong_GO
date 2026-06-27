using System;
using CatTennis.BallPhysics.Core;
using CatTennis.Rebuild.Shot;
using UnityEngine;

namespace CatTennis.Rebuild.Config
{
    [CreateAssetMenu(fileName = "ShotBalanceConfig", menuName = "Cat Tennis/Shot Balance")]
    public sealed class ShotBalanceConfig : ScriptableObject
    {
        [Header("Input")]
        [SerializeField] private float aimDeadZone = 0.25f;
        [SerializeField] private float verticalPriorityThreshold = 0.4f;
        [Header("Court")]
        [SerializeField] private float netHeight = 1f;
        [SerializeField] private float clearance = 0.12f;
        [Header("Profiles: landing ratio, apex height, flight time")]
        [SerializeField] private Vector3 safe = new Vector3(0.55f, 2.3f, 0.9f);
        [SerializeField] private Vector3 deep = new Vector3(0.88f, 2.1f, 0.85f);
        [SerializeField] private Vector3 drop = new Vector3(0.12f, 1.35f, 0.75f);
        [SerializeField] private Vector3 lob = new Vector3(0.72f, 4.2f, 1.2f);
        [SerializeField] private Vector3 smash = new Vector3(0.72f, 0.2f, 0.62f);
        [SerializeField] private Vector3 serve = new Vector3(0.72f, 2.5f, 0.95f);
        [Header("Serve")]
        [SerializeField] private Vector2 tossOffset = new Vector2(0.35f, 0.75f);
        [SerializeField] private float tossSpeed = 5.2f;

        public float AimDeadZone => aimDeadZone;
        public float VerticalPriorityThreshold => verticalPriorityThreshold;
        public Vector2 TossOffset => tossOffset;
        public float TossSpeed => tossSpeed;

        public ShotSettings CreateSettings(BallPhysicsConfig physics, CourtGeometryConfig court)
        {
            if (physics == null || court == null) throw new InvalidOperationException("Physics and court configs are required.");
            BallPhysicsSettings ball = physics.CreateSettings();
            court.ValidateOrThrow();
            float netX = (court.PlayerCourtMaxX + court.OpponentCourtMinX) * 0.5f;
            return new ShotSettings(ball.Gravity, court.GroundY + ball.BallRadius,
                court.PlayerCourtMinX, court.PlayerCourtMaxX, court.OpponentCourtMinX,
                court.OpponentCourtMaxX, netX, netHeight, clearance,
                ball.MaxHorizontalSpeed, ball.MaxRiseSpeed, ball.MaxFallSpeed,
                aimDeadZone, verticalPriorityThreshold, Profile(safe), Profile(deep),
                Profile(drop), Profile(lob), Profile(smash), Profile(serve), tossSpeed);
        }

        public void ValidateOrThrow()
        {
            if (aimDeadZone < 0f || verticalPriorityThreshold < 0f || netHeight <= 0f ||
                clearance < 0f || tossSpeed <= 0f)
                throw new InvalidOperationException("Shot balance values are invalid.");
            ValidateProfile(safe); ValidateProfile(deep); ValidateProfile(drop);
            ValidateProfile(lob); ValidateProfile(smash); ValidateProfile(serve);
        }

        private static ShotProfileSettings Profile(Vector3 value) =>
            new ShotProfileSettings(value.x, value.y, value.z);
        private static void ValidateProfile(Vector3 value)
        {
            if (value.x < 0f || value.x > 1f || value.y <= 0f || value.z <= 0f)
                throw new InvalidOperationException("Shot profile is invalid.");
        }
        private void OnValidate() { try { ValidateOrThrow(); } catch (Exception e) { Debug.LogError(e.Message, this); } }
    }
}
