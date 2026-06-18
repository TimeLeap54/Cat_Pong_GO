using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayerSpriteAnimationGenerator
{
    private const string ArtPath = "Assets/Art";
    private const string FramesPath = "Assets/Generated/PlayerFrames";
    private const string AnimationsPath = "Assets/Generated/PlayerAnimations";
    private const string ControllerPath = AnimationsPath + "/PlayerCat.controller";
    private const float FramePixelsPerUnit = 170f;

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
        Directory.CreateDirectory(FramesPath);
        Directory.CreateDirectory(AnimationsPath);

        var idle = CreateClip("PlayerIdle", "cat_tennis_idle_6f.png", 8f, true);
        var run = CreateClip("PlayerRun", "cat_tennis_run_6f.png", 12f, true);
        var backstep = CreateClip("PlayerBackstep", "cat_tennis_backstep_6f.png", 10f, true);
        var jump = CreateClip("PlayerJump", "cat_tennis_Jump_6f.png", 12f, false);
        var jSwing = CreateClip("PlayerJSwing", "cat_tennis_JSwing_6f.png", 18f, false);
        var kSmash = CreateClip("PlayerKSmash", "cat_tennis_KSmash_6f.png", 18f, false);

        return CreateController(idle, run, backstep, jump, jSwing, kSmash);
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

        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{FramesPath}/PlayerIdle_0.png");
        renderer.sortingOrder = 2;

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

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static AnimationClip CreateClip(string clipName, string sheetName, float frameRate, bool loop)
    {
        var frameSprites = SliceSheet(clipName, $"{ArtPath}/{sheetName}");
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

        var keyframes = new ObjectReferenceKeyframe[frameSprites.Count + 1];
        for (var i = 0; i < frameSprites.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = frameSprites[i]
            };
        }

        keyframes[^1] = new ObjectReferenceKeyframe
        {
            time = frameSprites.Count / frameRate,
            value = frameSprites[^1]
        };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        SetLoopTime(clip, loop);
        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private static List<Sprite> SliceSheet(string clipName, string sheetPath)
    {
        if (!File.Exists(sheetPath))
        {
            throw new FileNotFoundException($"Missing player animation sheet: {sheetPath}");
        }

        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        source.LoadImage(File.ReadAllBytes(sheetPath));

        var frameCount = 6;
        var frameWidth = Mathf.FloorToInt(source.width / (float)frameCount);
        var frameHeight = source.height;
        var sprites = new List<Sprite>();

        for (var i = 0; i < frameCount; i++)
        {
            var frame = new Texture2D(frameWidth, frameHeight, TextureFormat.RGBA32, false);
            frame.SetPixels(source.GetPixels(i * frameWidth, 0, frameWidth, frameHeight));
            RemoveDisconnectedSpriteFragments(frame);
            ClearLeftEdgeArtifacts(frame, frameWidth > 300 ? 64 : 0);
            frame.Apply();

            var framePath = $"{FramesPath}/{clipName}_{i}.png";
            File.WriteAllBytes(framePath, frame.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(frame);

            AssetDatabase.ImportAsset(framePath);
            ConfigureFrameImporter(framePath);
            sprites.Add(AssetDatabase.LoadAssetAtPath<Sprite>(framePath));
        }

        UnityEngine.Object.DestroyImmediate(source);
        return sprites;
    }

    private static void ClearLeftEdgeArtifacts(Texture2D texture, int clearWidth)
    {
        if (clearWidth <= 0)
        {
            return;
        }

        var pixels = texture.GetPixels32();
        var width = texture.width;
        var height = texture.height;
        var maxX = Mathf.Min(clearWidth, width);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < maxX; x++)
            {
                pixels[y * width + x] = Color.clear;
            }
        }

        texture.SetPixels32(pixels);
    }

    private static void RemoveDisconnectedSpriteFragments(Texture2D texture)
    {
        var width = texture.width;
        var height = texture.height;
        var pixels = texture.GetPixels32();
        var visited = new bool[pixels.Length];
        var components = new List<List<int>>();

        for (var i = 0; i < pixels.Length; i++)
        {
            if (visited[i] || pixels[i].a < 16)
            {
                continue;
            }

            var component = new List<int>();
            var queue = new Queue<int>();
            queue.Enqueue(i);
            visited[i] = true;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                component.Add(current);
                var x = current % width;
                var y = current / width;
                TryQueue(x - 1, y, width, height, pixels, visited, queue);
                TryQueue(x + 1, y, width, height, pixels, visited, queue);
                TryQueue(x, y - 1, width, height, pixels, visited, queue);
                TryQueue(x, y + 1, width, height, pixels, visited, queue);
            }

            components.Add(component);
        }

        if (components.Count <= 1)
        {
            return;
        }

        var mainComponent = components[0];
        foreach (var component in components)
        {
            if (component.Count > mainComponent.Count)
            {
                mainComponent = component;
            }
        }

        var keep = new bool[pixels.Length];
        foreach (var index in mainComponent)
        {
            keep[index] = true;
        }

        for (var i = 0; i < pixels.Length; i++)
        {
            if (!keep[i])
            {
                pixels[i] = Color.clear;
            }
        }

        texture.SetPixels32(pixels);
    }

    private static void TryQueue(int x, int y, int width, int height, Color32[] pixels, bool[] visited, Queue<int> queue)
    {
        if (x < 0 || y < 0 || x >= width || y >= height)
        {
            return;
        }

        var index = y * width + x;
        if (visited[index] || pixels[index].a < 16)
        {
            return;
        }

        visited[index] = true;
        queue.Enqueue(index);
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

    private static void ConfigureFrameImporter(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = FramePixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
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
