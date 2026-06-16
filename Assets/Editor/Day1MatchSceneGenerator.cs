using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEditorInternal;

public static class Day1MatchSceneGenerator
{
    private const string ScenesPath = "Assets/Scenes";
    private const string GeneratedPath = "Assets/Generated";

    private static Sprite squareSprite;
    private static Sprite circleSprite;

    [MenuItem("Tools/CatPong/Generate Day 1 Match Scene")]
    public static void Generate()
    {
        Directory.CreateDirectory(ScenesPath);
        Directory.CreateDirectory(GeneratedPath);

        EnsureProjectIs2DURP();
        EnsureSprites();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Match";

        CreateCamera();
        CreateCourt();
        CreatePlayer();
        CreateOpponent();
        CreateBall();

        EditorSceneManager.SaveScene(scene, $"{ScenesPath}/Match.unity");
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene($"{ScenesPath}/Match.unity", true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("CatPong", "Day 1 Match scene generated at Assets/Scenes/Match.unity", "OK");
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
    }

    private static void CreateCourt()
    {
        CreateBox("Court", new Vector2(0f, -1.1f), new Vector2(18f, 0.32f), new Color(0.18f, 0.48f, 0.34f), true, "Ground");
        CreateBox("Net", new Vector2(0f, 0.05f), new Vector2(0.14f, 2.3f), new Color(0.96f, 0.96f, 0.9f), true, "Net");
        CreateBoundary("BoundaryTop", new Vector2(0f, 5.7f), new Vector2(20f, 0.25f));
        CreateBoundary("BoundaryLeft", new Vector2(-10.6f, 2.2f), new Vector2(0.25f, 7f));
        CreateBoundary("BoundaryRight", new Vector2(10.6f, 2.2f), new Vector2(0.25f, 7f));
    }

    private static void CreatePlayer()
    {
        var player = CreateBox("Player", new Vector2(-5.5f, -0.2f), new Vector2(0.9f, 1.45f), new Color(1f, 0.62f, 0.28f), true);
        var body = player.AddComponent<Rigidbody2D>();
        body.freezeRotation = true;
        player.AddComponent<PlayerController>();
    }

    private static void CreateOpponent()
    {
        var opponent = CreateBox("Opponent", new Vector2(5.5f, -0.2f), new Vector2(0.9f, 1.45f), new Color(0.35f, 0.55f, 0.95f), true);
        var body = opponent.AddComponent<Rigidbody2D>();
        body.freezeRotation = true;
        body.bodyType = RigidbodyType2D.Kinematic;
    }

    private static void CreateBall()
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
        ball.AddComponent<BallController>();
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
}
