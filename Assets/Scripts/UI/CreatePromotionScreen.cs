using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public class CreatePromotionScreen : MonoBehaviour
{
    private VisualElement root;
    private TextField nameField;
    private TextField locationField;
    private TextField foundedField;
    private TextField descriptionField;
    private Button saveButton;
    private Button backButton;

    private void OnEnable()
    {
        // Get UI Document root
        root = GetComponent<UIDocument>().rootVisualElement;

        nameField = root.Q<TextField>("promotionNameField");
        locationField = root.Q<TextField>("locationField");
        foundedField = root.Q<TextField>("foundedField");
        descriptionField = root.Q<TextField>("descriptionField");
        saveButton = root.Q<Button>("saveButton");
        backButton = root.Q<Button>("backButton");

        saveButton.clicked += OnSaveClicked;
        backButton.clicked += OnBackClicked;
    }

    private void OnSaveClicked()
    {
        // Collect data from fields
        var promotion = new PromotionData
        {
            promotionName = nameField.value,
            location = locationField.value,
            foundedYear = foundedField.value,
            description = descriptionField.value
        };

        // Prompt for file name (temporary simple version)
        string folderPath = Path.Combine(Application.persistentDataPath, "Promotions");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(folderPath, promotion.promotionName + ".json");
        promotion.SaveToFile(filePath);

        Debug.Log($"Promotion saved: {filePath}");

        // Return to main menu
        SceneLoader.Instance.LoadScene("MainMenu");
    }

    private void OnBackClicked()
    {
        SceneLoader.Instance.LoadScene("MainMenu");
    }
}
