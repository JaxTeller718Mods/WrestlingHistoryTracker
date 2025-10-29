using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class PromotionDashboard : MonoBehaviour
{
    // ===== Promotion Info =====
    private PromotionData currentPromotion;
    private VisualElement promotionInfoPanel;
    private VisualElement editPanel;
    private Label nameLabel, locationLabel, foundedLabel, descriptionLabel, statusLabel;
    private TextField nameField, locationField, foundedField, descriptionField;
    private Button editPromotionButton, savePromotionButton, cancelPromotionButton;

    // ===== Navigation Buttons =====
    private Button promotionButton, wrestlersButton, titlesButton, showsButton, historyButton, returnButton;

    // ===== Wrestlers =====
    private VisualElement wrestlersPanel;
    private ScrollView wrestlerList;
    private VisualElement wrestlerDetails;
    private TextField wrestlerNameField, wrestlerNicknameField, wrestlerHometownField;
    private Toggle wrestlerIsFemaleToggle;
    private FloatField wrestlerHeightField, wrestlerWeightField;
    private Button addWrestlerButton, saveWrestlersButton, saveWrestlerButton, deleteWrestlerButton, cancelEditButton;
    private TextField newWrestlerField;
    private Toggle newWrestlerIsFemaleToggle;
    private WrestlerCollection wrestlerCollection;
    private VisualElement wrestlerAddPanel;
    private int selectedIndex = -1;

    // ===== Titles =====
    private VisualElement titlesPanel;
    private VisualElement titleAddPanel;
    private ScrollView titleList;
    private VisualElement titleDetails;
    private TextField titleNameField, titleDivisionField, titleEstablishedField, titleChampionField, titleNotesField;
    private Button addTitleButton, saveTitlesButton, saveTitleButton, deleteTitleButton, cancelTitleButton;
    private Button viewHistoryButton;
    private TextField newTitleField;
    private ScrollView titleHistoryList;
    private TitleCollection titleCollection;
    private int selectedTitleIndex = -1;

    // ===== Shows =====
    private VisualElement showsPanel, showDetails, showAddPanel;
    private ScrollView showsList, matchesList;
    private TextField showNameField, showDateField, newShowField, newShowDateField;
    private Button addShowButton, saveShowsButton, saveShowButton, deleteShowButton, cancelShowButton;
    private Button addMatchButton, saveMatchButton, cancelMatchButton;
    private VisualElement matchEditor;
    private TextField matchNameField, wrestlerAField, wrestlerBField, titleInvolvedField, winnerField;
    private Toggle isTitleMatchToggle;
    private ShowData currentEditingShow;

    private string originalShowName;
    private string originalShowDate;

    // ===== Histories =====
    private VisualElement historyPanel;
    private ScrollView matchHistoryList;
    private ScrollView titleLineageList;

    private VisualElement root;
    private VisualElement dashboardChrome;
    private readonly List<VisualElement> mainPanels = new();
    private readonly List<VisualElement> focusablePanels = new();
    private Coroutine initializationRoutine;

    private void OnEnable()
    {
        // ✅ Load active promotion from persistent session
        if (initializationRoutine != null)
        {
            StopCoroutine(initializationRoutine);
        }

        initializationRoutine = StartCoroutine(WaitForPromotionData());
    }

    private void OnDisable()
    {
        if (initializationRoutine != null)
        {
            StopCoroutine(initializationRoutine);
            initializationRoutine = null;
        }
    }

    private System.Collections.IEnumerator WaitForPromotionData()
    {
        const float timeoutSeconds = 2f;
        float startTime = Time.unscaledTime;

        while (PromotionSession.Instance == null || PromotionSession.Instance.CurrentPromotion == null)
        {
            if (Time.unscaledTime - startTime >= timeoutSeconds)
            {
                HandleMissingPromotionSession();
                initializationRoutine = null;
                yield break;
            }

            yield return null;
        }

        InitializeDashboard();
        initializationRoutine = null;
    }

    private void HandleMissingPromotionSession()
    {
        Debug.LogError("❌ No promotion loaded in session. Returning to Main Menu.");

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene("MainMenu");
        }
        else
        {
            Debug.LogWarning("SceneLoader singleton missing. Falling back to direct SceneManager load.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
    private void InitializeDashboard()
    {
        currentPromotion = PromotionSession.Instance.CurrentPromotion;
        Debug.Log($"✅ PromotionDashboard opened for: {currentPromotion.promotionName}");

        root = GetComponent<UIDocument>().rootVisualElement;
        dashboardChrome = root.Q<VisualElement>("dashboardChrome");
        mainPanels.Clear();
        focusablePanels.Clear();

        ExitWrestlerEditMode();


        // ===== Navigation =====
        promotionButton = root.Q<Button>("promotionButton");
        wrestlersButton = root.Q<Button>("wrestlersButton");
        titlesButton = root.Q<Button>("titlesButton");
        showsButton = root.Q<Button>("showsButton");
        historyButton = root.Q<Button>("historyButton");
        returnButton = root.Q<Button>("returnButton");

        statusLabel = root.Q<Label>("statusLabel");

        // ===== Promotion Info =====
        promotionInfoPanel = root.Q<VisualElement>("promotionInfoPanel");
        editPanel = root.Q<VisualElement>("editPanel");
        nameLabel = root.Q<Label>("nameLabel");
        locationLabel = root.Q<Label>("locationLabel");
        foundedLabel = root.Q<Label>("foundedLabel");
        descriptionLabel = root.Q<Label>("descriptionLabel");
        nameField = root.Q<TextField>("nameField");
        locationField = root.Q<TextField>("locationField");
        foundedField = root.Q<TextField>("foundedField");
        descriptionField = root.Q<TextField>("descriptionField");
        editPromotionButton = root.Q<Button>("editPromotionButton");
        savePromotionButton = root.Q<Button>("savePromotionButton");
        cancelPromotionButton = root.Q<Button>("cancelPromotionButton");

        // ===== Wrestlers =====
        wrestlersPanel = root.Q<VisualElement>("wrestlersPanel");
        wrestlerList = root.Q<ScrollView>("wrestlerList");
        wrestlerDetails = root.Q<VisualElement>("wrestlerDetails");
        wrestlerNameField = root.Q<TextField>("wrestlerNameField");
        wrestlerNicknameField = root.Q<TextField>("wrestlerNicknameField");
        wrestlerHometownField = root.Q<TextField>("wrestlerHometownField");
        wrestlerIsFemaleToggle = root.Q<Toggle>("wrestlerIsFemaleToggle");
        wrestlerHeightField = root.Q<FloatField>("wrestlerHeightField");
        wrestlerWeightField = root.Q<FloatField>("wrestlerWeightField");
        addWrestlerButton = root.Q<Button>("addWrestlerButton");
        saveWrestlersButton = root.Q<Button>("saveWrestlersButton");
        saveWrestlerButton = root.Q<Button>("saveWrestlerButton");
        deleteWrestlerButton = root.Q<Button>("deleteWrestlerButton");
        cancelEditButton = root.Q<Button>("cancelEditButton");
        newWrestlerField = root.Q<TextField>("newWrestlerField");
        newWrestlerIsFemaleToggle = root.Q<Toggle>("newWrestlerIsFemaleToggle");
        wrestlerAddPanel = root.Q<VisualElement>("wrestlerAddPanel");

        // ===== Titles =====
        titlesPanel = root.Q<VisualElement>("titlesPanel");
        titleList = root.Q<ScrollView>("titleList");
        titleDetails = root.Q<VisualElement>("titleDetails");
        titleNameField = root.Q<TextField>("titleNameField");
        titleDivisionField = root.Q<TextField>("titleDivisionField");
        titleEstablishedField = root.Q<TextField>("titleEstablishedField");
        titleChampionField = root.Q<TextField>("titleChampionField");
        titleNotesField = root.Q<TextField>("titleNotesField");
        addTitleButton = root.Q<Button>("addTitleButton");
        saveTitlesButton = root.Q<Button>("saveTitlesButton");
        saveTitleButton = root.Q<Button>("saveTitleButton");
        deleteTitleButton = root.Q<Button>("deleteTitleButton");
        cancelTitleButton = root.Q<Button>("cancelTitleButton");
        newTitleField = root.Q<TextField>("newTitleField");
        titleAddPanel = root.Q<VisualElement>("titleAddPanel");
        titleHistoryList = root.Q<ScrollView>("titleHistoryList");
        viewHistoryButton = root.Q<Button>("viewHistoryButton");

        // ===== Shows =====
        showsPanel = root.Q<VisualElement>("showsPanel");
        showsList = root.Q<ScrollView>("showsList");
        showDetails = root.Q<VisualElement>("showDetails");
        showAddPanel = root.Q<VisualElement>("showAddPanel");
        showNameField = root.Q<TextField>("showNameField");
        showDateField = root.Q<TextField>("showDateField");
        newShowField = root.Q<TextField>("newShowField");
        newShowDateField = root.Q<TextField>("newShowDateField");
        addShowButton = root.Q<Button>("addShowButton");
        saveShowsButton = root.Q<Button>("saveShowsButton");
        saveShowButton = root.Q<Button>("saveShowButton");
        deleteShowButton = root.Q<Button>("deleteShowButton");
        cancelShowButton = root.Q<Button>("cancelShowButton");
        matchesList = root.Q<ScrollView>("matchesList");
        matchEditor = root.Q<VisualElement>("matchEditor");
        matchNameField = root.Q<TextField>("matchNameField");
        wrestlerAField = root.Q<TextField>("wrestlerAField");
        wrestlerBField = root.Q<TextField>("wrestlerBField");
        isTitleMatchToggle = root.Q<Toggle>("isTitleMatchToggle");
        titleInvolvedField = root.Q<TextField>("titleInvolvedField");
        winnerField = root.Q<TextField>("winnerField");
        addMatchButton = root.Q<Button>("addMatchButton");
        saveMatchButton = root.Q<Button>("saveMatchButton");
        cancelMatchButton = root.Q<Button>("cancelMatchButton");

        // ===== Histories =====
        historyPanel = root.Q<VisualElement>("historyPanel");
        matchHistoryList = root.Q<ScrollView>("matchHistoryList");
        titleLineageList = root.Q<ScrollView>("titleLineageList");

        RegisterMainPanel(promotionInfoPanel);
        RegisterMainPanel(wrestlersPanel);
        RegisterMainPanel(titlesPanel);
        RegisterMainPanel(showsPanel);
        RegisterMainPanel(historyPanel);

        RegisterFocusablePanel(editPanel, wrestlerDetails, wrestlerAddPanel, titleDetails, titleAddPanel,
            titleHistoryList, showDetails, showAddPanel, matchEditor, showsPanel, historyPanel,
            promotionInfoPanel, wrestlersPanel, titlesPanel);


        // ===== Navigation wiring =====
        if (promotionButton != null)
            promotionButton.clicked += ShowPromotionPanel;
        if (wrestlersButton != null)
            wrestlersButton.clicked += ShowWrestlersPanel;
        if (titlesButton != null)
            titlesButton.clicked += ShowTitlesPanel;
        if (showsButton != null)
            showsButton.clicked += ShowShowsPanel;
        if (historyButton != null)
            historyButton.clicked += ShowHistoryPanel;
        if (returnButton != null)
        {
            returnButton.clicked += () =>
            {
                Debug.Log("Returning to Main Menu...");
                if (PromotionSession.Instance != null)
                    PromotionSession.Instance.CurrentPromotion = null;
                SceneLoader.Instance.LoadScene("MainMenu");
            };
        }

        // ===== Promotion actions =====
        if (editPromotionButton != null)
            editPromotionButton.clicked += () => SetEditMode(true);
        if (savePromotionButton != null)
            savePromotionButton.clicked += SavePromotionChanges;
        if (cancelPromotionButton != null)
            cancelPromotionButton.clicked += () => SetEditMode(false);

        // ===== Wrestler actions =====
        if (addWrestlerButton != null)
            addWrestlerButton.clicked += AddWrestler;
        if (saveWrestlersButton != null)
            saveWrestlersButton.clicked += SaveWrestlers;
        if (saveWrestlerButton != null)
            saveWrestlerButton.clicked += SaveSelectedWrestler;
        if (deleteWrestlerButton != null)
            deleteWrestlerButton.clicked += DeleteSelectedWrestler;
        if (cancelEditButton != null)
            cancelEditButton.clicked += HideWrestlerDetails;

        // ===== Title actions =====
        if (addTitleButton != null)
            addTitleButton.clicked += AddTitle;
        if (saveTitlesButton != null)
            saveTitlesButton.clicked += SaveTitles;
        if (saveTitleButton != null)
            saveTitleButton.clicked += SaveSelectedTitle;
        if (deleteTitleButton != null)
            deleteTitleButton.clicked += DeleteSelectedTitle;
        if (cancelTitleButton != null)
            cancelTitleButton.clicked += HideTitleDetails;
        if (viewHistoryButton != null)
            viewHistoryButton.clicked += ShowSelectedTitleHistory;

        // ===== Shows logic =====
        addShowButton.clicked += () =>
        {
            SetActivePanel(showsPanel);
            var show = new ShowData(newShowField.value, newShowDateField.value);
            currentPromotion.shows.Add(show);
            RefreshShowList();
            newShowField.value = "";
            newShowDateField.value = "";
            FocusPanel(showAddPanel ?? showsPanel);
        };

        saveShowsButton.clicked += () =>
        {
            SetActivePanel(showsPanel);
            DataManager.SavePromotion(currentPromotion);
            RefreshHistoryPanel();
            statusLabel.text = "💾 Shows saved & history refreshed.";
            FocusPanel(showsPanel);
        };

        saveShowButton.clicked += () =>
        {
            if (currentPromotion == null)
                return;

            SetActivePanel(showsPanel);
            TitleHistoryManager.EnsureHistoryLoaded(currentPromotion);
            foreach (ShowData showData in currentPromotion.shows)
            {
                TitleHistoryManager.UpdateShowResults(currentPromotion, showData);
            }

            currentEditingShow.showName = showNameField.value;
            currentEditingShow.date = showDateField.value;

            if (currentEditingShow == null)
            {
                statusLabel.text = "⚠️ No active show selected.";
                return;
            }

            string previousName = originalShowName;
            string previousDate = originalShowDate;

            currentEditingShow.showName = showNameField.value;
            currentEditingShow.date = showDateField.value;

            TitleHistoryManager.UpdateShowResults(currentPromotion, currentEditingShow, previousName, previousDate);

            DataManager.SavePromotion(currentPromotion);
            RefreshHistoryPanel();

            showDetails.AddToClassList("hidden");
            showAddPanel.RemoveFromClassList("hidden");
            RefreshShowList();
            statusLabel.text = $"✅ Show '{currentEditingShow.showName}' saved & history updated.";
            FocusPanel(showAddPanel ?? showsPanel);
        };

        cancelShowButton.clicked += () =>
        {
            SetActivePanel(showsPanel);
            showDetails.AddToClassList("hidden");
            showAddPanel.RemoveFromClassList("hidden");
            FocusPanel(showAddPanel ?? showsPanel);
        };

        deleteShowButton.clicked += () =>
        {
            if (currentEditingShow == null)
                return;

            var deletedShow = currentEditingShow;
            currentPromotion.shows.Remove(currentEditingShow);
            TitleHistoryManager.RemoveShow(currentPromotion, deletedShow);
            DataManager.SavePromotion(currentPromotion);
            RefreshHistoryPanel();
            currentEditingShow = null;
            RefreshShowList();
            showDetails.AddToClassList("hidden");
            showAddPanel.RemoveFromClassList("hidden");
            statusLabel.text = "🗑️ Show deleted and history cleaned.";
            SetActivePanel(showsPanel);
            FocusPanel(showAddPanel ?? showsPanel);
        };

        addMatchButton.clicked += () =>
        {
            SetActivePanel(showsPanel);
            matchEditor.RemoveFromClassList("hidden");
            FocusPanel(matchEditor);
        };

        saveMatchButton.clicked += () =>
        {
            SetActivePanel(showsPanel);
            var match = new MatchData
            {
                matchName = matchNameField.value,
                wrestlerA = wrestlerAField.value,
                wrestlerB = wrestlerBField.value,
                isTitleMatch = isTitleMatchToggle.value,
                titleName = titleInvolvedField.value,
                winner = winnerField.value
            };

            if (currentEditingShow == null)
            {
                statusLabel.text = "⚠️ No active show selected.";
                return;
            }

            currentEditingShow.matches.Add(match);
            matchEditor.AddToClassList("hidden");
            RefreshMatchList();
            statusLabel.text = $"✅ Match '{match.matchName}' added.";
            matchNameField.value = "";
            wrestlerAField.value = "";
            wrestlerBField.value = "";
            isTitleMatchToggle.value = false;
            titleInvolvedField.value = "";
            winnerField.value = "";
            FocusPanel(showDetails ?? showsPanel);
        };

        cancelMatchButton.clicked += () =>
        {
            SetActivePanel(showsPanel);
            matchEditor.AddToClassList("hidden");
            FocusPanel(showDetails ?? showsPanel);
        };

        LoadPromotionData();
        SetActivePanel(promotionInfoPanel);
    }

    // ---------------- Promotion Info ----------------
    private void LoadPromotionData()
    {
        currentPromotion = PromotionSession.Instance?.CurrentPromotion;
        if (currentPromotion == null)
        {
            statusLabel.text = "❌ No promotion loaded!";
            Debug.LogError("❌ LoadPromotionData: currentPromotion is null");
            return;
        }

        nameLabel.text = $"Name: {currentPromotion.promotionName}";
        locationLabel.text = $"Location: {currentPromotion.location}";
        foundedLabel.text = $"Founded: {currentPromotion.foundedYear}";
        descriptionLabel.text = $"Description: {currentPromotion.description}";

        wrestlerCollection = DataManager.LoadWrestlers(currentPromotion.promotionName);
        titleCollection = DataManager.LoadTitles(currentPromotion.promotionName);
        TitleHistoryManager.EnsureHistoryLoaded(currentPromotion);
        RefreshWrestlerList();
        RefreshTitleList();
        RefreshShowList();
        RefreshHistoryPanel();
    }

    private void SetEditMode(bool enable)
    {
        SetActivePanel(promotionInfoPanel);

        if (enable)
        {
            nameField.value = currentPromotion.promotionName;
            locationField.value = currentPromotion.location;
            foundedField.value = currentPromotion.foundedYear;
            descriptionField.value = currentPromotion.description;
            nameLabel.AddToClassList("hidden");
            locationLabel.AddToClassList("hidden");
            foundedLabel.AddToClassList("hidden");
            descriptionLabel.AddToClassList("hidden");
            editPromotionButton?.AddToClassList("hidden");
            editPanel?.RemoveFromClassList("hidden");
            FocusPanel(editPanel ?? promotionInfoPanel);
        }
        else
        {
            nameLabel.RemoveFromClassList("hidden");
            locationLabel.RemoveFromClassList("hidden");
            foundedLabel.RemoveFromClassList("hidden");
            descriptionLabel.RemoveFromClassList("hidden");
            editPromotionButton?.RemoveFromClassList("hidden");
            editPanel?.AddToClassList("hidden");
        }
    }

    private void SavePromotionChanges()
    {
        currentPromotion.promotionName = nameField.value.Trim();
        currentPromotion.location = locationField.value.Trim();
        currentPromotion.foundedYear = foundedField.value.Trim();
        currentPromotion.description = descriptionField.value.Trim();

        DataManager.SavePromotion(currentPromotion);
        LoadPromotionData();
        SetEditMode(false);
        statusLabel.text = "💾 Promotion saved!";
    }

    // ---------------- Navigation ----------------
    private void ShowPromotionPanel()
    {
        ExitWrestlerEditMode();
        SetActivePanel(promotionInfoPanel);
        SetEditMode(false);
    }

    private void ShowWrestlersPanel()
    {
        if (wrestlerDetails != null && !wrestlerDetails.ClassListContains("hidden"))
            EnterWrestlerEditMode();
        else
            ExitWrestlerEditMode();
        SetActivePanel(wrestlersPanel);
        if (wrestlerDetails != null && !wrestlerDetails.ClassListContains("hidden"))
            FocusPanel(wrestlerDetails);
    }

    private void ShowTitlesPanel()
    {
        SetActivePanel(titlesPanel);
        if (titleHistoryList != null)
        {
            titleHistoryList.AddToClassList("hidden");
            titleHistoryList.Clear();
        }

        if (selectedTitleIndex < 0)
            titleAddPanel?.RemoveFromClassList("hidden");

        if (titleDetails != null && !titleDetails.ClassListContains("hidden"))
            FocusPanel(titleDetails);
        else if (titleAddPanel != null)
            FocusPanel(titleAddPanel);
    }

    private void ShowShowsPanel()
    {
        ExitWrestlerEditMode();
        SetActivePanel(showsPanel);
        matchEditor?.AddToClassList("hidden");

        if (currentEditingShow == null)
        {
            showDetails?.AddToClassList("hidden");
            showAddPanel?.RemoveFromClassList("hidden");
            FocusPanel(showAddPanel ?? showsPanel);
        }
        else if (showDetails != null)
        {
            FocusPanel(showDetails);
        }
    }

    private void ShowHistoryPanel()
    {
        ExitWrestlerEditMode();
        SetActivePanel(historyPanel);
        RefreshHistoryPanel();
    }

    private void RegisterMainPanel(VisualElement panel)
    {
        if (panel == null)
            return;

        if (!mainPanels.Contains(panel))
            mainPanels.Add(panel);

        RegisterFocusablePanel(panel);
    }

    private void RegisterFocusablePanel(params VisualElement[] panels)
    {
        if (panels == null)
            return;

        foreach (var panel in panels)
        {
            if (panel == null)
                continue;

            if (!focusablePanels.Contains(panel))
                focusablePanels.Add(panel);
        }
    }

    private void EnterWrestlerEditMode()
    {
        root?.AddToClassList("editing-wrestler");
        dashboardChrome?.AddToClassList("hidden");
    }

    private void ExitWrestlerEditMode()
    {
        root?.RemoveFromClassList("editing-wrestler");
        dashboardChrome?.RemoveFromClassList("hidden");
    }

    private void SetActivePanel(VisualElement panel)
    {
        if (panel == null)
            return;

        foreach (var mainPanel in mainPanels)
        {
            if (mainPanel == null)
                continue;

            if (mainPanel == panel)
                mainPanel.RemoveFromClassList("hidden");
            else
                mainPanel.AddToClassList("hidden");
        }

        FocusPanel(panel);
    }

    private void FocusPanel(VisualElement panel)
    {
        if (panel == null)
            return;

        if (!focusablePanels.Contains(panel))
            focusablePanels.Add(panel);

        foreach (var element in focusablePanels)
            element?.RemoveFromClassList("focused-panel");

        panel.AddToClassList("focused-panel");

        if (root == null)
            return;

        root.schedule.Execute(() =>
        {
            var scrollParent = panel.GetFirstAncestorOfType<ScrollView>();
            if (scrollParent != null)
                scrollParent.ScrollTo(panel);

            panel.Focus();
        });
    }

    // ---------------- Wrestlers Logic ----------------
    private void RefreshWrestlerList()
    {
        wrestlerList.Clear();
        if (wrestlerCollection == null || wrestlerCollection.wrestlers.Count == 0)
        {
            var empty = new Label("No wrestlers added yet.") { style = { color = Color.gray } };
            wrestlerList.Add(empty);
            return;
        }
        for (int i = 0; i < wrestlerCollection.wrestlers.Count; i++)
        {
            int index = i;
            var w = wrestlerCollection.wrestlers[i];
            Button button = new(() => SelectWrestler(index)) { text = $"• {w.name}" };
            wrestlerList.Add(button);
        }
    }

    private void AddWrestler()
    {
        string name = newWrestlerField.value.Trim();
        if (string.IsNullOrEmpty(name))
        {
            statusLabel.text = "Enter a name first.";
            return;
        }
        if (wrestlerCollection == null)
            wrestlerCollection = new WrestlerCollection { promotionName = currentPromotion.promotionName };
        wrestlerCollection.wrestlers.Add(new WrestlerData { name = name, isFemale = (newWrestlerIsFemaleToggle != null && newWrestlerIsFemaleToggle.value) });
        newWrestlerField.value = "";
        if (newWrestlerIsFemaleToggle != null) newWrestlerIsFemaleToggle.value = false;
        RefreshWrestlerList();
        FocusPanel(wrestlerAddPanel ?? wrestlersPanel);
    }

    private void SelectWrestler(int index)
    {
        if (wrestlerCollection == null || index < 0 || index >= wrestlerCollection.wrestlers.Count)
            return;
        selectedIndex = index;
        SetActivePanel(wrestlersPanel);
        var w = wrestlerCollection.wrestlers[index];
        promotionInfoPanel.AddToClassList("hidden");
        titlesPanel.AddToClassList("hidden");
        wrestlerNameField.value = w.name;
        wrestlerNicknameField.value = w.nickname;
        wrestlerHometownField.value = w.hometown;
        wrestlerIsFemaleToggle.value = w.isFemale;
        wrestlerHeightField.value = w.height;
        wrestlerWeightField.value = w.weight;
        wrestlerAddPanel.AddToClassList("hidden");
        wrestlerDetails.RemoveFromClassList("hidden");
        EnterWrestlerEditMode();
        FocusPanel(wrestlerDetails);
    }

    private void HideWrestlerDetails()
    {
        wrestlerDetails.AddToClassList("hidden");
        wrestlerAddPanel.RemoveFromClassList("hidden");
        selectedIndex = -1;
        SetActivePanel(wrestlersPanel);
        EnterWrestlerEditMode();
        FocusPanel(wrestlerDetails);
    }

    private void SaveSelectedWrestler()
    {
        if (selectedIndex < 0 || wrestlerCollection == null)
            return;
        var w = wrestlerCollection.wrestlers[selectedIndex];
        w.name = wrestlerNameField.value.Trim();
        w.nickname = wrestlerNicknameField.value.Trim();
        w.hometown = wrestlerHometownField.value.Trim();
        w.isFemale = wrestlerIsFemaleToggle != null && wrestlerIsFemaleToggle.value;
        w.height = wrestlerHeightField.value;
        w.weight = wrestlerWeightField.value;
        DataManager.SaveWrestlers(wrestlerCollection);
        RefreshWrestlerList();
        HideWrestlerDetails();
        statusLabel.text = "💾 Wrestler saved!";
    }

    private void DeleteSelectedWrestler()
    {
        if (selectedIndex < 0 || wrestlerCollection == null)
            return;
        string name = wrestlerCollection.wrestlers[selectedIndex].name;
        wrestlerCollection.wrestlers.RemoveAt(selectedIndex);
        selectedIndex = -1;
        DataManager.SaveWrestlers(wrestlerCollection);
        RefreshWrestlerList();
        HideWrestlerDetails();
        statusLabel.text = $"🗑️ Deleted {name}";
    }

    private void SaveWrestlers()
    {
        if (wrestlerCollection != null)
            DataManager.SaveWrestlers(wrestlerCollection);
        statusLabel.text = "💾 All wrestlers saved.";
    }

    // ---------------- Titles Logic ----------------
    private void RefreshTitleList()
    {
        titleList.Clear();
        if (titleCollection == null || titleCollection.titles.Count == 0)
        {
            var empty = new Label("No titles created yet.") { style = { color = Color.gray } };
            titleList.Add(empty);
            return;
        }
        for (int i = 0; i < titleCollection.titles.Count; i++)
        {
            int index = i;
            var t = titleCollection.titles[i];
            Button btn = new(() => SelectTitle(index)) { text = $"• {t.titleName}" };
            titleList.Add(btn);
        }
    }

    private void AddTitle()
    {
        string name = newTitleField.value.Trim();
        if (string.IsNullOrEmpty(name))
        {
            statusLabel.text = "Enter a title name first.";
            return;
        }
        if (titleCollection == null)
            titleCollection = new TitleCollection { promotionName = currentPromotion.promotionName };
        titleCollection.titles.Add(new TitleData { titleName = name });
        newTitleField.value = "";
        RefreshTitleList();
        FocusPanel(titleAddPanel ?? titlesPanel);
    }

    private void SelectTitle(int index)
    {
        if (titleCollection == null || index < 0 || index >= titleCollection.titles.Count)
            return;
        selectedTitleIndex = index;
        SetActivePanel(titlesPanel);
        var t = titleCollection.titles[index];
        promotionInfoPanel.AddToClassList("hidden");
        wrestlersPanel.AddToClassList("hidden");
        titleNameField.value = t.titleName;
        titleDivisionField.value = t.division;
        titleEstablishedField.value = t.establishedYear;
        titleChampionField.value = t.currentChampion;
        titleNotesField.value = t.notes;
        titleAddPanel.AddToClassList("hidden");
        titleDetails.RemoveFromClassList("hidden");
        titleHistoryList?.AddToClassList("hidden");
        titleHistoryList?.Clear();
        FocusPanel(titleDetails);
    }

    private void HideTitleDetails()
    {
        titleDetails.AddToClassList("hidden");
        titleAddPanel.RemoveFromClassList("hidden");
        selectedTitleIndex = -1;
        titleHistoryList?.AddToClassList("hidden");
        titleHistoryList?.Clear();
        SetActivePanel(titlesPanel);
        FocusPanel(titleAddPanel ?? titlesPanel);
    }

    private void SaveSelectedTitle()
    {
        if (selectedTitleIndex < 0 || titleCollection == null)
            return;
        var t = titleCollection.titles[selectedTitleIndex];
        t.titleName = titleNameField.value.Trim();
        t.division = titleDivisionField.value.Trim();
        t.establishedYear = titleEstablishedField.value.Trim();
        t.currentChampion = titleChampionField.value.Trim();
        t.notes = titleNotesField.value.Trim();
        DataManager.SaveTitles(titleCollection);
        RefreshTitleList();
        HideTitleDetails();
        statusLabel.text = $"💾 Saved {t.titleName}";
    }

    private void DeleteSelectedTitle()
    {
        if (selectedTitleIndex < 0 || titleCollection == null)
            return;
        string name = titleCollection.titles[selectedTitleIndex].titleName;
        titleCollection.titles.RemoveAt(selectedTitleIndex);
        selectedTitleIndex = -1;
        DataManager.SaveTitles(titleCollection);
        RefreshTitleList();
        HideTitleDetails();
        statusLabel.text = $"🗑️ Deleted {name}";
    }

    private void SaveTitles()
    {
        if (titleCollection != null)
            DataManager.SaveTitles(titleCollection);
        statusLabel.text = "💾 All titles saved.";
    }

    private void ShowSelectedTitleHistory()
    {
        if (titleHistoryList == null)
            return;

        SetActivePanel(titlesPanel);

        if (currentPromotion == null)
        {
            statusLabel.text = "❌ No promotion loaded!";
            return;
        }

        if (selectedTitleIndex < 0 || titleCollection == null || selectedTitleIndex >= titleCollection.titles.Count)
        {
            statusLabel.text = "Select a title to view its history.";
            return;
        }

        var selectedTitle = titleCollection.titles[selectedTitleIndex];
        var history = TitleHistoryManager.GetHistory(currentPromotion.promotionName, selectedTitle.titleName);

        titleHistoryList.Clear();

        if (history == null || history.Count == 0)
        {
            var emptyLabel = new Label("No title history recorded yet.") { style = { color = Color.gray } };
            titleHistoryList.Add(emptyLabel);
        }
        else
        {
            foreach (var entry in history)
            {
                var entryElement = new VisualElement();
                entryElement.style.marginBottom = 6;
                entryElement.Add(new Label($"{entry.date} - {entry.matchName}"));
                entryElement.Add(new Label($"Winner: {entry.winner}"));
                titleHistoryList.Add(entryElement);
            }
        }

        titleDetails?.AddToClassList("hidden");
        titleAddPanel?.AddToClassList("hidden");
        titleHistoryList.RemoveFromClassList("hidden");
        FocusPanel(titleHistoryList);
        statusLabel.text = $"📜 Showing history for {selectedTitle.titleName}.";
    }


    // ---------------- Shows & Matches ----------------
    private void RefreshShowList()
    {
        showsList.Clear();
        if (currentPromotion == null || currentPromotion.shows == null)
            return;
        foreach (var show in currentPromotion.shows)
        {
            var label = new Label($"{show.showName} ({show.date})");
            label.RegisterCallback<ClickEvent>(_ => EditShow(show));
            showsList.Add(label);
        }
    }

    private void EditShow(ShowData show)
    {
        currentEditingShow = show;
        originalShowName = show.showName;
        originalShowDate = show.date;
        SetActivePanel(showsPanel);
        showAddPanel.AddToClassList("hidden");
        showDetails.RemoveFromClassList("hidden");
        showNameField.value = show.showName;
        showDateField.value = show.date;
        RefreshMatchList();
        FocusPanel(showDetails);
    }

    private void RefreshMatchList()
    {
        matchesList.Clear();
        if (currentEditingShow == null)
            return;
        foreach (var match in currentEditingShow.matches)
        {
            var label = new Label($"{match.matchName} - {match.wrestlerA} vs {match.wrestlerB}");
            matchesList.Add(label);
        }
    }

    private void RefreshHistoryPanel()
    {
        RefreshMatchHistoryList();
        RefreshTitleLineageList();
    }

    private void RefreshMatchHistoryList()
    {
        if (matchHistoryList == null || currentPromotion == null)
            return;

        matchHistoryList.Clear();

        var results = TitleHistoryManager.GetAllMatchResults(currentPromotion.promotionName);
        if (results == null || results.Count == 0)
        {
            matchHistoryList.Add(new Label("No match results recorded yet.") { style = { color = Color.gray } });
            return;
        }

        foreach (var result in results)
        {
            var entry = new VisualElement();
            entry.style.marginBottom = 6;
            entry.Add(new Label(string.IsNullOrEmpty(result.date) ? result.showName : $"{result.date} - {result.showName}"));
            entry.Add(new Label($"{result.matchName}: {result.wrestlerA} vs {result.wrestlerB}"));
            if (!string.IsNullOrEmpty(result.winner))
                entry.Add(new Label($"Winner: {result.winner}"));
            if (result.isTitleMatch && !string.IsNullOrEmpty(result.titleInvolved))
                entry.Add(new Label($"Title: {result.titleInvolved}"));
            matchHistoryList.Add(entry);
        }
    }

    private void RefreshTitleLineageList()
    {
        if (titleLineageList == null || currentPromotion == null)
            return;

        titleLineageList.Clear();

        var lineages = TitleHistoryManager.GetTitleLineages(currentPromotion.promotionName);
        if (lineages == null || lineages.Count == 0)
        {
            titleLineageList.Add(new Label("No title reigns recorded yet.") { style = { color = Color.gray } });
            return;
        }

        foreach (var lineage in lineages)
        {
            var titleHeader = new Label(lineage.titleName) { style = { unityFontStyleAndWeight = FontStyle.Bold } };
            titleLineageList.Add(titleHeader);

            if (lineage.reigns == null || lineage.reigns.Count == 0)
                continue;

            foreach (var reign in lineage.reigns)
            {
                string reignRange = string.IsNullOrEmpty(reign.dateLost) ? $"{reign.dateWon} - Present" : $"{reign.dateWon} - {reign.dateLost}";
                var reignLabel = new Label($"{reignRange}: {reign.championName} ({reign.eventName})");
                reignLabel.style.marginLeft = 10;
                titleLineageList.Add(reignLabel);
            }
        }
    }
}
