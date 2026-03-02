using UnityEngine;

namespace LastDay.Utilities
{
    /// <summary>
    /// Generic singleton base class for MonoBehaviours.
    /// Inherit from this instead of MonoBehaviour for manager classes.
    /// </summary>
    /// <typeparam name="T">The concrete manager type</typeparam>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        [Header("Singleton Settings")]
        [SerializeField] private bool persistAcrossScenes = true;

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[Singleton] Duplicate {typeof(T).Name} found on {gameObject.name}. Destroying.");
                Destroy(gameObject);
                return;
            }

            Instance = (T)(MonoBehaviour)this;

            if (persistAcrossScenes && transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
