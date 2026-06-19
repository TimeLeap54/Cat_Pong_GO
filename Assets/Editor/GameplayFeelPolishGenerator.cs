using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GameplayFeelPolishGenerator
{
    private const string MatchScene = "Assets/Scenes/Match.unity";
    private const string MainMenuScene = "Assets/Scenes/MainMenu.unity";
    private const string PlayerPrefab = "Assets/Generated/Prefabs/Player.prefab";
    private const string OpponentPrefab = "Assets/Generated/Prefabs/Opponent.prefab";
    private const string BallPrefab = "Assets/Generated/Prefabs/Gameplay/Ball.prefab";
    private const string NetPrefab = "Assets/Generated/Prefabs/Environment/Net.prefab";
    private const string CourtBackgroundPrefab = "Assets/Generated/Prefabs/Environment/CourtBackground.prefab";
    private const string AudioFolder = "Assets/Art/Audio/SFX";

    [MenuItem("Tools/CatPong/Apply Gameplay Feel Polish")]
    public static void Apply()
    {
        ConfigurePlayerPrefab();
        ConfigureOpponentPrefab();
        ConfigureBallPrefab();
        ConfigureNetPrefab();
        GeneratePawAudio();
        ConfigureMatchScene();
        ConfigureAudioInScene(MainMenuScene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Gameplay feel polish applied: scale, colliders, wall step, paw audio, and serve presentation.");
    }

    private static void ConfigurePlayerPrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
        try
        {
            const float visualScale = 1.06f;
            root.transform.localScale = Vector3.one * visualScale;
            ReplaceWithCapsule(
                root,
                new Vector2(0.72f / visualScale, 1.28f / visualScale),
                new Vector2(0f, -0.14f / visualScale));

            var controller = root.GetComponent<PlayerController>();
            SetFloat(controller, "moveSpeed", 3.8f);
            SetVector2(controller, "swingBoxSize", new Vector2(2.2f, 2.3f));
            SetVector2(controller, "spikeSwingOffset", new Vector2(0.08f, 0.82f));
            SetVector2(controller, "spikeSwingBoxSize", new Vector2(1.9f, 1.75f));
            SetFloat(controller, "swingInputBuffer", 0.14f);
            SetFloat(controller, "wallContactDistance", 0.12f);
            SetFloat(controller, "wallSlideSpeed", 2.4f);
            SetVector2(controller, "wallKickVelocity", new Vector2(5.2f, 7.6f));
            SetFloat(controller, "wallKickControlLock", 0.16f);
            SetFloat(controller, "jFullChargeTime", 0.9f);
            SetFloat(controller, "minLiftSwingPower", 4.5f);
            SetFloat(controller, "maxLiftSwingPower", 7.6f);
            SetFloat(controller, "highLiftRatio", 1.02f);
            SetFloat(controller, "lowLiftRatio", 0.46f);
            SetFloat(controller, "verticalAimInfluence", 0.28f);
            SetFloat(controller, "spikeSwingPower", 6.8f);
            SetFloat(controller, "chargedSpikePower", 8f);
            SetInt(controller, "smashHitsToCharge", 3);
            SetFloat(controller, "smashChainWindow", 6f);
            SetVector2(controller, "upwardServeVelocity", new Vector2(6.4f, 4.5f));
            SetVector2(controller, "spikeServeVelocity", new Vector2(7.6f, -0.25f));
            SetFloat(controller, "minServePowerMultiplier", 0.76f);
            SetFloat(controller, "maxServePowerMultiplier", 1.08f);
            SetInt(controller, "maxJumpCount", 1);
            SetFloat(controller, "dashSpeed", 6.1f);
            SetFloat(controller, "dashDuration", 0.16f);
            SetFloat(controller, "dashCooldown", 0.55f);
            SetFloat(controller, "doubleTapWindow", 0.24f);
            SetFloat(controller, "serveTossDuration", 0.3f);
            SetFloat(controller, "serveTossHeight", 0.72f);
            SetFloat(controller, "serveSwingLeadTime", 0.16f);

            var swingPoint = root.transform.Find("SwingPoint");
            if (swingPoint != null)
            {
                swingPoint.localPosition = new Vector3(0.52f, 0.34f, 0f);
                swingPoint.localScale = Vector3.one;
            }

            var servePoint = root.transform.Find("ServeHoldPoint");
            if (servePoint != null)
            {
                servePoint.localPosition = new Vector3(0.48f, 0.04f, 0f);
                servePoint.localScale = Vector3.one;
            }

            var groundCheck = root.transform.Find("GroundCheck");
            if (groundCheck != null)
            {
                groundCheck.localPosition = new Vector3(0f, -0.8f / visualScale, 0f);
                groundCheck.localScale = Vector3.one / visualScale;
            }

            var animator = root.GetComponent<Animator>();
            if (animator != null)
            {
                animator.speed = 0.86f;
            }

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureOpponentPrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(OpponentPrefab);
        try
        {
            const float visualScale = 1.06f;
            root.transform.localScale = Vector3.one * visualScale;
            ReplaceWithCapsule(
                root,
                new Vector2(0.78f / visualScale, 1.3f / visualScale),
                new Vector2(0f, -0.12f / visualScale));
            var animator = root.GetComponent<Animator>();
            if (animator != null)
            {
                animator.speed = 0.84f;
            }
            PrefabUtility.SaveAsPrefabAsset(root, OpponentPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureBallPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(BallPrefab) == null)
        {
            return;
        }

        var root = PrefabUtility.LoadPrefabContents(BallPrefab);
        try
        {
            root.transform.localScale = Vector3.one * 0.3f;
            var controller = root.GetComponent<BallController>();
            SetFloat(controller, "maxSpeed", 8.2f);
            SetFloat(controller, "minHorizontalSpeed", 0.4f);
            SetFloat(controller, "netHorizontalPush", 2.1f);

            var body = root.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.gravityScale = 0.82f;
                body.drag = 0f;
            }

            PrefabUtility.SaveAsPrefabAsset(root, BallPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureNetPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(NetPrefab) == null)
        {
            return;
        }

        var root = PrefabUtility.LoadPrefabContents(NetPrefab);
        try
        {
            root.transform.localPosition = new Vector3(0f, -0.075f, 0f);
            root.transform.localScale = Vector3.one;
            var collider = root.GetComponent<BoxCollider2D>();
            if (collider != null)
            {
                collider.size = new Vector2(0.14f, 1.85f);
                collider.offset = Vector2.zero;
            }

            var visual = root.transform.Find("NetVisual");
            if (visual != null)
            {
                var renderer = visual.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != null)
                {
                    var scale = 1.85f / renderer.sprite.bounds.size.y;
                    visual.localScale = new Vector3(scale, scale, 1f);
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, NetPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ReplaceWithCapsule(GameObject root, Vector2 size, Vector2 offset)
    {
        var box = root.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            UnityEngine.Object.DestroyImmediate(box);
        }

        var capsule = root.GetComponent<CapsuleCollider2D>();
        if (capsule == null)
        {
            capsule = root.AddComponent<CapsuleCollider2D>();
        }

        capsule.direction = CapsuleDirection2D.Vertical;
        capsule.size = size;
        capsule.offset = offset;
    }

    private static void ConfigureMatchScene()
    {
        var scene = EditorSceneManager.OpenScene(MatchScene, OpenSceneMode.Single);
        Directory.CreateDirectory("Assets/Generated/Prefabs/Gameplay");
        Directory.CreateDirectory("Assets/Generated/Prefabs/Environment");
        AssetDatabase.Refresh();

        var ball = GameObject.Find("Ball");
        if (ball != null)
        {
            ball.transform.localScale = Vector3.one * 0.3f;
            var ballController = ball.GetComponent<BallController>();
            SetVector2(ballController, "serveOffset", new Vector2(0.18f, 0.08f));
            SetFloat(ballController, "maxSpeed", 8.2f);
            SetFloat(ballController, "minHorizontalSpeed", 0.4f);
            SetFloat(ballController, "netHorizontalPush", 2.1f);
            var body = ball.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.gravityScale = 0.82f;
                body.drag = 0f;
            }
        }

        var net = GameObject.Find("Net");
        if (net != null)
        {
            net.transform.position = new Vector3(0f, -0.075f, 0f);
            net.transform.localScale = Vector3.one;

            var collider = net.GetComponent<BoxCollider2D>();
            if (collider != null)
            {
                collider.size = new Vector2(0.14f, 1.85f);
                collider.offset = Vector2.zero;
            }

            var visual = net.transform.Find("NetVisual");
            if (visual != null)
            {
                visual.localPosition = Vector3.zero;
                var renderer = visual.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != null)
                {
                    var scale = 1.85f / renderer.sprite.bounds.size.y;
                    visual.localScale = new Vector3(scale, scale, 1f);
                }
            }
        }

        ConfigureAudioOnRoots(scene.GetRootGameObjects());
        ConnectSceneObjectToPrefab(ball, BallPrefab);
        ConnectSceneObjectToPrefab(net, NetPrefab);
        ConnectSceneObjectToPrefab(GameObject.Find("CourtBackdrop"), CourtBackgroundPrefab);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConnectSceneObjectToPrefab(GameObject sceneObject, string prefabPath)
    {
        if (sceneObject == null)
        {
            return;
        }

        if (PrefabUtility.IsPartOfPrefabInstance(sceneObject))
        {
            var source = PrefabUtility.GetCorrespondingObjectFromSource(sceneObject);
            if (source != null && AssetDatabase.GetAssetPath(source) == prefabPath)
            {
                return;
            }
        }

        PrefabUtility.SaveAsPrefabAssetAndConnect(sceneObject, prefabPath, InteractionMode.AutomatedAction);
    }

    private static void ConfigureAudioInScene(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        ConfigureAudioOnRoots(scene.GetRootGameObjects());
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureAudioOnRoots(GameObject[] roots)
    {
        var rally = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/paw_rally.wav");
        var soft = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/paw_soft.wav");
        var smash = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/paw_smash.wav");
        var serve = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/paw_serve.wav");
        var wall = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/wall_step.wav");

        foreach (var root in roots)
        {
            foreach (var manager in root.GetComponentsInChildren<AudioManager>(true))
            {
                SetObject(manager, "hitClip", rally);
                SetObject(manager, "softPawClip", soft);
                SetObject(manager, "smashPawClip", smash);
                SetObject(manager, "servePawClip", serve);
                SetObject(manager, "wallStepClip", wall);
            }
        }
    }

    private static void GeneratePawAudio()
    {
        Directory.CreateDirectory(AudioFolder);
        WriteEffect($"{AudioFolder}/paw_soft.wav", 0.13f, EffectKind.Soft);
        WriteEffect($"{AudioFolder}/paw_rally.wav", 0.11f, EffectKind.Rally);
        WriteEffect($"{AudioFolder}/paw_smash.wav", 0.18f, EffectKind.Smash);
        WriteEffect($"{AudioFolder}/paw_serve.wav", 0.14f, EffectKind.Serve);
        WriteEffect($"{AudioFolder}/wall_step.wav", 0.1f, EffectKind.Wall);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    private static void WriteEffect(string assetPath, float duration, EffectKind kind)
    {
        const int sampleRate = 44100;
        var sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var random = new System.Random(1701 + (int)kind);

        using var stream = new FileStream(assetPath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + sampleCount * 2);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(sampleCount * 2);

        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)sampleRate;
            var progress = t / duration;
            var noise = (float)(random.NextDouble() * 2.0 - 1.0);
            var sample = Synthesize(kind, t, progress, noise);
            writer.Write((short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
        }
    }

    private static float Synthesize(EffectKind kind, float t, float progress, float noise)
    {
        var bodyEnvelope = Mathf.Pow(1f - progress, 2.2f);
        var clickEnvelope = Mathf.Exp(-t * 75f);

        switch (kind)
        {
            case EffectKind.Soft:
                return Mathf.Sin(2f * Mathf.PI * (210f - 85f * progress) * t) * bodyEnvelope * 0.48f
                    + noise * clickEnvelope * 0.08f;
            case EffectKind.Rally:
                return Mathf.Sin(2f * Mathf.PI * (185f - 35f * progress) * t) * bodyEnvelope * 0.42f
                    + Mathf.Sin(2f * Mathf.PI * 360f * t) * bodyEnvelope * 0.16f
                    + noise * clickEnvelope * 0.32f;
            case EffectKind.Smash:
                return Mathf.Sin(2f * Mathf.PI * (118f - 24f * progress) * t) * bodyEnvelope * 0.58f
                    + Mathf.Sin(2f * Mathf.PI * 245f * t) * bodyEnvelope * 0.2f
                    + noise * clickEnvelope * 0.46f;
            case EffectKind.Serve:
                return Mathf.Sin(2f * Mathf.PI * (225f - 45f * progress) * t) * bodyEnvelope * 0.5f
                    + noise * clickEnvelope * 0.24f;
            default:
                return Mathf.Sin(2f * Mathf.PI * (95f - 20f * progress) * t) * bodyEnvelope * 0.5f
                    + noise * clickEnvelope * 0.18f;
        }
    }

    private static void SetFloat(UnityEngine.Object target, string propertyName, float value)
    {
        if (target == null) return;
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(propertyName);
        if (property == null) return;
        property.floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetInt(UnityEngine.Object target, string propertyName, int value)
    {
        if (target == null) return;
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(propertyName);
        if (property == null) return;
        property.intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetVector2(UnityEngine.Object target, string propertyName, Vector2 value)
    {
        if (target == null) return;
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(propertyName);
        if (property == null) return;
        property.vector2Value = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObject(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        if (target == null) return;
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(propertyName);
        if (property == null) return;
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private enum EffectKind
    {
        Soft,
        Rally,
        Smash,
        Serve,
        Wall
    }
}
