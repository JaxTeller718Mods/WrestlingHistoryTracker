using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Centralized scene loader. Handles clean transitions and optional UI cleanup.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

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

    /// <summary>
    /// Loads the target scene by name, cleaning up UI and unused objects first.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        Debug.Log($"Loading scene: {sceneName}");

        // 🔹 Use Unity 6.x (2023+) API properly
        UIDocument[] activeUIDocuments = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);

        foreach (UIDocument doc in activeUIDocuments)
        {
            if (doc != null && doc.gameObject.scene.name != sceneName)
            {
                doc.gameObject.SetActive(false);
                Destroy(doc.gameObject);
            }
        }

        // 🔹 Load the new scene
        SceneManager.LoadScene(sceneName);

        // 🔹 Optional cleanup for memory safety
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }
}
