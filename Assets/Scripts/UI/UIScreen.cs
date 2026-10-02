using System.Collections;
using ARSurvival.Audio;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ARSurvival.UI
{
    /// <summary>
    /// Base class for every UI screen. Screens stay active and are shown/hidden through a
    /// CanvasGroup fade, so they keep receiving events while hidden (e.g. the Game Over screen
    /// fills itself in before it becomes visible).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIScreen : MonoBehaviour
    {
        [SerializeField] float fadeDuration = 0.2f;

        CanvasGroup group;
        Coroutine fade;

        public bool IsVisible { get; private set; }

        protected virtual void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            SetInteractive(false);
        }

        public void Show()
        {
            if (IsVisible)
                return;
            IsVisible = true;
            OnShow();
            SetInteractive(true);
            FadeTo(1f);
        }

        public void Hide()
        {
            if (!IsVisible)
                return;
            IsVisible = false;
            SetInteractive(false);
            FadeTo(0f);
            OnHide();
        }

        /// <summary>Refresh contents just before the screen fades in.</summary>
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }

        /// <summary>Wires a button to an action, with the shared UI click sound.</summary>
        protected static void Bind(Button button, UnityAction action)
        {
            if (button == null)
                return;
            button.onClick.AddListener(() =>
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayClick();
                action();
            });
        }

        void SetInteractive(bool interactive)
        {
            group.interactable = interactive;
            group.blocksRaycasts = interactive;
        }

        void FadeTo(float target)
        {
            if (fade != null)
                StopCoroutine(fade);
            if (!isActiveAndEnabled)
            {
                group.alpha = target;
                return;
            }
            fade = StartCoroutine(Fade(target));
        }

        IEnumerator Fade(float target)
        {
            float start = group.alpha;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / fadeDuration)
            {
                group.alpha = Mathf.Lerp(start, target, t);
                yield return null;
            }
            group.alpha = target;
            fade = null;
        }
    }
}
