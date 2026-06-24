using System;
using System.Collections.Generic;
using CatTennis.Rebuild.Ball;
using CatTennis.Rebuild.Cat;
using CatTennis.Rebuild.Config;
using CatTennis.Rebuild.Rules;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CatTennis.Rebuild.Flow
{
    /// <summary>Reports scene composition problems without mutating the scene.</summary>
    public sealed class MatchSceneValidator : MonoBehaviour
    {
        [SerializeField] private BallPhysicsConfig ballPhysicsConfig;
        [SerializeField] private CourtGeometryConfig courtGeometryConfig;
        [SerializeField] private Phase3PointLoopConfig pointLoopConfig;
        [SerializeField] private PlayerControlConfig playerControlConfig;
        [SerializeField] private ShotBalanceConfig shotBalanceConfig;
        [SerializeField] private BallController ball;
        [SerializeField] private PlayerCatController player;
        [SerializeField] private Collider2D playerCollider;
        [SerializeField] private Collider2D groundCollider;

        public void Configure(
            BallPhysicsConfig ballPhysics,
            CourtGeometryConfig courtGeometry,
            Phase3PointLoopConfig pointLoop,
            PlayerControlConfig playerControl,
            ShotBalanceConfig shotBalance,
            BallController ballController,
            PlayerCatController playerController,
            Collider2D bodyCollider,
            Collider2D courtGroundCollider)
        {
            ballPhysicsConfig = ballPhysics;
            courtGeometryConfig = courtGeometry;
            pointLoopConfig = pointLoop;
            playerControlConfig = playerControl;
            shotBalanceConfig = shotBalance;
            ball = ballController;
            player = playerController;
            playerCollider = bodyCollider;
            groundCollider = courtGroundCollider;
        }

        public IReadOnlyList<string> ValidateScene()
        {
            List<string> errors = new List<string>();
            RequireSingle<MatchBootstrapper>(errors);
            RequireSingle<PointLifecycleController>(errors);
            RequireSingle<CourtZoneDetector>(errors);
            RequireSingle<RallyFlowManager>(errors);
            RequireSingle<MatchFlowManager>(errors);
            RequireSingle<ResetFlowController>(errors);
            RequireSingle<PointLoopEventBridge>(errors);
            RequireSingle<PlayerShotEventBridge>(errors);
            RequireSingle<BallController>(errors);
            RequireSingle<PlayerCatController>(errors);
            RequireSingle<Camera>(errors);
            RequireSingle<EventSystem>(errors);

            if (ballPhysicsConfig == null || courtGeometryConfig == null ||
                pointLoopConfig == null || playerControlConfig == null ||
                shotBalanceConfig == null || ball == null || player == null ||
                playerCollider == null || groundCollider == null)
            {
                errors.Add("Match validator references are incomplete.");
                return errors;
            }

            ValidateConfigs(errors);
            ValidatePhysics(errors);
            ValidateLayers(errors);
            ValidateGround(errors);
            return errors;
        }

        public void ValidateOrThrow()
        {
            IReadOnlyList<string> errors = ValidateScene();
            if (errors.Count > 0)
            {
                throw new InvalidOperationException("Match scene validation failed:\n- " +
                    string.Join("\n- ", errors));
            }
        }

        private void ValidateConfigs(List<string> errors)
        {
            TryValidate(ballPhysicsConfig.CreateSettings, errors, "BallPhysicsConfig");
            TryValidate(courtGeometryConfig.ValidateOrThrow, errors, "CourtGeometryConfig");
            TryValidate(pointLoopConfig.ValidateOrThrow, errors, "PointLoopConfig");
            TryValidate(playerControlConfig.ValidateOrThrow, errors, "PlayerControlConfig");
            TryValidate(shotBalanceConfig.ValidateOrThrow, errors, "ShotBalanceConfig");

            UnityEngine.Object[] configs = {
                ballPhysicsConfig, courtGeometryConfig, pointLoopConfig,
                playerControlConfig, shotBalanceConfig
            };
            foreach (UnityEngine.Object config in configs)
            {
                if (config.name.Contains("Phase3") || config.name.Contains("Phase4"))
                {
                    errors.Add($"Lab config is referenced by Match: {config.name}");
                }
            }
        }

        private void ValidatePhysics(List<string> errors)
        {
            Rigidbody2D ballBody = ball.GetComponent<Rigidbody2D>();
            Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
            if (ballBody == null || ballBody.bodyType != RigidbodyType2D.Kinematic ||
                !Mathf.Approximately(ballBody.gravityScale, 0f))
            {
                errors.Add("Ball must be Kinematic with gravityScale zero.");
            }

            if (playerBody == null || playerBody.bodyType != RigidbodyType2D.Dynamic ||
                (playerBody.constraints & RigidbodyConstraints2D.FreezeRotation) == 0)
            {
                errors.Add("Player must be Dynamic with rotation frozen.");
            }

            if ((playerCollider.excludeLayers.value & (1 << playerControlConfig.BallLayer)) == 0)
            {
                errors.Add("Player collider must exclude the TennisBall layer.");
            }
        }

        private void ValidateLayers(List<string> errors)
        {
            RequireLayerName(playerControlConfig.PlayerBodyLayer, "PlayerBody", errors);
            RequireLayerName(playerControlConfig.BallLayer, "TennisBall", errors);
            if (LayerMask.LayerToName(groundCollider.gameObject.layer) != "Ground")
            {
                errors.Add("Ground object must use the Ground layer.");
            }
        }

        private void ValidateGround(List<string> errors)
        {
            Physics2D.SyncTransforms();
            if (Mathf.Abs(groundCollider.bounds.max.y - courtGeometryConfig.GroundY) > 0.001f)
            {
                errors.Add("Ground collider top does not match CourtGeometryConfig.GroundY.");
            }
        }

        private static void RequireSingle<T>(List<string> errors) where T : UnityEngine.Object
        {
            int count = FindObjectsOfType<T>(true).Length;
            if (count != 1)
            {
                errors.Add($"Expected exactly one {typeof(T).Name}, found {count}.");
            }
        }

        private static void RequireLayerName(int layer, string expected, List<string> errors)
        {
            if (LayerMask.LayerToName(layer) != expected)
            {
                errors.Add($"Layer {layer} must be named {expected}.");
            }
        }

        private static void TryValidate(Action validation, List<string> errors, string label)
        {
            try
            {
                validation();
            }
            catch (Exception exception)
            {
                errors.Add($"{label}: {exception.Message}");
            }
        }

        private static void TryValidate<T>(Func<T> validation, List<string> errors, string label)
        {
            try
            {
                validation();
            }
            catch (Exception exception)
            {
                errors.Add($"{label}: {exception.Message}");
            }
        }
    }
}
