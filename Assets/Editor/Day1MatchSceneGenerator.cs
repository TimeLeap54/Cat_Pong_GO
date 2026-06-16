using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditorInternal;
using UnityEngine.UI;

public static class Day1MatchSceneGenerator
{
    private const string ScenesPath = "Assets/Scenes";
    private const string GeneratedPath = "Assets/Generated";

    private static Sprite squareSprite;
    private static Sprite circleSprite;

    [MenuItem("Tools/CatPong/Generate Match Scene")]
    public static void Generate()
    {
        Directory.CreateDirectory(ScenesPath);
        Directory.CreateDirectory(GeneratedPath);

        EnsureProjectIs2DURP();
        EnsureSprites();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Match";

        CreateTournamentManager();
        CreateCamera();
        CreateCourt();
        var playerSetup = CreatePlayer();
        var opponentSetup = CreateOpponent();
        var ball = CreateBall();
        var playerSpawn = CreateMarker("PlayerSpawn", new Vector2(-5.5f, -0.2f));
        var opponentSpawn = CreateMarker("OpponentSpawn", new Vector2(5.5f, -0.2f));
        var canvas = CreateHud();
        ConnectServeReferences(playerSetup.controller, ball, playerSetup.serveHoldPoint);
        var matchManager = CreateMatchManager(playerSetup, opponentSetup, playerSpawn, opponentSpawn, ball, canvas.scoreUI);
        CreateGoalZone("LeftScoreZone", new Vector2(-5.05f, -1.2f), new Vector2(10.1f, 2.2f), true, matchManager);
        CreateGoalZone("RightScoreZone", new Vector2(5.05f, -1.2f), new Vector2(10.1f, 2.2f), false, matchManager);

        EditorSceneManager.SaveScene(scene, $"{ScenesPath}/Match.unity");
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene($"{ScenesPath}/Match.unity", true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("CatPong", "Match scene generated at Assets/Scenes/Match.unity", "OK");
    }

    private static void EnsureProjectIs2DURP()
    {
        if (GraphicsSettings.renderPipelineAsset == null)
        {
            Debug.LogWarning("URP asset is not assigned. This project should use the existing 2D URP template settings.");
        }
    }

    private static void CreateCamera()
    {
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 2.2f, -10f);

        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.2f;
        camera.backgroundColor = new Color(0.52f, 0.78f, 0.92f);
        cameraObject.AddComponent<UniversalAdditionalCameraData>();
    }

    private static void CreateCourt()
    {
        CreateBox("Court", new Vector2(0f, -1.1f), new Vector2(18f, 0.32f), new Color(0.18f, 0.48f, 0.34f), true, "Ground");
        CreateBox("Net", new Vector2(0f, 0.05f), new Vector2(0.14f, 2.3f), new Color(0.96f, 0.96f, 0.9f), true, "Net");
        CreateBoundary("BoundaryTop", new Vector2(0f, 5.7f), new Vector2(20f, 0.25f));
        CreateBoundary("BoundaryLeft", new Vector2(-10.6f, 2.2f), new Vector2(0.25f, 7f));
        CreateBoundary("BoundaryRight", new Vector2(10.6f, 2.2f), new Vector2(0.25f, 7f));
    }

    private static PlayerSetup CreatePlayer()
    {
        var player = CreateBox("Player", new Vector2(-5.5f, -0.2f), new Vector2(0.9f, 1.45f), new Color(1f, 0.62f, 0.28f), true);
        var body = player.AddComponent<Rigidbody2D>();
        body.freezeRotation = true;
        body.gravityScale = 1.75f;

        var swingPoint = new GameObject("SwingPoint").transform;
        swingPoint.SetParent(player.transform);
        swingPoint.localPosition = new Vector3(0.72f, 0.3f, 0f);

        var serveHoldPoint = new GameObject("ServeHoldPoint").transform;
        serveHoldPoint.SetParent(player.transform);
        serveHoldPoint.localPosition = new Vector3(0.58f, 0.22f, 0f);

        var groundCheck = new GameObject("GroundCheck").transform;
        groundCheck.SetParent(player.transform);
        groundCheck.localPosition = new Vector3(0f, -0.78f, 0f);

        var controller = player.AddComponent<PlayerController>();
        SetReference(controller, "swingPoint", swingPoint);
        SetReference(controller, "groundCheck", groundCheck);
        SetInt(controller, "groundLayer", LayerMask.GetMask("Default"));
        return new PlayerSetup(player, controller, serveHoldPoint);
    }

