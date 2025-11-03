using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuController : MonoBehaviour
{
    private VisualElement root;
    private Button newButton;
    private Button loadButton;
    private Button exportButton;
    private Button importButton;
    private Button exitButton;

    private VisualElement exportPanel;
    private VisualElement importPanel;
    private PopupField<string> exportPromotionDropdown;
    private TextField importPathField;
    private Button exportConfirmButton, exportCancelButton, openExportsFolderButton;
    private Button importConfirmButton, importCancelButton;
    private bool exportPanelVisible = false;
    private bool exportDropdownInteracting = false;

    private void OnEnable()
    {
        // Get root UI document
        var rootVE = GetComponent<UIDocument>().rootVisualElement;
        root = rootVE;

        // Query main buttons (matching your UXML)
        newButton = rootVE.Q<Button>("newButton");
        loadButton = rootVE.Q<Button>("loadButton");
        exportButton = rootVE.Q<Button>("exportButton");
        importButton = rootVE.Q<Button>("importButton");
        exitButton = rootVE.Q<Button>("exitButton");

        exportPanel = rootVE.Q<VisualElement>("exportPanel");
        importPanel = rootVE.Q<VisualElement>("importPanel");
        importPathField = rootVE.Q<TextField>("importPathField");
        exportConfirmButton = rootVE.Q<Button>("exportConfirmButton");
        exportCancelButton = rootVE.Q<Button>("exportCancelButton");
        openExportsFolderButton = rootVE.Q<Button>("openExportsFolderButton");
        importConfirmButton = rootVE.Q<Button>("importConfirmButton");
        importCancelButton = rootVE.Q<Button>("importCancelButton");

        // Replace placeholder with PopupField<string>
        EnsureExportPopupField();

        // Attach event handlers
        newButton.clicked += OnNewClicked;
        loadButton.clicked += OnLoadClicked;
        exitButton.clicked += OnExitClicked;
        if (exportButton != null) exportButton.clicked += OnExportClicked;
        if (importButton != null) importButton.clicked += OnImportClicked;
        if (exportConfirmButton != null) exportConfirmButton.clicked += OnExportConfirm;
        if (exportCancelButton != null) exportCancelButton.clicked += () => TogglePanel(exportPanel, false);
        if (openExportsFolderButton != null) openExportsFolderButton.clicked += OnOpenExportsFolder;
        if (importConfirmButton != null) importConfirmButton.clicked += OnImportConfirm;
        if (importCancelButton != null) importCancelButton.clicked += () => TogglePanel(importPanel, false);

        // Track interaction on the export dropdown to avoid refresh churn while open
        if (exportPromotionDropdown != null)
        {
            exportPromotionDropdown.RegisterCallback<PointerDownEvent>(_ => { exportDropdownInteracting = true; });
            exportPromotionDropdown.RegisterCallback<FocusInEvent>(_ => { exportDropdownInteracting = true; });
            exportPromotionDropdown.RegisterCallback<FocusOutEvent>(_ => { exportDropdownInteracting = false; });
        }

        // Swallow keyboard/gamepad navigation while export panel is visible (prevents focus cycling in builds)
        if (root != null)
        {
            root.RegisterCallback<NavigationMoveEvent>(e => { if (exportPanelVisible) e.StopImmediatePropagation(); });
            root.RegisterCallback<NavigationSubmitEvent>(e => { if (exportPanelVisible) e.StopImmediatePropagation(); });
            root.RegisterCallback<NavigationCancelEvent>(e => { if (exportPanelVisible) e.StopImmediatePropagation(); });
            root.RegisterCallback<KeyDownEvent>(e => { if (exportPanelVisible && (e.keyCode == KeyCode.Tab)) e.StopImmediatePropagation(); });
        }
    }

    private void RefreshExportDropdown()
    {
        if (exportPromotionDropdown == null) { EnsureExportPopupField(); if (exportPromotionDropdown == null) return; }
        if (exportDropdownInteracting) return; // guard while user is interacting
        var names = DataManager.ListSavedPromotions();
        var list = new System.Collections.Generic.List<string>(names);
        if (list.Count == 0) list.Add(string.Empty);
        bool changed = exportPromotionDropdown.choices == null || exportPromotionDropdown.choices.Count != list.Count;
        if (!changed)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (!string.Equals(exportPromotionDropdown.choices[i], list[i])) { changed = true; break; }
            }
        }
        if (changed)
        {
            exportPromotionDropdown.choices = list;
            exportPromotionDropdown.value = list[0];
        }
    }

    private void EnsureExportPopupField()
    {
        if (exportPromotionDropdown != null) return;
        if (root == null) return;
        var placeholder = root.Q<VisualElement>("exportPromotionDropdown");
        if (placeholder == null || placeholder.parent == null) return;
        var names = DataManager.ListSavedPromotions();
        var list = new System.Collections.Generic.List<string>(names);
        if (list.Count == 0) list.Add(string.Empty);
        var popup = new PopupField<string>("Select Promotion", list, 0)
        {
            name = "exportPromotionDropdown"
        };
        exportPromotionDropdown = popup;
        placeholder.parent.Add(popup);
        placeholder.RemoveFromHierarchy();
    }

    private void TogglePanel(VisualElement panel, bool show)
    {
        if (panel == null) return;
        if (!show && panel == exportPanel)
        {
            // Close any open dropdown popup before hiding the panel to avoid lingering popups in builds
            exportDropdownInteracting = false;
            exportPromotionDropdown?.Focus();
            exportPromotionDropdown?.Blur();
        }
        if (show) panel.RemoveFromClassList("hidden");
        else panel.AddToClassList("hidden");
        if (panel == exportPanel)
        {
            exportPanelVisible = show;
        }
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

    private void OnExportClicked()
    {
        if (!exportPanelVisible)
        {
            RefreshExportDropdown();
            TogglePanel(exportPanel, true);
        }
    }

    private void OnExportConfirm()
    {
        var name = (exportPromotionDropdown?.value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(name)) { Debug.LogWarning("No promotion selected to export."); TogglePanel(exportPanel, false); return; }
        var path = DataManager.ExportPromotionBundle(name);
        Debug.Log(!string.IsNullOrEmpty(path) ? $"Exported to: {path}" : "Export failed.");
        TogglePanel(exportPanel, false);
    }

    private void OnImportClicked()
    {
        if (importPathField != null) importPathField.value = string.Empty;
        TogglePanel(importPanel, true);
    }

    private void OnOpenExportsFolder()
    {
        var folder = DataManager.GetExportFolderPath();
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(folder)) UnityEditor.EditorUtility.RevealInFinder(folder);
#else
        if (!string.IsNullOrEmpty(folder)) Application.OpenURL("file:///" + folder.Replace("\\", "/"));
#endif
    }

    private void OnImportConfirm()
    {
        var path = (importPathField?.value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(path)) { Debug.LogWarning("Provide a bundle path (.json)."); return; }
        bool ok = DataManager.ImportPromotionBundle(path);
        Debug.Log(ok ? "Import completed." : "Import failed.");
        TogglePanel(importPanel, false);
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
