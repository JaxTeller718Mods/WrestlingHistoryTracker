using UnityEngine;

/// <summary>
/// Holds the currently active promotion during runtime.
/// Persists across all scene loads.
/// </summary>
public class PromotionSession : MonoBehaviour
{
    public static PromotionSession Instance;
    public PromotionData CurrentPromotion;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
        if (Instance != null)
            return;

        var sessionObject = new GameObject(nameof(PromotionSession));
        sessionObject.hideFlags = HideFlags.DontSave;
        sessionObject.AddComponent<PromotionSession>();
    }

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // persist through scene changes
    }
}
