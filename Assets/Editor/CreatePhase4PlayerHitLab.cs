using System.IO;
using CatTennis.Rebuild.Ball;
using CatTennis.Rebuild.Cat;
using CatTennis.Rebuild.Config;
using CatTennis.Rebuild.Debugging;
using CatTennis.Rebuild.Flow;
using CatTennis.Rebuild.Rules;
using CatTennis.Rebuild.State;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatTennis.Rebuild.EditorTools
{
    public static class CreatePhase4PlayerHitLab
    {
        private const string SettingsFolder = "Assets/Settings/Phase4";
        private const string ScenePath = "Assets/Scenes/Phase4_PlayerHitLab.unity";

        [MenuItem("Cat Tennis/Rebuild/Create Phase 4 Player Hit Lab")]
        public static void Create()
        {
            EnsureFolder("Assets/Settings");
            EnsureFolder(SettingsFolder);

            BallPhysicsConfig physics = LoadOrCreate<BallPhysicsConfig>(
                $"{SettingsFolder}/BallPhysicsConfig_Phase4.asset");
            CourtGeometryConfig geometry = LoadOrCreate<CourtGeometryConfig>(
                $"{SettingsFolder}/CourtGeometryConfig_Phase4.asset");
            geometry.Configure(-10f, 10f, -8f, -0.5f, 0.5f, 8f, 0.01f, true, -4f);
            Phase3PointLoopConfig loop = LoadOrCreate<Phase3PointLoopConfig>(
                $"{SettingsFolder}/PointLoopConfig_Phase4.asset");
            loop.Configure(5, 0.5f, HitterType.Opponent, new Vector2(4f, 1f), new Vector2(-4f, 6f));
            PlayerControlConfig playerConfig = LoadOrCreate<PlayerControlConfig>(
                $"{SettingsFolder}/PlayerControlConfig_Phase4.asset");
            playerConfig.Configure(5f, 7f, 3f, new Vector2(0f, -0.75f), 0.18f, 1 << 10, 8, 9);
            ShotBalanceConfig shotConfig = LoadOrCreate<ShotBalanceConfig>(
                $"{SettingsFolder}/ShotBalanceConfig_Phase4.asset");
            shotConfig.Configure(6f, 6f, 9f, -2f, 20f, 20f, 20f);
            EditorUtility.SetDirty(geometry);
            EditorUtility.SetDirty(loop);
            EditorUtility.SetDirty(playerConfig);
            EditorUtility.SetDirty(shotConfig);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            GameObject ground = CreateMarker("Ground", new Vector2(0f, -0.25f), new Vector2(20f, 0.5f), new Color(0.18f, 0.55f, 0.28f), sprite);
            ground.layer = 10;
            ground.AddComponent<BoxCollider2D>();
            CreateMarker("NeutralGap", new Vector2(0f, 0.02f), new Vector2(0.9f, 0.04f), new Color(0.95f, 0.85f, 0.3f), sprite);
            CreateMarker("OpponentSquare", new Vector2(5f, 0.75f), new Vector2(0.9f, 1.5f), new Color(0.45f, 0.7f, 1f), sprite);

            GameObject ballObject = CreateMarker("Ball", loop.ResetPosition, Vector2.one * 0.3f, Color.yellow, sprite);
            ballObject.layer = playerConfig.BallLayer;
            Rigidbody2D ballBody = ballObject.AddComponent<Rigidbody2D>();
            ballBody.bodyType = RigidbodyType2D.Kinematic;
            ballObject.AddComponent<CircleCollider2D>();
            BallPhysicsApplier applier = ballObject.AddComponent<BallPhysicsApplier>();
            BallController ball = ballObject.AddComponent<BallController>();
            applier.Configure(physics, true, 0f);

            GameObject systems = new GameObject("Phase4_PointLoopSystems");
            systems.SetActive(false);
            CourtZoneDetector detector = systems.AddComponent<CourtZoneDetector>();
            detector.Initialize(geometry);
            RallyFlowManager rally = systems.AddComponent<RallyFlowManager>();
            MatchFlowManager match = systems.AddComponent<MatchFlowManager>();
            match.Initialize(loop);
            ResetFlowController reset = systems.AddComponent<ResetFlowController>();
            reset.Initialize(ball, loop);
            PointLoopEventBridge pointBridge = systems.AddComponent<PointLoopEventBridge>();
            pointBridge.Configure(applier, ball, detector, rally, match, reset);
            SerializedObject pointBridgeSerialized = new SerializedObject(pointBridge);
            pointBridgeSerialized.FindProperty("startOnEnable").boolValue = true;
            pointBridgeSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject player = new GameObject("PlayerSquare");
            player.transform.position = new Vector2(-4f, 0.75f);
            player.SetActive(false);
            GameObject playerVisual = CreateMarker("Visual", Vector2.zero, new Vector2(0.9f, 1.5f), new Color(1f, 0.55f, 0.65f), sprite);
            playerVisual.transform.SetParent(player.transform, false);
            Rigidbody2D playerBody = player.AddComponent<Rigidbody2D>();
            playerBody.constraints = RigidbodyConstraints2D.FreezeRotation;
            CapsuleCollider2D playerCollider = player.AddComponent<CapsuleCollider2D>();
            playerCollider.size = new Vector2(0.8f, 1.4f);
            PlayerInputReader input = player.AddComponent<PlayerInputReader>();
            PlayerHitDetector hitDetector = player.AddComponent<PlayerHitDetector>();
            hitDetector.Initialize(ball, playerConfig);
            PlayerCatController playerController = player.AddComponent<PlayerCatController>();
            playerController.Initialize(input, hitDetector, playerConfig);
            PlayerShotEventBridge shotBridge = player.AddComponent<PlayerShotEventBridge>();
            shotBridge.Configure(hitDetector, pointBridge, shotConfig);
            player.SetActive(true);

            Phase3DebugHUD hud = systems.AddComponent<Phase3DebugHUD>();
            hud.Configure(match, rally, pointBridge);
            hud.ConfigurePlayer(playerController);
            systems.SetActive(true);

            EditorSceneManager.SaveScene(scene, ScenePath);
            RemoveFromBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created {ScenePath}. It remains excluded from Build Settings.");
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            camera.backgroundColor = new Color(0.4f, 0.75f, 0.95f);
            cameraObject.transform.position = new Vector3(0f, 3f, -10f);
        }

        private static GameObject CreateMarker(string name, Vector2 position, Vector2 scale, Color color, Sprite sprite)
        {
            GameObject marker = new GameObject(name);
            marker.transform.position = position;
            marker.transform.localScale = scale;
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

        private static void RemoveFromBuildSettings()
        {
            System.Collections.Generic.List<EditorBuildSettingsScene> kept =
                new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                if (buildScene.path != ScenePath)
                {
                    kept.Add(buildScene);
                }
            }

            EditorBuildSettings.scenes = kept.ToArray();
        }
    }
}
