using UnityEngine;

namespace ARSurvival.UI
{
    /// <summary>
    /// Shared "ocean night" cartoon palette and formatting so every screen looks consistent:
    /// deep navy water, glowing teal/purple/pink sky flowers, sunny yellow and starfish pink.
    /// </summary>
    public static class UIStyle
    {
        /// <summary>Sunny yellow: primary buttons, key numbers, selected items.</summary>
        public static readonly Color Accent = new(1f, 0.87f, 0.2f);
        public static readonly Color Pink = new(1f, 0.56f, 0.68f);
        public static readonly Color Teal = new(0.3f, 0.95f, 0.9f);
        public static readonly Color Purple = new(0.68f, 0.45f, 1f);
        public static readonly Color Danger = new(1f, 0.36f, 0.38f);
        public static readonly Color Warning = new(1f, 0.62f, 0.22f);
        public static readonly Color Navy = new(0.03f, 0.08f, 0.24f);
        public static readonly Color Panel = new(0.05f, 0.12f, 0.33f, 0.86f);
        public static readonly Color PanelSolid = new(0.06f, 0.13f, 0.35f, 1f);
        public static readonly Color ButtonAlt = new(0.13f, 0.4f, 0.68f, 0.96f);
        public static readonly Color TextPrimary = new(1f, 0.98f, 0.93f);
        public static readonly Color TextMuted = new(0.72f, 0.84f, 0.97f);
        public static readonly Color SegmentOff = new(1f, 1f, 1f, 0.15f);

        /// <summary>Seconds as m:ss (e.g. 1:05).</summary>
        public static string FormatTime(float seconds)
        {
            int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            return $"{total / 60}:{total % 60:00}";
        }
    }
}
