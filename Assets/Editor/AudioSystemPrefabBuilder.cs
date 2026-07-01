using CatTennis.Rebuild.Audio;
using CatTennis.Rebuild.Config;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatTennis.Rebuild.Editor
{
    public static class AudioSystemPrefabBuilder
    {
        private const string PrefabFolder = "Assets/Art/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/AudioSystem.prefab";
        private const string ConfigFolder = "Assets/Settings/Audio";
        private const string ConfigPath = ConfigFolder + "/AudioConfig.asset";
        private const string UiClickPath = "Assets/SFX/Click.WAV";
        private const string HitPath = "Assets/SFX/Hit(1).WAV";
        private const string SmashPath = "Assets/SFX/Smash(1).WAV";

        [MenuItem("Cat Tennis/Rebuild/Create Audio System Prefab")]
        public static void CreateAudioSystemPrefab()
        {
            EnsureFolder(ConfigFolder);
            EnsureFolder(PrefabFolder);
            ConfigureSfxImporter(UiClickPath);
            ConfigureSfxImporter(HitPath);
            ConfigureSfxImporter(SmashPath);
            AssetDatabase.Refresh();

            AudioConfig config = AssetDatabase.LoadAssetAtPath<AudioConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<AudioConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            config.Configure(
                LoadRequired<AudioClip>(UiClickPath),
                LoadRequired<AudioClip>(HitPath),
                LoadRequired<AudioClip>(SmashPath));
            EditorUtility.SetDirty(config);

            GameObject root = new GameObject("AudioSystem");
            AudioSource source = root.AddComponent<AudioSource>();
            ConfigureAudioSource(source, false);

            AudioSource bgmSource = root.AddComponent<AudioSource>();
            ConfigureAudioSource(bgmSource, true);

            SfxPlayer player = root.AddComponent<SfxPlayer>();
            AudioEventRouter router = root.AddComponent<AudioEventRouter>();

            SerializedObject playerSerialized = new SerializedObject(player);
            playerSerialized.FindProperty("config").objectReferenceValue = config;
            playerSerialized.FindProperty("source").objectReferenceValue = source;
            playerSerialized.FindProperty("bgmSource").objectReferenceValue = bgmSource;
            playerSerialized.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject routerSerialized = new SerializedObject(router);
            routerSerialized.FindProperty("sfxPlayer").objectReferenceValue = player;
            routerSerialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Debug.Log($"Created Audio System prefab: {PrefabPath}");
        }

        [MenuItem("Cat Tennis/Rebuild/Install Audio System In Open Scene")]
        public static void InstallAudioSystemInOpenScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Stop Play Mode before installing Audio System into the open scene.");
                return;
            }

            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                Debug.LogError("Close Prefab Mode before installing Audio System into the open scene.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                CreateAudioSystemPrefab();
            }

            Scene targetScene = SceneManager.GetActiveScene();
            if (!targetScene.IsValid() || !targetScene.isLoaded)
            {
                Debug.LogError("Audio System install failed: no valid active scene is open.");
                return;
            }

            int removedCount = RemoveExistingAudioSystems(targetScene);
            GameObject prefab = LoadRequired<GameObject>(PrefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, targetScene);
            instance.name = "AudioSystem";
            EnsureAudioListener(targetScene, instance);

            EditorSceneManager.MarkSceneDirty(targetScene);
            Selection.activeGameObject = instance;
            Debug.Log($"Installed Audio System in the open scene. Removed previous instances: {removedCount}");
        }

        [MenuItem("Cat Tennis/Rebuild/Select Audio System In Open Scene")]
        public static void SelectAudioSystemInOpenScene()
        {
            AudioEventRouter[] routers = Object.FindObjectsOfType<AudioEventRouter>(true);
            if (routers.Length == 0)
            {
                Debug.LogWarning("AudioSystem was not found in the open scene.");
                return;
            }

            Selection.activeGameObject = routers[0].gameObject;
            EditorGUIUtility.PingObject(routers[0].gameObject);
        }

        private static int RemoveExistingAudioSystems(Scene scene)
        {
            int removedCount = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (!IsAudioSystemRoot(root))
                {
                    continue;
                }

                Object.DestroyImmediate(root);
                removedCount++;
            }

            return removedCount;
        }

        private static void ConfigureAudioSource(AudioSource source, bool loop)
        {
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
        }

        private static bool IsAudioSystemRoot(GameObject root)
        {
            if (root == null)
            {
                return false;
            }

            if (root.name == "AudioSystem" || root.name.StartsWith("AudioSystem ("))
            {
                return true;
            }

            if (root.GetComponentInChildren<AudioEventRouter>(true) != null ||
                root.GetComponentInChildren<SfxPlayer>(true) != null)
            {
                return true;
            }

            Object source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(root);
            if (source == null)
            {
                source = PrefabUtility.GetCorrespondingObjectFromSource(root);
            }

            return source != null && AssetDatabase.GetAssetPath(source) == PrefabPath;
        }

        private static void EnsureAudioListener(Scene scene, GameObject fallbackRoot)
        {
            AudioListener[] listeners = Object.FindObjectsOfType<AudioListener>(true);
            for (int i = 0; i < listeners.Length; i++)
            {
                if (listeners[i] != null && listeners[i].gameObject.scene == scene)
                {
                    return;
                }
            }

            if (fallbackRoot.GetComponent<AudioListener>() == null)
            {
                fallbackRoot.AddComponent<AudioListener>();
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
            }
        }

        private static void ConfigureSfxImporter(string path)
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                return;
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.quality = 1f;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = false;
            importer.forceToMono = false;
            importer.SaveAndReimport();
        }

        private static T LoadRequired<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new UnityException($"Missing required asset: {path}");
            }

            return asset;
        }
    }
}
