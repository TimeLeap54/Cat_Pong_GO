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

public static class PlayerSpriteAnimationGenerator
{
    private const string ArtPath = "Assets/Art";
    private const string MapPath = ArtPath + "/Map1.png";
    private const string AnimationsPath = "Assets/Generated/PlayerAnimations";
    private const string PrefabsPath = "Assets/Generated/Prefabs";
    private const string ControllerPath = AnimationsPath + "/PlayerCat.controller";
    private const string PlayerPrefabPath = PrefabsPath + "/Player.prefab";
    private const float PixelsPerUnit = 170f;
    private const float MovementPixelsPerUnit = 179f;
    private static readonly Vector2 StablePivot = new Vector2(0.5f, 0.38f);

    [MenuItem("Tools/CatPong/Generate Player Sprite Animations")]
    public static void Generate()
    {
        var controller = GenerateAssets();
        ApplyToMatchScene(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static AnimatorController GenerateAssets()
    {
        Directory.CreateDirectory(AnimationsPath);

        var idle = CreateClip("PlayerIdle", "cat_tennis_idle_6f.png", 8f, true);
        var run = CreateClip("PlayerRun", "cat_tennis_run_6f.png", 12f, true);
        var backstep = CreateClip("PlayerBackstep", "cat_tennis_backstep_6f.png", 10f, true);
        var jump = CreateClip("PlayerJump", "cat_tennis_Jump_6f.png", 12f, false);
        var jSwing = CreateClip("PlayerJSwing", "cat_tennis_JSwing_6f.png", 18f, false);
        var kSmash = CreateClip("PlayerKSmash", "cat_tennis_KSmash_6f.png", 18f, false);

        return CreateController(idle, run, backstep, jump, jSwing, kSmash);
    }

    private static AnimationClip CreateClip(string clipName, string sheetName, float frameRate, bool loop)
    {
        var sprites = LoadSpritesFromMultipleSheet($"{ArtPath}/{sheetName}");
        if (sprites.Count == 0)
        {
            throw new InvalidOperationException($"No sub-sprites found in {sheetName}. Set Sprite Mode to Multiple and slice it first.");
        }

        var clipPath = $"{AnimationsPath}/{clipName}.anim";
        AssetDatabase.DeleteAsset(clipPath);

        var clip = new AnimationClip
        {
            frameRate = frameRate
        };

        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        var keyframes = new ObjectReferenceKeyframe[sprites.Count + 1];
        for (var i = 0; i < sprites.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = sprites[i]
            };
        }

        keyframes[^1] = new ObjectReferenceKeyframe
        {
            time = sprites.Count / frameRate,
            value = sprites[^1]
        };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        SetLoopTime(clip, loop);
        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private static List<Sprite> LoadSpritesFromMultipleSheet(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Missing player animation sheet: {path}");
        }

        ConfigureSheetImporter(path);
        var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(sprite => ExtractTrailingNumber(sprite.name))
            .ThenBy(sprite => sprite.name, StringComparer.Ordinal)
            .ToList();

        return sprites;
    }

    private static void ConfigureSheetImporter(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = GetPixelsPerUnit(path);
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = StablePivot;
        settings.spritePixelsPerUnit = GetPixelsPerUnit(path);
        importer.SetTextureSettings(settings);

        var dataProvider = GetSpriteDataProvider(importer);
        if (dataProvider != null)
        {
            dataProvider.InitSpriteEditorDataProvider();
            var spriteRects = dataProvider.GetSpriteRects();
            for (var i = 0; i < spriteRects.Length; i++)
            {
                spriteRects[i].alignment = SpriteAlignment.Custom;
                spriteRects[i].pivot = StablePivot;
            }

            dataProvider.SetSpriteRects(spriteRects);
            dataProvider.Apply();
        }

        importer.SaveAndReimport();
    }

    private static ISpriteEditorDataProvider GetSpriteDataProvider(AssetImporter importer)
    {
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        return factory.GetSpriteEditorDataProviderFromObject(importer);
    }

    private static int ExtractTrailingNumber(string value)
    {
        var index = value.Length - 1;
        while (index >= 0 && char.IsDigit(value[index]))
        {
            index--;
        }

        if (index == value.Length - 1)
        {
            return 0;
        }

        return int.TryParse(value[(index + 1)..], out var number) ? number : 0;
    }

    private static float GetPixelsPerUnit(string path)
    {
        var fileName = Path.GetFileName(path);
        return fileName is "cat_tennis_run_6f.png" or "cat_tennis_backstep_6f.png"
            ? MovementPixelsPerUnit
            : PixelsPerUnit;
    }

    private static void ApplyToMatchScene(AnimatorController controller)
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Match.unity", OpenSceneMode.Single);
        var player = GameObject.Find("Player");
        if (player == null)
        {
            return;
        }

        var renderer = player.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = player.AddComponent<SpriteRenderer>();
        }

