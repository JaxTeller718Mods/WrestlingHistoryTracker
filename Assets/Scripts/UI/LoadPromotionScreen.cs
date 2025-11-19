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
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 5;
            row.style.alignItems = Align.Center;

            Button loadButton = new Button(() => OnPromotionSelected(promotionName))
            {
                text = promotionName
            };
            loadButton.style.height = 30;
            loadButton.style.flexGrow = 1;
            loadButton.style.unityTextAlign = TextAnchor.MiddleCenter;

            Button deleteButton = new Button(() => OnDeletePromotionClicked(promotionName))
            {
                text = "Delete"
            };
            deleteButton.style.height = 30;
            deleteButton.style.marginLeft = 6;
            deleteButton.style.minWidth = 80;

            row.Add(loadButton);
            row.Add(deleteButton);

            promotionList.Add(row);
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

    private void OnDeletePromotionClicked(string promotionName)
    {
        if (string.IsNullOrWhiteSpace(promotionName)) return;

        bool deleted = DataManager.DeletePromotionAndAllData(promotionName);
        if (deleted)
        {
            statusLabel.text = $"Promotion '{promotionName}' deleted.";
            PopulatePromotionList();
        }
        else
        {
            statusLabel.text = $"Failed to delete promotion '{promotionName}'.";
        }
    }
}
