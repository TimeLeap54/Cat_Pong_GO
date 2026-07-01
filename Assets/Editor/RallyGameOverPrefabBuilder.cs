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
    public static class RallyGameOverPrefabBuilder
    {
        private const string PrefabFolder = "Assets/Art/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/RallyGameOver.prefab";
        private const string PanelPath = "Assets/Art/UI/RallyModeRuntime/Game_Over/GameOverPanel_Runtime.png";
        private const string BallPath = "Assets/Art/UI/RallyModeRuntime/Game_Over/GameOverBall_Runtime.png";
        private const string ButtonPath = "Assets/Art/UI/RallyModeRuntime/Game_Over/GameOverButton_Runtime.png";
        private const string NewBestPanelPath = "Assets/Art/UI/RallyModeRuntime/Best_Score/NewBestPanel_Runtime.png";
        private const string NewBestButtonPath = "Assets/Art/UI/RallyModeRuntime/Best_Score/NewBestButton_Runtime.png";
        private const string CrownPath = "Assets/Art/UI/RallyModeRuntime/Best_Score/Crown_Runtime.png";
        private const string DefaultFontPath = "Assets/Art/Fonts/Galmuri11-Bold Dynamic.asset";

        [MenuItem("Cat Tennis/Rebuild/Create Rally Game Over Prefab")]
        public static void CreateRallyGameOverPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null &&
                !EditorUtility.DisplayDialog(
                    "Recreate Rally Game Over Prefab?",
                    "This will overwrite the editable RallyGameOver prefab and reset panel/text/button positions to builder defaults.",
                    "Recreate",
                    "Cancel"))
            {
                return;
            }

            ConfigureSpriteImporter(PanelPath, 1024);
            ConfigureSpriteImporter(BallPath, 1024);
            ConfigureSpriteImporter(ButtonPath, 1024);
            ConfigureSpriteImporter(NewBestPanelPath, 1024);
            ConfigureSpriteImporter(NewBestButtonPath, 1024);
            ConfigureSpriteImporter(CrownPath, 1024);
            AssetDatabase.Refresh();
            EnsurePrefabFolder();

            Sprite panelSprite = LoadRequired<Sprite>(PanelPath);
            Sprite ballSprite = LoadRequired<Sprite>(BallPath);
            Sprite buttonSprite = LoadRequired<Sprite>(ButtonPath);
            Sprite newBestPanelSprite = LoadRequired<Sprite>(NewBestPanelPath);
            Sprite newBestButtonSprite = LoadRequired<Sprite>(NewBestButtonPath);
            Sprite crownSprite = LoadRequired<Sprite>(CrownPath);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultFontPath);

            GameObject root = new GameObject("RallyGameOver");
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 130;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();
            RallyGameOverPresenter presenter = root.AddComponent<RallyGameOverPresenter>();

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
                Vector2.zero, new Vector2(880f, 624f));

            Image ball = CreateImage("SadBall", panel.transform, ballSprite, false);
            ConfigureRect(ball.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-230f, -28f), new Vector2(244f, 268f));

            TMP_Text title = CreateText("TitleLabel", panel.transform, font, "RALLY OVER", 62f,
                new Vector2(0f, 214f), new Vector2(620f, 84f), new Color32(38, 100, 54, 255));
            TMP_Text finalLabel = CreateText("FinalScoreLabel", panel.transform, font, "FINAL SCORE", 31f,
                new Vector2(210f, 82f), new Vector2(280f, 42f), new Color32(38, 100, 54, 255));
            TMP_Text finalValue = CreateText("FinalScoreValue", panel.transform, font, "23", 72f,
                new Vector2(210f, 20f), new Vector2(220f, 78f), new Color32(38, 100, 54, 255));
            TMP_Text bestLabel = CreateText("BestScoreLabel", panel.transform, font, "BEST SCORE", 31f,
                new Vector2(210f, -84f), new Vector2(280f, 42f), new Color32(38, 100, 54, 255));
            TMP_Text bestValue = CreateText("BestScoreValue", panel.transform, font, "128", 68f,
                new Vector2(210f, -146f), new Vector2(240f, 78f), new Color32(38, 100, 54, 255));

            Button retryButton = CreateButton("RetryButton", panel.transform, buttonSprite,
                new Vector2(-170f, -242f), new Vector2(300f, 92f));
            TMP_Text retryText = CreateText("RetryLabel", retryButton.transform, font, "RETRY", 38f,
                Vector2.zero, new Vector2(250f, 58f), new Color32(250, 244, 218, 255));

            Button mainMenuButton = CreateButton("MainMenuButton", panel.transform, buttonSprite,
                new Vector2(202f, -242f), new Vector2(340f, 92f));
            TMP_Text mainMenuText = CreateText("MainMenuLabel", mainMenuButton.transform, font, "MAIN MENU", 34f,
                Vector2.zero, new Vector2(300f, 58f), new Color32(250, 244, 218, 255));

            SerializedObject serialized = new SerializedObject(presenter);
            serialized.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            serialized.FindProperty("titleLabel").objectReferenceValue = title;
            serialized.FindProperty("finalScoreLabel").objectReferenceValue = finalLabel;
            serialized.FindProperty("finalScoreValue").objectReferenceValue = finalValue;
            serialized.FindProperty("bestScoreLabel").objectReferenceValue = bestLabel;
            serialized.FindProperty("bestScoreValue").objectReferenceValue = bestValue;
            serialized.FindProperty("retryButton").objectReferenceValue = retryButton;
            serialized.FindProperty("mainMenuButton").objectReferenceValue = mainMenuButton;

            GameObject newBestPanelRoot = new GameObject("NewBestPanelRoot");
            newBestPanelRoot.transform.SetParent(root.transform, false);
            RectTransform newBestRootRect = newBestPanelRoot.AddComponent<RectTransform>();
            newBestRootRect.anchorMin = Vector2.zero;
            newBestRootRect.anchorMax = Vector2.one;
            newBestRootRect.pivot = new Vector2(0.5f, 0.5f);
            newBestRootRect.offsetMin = Vector2.zero;
            newBestRootRect.offsetMax = Vector2.zero;

            Image newBestPanel = CreateImage("NewBestPanel", newBestPanelRoot.transform, newBestPanelSprite, false);
            ConfigureRect(newBestPanel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 74f), new Vector2(560f, 596f));
            CreateNewBestDetailElements(newBestPanel.transform, font, crownSprite);
            TMP_Text newBestValue = CreateText("NewBestScoreValue", newBestPanel.transform, font, "146", 82f,
                new Vector2(78f, -164f), new Vector2(220f, 96f), new Color32(38, 100, 54, 255));

            Button newBestRetryButton = CreateButton("NewBestRetryButton", newBestPanelRoot.transform, newBestButtonSprite,
                new Vector2(-138f, -312f), new Vector2(230f, 70f));
            TMP_Text newBestRetryText = CreateText("NewBestRetryLabel", newBestRetryButton.transform, font, "RETRY", 28f,
                Vector2.zero, new Vector2(190f, 44f), new Color32(250, 244, 218, 255));

            Button newBestMainMenuButton = CreateButton("NewBestMainMenuButton", newBestPanelRoot.transform, newBestButtonSprite,
                new Vector2(138f, -312f), new Vector2(230f, 70f));
            TMP_Text newBestMainMenuText = CreateText("NewBestMainMenuLabel", newBestMainMenuButton.transform, font, "MAIN MENU", 24f,
                Vector2.zero, new Vector2(204f, 44f), new Color32(250, 244, 218, 255));

            serialized.FindProperty("newBestPanelRoot").objectReferenceValue = newBestPanelRoot;
            serialized.FindProperty("newBestScoreValue").objectReferenceValue = newBestValue;
            serialized.FindProperty("newBestRetryButton").objectReferenceValue = newBestRetryButton;
            serialized.FindProperty("newBestMainMenuButton").objectReferenceValue = newBestMainMenuButton;
            serialized.FindProperty("language").enumValueIndex = (int)RallyHudLanguage.English;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            panelRoot.SetActive(false);
            newBestPanelRoot.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Debug.Log($"Created Rally Game Over prefab: {PrefabPath}");

            if (!EditorApplication.isPlaying &&
                PrefabStageUtility.GetCurrentPrefabStage() == null &&
                SceneManager.GetActiveScene().name == "Rebuild_Match")
            {
                InstallRallyGameOverInOpenScene();
            }
        }

        [MenuItem("Cat Tennis/Rebuild/Open Rally Game Over Prefab For Editing")]
        public static void OpenRallyGameOverPrefabForEditing()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                CreateRallyGameOverPrefab();
            }

            AssetDatabase.OpenAsset(LoadRequired<GameObject>(PrefabPath));
        }

        [MenuItem("Cat Tennis/Rebuild/Install Rally Game Over In Open Scene")]
        public static void InstallRallyGameOverInOpenScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Stop Play Mode before installing Rally Game Over into the open scene. Scene installs made during Play Mode are not saved.");
                return;
            }

            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                Debug.LogError("Close Prefab Mode before installing Rally Game Over into the open scene.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                CreateRallyGameOverPrefab();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Scene targetScene = SceneManager.GetActiveScene();
            if (!targetScene.IsValid() || !targetScene.isLoaded)
            {
                Debug.LogError("Rally Game Over install failed: no valid active scene is open.");
                return;
            }

            int removedCount = RemoveExistingRallyGameOver(targetScene);

            GameObject prefab = LoadRequired<GameObject>(PrefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, targetScene);
            instance.name = "RallyGameOver";

            RallyGameOverPresenter presenter = instance.GetComponent<RallyGameOverPresenter>();
            RallyFlowManager rallyFlow = FindInScene<RallyFlowManager>(targetScene);
            MatchBootstrapper bootstrapper = FindInScene<MatchBootstrapper>(targetScene);
            if (presenter != null)
            {
                presenter.Bind(rallyFlow, bootstrapper);
                SerializedObject serialized = new SerializedObject(presenter);
                serialized.FindProperty("rallyFlow").objectReferenceValue = rallyFlow;
                serialized.FindProperty("bootstrapper").objectReferenceValue = bootstrapper;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(presenter);
            }

            if (rallyFlow == null || bootstrapper == null)
            {
                Debug.LogWarning("Rally Game Over was installed, but RallyFlowManager or MatchBootstrapper was not found. Open Rebuild_Match before installing, or bind it manually.");
            }

            EditorSceneManager.MarkSceneDirty(targetScene);
            Selection.activeGameObject = instance;
            Debug.Log($"Installed Rally Game Over in the open scene. Removed previous instances: {removedCount}");
        }

        [MenuItem("Cat Tennis/Rebuild/Select Rally Game Over In Open Scene")]
        public static void SelectRallyGameOverInOpenScene()
        {
            RallyGameOverPresenter[] presenters = Object.FindObjectsOfType<RallyGameOverPresenter>(true);
            for (int i = 0; i < presenters.Length; i++)
            {
                Selection.activeGameObject = presenters[i].gameObject;
                EditorGUIUtility.PingObject(presenters[i].gameObject);
                return;
            }

            Debug.LogWarning("RallyGameOver was not found in the open scene.");
        }

        [MenuItem("Cat Tennis/Rebuild/Preview Rally Game Over Normal Panel")]
        public static void PreviewNormalPanel()
        {
            SetPanelPreview(showNormal: true, showNewBest: false);
        }

        [MenuItem("Cat Tennis/Rebuild/Preview Rally Game Over New Best Panel")]
        public static void PreviewNewBestPanel()
        {
            SetPanelPreview(showNormal: false, showNewBest: true);
        }

        [MenuItem("Cat Tennis/Rebuild/Hide Rally Game Over Panels")]
        public static void HidePanels()
        {
            SetPanelPreview(showNormal: false, showNewBest: false);
        }

        [MenuItem("Cat Tennis/Rebuild/Add New Best Detail Elements")]
        public static void AddNewBestDetailElementsToSelected()
        {
            Transform newBestPanel = FindEditableNewBestPanel();
            if (newBestPanel == null)
            {
                Debug.LogWarning("NewBestPanel was not found. Select RallyGameOver or NewBestPanel, then try again.");
                return;
            }

            ConfigureSpriteImporter(CrownPath, 1024);
            AssetDatabase.Refresh();

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultFontPath);
            Sprite crownSprite = LoadRequired<Sprite>(CrownPath);
            CreateNewBestDetailElements(newBestPanel, font, crownSprite);
            EditorUtility.SetDirty(newBestPanel.gameObject);
            if (newBestPanel.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(newBestPanel.gameObject.scene);
            }

            Selection.activeGameObject = newBestPanel.gameObject;
            EditorGUIUtility.PingObject(newBestPanel.gameObject);
            Debug.Log("Added/updated New Best detail elements.");
        }

        [MenuItem("Cat Tennis/Rebuild/Apply Selected Rally Game Over Layout To Prefab")]
        public static void ApplySelectedRallyGameOverLayoutToPrefab()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Stop Play Mode before applying Rally Game Over layout changes to the prefab.");
                return;
            }

            RallyGameOverPresenter sourcePresenter = FindEditablePresenter();
            if (sourcePresenter == null)
            {
                Debug.LogWarning("RallyGameOver was not found. Select the RallyGameOver object you edited, then try again.");
                return;
            }

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefabAsset == null)
            {
                Debug.LogWarning("RallyGameOver prefab does not exist yet. Create it first, then apply layout changes.");
                return;
            }

            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.assetPath == PrefabPath)
            {
                EditorSceneManager.MarkSceneDirty(prefabStage.scene);
                AssetDatabase.SaveAssets();
                Debug.Log("Rally Game Over prefab is already open for editing. Save the prefab, then install it into Rebuild_Match.");
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
            Debug.Log("Applied selected Rally Game Over layout/text changes to the prefab. Run Install Rally Game Over In Open Scene to reinstall this saved layout.");
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

        private static void CreateNewBestDetailElements(Transform newBestPanel, TMP_FontAsset font, Sprite crownSprite)
        {
            Image crown = GetOrCreateImage("CrownIcon", newBestPanel, crownSprite, false);
            ConfigureRect(crown.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-112f, -164f), new Vector2(88f, 46f));

            TMP_Text bestScore = GetOrCreateText("NewBestTopLabel", newBestPanel, font);
            ConfigureText(bestScore, "BEST SCORE", 30f, new Vector2(0f, 166f), new Vector2(340f, 44f),
                new Color32(38, 100, 54, 255));

            TMP_Text rally = GetOrCreateText("NewBestRallyLabel", newBestPanel, font);
            ConfigureText(rally, "RALLY", 24f, new Vector2(0f, 78f), new Vector2(180f, 36f),
                new Color32(38, 100, 54, 255));
        }

        private static Image GetOrCreateImage(string name, Transform parent, Sprite sprite, bool raycastTarget)
        {
            Transform existing = parent.Find(name);
            Image image;
            if (existing != null && existing.TryGetComponent(out image))
            {
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = raycastTarget;
                return image;
            }

            return CreateImage(name, parent, sprite, raycastTarget);
        }

        private static TMP_Text GetOrCreateText(string name, Transform parent, TMP_FontAsset font)
        {
            Transform existing = parent.Find(name);
            TMP_Text text;
            if (existing != null && existing.TryGetComponent(out text))
            {
                text.font = font;
                return text;
            }

            return CreateText(name, parent, font, name, 24f, Vector2.zero, new Vector2(160f, 40f),
                new Color32(38, 100, 54, 255));
        }

        private static void ConfigureText(TMP_Text text, string value, float size, Vector2 position, Vector2 rectSize, Color32 color)
        {
            text.text = value;
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

        private static void SetPanelPreview(bool showNormal, bool showNewBest)
        {
            RallyGameOverPresenter presenter = FindEditablePresenter();
            if (presenter == null)
            {
                Debug.LogWarning("RallyGameOver was not found. Open the prefab for editing or install it in Rebuild_Match first.");
                return;
            }

            SerializedObject serialized = new SerializedObject(presenter);
            GameObject normalRoot = serialized.FindProperty("panelRoot").objectReferenceValue as GameObject;
            GameObject newBestRoot = serialized.FindProperty("newBestPanelRoot").objectReferenceValue as GameObject;

            if (normalRoot != null)
            {
                normalRoot.SetActive(showNormal);
                EditorUtility.SetDirty(normalRoot);
            }

            if (newBestRoot != null)
            {
                newBestRoot.SetActive(showNewBest);
                EditorUtility.SetDirty(newBestRoot);
            }

            EditorUtility.SetDirty(presenter);
            if (presenter.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(presenter.gameObject.scene);
            }

            Selection.activeGameObject = showNewBest && newBestRoot != null
                ? newBestRoot
                : showNormal && normalRoot != null
                    ? normalRoot
                    : presenter.gameObject;
            EditorGUIUtility.PingObject(Selection.activeGameObject);
        }

        private static RallyGameOverPresenter FindEditablePresenter()
        {
            if (Selection.activeGameObject != null)
            {
                RallyGameOverPresenter selectedPresenter = Selection.activeGameObject.GetComponentInParent<RallyGameOverPresenter>(true);
                if (selectedPresenter != null)
                {
                    return selectedPresenter;
                }
            }

            RallyGameOverPresenter[] presenters = Object.FindObjectsOfType<RallyGameOverPresenter>(true);
            return presenters.Length > 0 ? presenters[0] : null;
        }

        private static Transform FindEditableNewBestPanel()
        {
            if (Selection.activeGameObject != null)
            {
                Transform selected = Selection.activeGameObject.transform;
                if (selected.name == "NewBestPanel")
                {
                    return selected;
                }

                Transform child = selected.Find("NewBestPanel");
                if (child != null)
                {
                    return child;
                }

                Transform nested = selected.Find("NewBestPanelRoot/NewBestPanel");
                if (nested != null)
                {
                    return nested;
                }
            }

            RallyGameOverPresenter presenter = FindEditablePresenter();
            if (presenter == null)
            {
                return null;
            }

            Transform fromPresenter = presenter.transform.Find("NewBestPanelRoot/NewBestPanel");
            return fromPresenter != null ? fromPresenter : null;
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

        private static int RemoveExistingRallyGameOver(Scene scene)
        {
            int removedCount = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (!IsRallyGameOverRoot(root))
                {
                    continue;
                }

                Object.DestroyImmediate(root);
                removedCount++;
            }

            return removedCount;
        }

        private static bool IsRallyGameOverRoot(GameObject root)
        {
            if (root == null)
            {
                return false;
            }

            if (root.name == "RallyGameOver" || root.name.StartsWith("RallyGameOver ("))
            {
                return true;
            }

            if (root.GetComponentInChildren<RallyGameOverPresenter>(true) != null)
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
