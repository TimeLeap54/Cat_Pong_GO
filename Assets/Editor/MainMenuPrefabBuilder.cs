using CatTennis.Rebuild.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CatTennis.Rebuild.Editor
{
    public static class MainMenuPrefabBuilder
    {
        private const string PrefabFolder = "Assets/Art/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/MainMenuRuntime.prefab";
        private const string BackgroundPath = "Assets/Art/UI/Main_Background.png";
        private const string CharacterPath = "Assets/Art/UI/MainMenuClean/MainCharacter_Clean.png";
        private const string TitlePath = "Assets/Art/UI/MainMenuClean/Title_Eng_Clean.png";
        private const string StartPath = "Assets/Art/UI/MainMenuClean/Start_Clean.png";
        private const string HowToPlayPath = "Assets/Art/UI/MainMenuClean/How_To_Play_Clean.png";
        private const string SettingsPath = "Assets/Art/UI/MainMenuClean/Settings_Clean.png";
        private const string HowToPlayGuidePath = "Assets/Art/UI/MainMenuPanelsRuntime/How_To_Play_Guide_Runtime.png";
        private const string HowToPlayGotItButtonPath = "Assets/Art/UI/MainMenuPanelsRuntime/HowToPlay_Button_Runtime.png";
        private const string HowToPlayMoveIconPath = "Assets/Art/UI/MainMenuPanelsRuntime/HowToPlay_Move_Runtime.png";
        private const string HowToPlayHitIconPath = "Assets/Art/UI/MainMenuPanelsRuntime/HowToPlay_Hit_Runtime.png";
        private const string HowToPlaySmashIconPath = "Assets/Art/UI/MainMenuPanelsRuntime/HowToPlay_Smash_Runtime.png";
        private const string SettingsPanelPath = "Assets/Art/UI/MainMenuPanelsRuntime/Settings_Panel_Runtime.png";
        private const string SettingsRowPath = "Assets/Art/UI/MainMenuPanelsRuntime/Settings_Row_Runtime.png";
        private const string SettingsBgmIconPath = "Assets/Art/UI/MainMenuPanelsRuntime/Settings_Icon_Bgm_Runtime.png";
        private const string SettingsSfxIconPath = "Assets/Art/UI/MainMenuPanelsRuntime/Settings_Icon_Sfx_Runtime.png";
        private const string SettingsLanguageIconPath = "Assets/Art/UI/MainMenuPanelsRuntime/Settings_Icon_Language_Runtime.png";
        private const string SettingsAccountIconPath = "Assets/Art/UI/MainMenuPanelsRuntime/Settings_Icon_Account_Runtime.png";
        private const string SettingsArrowButtonPath = "Assets/Art/UI/MainMenuPanelsRuntime/Settings_ArrowButton_Runtime.png";
        private const string SettingsSliderTrackPath = "Assets/Art/UI/MainMenuPanelsRuntime/Settings_VolumeSlider_Background_Runtime.png";
        private const string SettingsSliderFillPath = "Assets/Art/UI/MainMenuPanelsRuntime/Settings_VolumeSlider_Fill_Runtime.png";
        private const string SettingsSliderKnobPath = "Assets/Art/UI/MainMenuPanelsRuntime/Settings_VolumeSlider_Handle_Runtime.png";
        private const string DefaultFontPath = "Assets/Art/Fonts/Galmuri11-Bold Dynamic.asset";

        [MenuItem("Cat Tennis/Rebuild/Create Main Menu Runtime Prefab")]
        public static void CreateMainMenuRuntimePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null &&
                !EditorUtility.DisplayDialog(
                    "Recreate Main Menu Runtime Prefab?",
                    "This will overwrite the editable MainMenuRuntime prefab and reset positions to builder defaults.",
                    "Recreate",
                    "Cancel"))
            {
                return;
            }

            ConfigureSpriteImporter(BackgroundPath, 2048, FilterMode.Bilinear);
            ConfigureSpriteImporter(CharacterPath, 2048, FilterMode.Bilinear);
            ConfigureSpriteImporter(TitlePath, 2048, FilterMode.Bilinear);
            ConfigureSpriteImporter(StartPath, 2048, FilterMode.Bilinear);
            ConfigureSpriteImporter(HowToPlayPath, 2048, FilterMode.Bilinear);
            ConfigureSpriteImporter(SettingsPath, 2048, FilterMode.Bilinear);
            ConfigureSpriteImporter(HowToPlayGuidePath, 2048, FilterMode.Bilinear);
            ConfigureSpriteImporter(HowToPlayGotItButtonPath, 2048, FilterMode.Bilinear);
            ConfigureSpriteImporter(HowToPlayMoveIconPath, 1024, FilterMode.Bilinear);
            ConfigureSpriteImporter(HowToPlayHitIconPath, 1024, FilterMode.Bilinear);
            ConfigureSpriteImporter(HowToPlaySmashIconPath, 1024, FilterMode.Bilinear);
            ConfigureSpriteImporter(SettingsPanelPath, 2048, FilterMode.Bilinear);
            ConfigureSpriteImporter(SettingsRowPath, 1024, FilterMode.Bilinear);
            ConfigureSpriteImporter(SettingsBgmIconPath, 1024, FilterMode.Bilinear);
            ConfigureSpriteImporter(SettingsSfxIconPath, 1024, FilterMode.Bilinear);
            ConfigureSpriteImporter(SettingsLanguageIconPath, 1024, FilterMode.Bilinear);
            ConfigureSpriteImporter(SettingsAccountIconPath, 1024, FilterMode.Bilinear);
            ConfigureSpriteImporter(SettingsArrowButtonPath, 1024, FilterMode.Bilinear);
            ConfigureSpriteImporter(SettingsSliderTrackPath, 1024, FilterMode.Bilinear);
            ConfigureSpriteImporter(SettingsSliderFillPath, 1024, FilterMode.Bilinear);
            ConfigureSpriteImporter(SettingsSliderKnobPath, 1024, FilterMode.Bilinear);
            AssetDatabase.Refresh();
            EnsurePrefabFolder();

            Sprite backgroundSprite = LoadRequired<Sprite>(BackgroundPath);
            Sprite characterSprite = LoadRequired<Sprite>(CharacterPath);
            Sprite titleSprite = LoadRequired<Sprite>(TitlePath);
            Sprite startSprite = LoadRequired<Sprite>(StartPath);
            Sprite howToPlaySprite = LoadRequired<Sprite>(HowToPlayPath);
            Sprite settingsSprite = LoadRequired<Sprite>(SettingsPath);
            Sprite howToPlayGuideSprite = LoadRequired<Sprite>(HowToPlayGuidePath);
            Sprite howToPlayGotItButtonSprite = LoadRequired<Sprite>(HowToPlayGotItButtonPath);
            Sprite howToPlayMoveIconSprite = LoadRequired<Sprite>(HowToPlayMoveIconPath);
            Sprite howToPlayHitIconSprite = LoadRequired<Sprite>(HowToPlayHitIconPath);
            Sprite howToPlaySmashIconSprite = LoadRequired<Sprite>(HowToPlaySmashIconPath);
            Sprite settingsPanelSprite = LoadRequired<Sprite>(SettingsPanelPath);
            Sprite settingsRowSprite = LoadRequired<Sprite>(SettingsRowPath);
            Sprite settingsBgmIconSprite = LoadRequired<Sprite>(SettingsBgmIconPath);
            Sprite settingsSfxIconSprite = LoadRequired<Sprite>(SettingsSfxIconPath);
            Sprite settingsLanguageIconSprite = LoadRequired<Sprite>(SettingsLanguageIconPath);
            Sprite settingsAccountIconSprite = LoadRequired<Sprite>(SettingsAccountIconPath);
            Sprite settingsArrowButtonSprite = LoadRequired<Sprite>(SettingsArrowButtonPath);
            Sprite settingsSliderTrackSprite = LoadRequired<Sprite>(SettingsSliderTrackPath);
            Sprite settingsSliderFillSprite = LoadRequired<Sprite>(SettingsSliderFillPath);
            Sprite settingsSliderKnobSprite = LoadRequired<Sprite>(SettingsSliderKnobPath);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultFontPath);

            GameObject root = new GameObject("MainMenuRuntime");
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();
            MainMenuController controller = root.AddComponent<MainMenuController>();

            Image background = CreateImage("Background", root.transform, backgroundSprite, false);
            RectTransform backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
            backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
            backgroundRect.pivot = new Vector2(0.5f, 0.5f);
            backgroundRect.anchoredPosition = Vector2.zero;
            backgroundRect.sizeDelta = new Vector2(1920f, 1080f);
            AspectRatioFitter backgroundFitter = background.gameObject.AddComponent<AspectRatioFitter>();
            backgroundFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            backgroundFitter.aspectRatio = backgroundSprite.rect.width / backgroundSprite.rect.height;

            GameObject mainPanel = CreateFullPanel("MainPanel", root.transform);
            Image title = CreateImage("Title", mainPanel.transform, titleSprite, false);
            ConfigureRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(330f, -70f), new Vector2(540f, 363f));

            Image character = CreateImage("MainCharacter", mainPanel.transform, characterSprite, false);
            ConfigureRect(character.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-560f, 70f), new Vector2(430f, 523f));

            Button startButton = CreateButton("StartButton", mainPanel.transform, startSprite,
                new Vector2(330f, -115f), new Vector2(430f, 198f));
            TMP_Text startLabel = CreateText("StartLabel", startButton.transform, font, "START", 42f,
                Vector2.zero, new Vector2(340f, 60f), new Color32(74, 66, 53, 255));

            Button howToPlayButton = CreateButton("HowToPlayButton", mainPanel.transform, howToPlaySprite,
                new Vector2(115f, -390f), new Vector2(320f, 91f));
            TMP_Text howToPlayLabel = CreateText("HowToPlayLabel", howToPlayButton.transform, font, "HOW TO PLAY", 24f,
                Vector2.zero, new Vector2(280f, 42f), new Color32(74, 66, 53, 255));

            Button settingsButton = CreateButton("SettingsButton", mainPanel.transform, settingsSprite,
                new Vector2(545f, -390f), new Vector2(310f, 81f));
            TMP_Text settingsLabel = CreateText("SettingsLabel", settingsButton.transform, font, "SETTINGS", 28f,
                Vector2.zero, new Vector2(250f, 42f), new Color32(74, 66, 53, 255));

            GameObject howToPlayPanel = CreateFullPanel("HowToPlayPanel", root.transform);
            Image howToPlayGuide = CreateImage("GuidePanel", howToPlayPanel.transform, howToPlayGuideSprite, true);
            ConfigureRect(howToPlayGuide.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f), new Vector2(560f, 760f));

            CreateText("GuideTitleLabel", howToPlayPanel.transform, font, "HOW TO PLAY", 44f,
                new Vector2(0f, 285f), new Vector2(420f, 58f), new Color32(34, 103, 58, 255));

            Image moveRow = CreateImage("MoveRow", howToPlayPanel.transform, howToPlayMoveIconSprite, false);
            ConfigureRect(moveRow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 145f), new Vector2(420f, 139f));
            CreateGuideRowText(howToPlayPanel.transform, font, "1  MOVE\n<  LEFT\n>  RIGHT\n^  UP",
                new Vector2(118f, 145f), new Vector2(260f, 150f), 28f);

            Image hitRow = CreateImage("HitRow", howToPlayPanel.transform, howToPlayHitIconSprite, false);
            ConfigureRect(hitRow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -20f), new Vector2(420f, 145f));
            CreateGuideRowText(howToPlayPanel.transform, font, "2  HIT\nReturn the ball",
                new Vector2(118f, -20f), new Vector2(260f, 120f), 30f);

            Image smashRow = CreateImage("SmashRow", howToPlayPanel.transform, howToPlaySmashIconSprite, false);
            ConfigureRect(smashRow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -185f), new Vector2(420f, 143f));
            CreateGuideRowText(howToPlayPanel.transform, font, "3  SMASH\nPower shot",
                new Vector2(118f, -185f), new Vector2(260f, 120f), 30f);

            Button gotItButton = CreateButton("GotItButton", howToPlayPanel.transform, howToPlayGotItButtonSprite,
                new Vector2(0f, -325f), new Vector2(230f, 72f));
            TMP_Text gotItLabel = CreateText("GotItLabel", gotItButton.transform, font, "GOT IT!", 34f,
                Vector2.zero, new Vector2(190f, 46f), new Color32(248, 240, 210, 255));

            GameObject settingsPanel = CreateFullPanel("SettingsPanel", root.transform);
            Image settingsFrame = CreateImage("SettingsFrame", settingsPanel.transform, settingsPanelSprite, true);
            ConfigureRect(settingsFrame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f), new Vector2(565f, 748f));

            CreateText("SettingsTitleLabel", settingsPanel.transform, font, "SETTINGS", 44f,
                new Vector2(0f, 275f), new Vector2(390f, 64f), new Color32(74, 47, 32, 255));

            SettingsVolumePresenter settingsVolumePresenter = settingsPanel.AddComponent<SettingsVolumePresenter>();
            Slider bgmSlider = CreateSettingsSliderRow(settingsPanel.transform, font, "BGM", settingsRowSprite, settingsBgmIconSprite,
                settingsSliderTrackSprite, settingsSliderFillSprite, settingsSliderKnobSprite, new Vector2(0f, 145f));
            Slider sfxSlider = CreateSettingsSliderRow(settingsPanel.transform, font, "SFX", settingsRowSprite, settingsSfxIconSprite,
                settingsSliderTrackSprite, settingsSliderFillSprite, settingsSliderKnobSprite, new Vector2(0f, 35f));
            CreateSettingsNavigationRow(settingsPanel.transform, font, "LANGUAGE", settingsRowSprite, settingsLanguageIconSprite,
                settingsArrowButtonSprite, new Vector2(0f, -75f));
            CreateSettingsNavigationRow(settingsPanel.transform, font, "ACCOUNT", settingsRowSprite, settingsAccountIconSprite,
                settingsArrowButtonSprite, new Vector2(0f, -185f));

            Button settingsBackButton = CreateButton("SettingsBackButton", settingsPanel.transform, howToPlayGotItButtonSprite,
                new Vector2(0f, -330f), new Vector2(230f, 72f));
            TMP_Text settingsBackLabel = CreateText("SettingsBackLabel", settingsBackButton.transform, font, "BACK", 34f,
                Vector2.zero, new Vector2(190f, 46f), new Color32(248, 240, 210, 255));

            SerializedObject settingsVolumeSerialized = new SerializedObject(settingsVolumePresenter);
            settingsVolumeSerialized.FindProperty("bgmSlider").objectReferenceValue = bgmSlider;
            settingsVolumeSerialized.FindProperty("sfxSlider").objectReferenceValue = sfxSlider;
            settingsVolumeSerialized.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(startButton.onClick, controller.StartRally);
            UnityEventTools.AddPersistentListener(howToPlayButton.onClick, controller.ShowHowToPlay);
            UnityEventTools.AddPersistentListener(settingsButton.onClick, controller.ShowSettings);
            UnityEventTools.AddPersistentListener(gotItButton.onClick, controller.ShowMainMenu);
            UnityEventTools.AddPersistentListener(settingsBackButton.onClick, controller.ShowMainMenu);

            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("mainPanel").objectReferenceValue = mainPanel;
            serialized.FindProperty("modePanel").objectReferenceValue = null;
            serialized.FindProperty("howToPlayPanel").objectReferenceValue = howToPlayPanel;
            serialized.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            mainPanel.SetActive(true);
            howToPlayPanel.SetActive(false);
            settingsPanel.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Debug.Log($"Created Main Menu Runtime prefab: {PrefabPath}");

            if (!EditorApplication.isPlaying &&
                PrefabStageUtility.GetCurrentPrefabStage() == null &&
                SceneManager.GetActiveScene().name == "Rebuild_MainMenu")
            {
                InstallMainMenuRuntimeInOpenScene();
            }
        }

        [MenuItem("Cat Tennis/Rebuild/Open Main Menu Runtime Prefab For Editing")]
        public static void OpenMainMenuRuntimePrefabForEditing()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                CreateMainMenuRuntimePrefab();
            }

            AssetDatabase.OpenAsset(LoadRequired<GameObject>(PrefabPath));
        }

        [MenuItem("Cat Tennis/Rebuild/Install Main Menu Runtime In Open Scene")]
        public static void InstallMainMenuRuntimeInOpenScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Stop Play Mode before installing Main Menu Runtime into the open scene.");
                return;
            }

            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                Debug.LogError("Close Prefab Mode before installing Main Menu Runtime into the open scene.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                CreateMainMenuRuntimePrefab();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Scene targetScene = SceneManager.GetActiveScene();
            if (!targetScene.IsValid() || !targetScene.isLoaded)
            {
                Debug.LogError("Main Menu Runtime install failed: no valid active scene is open.");
                return;
            }

            int removedCount = RemoveExistingMainMenuObjects(targetScene);
            EnsureEventSystem(targetScene);

            GameObject prefab = LoadRequired<GameObject>(PrefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, targetScene);
            instance.name = "MainMenuRuntime";
            SetLaunchPanelVisibility(instance.transform);

            EditorSceneManager.MarkSceneDirty(targetScene);
            Selection.activeGameObject = instance;
            Debug.Log($"Installed Main Menu Runtime in the open scene. Removed previous menu objects: {removedCount}");
        }

        [MenuItem("Cat Tennis/Rebuild/Select Main Menu Runtime In Open Scene")]
        public static void SelectMainMenuRuntimeInOpenScene()
        {
            MainMenuController presenter = FindEditableController();
            if (presenter == null)
            {
                Debug.LogWarning("MainMenuRuntime was not found in the open scene.");
                return;
            }

            Selection.activeGameObject = presenter.gameObject;
            EditorGUIUtility.PingObject(presenter.gameObject);
        }

        [MenuItem("Cat Tennis/Rebuild/Preview Main Menu Main Panel")]
        public static void PreviewMainPanel()
        {
            SetPanelPreview("MainPanel");
        }

        [MenuItem("Cat Tennis/Rebuild/Preview Main Menu How To Play Panel")]
        public static void PreviewHowToPlayPanel()
        {
            SetPanelPreview("HowToPlayPanel");
        }

        [MenuItem("Cat Tennis/Rebuild/Preview Main Menu Settings Panel")]
        public static void PreviewSettingsPanel()
        {
            SetPanelPreview("SettingsPanel");
        }

        [MenuItem("Cat Tennis/Rebuild/Upgrade Main Menu Settings Sliders In Selection")]
        public static void UpgradeSettingsSlidersInSelection()
        {
            ConfigureSpriteImporter(SettingsSliderTrackPath, 1024, FilterMode.Bilinear);
            ConfigureSpriteImporter(SettingsSliderFillPath, 1024, FilterMode.Bilinear);
            ConfigureSpriteImporter(SettingsSliderKnobPath, 1024, FilterMode.Bilinear);
            AssetDatabase.Refresh();

            MainMenuController controller = FindEditableController();
            if (controller == null)
            {
                Debug.LogWarning("MainMenuRuntime was not found. Select the edited MainMenuRuntime or open the prefab first.");
                return;
            }

            Transform settingsPanel = controller.transform.Find("SettingsPanel");
            if (settingsPanel == null)
            {
                Debug.LogWarning("SettingsPanel was not found under MainMenuRuntime.");
                return;
            }

            SettingsVolumePresenter presenter = settingsPanel.GetComponent<SettingsVolumePresenter>();
            if (presenter == null)
            {
                presenter = settingsPanel.gameObject.AddComponent<SettingsVolumePresenter>();
            }

            Slider bgmSlider = UpgradeSettingsSliderRow(settingsPanel, "BGM");
            Slider sfxSlider = UpgradeSettingsSliderRow(settingsPanel, "SFX");

            SerializedObject serialized = new SerializedObject(presenter);
            serialized.FindProperty("bgmSlider").objectReferenceValue = bgmSlider;
            serialized.FindProperty("sfxSlider").objectReferenceValue = sfxSlider;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(settingsPanel.gameObject);
            if (controller.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            }

            Debug.Log("Upgraded Settings BGM/SFX rows to runtime sliders. Run Apply Selected Main Menu Layout To Prefab, then Install.");
        }

        [MenuItem("Cat Tennis/Rebuild/Apply Selected Main Menu Layout To Prefab")]
        public static void ApplySelectedMainMenuLayoutToPrefab()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Stop Play Mode before applying Main Menu layout changes to the prefab.");
                return;
            }

            MainMenuController sourceController = FindEditableController();
            if (sourceController == null)
            {
                Debug.LogWarning("MainMenuRuntime was not found. Select the MainMenuRuntime object you edited, then try again.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                Debug.LogWarning("MainMenuRuntime prefab does not exist yet. Create it first, then apply layout changes.");
                return;
            }

            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.assetPath == PrefabPath)
            {
                SetLaunchPanelVisibility(prefabStage.prefabContentsRoot.transform);
                PrefabUtility.SaveAsPrefabAsset(prefabStage.prefabContentsRoot, PrefabPath);
                EditorSceneManager.MarkSceneDirty(prefabStage.scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Applied Main Menu prefab editing changes to the prefab. Close Prefab Mode, then run Install Main Menu Runtime In Open Scene.");
                return;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                CopyEditableUiState(sourceController.transform, prefabRoot.transform);
                SetLaunchPanelVisibility(prefabRoot.transform);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Applied selected Main Menu layout/text changes to the prefab. Run Install Main Menu Runtime In Open Scene to reinstall this saved layout.");
        }

        [MenuItem("Cat Tennis/Rebuild/Fix Main Menu Rally Hit Area")]
        public static void FixMainMenuRallyHitArea()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Stop Play Mode before fixing Main Menu hit areas.");
                return;
            }

            MainMenuController controller = FindEditableController();
            if (controller == null)
            {
                Debug.LogWarning("MainMenuRuntime was not found. Select or install MainMenuRuntime first.");
                return;
            }

            Transform rallyButton = controller.transform.Find("ModePanel/RallyButton");
            if (rallyButton != null)
            {
                RectTransform rallyRect = rallyButton as RectTransform;
                if (rallyRect != null)
                {
                    Vector2 size = rallyRect.sizeDelta;
                    rallyRect.sizeDelta = new Vector2(Mathf.Max(size.x, 420f), Mathf.Max(size.y, 430f));
                    EditorUtility.SetDirty(rallyRect);
                }

                Image rallyImage = rallyButton.GetComponent<Image>();
                if (rallyImage != null)
                {
                    rallyImage.raycastTarget = true;
                    EditorUtility.SetDirty(rallyImage);
                }
            }

            if (controller.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            }

            EditorUtility.SetDirty(controller);
            Debug.Log("Fixed Main Menu RallyButton hit area. Apply Selected Main Menu Layout To Prefab, then reinstall to persist this.");
        }

        [MenuItem("Cat Tennis/Rebuild/Restore Main Menu Back Button")]
        public static void RestoreMainMenuBackButton()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Stop Play Mode before restoring Main Menu BackButton.");
                return;
            }

            MainMenuController controller = FindEditableController();
            if (controller == null)
            {
                Debug.LogWarning("MainMenuRuntime was not found. Select or install MainMenuRuntime first.");
                return;
            }

            Transform backButton = controller.transform.Find("ModePanel/BackButton");
            if (backButton == null)
            {
                Debug.LogWarning("BackButton was not found under ModePanel. Recreate the Main Menu prefab if you deleted it from the hierarchy.");
                return;
            }

            backButton.gameObject.SetActive(true);
            EditorUtility.SetDirty(backButton.gameObject);

            if (controller.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            }

            EditorUtility.SetDirty(controller);
            Debug.Log("Restored Main Menu BackButton. Apply Selected Main Menu Layout To Prefab, then reinstall to persist this.");
        }

        [MenuItem("Cat Tennis/Rebuild/Fix Main Menu Empty Click Areas")]
        public static void FixMainMenuEmptyClickAreas()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Stop Play Mode before fixing Main Menu empty click areas.");
                return;
            }

            MainMenuController controller = FindEditableController();
            if (controller == null)
            {
                Debug.LogWarning("MainMenuRuntime was not found. Select or install MainMenuRuntime first.");
                return;
            }

            NormalizeButtonHitArea(controller.transform.Find("ModePanel/RallyButton"));
            NormalizeButtonHitArea(controller.transform.Find("ModePanel/BackButton"));
            DisableButtonIfPresent(controller.transform.Find("ModePanel/TournamentButton"));

            if (controller.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            }

            EditorUtility.SetDirty(controller);
            Debug.Log("Fixed Main Menu empty click areas. Empty mode-screen clicks now do nothing. Apply Selected Main Menu Layout To Prefab, then reinstall to persist this.");
        }

        [MenuItem("Cat Tennis/Rebuild/Normalize Main Menu UI Scales")]
        public static void NormalizeMainMenuUiScales()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Stop Play Mode before normalizing Main Menu UI scales.");
                return;
            }

            MainMenuController controller = FindEditableController();
            if (controller == null)
            {
                Debug.LogWarning("MainMenuRuntime was not found. Select or install MainMenuRuntime first.");
                return;
            }

            NormalizeRectScale(controller.transform.Find("MainPanel/Title"), true);
            NormalizeRectScale(controller.transform.Find("MainPanel/StartButton"), true);
            NormalizeRectScale(controller.transform.Find("MainPanel/HowToPlayButton"), true);
            NormalizeRectScale(controller.transform.Find("MainPanel/SettingsButton"), true);
            NormalizeRectScale(controller.transform.Find("ModePanel/ModeTitle"), true);
            NormalizeRectScale(controller.transform.Find("ModePanel/RallyButton"), true);
            NormalizeRectScale(controller.transform.Find("ModePanel/BackButton"), true);
            ResetMainMenuRuntimeSizes(controller.transform);

            DisableButtonIfPresent(controller.transform.Find("ModePanel/TournamentButton"));

            if (controller.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            }

            EditorUtility.SetDirty(controller);
            Debug.Log("Normalized Main Menu UI scales. Visual size is now stored in RectTransform sizes/font sizes instead of local scale. Apply Selected Main Menu Layout To Prefab, then reinstall.");
        }

        private static void NormalizeMainMenuHierarchyScales(Transform root)
        {
            NormalizeRectScale(root.Find("MainPanel/Title"), true);
            NormalizeRectScale(root.Find("MainPanel/StartButton"), true);
            NormalizeRectScale(root.Find("MainPanel/HowToPlayButton"), true);
            NormalizeRectScale(root.Find("MainPanel/SettingsButton"), true);
            NormalizeRectScale(root.Find("ModePanel/ModeTitle"), true);
            NormalizeRectScale(root.Find("ModePanel/RallyButton"), true);
            NormalizeRectScale(root.Find("ModePanel/BackButton"), true);
            ResetMainMenuRuntimeSizes(root);
        }

        private static void ResetMainMenuRuntimeSizes(Transform root)
        {
            SetRectSize(root.Find("MainPanel/Title"), new Vector2(540f, 363f));
            SetRectSize(root.Find("MainPanel/StartButton"), new Vector2(430f, 198f));
            SetRectSize(root.Find("MainPanel/StartButton/StartLabel"), new Vector2(340f, 60f));
            SetTextSize(root.Find("MainPanel/StartButton/StartLabel"), 42f);

            SetRectSize(root.Find("MainPanel/HowToPlayButton"), new Vector2(320f, 91f));
            SetRectSize(root.Find("MainPanel/HowToPlayButton/HowToPlayLabel"), new Vector2(280f, 42f));
            SetTextSize(root.Find("MainPanel/HowToPlayButton/HowToPlayLabel"), 24f);

            SetRectSize(root.Find("MainPanel/SettingsButton"), new Vector2(310f, 81f));
            SetRectSize(root.Find("MainPanel/SettingsButton/SettingsLabel"), new Vector2(250f, 42f));
            SetTextSize(root.Find("MainPanel/SettingsButton/SettingsLabel"), 28f);

            SetRectSize(root.Find("ModePanel/ModeTitle"), new Vector2(470f, 316f));
            SetRectSize(root.Find("ModePanel/RallyButton"), new Vector2(290f, 435f));
            SetRectSize(root.Find("ModePanel/RallyButton/RallyLabel"), new Vector2(240f, 58f));
            SetTextSize(root.Find("ModePanel/RallyButton/RallyLabel"), 36f);

            SetRectSize(root.Find("ModePanel/BackButton"), new Vector2(250f, 80f));
            SetRectSize(root.Find("ModePanel/BackButton/BackLabel"), new Vector2(180f, 42f));
            SetTextSize(root.Find("ModePanel/BackButton/BackLabel"), 28f);
        }

        private static void SetRectSize(Transform target, Vector2 size)
        {
            RectTransform rect = target as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.localScale = Vector3.one;
            rect.sizeDelta = size;
            EditorUtility.SetDirty(rect);
        }

        private static void SetTextSize(Transform target, float fontSize)
        {
            TMP_Text text = target != null ? target.GetComponent<TMP_Text>() : null;
            if (text == null)
            {
                return;
            }

            text.fontSize = fontSize;
            text.fontSizeMin = Mathf.Min(text.fontSizeMin, fontSize);
            text.fontSizeMax = Mathf.Max(text.fontSizeMax, fontSize);
            EditorUtility.SetDirty(text);
        }

        private static void NormalizeRectScale(Transform target, bool includeChildren)
        {
            if (target == null)
            {
                return;
            }

            Vector3 oldScale = target.localScale;
            RectTransform rect = target as RectTransform;
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(rect.sizeDelta.x * oldScale.x, rect.sizeDelta.y * oldScale.y);
                rect.localScale = Vector3.one;
                EditorUtility.SetDirty(rect);
            }

            TMP_Text text = target.GetComponent<TMP_Text>();
            if (text != null)
            {
                float textScale = Mathf.Max(Mathf.Abs(oldScale.x), Mathf.Abs(oldScale.y));
                text.fontSize *= textScale;
                EditorUtility.SetDirty(text);
            }

            if (!includeChildren)
            {
                return;
            }

            for (int i = 0; i < target.childCount; i++)
            {
                Transform child = target.GetChild(i);
                Vector3 childScale = child.localScale;
                RectTransform childRect = child as RectTransform;
                if (childRect != null)
                {
                    childRect.anchoredPosition = new Vector2(
                        childRect.anchoredPosition.x * oldScale.x,
                        childRect.anchoredPosition.y * oldScale.y);
                    childRect.sizeDelta = new Vector2(
                        childRect.sizeDelta.x * oldScale.x * childScale.x,
                        childRect.sizeDelta.y * oldScale.y * childScale.y);
                    childRect.localScale = Vector3.one;
                    EditorUtility.SetDirty(childRect);
                }

                TMP_Text childText = child.GetComponent<TMP_Text>();
                if (childText != null)
                {
                    float combinedTextScale = Mathf.Max(
                        Mathf.Abs(oldScale.x * childScale.x),
                        Mathf.Abs(oldScale.y * childScale.y));
                    childText.fontSize *= combinedTextScale;
                    EditorUtility.SetDirty(childText);
                }
            }
        }

        private static void NormalizeButtonHitArea(Transform target)
        {
            if (target == null)
            {
                return;
            }

            RectTransform rect = target as RectTransform;
            if (rect != null)
            {
                rect.localScale = Vector3.one;
                EditorUtility.SetDirty(rect);
            }

            Image image = target.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
                EditorUtility.SetDirty(image);
            }

            Button button = target.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = true;
                EditorUtility.SetDirty(button);
            }
        }

        private static void DisableButtonIfPresent(Transform target)
        {
            if (target == null)
            {
                return;
            }

            Button button = target.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = false;
                button.onClick.RemoveAllListeners();
                EditorUtility.SetDirty(button);
            }

            Image image = target.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = false;
                EditorUtility.SetDirty(image);
            }
        }

        private static GameObject CreateFullPanel(string name, Transform parent)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return panel;
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

        private static TMP_Text CreateGuideRowText(
            Transform parent,
            TMP_FontAsset font,
            string value,
            Vector2 position,
            Vector2 rectSize,
            float size)
        {
            TMP_Text text = CreateText("GuideRowLabel", parent, font, value, size, position, rectSize,
                new Color32(34, 103, 58, 255));
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.lineSpacing = -8f;
            EditorUtility.SetDirty(text);
            return text;
        }

        private static Slider CreateSettingsSliderRow(
            Transform parent,
            TMP_FontAsset font,
            string label,
            Sprite rowSprite,
            Sprite iconSprite,
            Sprite sliderTrackSprite,
            Sprite sliderFillSprite,
            Sprite sliderKnobSprite,
            Vector2 position)
        {
            Transform row = CreateSettingsRowRoot(label + "Row", parent, rowSprite, position);
            Slider slider = row.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.value = 1f;
            slider.direction = Slider.Direction.LeftToRight;

            Image icon = CreateImage(label + "Icon", row, iconSprite, false);
            ConfigureRect(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-175f, 0f), new Vector2(54f, 54f));

            TMP_Text rowLabel = CreateText(label + "Label", row, font, label, 34f,
                new Vector2(-72f, 0f), new Vector2(140f, 52f), new Color32(74, 47, 32, 255));
            rowLabel.alignment = TextAlignmentOptions.MidlineLeft;

            ConfigureSettingsSliderVisuals(row, slider, label, sliderTrackSprite, sliderFillSprite, sliderKnobSprite);
            return slider;
        }

        private static Slider UpgradeSettingsSliderRow(Transform settingsPanel, string label)
        {
            Transform row = settingsPanel.Find(label + "Row");
            if (row == null)
            {
                Debug.LogWarning($"{label}Row was not found under SettingsPanel.");
                return null;
            }

            Slider slider = row.GetComponent<Slider>();
            if (slider == null)
            {
                slider = row.gameObject.AddComponent<Slider>();
            }

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;

            if (slider.value <= 0f)
            {
                slider.value = 1f;
            }

            Sprite sliderTrackSprite = LoadRequired<Sprite>(SettingsSliderTrackPath);
            Sprite sliderFillSprite = LoadRequired<Sprite>(SettingsSliderFillPath);
            Sprite sliderKnobSprite = LoadRequired<Sprite>(SettingsSliderKnobPath);

            ConfigureSettingsSliderVisuals(row, slider, label, sliderTrackSprite, sliderFillSprite, sliderKnobSprite);
            EditorUtility.SetDirty(slider);
            return slider;
        }

        private static void ConfigureSettingsSliderVisuals(
            Transform row,
            Slider slider,
            string label,
            Sprite sliderTrackSprite,
            Sprite sliderFillSprite,
            Sprite sliderKnobSprite)
        {
            slider.value = Mathf.Clamp01(slider.value);
            DestroySettingsSliderVisuals(row, label);

            RectTransform sliderArea = CreateRectTransform(label + "SliderArea", row);
            ConfigureRect(sliderArea, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(112f, 0f), new Vector2(190f, 42f));

            Image background = CreateSliderImage(label + "SliderBackground", sliderArea, sliderTrackSprite, false);
            StretchRect(background.rectTransform, new Vector2(0f, 8f), new Vector2(0f, -8f));

            RectTransform valueArea = CreateRectTransform(label + "ValueArea", sliderArea);
            StretchRect(valueArea, new Vector2(14f, 0f), new Vector2(-14f, 0f));

            RectTransform fillArea = CreateRectTransform(label + "FillArea", valueArea);
            StretchRect(fillArea, new Vector2(0f, 12f), new Vector2(0f, -12f));

            Image fill = CreateSliderImage(label + "SliderFill", fillArea, sliderFillSprite, false);
            StretchRect(fill.rectTransform, Vector2.zero, Vector2.zero);

            RectTransform handleArea = CreateRectTransform(label + "HandleArea", valueArea);
            StretchRect(handleArea, Vector2.zero, Vector2.zero);

            Image handle = CreateSliderImage(label + "SliderHandle", handleArea, sliderKnobSprite, true);
            ConfigureRect(handle.rectTransform, new Vector2(Mathf.Clamp01(slider.value), 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 64f));

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;

            EditorUtility.SetDirty(sliderArea);
            EditorUtility.SetDirty(background);
            EditorUtility.SetDirty(valueArea);
            EditorUtility.SetDirty(fillArea);
            EditorUtility.SetDirty(fill);
            EditorUtility.SetDirty(handleArea);
            EditorUtility.SetDirty(handle);
            EditorUtility.SetDirty(slider);
        }

        private static void DestroySettingsSliderVisuals(Transform row, string label)
        {
            string[] names =
            {
                label + "SliderArea",
                label + "ValueArea",
                label + "SliderBackground",
                label + "FillArea",
                label + "SliderFill",
                label + "HandleArea",
                label + "SliderHandle",
                label + "SliderTrack",
                label + "SliderKnob"
            };

            for (int i = 0; i < names.Length; i++)
            {
                DestroyDescendantsByName(row, names[i]);
            }
        }

        private static Image CreateSliderImage(string name, Transform parent, Sprite sprite, bool raycastTarget)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            Image image = obj.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = raycastTarget;
            image.preserveAspect = false;
            return image;
        }

        private static void DestroyChildIfExists(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void DestroyDescendantsByName(Transform root, string name)
        {
            RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
            for (int i = rects.Length - 1; i >= 0; i--)
            {
                RectTransform rect = rects[i];
                if (rect.transform == root || rect.name != name)
                {
                    continue;
                }

                Object.DestroyImmediate(rect.gameObject);
            }
        }

        private static RectTransform FindOrCreateChildRect(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                RectTransform existing = child as RectTransform;
                if (existing != null)
                {
                    return existing;
                }
            }

            RectTransform created = CreateRectTransform(name, parent);
            EditorUtility.SetDirty(created);
            return created;
        }

        private static RectTransform FindDescendantRect(Transform root, string name)
        {
            Transform direct = root.Find(name);
            if (direct != null)
            {
                return direct as RectTransform;
            }

            RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < rects.Length; i++)
            {
                if (rects[i].name == name)
                {
                    return rects[i];
                }
            }

            return null;
        }

        private static void CreateSettingsNavigationRow(
            Transform parent,
            TMP_FontAsset font,
            string label,
            Sprite rowSprite,
            Sprite iconSprite,
            Sprite arrowButtonSprite,
            Vector2 position)
        {
            Transform row = CreateSettingsRowRoot(label + "Row", parent, rowSprite, position);

            Image icon = CreateImage(label + "Icon", row, iconSprite, false);
            ConfigureRect(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-178f, 0f), new Vector2(54f, 54f));

            TMP_Text rowLabel = CreateText(label + "Label", row, font, label, 32f,
                new Vector2(-8f, 0f), new Vector2(245f, 52f), new Color32(74, 47, 32, 255));
            rowLabel.alignment = TextAlignmentOptions.MidlineLeft;

            Image arrow = CreateImage(label + "ArrowButton", row, arrowButtonSprite, true);
            ConfigureRect(arrow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(185f, 0f), new Vector2(52f, 52f));
        }

        private static Transform CreateSettingsRowRoot(string name, Transform parent, Sprite rowSprite, Vector2 position)
        {
            Image rowImage = CreateImage(name, parent, rowSprite, true);
            ConfigureRect(rowImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                position, new Vector2(445f, 90f));
            return rowImage.transform;
        }

        private static RectTransform CreateRectTransform(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            return obj.AddComponent<RectTransform>();
        }

        private static void ConfigureRect(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void StretchRect(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetPanelPreview(string visiblePanelName)
        {
            MainMenuController controller = FindEditableController();
            if (controller == null)
            {
                Debug.LogWarning("MainMenuRuntime was not found. Open the prefab for editing or install it in Rebuild_MainMenu first.");
                return;
            }

            SerializedObject serialized = new SerializedObject(controller);
            GameObject mainPanel = serialized.FindProperty("mainPanel").objectReferenceValue as GameObject;
            GameObject modePanel = serialized.FindProperty("modePanel").objectReferenceValue as GameObject;
            GameObject howToPlayPanel = serialized.FindProperty("howToPlayPanel").objectReferenceValue as GameObject;
            GameObject settingsPanel = serialized.FindProperty("settingsPanel").objectReferenceValue as GameObject;

            SetPreviewObjectActive(mainPanel, visiblePanelName == "MainPanel");
            SetPreviewObjectActive(modePanel, visiblePanelName == "ModePanel");
            SetPreviewObjectActive(howToPlayPanel, visiblePanelName == "HowToPlayPanel");
            SetPreviewObjectActive(settingsPanel, visiblePanelName == "SettingsPanel");

            EditorUtility.SetDirty(controller);
            if (controller.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            }

            Selection.activeGameObject = visiblePanelName switch
            {
                "MainPanel" when mainPanel != null => mainPanel,
                "ModePanel" when modePanel != null => modePanel,
                "HowToPlayPanel" when howToPlayPanel != null => howToPlayPanel,
                "SettingsPanel" when settingsPanel != null => settingsPanel,
                _ => controller.gameObject
            };
            EditorGUIUtility.PingObject(Selection.activeGameObject);
        }

        private static void SetPreviewObjectActive(GameObject target, bool active)
        {
            if (target == null)
            {
                return;
            }

            target.SetActive(active);
            EditorUtility.SetDirty(target);
        }

        private static void SetLaunchPanelVisibility(Transform root)
        {
            Transform mainPanel = root.Find("MainPanel");
            Transform modePanel = root.Find("ModePanel");
            Transform howToPlayPanel = root.Find("HowToPlayPanel");
            Transform settingsPanel = root.Find("SettingsPanel");

            if (mainPanel != null)
            {
                mainPanel.gameObject.SetActive(true);
                EditorUtility.SetDirty(mainPanel.gameObject);
            }

            if (modePanel != null)
            {
                modePanel.gameObject.SetActive(false);
                EditorUtility.SetDirty(modePanel.gameObject);
            }

            if (howToPlayPanel != null)
            {
                howToPlayPanel.gameObject.SetActive(false);
                EditorUtility.SetDirty(howToPlayPanel.gameObject);
            }

            if (settingsPanel != null)
            {
                settingsPanel.gameObject.SetActive(false);
                EditorUtility.SetDirty(settingsPanel.gameObject);
            }
        }

        private static MainMenuController FindEditableController()
        {
            if (Selection.activeGameObject != null)
            {
                MainMenuController selected = Selection.activeGameObject.GetComponentInParent<MainMenuController>(true);
                if (selected != null)
                {
                    return selected;
                }
            }

            MainMenuController[] controllers = Object.FindObjectsOfType<MainMenuController>(true);
            return controllers.Length > 0 ? controllers[0] : null;
        }

        private static void CopyEditableUiState(Transform sourceRoot, Transform targetRoot)
        {
            CopyEditableUiObject(sourceRoot, targetRoot);
            RemoveTargetChildrenMissingInSource(sourceRoot, targetRoot);

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

        private static void RemoveTargetChildrenMissingInSource(Transform sourceRoot, Transform targetRoot)
        {
            for (int i = targetRoot.childCount - 1; i >= 0; i--)
            {
                Transform targetChild = targetRoot.GetChild(i);
                if (sourceRoot.Find(targetChild.name) != null)
                {
                    continue;
                }

                Object.DestroyImmediate(targetChild.gameObject);
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

            Slider sourceSlider = source.GetComponent<Slider>();
            if (sourceSlider != null)
            {
                Slider targetSlider = target.GetComponent<Slider>();
                if (targetSlider == null)
                {
                    targetSlider = target.gameObject.AddComponent<Slider>();
                }

                targetSlider.minValue = sourceSlider.minValue;
                targetSlider.maxValue = sourceSlider.maxValue;
                targetSlider.wholeNumbers = sourceSlider.wholeNumbers;
                targetSlider.direction = sourceSlider.direction;
                targetSlider.value = sourceSlider.value;
                targetSlider.interactable = sourceSlider.interactable;
                targetSlider.transition = sourceSlider.transition;
                targetSlider.fillRect = ResolveTargetRect(source, target, sourceSlider.fillRect);
                targetSlider.handleRect = ResolveTargetRect(source, target, sourceSlider.handleRect);
                targetSlider.targetGraphic = targetSlider.handleRect != null
                    ? targetSlider.handleRect.GetComponent<Graphic>()
                    : null;
                EditorUtility.SetDirty(targetSlider);
            }

            SettingsVolumePresenter sourceVolumePresenter = source.GetComponent<SettingsVolumePresenter>();
            if (sourceVolumePresenter != null)
            {
                SettingsVolumePresenter targetVolumePresenter = target.GetComponent<SettingsVolumePresenter>();
                if (targetVolumePresenter == null)
                {
                    targetVolumePresenter = target.gameObject.AddComponent<SettingsVolumePresenter>();
                }

                SerializedObject sourceSerialized = new SerializedObject(sourceVolumePresenter);
                SerializedObject targetSerialized = new SerializedObject(targetVolumePresenter);
                targetSerialized.FindProperty("bgmSlider").objectReferenceValue =
                    ResolveTargetComponent<Slider>(source, target,
                        sourceSerialized.FindProperty("bgmSlider").objectReferenceValue as Component);
                targetSerialized.FindProperty("sfxSlider").objectReferenceValue =
                    ResolveTargetComponent<Slider>(source, target,
                        sourceSerialized.FindProperty("sfxSlider").objectReferenceValue as Component);
                targetSerialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(targetVolumePresenter);
            }
        }

        private static RectTransform ResolveTargetRect(Transform sourceRoot, Transform targetRoot, RectTransform sourceRect)
        {
            if (sourceRect == null)
            {
                return null;
            }

            return ResolveTargetComponent<RectTransform>(sourceRoot, targetRoot, sourceRect);
        }

        private static T ResolveTargetComponent<T>(Transform sourceRoot, Transform targetRoot, Component sourceComponent)
            where T : Component
        {
            if (sourceComponent == null)
            {
                return null;
            }

            string relativePath = GetRelativePath(sourceRoot, sourceComponent.transform);
            Transform targetTransform = string.IsNullOrEmpty(relativePath)
                ? targetRoot
                : targetRoot.Find(relativePath);
            return targetTransform != null ? targetTransform.GetComponent<T>() : null;
        }

        private static string GetRelativePath(Transform root, Transform target)
        {
            if (root == target)
            {
                return string.Empty;
            }

            string path = target.name;
            Transform current = target.parent;
            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return current == root ? path : string.Empty;
        }

        private static int RemoveExistingMainMenuObjects(Scene scene)
        {
            int removedCount = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (!IsMainMenuRuntimeRoot(root) &&
                    root.GetComponentInChildren<MainMenuController>(true) == null &&
                    root.GetComponentInChildren<Canvas>(true) == null)
                {
                    continue;
                }

                Object.DestroyImmediate(root);
                removedCount++;
            }

            return removedCount;
        }

        private static bool IsMainMenuRuntimeRoot(GameObject root)
        {
            if (root == null)
            {
                return false;
            }

            if (root.name == "MainMenuRuntime" || root.name.StartsWith("MainMenuRuntime ("))
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

        private static void EnsureEventSystem(Scene scene)
        {
            if (FindInScene<EventSystem>(scene) != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            SceneManager.MoveGameObjectToScene(eventSystem, scene);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
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

        private static void ConfigureSpriteImporter(string path, int maxSize, FilterMode filterMode)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            TextureImporterSettings spriteSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(spriteSettings);
            spriteSettings.spriteMeshType = SpriteMeshType.FullRect;
            spriteSettings.spriteExtrude = 4;
            importer.SetTextureSettings(spriteSettings);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = filterMode;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
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
