using ARSurvival.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace ARSurvival.UI
{
    /// <summary>
    /// Placement: guides the player through scanning and tap-to-place. The prompt and the
    /// scanning reticle change once ARCore has found a surface. Only the Abort button catches
    /// touches, so taps reach the AR raycast.
    /// </summary>
    public class PlacementScreen : UIScreen
    {
        const string ScanningText = "Move your phone slowly over the floor to find a good spot";
        const string ReadyText = "Spot found! Tap the glowing ground to set up camp";

        [SerializeField] TMP_Text instruction;
        [SerializeField] TMP_Text status;
        [SerializeField] RectTransform reticle;
        [SerializeField] Button backButton;
        [SerializeField] ARPlaneManager planeManager;

        protected override void Awake()
        {
            base.Awake();
            Bind(backButton, () => GameManager.Instance.ReturnToMenu());
        }

        protected override void OnShow() => UpdatePrompt();

        void Update()
        {
            if (IsVisible)
                UpdatePrompt();
        }

        void UpdatePrompt()
        {
            bool surfaceFound = planeManager != null && planeManager.trackables.count > 0;
            instruction.text = surfaceFound ? ReadyText : ScanningText;
            status.text = surfaceFound ? "READY!" : "SEARCHING...";
            status.color = surfaceFound ? UIStyle.Accent : UIStyle.TextMuted;

            // Reticle breathes while scanning and tightens once a surface is found.
            float t = Time.unscaledTime;
            float scale = surfaceFound ? 0.9f + 0.03f * Mathf.Sin(t * 6f) : 1f + 0.08f * Mathf.Sin(t * 2.5f);
            reticle.localScale = Vector3.one * scale;
        }
    }
}
