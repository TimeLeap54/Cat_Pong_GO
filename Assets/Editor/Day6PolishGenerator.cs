using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Day6PolishGenerator
{
    private const string ScenesPath = "Assets/Scenes";
    private const string GeneratedPath = "Assets/Generated";
    private const string AudioPath = "Assets/Generated/Audio";

    private static Sprite playerCatSprite;
    private static Sprite opponentCatSprite;
    private static Sprite ballSprite;
    private static Sprite netSprite;
    private static Sprite courtSprite;
    private static Sprite panelSprite;
    private static Sprite startButtonSprite;
    private static Sprite restartButtonSprite;
    private static AudioClip hitClip;
    private static AudioClip scoreClip;
    private static AudioClip buttonClip;
    private static AudioClip winClip;
    private static AudioClip loseClip;
    private static AudioClip bgmClip;

    [MenuItem("Tools/CatPong/Generate Day 6 Polish")]
    public static void Generate()
    {
        Directory.CreateDirectory(ScenesPath);
        Directory.CreateDirectory(GeneratedPath);
        Directory.CreateDirectory(AudioPath);

        EnsureSprites();
        EnsureAudioClips();
        Day1MatchSceneGenerator.Generate();
        PolishMatchScene();
        CreateMainMenuScene();
        ConfigureBuildScenes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog("CatPong", "Day 6 polish generated.", "OK");
        }
    }

    private static void EnsureSprites()
    {
        playerCatSprite = CreateSpriteAsset($"{GeneratedPath}/player_cat_idle.png", texture => PaintCat(texture, new Color(1f, 0.58f, 0.24f), new Color(1f, 0.9f, 0.72f)));
        opponentCatSprite = CreateSpriteAsset($"{GeneratedPath}/opponent_cat_idle.png", texture => PaintCat(texture, new Color(0.34f, 0.52f, 0.94f), new Color(0.78f, 0.88f, 1f)));
        ballSprite = CreateSpriteAsset($"{GeneratedPath}/tennis_ball.png", PaintBall);
        netSprite = CreateSpriteAsset($"{GeneratedPath}/tennis_net.png", PaintNet);
        courtSprite = CreateSpriteAsset($"{GeneratedPath}/court_background.png", PaintCourt);
        panelSprite = CreateSpriteAsset($"{GeneratedPath}/ui_score_panel.png", texture => PaintPanel(texture, new Color(0.08f, 0.12f, 0.13f, 0.82f)));
        startButtonSprite = CreateSpriteAsset($"{GeneratedPath}/button_start.png", texture => PaintPanel(texture, new Color(1f, 0.84f, 0.32f, 0.96f)));
        restartButtonSprite = CreateSpriteAsset($"{GeneratedPath}/button_restart.png", texture => PaintPanel(texture, new Color(0.86f, 0.92f, 1f, 0.96f)));
    }

    private static void EnsureAudioClips()
    {
        hitClip = CreateTone($"{AudioPath}/hit_clip.asset", "hit_clip", 660f, 0.08f, 0.5f);
        scoreClip = CreateTone($"{AudioPath}/score_clip.asset", "score_clip", 880f, 0.18f, 0.42f);
        buttonClip = CreateTone($"{AudioPath}/button_clip.asset", "button_clip", 520f, 0.07f, 0.32f);
        winClip = CreateTone($"{AudioPath}/win_clip.asset", "win_clip", 740f, 0.4f, 0.38f);
        loseClip = CreateTone($"{AudioPath}/lose_clip.asset", "lose_clip", 220f, 0.36f, 0.32f);
        bgmClip = CreateLoop($"{AudioPath}/bgm_loop.asset", "bgm_loop");
    }

    private static void PolishMatchScene()
    {
        var scene = EditorSceneManager.OpenScene($"{ScenesPath}/Match.unity", OpenSceneMode.Single);

        ApplySprite("Player", playerCatSprite, new Vector3(1.15f, 1.55f, 1f), Color.white, 2);
        ApplySprite("Opponent", opponentCatSprite, new Vector3(1.15f, 1.55f, 1f), Color.white, 2);
        ApplySprite("Ball", ballSprite, Vector3.one * 0.42f, Color.white, 4);
        ApplySprite("Net", netSprite, new Vector3(0.22f, 2.45f, 1f), Color.white, 3);
        ApplySprite("GroundBase", courtSprite, new Vector3(24f, 0.7f, 1f), Color.white, -1);
        ApplySprite("CourtInBounds", panelSprite, new Vector3(17.2f, 0.12f, 1f), new Color(0.2f, 0.62f, 0.38f), 0);

        EnsureCourtBackdrop();
        EnsureAudioManager();
        PolishHud();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void CreateMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainMenu";

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.2f;
        camera.backgroundColor = new Color(0.48f, 0.74f, 0.9f);
        cameraObject.AddComponent<AudioListener>();

        var backdrop = new GameObject("MenuCourtBackdrop");
        backdrop.transform.position = new Vector3(0f, -0.2f, 1f);
        backdrop.transform.localScale = new Vector3(18f, 9f, 1f);
        var backdropRenderer = backdrop.AddComponent<SpriteRenderer>();
        backdropRenderer.sprite = courtSprite;
        backdropRenderer.sortingOrder = -10;

        var cat = new GameObject("MenuPlayerCat");
        cat.transform.position = new Vector3(-2.2f, -1.5f, 0f);
        cat.transform.localScale = new Vector3(1.7f, 1.7f, 1f);
        var catRenderer = cat.AddComponent<SpriteRenderer>();
        catRenderer.sprite = playerCatSprite;
        catRenderer.sortingOrder = 2;

        var ball = new GameObject("MenuBall");
        ball.transform.position = new Vector3(1.4f, -0.65f, 0f);
        ball.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
        var ballRenderer = ball.AddComponent<SpriteRenderer>();
        ballRenderer.sprite = ballSprite;
        ballRenderer.sortingOrder = 3;

        var tournamentManager = new GameObject("TournamentManager");
        tournamentManager.AddComponent<TournamentManager>();
        EnsureAudioManager();
        CreateMenuCanvas();

        EditorSceneManager.SaveScene(scene, $"{ScenesPath}/MainMenu.unity");
    }

    private static void CreateMenuCanvas()
    {
        var canvasObject = new GameObject("Canvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        canvasObject.AddComponent<GraphicRaycaster>();
        canvasObject.AddComponent<MainMenuUI>();

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        CreateText("TitleText", canvasObject.transform, "Cat Tennis Tournament", new Vector2(0f, 170f), new Vector2(780f, 90f), 52, TextAnchor.MiddleCenter, Color.white);
        CreateText("SubtitleText", canvasObject.transform, "First to 5 points. Win 3 rounds.", new Vector2(0f, 104f), new Vector2(620f, 52f), 24, TextAnchor.MiddleCenter, new Color(0.92f, 1f, 0.96f));
        var startButton = CreateButton("StartButton", canvasObject.transform, "Start Tournament", new Vector2(0f, -16f), new Vector2(280f, 58f), startButtonSprite);
        var menu = canvasObject.GetComponent<MainMenuUI>();
        startButton.onClick.AddListener(menu.StartGame);
        CreateText("ControlsText", canvasObject.transform, "Move A/D  |  Jump Space  |  J tap drop / hold rally  |  K smash", new Vector2(0f, -124f), new Vector2(820f, 56f), 20, TextAnchor.MiddleCenter, Color.white);
    }

    private static void PolishHud()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            return;
        }

        var helpText = GameObject.Find("HelpText")?.GetComponent<Text>();
        if (helpText != null)
        {
            helpText.text = "A/D Move   Space Jump   J tap drop / hold rally / long drive   K smash";
            helpText.fontSize = 17;
            helpText.color = new Color(0.92f, 1f, 0.96f);
        }

        var resultPanel = GameObject.Find("ResultPanel")?.GetComponent<Image>();
        if (resultPanel != null)
        {
            resultPanel.sprite = panelSprite;
            resultPanel.color = new Color(0.06f, 0.09f, 0.1f, 0.86f);
        }

        ApplyButtonSprite("NextButton", startButtonSprite);
        ApplyButtonSprite("RestartButton", restartButtonSprite);
    }

    private static void EnsureCourtBackdrop()
    {
        var backdrop = GameObject.Find("CourtBackdrop");
        if (backdrop == null)
        {
            backdrop = new GameObject("CourtBackdrop");
        }

        backdrop.transform.position = new Vector3(0f, 1.9f, 1f);
        backdrop.transform.localScale = new Vector3(18f, 8f, 1f);
        var renderer = backdrop.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = backdrop.AddComponent<SpriteRenderer>();
        }

        renderer.sprite = courtSprite;
        renderer.color = new Color(1f, 1f, 1f, 0.72f);
        renderer.sortingOrder = -10;
    }

    private static void EnsureAudioManager()
    {
        var obj = GameObject.Find("AudioManager");
        if (obj == null)
        {
            obj = new GameObject("AudioManager");
        }

        var manager = obj.GetComponent<AudioManager>();
        if (manager == null)
        {
            manager = obj.AddComponent<AudioManager>();
        }

        SetReference(manager, "hitClip", hitClip);
        SetReference(manager, "scoreClip", scoreClip);
        SetReference(manager, "buttonClip", buttonClip);
        SetReference(manager, "winClip", winClip);
        SetReference(manager, "loseClip", loseClip);
        SetReference(manager, "bgmClip", bgmClip);
    }

    private static void ConfigureBuildScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene($"{ScenesPath}/MainMenu.unity", true),
            new EditorBuildSettingsScene($"{ScenesPath}/Match.unity", true)
        };
    }

    private static void ApplySprite(string objectName, Sprite sprite, Vector3 scale, Color color, int order)
    {
        var obj = GameObject.Find(objectName);
        if (obj == null)
        {
            return;
        }

        obj.transform.localScale = scale;
        var renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = obj.AddComponent<SpriteRenderer>();
        }

        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
    }

    private static void ApplyButtonSprite(string name, Sprite sprite)
    {
        var image = GameObject.Find(name)?.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
    }

    private static Text CreateText(string name, Transform parent, string text, Vector2 position, Vector2 size, int fontSize, TextAnchor anchor, Color color)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var uiText = obj.AddComponent<Text>();
        uiText.text = text;
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiText.fontSize = fontSize;
        uiText.color = color;
        uiText.alignment = anchor;
        uiText.rectTransform.anchoredPosition = position;
        uiText.rectTransform.sizeDelta = size;
        return uiText;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size, Sprite sprite)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var image = obj.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        image.rectTransform.anchoredPosition = position;
        image.rectTransform.sizeDelta = size;

        var button = obj.AddComponent<Button>();
        button.targetGraphic = image;

        var text = CreateText("Text", obj.transform, label, Vector2.zero, size, 22, TextAnchor.MiddleCenter, new Color(0.09f, 0.12f, 0.1f));
        text.fontStyle = FontStyle.Bold;
        return button;
    }

    private static Sprite CreateSpriteAsset(string path, Action<Texture2D> painter)
    {
        if (!File.Exists(path))
        {
            var texture = new Texture2D(96, 96, TextureFormat.RGBA32, false);
            painter(texture);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 64f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static AudioClip CreateTone(string path, string name, float frequency, float duration, float volume)
    {
        var existing = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (existing != null)
        {
            return existing;
        }

        var sampleRate = 44100;
        var samples = Mathf.CeilToInt(sampleRate * duration);
        var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        var data = new float[samples];
        for (var i = 0; i < samples; i++)
        {
            var t = i / (float)sampleRate;
            var fade = Mathf.Clamp01(1f - t / duration);
            data[i] = Mathf.Sin(t * frequency * Mathf.PI * 2f) * volume * fade;
        }

        clip.SetData(data, 0);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static AudioClip CreateLoop(string path, string name)
    {
        var existing = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (existing != null)
        {
            return existing;
        }

        var sampleRate = 44100;
        var duration = 2f;
        var samples = Mathf.CeilToInt(sampleRate * duration);
        var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        var data = new float[samples];
        for (var i = 0; i < samples; i++)
        {
            var t = i / (float)sampleRate;
            data[i] = (Mathf.Sin(t * 220f * Mathf.PI * 2f) * 0.12f) + (Mathf.Sin(t * 330f * Mathf.PI * 2f) * 0.08f);
        }

        clip.SetData(data, 0);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static void PaintCat(Texture2D texture, Color bodyColor, Color faceColor)
    {
        Clear(texture);
        FillCircle(texture, 48, 52, 24, bodyColor);
        FillTriangle(texture, 25, 71, 38, 92, 46, 69, bodyColor);
        FillTriangle(texture, 50, 69, 58, 92, 71, 71, bodyColor);
        FillCircle(texture, 48, 51, 15, faceColor);
        FillCircle(texture, 39, 55, 3, Color.black);
        FillCircle(texture, 57, 55, 3, Color.black);
        FillRect(texture, 45, 44, 6, 4, new Color(0.9f, 0.35f, 0.42f));
        FillRect(texture, 68, 38, 24, 5, new Color(0.45f, 0.25f, 0.12f));
    }

    private static void PaintBall(Texture2D texture)
    {
        Clear(texture);
        FillCircle(texture, 48, 48, 32, new Color(1f, 0.9f, 0.08f));
        FillCircle(texture, 38, 56, 3, Color.white);
        FillCircle(texture, 58, 40, 3, Color.white);
    }

    private static void PaintNet(Texture2D texture)
    {
        Clear(texture);
        FillRect(texture, 44, 4, 8, 88, new Color(0.95f, 0.95f, 0.86f));
        for (var y = 8; y < 90; y += 12)
        {
            FillRect(texture, 34, y, 28, 2, new Color(0.72f, 0.76f, 0.72f));
        }
    }

    private static void PaintCourt(Texture2D texture)
    {
        FillRect(texture, 0, 0, 96, 96, new Color(0.46f, 0.75f, 0.88f));
        FillRect(texture, 0, 0, 96, 22, new Color(0.12f, 0.33f, 0.25f));
        FillRect(texture, 7, 20, 82, 4, new Color(0.94f, 0.92f, 0.74f));
        FillRect(texture, 47, 20, 2, 13, new Color(0.94f, 0.92f, 0.74f));
    }

    private static void PaintPanel(Texture2D texture, Color color)
    {
        Clear(texture);
        FillRect(texture, 4, 4, 88, 88, color);
    }

    private static void Clear(Texture2D texture)
    {
        FillRect(texture, 0, 0, texture.width, texture.height, Color.clear);
    }

    private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        for (var py = y; py < y + height; py++)
        {
            for (var px = x; px < x + width; px++)
            {
                SetPixel(texture, px, py, color);
            }
        }
    }

    private static void FillCircle(Texture2D texture, int cx, int cy, int radius, Color color)
    {
        var radiusSquared = radius * radius;
        for (var y = cy - radius; y <= cy + radius; y++)
        {
            for (var x = cx - radius; x <= cx + radius; x++)
            {
                var dx = x - cx;
                var dy = y - cy;
                if (dx * dx + dy * dy <= radiusSquared)
                {
                    SetPixel(texture, x, y, color);
                }
            }
        }
    }

    private static void FillTriangle(Texture2D texture, int x1, int y1, int x2, int y2, int x3, int y3, Color color)
    {
        var minX = Mathf.Min(x1, Mathf.Min(x2, x3));
        var maxX = Mathf.Max(x1, Mathf.Max(x2, x3));
        var minY = Mathf.Min(y1, Mathf.Min(y2, y3));
        var maxY = Mathf.Max(y1, Mathf.Max(y2, y3));

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                if (PointInTriangle(x, y, x1, y1, x2, y2, x3, y3))
                {
                    SetPixel(texture, x, y, color);
                }
            }
        }
    }

    private static bool PointInTriangle(float px, float py, float ax, float ay, float bx, float by, float cx, float cy)
    {
        var d1 = Sign(px, py, ax, ay, bx, by);
        var d2 = Sign(px, py, bx, by, cx, cy);
        var d3 = Sign(px, py, cx, cy, ax, ay);
        var hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
        var hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(hasNegative && hasPositive);
    }

    private static float Sign(float px, float py, float ax, float ay, float bx, float by)
    {
        return (px - bx) * (ay - by) - (ax - bx) * (py - by);
    }

    private static void SetPixel(Texture2D texture, int x, int y, Color color)
    {
        if (x >= 0 && y >= 0 && x < texture.width && y < texture.height)
        {
            texture.SetPixel(x, y, color);
        }
    }

    private static void SetReference(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(fieldName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
