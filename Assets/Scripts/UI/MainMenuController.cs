using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuController : MonoBehaviour
{
    private Button newButton;
    private Button loadButton;
    private Button exitButton;

    private void OnEnable()
    {
        // Get root UI document
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Query main buttons (matching your UXML)
        newButton = root.Q<Button>("newButton");
        loadButton = root.Q<Button>("loadButton");
        exitButton = root.Q<Button>("exitButton");

        // Attach event handlers
        newButton.clicked += OnNewClicked;
        loadButton.clicked += OnLoadClicked;
        exitButton.clicked += OnExitClicked;
    }

    private void OnNewClicked()
    {
        Debug.Log("Navigating to Create Promotion scene...");
        SceneLoader.Instance.LoadScene("CreatePromotion");
    }

    private void OnLoadClicked()
    {
        Debug.Log("Navigating to Load Promotion scene...");
        SceneLoader.Instance.LoadScene("LoadPromotion");
    }

    private void OnExitClicked()
    {
        Debug.Log("Exiting application...");
#if UNITY_EDITOR
        EditorApplication.isPlaying = false; // Stop play mode in the Editor
#else
        Application.Quit(); // Quit in a build
#endif
    }
}

