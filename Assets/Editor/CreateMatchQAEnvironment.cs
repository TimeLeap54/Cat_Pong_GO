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
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace CatTennis.Rebuild.EditorTools
{
    public static class CreateMatchQAEnvironment
    {
        private const string SettingsFolder = "Assets/Settings/Match";
        private const string VisualPrefabFolder = "Assets/Art/Prefabs";
        private const string MainMenuRuntimeUiFolder = "Assets/Art/UI/MainMenuRuntime";
        private const string MainMenuAtlasPath = "Assets/Art/UI/MainMenuRuntime.spriteatlas";
        private const string MainMenuPath = "Assets/Scenes/Rebuild_MainMenu.unity";
        private const string MatchPath = "Assets/Scenes/Rebuild_Match.unity";
        private const string AiSettingsFolder = "Assets/Settings/AI";
        private const string PlayerVisualPrefabPath = VisualPrefabFolder + "/PlayerVisual.prefab";
        private const string OpponentVisualPrefabPath = VisualPrefabFolder + "/OpponentVisual.prefab";
        private const string BallVisualPrefabPath = VisualPrefabFolder + "/BallVisual.prefab";
        private const string NetVisualPrefabPath = VisualPrefabFolder + "/NetVisual.prefab";
        private const string BackgroundVisualPrefabPath = VisualPrefabFolder + "/BackgroundVisual.prefab";

        [MenuItem("Cat Tennis/Rebuild/Create Match QA Environment")]
        public static void Create()
        {
            EnsureFolder("Assets/Settings");
            EnsureFolder(SettingsFolder);
            EnsureFolder(VisualPrefabFolder);
            EnsureFolder(AiSettingsFolder);
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
            AIBalanceConfig rookieAi=LoadOrCreate<AIBalanceConfig>($"{AiSettingsFolder}/AI_Rookie.asset");
            AIBalanceConfig dojoAi=LoadOrCreate<AIBalanceConfig>($"{AiSettingsFolder}/AI_Dojo.asset");
            AIBalanceConfig masterAi=LoadOrCreate<AIBalanceConfig>($"{AiSettingsFolder}/AI_Master.asset");
            rookieAi.Configure(3.4f,0.32f,5f,72f,14f,6f,8f);
            dojoAi.Configure(4.5f,0.16f,5f,50f,22f,12f,16f);
            masterAi.Configure(5.4f,0.07f,5f,28f,32f,18f,22f);
            EditorUtility.SetDirty(geometry);
            EditorUtility.SetDirty(loop);
            EditorUtility.SetDirty(playerConfig);
            EditorUtility.SetDirty(shotConfig);
            EditorUtility.SetDirty(rookieAi);EditorUtility.SetDirty(dojoAi);EditorUtility.SetDirty(masterAi);
            AssetDatabase.SaveAssets();

            PrepareMainMenuUiAssets();
            CreateVisualPrefabsIfMissing();

            CreateMainMenuScene(false);
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
            dojoAi=AssetDatabase.LoadAssetAtPath<AIBalanceConfig>($"{AiSettingsFolder}/AI_Dojo.asset");
            CreateMatchScene(physics, geometry, loop, playerConfig, shotConfig,dojoAi);
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Created production MainMenu and Match QA scenes.");
        }

        [MenuItem("Cat Tennis/Rebuild/Rebuild Main Menu UI")]
        public static void RebuildMainMenuUi()
        {
            PrepareMainMenuUiAssets();
            CreateMainMenuScene(!Application.isBatchMode);
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Rebuilt MainMenu UI scene.");
        }

        [MenuItem("Cat Tennis/Rebuild/Prepare Main Menu UI Assets")]
        public static void PrepareMainMenuUiAssets()
        {
            EnsureFolder(MainMenuRuntimeUiFolder);

            TrimTransparentUiSprite("Assets/Art/UI/Asset14.png", $"{MainMenuRuntimeUiFolder}/Logo.png", 12);
            TrimTransparentUiSprite("Assets/Art/UI/Main_Character.png", $"{MainMenuRuntimeUiFolder}/MainCharacter.png", 12);
            TrimTransparentUiSprite("Assets/Art/UI/Start.png", $"{MainMenuRuntimeUiFolder}/Start.png", 12);
            TrimTransparentUiSprite("Assets/Art/UI/HowtoPlay.png", $"{MainMenuRuntimeUiFolder}/HowtoPlay.png", 12);
            TrimTransparentUiSprite("Assets/Art/UI/Settings.png", $"{MainMenuRuntimeUiFolder}/Settings.png", 12);
            TrimTransparentUiSprite("Assets/Art/UI/Tournament.png", $"{MainMenuRuntimeUiFolder}/Tournament.png", 12);
            TrimTransparentUiSprite("Assets/Art/UI/Rally.png", $"{MainMenuRuntimeUiFolder}/Rally.png", 12);
            TrimTransparentUiSprite("Assets/Art/UI/Asset18 (1).png", $"{MainMenuRuntimeUiFolder}/SelectMode.png", 12);
            TrimTransparentUiSprite("Assets/Art/UI/Asset19 (1).png", $"{MainMenuRuntimeUiFolder}/Back.png", 12);

            AssetDatabase.Refresh();
            ConfigureUiTextureImporter("Assets/Art/UI/Main_Background.png", 2048);
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { MainMenuRuntimeUiFolder }))
            {
                ConfigureUiTextureImporter(AssetDatabase.GUIDToAssetPath(guid), 1024);
            }

            CreateOrUpdateMainMenuAtlas();
            AssetDatabase.SaveAssets();
            Debug.Log("Prepared runtime MainMenu UI assets.");
        }

        private static void CreateMainMenuScene(bool additive)
        {
            Scene previousActiveScene = EditorSceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                additive ? NewSceneMode.Additive : NewSceneMode.Single);
            EditorSceneManager.SetActiveScene(scene);
            CreateCamera(new Color(0.25f, 0.65f, 0.88f));
            CreateEventSystem();

            MainMenuController controller = new GameObject("MainMenuController").AddComponent<MainMenuController>();
            Canvas canvas = CreateMainMenuCanvas();

            Sprite background = LoadRequired<Sprite>("Assets/Art/UI/Main_Background.png");
            Sprite logo = LoadRequired<Sprite>($"{MainMenuRuntimeUiFolder}/Logo.png");
            Sprite character = LoadRequired<Sprite>($"{MainMenuRuntimeUiFolder}/MainCharacter.png");
            Sprite start = LoadRequired<Sprite>($"{MainMenuRuntimeUiFolder}/Start.png");
            Sprite howToPlay = LoadRequired<Sprite>($"{MainMenuRuntimeUiFolder}/HowtoPlay.png");
            Sprite settings = LoadRequired<Sprite>($"{MainMenuRuntimeUiFolder}/Settings.png");
            Sprite tournament = LoadRequired<Sprite>($"{MainMenuRuntimeUiFolder}/Tournament.png");
            Sprite rally = LoadRequired<Sprite>($"{MainMenuRuntimeUiFolder}/Rally.png");
            Sprite selectMode = LoadRequired<Sprite>($"{MainMenuRuntimeUiFolder}/SelectMode.png");
            Sprite back = LoadRequired<Sprite>($"{MainMenuRuntimeUiFolder}/Back.png");

            Image backgroundImage = CreateImage("Background", canvas.transform, background);
            backgroundImage.preserveAspect = true;
            RectTransform backgroundRect = backgroundImage.rectTransform;
            backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
            backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
            backgroundRect.sizeDelta = new Vector2(1920f, 1080f);
            AspectRatioFitter backgroundFitter = backgroundImage.gameObject.AddComponent<AspectRatioFitter>();
            backgroundFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            backgroundFitter.aspectRatio = background.rect.width / background.rect.height;

            GameObject mainPanel = CreatePanel("MainPanel", canvas.transform);
            CreateImage("Logo", mainPanel.transform, logo, new Vector2(0f, 260f), new Vector2(820f, 476f));
            CreateImage("Character", mainPanel.transform, character, new Vector2(-560f, -250f), new Vector2(420f, 511f));
            Button startButton = CreateSpriteButton("StartButton", mainPanel.transform, start, new Vector2(0f, -140f), new Vector2(620f, 300f));
            CreateSpriteButton("HowToPlayButton", mainPanel.transform, howToPlay, new Vector2(-260f, -405f), new Vector2(390f, 117f));
            CreateSpriteButton("SettingsButton", mainPanel.transform, settings, new Vector2(260f, -405f), new Vector2(390f, 135f));
            UnityEventTools.AddPersistentListener(startButton.onClick, controller.ShowModeSelection);

            GameObject modePanel = CreatePanel("ModePanel", canvas.transform);
            CreateImage("Logo", modePanel.transform, logo, new Vector2(0f, 285f), new Vector2(760f, 441f));
            CreateImage("SelectModeLabel", modePanel.transform, selectMode, new Vector2(0f, 55f), new Vector2(420f, 124f));
            Button tournamentButton = CreateSpriteButton("TournamentButton", modePanel.transform, tournament, new Vector2(-260f, -210f), new Vector2(310f, 449f));
            Button rallyButton = CreateSpriteButton("RallyButton", modePanel.transform, rally, new Vector2(260f, -210f), new Vector2(303f, 449f));
            Button backButton = CreateSpriteButton("BackButton", modePanel.transform, back, new Vector2(0f, -455f), new Vector2(300f, 97f));
            UnityEventTools.AddPersistentListener(tournamentButton.onClick, controller.StartTournament);
            UnityEventTools.AddPersistentListener(rallyButton.onClick, controller.StartRally);
            UnityEventTools.AddPersistentListener(backButton.onClick, controller.ShowMainMenu);
            modePanel.SetActive(false);

            SerializedObject controllerSerialized = new SerializedObject(controller);
            controllerSerialized.FindProperty("mainPanel").objectReferenceValue = mainPanel;
            controllerSerialized.FindProperty("modePanel").objectReferenceValue = modePanel;
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, MainMenuPath);
            if (additive)
            {
                if (previousActiveScene.IsValid())
                {
                    EditorSceneManager.SetActiveScene(previousActiveScene);
                }

                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void TrimTransparentUiSprite(string sourcePath, string outputPath, int padding)
        {
            string fullSourcePath = Path.GetFullPath(sourcePath);
            Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!source.LoadImage(File.ReadAllBytes(fullSourcePath)))
            {
                throw new InvalidOperationException($"Could not read UI texture: {sourcePath}");
            }

            Color32[] pixels = source.GetPixels32();
            int minX = source.width;
            int minY = source.height;
            int maxX = -1;
            int maxY = -1;
            for (int y = 0; y < source.height; y++)
            {
                for (int x = 0; x < source.width; x++)
                {
                    if (pixels[y * source.width + x].a == 0)
                    {
                        continue;
                    }

                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            if (maxX < minX || maxY < minY)
            {
                UnityEngine.Object.DestroyImmediate(source);
                throw new InvalidOperationException($"UI texture is fully transparent: {sourcePath}");
            }

            minX = Mathf.Max(0, minX - padding);
            minY = Mathf.Max(0, minY - padding);
            maxX = Mathf.Min(source.width - 1, maxX + padding);
            maxY = Mathf.Min(source.height - 1, maxY + padding);
            int width = maxX - minX + 1;
            int height = maxY - minY + 1;

            Texture2D trimmed = new Texture2D(width, height, TextureFormat.RGBA32, false);
            trimmed.SetPixels(source.GetPixels(minX, minY, width, height));
            trimmed.Apply();
            File.WriteAllBytes(Path.GetFullPath(outputPath), trimmed.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(trimmed);
            UnityEngine.Object.DestroyImmediate(source);
        }

        private static void ConfigureUiTextureImporter(string path, int maxSize)
        {
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                throw new InvalidOperationException($"Expected a texture importer at {path}.");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = maxSize;

            SetPlatformTextureSettings(importer, "Standalone", maxSize);
            SetPlatformTextureSettings(importer, "Android", maxSize);
            SetPlatformTextureSettings(importer, "iPhone", maxSize);
            importer.SaveAndReimport();
        }

        private static void SetPlatformTextureSettings(TextureImporter importer, string platform, int maxSize)
        {
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
            settings.overridden = true;
            settings.maxTextureSize = maxSize;
            settings.format = TextureImporterFormat.Automatic;
            settings.textureCompression = TextureImporterCompression.Compressed;
            settings.compressionQuality = 50;
            importer.SetPlatformTextureSettings(settings);
        }

        private static void CreateOrUpdateMainMenuAtlas()
        {
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(MainMenuAtlasPath);
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, MainMenuAtlasPath);
            }

            SpriteAtlasPackingSettings packing = atlas.GetPackingSettings();
            packing.enableRotation = false;
            packing.enableTightPacking = true;
            packing.padding = 4;
            atlas.SetPackingSettings(packing);

            SpriteAtlasTextureSettings texture = atlas.GetTextureSettings();
            texture.generateMipMaps = false;
            texture.filterMode = FilterMode.Bilinear;
            texture.sRGB = true;
            atlas.SetTextureSettings(texture);

            TextureImporterPlatformSettings platform = atlas.GetPlatformSettings("DefaultTexturePlatform");
            platform.maxTextureSize = 2048;
            platform.format = TextureImporterFormat.Automatic;
            platform.textureCompression = TextureImporterCompression.Compressed;
            atlas.SetPlatformSettings(platform);

            UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MainMenuRuntimeUiFolder);
            atlas.Remove(atlas.GetPackables());
            atlas.Add(new[] { folder });
            EditorUtility.SetDirty(atlas);
            SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
        }

        private static Canvas CreateMainMenuCanvas()
        {
            GameObject canvasObject = new GameObject("Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static GameObject CreatePanel(string name, Transform parent)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return panel;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite)
        {
            GameObject imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateImage(
            string name,
            Transform parent,
            Sprite sprite,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            Image image = CreateImage(name, parent, sprite);
            image.preserveAspect = true;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return image;
        }

        private static Button CreateSpriteButton(
            string name,
            Transform parent,
            Sprite sprite,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            Image image = CreateImage(name, parent, sprite, anchoredPosition, size);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static void CreateMatchScene(
            BallPhysicsConfig physics,
            CourtGeometryConfig geometry,
            Phase3PointLoopConfig loop,
            PlayerControlConfig playerConfig,
            ShotBalanceConfig shotConfig,AIBalanceConfig aiConfig)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(Color.black);
            CreateEventSystem();
            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            GameObject court = new GameObject("Court");
            Sprite backgroundSprite = LoadRequired<Sprite>("Assets/Art/Environment/Map1.png");
            float backgroundScale = 20f / backgroundSprite.bounds.size.x;
            InstantiateVisual(BackgroundVisualPrefabPath, "BackgroundVisual", court.transform,
                new Vector3(0f, 3f, 0f), Vector3.one * backgroundScale);
            GameObject ground = CreateMarker("Ground", new Vector2(0f, -0.25f),
                new Vector2(20f, 0.5f), Color.clear, sprite, court.transform);
            ground.layer = 10;
            BoxCollider2D groundCollider = ground.AddComponent<BoxCollider2D>();
            groundCollider.size = sprite.bounds.size;
            InstantiateVisual(NetVisualPrefabPath, "NetVisual", court.transform,
                new Vector3(0f, 0.5f, 0f), Vector3.one * 0.52f);

            GameObject opponent = new GameObject("Opponent");
            opponent.transform.position = new Vector3(5f, 0.75f, 0f);
            opponent.layer=8; opponent.SetActive(false);
            Rigidbody2D opponentBody=opponent.AddComponent<Rigidbody2D>();
            opponentBody.gravityScale=playerConfig.GravityScale;
            opponentBody.constraints=RigidbodyConstraints2D.FreezeRotation;
            CapsuleCollider2D opponentCollider=opponent.AddComponent<CapsuleCollider2D>();
            opponentCollider.size=new Vector2(.8f,1.4f);
            opponentCollider.excludeLayers|=1<<playerConfig.BallLayer;
            opponent.AddComponent<CatMotor>();
            OpponentHitDetector opponentHit=opponent.AddComponent<OpponentHitDetector>();
            OpponentAIController opponentAI=opponent.AddComponent<OpponentAIController>();
            GameObject opponentVisual=InstantiateVisual(OpponentVisualPrefabPath, "VisualRoot", opponent.transform,
                new Vector3(0f, -0.19f, 0f), Vector3.one);

            GameObject ballObject = new GameObject("Ball");
            ballObject.transform.position = loop.ResetPosition;
            InstantiateVisual(BallVisualPrefabPath, "VisualRoot", ballObject.transform,
                Vector3.zero, Vector3.one * 0.3f);
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
            Rigidbody2D playerBody = player.AddComponent<Rigidbody2D>();
            playerBody.bodyType = RigidbodyType2D.Dynamic;
            playerBody.gravityScale = playerConfig.GravityScale;
            playerBody.constraints = RigidbodyConstraints2D.FreezeRotation;
            CapsuleCollider2D playerCollider = player.AddComponent<CapsuleCollider2D>();
            playerCollider.size = new Vector2(0.8f, 1.4f);
            PlayerInputReader input = player.AddComponent<PlayerInputReader>();
            PlayerHitDetector hitDetector = player.AddComponent<PlayerHitDetector>();
            hitDetector.Initialize(ball, playerConfig, shotConfig);
            PlayerCatController playerController = player.AddComponent<PlayerCatController>();
            SerializedObject playerSerialized = new SerializedObject(playerController);
            playerSerialized.FindProperty("inputReader").objectReferenceValue = input;
            playerSerialized.FindProperty("hitDetector").objectReferenceValue = hitDetector;
            playerSerialized.FindProperty("config").objectReferenceValue = playerConfig;
            playerSerialized.ApplyModifiedPropertiesWithoutUndo();
            playerCollider.excludeLayers |= 1 << playerConfig.BallLayer;
            GameObject playerVisual = InstantiateVisual(
                PlayerVisualPrefabPath,
                "VisualRoot",
                player.transform,
                new Vector3(0f, -0.19f, 0f),
                Vector3.one);
            playerVisual.GetComponent<CatAnimationPresenter>().Configure(playerController);
            PlayerManualHitboxController manualHitboxes =
                CreateManualHitboxRig(player, hitDetector, playerConfig);
            playerSerialized = new SerializedObject(playerController);
            playerSerialized.FindProperty("manualHitboxes").objectReferenceValue = manualHitboxes;
            playerSerialized.ApplyModifiedPropertiesWithoutUndo();

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
            ServeFlowController serveFlow = systems.AddComponent<ServeFlowController>();
            OpponentServeFlowController opponentServeFlow = systems.AddComponent<OpponentServeFlowController>();
            ShotExecutionController shotExecutor=systems.AddComponent<ShotExecutionController>();
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
            shotBridgeSerialized.FindProperty("ballPhysicsConfig").objectReferenceValue = physics;
            shotBridgeSerialized.FindProperty("courtGeometryConfig").objectReferenceValue = geometry;
            shotBridgeSerialized.FindProperty("shotExecutionController").objectReferenceValue = shotExecutor;
            shotBridgeSerialized.ApplyModifiedPropertiesWithoutUndo();
            serveFlow.Configure(ball, playerController, hitDetector, shotConfig);
            opponentServeFlow.Configure(ball, opponentAI, shotConfig);
            shotExecutor.Configure(pointBridge,shotConfig,physics,geometry);
            opponentHit.Configure(ball,playerConfig,shotExecutor);
            opponentAI.Configure(ball,physics,geometry,playerConfig,aiConfig,rally,opponentHit, playerController);
            opponentVisual.GetComponent<CatAnimationPresenter>().Configure(opponentAI);
            pointBridge.SetServeFlow(serveFlow);
            pointBridge.SetOpponentServeFlow(opponentServeFlow);
            pointBridge.SetShotExecutor(shotExecutor);
            pointBridge.SetOpponentReset(opponentAI,new Vector2(5f,.75f));
            bootstrapper.Configure(
                physics, geometry, loop, playerConfig, shotConfig,aiConfig,
                validator, lifecycle, detector, rally, match, reset, pointBridge,
                ballPhysics, ball, input, hitDetector, playerController, shotBridge, serveFlow,
                opponentServeFlow,
                shotExecutor,opponentAI,opponentHit,new Vector2(5f,.75f),
                playerCollider, groundCollider);
            Phase3DebugHUD hud = systems.AddComponent<Phase3DebugHUD>();
            hud.Configure(match, rally, pointBridge);
            hud.ConfigurePlayer(playerController);
            hud.ConfigureNavigation(bootstrapper);

            player.SetActive(true);
            opponent.SetActive(true);
            systems.SetActive(true);
            EditorSceneManager.SaveScene(scene, MatchPath);
        }

        private static PlayerManualHitboxController CreateManualHitboxRig(
            GameObject player,
            PlayerHitDetector hitDetector,
            PlayerControlConfig playerConfig)
        {
            PlayerManualHitboxController controller =
                player.AddComponent<PlayerManualHitboxController>();

            GameObject root = new GameObject("ManualHitboxes");
            root.transform.SetParent(player.transform, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localScale = Vector3.one;

            ManualHitboxTrigger normal = CreateManualHitbox(
                "NormalHitbox",
                ManualHitboxKind.Normal,
                root.transform,
                playerConfig.PlayerBodyLayer,
                new Vector2(0.45f, 0.35f),
                new Vector2(0.7f, 0.6f));
            ManualHitboxTrigger smash = CreateManualHitbox(
                "SmashHitbox",
                ManualHitboxKind.Smash,
                root.transform,
                playerConfig.PlayerBodyLayer,
                new Vector2(0.35f, 0.95f),
                new Vector2(0.85f, 0.75f));
            controller.Configure(normal, smash, hitDetector, root.transform);
            hitDetector.SetManualHitboxController(controller);
            return controller;
        }

        private static ManualHitboxTrigger CreateManualHitbox(
            string name,
            ManualHitboxKind kind,
            Transform parent,
            int layer,
            Vector2 localPosition,
            Vector2 size)
        {
            GameObject hitbox = new GameObject(name);
            hitbox.layer = layer;
            hitbox.transform.SetParent(parent, false);
            hitbox.transform.localPosition = localPosition;
            BoxCollider2D box = hitbox.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = size;
            box.enabled = false;
            ManualHitboxTrigger trigger = hitbox.AddComponent<ManualHitboxTrigger>();
            trigger.Configure(kind, null);
            return trigger;
        }

        private static Transform CreateAnchor(string name, Transform parent, Vector2 localPosition)
        {
            GameObject anchor = new GameObject(name);
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = localPosition;
            return anchor.transform;
        }

        private static void CreateCamera(Color background)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            camera.backgroundColor = background;
            cameraObject.AddComponent<AudioListener>();
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

        private static void CreateVisualPrefabsIfMissing()
        {
            CreateCharacterVisualPrefabIfMissing(
                PlayerVisualPrefabPath,
                "Assets/Art/Characters/Player/cat_tennis_idle_6f.png",
                "Assets/Art/Animations/Player/PlayerCat.controller");
            CreateCharacterVisualPrefabIfMissing(
                OpponentVisualPrefabPath,
                "Assets/Art/Characters/Opponent/opponent_cat_idle_6f.png",
                "Assets/Art/Animations/Opponent/OpponentCat.controller");
            CreateSpriteVisualPrefabIfMissing(
                BallVisualPrefabPath,
                "Assets/Art/Gameplay/Ball_64.png",
                4);
            CreateSpriteVisualPrefabIfMissing(
                NetVisualPrefabPath,
                "Assets/Art/Environment/Net_64.png",
                3);
            CreateSpriteVisualPrefabIfMissing(
                BackgroundVisualPrefabPath,
                "Assets/Art/Environment/Map1.png",
                -10);
            AssetDatabase.SaveAssets();
        }

        private static void CreateCharacterVisualPrefabIfMissing(
            string prefabPath,
            string spriteSheetPath,
            string controllerPath)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                return;
            }

            Sprite sprite = LoadFirstSprite(spriteSheetPath);
            RuntimeAnimatorController controller = LoadRequired<RuntimeAnimatorController>(controllerPath);
            GameObject visual = new GameObject("VisualRoot");
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 2;
            Animator animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            visual.AddComponent<CatAnimationPresenter>();
            PrefabUtility.SaveAsPrefabAsset(visual, prefabPath);
            UnityEngine.Object.DestroyImmediate(visual);
        }

        private static void CreateSpriteVisualPrefabIfMissing(
            string prefabPath,
            string spritePath,
            int sortingOrder)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                return;
            }

            GameObject visual = new GameObject("VisualRoot");
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadRequired<Sprite>(spritePath);
            renderer.sortingOrder = sortingOrder;
            PrefabUtility.SaveAsPrefabAsset(visual, prefabPath);
            UnityEngine.Object.DestroyImmediate(visual);
        }

        private static GameObject InstantiateVisual(
            string prefabPath,
            string instanceName,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale)
        {
            GameObject prefab = LoadRequired<GameObject>(prefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = instanceName;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = localScale;
            return instance;
        }

        private static Sprite LoadFirstSprite(string path)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            Sprite selected = null;
            foreach (UnityEngine.Object asset in assets)
            {
                if (asset is Sprite sprite &&
                    (selected == null || string.CompareOrdinal(sprite.name, selected.name) < 0))
                {
                    selected = sprite;
                }
            }

            if (selected == null)
            {
                throw new InvalidOperationException($"No sprites found at {path}.");
            }

            return selected;
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"Required visual asset is missing: {path}");
            }

            return asset;
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
