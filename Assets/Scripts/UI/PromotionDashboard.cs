using UnityEngine;
        using UnityEngine.UIElements;
        using System.Collections.Generic;
        using System.Linq;
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
        private Button promotionButton, wrestlersButton, titlesButton, showsButton, historyButton, rankingsButton, returnButton;
        // ===== Wrestlers =====
        private VisualElement wrestlersPanel;
    private ScrollView wrestlerList;
    private ListView wrestlerListView;
        private VisualElement wrestlerDetails;
        private TextField wrestlerNameField, wrestlerHometownField;
        private Toggle wrestlerIsFemaleToggle, wrestlerIsTagTeamToggle;
        private FloatField wrestlerHeightField, wrestlerWeightField;
        private Button addWrestlerButton, saveWrestlersButton, saveWrestlerButton, deleteWrestlerButton, cancelEditButton;
        private TextField newWrestlerField;
        private Toggle newWrestlerIsFemaleToggle;
        private Toggle newWrestlerIsTagTeamToggle;
        private WrestlerCollection wrestlerCollection;
        private VisualElement wrestlerAddPanel;
        private int selectedIndex = -1;
        // ===== Titles =====
        private VisualElement titlesPanel;
        private VisualElement titleAddPanel;
        private ScrollView titleList;
        private ListView titleListView;
        private VisualElement titleDetails;
        private TextField titleNameField, titleDivisionField, titleChampionField, titleNotesField;
        private Button addTitleButton, saveTitlesButton, saveTitleButton, deleteTitleButton, cancelTitleButton;
        private Button viewHistoryButton;
        private TextField newTitleField;
        private ScrollView titleHistoryList;
        private TitleCollection titleCollection;
        private int selectedTitleIndex = -1;
        // ===== Shows =====
        private VisualElement showsPanel, showDetails, showAddPanel;
        private ScrollView showsList, matchesList;
        private ListView showsListView;
        private TextField showNameField, showDateField, newShowField, newShowDateField;
        private Button addShowButton, saveShowsButton, saveShowButton, deleteShowButton, cancelShowButton;
        private Button addMatchButton, saveMatchButton, cancelMatchButton;
            private Button viewMatchesButton;
        private Button addSegmentButton, saveSegmentButton, cancelSegmentButton;
        private VisualElement matchEditor, segmentEditor, matchesView;
        private DropdownField matchTypeDropdown;
        private DropdownField wrestlerADropdown, wrestlerBDropdown, wrestlerCDropdown, wrestlerDDropdown, winnerDropdown;
    private bool winnerHandlersHooked;
        private TextField segmentTextField;
        private DropdownField titleDropdown;
        private Toggle isTitleMatchToggle;
        private ShowData currentEditingShow;
        private string originalShowName;
        private string originalShowDate;
        // ===== Histories =====
        private VisualElement historyPanel;
        private ScrollView matchHistoryList;
        private ScrollView titleLineageList;
        private VisualElement historyShowsPanel, historyResultsPanel;
        private ScrollView historyShowsList, historyShowMatchesList;
        private ListView historyShowsListView;
        private Button historyCloseResultsButton;
        private Label historyResultsHeader;
        // ===== Rankings =====
        private VisualElement rankingsPanel;
        private ScrollView rankingsList;
        private Button rankingsMenButton, rankingsWomenButton, rankingsTagButton;
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
        rankingsButton = root.Q<Button>("rankingsButton");
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
        wrestlerIsTagTeamToggle = root.Q<Toggle>("wrestlerIsTagTeamToggle");
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
        newWrestlerIsTagTeamToggle = root.Q<Toggle>("newWrestlerIsTagTeamToggle");
        wrestlerAddPanel = root.Q<VisualElement>("wrestlerAddPanel");
        EnsureWrestlerListView();
        // ===== Titles =====
        titlesPanel = root.Q<VisualElement>("titlesPanel");
        titleList = root.Q<ScrollView>("titleList");
        titleDetails = root.Q<VisualElement>("titleDetails");
        titleNameField = root.Q<TextField>("titleNameField");
        titleDivisionField = root.Q<TextField>("titleDivisionField");
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
        segmentEditor = root.Q<VisualElement>("segmentEditor");
                matchesView = root.Q<VisualElement>("matchesView");
        matchTypeDropdown = root.Q<DropdownField>("matchTypeDropdown");
        wrestlerADropdown = root.Q<DropdownField>("wrestlerADropdown");
        wrestlerBDropdown = root.Q<DropdownField>("wrestlerBDropdown");
        wrestlerCDropdown = root.Q<DropdownField>("wrestlerCDropdown");
        wrestlerDDropdown = root.Q<DropdownField>("wrestlerDDropdown");
        isTitleMatchToggle = root.Q<Toggle>("isTitleMatchToggle");
        titleDropdown = root.Q<DropdownField>("titleDropdown");
        winnerDropdown = root.Q<DropdownField>("winnerDropdown");
        segmentTextField = root.Q<TextField>("segmentTextField");
        addMatchButton = root.Q<Button>("addMatchButton");
        addSegmentButton = root.Q<Button>("addSegmentButton");
                viewMatchesButton = root.Q<Button>("viewMatchesButton");
        saveMatchButton = root.Q<Button>("saveMatchButton");
        cancelMatchButton = root.Q<Button>("cancelMatchButton");
        saveSegmentButton = root.Q<Button>("saveSegmentButton");
        cancelSegmentButton = root.Q<Button>("cancelSegmentButton");
        // ===== Histories =====
        historyPanel = root.Q<VisualElement>("historyPanel");
        matchHistoryList = root.Q<ScrollView>("matchHistoryList");
        titleLineageList = root.Q<ScrollView>("titleLineageList");
        historyShowsPanel = root.Q<VisualElement>("historyShowsPanel");
        historyResultsPanel = root.Q<VisualElement>("historyResultsPanel");
        historyShowsList = root.Q<ScrollView>("historyShowsList");
        historyShowMatchesList = root.Q<ScrollView>("historyShowMatchesList");
        historyCloseResultsButton = root.Q<Button>("historyCloseResultsButton");
        historyResultsHeader = root.Q<Label>("historyResultsHeader");
        // ===== Rankings =====
        rankingsPanel = root.Q<VisualElement>("rankingsPanel");
        rankingsList = root.Q<ScrollView>("rankingsList");
        rankingsMenButton = root.Q<Button>("rankingsMenButton");
        rankingsWomenButton = root.Q<Button>("rankingsWomenButton");
        rankingsTagButton = root.Q<Button>("rankingsTagButton");
        RegisterMainPanel(promotionInfoPanel);
        RegisterMainPanel(wrestlersPanel);
        RegisterMainPanel(titlesPanel);
        RegisterMainPanel(showsPanel);
        RegisterMainPanel(historyPanel);
        RegisterMainPanel(rankingsPanel);
        RegisterFocusablePanel(editPanel, wrestlerDetails, wrestlerAddPanel, titleDetails, titleAddPanel,
        titleHistoryList, showDetails, showAddPanel, matchEditor, showsPanel, historyPanel,
        rankingsPanel, promotionInfoPanel, wrestlersPanel, titlesPanel);
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
        if (rankingsButton != null)
        rankingsButton.clicked += ShowRankingsPanel;
        if (rankingsMenButton != null)
        rankingsMenButton.clicked += () => PopulateRankings(RankCategory.Men);
        if (rankingsWomenButton != null)
        rankingsWomenButton.clicked += () => PopulateRankings(RankCategory.Women);
        if (rankingsTagButton != null)
        rankingsTagButton.clicked += () => PopulateRankings(RankCategory.TagTeam);
        if (historyCloseResultsButton != null)
        {
        historyCloseResultsButton.clicked += () =>
        {
        historyResultsPanel?.AddToClassList("hidden");
        historyShowsPanel?.RemoveFromClassList("hidden");
        FocusPanel(historyShowsPanel ?? historyPanel);
        };
        }
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
            showsPanel?.RemoveFromClassList("editor-full");
        showsPanel?.RemoveFromClassList("editing-show");
        showsList?.RemoveFromClassList("hidden");
        var show = new ShowData(newShowField.value, newShowDateField.value);
        currentPromotion.shows.Add(show);
        RefreshShowList();
        newShowField.value = "";
        newShowDateField.value = "";
        showsPanel?.RemoveFromClassList("editor-full");
            FocusPanel(showAddPanel ?? showsPanel);
        };
        saveShowsButton.clicked += () =>
        {
        SetActivePanel(showsPanel);
            showsPanel?.RemoveFromClassList("editor-full");
        showsPanel?.RemoveFromClassList("editing-show");
        showsList?.RemoveFromClassList("hidden");
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
            showsPanel?.RemoveFromClassList("editor-full");
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
        showsPanel?.RemoveFromClassList("editing-show");
        showsList?.RemoveFromClassList("hidden");
        RefreshShowList();
        statusLabel.text = $"✅ Show '{currentEditingShow.showName}' saved & history updated.";
        showsPanel?.RemoveFromClassList("editor-full");
            FocusPanel(showAddPanel ?? showsPanel);
        };
        cancelShowButton.clicked += () =>
        {
        SetActivePanel(showsPanel);
            showsPanel?.RemoveFromClassList("editor-full");
        showDetails.AddToClassList("hidden");
        showAddPanel.RemoveFromClassList("hidden");
        showsPanel?.RemoveFromClassList("editing-show");
        showsList?.RemoveFromClassList("hidden");
        showsPanel?.RemoveFromClassList("editor-full");
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
            showsPanel?.RemoveFromClassList("editor-full");
        showsPanel?.RemoveFromClassList("editing-show");
        showsList?.RemoveFromClassList("hidden");
        showsPanel?.RemoveFromClassList("editor-full");
            FocusPanel(showAddPanel ?? showsPanel);
        };
        addMatchButton.clicked += () =>
        {
        SetActivePanel(showsPanel);
            showsPanel?.RemoveFromClassList("editor-full");
        PopulateTitleDropdown();
        PopulateMatchTypeDropdown();
        PopulateWrestlerDropdowns();
        UpdateWinnerChoices();
        matchEditor.RemoveFromClassList("hidden");
        segmentEditor?.AddToClassList("hidden");
        matchesView?.AddToClassList("hidden");
        showsPanel?.AddToClassList("editor-full");
            FocusPanel(matchEditor);
        };
        if (addSegmentButton != null)
        {
        addSegmentButton.clicked += () =>
        {
        SetActivePanel(showsPanel);
            showsPanel?.RemoveFromClassList("editor-full");
        segmentEditor?.RemoveFromClassList("hidden");
        matchEditor?.AddToClassList("hidden");
        matchesView?.AddToClassList("hidden");
        showsPanel?.AddToClassList("editor-full");
                showsPanel?.AddToClassList("editor-full");
                FocusPanel(segmentEditor);
        };
        }
        if (viewMatchesButton != null)
        {
        viewMatchesButton.clicked += () =>
        {
        SetActivePanel(showsPanel);
            showsPanel?.RemoveFromClassList("editor-full");
        matchesView?.RemoveFromClassList("hidden");
        matchEditor?.AddToClassList("hidden");
        segmentEditor?.AddToClassList("hidden");
        RefreshMatchList();
        FocusPanel(matchesView ?? showsPanel);
        };
        }
        if (isTitleMatchToggle != null)
        {
        isTitleMatchToggle.RegisterValueChangedCallback(_ =>
        {
        if (titleDropdown != null)
        {
        titleDropdown.SetEnabled(isTitleMatchToggle.value);
        if (isTitleMatchToggle.value)
        {
        if (string.IsNullOrEmpty(titleDropdown.value))
        PopulateTitleDropdown();
        PopulateMatchTypeDropdown();
        }
        }
        });
        }
        saveMatchButton.clicked += () =>
        {
        SetActivePanel(showsPanel);
            showsPanel?.RemoveFromClassList("editor-full");
        // Validate participants (need at least two unique names)
        var rawNames = new List<string>
        {
        wrestlerADropdown?.value?.Trim(),
        wrestlerBDropdown?.value?.Trim(),
        wrestlerCDropdown != null ? wrestlerCDropdown.value?.Trim() : null,
        wrestlerDDropdown != null ? wrestlerDDropdown.value?.Trim() : null
        };
        var participants = new List<string>();
        var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        bool hasDuplicate = false;
        foreach (var n in rawNames)
        {
        if (string.IsNullOrWhiteSpace(n))
        continue;
        if (!seen.Add(n))
        hasDuplicate = true;
        else
        participants.Add(n);
        }
        if (participants.Count < 2)
        {
        statusLabel.text = "Enter at least two unique participants.";
        showsPanel?.AddToClassList("editor-full");
            FocusPanel(matchEditor);
        return;
        }
        if (hasDuplicate)
        {
        statusLabel.text = "Duplicate names detected. Participants must be unique.";
        showsPanel?.AddToClassList("editor-full");
            FocusPanel(matchEditor);
        return;
        }
        // Validate winner (if provided) must be one of the participants
        string winnerInput = winnerDropdown != null ? winnerDropdown.value?.Trim() : string.Empty;
        if (!string.IsNullOrEmpty(winnerInput))
        {
        bool winnerValid = false;
        foreach (var p in participants)
        {
        if (string.Equals(p, winnerInput, System.StringComparison.OrdinalIgnoreCase))
        {
        winnerValid = true;
        break;
        }
        }
        if (!winnerValid)
        {
        statusLabel.text = "Winner must be one of the participants.";
        showsPanel?.AddToClassList("editor-full");
            FocusPanel(matchEditor);
        return;
        }
        }
        // Write validated names back to fields so MatchData uses them
        if (wrestlerADropdown != null) wrestlerADropdown.value = participants[0];
        if (wrestlerBDropdown != null) wrestlerBDropdown.value = participants[1];
        if (wrestlerCDropdown != null) wrestlerCDropdown.value = participants.Count > 2 ? participants[2] : "";
        if (wrestlerDDropdown != null) wrestlerDDropdown.value = participants.Count > 3 ? participants[3] : "";
        var match = new MatchData
        {
        matchName = (matchTypeDropdown != null ? matchTypeDropdown.value : ""),
        wrestlerA = participants.Count > 0 ? participants[0] : "",
        wrestlerB = participants.Count > 1 ? participants[1] : "",
        wrestlerC = participants.Count > 2 ? participants[2] : "",
        wrestlerD = participants.Count > 3 ? participants[3] : "",
        isTitleMatch = isTitleMatchToggle.value,
        titleName = (titleDropdown != null ? titleDropdown.value : ""),
        winner = winnerInput
        };
        if (currentEditingShow == null)
        {
        statusLabel.text = "⚠️ No active show selected.";
        return;
        }
            currentEditingShow.matches.Add(match);
            try
            {
                if (currentEditingShow.entryOrder == null)
                    currentEditingShow.entryOrder = new System.Collections.Generic.List<string>();
                currentEditingShow.entryOrder.Add($"M:{currentEditingShow.matches.Count - 1}");
            }
            catch { }
            // Persist immediately so existing items are never overwritten by subsequent saves
            DataManager.SavePromotion(currentPromotion);
        matchEditor.AddToClassList("hidden");
        RefreshMatchList();
        statusLabel.text = $"✅ Match '{match.matchName}' added.";
        ResetDropdown(matchTypeDropdown);
        ResetDropdown(wrestlerADropdown);
        ResetDropdown(wrestlerBDropdown);
        ResetDropdown(wrestlerCDropdown);
        ResetDropdown(wrestlerDDropdown);
        isTitleMatchToggle.value = false;
        if (titleDropdown != null)
        {
        // reset to first option (empty or first title)
        if (titleDropdown.choices != null && titleDropdown.choices.Count > 0)
        titleDropdown.value = titleDropdown.choices[0];
        }
        if (winnerDropdown != null) { if (winnerDropdown.choices != null && winnerDropdown.choices.Count > 0) winnerDropdown.value = winnerDropdown.choices[0]; }
        showsPanel?.RemoveFromClassList("editor-full");
            FocusPanel(showDetails ?? showsPanel);
        };
        cancelMatchButton.clicked += () =>
        {
        SetActivePanel(showsPanel);
            showsPanel?.RemoveFromClassList("editor-full");
        matchEditor.AddToClassList("hidden");
        showsPanel?.RemoveFromClassList("editor-full");
            FocusPanel(showDetails ?? showsPanel);
        };
        if (saveSegmentButton != null)
        {
        saveSegmentButton.clicked += () =>
        {
        SetActivePanel(showsPanel);
            showsPanel?.RemoveFromClassList("editor-full");
        if (currentEditingShow == null)
        {
        statusLabel.text = "No active show selected.";
        return;
        }
        string text = segmentTextField != null ? segmentTextField.value?.Trim() : string.Empty;
        if (string.IsNullOrEmpty(text))
        {
        statusLabel.text = "Enter segment text before saving.";
        showsPanel?.AddToClassList("editor-full");
                showsPanel?.AddToClassList("editor-full");
                FocusPanel(segmentEditor);
        return;
        }
        if (currentEditingShow.segments == null)
        currentEditingShow.segments = new System.Collections.Generic.List<SegmentData>();
                currentEditingShow.segments.Add(new SegmentData { text = text });
                try
                {
                    if (currentEditingShow.entryOrder == null)
                        currentEditingShow.entryOrder = new System.Collections.Generic.List<string>();
                    currentEditingShow.entryOrder.Add($"S:{currentEditingShow.segments.Count - 1}");
                }
                catch { }
        DataManager.SavePromotion(currentPromotion);
        segmentEditor?.AddToClassList("hidden");
        matchesView?.AddToClassList("hidden");
        segmentTextField.value = string.Empty;
        statusLabel.text = "Segment added to show.";
        showsPanel?.RemoveFromClassList("editor-full");
            FocusPanel(showDetails ?? showsPanel);
        };
        }
        if (cancelSegmentButton != null)
        {
        cancelSegmentButton.clicked += () =>
        {
        SetActivePanel(showsPanel);
            showsPanel?.RemoveFromClassList("editor-full");
        segmentEditor?.AddToClassList("hidden");
        matchesView?.AddToClassList("hidden");
        showsPanel?.RemoveFromClassList("editor-full");
            FocusPanel(showDetails ?? showsPanel);
        };
        }
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
            showsPanel?.RemoveFromClassList("editor-full");
        matchEditor?.AddToClassList("hidden");
        matchesView?.AddToClassList("hidden");
        // Ensure list is visible when not editing a specific show
        showsPanel?.RemoveFromClassList("editing-show");
        showsList?.RemoveFromClassList("hidden");
        if (currentEditingShow == null)
        {
        showDetails?.AddToClassList("hidden");
        showAddPanel?.RemoveFromClassList("hidden");
        showsPanel?.RemoveFromClassList("editor-full");
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
        if (historyShowsPanel != null)
        historyShowsPanel.RemoveFromClassList("hidden");
        if (historyResultsPanel != null)
        historyResultsPanel.AddToClassList("hidden");
        // Hide aggregated sections while using show browser
        matchHistoryList?.AddToClassList("hidden");
        titleLineageList?.AddToClassList("hidden");
        PopulateHistoryShowsList();
        FocusPanel(historyShowsPanel ?? historyPanel);
        }
        // ---------------- Rankings ----------------
        private enum RankCategory { Men, Women, TagTeam }
        private void ShowRankingsPanel()
        {
        ExitWrestlerEditMode();
        SetActivePanel(rankingsPanel);
        PopulateRankings(RankCategory.Men);
        FocusPanel(rankingsPanel);
        }
        private void PopulateRankings(RankCategory category)
        {
        if (rankingsList == null || currentPromotion == null)
        return;
        rankingsList.Clear();
        var records = new Dictionary<string, (int wins, int losses)>(System.StringComparer.OrdinalIgnoreCase);
        // Build a quick lookup for wrestler flags by name
        var flagByName = new Dictionary<string, (bool isFemale, bool isTagTeam)>(System.StringComparer.OrdinalIgnoreCase);
        if (wrestlerCollection != null && wrestlerCollection.wrestlers != null)
        {
        foreach (var w in wrestlerCollection.wrestlers)
        {
        if (!string.IsNullOrEmpty(w.name))
        flagByName[w.name] = (w.isFemale, w.isTagTeam);
        }
        }
        // Iterate all shows and matches to compile win/loss
        if (currentPromotion.shows != null)
        {
        foreach (var show in currentPromotion.shows)
        {
        if (show?.matches == null) continue;
        foreach (var m in show.matches)
        {
        var participants = new List<string>();
        if (!string.IsNullOrWhiteSpace(m.wrestlerA)) participants.Add(m.wrestlerA.Trim());
        if (!string.IsNullOrWhiteSpace(m.wrestlerB)) participants.Add(m.wrestlerB.Trim());
        if (!string.IsNullOrWhiteSpace(m.wrestlerC)) participants.Add(m.wrestlerC.Trim());
        if (!string.IsNullOrWhiteSpace(m.wrestlerD)) participants.Add(m.wrestlerD.Trim());
        if (participants.Count < 2)
        continue;
        string winner = string.IsNullOrWhiteSpace(m.winner) ? null : m.winner.Trim();
        foreach (var p in participants)
        {
        // Check flags; if unknown, treat as non-matching so we skip
        flagByName.TryGetValue(p, out var flags);
        bool include = category switch
        {
        RankCategory.Men => !flags.isFemale && !flags.isTagTeam,
        RankCategory.Women => flags.isFemale && !flags.isTagTeam,
        RankCategory.TagTeam => flags.isTagTeam,
        _ => false
        };
        if (!include) continue;
        if (!records.ContainsKey(p)) records[p] = (0, 0);
        var r = records[p];
        if (!string.IsNullOrEmpty(winner) && string.Equals(p, winner, System.StringComparison.OrdinalIgnoreCase))
        r.wins++;
        else if (!string.IsNullOrEmpty(winner))
        r.losses++;
        records[p] = r;
        }
        }
        }
        }
        // Sort: wins desc, losses asc, then name
        foreach (var entry in records.OrderByDescending(e => e.Value.wins).ThenBy(e => e.Value.losses).ThenBy(e => e.Key))
        {
        int total = entry.Value.wins + entry.Value.losses;
        string pct = total > 0 ? ((float)entry.Value.wins / total).ToString("P0") : "0%";
        rankingsList.Add(new Label($"{entry.Key} — {entry.Value.wins}-{entry.Value.losses} ({pct})"));
        }
        if (records.Count == 0)
        {
        rankingsList.Add(new Label("No results yet for this category.") { style = { color = Color.gray } });
        }
        }
        private void PopulateHistoryShowsList()
        {
        if (historyShowsList == null || currentPromotion == null)
        return;
        historyShowsList.Clear();
        if (currentPromotion.shows == null || currentPromotion.shows.Count == 0)
        {
        historyShowsList.Add(new Label("No shows recorded yet.") { style = { color = Color.gray } });
        return;
        }
        foreach (var show in currentPromotion.shows)
        {
        var s = show; // capture
        Button btn = new(() => ShowSelectedShowHistory(s)) { text = $"{s.showName} - {s.date}" };
        historyShowsList.Add(btn);
        }
        }
        private void ShowSelectedShowHistory(ShowData show)
        {
        if (historyResultsPanel == null || historyShowMatchesList == null)
        return;
        historyShowsPanel?.AddToClassList("hidden");
        historyResultsPanel.RemoveFromClassList("hidden");
        if (historyResultsHeader != null)
        {
        string date = string.IsNullOrEmpty(show?.date) ? "" : $" - {show.date}";
        historyResultsHeader.text = $"Results: {show?.showName}{date}";
        }
        historyShowMatchesList.Clear();
        bool anyEntries = false;
        if (show != null && show.entryOrder != null && show.entryOrder.Count > 0)
        {
            foreach (var token in show.entryOrder)
            {
                if (string.IsNullOrEmpty(token) || token.Length < 3 || token[1] != ':')
                    continue;
                char kind = token[0];
                if (!int.TryParse(token.Substring(2), out int idx))
                    continue;
                if (kind == 'M')
                {
                    if (show.matches == null || idx < 0 || idx >= show.matches.Count) continue;
                    var match = show.matches[idx];
                    var entry = new VisualElement();
                    entry.style.marginBottom = 6;
                    var parts = new List<string>();
                    if (!string.IsNullOrEmpty(match.wrestlerA)) parts.Add(match.wrestlerA);
                    if (!string.IsNullOrEmpty(match.wrestlerB)) parts.Add(match.wrestlerB);
                    if (!string.IsNullOrEmpty(match.wrestlerC)) parts.Add(match.wrestlerC);
                    if (!string.IsNullOrEmpty(match.wrestlerD)) parts.Add(match.wrestlerD);
                    string vsLine = parts.Count > 0 ? string.Join(" vs ", parts) : "";
                    entry.Add(new Label(match.matchName));
                    if (!string.IsNullOrEmpty(vsLine)) entry.Add(new Label(vsLine));
                    if (!string.IsNullOrEmpty(match.winner)) entry.Add(new Label($"Winner: {match.winner}"));
                    if (match.isTitleMatch && !string.IsNullOrEmpty(match.titleName)) entry.Add(new Label($"Title: {match.titleName}"));
                    historyShowMatchesList.Add(entry);
                    anyEntries = true;
                }
                else if (kind == 'S')
                {
                    if (show.segments == null || idx < 0 || idx >= show.segments.Count) continue;
                    var segment = show.segments[idx];
                    var entry = new VisualElement();
                    entry.style.marginBottom = 6;
                    entry.Add(new Label("Segment:"));
                    entry.Add(new Label(segment.text));
                    historyShowMatchesList.Add(entry);
                    anyEntries = true;
                }
            }
        }
        else
        {
            if (show != null && show.matches != null && show.matches.Count > 0)
            {
                foreach (var match in show.matches)
                {
                    var entry = new VisualElement();
                    entry.style.marginBottom = 6;
                    var parts = new List<string>();
                    if (!string.IsNullOrEmpty(match.wrestlerA)) parts.Add(match.wrestlerA);
                    if (!string.IsNullOrEmpty(match.wrestlerB)) parts.Add(match.wrestlerB);
                    if (!string.IsNullOrEmpty(match.wrestlerC)) parts.Add(match.wrestlerC);
                    if (!string.IsNullOrEmpty(match.wrestlerD)) parts.Add(match.wrestlerD);
                    string vsLine = parts.Count > 0 ? string.Join(" vs ", parts) : "";
                    entry.Add(new Label(match.matchName));
                    if (!string.IsNullOrEmpty(vsLine)) entry.Add(new Label(vsLine));
                    if (!string.IsNullOrEmpty(match.winner)) entry.Add(new Label($"Winner: {match.winner}"));
                    if (match.isTitleMatch && !string.IsNullOrEmpty(match.titleName)) entry.Add(new Label($"Title: {match.titleName}"));
                    historyShowMatchesList.Add(entry);
                    anyEntries = true;
                }
            }
            if (show != null && show.segments != null && show.segments.Count > 0)
            {
                foreach (var segment in show.segments)
                {
                    var entry = new VisualElement();
                    entry.style.marginBottom = 6;
                    entry.Add(new Label("Segment:"));
                    entry.Add(new Label(segment.text));
                    historyShowMatchesList.Add(entry);
                    anyEntries = true;
                }
            }
        }
        if (!anyEntries)
        {
        historyShowMatchesList.Add(new Label("No results for this show yet.") { style = { color = Color.gray } });
        }
        FocusPanel(historyResultsPanel);
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
        EnsureWrestlerListView();
        if (wrestlerCollection == null || wrestlerCollection.wrestlers == null)
        {
            if (wrestlerListView != null)
            {
                wrestlerListView.itemsSource = System.Array.Empty<WrestlerData>();
                wrestlerListView.Rebuild();
            }
            return;
        }
        wrestlerListView.itemsSource = wrestlerCollection.wrestlers;
        wrestlerListView.Rebuild();
    }

    private void EnsureWrestlerListView()
    {
        if (wrestlerListView != null)
            return;

        var parent = wrestlerList != null ? wrestlerList.parent : wrestlersPanel;
        wrestlerListView = new ListView
        {
            name = "wrestlerListView",
            selectionType = SelectionType.None
        };
        wrestlerListView.style.flexGrow = 1;
        wrestlerListView.makeItem = () =>
        {
            var b = new Button();
            b.RegisterCallback<ClickEvent>(_ =>
            {
                if (b.userData is int idx)
                    SelectWrestler(idx);
            });
            return b;
        };
        wrestlerListView.bindItem = (ve, i) =>
        {
            var b = (Button)ve;
            if (wrestlerCollection != null && wrestlerCollection.wrestlers != null && i >= 0 && i < wrestlerCollection.wrestlers.Count)
            {
                b.text = $"• {wrestlerCollection.wrestlers[i].name}";
                b.userData = i;
            }
            else
            {
                b.text = string.Empty;
                b.userData = -1;
            }
        };

        parent?.Add(wrestlerListView);
        if (wrestlerList != null)
            wrestlerList.style.display = DisplayStyle.None;
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
        wrestlerCollection.wrestlers.Add(new WrestlerData { name = name, isFemale = (newWrestlerIsFemaleToggle != null && newWrestlerIsFemaleToggle.value), isTagTeam = (newWrestlerIsTagTeamToggle != null && newWrestlerIsTagTeamToggle.value) });
        newWrestlerField.value = "";
        if (newWrestlerIsFemaleToggle != null) newWrestlerIsFemaleToggle.value = false;
        if (newWrestlerIsTagTeamToggle != null) newWrestlerIsTagTeamToggle.value = false;
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
        if (wrestlerIsTagTeamToggle != null)
        wrestlerIsTagTeamToggle.value = w.isTagTeam;
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
        if (selectedIndex < 0 || wrestlerCollection == null) return; if (selectedIndex >= (wrestlerCollection?.wrestlers?.Count ?? 0)) return;
        var w = wrestlerCollection.wrestlers[selectedIndex];
        w.name = (wrestlerNameField?.value ?? string.Empty).Trim();
        if (wrestlerIsTagTeamToggle != null)
        w.isTagTeam = wrestlerIsTagTeamToggle.value;
        w.hometown = (wrestlerHometownField?.value ?? string.Empty).Trim();
        w.isFemale = wrestlerIsFemaleToggle != null && wrestlerIsFemaleToggle.value;
        if (wrestlerHeightField != null) w.height = wrestlerHeightField.value;
        if (wrestlerWeightField != null) w.weight = wrestlerWeightField.value;
        DataManager.SaveWrestlers(wrestlerCollection);
        RefreshWrestlerList();
        HideWrestlerDetails();
        statusLabel.text = "💾 Wrestler saved!";
        }
        private void DeleteSelectedWrestler()
        {
        if (selectedIndex < 0 || wrestlerCollection == null) return; if (selectedIndex >= (wrestlerCollection?.wrestlers?.Count ?? 0)) return;
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
        PopulateTitleDropdown();
        PopulateMatchTypeDropdown();
        PopulateWrestlerDropdowns();
        return;
        }
        for (int i = 0; i < titleCollection.titles.Count; i++)
        {
        int index = i;
        var t = titleCollection.titles[i];
        Button btn = new(() => SelectTitle(index)) { text = $"• {t.titleName}" };
        titleList.Add(btn);
        }
        PopulateTitleDropdown();
        PopulateMatchTypeDropdown();
        PopulateWrestlerDropdowns();
        }
        private void PopulateWrestlerDropdowns()
        {
        var names = new List<string>();
        names.Add("");
        if (wrestlerCollection != null && wrestlerCollection.wrestlers != null)
        {
        foreach (var w in wrestlerCollection.wrestlers)
        if (!string.IsNullOrEmpty(w.name)) names.Add(w.name);
        }
        void ApplyChoices(DropdownField dd)
        {
        if (dd == null) return;
        if (dd.choices == null || dd.choices.Count != names.Count || !dd.choices.SequenceEqual(names)) dd.choices = new List<string>(names);
        if (string.IsNullOrEmpty(dd.value) || !dd.choices.Contains(dd.value))
        dd.SetValueWithoutNotify(dd.choices.Count > 0 ? dd.choices[0] : "");
        }
        ApplyChoices(wrestlerADropdown);
        ApplyChoices(wrestlerBDropdown);
        ApplyChoices(wrestlerCDropdown);
        ApplyChoices(wrestlerDDropdown);
        UpdateWinnerChoices();
        }
        private void UpdateWinnerChoices()
        {
        if (winnerDropdown == null)
        return;
        var choices = new List<string>();
        choices.Add("");
        void AddIfNotEmpty(string s)
        {
        if (!string.IsNullOrWhiteSpace(s) && !choices.Contains(s)) choices.Add(s);
        }
        AddIfNotEmpty(wrestlerADropdown != null ? wrestlerADropdown.value : null);
        AddIfNotEmpty(wrestlerBDropdown != null ? wrestlerBDropdown.value : null);
        AddIfNotEmpty(wrestlerCDropdown != null ? wrestlerCDropdown.value : null);
        AddIfNotEmpty(wrestlerDDropdown != null ? wrestlerDDropdown.value : null);
        if (winnerDropdown.choices == null || winnerDropdown.choices.Count != choices.Count || !winnerDropdown.choices.SequenceEqual(choices)) winnerDropdown.choices = choices; if (string.IsNullOrEmpty(winnerDropdown.value) || !choices.Contains(winnerDropdown.value)) winnerDropdown.SetValueWithoutNotify(choices[0]);
        // Wire change handlers
        void EnsureHandler(DropdownField dd)
        {
        if (dd == null) return;
        dd.RegisterValueChangedCallback(_ => UpdateWinnerChoices());
        }
        if (!winnerHandlersHooked) { EnsureHandler(wrestlerADropdown); EnsureHandler(wrestlerBDropdown); EnsureHandler(wrestlerCDropdown); EnsureHandler(wrestlerDDropdown); winnerHandlersHooked = true; }
        }
        private void ResetDropdown(DropdownField dd)
        {
        if (dd == null) return;
        if (dd.choices != null && dd.choices.Count > 0) dd.SetValueWithoutNotify(dd.choices[0]); else dd.SetValueWithoutNotify("");
        }
        private void PopulateMatchTypeDropdown()
        {
        if (matchTypeDropdown == null)
        return;
        var types = new List<string>
        {
        "Singles Match",
        "Tag Team Match",
        "Triple Threat",
        "Fatal 4-Way",
        "Steel Cage",
        "Hell in a Cell",
        "Last Man Standing",
        "Hardcore Match",
        "Extreme Rules Match",
        "Iron Man Match",
        "First Blood Match"
        };
        matchTypeDropdown.choices = types;
        if (string.IsNullOrEmpty(matchTypeDropdown.value) || !types.Contains(matchTypeDropdown.value))
        matchTypeDropdown.value = types[0];
        }private void PopulateTitleDropdown()
        {
        if (titleDropdown == null)
        return;
        var choices = new List<string>();
        // Add empty option for "no title"
        choices.Add("");
        if (titleCollection != null && titleCollection.titles != null)
        {
        foreach (var t in titleCollection.titles)
        if (!string.IsNullOrEmpty(t.titleName)) choices.Add(t.titleName);
        }
        titleDropdown.choices = choices;
        // Determine a good default: prefer a title that currently has a champion
        string championTitle = null;
        if (titleCollection != null && titleCollection.titles != null)
        {
        foreach (var t in titleCollection.titles)
        {
        if (!string.IsNullOrEmpty(t.titleName) && !string.IsNullOrEmpty(t.currentChampion))
        {
        championTitle = t.titleName;
        break;
        }
        }
        }
        // Preserve current selection if possible, else use championTitle, else first
        if (string.IsNullOrEmpty(titleDropdown.value) || !choices.Contains(titleDropdown.value))
        {
        if (!string.IsNullOrEmpty(championTitle) && choices.Contains(championTitle))
        titleDropdown.value = championTitle;
        else
        titleDropdown.value = choices.Count > 0 ? choices[0] : "";
        }
        // Sync enable state with toggle
        if (isTitleMatchToggle != null)
        titleDropdown.SetEnabled(isTitleMatchToggle.value);
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
            showsPanel?.RemoveFromClassList("editor-full");
        showsPanel?.AddToClassList("editing-show");
        showsList?.AddToClassList("hidden");
        showAddPanel.AddToClassList("hidden");
        showDetails.RemoveFromClassList("hidden");
        matchesView?.AddToClassList("hidden");
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

        bool any = false;
        if (currentEditingShow.entryOrder != null && currentEditingShow.entryOrder.Count > 0)
        {
            foreach (var token in currentEditingShow.entryOrder)
            {
                if (string.IsNullOrEmpty(token) || token.Length < 3 || token[1] != ':')
                    continue;
                char kind = token[0];
                if (!int.TryParse(token.Substring(2), out int idx))
                    continue;
                if (kind == 'M')
                {
                    if (currentEditingShow.matches == null || idx < 0 || idx >= currentEditingShow.matches.Count) continue;
                    var m = currentEditingShow.matches[idx];
                    var parts = new List<string>();
                    if (!string.IsNullOrEmpty(m.wrestlerA)) parts.Add(m.wrestlerA);
                    if (!string.IsNullOrEmpty(m.wrestlerB)) parts.Add(m.wrestlerB);
                    if (!string.IsNullOrEmpty(m.wrestlerC)) parts.Add(m.wrestlerC);
                    if (!string.IsNullOrEmpty(m.wrestlerD)) parts.Add(m.wrestlerD);
                    string vsLine = parts.Count > 0 ? string.Join(" vs ", parts) : "";
                    matchesList.Add(new Label(string.IsNullOrEmpty(vsLine) ? m.matchName : $"{m.matchName} - {vsLine}"));
                    any = true;
                }
                else if (kind == 'S')
                {
                    if (currentEditingShow.segments == null || idx < 0 || idx >= currentEditingShow.segments.Count) continue;
                    var seg = currentEditingShow.segments[idx];
                    var segText = string.IsNullOrEmpty(seg?.text) ? "(Empty segment)" : seg.text;
                    matchesList.Add(new Label($"Segment: {segText}"));
                    any = true;
                }
            }
        }
        if (!any)
        {
            // Fallback order if no entryOrder exists
            if (currentEditingShow.matches != null)
            {
                foreach (var m in currentEditingShow.matches)
                {
                    var parts = new List<string>();
                    if (!string.IsNullOrEmpty(m.wrestlerA)) parts.Add(m.wrestlerA);
                    if (!string.IsNullOrEmpty(m.wrestlerB)) parts.Add(m.wrestlerB);
                    if (!string.IsNullOrEmpty(m.wrestlerC)) parts.Add(m.wrestlerC);
                    if (!string.IsNullOrEmpty(m.wrestlerD)) parts.Add(m.wrestlerD);
                    string vsLine = parts.Count > 0 ? string.Join(" vs ", parts) : "";
                    matchesList.Add(new Label(string.IsNullOrEmpty(vsLine) ? m.matchName : $"{m.matchName} - {vsLine}"));
                }
            }
            if (currentEditingShow.segments != null)
            {
                foreach (var seg in currentEditingShow.segments)
                {
                    var segText = string.IsNullOrEmpty(seg?.text) ? "(Empty segment)" : seg.text;
                    matchesList.Add(new Label($"Segment: {segText}"));
                }
            }
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
        
