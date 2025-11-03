using UnityEngine;
using UnityEngine.UIElements;

public class LoadPromotionScreen : MonoBehaviour
{
    private ScrollView promotionList;
    private Label statusLabel;
    private Button backButton;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        promotionList = root.Q<ScrollView>("promotionList");
        statusLabel = root.Q<Label>("statusLabel");
        backButton = root.Q<Button>("backButton");

        backButton.clicked += () =>
        {
            if (SceneLoader.Instance != null) SceneLoader.Instance.LoadScene("MainMenu");
            else UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        };

        PopulatePromotionList();
    }

    private void PopulatePromotionList()
    {
        promotionList.Clear();

        string[] promotions = DataManager.ListSavedPromotions();

        if (promotions == null || promotions.Length == 0)
        {
            statusLabel.text = "No saved promotions found.";
            return;
        }

        statusLabel.text = "Select a promotion to load:";

        foreach (string promotionName in promotions)
        {
            Button button = new Button(() => OnPromotionSelected(promotionName))
            {
                text = promotionName
            };

            button.style.height = 30;
            button.style.marginBottom = 5;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.style.alignSelf = Align.Stretch;
            button.style.minWidth = 200;

            promotionList.Add(button);
        }
    }

    private void OnPromotionSelected(string promotionName)
    {
        PromotionData loadedPromotion = DataManager.LoadPromotion(promotionName);

        if (loadedPromotion == null)
        {
            statusLabel.text = $"❌ Failed to load {promotionName}";
            Debug.LogError($"Failed to load promotion: {promotionName}");
            return;
        }

        statusLabel.text = $"✅ Loaded: {loadedPromotion.promotionName}";
        Debug.Log($"Promotion loaded successfully: {loadedPromotion.promotionName}");
        // Store in persistent session before switching scenes
        if (PromotionSession.Instance != null)
            PromotionSession.Instance.CurrentPromotion = loadedPromotion;

        // Load via SceneLoader if available; fallback to SceneManager
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene("PromotionDashboard");
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("PromotionDashboard");
    }
}


