using UnityEngine;

/// <summary>
/// Holds the currently active promotion during runtime.
/// Persists across all scene loads.
/// </summary>
public class PromotionSession : MonoBehaviour
{
    public static PromotionSession Instance;
    public PromotionData CurrentPromotion;

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