        var idleSprites = LoadSpritesFromMultipleSheet($"{ArtPath}/cat_tennis_idle_6f.png");
        if (idleSprites.Count > 0)
        {
            renderer.sprite = idleSprites[0];
        }

        renderer.drawMode = SpriteDrawMode.Simple;
        renderer.sortingOrder = 2;
        player.transform.localScale = Vector3.one;

        var animator = player.GetComponent<Animator>();
        if (animator == null)
        {
            animator = player.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;

        var controllerComponent = player.GetComponent<PlayerController>();
        if (controllerComponent != null)
        {
            SetReference(controllerComponent, "animator", animator);
        }

        ApplyCourtBackdrop();
        ApplyInvisibleCourtZones();

        Directory.CreateDirectory(PrefabsPath);
        PrefabUtility.SaveAsPrefabAssetAndConnect(player, PlayerPrefabPath, InteractionMode.AutomatedAction);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ApplyCourtBackdrop()
    {
        if (!File.Exists(MapPath))
        {
            return;
        }

        ConfigureSingleSpriteImporter(MapPath, 100f);
        var mapSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MapPath);
        if (mapSprite == null)
        {
            return;
        }

        var backdrop = GameObject.Find("CourtBackdrop");
        if (backdrop == null)
        {
            backdrop = new GameObject("CourtBackdrop");
            backdrop.transform.position = new Vector3(0f, 1.9f, 1f);
            backdrop.transform.localScale = new Vector3(18f, 8f, 1f);
        }

        var renderer = backdrop.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = backdrop.AddComponent<SpriteRenderer>();
        }