    private static OpponentSetup CreateOpponent()
    {
        var opponent = CreateBox("Opponent", new Vector2(5.5f, -0.2f), new Vector2(0.9f, 1.45f), new Color(0.35f, 0.55f, 0.95f), true);
        var body = opponent.AddComponent<Rigidbody2D>();
        body.freezeRotation = true;
        body.gravityScale = 1.75f;

        var swingPoint = new GameObject("OpponentSwingPoint").transform;
        swingPoint.SetParent(opponent.transform);
        swingPoint.localPosition = new Vector3(-0.72f, 0.3f, 0f);

        var ai = opponent.AddComponent<OpponentAI>();
        SetReference(ai, "swingPoint", swingPoint);
        return new OpponentSetup(opponent, ai);
    }

    private static BallController CreateBall()
    {
        var ball = new GameObject("Ball");
        ball.transform.position = new Vector3(-2.5f, 1.5f, 0f);
        ball.transform.localScale = Vector3.one * 0.34f;

        var renderer = ball.AddComponent<SpriteRenderer>();
        renderer.sprite = circleSprite;
        renderer.color = new Color(1f, 0.9f, 0.08f);

        var body = ball.AddComponent<Rigidbody2D>();
        body.gravityScale = 1.0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        ball.AddComponent<CircleCollider2D>();
        return ball.AddComponent<BallController>();
    }

    private static GameObject CreateBox(string name, Vector2 position, Vector2 size, Color color, bool collider, string tag = null)
    {
        var obj = new GameObject(name);
        obj.transform.position = new Vector3(position.x, position.y, 0f);
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);

        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = squareSprite;
        renderer.color = color;

        if (collider)
        {
            obj.AddComponent<BoxCollider2D>();
        }

        if (!string.IsNullOrEmpty(tag))
        {
            EnsureTag(tag);
            obj.tag = tag;
        }

