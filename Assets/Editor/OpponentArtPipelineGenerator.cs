using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OpponentArtPipelineGenerator
{
    private const string ArtRoot = "Assets/Art";
    private const string PlayerArtPath = ArtRoot + "/Characters/Player";
    private const string OpponentArtPath = ArtRoot + "/Characters/Opponent";
    private const string EnvironmentArtPath = ArtRoot + "/Environment";
    private const string GameplayArtPath = ArtRoot + "/Gameplay";
    private const string AnimationPath = "Assets/Generated/OpponentAnimations";
    private const string PrefabPath = "Assets/Generated/Prefabs/Opponent.prefab";
    private const string ControllerPath = AnimationPath + "/OpponentCat.controller";
    private static readonly Vector2 StablePivot = new Vector2(0.5f, 0.38f);

    private static readonly (string source, string destination)[] AssetMoves =
    {
        ("cat_tennis_idle_6f.png", PlayerArtPath + "/cat_tennis_idle_6f.png"),
        ("cat_tennis_run_6f.png", PlayerArtPath + "/cat_tennis_run_6f.png"),
        ("cat_tennis_backstep_6f.png", PlayerArtPath + "/cat_tennis_backstep_6f.png"),
        ("cat_tennis_Jump_6f.png", PlayerArtPath + "/cat_tennis_Jump_6f.png"),
        ("cat_tennis_JSwing_6f.png", PlayerArtPath + "/cat_tennis_JSwing_6f.png"),
        ("cat_tennis_KSmash_6f.png", PlayerArtPath + "/cat_tennis_KSmash_6f.png"),
        ("1.png", OpponentArtPath + "/opponent_cat_idle_6f.png"),
        ("2.png", OpponentArtPath + "/opponent_cat_run_8f.png"),
        ("3.png", OpponentArtPath + "/opponent_cat_backstep_6f.png"),
        ("4.png", OpponentArtPath + "/opponent_cat_jump_8f.png"),
        ("5.png", OpponentArtPath + "/opponent_cat_jswing_8f.png"),
        ("6.png", OpponentArtPath + "/opponent_cat_ksmash_8f.png"),
        ("Map1.png", EnvironmentArtPath + "/Map1.png"),
        ("Net_64.png", EnvironmentArtPath + "/Net_64.png"),
        ("Ball_64.png", GameplayArtPath + "/Ball_64.png")
    };

    [MenuItem("Tools/CatPong/Organize Art And Generate Opponent")]
    public static void Generate()
    {
        OrganizeAssets();
        Directory.CreateDirectory(AnimationPath);

        var idle = CreateClip("OpponentIdle", "opponent_cat_idle_6f.png", 8f, true, 170f);
        var run = CreateClip("OpponentRun", "opponent_cat_run_8f.png", 12f, true, 176f);
        var backstep = CreateClip("OpponentBackstep", "opponent_cat_backstep_6f.png", 10f, true, 176f);
        var jump = CreateClip("OpponentJump", "opponent_cat_jump_8f.png", 12f, false, 170f);
        var jSwing = CreateClip("OpponentJSwing", "opponent_cat_jswing_8f.png", 18f, false, 170f);
        var kSmash = CreateClip("OpponentKSmash", "opponent_cat_ksmash_8f.png", 18f, false, 168f);
        var controller = CreateController(idle, run, backstep, jump, jSwing, kSmash);

        ApplyToMatchScene(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void OrganizeAssets()
    {
        EnsureFolder(ArtRoot, "Characters");
        EnsureFolder(ArtRoot + "/Characters", "Player");
        EnsureFolder(ArtRoot + "/Characters", "Opponent");
        EnsureFolder(ArtRoot, "Environment");
        EnsureFolder(ArtRoot, "Gameplay");

        foreach (var (source, destination) in AssetMoves)
        {
            var sourcePath = ArtRoot + "/" + source;
            if (AssetDatabase.LoadMainAssetAtPath(destination) != null || AssetDatabase.LoadMainAssetAtPath(sourcePath) == null)
            {
                continue;
            }

            var error = AssetDatabase.MoveAsset(sourcePath, destination);
            if (!string.IsNullOrEmpty(error))
            {
                throw new InvalidOperationException($"Could not move {sourcePath} to {destination}: {error}");
            }
        }
    }

    private static void EnsureFolder(string parent, string name)
    {
        var path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static AnimationClip CreateClip(string clipName, string sheetName, float frameRate, bool loop, float pixelsPerUnit)
    {
        var sprites = LoadSprites($"{OpponentArtPath}/{sheetName}", pixelsPerUnit);
        if (sprites.Count == 0)
        {
            throw new InvalidOperationException($"No Multiple Sprite frames found in {sheetName}.");
        }

        var clipPath = $"{AnimationPath}/{clipName}.anim";
        AssetDatabase.DeleteAsset(clipPath);
        var clip = new AnimationClip { frameRate = frameRate };
        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        var frames = new ObjectReferenceKeyframe[sprites.Count + 1];
        for (var i = 0; i < sprites.Count; i++)
        {
            frames[i] = new ObjectReferenceKeyframe { time = i / frameRate, value = sprites[i] };
        }

        frames[^1] = new ObjectReferenceKeyframe
        {
            time = sprites.Count / frameRate,
            value = sprites[^1]
        };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private static List<Sprite> LoadSprites(string path, float pixelsPerUnit)
    {
        ConfigureMultipleSpriteImporter(path, pixelsPerUnit);
        return AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(sprite => ExtractTrailingNumber(sprite.name))
            .ThenBy(sprite => sprite.name, StringComparer.Ordinal)
            .ToList();
    }

    private static void ConfigureMultipleSpriteImporter(string path, float pixelsPerUnit)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            throw new FileNotFoundException($"Missing texture importer for {path}");
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        var textureSettings = new TextureImporterSettings();
        importer.ReadTextureSettings(textureSettings);
        textureSettings.spriteAlignment = (int)SpriteAlignment.Custom;
        textureSettings.spritePivot = StablePivot;
        textureSettings.spritePixelsPerUnit = pixelsPerUnit;
        importer.SetTextureSettings(textureSettings);

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var spriteRects = provider.GetSpriteRects();
        for (var i = 0; i < spriteRects.Length; i++)
        {
            spriteRects[i].name = RenameFrame(spriteRects[i].name, Path.GetFileNameWithoutExtension(path), i);
            spriteRects[i].alignment = SpriteAlignment.Custom;
            spriteRects[i].pivot = StablePivot;
        }

        provider.SetSpriteRects(spriteRects);
        provider.Apply();
        importer.SaveAndReimport();
    }

    private static string RenameFrame(string currentName, string sheetName, int index)
    {
        return $"{sheetName}_{index}";
    }

    private static int ExtractTrailingNumber(string value)
    {
        var index = value.Length - 1;
        while (index >= 0 && char.IsDigit(value[index]))
        {
            index--;
        }

        return index == value.Length - 1 || !int.TryParse(value[(index + 1)..], out var number) ? 0 : number;
    }

    private static AnimatorController CreateController(
        AnimationClip idle,
        AnimationClip run,
        AnimationClip backstep,
        AnimationClip jump,
        AnimationClip jSwing,
        AnimationClip kSmash)
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("JSwing", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("KSmash", AnimatorControllerParameterType.Trigger);

        var machine = controller.layers[0].stateMachine;
        var idleState = machine.AddState("Idle", new Vector3(240f, 140f));
        var runState = machine.AddState("Run", new Vector3(500f, 60f));
        var backstepState = machine.AddState("Backstep", new Vector3(500f, 220f));
        var jumpState = machine.AddState("Jump", new Vector3(240f, -70f));
        var jSwingState = machine.AddState("J Swing", new Vector3(760f, 60f));
        var kSmashState = machine.AddState("K Smash", new Vector3(760f, 220f));

        idleState.motion = idle;
        runState.motion = run;
        backstepState.motion = backstep;
        jumpState.motion = jump;
        jSwingState.motion = jSwing;
        kSmashState.motion = kSmash;
        machine.defaultState = idleState;

        AddMoveTransition(idleState, runState, "MoveX", -0.1f, AnimatorConditionMode.Less);
        AddMoveTransition(runState, idleState, "MoveX", -0.1f, AnimatorConditionMode.Greater);
        AddMoveTransition(idleState, backstepState, "MoveX", 0.1f, AnimatorConditionMode.Greater);
        AddMoveTransition(backstepState, idleState, "MoveX", 0.1f, AnimatorConditionMode.Less);
        AddMoveTransition(runState, backstepState, "MoveX", 0.1f, AnimatorConditionMode.Greater);
        AddMoveTransition(backstepState, runState, "MoveX", -0.1f, AnimatorConditionMode.Less);

        AddTriggerTransition(machine, jumpState, "Jump");
        AddTriggerTransition(machine, jSwingState, "JSwing");
        AddTriggerTransition(machine, kSmashState, "KSmash");
        AddExitTransition(jumpState, idleState);
        AddExitTransition(jSwingState, idleState);
        AddExitTransition(kSmashState, idleState);
        return controller;
    }

    private static void AddMoveTransition(AnimatorState from, AnimatorState to, string parameter, float threshold, AnimatorConditionMode mode)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.04f;
        transition.AddCondition(mode, threshold, parameter);
    }

    private static void AddTriggerTransition(AnimatorStateMachine machine, AnimatorState state, string trigger)
    {
        var transition = machine.AddAnyStateTransition(state);
        transition.hasExitTime = false;
        transition.duration = 0.02f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
    }

    private static void AddExitTransition(AnimatorState from, AnimatorState to)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = 0.92f;
        transition.duration = 0.04f;
    }

    private static void ApplyToMatchScene(AnimatorController controller)
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Match.unity", OpenSceneMode.Single);
        var opponent = GameObject.Find("Opponent");
        if (opponent == null)
        {
            throw new InvalidOperationException("Opponent object was not found in Match scene.");
        }

        var renderer = opponent.GetComponent<SpriteRenderer>();
        if (!renderer)
        {
            renderer = opponent.AddComponent<SpriteRenderer>();
        }
        var idleSprites = LoadSprites($"{OpponentArtPath}/opponent_cat_idle_6f.png", 170f);
        renderer.sprite = idleSprites[0];
        renderer.drawMode = SpriteDrawMode.Simple;
        renderer.sortingOrder = 2;
        renderer.color = Color.white;

        var oldScale = opponent.transform.localScale;
        var collider = opponent.GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.size = new Vector2(collider.size.x * oldScale.x, collider.size.y * oldScale.y);
        }

        opponent.transform.localScale = Vector3.one;

        var swingPoint = opponent.transform.Find("OpponentSwingPoint");
        if (swingPoint != null)
        {
            swingPoint.localPosition = new Vector3(
                swingPoint.localPosition.x * oldScale.x,
                swingPoint.localPosition.y * oldScale.y,
                0f);
            swingPoint.localScale = Vector3.one;
        }

        var animator = opponent.GetComponent<Animator>();
        if (!animator)
        {
            animator = opponent.AddComponent<Animator>();
        }
        animator.runtimeAnimatorController = controller;

        var ai = opponent.GetComponent<OpponentAI>();
        if (ai != null)
        {
            SetReference(ai, "animator", animator);
        }

        ApplyBallSprite();
        ApplyNetSprite();

        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath) ?? "Assets/Generated/Prefabs");
        PrefabUtility.SaveAsPrefabAssetAndConnect(opponent, PrefabPath, InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ApplyBallSprite()
    {
        const string path = GameplayArtPath + "/Ball_64.png";
        ConfigureSingleSpriteImporter(path, 64f);
        var ball = GameObject.Find("Ball");
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (ball == null || sprite == null)
        {
            return;
        }

        var renderer = ball.GetComponent<SpriteRenderer>();
        if (!renderer)
        {
            renderer = ball.AddComponent<SpriteRenderer>();
        }
        renderer.sprite = sprite;
        renderer.drawMode = SpriteDrawMode.Simple;
        renderer.color = Color.white;
        renderer.sortingOrder = 4;
    }

    private static void ApplyNetSprite()
    {
        const string path = EnvironmentArtPath + "/Net_64.png";
        ConfigureSingleSpriteImporter(path, 64f);
        var net = GameObject.Find("Net");
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (net == null || sprite == null)
        {
            return;
        }

        var rootRenderer = net.GetComponent<SpriteRenderer>();
        if (rootRenderer != null)
        {
            rootRenderer.enabled = false;
        }

        var visual = net.transform.Find("NetVisual");
        if (visual == null)
        {
            visual = new GameObject("NetVisual").transform;
            visual.SetParent(net.transform, false);
        }

        var renderer = visual.GetComponent<SpriteRenderer>();
        if (!renderer)
        {
            renderer = visual.gameObject.AddComponent<SpriteRenderer>();
        }
        renderer.sprite = sprite;
        renderer.drawMode = SpriteDrawMode.Simple;
        renderer.color = Color.white;
        renderer.sortingOrder = 3;

        visual.localPosition = Vector3.zero;
        var targetHeight = net.transform.localScale.y;
        var uniformScale = targetHeight / sprite.bounds.size.y;
        visual.localScale = new Vector3(
            uniformScale / net.transform.localScale.x,
            uniformScale / net.transform.localScale.y,
            1f);
    }

    private static void ConfigureSingleSpriteImporter(string path, float pixelsPerUnit)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static void SetReference(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(fieldName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
