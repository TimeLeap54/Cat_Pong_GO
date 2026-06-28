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
        [SerializeField] private AIBalanceConfig aiBalanceConfig;
        [SerializeField] private MovementBalanceConfig movementBalanceConfig;
        [SerializeField] private RallyAiBalanceConfig rallyAiConfig;

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
        [SerializeField] private ServeFlowController serveFlow;
        [SerializeField] private OpponentServeFlowController opponentServeFlow;
        [SerializeField] private ShotExecutionController shotExecutor;
        [SerializeField] private OpponentAIController opponent;
        [SerializeField] private OpponentHitDetector opponentHitDetector;
        [SerializeField] private Vector2 opponentResetPosition;
        [SerializeField] private Collider2D playerCollider;
        [SerializeField] private Collider2D groundCollider;

        private bool initialized;
        public static bool SelectedRallyMode = true; // 메인 메뉴에서 주입할 정적 변수

        public bool IsInitialized => initialized;

        public void Configure(
            BallPhysicsConfig ballPhysicsSettings,
            CourtGeometryConfig courtGeometry,
            Phase3PointLoopConfig pointLoop,
            PlayerControlConfig playerControl,
            ShotBalanceConfig shotBalance,
            AIBalanceConfig aiBalance,
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
            ServeFlowController serveFlowController,
            OpponentServeFlowController opponentServeFlowController,
            ShotExecutionController executionController,
            OpponentAIController opponentController,
            OpponentHitDetector aiHitDetector,
            Vector2 aiResetPosition,
            Collider2D bodyCollider,
            Collider2D courtGroundCollider)
        {
            ballPhysicsConfig = ballPhysicsSettings;
            courtGeometryConfig = courtGeometry;
            pointLoopConfig = pointLoop;
            playerControlConfig = playerControl;
            shotBalanceConfig = shotBalance;
            aiBalanceConfig = aiBalance;
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
            serveFlow = serveFlowController;
            opponentServeFlow = opponentServeFlowController;
            shotExecutor=executionController; opponent=opponentController;
            opponentHitDetector=aiHitDetector; opponentResetPosition=aiResetPosition;
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

            if (opponentServeFlow == null)
            {
                opponentServeFlow = GetComponent<OpponentServeFlowController>();
                if (opponentServeFlow == null)
                {
                    opponentServeFlow = gameObject.AddComponent<OpponentServeFlowController>();
                }
            }

            RequireReferences();
            courtGeometryConfig.ValidateOrThrow();

            // 런타임 모드 선택 상태를 Config 인스턴스에 주입
            pointLoopConfig.ConfigureRallyMode(SelectedRallyMode);

            pointLoopConfig.ValidateOrThrow();
            playerControlConfig.ValidateOrThrow();
            shotBalanceConfig.ValidateOrThrow();
            aiBalanceConfig.ValidateOrThrow();
            ballPhysicsConfig.CreateSettings();

            lifecycle.Initialize();
            ballPhysics.Configure(ballPhysicsConfig, true, courtGeometryConfig.GroundY);
            detector.Initialize(courtGeometryConfig);
            match.Initialize(pointLoopConfig);
            reset.Initialize(ball, pointLoopConfig);
            hitDetector.Initialize(ball, playerControlConfig, shotBalanceConfig);
            hitDetector.SetPointIdProvider(() => rally.GlobalPointId);
            player.Initialize(input, hitDetector, playerControlConfig,
                courtGeometryConfig.WorldMinX, courtGeometryConfig.PlayerCourtMaxX);
            player.ResetPlayer(pointLoopConfig.PlayerResetPosition);
            pointBridge.Configure(ballPhysics, ball, detector, rally, match, reset);
            pointBridge.SetConfig(pointLoopConfig);
            pointBridge.SetLifecycle(lifecycle);
            pointBridge.SetPlayerReset(player, pointLoopConfig.PlayerResetPosition);
            player.SetMovementBalance(movementBalanceConfig);
            pointBridge.SetMovementBalance(movementBalanceConfig);
            shotExecutor.Configure(pointBridge,shotBalanceConfig,ballPhysicsConfig,courtGeometryConfig, rallyAiConfig);
            pointBridge.SetShotExecutor(shotExecutor);
            shotBridge.Configure(hitDetector, shotExecutor);
            opponentHitDetector.Configure(ball,playerControlConfig,shotExecutor);
            opponent.Configure(ball,ballPhysicsConfig,courtGeometryConfig,playerControlConfig,
                aiBalanceConfig,rally,opponentHitDetector, player, rallyAiConfig);
            pointBridge.SetOpponentReset(opponent,opponentResetPosition);
            serveFlow.Configure(ball, player, hitDetector, shotBalanceConfig);
            if (opponentServeFlow != null)
            {
                opponentServeFlow.Configure(ball, opponent, shotBalanceConfig);
            }
            PlayerManualHitboxController manualHitboxes = player.GetComponent<PlayerManualHitboxController>();
            if (manualHitboxes != null)
            {
                manualHitboxes.Bind(hitDetector);
                hitDetector.SetManualHitboxController(manualHitboxes);
            }
            OpponentManualHitboxController aiManualHitboxes = opponent.GetComponent<OpponentManualHitboxController>();
            if (aiManualHitboxes != null)
            {
                aiManualHitboxes.Bind(opponentHitDetector);
                opponentHitDetector.SetManualHitboxController(aiManualHitboxes);
            }
            pointBridge.SetServeFlow(serveFlow);
            if (opponentServeFlow != null)
            {
                pointBridge.SetOpponentServeFlow(opponentServeFlow);
            }
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
                shotBalanceConfig == null || aiBalanceConfig == null || validator == null || lifecycle == null ||
                detector == null || rally == null || match == null || reset == null ||
                pointBridge == null || ballPhysics == null || ball == null || input == null ||
                hitDetector == null || player == null || shotBridge == null ||
                serveFlow == null || opponentServeFlow == null ||
                shotExecutor==null||opponent==null||opponentHitDetector==null||
                playerCollider == null || groundCollider == null)
            {
                throw new InvalidOperationException("MatchBootstrapper references are incomplete.");
            }
        }
    }
}
