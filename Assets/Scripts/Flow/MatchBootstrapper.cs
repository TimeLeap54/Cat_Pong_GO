using System;
using CatTennis.Rebuild.Ball;
using CatTennis.Rebuild.Cat;
using CatTennis.Rebuild.Config;
using CatTennis.Rebuild.Rules;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatTennis.Rebuild.Flow
{
    /// <summary>Match composition root; initializes references in one fixed order.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class MatchBootstrapper : MonoBehaviour
    {
        [Header("Configs")]
        [SerializeField] private BallPhysicsConfig ballPhysicsConfig;
        [SerializeField] private CourtGeometryConfig courtGeometryConfig;
        [SerializeField] private Phase3PointLoopConfig pointLoopConfig;
        [SerializeField] private PlayerControlConfig playerControlConfig;
        [SerializeField] private ShotBalanceConfig shotBalanceConfig;

        [Header("Systems")]
        [SerializeField] private MatchSceneValidator validator;
        [SerializeField] private PointLifecycleController lifecycle;
        [SerializeField] private CourtZoneDetector detector;
        [SerializeField] private RallyFlowManager rally;
        [SerializeField] private MatchFlowManager match;
        [SerializeField] private ResetFlowController reset;
        [SerializeField] private PointLoopEventBridge pointBridge;

        [Header("Actors")]
        [SerializeField] private BallPhysicsApplier ballPhysics;
        [SerializeField] private BallController ball;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerHitDetector hitDetector;
        [SerializeField] private PlayerCatController player;
        [SerializeField] private PlayerShotEventBridge shotBridge;
        [SerializeField] private Collider2D playerCollider;
        [SerializeField] private Collider2D groundCollider;

        private bool initialized;

        public bool IsInitialized => initialized;

        public void Configure(
            BallPhysicsConfig ballPhysicsSettings,
            CourtGeometryConfig courtGeometry,
            Phase3PointLoopConfig pointLoop,
            PlayerControlConfig playerControl,
            ShotBalanceConfig shotBalance,
            MatchSceneValidator sceneValidator,
            PointLifecycleController pointLifecycle,
            CourtZoneDetector courtDetector,
            RallyFlowManager rallyManager,
            MatchFlowManager matchManager,
            ResetFlowController resetController,
            PointLoopEventBridge pointLoopBridge,
            BallPhysicsApplier physicsApplier,
            BallController ballController,
            PlayerInputReader inputReader,
            PlayerHitDetector playerHitDetector,
            PlayerCatController playerController,
            PlayerShotEventBridge playerShotBridge,
            Collider2D bodyCollider,
            Collider2D courtGroundCollider)
        {
            ballPhysicsConfig = ballPhysicsSettings;
            courtGeometryConfig = courtGeometry;
            pointLoopConfig = pointLoop;
            playerControlConfig = playerControl;
            shotBalanceConfig = shotBalance;
            validator = sceneValidator;
            lifecycle = pointLifecycle;
            detector = courtDetector;
            rally = rallyManager;
            match = matchManager;
            reset = resetController;
            pointBridge = pointLoopBridge;
            ballPhysics = physicsApplier;
            ball = ballController;
            input = inputReader;
            hitDetector = playerHitDetector;
            player = playerController;
            shotBridge = playerShotBridge;
            playerCollider = bodyCollider;
            groundCollider = courtGroundCollider;
        }

        private void Start()
        {
            InitializeMatch();
        }

        public bool InitializeMatch()
        {
            if (initialized)
            {
                return false;
            }

            RequireReferences();
            courtGeometryConfig.ValidateOrThrow();
            pointLoopConfig.ValidateOrThrow();
            playerControlConfig.ValidateOrThrow();
            shotBalanceConfig.ValidateOrThrow();
            ballPhysicsConfig.CreateSettings();

            lifecycle.Initialize();
            ballPhysics.Configure(ballPhysicsConfig, true, courtGeometryConfig.GroundY);
            detector.Initialize(courtGeometryConfig);
            match.Initialize(pointLoopConfig);
            reset.Initialize(ball, pointLoopConfig);
            hitDetector.Initialize(ball, playerControlConfig);
            player.Initialize(input, hitDetector, playerControlConfig);
            player.ResetPlayer(pointLoopConfig.PlayerResetPosition);
            pointBridge.Configure(ballPhysics, ball, detector, rally, match, reset);
            pointBridge.SetLifecycle(lifecycle);
            pointBridge.SetPlayerReset(player, pointLoopConfig.PlayerResetPosition);
            shotBridge.Configure(hitDetector, pointBridge, shotBalanceConfig);
            validator.Configure(
                ballPhysicsConfig,
                courtGeometryConfig,
                pointLoopConfig,
                playerControlConfig,
                shotBalanceConfig,
                ball,
                player,
                playerCollider,
                groundCollider);
            validator.ValidateOrThrow();

            initialized = true;
            pointBridge.StartInitialPoint();
            return true;
        }

        public void RetryMatch()
        {
            if (!initialized)
            {
                return;
            }

            pointBridge.RetryMatch();
        }

        public void ReturnToMainMenu()
        {
            reset.CancelPendingReset();
            lifecycle.ResetForSceneReload();
            SceneManager.LoadScene("Rebuild_MainMenu");
        }

        private void RequireReferences()
        {
            if (ballPhysicsConfig == null || courtGeometryConfig == null ||
                pointLoopConfig == null || playerControlConfig == null ||
                shotBalanceConfig == null || validator == null || lifecycle == null ||
                detector == null || rally == null || match == null || reset == null ||
                pointBridge == null || ballPhysics == null || ball == null || input == null ||
                hitDetector == null || player == null || shotBridge == null ||
                playerCollider == null || groundCollider == null)
            {
                throw new InvalidOperationException("MatchBootstrapper references are incomplete.");
            }
        }
    }
}
