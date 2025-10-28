using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class PromotionDashboard : MonoBehaviour
{
    // ===== Promotion Info =====
    private PromotionData currentPromotion;
    private VisualElement promotionInfoPanel;
    private Label nameLabel, locationLabel, foundedLabel, descriptionLabel, statusLabel;
    private TextField nameField, locationField, foundedField, descriptionField;
    private Button editPromotionButton, savePromotionButton, cancelPromotionButton;

    // ===== Navigation Buttons =====
    private Button promotionButton, wrestlersButton, titlesButton, returnButton;

    // ===== Wrestlers =====
    private VisualElement wrestlersPanel;
    private ScrollView wrestlerList;
    private VisualElement wrestlerDetails;
    private TextField wrestlerNameField, wrestlerNicknameField, wrestlerHometownField, wrestlerDebutField;
    private FloatField wrestlerHeightField, wrestlerWeightField;
    private Button addWrestlerButton, saveWrestlersButton, saveWrestlerButton, deleteWrestlerButton, cancelEditButton;
    private TextField newWrestlerField;
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
    private TextField newTitleField;
    private TitleCollection titleCollection;
    private int selectedTitleIndex = -1;

    // Shows tab
    // ---- Shows ----
    private VisualElement showsPanel, showDetails, showAddPanel;
    private ScrollView showsList, matchesList;
    private TextField showNameField, showDateField, newShowField, newShowDateField;
    private Button addShowButton, saveShowsButton, saveShowButton, deleteShowButton, cancelShowButton;
    private Button addMatchButton, saveMatchButton, cancelMatchButton;
    private VisualElement matchEditor;
    private TextField matchNameField, wrestlerAField, wrestlerBField, titleInvolvedField, winnerField;
    private Toggle isTitleMatchToggle;

    // runtime variables
    private ShowData currentEditingShow;


private void OnEnable()
{
    // ✅ Get promotion from persistent session
    if (PromotionSession.Instance == null || PromotionSession.Instance.CurrentPromotion == null)
    {
        Debug.LogError("❌ No promotion loaded in session. Returning to Main Menu.");
        SceneLoader.Instance.LoadScene("MainMenu");
        return;
    }

    currentPromotion = PromotionSession.Instance.CurrentPromotion;
    Debug.Log($"✅ PromotionDashboard opened for: {currentPromotion.promotionName}");



        var root = GetComponent<UIDocument>().rootVisualElement;

        // Navigation
        promotionButton = root.Q<Button>("promotionButton");
        wrestlersButton = root.Q<Button>("wrestlersButton");
        titlesButton = root.Q<Button>("titlesButton");
// Example: Hooking up the "Return to Main Menu" button
Button returnButton = root.Q<Button>("returnButton");
if (returnButton != null)
{
    returnButton.clicked += () =>
    {
        Debug.Log("Returning to Main Menu...");

        // ✅ Clear active promotion data
        if (PromotionSession.Instance != null)
            PromotionSession.Instance.CurrentPromotion = null;

        // ✅ Load Main Menu scene
        SceneLoader.Instance.LoadScene("MainMenu");
    };
}

        statusLabel = root.Q<Label>("statusLabel");

        // Promotion info
        promotionInfoPanel = root.Q<VisualElement>("promotionInfoPanel");
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

        // Wrestlers
        wrestlersPanel = root.Q<VisualElement>("wrestlersPanel");
        wrestlerList = root.Q<ScrollView>("wrestlerList");
        wrestlerDetails = root.Q<VisualElement>("wrestlerDetails");
        wrestlerNameField = root.Q<TextField>("wrestlerNameField");
        wrestlerNicknameField = root.Q<TextField>("wrestlerNicknameField");
        wrestlerHometownField = root.Q<TextField>("wrestlerHometownField");
        wrestlerDebutField = root.Q<TextField>("wrestlerDebutField");
        wrestlerHeightField = root.Q<FloatField>("wrestlerHeightField");
        wrestlerWeightField = root.Q<FloatField>("wrestlerWeightField");
        addWrestlerButton = root.Q<Button>("addWrestlerButton");
        saveWrestlersButton = root.Q<Button>("saveWrestlersButton");
        saveWrestlerButton = root.Q<Button>("saveWrestlerButton");
        deleteWrestlerButton = root.Q<Button>("deleteWrestlerButton");
        cancelEditButton = root.Q<Button>("cancelEditButton");
        newWrestlerField = root.Q<TextField>("newWrestlerField");
        wrestlerAddPanel = root.Q<VisualElement>("wrestlerAddPanel");

        // Titles
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

        // Navigation wiring
        promotionButton.clicked += ShowPromotionPanel;
        wrestlersButton.clicked += ShowWrestlersPanel;
        titlesButton.clicked += ShowTitlesPanel;
        returnButton.clicked += () => SceneLoader.Instance.LoadScene("MainMenu");

        // Promotion actions
        editPromotionButton.clicked += () => SetEditMode(true);
        savePromotionButton.clicked += SavePromotionChanges;
        cancelPromotionButton.clicked += () => SetEditMode(false);

        // Wrestler actions
        addWrestlerButton.clicked += AddWrestler;
        saveWrestlersButton.clicked += SaveWrestlers;
        saveWrestlerButton.clicked += SaveSelectedWrestler;
        deleteWrestlerButton.clicked += DeleteSelectedWrestler;
        cancelEditButton.clicked += HideWrestlerDetails;

        // Title actions
        addTitleButton.clicked += AddTitle;
        saveTitlesButton.clicked += SaveTitles;
        saveTitleButton.clicked += SaveSelectedTitle;
        deleteTitleButton.clicked += DeleteSelectedTitle;
        cancelTitleButton.clicked += HideTitleDetails;


        addShowButton.clicked += () =>
        {
            var show = new ShowData(newShowField.value, newShowDateField.value);
            currentPromotion.shows.Add(show);
            RefreshShowList();
            newShowField.value = "";
            newShowDateField.value = "";
        };

        saveShowsButton.clicked += () =>
        {
            DataManager.SavePromotion(currentPromotion);

        };

        saveShowButton.clicked += () =>
        {
            currentEditingShow.showName = showNameField.value;
            currentEditingShow.date = showDateField.value;
            showDetails.AddToClassList("hidden");
            showAddPanel.RemoveFromClassList("hidden");
            RefreshShowList();
        };

        cancelShowButton.clicked += () =>
        {
            showDetails.AddToClassList("hidden");
            showAddPanel.RemoveFromClassList("hidden");
        };

        deleteShowButton.clicked += () =>
        {
            currentPromotion.shows.Remove(currentEditingShow);
            currentEditingShow = null;
            RefreshShowList();
            showDetails.AddToClassList("hidden");
            showAddPanel.RemoveFromClassList("hidden");
        };

        addMatchButton.clicked += () =>
        {
            matchEditor.RemoveFromClassList("hidden");
        };

        saveMatchButton.clicked += () =>
        {
            var match = new MatchData(matchNameField.value, wrestlerAField.value, wrestlerBField.value)
            {
                isTitleMatch = isTitleMatchToggle.value,
                titleInvolved = titleInvolvedField.value,
                winner = winnerField.value
            };

            currentEditingShow.matches.Add(match);
            matchEditor.AddToClassList("hidden");
            RefreshMatchList();
        };

        cancelMatchButton.clicked += () =>
        {
            matchEditor.AddToClassList("hidden");
        };

        LoadPromotionData();
    }

private void LoadPromotionData()
{
    // ✅ Use PromotionSession instead of PromotionManager
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

    // Load associated wrestler and title data
    wrestlerCollection = DataManager.LoadWrestlers(currentPromotion.promotionName);
    titleCollection = DataManager.LoadTitles(currentPromotion.promotionName);

    RefreshWrestlerList();
    RefreshTitleList();
}


    // ---------------- Promotion Editing ----------------
    private void SetEditMode(bool enable)
    {
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
            editPromotionButton.AddToClassList("hidden");
        }
        else
        {
            nameLabel.RemoveFromClassList("hidden");
            locationLabel.RemoveFromClassList("hidden");
            foundedLabel.RemoveFromClassList("hidden");
            descriptionLabel.RemoveFromClassList("hidden");
            editPromotionButton.RemoveFromClassList("hidden");
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

    // ---------------- Navigation Between Panels ----------------
    private void ShowPromotionPanel()
    {
        promotionInfoPanel.RemoveFromClassList("hidden");
        wrestlersPanel.AddToClassList("hidden");
        titlesPanel.AddToClassList("hidden");
    }

    private void ShowWrestlersPanel()
    {
        promotionInfoPanel.AddToClassList("hidden");
        wrestlersPanel.RemoveFromClassList("hidden");
        titlesPanel.AddToClassList("hidden");
    }

    private void ShowTitlesPanel()
    {
        promotionInfoPanel.AddToClassList("hidden");
        wrestlersPanel.AddToClassList("hidden");
        titlesPanel.RemoveFromClassList("hidden");
    }

    // ---------------- Wrestlers Logic ----------------
    private void RefreshWrestlerList()
    {
        wrestlerList.Clear();

        if (wrestlerCollection == null || wrestlerCollection.wrestlers.Count == 0)
        {
            Label empty = new Label("No wrestlers added yet.");
            empty.style.color = Color.gray;
            wrestlerList.Add(empty);
            return;
        }

        for (int i = 0; i < wrestlerCollection.wrestlers.Count; i++)
        {
            int index = i;
            var wrestler = wrestlerCollection.wrestlers[i];
            Button button = new Button(() => SelectWrestler(index))
            {
                text = $"• {wrestler.name}"
            };
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

        wrestlerCollection.wrestlers.Add(new WrestlerData { name = name });
        newWrestlerField.value = "";
        RefreshWrestlerList();
    }

    private void SelectWrestler(int index)
    {
        if (wrestlerCollection == null || index < 0 || index >= wrestlerCollection.wrestlers.Count)
            return;

        selectedIndex = index;
        var w = wrestlerCollection.wrestlers[index];

        // Hide unrelated UI
        promotionInfoPanel.AddToClassList("hidden");
        titlesPanel.AddToClassList("hidden");

        wrestlerNameField.value = w.name;
        wrestlerNicknameField.value = w.nickname;
        wrestlerHometownField.value = w.hometown;
        wrestlerDebutField.value = w.debutYear;
        wrestlerHeightField.value = w.height;
        wrestlerWeightField.value = w.weight;
        wrestlerAddPanel.AddToClassList("hidden"); // hide add section when editing
        wrestlerDetails.RemoveFromClassList("hidden");
    }

    private void HideWrestlerDetails()
    {
        wrestlerDetails.AddToClassList("hidden");
        wrestlerAddPanel.RemoveFromClassList("hidden"); // show add section again
        promotionInfoPanel.RemoveFromClassList("hidden");
        titlesPanel.RemoveFromClassList("hidden");
        selectedIndex = -1;
    }

    private void SaveSelectedWrestler()
    {
        if (selectedIndex < 0 || wrestlerCollection == null)
            return;

        var w = wrestlerCollection.wrestlers[selectedIndex];
        w.name = wrestlerNameField.value.Trim();
        w.nickname = wrestlerNicknameField.value.Trim();
        w.hometown = wrestlerHometownField.value.Trim();
        w.debutYear = wrestlerDebutField.value.Trim();
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
            Label empty = new Label("No titles created yet.");
            empty.style.color = Color.gray;
            titleList.Add(empty);
            return;
        }

        for (int i = 0; i < titleCollection.titles.Count; i++)
        {
            int index = i;
            var title = titleCollection.titles[i];
            Button btn = new Button(() => SelectTitle(index)) { text = $"• {title.titleName}" };
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
    }

    private void SelectTitle(int index)
    {
        if (titleCollection == null || index < 0 || index >= titleCollection.titles.Count)
            return;

        selectedTitleIndex = index;
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
    }

    private void HideTitleDetails()
    {
        titleDetails.AddToClassList("hidden");
        promotionInfoPanel.RemoveFromClassList("hidden");
        wrestlersPanel.RemoveFromClassList("hidden");
        titleAddPanel.RemoveFromClassList("hidden");
        selectedTitleIndex = -1;
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
        showAddPanel.AddToClassList("hidden");
        showDetails.RemoveFromClassList("hidden");

        showNameField.value = show.showName;
        showDateField.value = show.date;
        RefreshMatchList();
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


}
