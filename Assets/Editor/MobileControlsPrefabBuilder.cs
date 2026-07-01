using CatTennis.Rebuild.Cat;
using CatTennis.Rebuild.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CatTennis.Rebuild.Editor
{
    public static class MobileControlsPrefabBuilder
    {
        private const string PrefabFolder = "Assets/Art/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/MobileControls.prefab";
        private const string JoystickBasePath = "Assets/Art/UI/Mobile_JoyStickRuntime/JoyStickBase_Runtime.png";
        private const string JoystickKnobPath = "Assets/Art/UI/Mobile_JoyStickRuntime/JoyStickKnob_Runtime.png";
        private const string HitPath = "Assets/Art/UI/Mobile_JoyStickRuntime/Hit_Runtime.png";
        private const string SmashPath = "Assets/Art/UI/Mobile_JoyStickRuntime/Smash_Runtime.png";

        [MenuItem("Cat Tennis/Rebuild/Create Mobile Controls Prefab")]
        public static void CreateMobileControlsPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null &&
                !EditorUtility.DisplayDialog(
                    "Recreate Mobile Controls Prefab?",
                    "This will overwrite the editable MobileControls prefab and reset joystick/button positions to builder defaults.",
                    "Recreate",
                    "Cancel"))
            {
                return;
            }

            AssetDatabase.Refresh();
            ConfigureSpriteImporter(JoystickBasePath, 1024);
            ConfigureSpriteImporter(JoystickKnobPath, 1024);
            ConfigureSpriteImporter(HitPath, 1024);
            ConfigureSpriteImporter(SmashPath, 1024);
            AssetDatabase.Refresh();

            EnsurePrefabFolder();

            GameObject root = new GameObject("MobileControls");
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 110;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();
            MobileControlsPresenter presenter = root.AddComponent<MobileControlsPresenter>();

            MobileJoystick joystick = CreateJoystick(root.transform, LoadRequired<Sprite>(JoystickBasePath), LoadRequired<Sprite>(JoystickKnobPath));
            MobileControlButton hit = CreateButton("HitButton", root.transform, LoadRequired<Sprite>(HitPath),
                MobileControlAction.Hit, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-248f, 66f), new Vector2(132f, 132f));
            MobileControlButton smash = CreateButton("SmashButton", root.transform, LoadRequired<Sprite>(SmashPath),
                MobileControlAction.Smash, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-112f, 118f), new Vector2(138f, 144f));

            SerializedObject serialized = new SerializedObject(presenter);
            serialized.FindProperty("joystick").objectReferenceValue = joystick;
            SerializedProperty buttons = serialized.FindProperty("buttons");
            buttons.arraySize = 2;
            buttons.GetArrayElementAtIndex(0).objectReferenceValue = hit;
            buttons.GetArrayElementAtIndex(1).objectReferenceValue = smash;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Debug.Log($"Created Mobile Controls prefab: {PrefabPath}");
        }

        [MenuItem("Cat Tennis/Rebuild/Open Mobile Controls Prefab For Editing")]
        public static void OpenMobileControlsPrefabForEditing()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                CreateMobileControlsPrefab();
            }

            AssetDatabase.OpenAsset(LoadRequired<GameObject>(PrefabPath));
        }

        [MenuItem("Cat Tennis/Rebuild/Install Mobile Controls In Open Scene")]
        public static void InstallMobileControlsInOpenScene()
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                Debug.LogError("Close Prefab Mode before installing Mobile Controls into the open scene.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                CreateMobileControlsPrefab();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Scene targetScene = SceneManager.GetActiveScene();
            if (!targetScene.IsValid() || !targetScene.isLoaded)
            {
                Debug.LogError("Mobile Controls install failed: no valid active scene is open.");
                return;
            }

            int removedCount = RemoveExistingMobileControls(targetScene);

            GameObject prefab = LoadRequired<GameObject>(PrefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, targetScene);
            instance.name = "MobileControls";

            MobileControlsPresenter presenter = instance.GetComponent<MobileControlsPresenter>();
            PlayerInputReader inputReader = FindInScene<PlayerInputReader>(targetScene);
            if (presenter != null && inputReader != null)
            {
                presenter.Bind(inputReader);
                SerializedObject serialized = new SerializedObject(presenter);
                serialized.FindProperty("inputReader").objectReferenceValue = inputReader;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(presenter);
            }
            else if (inputReader == null)
            {
                Debug.LogWarning("Mobile Controls were installed, but PlayerInputReader was not found. Open Rebuild_Match before installing, or bind it manually.");
            }

            EditorSceneManager.MarkSceneDirty(targetScene);
            Selection.activeGameObject = instance;
            Debug.Log($"Installed Mobile Controls in the open scene. Removed previous instances: {removedCount}");
        }

        private static MobileControlButton CreateButton(
            string name,
            Transform parent,
            Sprite sprite,
            MobileControlAction action,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            Image image = obj.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = true;

            MobileControlButton button = obj.AddComponent<MobileControlButton>();
            button.Configure(action, null);

            RectTransform rect = image.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return button;
        }

        private static MobileJoystick CreateJoystick(Transform parent, Sprite baseSprite, Sprite knobSprite)
        {
            GameObject obj = new GameObject("MoveJoystick");
            obj.transform.SetParent(parent, false);

            Image baseImage = obj.AddComponent<Image>();
            baseImage.sprite = baseSprite;
            baseImage.preserveAspect = true;
            baseImage.raycastTarget = true;

            RectTransform baseRect = baseImage.rectTransform;
            baseRect.anchorMin = new Vector2(0f, 0f);
            baseRect.anchorMax = new Vector2(0f, 0f);
            baseRect.pivot = new Vector2(0f, 0f);
            baseRect.anchoredPosition = new Vector2(62f, 42f);
            baseRect.sizeDelta = new Vector2(220f, 220f);

            GameObject knobObj = new GameObject("Knob");
            knobObj.transform.SetParent(obj.transform, false);

            Image knobImage = knobObj.AddComponent<Image>();
            knobImage.sprite = knobSprite;
            knobImage.preserveAspect = true;
            knobImage.raycastTarget = false;

            RectTransform knobRect = knobImage.rectTransform;
            knobRect.anchorMin = new Vector2(0.5f, 0.5f);
            knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.pivot = new Vector2(0.5f, 0.5f);
            knobRect.anchoredPosition = Vector2.zero;
            knobRect.sizeDelta = new Vector2(92f, 96f);

            MobileJoystick joystick = obj.AddComponent<MobileJoystick>();
            joystick.Configure(baseRect, knobRect, null);
            return joystick;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            T[] components = Object.FindObjectsOfType<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i].gameObject.scene == scene)
                {
                    return components[i];
                }
            }

            return null;
        }

        private static int RemoveExistingMobileControls(Scene scene)
        {
            int removedCount = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (!IsMobileControlsRoot(root))
                {
                    continue;
                }

                Object.DestroyImmediate(root);
                removedCount++;
            }

            return removedCount;
        }

        private static bool IsMobileControlsRoot(GameObject root)
        {
            if (root == null)
            {
                return false;
            }

            if (root.name == "MobileControls" || root.name.StartsWith("MobileControls ("))
            {
                return true;
            }

            if (root.GetComponentInChildren<MobileControlsPresenter>(true) != null)
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

        private static void EnsurePrefabFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Art/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/Art", "Prefabs");
            }

            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/Art/Prefabs", "UI");
            }
        }

        private static void ConfigureSpriteImporter(string path, int maxSize)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = maxSize;
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
