using UnityEngine;

namespace ARSurvival.Core
{
    /// <summary>
    /// Singleton pattern for scene-level managers (GameManager, AudioManager).
    /// The game runs in a single scene, so instances are not carried across scene loads.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Duplicate {typeof(T).Name} destroyed.", this);
                Destroy(gameObject);
                return;
            }
            Instance = (T)this;
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
