using CatTennis.Rebuild.Flow;
using CatTennis.Rebuild.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CatTennis.Rebuild.Editor
{
    public static class PauseMenuPrefabBuilder
    {
        private const string PrefabFolder = "Assets/Art/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/PauseMenu.prefab";
        private const string PanelPath = "Assets/Art/UI/Paused/Pause.png";
        private const string ButtonPath = "Assets/Art/UI/Paused/Button.png";
        private const string DefaultFontPath = "Assets/Art/Fonts/Galmuri11-Bold Dynamic.asset";

        [MenuItem("Cat Tennis/Rebuild/Create Pause Menu Prefab")]
        public static void CreatePauseMenuPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null &&
                !EditorUtility.DisplayDialog(
                    "Recreate Pause Menu Prefab?",
                    "This will overwrite the editable PauseMenu prefab and reset panel/text/button positions to builder defaults.",
                    "Recreate",
                    "Cancel"))
            {
                return;
            }

            ConfigureSpriteImporter(PanelPath, 1024);
            ConfigureSpriteImporter(ButtonPath, 1024);
            AssetDatabase.Refresh();
            EnsurePrefabFolder();

            Sprite panelSprite = LoadRequired<Sprite>(PanelPath);
            Sprite buttonSprite = LoadRequired<Sprite>(ButtonPath);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultFontPath);

            GameObject root = new GameObject("PauseMenu");
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();
            PauseMenuPresenter presenter = root.AddComponent<PauseMenuPresenter>();

            GameObject panelRoot = new GameObject("PanelRoot");
            panelRoot.transform.SetParent(root.transform, false);
            RectTransform panelRootRect = panelRoot.AddComponent<RectTransform>();
            panelRootRect.anchorMin = Vector2.zero;
            panelRootRect.anchorMax = Vector2.one;
            panelRootRect.pivot = new Vector2(0.5f, 0.5f);
            panelRootRect.offsetMin = Vector2.zero;
            panelRootRect.offsetMax = Vector2.zero;

            Image panel = CreateImage("Panel", panelRoot.transform, panelSprite, false);
            ConfigureRect(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(420f, 420f));

            TMP_Text title = CreateText("TitleLabel", panel.transform, font, "PAUSED", 38f,
                new Vector2(0f, 138f), new Vector2(260f, 54f), new Color32(38, 100, 54, 255));

            Button resumeButton = CreateButton("ResumeButton", panel.transform, buttonSprite,
                new Vector2(0f, 62f), new Vector2(300f, 70f));
            TMP_Text resumeText = CreateText("ResumeLabel", resumeButton.transform, font, "RESUME", 28f,
                Vector2.zero, new Vector2(240f, 44f), new Color32(250, 244, 218, 255));

            Button restartButton = CreateButton("RestartButton", panel.transform, buttonSprite,
                new Vector2(0f, -24f), new Vector2(300f, 70f));
            TMP_Text restartText = CreateText("RestartLabel", restartButton.transform, font, "RESTART", 28f,
                Vector2.zero, new Vector2(240f, 44f), new Color32(250, 244, 218, 255));

            Button mainMenuButton = CreateButton("MainMenuButton", panel.transform, buttonSprite,
                new Vector2(0f, -110f), new Vector2(300f, 70f));
            TMP_Text mainMenuText = CreateText("MainMenuLabel", mainMenuButton.transform, font, "MAIN MENU", 26f,
                Vector2.zero, new Vector2(260f, 44f), new Color32(250, 244, 218, 255));

            SerializedObject serialized = new SerializedObject(presenter);
            serialized.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            serialized.FindProperty("resumeButton").objectReferenceValue = resumeButton;
            serialized.FindProperty("restartButton").objectReferenceValue = restartButton;
            serialized.FindProperty("mainMenuButton").objectReferenceValue = mainMenuButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            panelRoot.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Debug.Log($"Created Pause Menu prefab: {PrefabPath}");

            if (!EditorApplication.isPlaying &&
                PrefabStageUtility.GetCurrentPrefabStage() == null &&
                SceneManager.GetActiveScene().name == "Rebuild_Match")
            {
                InstallPauseMenuInOpenScene();
            }
        }

        [MenuItem("Cat Tennis/Rebuild/Open Pause Menu Prefab For Editing")]
        public static void OpenPauseMenuPrefabForEditing()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                CreatePauseMenuPrefab();
            }

            AssetDatabase.OpenAsset(LoadRequired<GameObject>(PrefabPath));
        }

        [MenuItem("Cat Tennis/Rebuild/Install Pause Menu In Open Scene")]
        public static void InstallPauseMenuInOpenScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Stop Play Mode before installing Pause Menu into the open scene.");
                return;
            }

            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                Debug.LogError("Close Prefab Mode before installing Pause Menu into the open scene.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                CreatePauseMenuPrefab();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Scene targetScene = SceneManager.GetActiveScene();
            if (!targetScene.IsValid() || !targetScene.isLoaded)
            {
                Debug.LogError("Pause Menu install failed: no valid active scene is open.");
                return;
            }

            int removedCount = RemoveExistingPauseMenus(targetScene);

            GameObject prefab = LoadRequired<GameObject>(PrefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, targetScene);
            instance.name = "PauseMenu";

            PauseMenuPresenter pauseMenu = instance.GetComponent<PauseMenuPresenter>();
            MatchBootstrapper bootstrapper = FindInScene<MatchBootstrapper>(targetScene);
            RallyHudPresenter rallyHud = FindInScene<RallyHudPresenter>(targetScene);

            if (pauseMenu != null)
            {
                pauseMenu.Bind(bootstrapper);
                SerializedObject serializedPause = new SerializedObject(pauseMenu);
                serializedPause.FindProperty("bootstrapper").objectReferenceValue = bootstrapper;
                serializedPause.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(pauseMenu);
            }

            if (rallyHud != null)
            {
                rallyHud.RegisterPauseMenu(pauseMenu);
                SerializedObject serializedHud = new SerializedObject(rallyHud);
                serializedHud.FindProperty("pauseMenu").objectReferenceValue = pauseMenu;
                serializedHud.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(rallyHud);
            }
            else
            {
                Debug.LogWarning("Pause Menu was installed, but RallyHUD was not found. Install Rally HUD first, or bind pauseMenu manually.");
            }

            if (bootstrapper == null)
            {
                Debug.LogWarning("Pause Menu was installed, but MatchBootstrapper was not found. Open Rebuild_Match before installing, or bind it manually.");
            }

            EditorSceneManager.MarkSceneDirty(targetScene);
            Selection.activeGameObject = instance;
            Debug.Log($"Installed Pause Menu in the open scene. Removed previous instances: {removedCount}");
        }

        [MenuItem("Cat Tennis/Rebuild/Select Pause Menu In Open Scene")]
        public static void SelectPauseMenuInOpenScene()
        {
            PauseMenuPresenter presenter = FindEditablePresenter();
            if (presenter == null)
            {
                Debug.LogWarning("PauseMenu was not found in the open scene.");
                return;
            }

            Selection.activeGameObject = presenter.gameObject;
            EditorGUIUtility.PingObject(presenter.gameObject);
        }

        [MenuItem("Cat Tennis/Rebuild/Preview Pause Menu Panel")]
        public static void PreviewPauseMenuPanel()
        {
            SetPanelPreview(true);
        }

        [MenuItem("Cat Tennis/Rebuild/Hide Pause Menu Panel")]
        public static void HidePauseMenuPanel()
        {
            SetPanelPreview(false);
        }

        [MenuItem("Cat Tennis/Rebuild/Apply Selected Pause Menu Layout To Prefab")]
        public static void ApplySelectedPauseMenuLayoutToPrefab()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Stop Play Mode before applying Pause Menu layout changes to the prefab.");
                return;
            }

            PauseMenuPresenter sourcePresenter = FindEditablePresenter();
            if (sourcePresenter == null)
            {
                Debug.LogWarning("PauseMenu was not found. Select the PauseMenu object you edited, then try again.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                Debug.LogWarning("PauseMenu prefab does not exist yet. Create it first, then apply layout changes.");
                return;
            }

            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.assetPath == PrefabPath)
            {
                EditorSceneManager.MarkSceneDirty(prefabStage.scene);
                AssetDatabase.SaveAssets();
                Debug.Log("Pause Menu prefab is already open for editing. Save the prefab, then install it into Rebuild_Match.");
                return;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                CopyEditableUiState(sourcePresenter.transform, prefabRoot.transform);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Applied selected Pause Menu layout/text changes to the prefab. Run Install Pause Menu In Open Scene to reinstall this saved layout.");
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

        private static Button CreateButton(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
        {
            Image image = CreateImage(name, parent, sprite, true);
            ConfigureRect(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string value,
            float size,
            Vector2 position,
            Vector2 rectSize,
            Color32 color)
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
            text.color = color;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Overflow;
            ConfigureRect(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, rectSize);
            return text;
        }

        private static void ConfigureRect(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetPanelPreview(bool show)
        {
            PauseMenuPresenter presenter = FindEditablePresenter();
            if (presenter == null)
            {
                Debug.LogWarning("PauseMenu was not found. Open the prefab for editing or install it in Rebuild_Match first.");
                return;
            }

            SerializedObject serialized = new SerializedObject(presenter);
            GameObject panelRoot = serialized.FindProperty("panelRoot").objectReferenceValue as GameObject;
            if (panelRoot != null)
            {
                panelRoot.SetActive(show);
                EditorUtility.SetDirty(panelRoot);
                Selection.activeGameObject = panelRoot;
                EditorGUIUtility.PingObject(panelRoot);
            }

            EditorUtility.SetDirty(presenter);
            if (presenter.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(presenter.gameObject.scene);
            }
        }

        private static PauseMenuPresenter FindEditablePresenter()
        {
            if (Selection.activeGameObject != null)
            {
                PauseMenuPresenter selectedPresenter = Selection.activeGameObject.GetComponentInParent<PauseMenuPresenter>(true);
                if (selectedPresenter != null)
                {
                    return selectedPresenter;
                }
            }

            PauseMenuPresenter[] presenters = Object.FindObjectsOfType<PauseMenuPresenter>(true);
            return presenters.Length > 0 ? presenters[0] : null;
        }

        private static void CopyEditableUiState(Transform sourceRoot, Transform targetRoot)
        {
            CopyEditableUiObject(sourceRoot, targetRoot);

            for (int i = 0; i < sourceRoot.childCount; i++)
            {
                Transform sourceChild = sourceRoot.GetChild(i);
                Transform targetChild = targetRoot.Find(sourceChild.name);
                if (targetChild == null)
                {
                    continue;
                }

                CopyEditableUiState(sourceChild, targetChild);
            }
        }

        private static void CopyEditableUiObject(Transform source, Transform target)
        {
            target.gameObject.SetActive(source.gameObject.activeSelf);

            RectTransform sourceRect = source as RectTransform;
            RectTransform targetRect = target as RectTransform;
            if (sourceRect != null && targetRect != null)
            {
                targetRect.anchorMin = sourceRect.anchorMin;
                targetRect.anchorMax = sourceRect.anchorMax;
                targetRect.pivot = sourceRect.pivot;
                targetRect.anchoredPosition = sourceRect.anchoredPosition;
                targetRect.sizeDelta = sourceRect.sizeDelta;
                targetRect.localPosition = sourceRect.localPosition;
                targetRect.localRotation = sourceRect.localRotation;
                targetRect.localScale = sourceRect.localScale;
                targetRect.offsetMin = sourceRect.offsetMin;
                targetRect.offsetMax = sourceRect.offsetMax;
                EditorUtility.SetDirty(targetRect);
            }

            Image sourceImage = source.GetComponent<Image>();
            Image targetImage = target.GetComponent<Image>();
            if (sourceImage != null && targetImage != null)
            {
                targetImage.sprite = sourceImage.sprite;
                targetImage.color = sourceImage.color;
                targetImage.preserveAspect = sourceImage.preserveAspect;
                targetImage.raycastTarget = sourceImage.raycastTarget;
                targetImage.type = sourceImage.type;
                targetImage.fillCenter = sourceImage.fillCenter;
                EditorUtility.SetDirty(targetImage);
            }

            TMP_Text sourceText = source.GetComponent<TMP_Text>();
            TMP_Text targetText = target.GetComponent<TMP_Text>();
            if (sourceText != null && targetText != null)
            {
                targetText.text = sourceText.text;
                targetText.font = sourceText.font;
                targetText.fontSize = sourceText.fontSize;
                targetText.fontStyle = sourceText.fontStyle;
                targetText.color = sourceText.color;
                targetText.alignment = sourceText.alignment;
                targetText.enableAutoSizing = sourceText.enableAutoSizing;
                targetText.fontSizeMin = sourceText.fontSizeMin;
                targetText.fontSizeMax = sourceText.fontSizeMax;
                targetText.enableWordWrapping = sourceText.enableWordWrapping;
                targetText.overflowMode = sourceText.overflowMode;
                targetText.characterSpacing = sourceText.characterSpacing;
                targetText.wordSpacing = sourceText.wordSpacing;
                targetText.lineSpacing = sourceText.lineSpacing;
                targetText.richText = sourceText.richText;
                EditorUtility.SetDirty(targetText);
            }
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

        private static int RemoveExistingPauseMenus(Scene scene)
        {
            int removedCount = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (!IsPauseMenuRoot(root))
                {
                    continue;
                }

                Object.DestroyImmediate(root);
                removedCount++;
            }

            return removedCount;
        }

        private static bool IsPauseMenuRoot(GameObject root)
        {
            if (root == null)
            {
                return false;
            }

            if (root.name == "PauseMenu" || root.name.StartsWith("PauseMenu ("))
            {
                return true;
            }

            if (root.GetComponentInChildren<PauseMenuPresenter>(true) != null)
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
