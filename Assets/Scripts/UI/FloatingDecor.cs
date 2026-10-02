using UnityEngine;

namespace ARSurvival.UI
{
    /// <summary>
    /// Ambient menu decoration: the glowing sky flowers gently bob and sway. Animates a fixed set
    /// of UI elements; nothing is created at runtime.
    /// </summary>
    public class FloatingDecor : MonoBehaviour
    {
        [SerializeField] RectTransform[] flowers;

        Vector2[] home;

        void Awake()
        {
            home = new Vector2[flowers.Length];
            for (int i = 0; i < flowers.Length; i++)
                home[i] = flowers[i].anchoredPosition;
        }

        void Update()
        {
            float t = Time.unscaledTime;
            for (int i = 0; i < flowers.Length; i++)
            {
                float phase = i * 1.37f;
                flowers[i].anchoredPosition = home[i] + new Vector2(0f, Mathf.Sin(t * 0.6f + phase) * 14f);
                flowers[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.25f + phase) * 12f);
            }
        }
    }
}