        renderer.sprite = mapSprite;
        renderer.color = Color.white;
        renderer.sortingOrder = -20;
    }

    private static void ApplyInvisibleCourtZones()
    {
        ConfigureInvisibleCourtBox("GroundBase", new Vector2(0f, -1.35f), new Vector2(24f, 0.7f), false);
        ConfigureInvisibleCourtBox("CourtInBounds", new Vector2(0f, -0.96f), new Vector2(17.2f, 0.12f), true);
        ConfigureInvisibleCourtBox("LeftOutLine", new Vector2(-8.6f, -0.84f), new Vector2(0.08f, 0.42f), true);
        ConfigureInvisibleCourtBox("RightOutLine", new Vector2(8.6f, -0.84f), new Vector2(0.08f, 0.42f), true);
        ConfigureInvisibleCourtBox("CenterCourtLine", new Vector2(0f, -0.84f), new Vector2(0.05f, 0.32f), true);
        ConfigureInvisibleGoalZone("LeftScoreZone", new Vector2(-4.3f, -0.86f), new Vector2(8.6f, 0.22f));
        ConfigureInvisibleGoalZone("RightScoreZone", new Vector2(4.3f, -0.86f), new Vector2(8.6f, 0.22f));
    }

    private static void ConfigureInvisibleCourtBox(string name, Vector2 position, Vector2 scale, bool removeCollider)
    {
        var obj = GameObject.Find(name);
        if (obj == null)
        {
            return;
        }

        obj.transform.position = new Vector3(position.x, position.y, 0f);
        obj.transform.localScale = new Vector3(scale.x, scale.y, 1f);

        var renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }

        var box = obj.GetComponent<BoxCollider2D>();
        if (removeCollider && box != null)
        {
            UnityEngine.Object.DestroyImmediate(box);
        }
    }

    private static void ConfigureInvisibleGoalZone(string name, Vector2 position, Vector2 size)
    {
        var zone = GameObject.Find(name);
        if (zone == null)
        {
            return;
        }

        zone.transform.position = new Vector3(position.x, position.y, 0f);

        var renderer = zone.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }

        var box = zone.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.size = size;
            box.isTrigger = true;
        }
    }

    private static void ConfigureSingleSpriteImporter(string path, float pixelsPerUnit)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
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

    private static AnimatorController CreateController(AnimationClip idle, AnimationClip run, AnimationClip backstep, AnimationClip jump, AnimationClip jSwing, AnimationClip kSmash)
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("JSwing", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("KSmash", AnimatorControllerParameterType.Trigger);

        var stateMachine = controller.layers[0].stateMachine;
        var idleState = stateMachine.AddState("Idle", new Vector3(240f, 140f, 0f));
        var runState = stateMachine.AddState("Run", new Vector3(500f, 60f, 0f));
        var backstepState = stateMachine.AddState("Backstep", new Vector3(500f, 220f, 0f));
        var jumpState = stateMachine.AddState("Jump", new Vector3(240f, -70f, 0f));
        var jSwingState = stateMachine.AddState("J Swing", new Vector3(760f, 60f, 0f));
        var kSmashState = stateMachine.AddState("K Smash", new Vector3(760f, 220f, 0f));

        idleState.motion = idle;
        runState.motion = run;
        backstepState.motion = backstep;
        jumpState.motion = jump;
        jSwingState.motion = jSwing;
        kSmashState.motion = kSmash;
        stateMachine.defaultState = idleState;

        AddMoveTransition(idleState, runState, "MoveX", 0.1f, AnimatorConditionMode.Greater);
        AddMoveTransition(runState, idleState, "MoveX", 0.1f, AnimatorConditionMode.Less);
        AddMoveTransition(idleState, backstepState, "MoveX", -0.1f, AnimatorConditionMode.Less);
        AddMoveTransition(backstepState, idleState, "MoveX", -0.1f, AnimatorConditionMode.Greater);
        AddMoveTransition(runState, backstepState, "MoveX", -0.1f, AnimatorConditionMode.Less);
        AddMoveTransition(backstepState, runState, "MoveX", 0.1f, AnimatorConditionMode.Greater);

        AddTriggerTransition(stateMachine, jumpState, "Jump");
        AddTriggerTransition(stateMachine, jSwingState, "JSwing");
        AddTriggerTransition(stateMachine, kSmashState, "KSmash");
        AddExitTransition(jumpState, idleState, 0.92f);
        AddExitTransition(jSwingState, idleState, 0.92f);
        AddExitTransition(kSmashState, idleState, 0.92f);

        return controller;
    }

    private static void SetLoopTime(AnimationClip clip, bool loop)
    {
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
    }

    private static void AddMoveTransition(AnimatorState from, AnimatorState to, string parameter, float threshold, AnimatorConditionMode mode)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.04f;
        transition.AddCondition(mode, threshold, parameter);
    }

    private static void AddTriggerTransition(AnimatorStateMachine stateMachine, AnimatorState to, string trigger)
    {
        var transition = stateMachine.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.02f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
    }

    private static void AddExitTransition(AnimatorState from, AnimatorState to, float exitTime)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = exitTime;
        transition.duration = 0.04f;
    }

    private static void SetReference(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(fieldName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
