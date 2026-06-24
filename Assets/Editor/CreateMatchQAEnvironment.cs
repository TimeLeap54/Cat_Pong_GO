using System;
using System.IO;
using CatTennis.Rebuild.Ball;
using CatTennis.Rebuild.Cat;
using CatTennis.Rebuild.Config;
using CatTennis.Rebuild.Debugging;
using CatTennis.Rebuild.Flow;
using CatTennis.Rebuild.Rules;
using CatTennis.Rebuild.State;
using CatTennis.Rebuild.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CatTennis.Rebuild.EditorTools
{
    public static class CreateMatchQAEnvironment
    {
        private const string SettingsFolder = "Assets/Settings/Match";
        private const string MainMenuPath = "Assets/Scenes/Rebuild_MainMenu.unity";
        private const string MatchPath = "Assets/Scenes/Rebuild_Match.unity";

        [MenuItem("Cat Tennis/Rebuild/Create Match QA Environment")]
        public static void Create()
        {
            EnsureFolder("Assets/Settings");
            EnsureFolder(SettingsFolder);
            EnsureLayer(8, "PlayerBody");
            EnsureLayer(9, "TennisBall");
            EnsureLayer(10, "Ground");
            EnsureLayer(11, "CourtSensor");

            BallPhysicsConfig physics = LoadOrCreate<BallPhysicsConfig>(
                $"{SettingsFolder}/BallPhysicsConfig_Match.asset");
            CourtGeometryConfig geometry = LoadOrCreate<CourtGeometryConfig>(
                $"{SettingsFolder}/CourtGeometryConfig_Match.asset");
            geometry.Configure(-10f, 10f, -8f, -0.5f, 0.5f, 8f, 0.01f, true, -4f, 0f);
            Phase3PointLoopConfig loop = LoadOrCreate<Phase3PointLoopConfig>(
                $"{SettingsFolder}/PointLoopConfig_Match.asset");
            loop.Configure(5, 0.35f, HitterType.Opponent, new Vector2(4f, 1f), new Vector2(-4f, 6f));
            loop.SetPlayerResetPosition(new Vector2(-4f, 0.75f));
            PlayerControlConfig playerConfig = LoadOrCreate<PlayerControlConfig>(
                $"{SettingsFolder}/PlayerControlConfig_Match.asset");
            playerConfig.Configure(5f, 7f, 3f, new Vector2(0f, -0.75f), 0.18f, 1 << 10, 8, 9);
            ShotBalanceConfig shotConfig = LoadOrCreate<ShotBalanceConfig>(
                $"{SettingsFolder}/ShotBalanceConfig_Match.asset");
            shotConfig.Configure(6f, 6f, 9f, -2f, 20f, 20f, 20f);
            EditorUtility.SetDirty(geometry);
            EditorUtility.SetDirty(loop);
            EditorUtility.SetDirty(playerConfig);
            EditorUtility.SetDirty(shotConfig);
            AssetDatabase.SaveAssets();

            CreateMainMenuScene();
            physics = AssetDatabase.LoadAssetAtPath<BallPhysicsConfig>(
                $"{SettingsFolder}/BallPhysicsConfig_Match.asset");
            geometry = AssetDatabase.LoadAssetAtPath<CourtGeometryConfig>(
                $"{SettingsFolder}/CourtGeometryConfig_Match.asset");
            loop = AssetDatabase.LoadAssetAtPath<Phase3PointLoopConfig>(
                $"{SettingsFolder}/PointLoopConfig_Match.asset");
            playerConfig = AssetDatabase.LoadAssetAtPath<PlayerControlConfig>(
                $"{SettingsFolder}/PlayerControlConfig_Match.asset");
            shotConfig = AssetDatabase.LoadAssetAtPath<ShotBalanceConfig>(
                $"{SettingsFolder}/ShotBalanceConfig_Match.asset");
            CreateMatchScene(physics, geometry, loop, playerConfig, shotConfig);
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Created production MainMenu and Match QA scenes.");
        }

        private static void CreateMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(new Color(0.25f, 0.65f, 0.88f));
            new GameObject("MainMenuController").AddComponent<MainMenuController>();
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, MainMenuPath);
        }

        private static void CreateMatchScene(
            BallPhysicsConfig physics,
            CourtGeometryConfig geometry,
            Phase3PointLoopConfig loop,
            PlayerControlConfig playerConfig,
            ShotBalanceConfig shotConfig)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(new Color(0.38f, 0.73f, 0.92f));
            CreateEventSystem();
            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            GameObject court = new GameObject("Court");
            GameObject ground = CreateMarker("Ground", new Vector2(0f, -0.25f), new Vector2(20f, 0.5f), new Color(0.16f, 0.52f, 0.25f), sprite, court.transform);
            ground.layer = 10;
            BoxCollider2D groundCollider = ground.AddComponent<BoxCollider2D>();
            groundCollider.size = sprite.bounds.size;
            CreateMarker("PlayerCourt", new Vector2(-4.25f, 0.04f), new Vector2(7.5f, 0.06f), Color.white, sprite, court.transform);
            CreateMarker("OpponentCourt", new Vector2(4.25f, 0.04f), new Vector2(7.5f, 0.06f), Color.white, sprite, court.transform);
            CreateMarker("NetVisual", new Vector2(0f, 0.5f), new Vector2(0.12f, 1f), new Color(0.08f, 0.2f, 0.14f), sprite, court.transform);

            CreateMarker("Opponent", new Vector2(5f, 0.75f), new Vector2(0.9f, 1.5f), new Color(0.4f, 0.65f, 1f), sprite, null);

            GameObject ballObject = CreateMarker("Ball", loop.ResetPosition, Vector2.one * 0.3f, Color.yellow, sprite, null);
            ballObject.layer = 9;
            Rigidbody2D ballBody = ballObject.AddComponent<Rigidbody2D>();
            ballBody.bodyType = RigidbodyType2D.Kinematic;
            ballBody.gravityScale = 0f;
            CircleCollider2D ballCollider = ballObject.AddComponent<CircleCollider2D>();
            ballCollider.isTrigger = true;
            BallPhysicsApplier ballPhysics = ballObject.AddComponent<BallPhysicsApplier>();
            BallController ball = ballObject.AddComponent<BallController>();
            ballPhysics.Configure(physics, true, geometry.GroundY);

            GameObject player = new GameObject("Player");
            player.transform.position = loop.PlayerResetPosition;
            player.layer = 8;
            player.SetActive(false);
            CreateMarker("Visual", Vector2.zero, new Vector2(0.9f, 1.5f), new Color(1f, 0.5f, 0.62f), sprite, player.transform);
            Rigidbody2D playerBody = player.AddComponent<Rigidbody2D>();
            playerBody.bodyType = RigidbodyType2D.Dynamic;
            playerBody.gravityScale = playerConfig.GravityScale;
            playerBody.constraints = RigidbodyConstraints2D.FreezeRotation;
            CapsuleCollider2D playerCollider = player.AddComponent<CapsuleCollider2D>();
            playerCollider.size = new Vector2(0.8f, 1.4f);
            PlayerInputReader input = player.AddComponent<PlayerInputReader>();
            PlayerHitDetector hitDetector = player.AddComponent<PlayerHitDetector>();
            hitDetector.Initialize(ball, playerConfig);
            PlayerCatController playerController = player.AddComponent<PlayerCatController>();
            SerializedObject playerSerialized = new SerializedObject(playerController);
            playerSerialized.FindProperty("inputReader").objectReferenceValue = input;
            playerSerialized.FindProperty("hitDetector").objectReferenceValue = hitDetector;
            playerSerialized.FindProperty("config").objectReferenceValue = playerConfig;
            playerSerialized.ApplyModifiedPropertiesWithoutUndo();
            playerCollider.excludeLayers |= 1 << playerConfig.BallLayer;

            GameObject systems = new GameObject("MatchSystems");
            systems.SetActive(false);
            MatchSceneValidator validator = systems.AddComponent<MatchSceneValidator>();
            PointLifecycleController lifecycle = systems.AddComponent<PointLifecycleController>();
            CourtZoneDetector detector = systems.AddComponent<CourtZoneDetector>();
            RallyFlowManager rally = systems.AddComponent<RallyFlowManager>();
            MatchFlowManager match = systems.AddComponent<MatchFlowManager>();
            ResetFlowController reset = systems.AddComponent<ResetFlowController>();
            PointLoopEventBridge pointBridge = systems.AddComponent<PointLoopEventBridge>();
            PlayerShotEventBridge shotBridge = player.AddComponent<PlayerShotEventBridge>();
            MatchBootstrapper bootstrapper = systems.AddComponent<MatchBootstrapper>();

            detector.Initialize(geometry);
            match.Initialize(loop);
            reset.Initialize(ball, loop);
            pointBridge.Configure(ballPhysics, ball, detector, rally, match, reset);
            pointBridge.SetLifecycle(lifecycle);
            pointBridge.SetPlayerReset(playerController, loop.PlayerResetPosition);
            SerializedObject shotBridgeSerialized = new SerializedObject(shotBridge);
            shotBridgeSerialized.FindProperty("hitDetector").objectReferenceValue = hitDetector;
            shotBridgeSerialized.FindProperty("pointLoopBridge").objectReferenceValue = pointBridge;
            shotBridgeSerialized.FindProperty("shotConfig").objectReferenceValue = shotConfig;
            shotBridgeSerialized.ApplyModifiedPropertiesWithoutUndo();
            bootstrapper.Configure(
                physics, geometry, loop, playerConfig, shotConfig,
                validator, lifecycle, detector, rally, match, reset, pointBridge,
                ballPhysics, ball, input, hitDetector, playerController, shotBridge,
                playerCollider, groundCollider);
            Phase3DebugHUD hud = systems.AddComponent<Phase3DebugHUD>();
            hud.Configure(match, rally, pointBridge);
            hud.ConfigurePlayer(playerController);
            hud.ConfigureNavigation(bootstrapper);

            player.SetActive(true);
            systems.SetActive(true);
            EditorSceneManager.SaveScene(scene, MatchPath);
        }

        private static void CreateCamera(Color background)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            camera.backgroundColor = background;
            cameraObject.transform.position = new Vector3(0f, 3f, -10f);
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static GameObject CreateMarker(
            string name,
            Vector2 position,
            Vector2 scale,
            Color color,
            Sprite sprite,
            Transform parent)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(parent, false);
            if (parent == null)
            {
                marker.transform.position = position;
            }
            else
            {
                marker.transform.localPosition = position;
            }
            marker.transform.localScale = new Vector3(
                scale.x / sprite.bounds.size.x,
                scale.y / sprite.bounds.size.y,
                1f);
            SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            return marker;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureLayer(int index, string layerName)
        {
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            SerializedProperty layer = layers.GetArrayElementAtIndex(index);
            if (!string.IsNullOrEmpty(layer.stringValue) && layer.stringValue != layerName)
            {
                throw new InvalidOperationException(
                    $"Layer {index} is already used by '{layer.stringValue}'.");
            }

            layer.stringValue = layerName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuPath, true),
                new EditorBuildSettingsScene(MatchPath, true)
            };
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
            }
        }
    }
}
