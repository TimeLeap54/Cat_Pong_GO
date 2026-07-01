using CatTennis.Rebuild.Flow;
using CatTennis.Rebuild.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CatTennis.Rebuild.Editor
{
    public static class RallyHudPrefabBuilder
    {
        private const string PrefabFolder = "Assets/Art/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/RallyHUD.prefab";
        private const string BestSpritePath = "Assets/Art/UI/RallyModeRuntime/Best_Count_Runtime.png";
        private const string RallySpritePath = "Assets/Art/UI/RallyModeRuntime/Rally_Count_Runtime.png";
        private const string StopSpritePath = "Assets/Art/UI/RallyModeRuntime/Stop_Runtime.png";
        private const string DefaultFontPath = "Assets/Art/Fonts/Galmuri11-Bold Dynamic.asset";

        [MenuItem("Cat Tennis/Rebuild/Create Rally HUD Prefab")]
        public static void CreateRallyHudPrefab()
        {
            ConfigureSpriteImporter(BestSpritePath, 1024);
            ConfigureSpriteImporter(RallySpritePath, 1024);
            ConfigureSpriteImporter(StopSpritePath, 1024);
            AssetDatabase.Refresh();

            if (!AssetDatabase.IsValidFolder("Assets/Art/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/Art", "Prefabs");
            }

            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/Art/Prefabs", "UI");
            }

            Sprite bestSprite = LoadRequired<Sprite>(BestSpritePath);
            Sprite rallySprite = LoadRequired<Sprite>(RallySpritePath);
            Sprite stopSprite = LoadRequired<Sprite>(StopSpritePath);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultFontPath);

            GameObject root = new GameObject("RallyHUD");
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            RallyHudPresenter presenter = root.AddComponent<RallyHudPresenter>();

            Image bestPanel = CreateImage("BestPanel", root.transform, bestSprite, false);
            ConfigureRect(bestPanel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(44f, -38f), new Vector2(384f, 126f));
            TMP_Text bestLabel = CreateText("BestLabel", bestPanel.transform, font, "BEST", 32,
                new Vector2(-48f, 0f), new Vector2(126f, 48f));
            TMP_Text bestValue = CreateText("BestValue", bestPanel.transform, font, "128", 48,
                new Vector2(106f, 0f), new Vector2(144f, 60f));

            Image rallyPanel = CreateImage("RallyPanel", root.transform, rallySprite, false);
            ConfigureRect(rallyPanel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -38f), new Vector2(560f, 292f));
            TMP_Text rallyLabel = CreateText("RallyLabel", rallyPanel.transform, font, "RALLY", 36,
                new Vector2(0f, 54f), new Vector2(250f, 48f));
            TMP_Text rallyValue = CreateText("RallyValue", rallyPanel.transform, font, "23", 92,
                new Vector2(74f, -50f), new Vector2(220f, 112f));

            Image pauseImage = CreateImage("PauseButton", root.transform, stopSprite, true);
            ConfigureRect(pauseImage.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-44f, -38f), new Vector2(128f, 132f));
            Button pauseButton = pauseImage.gameObject.AddComponent<Button>();
            pauseButton.targetGraphic = pauseImage;

            SerializedObject serialized = new SerializedObject(presenter);
            serialized.FindProperty("rallyLabel").objectReferenceValue = rallyLabel;
            serialized.FindProperty("rallyValue").objectReferenceValue = rallyValue;
            serialized.FindProperty("bestLabel").objectReferenceValue = bestLabel;
            serialized.FindProperty("bestValue").objectReferenceValue = bestValue;
            serialized.FindProperty("pauseButton").objectReferenceValue = pauseButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Debug.Log($"Created Rally HUD prefab: {PrefabPath}");
        }

        [MenuItem("Cat Tennis/Rebuild/Install Rally HUD In Open Scene")]
        public static void InstallRallyHudInOpenScene()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                CreateRallyHudPrefab();
            }

            GameObject prefab = LoadRequired<GameObject>(PrefabPath);
            RallyHudPresenter[] existingHuds = Object.FindObjectsOfType<RallyHudPresenter>(true);
            for (int i = 0; i < existingHuds.Length; i++)
            {
                if (existingHuds[i].gameObject.name == "RallyHUD")
                {
                    Object.DestroyImmediate(existingHuds[i].gameObject);
                }
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "RallyHUD";

            RallyHudPresenter presenter = instance.GetComponent<RallyHudPresenter>();
            RallyFlowManager rallyFlow = Object.FindObjectOfType<RallyFlowManager>(true);
            if (presenter != null && rallyFlow != null)
            {
                SerializedObject serialized = new SerializedObject(presenter);
                serialized.FindProperty("rallyFlow").objectReferenceValue = rallyFlow;
                serialized.FindProperty("language").enumValueIndex = (int)RallyHudLanguage.English;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            else if (rallyFlow == null)
            {
                Debug.LogWarning("Rally HUD was installed, but RallyFlowManager was not found. Open Rebuild_Match before installing, or bind it manually.");
            }

            EditorSceneManager.MarkSceneDirty(instance.scene);
            Selection.activeGameObject = instance;
            Debug.Log("Installed Rally HUD in the open scene.");
        }

        [MenuItem("Cat Tennis/Rebuild/Open Rally HUD Prefab For Editing")]
        public static void OpenRallyHudPrefabForEditing()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                CreateRallyHudPrefab();
            }

            GameObject prefab = LoadRequired<GameObject>(PrefabPath);
            AssetDatabase.OpenAsset(prefab);
        }

        [MenuItem("Cat Tennis/Rebuild/Select Rally HUD In Open Scene")]
        public static void SelectRallyHudInOpenScene()
        {
            RallyHudPresenter[] existingHuds = Object.FindObjectsOfType<RallyHudPresenter>(true);
            for (int i = 0; i < existingHuds.Length; i++)
            {
                if (existingHuds[i].gameObject.name == "RallyHUD")
                {
                    Selection.activeGameObject = existingHuds[i].gameObject;
                    EditorGUIUtility.PingObject(existingHuds[i].gameObject);
                    return;
                }
            }

            Debug.LogWarning("RallyHUD was not found in the open scene.");
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, bool raycastTarget)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            Image image = obj.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string value,
            float size,
            Vector2 position,
            Vector2 rectSize)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyles.Normal;
            text.richText = false;
            text.enableAutoSizing = false;
            text.enableWordWrapping = false;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color32(38, 100, 54, 255);
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Overflow;
            ConfigureRect(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, rectSize);
            return text;
        }

        private static void ConfigureRect(
            RectTransform rect,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
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
