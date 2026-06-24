using System.IO;
using CatTennis.Rebuild.Ball;
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
    public static class CreatePhase3PointLoopLab
    {
        private const string SettingsFolder = "Assets/Settings/Phase3";
        private const string ScenePath = "Assets/Scenes/Phase3_PointLoopLab.unity";

        [MenuItem("Cat Tennis/Rebuild/Create Phase 3 Point Loop Lab")]
        public static void Create()
        {
            EnsureFolder("Assets/Editor");
            EnsureFolder("Assets/Settings");
            EnsureFolder(SettingsFolder);

            BallPhysicsConfig physics = LoadOrCreate<BallPhysicsConfig>(
                $"{SettingsFolder}/BallPhysicsConfig_Phase3.asset");
            CourtGeometryConfig geometry = LoadOrCreate<CourtGeometryConfig>(
                $"{SettingsFolder}/CourtGeometryConfig_Phase3.asset");
            geometry.Configure(-10f, 10f, -8f, -0.5f, 0.5f, 8f, 0.01f, true, -4f);
            Phase3PointLoopConfig loop = LoadOrCreate<Phase3PointLoopConfig>(
                $"{SettingsFolder}/PointLoopConfig_Phase3.asset");
            loop.Configure(5, 0.5f, HitterType.Player, new Vector2(-4f, 1f), new Vector2(4f, 6f));
            EditorUtility.SetDirty(geometry);
            EditorUtility.SetDirty(loop);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            Sprite debugSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            CreateMarker("Ground", new Vector2(0f, -0.25f), new Vector2(20f, 0.5f), new Color(0.18f, 0.55f, 0.28f), debugSprite);
            CreateMarker("NeutralGap", new Vector2(0f, 0.02f), new Vector2(0.9f, 0.04f), new Color(0.95f, 0.85f, 0.3f), debugSprite);
            CreateMarker("PlayerSquare", new Vector2(-5f, 0.75f), new Vector2(0.9f, 1.5f), new Color(1f, 0.55f, 0.65f), debugSprite);
            CreateMarker("OpponentSquare", new Vector2(5f, 0.75f), new Vector2(0.9f, 1.5f), new Color(0.45f, 0.7f, 1f), debugSprite);

            GameObject ballObject = CreateMarker("Ball", loop.ResetPosition, Vector2.one * 0.3f, Color.yellow, debugSprite);
            Rigidbody2D body = ballObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            BallPhysicsApplier applier = ballObject.AddComponent<BallPhysicsApplier>();
            BallController ball = ballObject.AddComponent<BallController>();
            applier.Configure(physics, true, 0f);

            GameObject systems = new GameObject("Phase3_PointLoopSystems");
            systems.SetActive(false);
            CourtZoneDetector detector = systems.AddComponent<CourtZoneDetector>();
            detector.Initialize(geometry);
            RallyFlowManager rally = systems.AddComponent<RallyFlowManager>();
            MatchFlowManager match = systems.AddComponent<MatchFlowManager>();
            match.Initialize(loop);
            ResetFlowController reset = systems.AddComponent<ResetFlowController>();
            reset.Initialize(ball, loop);
            PointLoopEventBridge bridge = systems.AddComponent<PointLoopEventBridge>();
            bridge.Configure(applier, ball, detector, rally, match, reset);
            SerializedObject bridgeSerialized = new SerializedObject(bridge);
            bridgeSerialized.FindProperty("startOnEnable").boolValue = true;
            bridgeSerialized.ApplyModifiedPropertiesWithoutUndo();
            Phase3DebugHUD hud = systems.AddComponent<Phase3DebugHUD>();
            hud.Configure(match, rally, bridge);
            systems.SetActive(true);

            EditorSceneManager.SaveScene(scene, ScenePath);
            RemoveLabFromBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created {ScenePath}. It remains excluded from Build Settings.");
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            camera.backgroundColor = new Color(0.4f, 0.75f, 0.95f);
            cameraObject.transform.position = new Vector3(0f, 3f, -10f);
        }

        private static GameObject CreateMarker(
            string name,
            Vector2 position,
            Vector2 scale,
            Color color,
            Sprite sprite)
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
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void RemoveLabFromBuildSettings()
        {
            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            System.Collections.Generic.List<EditorBuildSettingsScene> kept =
                new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (EditorBuildSettingsScene buildScene in existing)
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
