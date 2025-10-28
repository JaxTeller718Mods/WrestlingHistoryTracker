using UnityEngine;

public class PromotionManager : MonoBehaviour
{
    public static PromotionManager Instance { get; private set; }

    public PromotionData CurrentPromotion { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetCurrentPromotion(PromotionData promotion)
    {
        CurrentPromotion = promotion;
        Debug.Log($"📂 Active Promotion: {promotion.promotionName}");
    }

    public void ClearPromotion()
    {
        CurrentPromotion = null;
    }
}