        return obj;
    }

    private static GameObject CreateMarker(string name, Vector2 position)
    {
        var marker = new GameObject(name);
        marker.transform.position = new Vector3(position.x, position.y, 0f);
        return marker;
    }

    private static void CreateGoalZone(string name, Vector2 position, Vector2 size, bool playerSide, MatchManager matchManager)
    {
        var zone = new GameObject(name);
        zone.transform.position = new Vector3(position.x, position.y, 0f);
        var collider = zone.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.isTrigger = true;
        var goalZone = zone.AddComponent<GoalZone>();
        goalZone.Init(matchManager, playerSide);
    }

    private static void ConnectServeReferences(PlayerController player, BallController ball, Transform serveAnchor)
    {
        SetReference(player, "serveBall", ball);
        SetReference(ball, "serveAnchor", serveAnchor);
    }

    private static void CreateTournamentManager()
    {
        var obj = new GameObject("TournamentManager");
        obj.AddComponent<TournamentManager>().StartTournament();
    }

    private static MatchManager CreateMatchManager(PlayerSetup playerSetup, OpponentSetup opponentSetup, GameObject playerSpawn, GameObject opponentSpawn, BallController ball, ScoreUI scoreUI)
    {
        var obj = new GameObject("MatchManager");
        var manager = obj.AddComponent<MatchManager>();
        SetReference(manager, "ball", ball);
        SetReference(manager, "scoreUI", scoreUI);
        SetReference(manager, "player", playerSetup.player.transform);
        SetReference(manager, "opponent", opponentSetup.opponent.transform);
        SetReference(manager, "playerSpawn", playerSpawn.transform);
        SetReference(manager, "opponentSpawn", opponentSpawn.transform);
        SetReference(manager, "serveAnchor", playerSetup.serveHoldPoint);
        SetReference(manager, "opponentAI", opponentSetup.ai);
        return manager;
    }

    private static HudReferences CreateHud()
    {
        var canvasObject = new GameObject("Canvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        var scoreText = CreateText("ScoreText", canvasObject.transform, "0 : 0", new Vector2(0f, -45f), 42, TextAnchor.MiddleCenter);
        scoreText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        scoreText.rectTransform.anchorMax = new Vector2(0.5f, 1f);

        var roundText = CreateText("RoundText", canvasObject.transform, "Round 1: Rookie Cat", new Vector2(0f, -92f), 22, TextAnchor.MiddleCenter);
        roundText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        roundText.rectTransform.anchorMax = new Vector2(0.5f, 1f);

        var helpText = CreateText("HelpText", canvasObject.transform, "Serve: WASD aim, J lift, K spike", new Vector2(0f, -132f), 18, TextAnchor.MiddleCenter);
        helpText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        helpText.rectTransform.anchorMax = new Vector2(0.5f, 1f);

        var resultPanel = CreatePanel("ResultPanel", canvasObject.transform, new Vector2(0f, 0f), new Vector2(380f, 245f));
        var resultText = CreateText("ResultText", resultPanel.transform, "You Win!", new Vector2(0f, 70f), 32, TextAnchor.MiddleCenter);
        var nextButton = CreateButton("NextButton", resultPanel.transform, "Next Round", new Vector2(0f, -8f), new Vector2(190f, 46f));
        var restartButton = CreateButton("RestartButton", resultPanel.transform, "Restart", new Vector2(0f, -70f), new Vector2(190f, 46f));

        var scoreUI = canvasObject.AddComponent<ScoreUI>();
        SetReference(scoreUI, "scoreText", scoreText);
        SetReference(scoreUI, "roundText", roundText);
        SetReference(scoreUI, "resultPanel", resultPanel);
        SetReference(scoreUI, "resultText", resultText);
        SetReference(scoreUI, "nextButton", nextButton);
        SetReference(scoreUI, "restartButton", restartButton);

        return new HudReferences(scoreUI);
    }

    private static Text CreateText(string name, Transform parent, string text, Vector2 position, int fontSize, TextAnchor anchor)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var uiText = obj.AddComponent<Text>();
        uiText.text = text;
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiText.fontSize = fontSize;
        uiText.color = Color.white;
        uiText.alignment = anchor;
        uiText.rectTransform.anchoredPosition = position;
        uiText.rectTransform.sizeDelta = new Vector2(460f, 70f);
        return uiText;
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var image = obj.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.66f);
        image.rectTransform.anchoredPosition = position;
        image.rectTransform.sizeDelta = size;
        return obj;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size)
    {
        var obj = CreatePanel(name, parent, position, size);
        var image = obj.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.92f);
        var button = obj.AddComponent<Button>();
        button.targetGraphic = image;

        var text = CreateText("Text", obj.transform, label, Vector2.zero, 22, TextAnchor.MiddleCenter);
        text.color = Color.black;
        text.rectTransform.sizeDelta = size;
        return button;
    }

    private static GameObject CreateBoundary(string name, Vector2 position, Vector2 size)
    {
        var obj = new GameObject(name);
        obj.transform.position = new Vector3(position.x, position.y, 0f);
        var collider = obj.AddComponent<BoxCollider2D>();
        collider.size = size;
        return obj;
    }

    private static void EnsureSprites()
    {
        squareSprite = CreateSpriteAsset($"{GeneratedPath}/square.png", false);
        circleSprite = CreateSpriteAsset($"{GeneratedPath}/circle.png", true);
    }

    private static Sprite CreateSpriteAsset(string path, bool circle)
    {
        if (!File.Exists(path))
        {
            var texture = new Texture2D(64, 64);
            var pixels = new Color[64 * 64];
            for (var y = 0; y < 64; y++)
            {
                for (var x = 0; x < 64; x++)
                {
                    var dx = x - 31.5f;
                    var dy = y - 31.5f;
                    var inside = !circle || dx * dx + dy * dy <= 31.5f * 31.5f;
                    pixels[y * 64 + x] = inside ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 64f;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void EnsureTag(string tag)
    {
        foreach (var existingTag in InternalEditorUtility.tags)
        {
            if (existingTag == tag)
            {
                return;
            }
        }

        InternalEditorUtility.AddTag(tag);
    }

    private static void SetReference(Object target, string fieldName, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(fieldName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetInt(Object target, string fieldName, int value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(fieldName).intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private readonly struct HudReferences
    {
        public readonly ScoreUI scoreUI;

        public HudReferences(ScoreUI scoreUI)
        {
            this.scoreUI = scoreUI;
        }
    }

    private readonly struct PlayerSetup
    {
        public readonly GameObject player;
        public readonly PlayerController controller;
        public readonly Transform serveHoldPoint;

        public PlayerSetup(GameObject player, PlayerController controller, Transform serveHoldPoint)
        {
            this.player = player;
            this.controller = controller;
            this.serveHoldPoint = serveHoldPoint;
        }
    }

    private readonly struct OpponentSetup
    {
        public readonly GameObject opponent;
        public readonly OpponentAI ai;

        public OpponentSetup(GameObject opponent, OpponentAI ai)
        {
            this.opponent = opponent;
            this.ai = ai;
        }
    }
}
