using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatTennis.Rebuild.EditorTools
{
    public static class CaptureMatchQAPreview
    {
        [MenuItem("Cat Tennis/Rebuild/Capture Match QA Preview")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene(
                "Assets/Scenes/Rebuild_Match.unity",
                OpenSceneMode.Single);
            Camera camera = Object.FindObjectOfType<Camera>();
            if (camera == null)
            {
                throw new MissingReferenceException("Match camera was not found.");
            }

            const int width = 1600;
            const int height = 900;
            RenderTexture target = new RenderTexture(width, height, 24);
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            string output = Path.GetFullPath("MatchQA_Preview.png");
            File.WriteAllBytes(output, image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = previous;
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(image);
            Debug.Log($"Captured Match QA preview: {output}");
        }
    }
}
