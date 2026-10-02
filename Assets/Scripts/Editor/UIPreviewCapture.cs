using System.IO;
using ARSurvival.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ARSurvival.EditorTools
{
    /// <summary>
    /// Dev tool: renders each UI screen at 1080x1920 to PNGs (in Library/UIPreview) so layouts can be
    /// reviewed without a device. Does not save any change to the scene.
    /// </summary>
    public static class UIPreviewCapture
    {
        [MenuItem("Tools/AR Survival/Capture UI Previews")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene(ProjectPaths.GameScene);
            var outDir = Path.GetFullPath("Library/UIPreview");
            Directory.CreateDirectory(outDir);

            var canvas = Object.FindAnyObjectByType<UIManager>().GetComponent<Canvas>();
            var camGo = new GameObject("PreviewCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.35f, 0.33f, 0.3f); // stand-in for the camera feed
            var rt = new RenderTexture(1080, 1920, 24);
            cam.targetTexture = rt;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;

            // Sample data for dynamic fields.
            SetText("PlacementScreen/Card/Instruction", "Spot found! Tap the glowing ground to set up camp");
            SetText("PlacementScreen/Card/Status", "READY!");
            SetText("MainMenuScreen/Card/DifficultyHint", "Survive 1:30  ~  up to 3 enemies at once");
            SetText("GameOverScreen/Stats/FINALSCORE/Value", "340");
            SetText("GameOverScreen/Stats/ENEMIESDEFEATED/Value", "21");
            SetText("GameOverScreen/Stats/TIMESURVIVED/Value", "1:12");
            SetText("HudScreen/KillFeed", "ORK BLASTED!  <color=#FFDE33>+25</color>");
            SetText("HudScreen/RoundBanner/Subtitle", "SURVIVE 1:30  ~  SHALLOWS");
            SetText("HudScreen/HealthPanel/Value", "14");
            SetText("LeaderboardScreen/Card/EmptyText", "");
            var segments = canvas.transform.Find("SafeArea/HudScreen/HealthPanel");
            for (int i = 7; i < 10; i++)
                segments.Find($"Seg_{i}").GetComponent<UnityEngine.UI.Image>().color = UIStyle.SegmentOff;
            var sampleRow = canvas.transform.Find("SafeArea/LeaderboardScreen/Card/Row_0");
            string[] sample = { "29 Sep 14:05", "Deep Sea", "340", "21", "1:12" };
            for (int i = 0; i < sample.Length; i++)
                sampleRow.Find($"Col_{i}").GetComponent<TMP_Text>().text = sample[i];

            var screens = canvas.GetComponentsInChildren<UIScreen>(true);
            foreach (var screen in screens)
            {
                foreach (var other in screens)
                    other.GetComponent<CanvasGroup>().alpha = other == screen ? 1f : 0f;
                if (screen is HudScreen hud)
                {
                    // Show the transient feedback elements so they can be reviewed too.
                    hud.transform.Find("RoundBanner").GetComponent<CanvasGroup>().alpha = 1f;
                    SetAlpha(hud.transform.Find("DamageVignette"), 0.5f);
                    SetAlpha(hud.transform.Find("HitMarker"), 1f);
                    hud.transform.Find("KillFeed").GetComponent<TMP_Text>().alpha = 1f;
                }
                if (screen is LeaderboardScreen)
                    canvas.transform.Find("SafeArea/MainMenuScreen").GetComponent<CanvasGroup>().alpha = 1f;

                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(outDir, screen.name + ".png"), tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }

            RenderTexture.active = null;
            Object.DestroyImmediate(camGo);
            rt.Release();
            // Reopen from disk so none of the preview changes are kept.
            EditorSceneManager.OpenScene(ProjectPaths.GameScene);
            Debug.Log($"[AR Survival] UI previews written to {outDir}");

            static void SetAlpha(Transform t, float alpha)
            {
                var graphic = t.GetComponent<UnityEngine.UI.Graphic>();
                var c = graphic.color;
                c.a = alpha;
                graphic.color = c;
            }

            void SetText(string path, string value)
            {
                var t = canvas.transform.Find("SafeArea/" + path);
                if (t != null)
                    t.GetComponent<TMP_Text>().text = value;
            }
        }
    }
}
