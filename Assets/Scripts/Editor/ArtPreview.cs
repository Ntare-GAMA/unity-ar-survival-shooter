using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ARSurvival.EditorTools
{
    /// <summary>
    /// Dev tool: renders prefabs/models (optionally posed at a moment of an animation clip) to PNGs
    /// in Library/ArtPreview, and logs their bounds and bone names. Used to fit third-party art
    /// (scale, facing, gun placement) without an interactive editor. Never saves scene changes.
    /// </summary>
    public static class ArtPreview
    {
        public const string OutDir = "Library/ArtPreview";

        public struct Shot
        {
            public string File;
            public GameObject Asset;
            public AnimationClip Clip;
            public float ClipTime; // 0..1 normalised
        }

        /// <summary>Renders each shot from the front (+Z facing camera) and the right side.</summary>
        public static void Render(IEnumerable<Shot> shots, StringBuilder log, bool newScene = true)
        {
            if (newScene)
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory(OutDir);

            var lightGo = new GameObject("Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            lightGo.transform.rotation = Quaternion.Euler(40f, 150f, 0f);
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);

            var camGo = new GameObject("Cam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.25f, 0.27f, 0.3f);
            cam.orthographic = true;
            var rt = new RenderTexture(640, 640, 24);
            cam.targetTexture = rt;

            foreach (var shot in shots)
            {
                var go = (GameObject)Object.Instantiate(shot.Asset);
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                if (shot.Clip != null)
                {
                    // Clip bindings are relative to the Animator's object, which may be nested.
                    var animator = go.GetComponentInChildren<Animator>();
                    shot.Clip.SampleAnimation(animator != null ? animator.gameObject : go, shot.ClipTime * shot.Clip.length);
                }

                var bounds = Bounds(go);
                log?.AppendLine($"{shot.File}: center {bounds.center:F3} size {bounds.size:F3}");

                float half = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) * 1.15f;
                cam.orthographicSize = half;
                cam.nearClipPlane = 0.001f;
                cam.farClipPlane = half * 20f;

                // Front (camera on +Z), right side (camera on +X), top (camera on +Y, +Z up in image).
                var views = new[] { Vector3.forward, Vector3.right, Vector3.up };
                var sheet = new Texture2D(640 * views.Length, 640, TextureFormat.RGB24, false);
                for (int v = 0; v < views.Length; v++)
                {
                    var view = RenderView(cam, rt, bounds, views[v], half);
                    sheet.SetPixels(640 * v, 0, 640, 640, view.GetPixels());
                    Object.DestroyImmediate(view);
                }
                sheet.Apply();
                File.WriteAllBytes(Path.Combine(OutDir, shot.File + ".png"), sheet.EncodeToPNG());
                Object.DestroyImmediate(sheet);
                Object.DestroyImmediate(go);
            }

            cam.targetTexture = null;
            rt.Release();
        }

        /// <summary>Camera placed along +dir looking back at the model (front view = model's face).</summary>
        static Texture2D RenderView(Camera cam, RenderTexture rt, Bounds bounds, Vector3 dir, float half)
        {
            cam.transform.position = bounds.center + dir * half * 8f;
            cam.transform.LookAt(bounds.center, dir == Vector3.up ? Vector3.forward : Vector3.up);
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(640, 640, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 640, 640), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            return tex;
        }

        public static Bounds Bounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(go.transform.position, Vector3.one * 0.1f);
            var b = renderers[0].bounds;
            foreach (var r in renderers)
                if (r.enabled && r.gameObject.activeInHierarchy)
                    b.Encapsulate(r.bounds);
            return b;
        }

        public static void WriteLog(StringBuilder log)
        {
            Directory.CreateDirectory(OutDir);
            File.WriteAllText(Path.Combine(OutDir, "log.txt"), log.ToString());
        }
    }
}
