using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace D22.Editor
{
    public static class D22ReviewCapture
    {
        [MenuItem("D22/Capture Lighting Review")]
        public static void Capture()
        {
            D22SceneBuilder.OpenWorkspace();
            D22SceneBuilder.Validate();
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../docs/previews"));
            Directory.CreateDirectory(output);
            CaptureView(output + "/unity-stage.png", new Vector3(.8f, 1.7f, 1), new Vector3(0, 1.8f, 5.1f));
            CaptureView(output + "/unity-entry.png", new Vector3(.6f, 1.8f, -2.5f), new Vector3(0, 1.9f, 4.8f));
            // The first render primes URP material uploads; save the stage after warmup.
            CaptureView(output + "/unity-stage.png", new Vector3(.8f, 1.7f, 1), new Vector3(0, 1.8f, 5.1f));
            Debug.Log("D22_REVIEW_CAPTURE_COMPLETE " + output);
        }

        static void CaptureView(string path, Vector3 position, Vector3 target)
        {
            var go = new GameObject("Temporary review camera") { hideFlags = HideFlags.HideAndDontSave };
            var camera = go.AddComponent<Camera>();
            camera.CopyFrom(Camera.main);
            camera.enabled = false;
            camera.fieldOfView = 72;
            camera.transform.position = position;
            camera.transform.LookAt(target);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var texture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                texture.Create();
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = texture };
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = texture;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                texture.Release();
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(pixels);
                Object.DestroyImmediate(go);
            }
        }
    }
}
