using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

public class PromotionDashboard : MonoBehaviour
{
    private void SetupDropdownOverlay(DropdownField dd)
    {
        if (dd == null) return;
        // Avoid BringToFront on the dropdown or its row to prevent layout reordering.
        // If needed for inter-document stacking, bring the root panel forward.
        dd.RegisterCallback<PointerDownEvent>(_ => { root?.BringToFront(); });
        dd.RegisterCallback<FocusInEvent>(_ => { root?.BringToFront(); });
    }
    private const string DateFormat = "MM/dd/yyyy";
    // Data
    private PromotionData currentPromotion;

    // Root and navigation
    private VisualElement root;
    private Label statusLabel;
    private Button promotionButton, wrestlersButton, titlesButton, tournamentsButton, stablesButton, tagTeamsButton, showsButton, calendarButton, historyButton, rankingsButton, returnButton;

    // Panels
    private VisualElement promotionInfoPanel, wrestlersPanel, titlesPanel, tournamentsPanel, stablesPanel, tagTeamsPanel, showsPanel, calendarPanel, cardBuilderPanel, historyPanel, rankingsPanel;
    // History subpanels
    private VisualElement historyShowsPanel, historyResultsPanel;
    private Label historyResultsHeader;
    private ScrollView historyShowMatchesList;
    private TextField historyLocationFilterField;
    // Shows subpanels (for reordering)
    private VisualElement matchesView;

    // Bookkeeping
    private readonly List<VisualElement> mainPanels = new();
    private readonly List<VisualElement> focusablePanels = new();
    private Coroutine initializationRoutine;

    // Virtualized lists (Step 1)
    private ScrollView wrestlerListScroll, showsListScroll, historyShowsListScroll, rankingsListScroll;
    private ListView wrestlerListView, showsListView, historyShowsListView, rankingsListView;
    private Button rankingsMenButton, rankingsWomenButton, rankingsTagButton, rankingsStableButton;

    // Calendar & Card Builder
    private CalendarView calendarView;
    private CardBuilderView cardBuilderView;
    private WrestlerCollection wrestlerCollection;
    // Wrestler UI
    private VisualElement wrestlerDetails, wrestlerAddPanel;
    private TextField wrestlerNameField, wrestlerHometownField, newWrestlerField;
    private Toggle wrestlerIsFemaleToggle, newWrestlerIsFemaleToggle;
    private FloatField wrestlerHeightField, wrestlerWeightField;
    private Button addWrestlerButton, saveWrestlersButton, saveWrestlerButton, deleteWrestlerButton, cancelEditButton;
    private int selectedWrestlerIndex = -1;
    // Titles (Step 4)
    private ScrollView titleListScroll, titleHistoryList;
    private ListView titleListView;
    private VisualElement titleDetailsPanel, titleAddPanel;
    private Button viewHistoryButton;
    private TextField titleNameField, titleDivisionField, titleChampionField, titleNotesField;
    private TextField newTitleField;
    private Button addTitleButton, saveTitlesButton, saveTitleButton, deleteTitleButton, cancelTitleButton;
    private TitleCollection titleCollection;
    private int selectedTitleIndex = -1;
    // Tournaments UI
    private ScrollView tournamentListScroll;
    private ListView tournamentListView;
    private VisualElement tournamentAddPanel, tournamentManagePanel;
    private TextField newTournamentNameField, tournamentNameField;
    private DropdownField newTournamentTypeDropdown, tournamentTypeDropdown, tournamentEntrantDropdown;
    private ScrollView tournamentEntrantsList, tournamentMatchesList;
    private Button addTournamentButton, saveTournamentsButton, viewTournamentsButton, saveTournamentButton, deleteTournamentButton, cancelTournamentButton;
    private Button addEntrantButton, removeEntrantButton, generateBracketButton, advanceRoundButton, clearBracketButton;
    private TournamentCollection tournamentCollection;
    private int selectedTournamentIndex = -1;
    // Tag Teams UI
    private ScrollView tagTeamListScroll;
    private ListView tagTeamListView;
    private TextField teamNameField;
    private DropdownField teamMemberADropdown, teamMemberBDropdown;
    private Button addTeamButton, saveTeamsButton, saveTeamButton, deleteTeamButton, cancelTeamButton;
    private TagTeamCollection tagTeamCollection;
    private int selectedTeamIndex = -1;
    private Label tagTeamEmptyLabel;
    // Persist per-title match history toggle (by promotion + title)
    private readonly Dictionary<string, bool> titleHistoryToggleByTitle = new Dictionary<string, bool>(System.StringComparer.OrdinalIgnoreCase);
    // Title stats UI
    private VisualElement titleStatsPanel; // legacy stats block under details (hidden)
    private VisualElement titleStatsView;  // standalone stats panel opened from history
    private ScrollView titleStatsList;
    private Button titleStatsCloseButton;
    private Label titleStatsCurrentLabel, titleStatsSummaryLabel, titleStatsLongestLabel, titleStatsShortestLabel, titleStatsMostDefensesLabel;

    // Promotion info widgets
    private Label nameLabel, locationLabel, foundedLabel, descriptionLabel;
    private Button editPromotionButton, savePromotionButton, cancelPromotionButton;
    private TextField nameField, locationField, foundedField, descriptionField;
    private VisualElement editPanel;

    // Shows UI (details + editors)
    private VisualElement showDetailsPanel, showAddPanel, matchEditor, segmentEditor;
    private TextField newShowField, newShowDateField, showNameField, showDateField;
    private TextField showVenueField, showCityField, newShowVenueField, newShowCityField;
    private IntegerField showAttendanceField, newShowAttendanceField;
    private FloatField showRatingField, newShowRatingField;
    private Button addShowButton, saveShowsButton, saveShowButton, deleteShowButton, cancelShowButton, viewMatchesButton;
    private Button addMatchButton, addSegmentButton, saveMatchButton, cancelMatchButton, saveSegmentButton, cancelSegmentButton;
    private DropdownField matchTypeDropdown, wrestlerADropdown, wrestlerBDropdown, wrestlerCDropdown, wrestlerDDropdown, titleDropdown, winnerDropdown;
    private Toggle isTitleMatchToggle;
    private TextField segmentNameField, segmentTextField;
    private int selectedShowIndex = -1;

    // Date picker (for show dates)
    private VisualElement datePickerOverlay, datePickerPopup, datePickerGrid;
    private Label datePickerMonthLabel;
    private Button datePrevButton, dateNextButton;
    private DateTime datePickerMonth = DateTime.Today;
    private TextField activeDateField;
    private Button showDatePickButton, newShowDatePickButton;

    private void OnEnable()
    {
        if (initializationRoutine != null)
            StopCoroutine(initializationRoutine);
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

    private IEnumerator WaitForPromotionData()
    {
        const float timeoutSeconds = 2f;
        float startTime = Time.unscaledTime;
        while (PromotionSession.Instance == null || PromotionSession.Instance.CurrentPromotion == null)
        {
            if (Time.unscaledTime - startTime >= timeoutSeconds)
            {
                Debug.LogWarning("PromotionDashboard: No promotion loaded in session; proceeding.");
                break;
            }
            yield return null;
        }
        InitializeDashboard();
        initializationRoutine = null;
    }

    private void InitializeDashboard()
    {
        currentPromotion = PromotionSession.Instance != null ? PromotionSession.Instance.CurrentPromotion : null;
        root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null)
        {
            Debug.LogError("PromotionDashboard: rootVisualElement not found.");
            return;
        }

        // Query navigation
        promotionButton = root.Q<Button>("promotionButton");
        wrestlersButton = root.Q<Button>("wrestlersButton");
        titlesButton = root.Q<Button>("titlesButton");
        tournamentsButton = root.Q<Button>("tournamentsButton");
        stablesButton = root.Q<Button>("stablesButton");
        tagTeamsButton = root.Q<Button>("tagTeamsButton");
        showsButton = root.Q<Button>("showsButton");
        calendarButton = root.Q<Button>("calendarButton");
        historyButton = root.Q<Button>("historyButton");
        rankingsButton = root.Q<Button>("rankingsButton");
        returnButton = root.Q<Button>("returnButton");
        statusLabel = root.Q<Label>("statusLabel");

        // Query panels
        promotionInfoPanel = root.Q<VisualElement>("promotionInfoPanel");
        wrestlersPanel = root.Q<VisualElement>("wrestlersPanel");
        titlesPanel = root.Q<VisualElement>("titlesPanel");
        tournamentsPanel = root.Q<VisualElement>("tournamentsPanel");
        stablesPanel = root.Q<VisualElement>("stablesPanel");
        tagTeamsPanel = root.Q<VisualElement>("tagTeamsPanel");
        showsPanel = root.Q<VisualElement>("showsPanel");
        calendarPanel = root.Q<VisualElement>("calendarPanel");
        cardBuilderPanel = root.Q<VisualElement>("cardBuilderPanel");
        historyPanel = root.Q<VisualElement>("historyPanel");
        historyShowsPanel = root.Q<VisualElement>("historyShowsPanel");
        historyResultsPanel = root.Q<VisualElement>("historyResultsPanel");
        historyResultsHeader = root.Q<Label>("historyResultsHeader");
        historyShowMatchesList = root.Q<ScrollView>("historyShowMatchesList");
        historyLocationFilterField = root.Q<TextField>("historyLocationFilterField");
        rankingsPanel = root.Q<VisualElement>("rankingsPanel");

        // Query list ScrollViews/buttons used as anchors in UXML
        wrestlerListScroll = root.Q<ScrollView>("wrestlerList");
        // Wrestler details/add panel
        wrestlerDetails = root.Q<VisualElement>("wrestlerDetails");
        wrestlerAddPanel = root.Q<VisualElement>("wrestlerAddPanel");
        wrestlerNameField = root.Q<TextField>("wrestlerNameField");
        wrestlerHometownField = root.Q<TextField>("wrestlerHometownField");
        wrestlerIsFemaleToggle = root.Q<Toggle>("wrestlerIsFemaleToggle");
        wrestlerHeightField = root.Q<FloatField>("wrestlerHeightField");
        wrestlerWeightField = root.Q<FloatField>("wrestlerWeightField");
        newWrestlerField = root.Q<TextField>("newWrestlerField");
        newWrestlerIsFemaleToggle = root.Q<Toggle>("newWrestlerIsFemaleToggle");
        addWrestlerButton = root.Q<Button>("addWrestlerButton");
        saveWrestlersButton = root.Q<Button>("saveWrestlersButton");
        saveWrestlerButton = root.Q<Button>("saveWrestlerButton");
        deleteWrestlerButton = root.Q<Button>("deleteWrestlerButton");
        cancelEditButton = root.Q<Button>("cancelEditButton");
        titleListScroll = root.Q<ScrollView>("titleList");
        titleHistoryList = root.Q<ScrollView>("titleHistoryList");
        // Tournaments queries
        tournamentListScroll = root.Q<ScrollView>("tournamentList");
        tournamentAddPanel = root.Q<VisualElement>("tournamentAddPanel");
        tournamentManagePanel = root.Q<VisualElement>("tournamentManagePanel");
        newTournamentNameField = root.Q<TextField>("newTournamentNameField");
        newTournamentTypeDropdown = root.Q<DropdownField>("newTournamentTypeDropdown");
        SetupDropdownOverlay(newTournamentTypeDropdown);
        tournamentNameField = root.Q<TextField>("tournamentNameField");
        tournamentTypeDropdown = root.Q<DropdownField>("tournamentTypeDropdown");
        SetupDropdownOverlay(tournamentTypeDropdown);
        tournamentEntrantDropdown = root.Q<DropdownField>("tournamentEntrantDropdown");
        SetupDropdownOverlay(tournamentEntrantDropdown);
        tournamentEntrantsList = root.Q<ScrollView>("tournamentEntrantsList");
        tournamentMatchesList = root.Q<ScrollView>("tournamentMatchesList");
        addTournamentButton = root.Q<Button>("addTournamentButton");
        saveTournamentsButton = root.Q<Button>("saveTournamentsButton");
        viewTournamentsButton = root.Q<Button>("viewTournamentsButton");
        saveTournamentButton = root.Q<Button>("saveTournamentButton");
        deleteTournamentButton = root.Q<Button>("deleteTournamentButton");
        cancelTournamentButton = root.Q<Button>("cancelTournamentButton");
        addEntrantButton = root.Q<Button>("addEntrantButton");
        removeEntrantButton = root.Q<Button>("removeEntrantButton");
        generateBracketButton = root.Q<Button>("generateBracketButton");
        advanceRoundButton = root.Q<Button>("advanceRoundButton");
        clearBracketButton = root.Q<Button>("clearBracketButton");
        // Stables queries
        stableListScroll = root.Q<ScrollView>("stableList");
        stableNameField = root.Q<TextField>("stableNameField");
        stableMemberDropdown = root.Q<DropdownField>("stableMemberDropdown");
        SetupDropdownOverlay(stableMemberDropdown);
        stableMembersList = root.Q<ScrollView>("stableMembersList");
        addStableButton = root.Q<Button>("addStableButton");
        saveStablesButton = root.Q<Button>("saveStablesButton");
        saveStableButton = root.Q<Button>("saveStableButton");
        deleteStableButton = root.Q<Button>("deleteStableButton");
        cancelStableButton = root.Q<Button>("cancelStableButton");
        addStableMemberButton = root.Q<Button>("addStableMemberButton");
        removeStableMemberButton = root.Q<Button>("removeStableMemberButton");
        stableActionsRow = root.Q<VisualElement>("stableActionsRow");
        tagTeamListScroll = root.Q<ScrollView>("tagTeamList");
        showsListScroll = root.Q<ScrollView>("showsList");
        historyShowsListScroll = root.Q<ScrollView>("historyShowsList");
        rankingsListScroll = root.Q<ScrollView>("rankingsList");
        matchesView = root.Q<VisualElement>("matchesView");
        rankingsMenButton = root.Q<Button>("rankingsMenButton");
        rankingsWomenButton = root.Q<Button>("rankingsWomenButton");
        rankingsTagButton = root.Q<Button>("rankingsTagButton");
        rankingsStableButton = root.Q<Button>("rankingsStableButton");
        // Title edit/display widgets
        titleDetailsPanel = root.Q<VisualElement>("titleDetails");
        titleAddPanel = root.Q<VisualElement>("titleAddPanel");
        titleNameField = root.Q<TextField>("titleNameField");
        titleDivisionField = root.Q<TextField>("titleDivisionField");
        titleChampionField = root.Q<TextField>("titleChampionField");
        titleNotesField = root.Q<TextField>("titleNotesField");
        viewHistoryButton = root.Q<Button>("viewHistoryButton");
        titleStatsPanel = root.Q<VisualElement>("titleStatsPanel");
        titleStatsView = root.Q<VisualElement>("titleStatsView");
        titleStatsList = root.Q<ScrollView>("titleStatsList");
        titleStatsCloseButton = root.Q<Button>("titleStatsCloseButton");
        titleStatsCurrentLabel = root.Q<Label>("titleStatsCurrent");
        titleStatsSummaryLabel = root.Q<Label>("titleStatsSummary");
        titleStatsLongestLabel = root.Q<Label>("titleStatsLongest");
        titleStatsShortestLabel = root.Q<Label>("titleStatsShortest");
        titleStatsMostDefensesLabel = root.Q<Label>("titleStatsMostDefenses");
        // Hide stats panel on details by default; stats will render in history view
        titleStatsPanel?.AddToClassList("hidden");
        // Shows add/save widgets
        newShowField = root.Q<TextField>("newShowField");
        newShowDateField = root.Q<TextField>("newShowDateField");
        newShowVenueField = root.Q<TextField>("newShowVenueField");
        newShowCityField = root.Q<TextField>("newShowCityField");
        newShowAttendanceField = root.Q<IntegerField>("newShowAttendanceField");
        newShowRatingField = root.Q<FloatField>("newShowRatingField");
        addShowButton = root.Q<Button>("addShowButton");
        saveShowsButton = root.Q<Button>("saveShowsButton");
        // Show details and editors
        showDetailsPanel = root.Q<VisualElement>("showDetails");
        showAddPanel = root.Q<VisualElement>("showAddPanel");
        showNameField = root.Q<TextField>("showNameField");
        showDateField = root.Q<TextField>("showDateField");
        showVenueField = root.Q<TextField>("showVenueField");
        showCityField = root.Q<TextField>("showCityField");
        showAttendanceField = root.Q<IntegerField>("showAttendanceField");
        showRatingField = root.Q<FloatField>("showRatingField");
        saveShowButton = root.Q<Button>("saveShowButton");
        deleteShowButton = root.Q<Button>("deleteShowButton");
        cancelShowButton = root.Q<Button>("cancelShowButton");
        viewMatchesButton = root.Q<Button>("viewMatchesButton");
        addMatchButton = root.Q<Button>("addMatchButton");
        addSegmentButton = root.Q<Button>("addSegmentButton");
        matchEditor = root.Q<VisualElement>("matchEditor");
        segmentEditor = root.Q<VisualElement>("segmentEditor");
        matchTypeDropdown = root.Q<DropdownField>("matchTypeDropdown");
        wrestlerADropdown = root.Q<DropdownField>("wrestlerADropdown");
        wrestlerBDropdown = root.Q<DropdownField>("wrestlerBDropdown");
        wrestlerCDropdown = root.Q<DropdownField>("wrestlerCDropdown");
        wrestlerDDropdown = root.Q<DropdownField>("wrestlerDDropdown");
        isTitleMatchToggle = root.Q<Toggle>("isTitleMatchToggle");
        titleDropdown = root.Q<DropdownField>("titleDropdown");
        winnerDropdown = root.Q<DropdownField>("winnerDropdown");
        saveMatchButton = root.Q<Button>("saveMatchButton");
        cancelMatchButton = root.Q<Button>("cancelMatchButton");
        segmentNameField = root.Q<TextField>("segmentNameField");
        segmentTextField = root.Q<TextField>("segmentTextField");
        saveSegmentButton = root.Q<Button>("saveSegmentButton");
        cancelSegmentButton = root.Q<Button>("cancelSegmentButton");

        // Attach date picker buttons and normalization to date fields
        if (showDateField != null)
        {
            showDatePickButton = new Button(() => OpenDatePicker(showDateField)) { text = "\U0001F4C5" };
            showDatePickButton.style.width = 28; showDatePickButton.style.height = 22; showDatePickButton.style.marginLeft = 6;
            showDateField.parent?.Add(showDatePickButton);
            showDateField.RegisterCallback<FocusOutEvent>(_ => { NormalizeDateField(showDateField); });
        }
        if (newShowDateField != null)
        {
            newShowDatePickButton = new Button(() => OpenDatePicker(newShowDateField)) { text = "\U0001F4C5" };
            newShowDatePickButton.style.width = 28; newShowDatePickButton.style.height = 22; newShowDatePickButton.style.marginLeft = 6;
            newShowDateField.parent?.Add(newShowDatePickButton);
            newShowDateField.RegisterCallback<FocusOutEvent>(_ => { NormalizeDateField(newShowDateField); });
        }

        if (historyLocationFilterField != null)
        {
            historyLocationFilterField.RegisterValueChangedCallback(_ => PopulateHistoryShowsList());
        }
        newTitleField = root.Q<TextField>("newTitleField");
        addTitleButton = root.Q<Button>("addTitleButton");
        saveTitlesButton = root.Q<Button>("saveTitlesButton");
        saveTitleButton = root.Q<Button>("saveTitleButton");
        deleteTitleButton = root.Q<Button>("deleteTitleButton");
        cancelTitleButton = root.Q<Button>("cancelTitleButton");
        
        // Promotion info UI
        nameLabel = root.Q<Label>("nameLabel");
        locationLabel = root.Q<Label>("locationLabel");
        foundedLabel = root.Q<Label>("foundedLabel");
        descriptionLabel = root.Q<Label>("descriptionLabel");
        editPromotionButton = root.Q<Button>("editPromotionButton");
        savePromotionButton = root.Q<Button>("savePromotionButton");
        cancelPromotionButton = root.Q<Button>("cancelPromotionButton");
        nameField = root.Q<TextField>("nameField");
        locationField = root.Q<TextField>("locationField");
        foundedField = root.Q<TextField>("foundedField");
        descriptionField = root.Q<TextField>("descriptionField");
        editPanel = root.Q<VisualElement>("editPanel");
        // Tag Teams queries
        teamNameField = root.Q<TextField>("teamNameField");
        teamMemberADropdown = root.Q<DropdownField>("teamMemberADropdown");
        teamMemberBDropdown = root.Q<DropdownField>("teamMemberBDropdown");
        SetupDropdownOverlay(teamMemberADropdown);
        SetupDropdownOverlay(teamMemberBDropdown);
        addTeamButton = root.Q<Button>("addTeamButton");
        saveTeamsButton = root.Q<Button>("saveTeamsButton");
        saveTeamButton = root.Q<Button>("saveTeamButton");
        deleteTeamButton = root.Q<Button>("deleteTeamButton");
        cancelTeamButton = root.Q<Button>("cancelTeamButton");
        tagTeamEmptyLabel = root.Q<Label>("tagTeamEmptyLabel");

        // Ensure Tournaments default state: show Add, hide Manage
        if (tournamentManagePanel != null) tournamentManagePanel.AddToClassList("hidden");
        if (tournamentAddPanel != null) tournamentAddPanel.RemoveFromClassList("hidden");

        // Register panels
        mainPanels.Clear();
        RegisterMainPanel(promotionInfoPanel);
        RegisterMainPanel(wrestlersPanel);
        RegisterMainPanel(titlesPanel);
        RegisterMainPanel(tournamentsPanel);
        RegisterMainPanel(stablesPanel);
        RegisterMainPanel(tagTeamsPanel);
        RegisterMainPanel(showsPanel);
        RegisterMainPanel(calendarPanel);
        RegisterMainPanel(cardBuilderPanel);
        RegisterMainPanel(historyPanel);
        RegisterMainPanel(rankingsPanel);

        // Wire navigation
        if (promotionButton != null) promotionButton.clicked += ShowPromotionPanel;
        if (wrestlersButton != null) wrestlersButton.clicked += ShowWrestlersPanel;
        if (titlesButton != null) titlesButton.clicked += ShowTitlesPanel;
        if (tagTeamsButton != null) tagTeamsButton.clicked += ShowTagTeamsPanel;
        if (showsButton != null) showsButton.clicked += ShowShowsPanel;
        if (calendarButton != null) calendarButton.clicked += ShowCalendarPanel;
        if (historyButton != null) historyButton.clicked += ShowHistoryPanel;
        if (rankingsButton != null) rankingsButton.clicked += ShowRankingsPanel;
        if (stablesButton != null) stablesButton.clicked += ShowStablesPanel;
        if (tournamentsButton != null) tournamentsButton.clicked += ShowTournamentsPanel;
        if (viewTournamentsButton != null) viewTournamentsButton.clicked += ShowTournamentManagePanel;
        if (rankingsMenButton != null) rankingsMenButton.clicked += () => PopulateRankings(RankCategory.Men);
        if (rankingsWomenButton != null) rankingsWomenButton.clicked += () => PopulateRankings(RankCategory.Women);
        if (rankingsTagButton != null) rankingsTagButton.clicked += () => PopulateRankings(RankCategory.TagTeam);
        if (rankingsStableButton != null) rankingsStableButton.clicked += () => PopulateRankings(RankCategory.Stable);
        // Tournaments handlers
        if (addTournamentButton != null) addTournamentButton.clicked += OnAddTournament;
        if (saveTournamentsButton != null) saveTournamentsButton.clicked += OnSaveTournaments;
        if (saveTournamentButton != null) saveTournamentButton.clicked += OnSaveSelectedTournament;
        if (deleteTournamentButton != null) deleteTournamentButton.clicked += OnDeleteSelectedTournament;
        if (cancelTournamentButton != null) cancelTournamentButton.clicked += OnCancelEditTournament;
        if (addEntrantButton != null) addEntrantButton.clicked += OnAddEntrant;
        if (removeEntrantButton != null) removeEntrantButton.clicked += OnRemoveEntrant;
        if (generateBracketButton != null) generateBracketButton.clicked += OnGenerateBracket;
        if (advanceRoundButton != null) advanceRoundButton.clicked += OnAdvanceRound;
        if (clearBracketButton != null) clearBracketButton.clicked += OnClearBracket;
        if (viewHistoryButton != null) viewHistoryButton.clicked += ShowSelectedTitleHistory;
        if (titleStatsCloseButton != null) titleStatsCloseButton.clicked += () =>
        {
            // Return to lineage view
            titleStatsView?.AddToClassList("hidden");
            titleHistoryList?.RemoveFromClassList("hidden");
            FocusPanel(titleHistoryList);
        };
        // Titles handlers
        if (addTitleButton != null) addTitleButton.clicked += OnAddTitle;
        if (saveTitlesButton != null) saveTitlesButton.clicked += OnSaveTitles;
        if (saveTitleButton != null) saveTitleButton.clicked += OnSaveSelectedTitle;
        if (deleteTitleButton != null) deleteTitleButton.clicked += OnDeleteSelectedTitle;
        if (cancelTitleButton != null) cancelTitleButton.clicked += OnCancelEditTitle;

        // Initialize Calendar & Card Builder views
        if (calendarPanel != null)
        {
            calendarView = new CalendarView();
            calendarView.Initialize(calendarPanel, () => currentPromotion);
            calendarView.CreateShowRequested += OnCreateShowFromCalendar;
            calendarView.EditShowRequested += OnEditShowFromCalendar;
        }
        if (cardBuilderPanel != null)
        {
            cardBuilderView = new CardBuilderView();
            cardBuilderView.Initialize(cardBuilderPanel, () => currentPromotion);
            cardBuilderView.Saved += OnCardBuilderSaved;
            cardBuilderView.Canceled += () => SetActivePanel(calendarPanel);
        }
        // Tag Teams handlers
        if (addTeamButton != null) addTeamButton.clicked += OnAddTeam;
        if (saveTeamsButton != null) saveTeamsButton.clicked += OnSaveTeams;
        if (saveTeamButton != null) saveTeamButton.clicked += OnSaveSelectedTeam;
        if (deleteTeamButton != null) deleteTeamButton.clicked += OnDeleteSelectedTeam;
        if (cancelTeamButton != null) cancelTeamButton.clicked += OnCancelEditTeam;
        // Wrestlers handlers
        if (addWrestlerButton != null) addWrestlerButton.clicked += OnAddWrestler;
        if (saveWrestlersButton != null) saveWrestlersButton.clicked += OnSaveWrestlers;
        if (saveWrestlerButton != null) saveWrestlerButton.clicked += OnSaveSelectedWrestler;
        if (deleteWrestlerButton != null) deleteWrestlerButton.clicked += OnDeleteSelectedWrestler;
        if (cancelEditButton != null) cancelEditButton.clicked += OnCancelEditWrestler;
        // Shows handlers (basic add/save)
        if (addShowButton != null) addShowButton.clicked += OnAddShow;
        if (saveShowsButton != null) saveShowsButton.clicked += OnSaveShows;
        // Promotion info handlers
        if (editPromotionButton != null) editPromotionButton.clicked += ShowPromotionEditPanel;
        if (savePromotionButton != null) savePromotionButton.clicked += SavePromotionEdits;
        if (cancelPromotionButton != null) cancelPromotionButton.clicked += HidePromotionEditPanel;
        // Shows handlers
        if (addShowButton != null) addShowButton.clicked += OnAddShow;
        if (saveShowsButton != null) saveShowsButton.clicked += OnSaveShows;
        if (saveShowButton != null) saveShowButton.clicked += OnSaveSelectedShow;
        if (deleteShowButton != null) deleteShowButton.clicked += OnDeleteSelectedShow;
        if (cancelShowButton != null) cancelShowButton.clicked += OnCancelEditShow;
        if (viewMatchesButton != null) viewMatchesButton.clicked += () =>
        {
            if (selectedShowIndex >= 0 && currentPromotion?.shows != null && selectedShowIndex < currentPromotion.shows.Count)
                EditShow(currentPromotion.shows[selectedShowIndex]);
        };
        // Stables handlers (registered outside of other callbacks)
        if (addStableButton != null) addStableButton.clicked += OnAddStable;
        if (saveStablesButton != null) saveStablesButton.clicked += OnSaveStables;
        if (saveStableButton != null) saveStableButton.clicked += OnSaveSelectedStable;
        if (deleteStableButton != null) deleteStableButton.clicked += OnDeleteSelectedStable;
        if (cancelStableButton != null) cancelStableButton.clicked += OnCancelEditStable;
        if (addStableMemberButton != null) addStableMemberButton.clicked += OnAddStableMember;
        if (removeStableMemberButton != null) removeStableMemberButton.clicked += OnRemoveStableMember;
        if (addMatchButton != null) addMatchButton.clicked += ShowMatchEditor;
        if (addSegmentButton != null) addSegmentButton.clicked += ShowSegmentEditor;
        if (saveMatchButton != null) saveMatchButton.clicked += SaveMatch;
        if (cancelMatchButton != null) cancelMatchButton.clicked += () => { matchEditor?.AddToClassList("hidden"); FocusPanel(showDetailsPanel ?? showsPanel); };
        if (saveSegmentButton != null) saveSegmentButton.clicked += SaveSegment;
        if (cancelSegmentButton != null) cancelSegmentButton.clicked += () => { segmentEditor?.AddToClassList("hidden"); FocusPanel(showDetailsPanel ?? showsPanel); };
        if (returnButton != null)
        {
            returnButton.clicked += () =>
            {
                Debug.Log("Returning to Main Menu...");
                if (PromotionSession.Instance != null)
                    PromotionSession.Instance.CurrentPromotion = null;
                if (SceneLoader.Instance != null)
                    SceneLoader.Instance.LoadScene("MainMenu");
                else
                    UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            };
        }

        var closeResultsBtn = root.Q<Button>("historyCloseResultsButton");
        if (closeResultsBtn != null)
        {
            closeResultsBtn.clicked += () =>
            {
                historyResultsPanel?.AddToClassList("hidden");
                historyShowsPanel?.RemoveFromClassList("hidden");
                FocusPanel(historyShowsPanel ?? historyPanel);
            };
        }

        // Ensure virtualized lists
        EnsureWrestlerListView();
        EnsureTitleListView();
        EnsureTournamentListView();
        EnsureStableListView();
        EnsureTagTeamListView();
        EnsureShowsListView();
        EnsureHistoryShowsListView();
        EnsureRankingsListView();
        EnsureMatchesOrderListView();
        EnsureDefaultMatchTypes();
        EnsureTournamentTypeChoices();
        if (tournamentTypeDropdown != null)
            tournamentTypeDropdown.RegisterValueChangedCallback(_ => PopulateEntrantChoices(tournamentTypeDropdown.value));

        // Stables handlers
        if (addStableButton != null) addStableButton.clicked += OnAddStable;
        if (saveStablesButton != null) saveStablesButton.clicked += OnSaveStables;
        if (saveStableButton != null) saveStableButton.clicked += OnSaveSelectedStable;
        if (deleteStableButton != null) deleteStableButton.clicked += OnDeleteSelectedStable;
        if (cancelStableButton != null) cancelStableButton.clicked += OnCancelEditStable;
        if (addStableMemberButton != null) addStableMemberButton.clicked += OnAddStableMember;
        if (removeStableMemberButton != null) removeStableMemberButton.clicked += OnRemoveStableMember;

        // Load data for lists
        if (currentPromotion != null)
        {
            wrestlerCollection = DataManager.LoadWrestlers(currentPromotion.promotionName);
            titleCollection = DataManager.LoadTitles(currentPromotion.promotionName);
            tagTeamCollection = DataManager.LoadTagTeams(currentPromotion.promotionName);
            tournamentCollection = DataManager.LoadTournaments(currentPromotion.promotionName);
            stableCollection = DataManager.LoadStables(currentPromotion.promotionName);
            // Step 2: ensure stable IDs and upgraded entryOrder
            EnsureStableIdsAndEntryOrder();
        }
        RefreshWrestlerList();
        RefreshTitleList();
        RefreshTagTeamList();
        RefreshTournamentList();
        RefreshStableList();
        RefreshShowList();
        PopulateHistoryShowsList();
        PopulateRankings(RankCategory.Men);

        // Default panel and status
        SetActivePanel(promotionInfoPanel ?? root);
        if (statusLabel != null)
            statusLabel.text = currentPromotion != null ? $"Loaded: {currentPromotion.promotionName}" : "Ready.";
        // Populate promotion info labels
        UpdatePromotionInfoUI();
    }

    private void EnsureDefaultMatchTypes()
    {
        if (matchTypeDropdown == null) return;
        if (matchTypeDropdown.choices == null || matchTypeDropdown.choices.Count == 0)
        {
            matchTypeDropdown.choices = new List<string>
            {
                "Singles",
                "Tag Team",
                "Trios",
                "Triple Threat",
                "Fatal Four Way",
                "Ladder",
                "Steel Cage",
                "No DQ",
                "Submission",
                "Falls Count Anywhere",
                "Battle Royal"
            };
            matchTypeDropdown.value = matchTypeDropdown.choices[0];
        }
    }

    private void UpdatePromotionInfoUI()
    {
        if (currentPromotion == null)
        {
            if (nameLabel != null) nameLabel.text = "Name: [None]";
            if (locationLabel != null) locationLabel.text = "Location: [None]";
            if (foundedLabel != null) foundedLabel.text = "Founded: [None]";
            if (descriptionLabel != null) descriptionLabel.text = "Description: [None]";
            return;
        }

        if (nameLabel != null) nameLabel.text = $"Name: {currentPromotion.promotionName}";
        if (locationLabel != null) locationLabel.text = $"Location: {currentPromotion.location}";
        if (foundedLabel != null) foundedLabel.text = $"Founded: {currentPromotion.foundedYear}";
        if (descriptionLabel != null) descriptionLabel.text = $"Description: {currentPromotion.description}";
    }

    private void ShowPromotionEditPanel()
    {
        if (currentPromotion == null || editPanel == null) return;
        if (nameField != null) nameField.value = currentPromotion.promotionName;
        if (locationField != null) locationField.value = currentPromotion.location;
        if (foundedField != null) foundedField.value = currentPromotion.foundedYear;
        if (descriptionField != null) descriptionField.value = currentPromotion.description;
        editPanel.RemoveFromClassList("hidden");
        FocusPanel(editPanel);
    }

    private void HidePromotionEditPanel()
    {
        editPanel?.AddToClassList("hidden");
        FocusPanel(promotionInfoPanel ?? root);
    }

    private void SavePromotionEdits()
    {
        if (currentPromotion == null) return;
        if (nameField != null) currentPromotion.promotionName = nameField.value;
        if (locationField != null) currentPromotion.location = locationField.value;
        if (foundedField != null) currentPromotion.foundedYear = foundedField.value;
        if (descriptionField != null) currentPromotion.description = descriptionField.value;

        DataManager.SavePromotion(currentPromotion);
        UpdatePromotionInfoUI();
        HidePromotionEditPanel();
        if (statusLabel != null) statusLabel.text = "Promotion updated.";
    }

    // Panel registration helpers
    private void RegisterMainPanel(VisualElement panel)
    {
        if (panel == null) return;
        if (!mainPanels.Contains(panel)) mainPanels.Add(panel);
        RegisterFocusablePanel(panel);
    }

    private void RegisterFocusablePanel(params VisualElement[] panels)
    {
        if (panels == null) return;
        foreach (var p in panels)
        {
            if (p == null) continue;
            if (!focusablePanels.Contains(p)) focusablePanels.Add(p);
        }
    }

    private void SetActivePanel(VisualElement panel)
    {
        if (panel == null) return;
        foreach (var p in mainPanels)
        {
            if (p == null) continue;
            if (p == panel) p.RemoveFromClassList("hidden"); else p.AddToClassList("hidden");
        }
        FocusPanel(panel);
    }

    private void FocusPanel(VisualElement panel)
    {
        if (panel == null) return;
        foreach (var p in focusablePanels) p?.RemoveFromClassList("focused-panel");
        panel.AddToClassList("focused-panel");
        var scrollParent = panel.GetFirstAncestorOfType<ScrollView>();
        scrollParent?.ScrollTo(panel);
        panel.Focus();
    }

    // Navigation handlers
    private void ShowPromotionPanel() => SetActivePanel(promotionInfoPanel);
    private void ShowWrestlersPanel() => SetActivePanel(wrestlersPanel);
    private void ShowTitlesPanel() => SetActivePanel(titlesPanel);
    private void ShowTournamentsPanel()
    {
        // Default to Add view when entering the tab
        ShowTournamentAddPanel();
    }
    private void ShowStablesPanel()
    {
        SetActivePanel(stablesPanel);
        // Ensure data and views
        stableCollection ??= DataManager.LoadStables(currentPromotion?.promotionName);
        EnsureStableListView();
        RefreshStableList();
        // Auto-select first stable to populate members and dropdown
        if ((selectedStableIndex < 0) && (stableCollection?.stables != null) && stableCollection.stables.Count > 0)
        {
            SelectStable(0);
        }
        else
        {
            // Populate dropdown choices even if no selection yet
            PopulateStableMemberChoices();
        }
    }
    private void ShowTagTeamsPanel()
    {
        SetActivePanel(tagTeamsPanel);
        RefreshTagTeamList();
        FocusPanel(tagTeamsPanel ?? titlesPanel ?? root);
    }
    private void ShowShowsPanel() => SetActivePanel(showsPanel);
    private void ShowCalendarPanel()
    {
        calendarView?.Refresh();
        SetActivePanel(calendarPanel);
    }
    private void ShowHistoryPanel() => SetActivePanel(historyPanel);
    private void ShowRankingsPanel() => SetActivePanel(rankingsPanel);

    private void OnCreateShowFromCalendar(DateTime date)
    {
        if (cardBuilderView == null) return;
        cardBuilderView.BeginNew(date);
        SetActivePanel(cardBuilderPanel);
    }

    private void OnEditShowFromCalendar(ShowData show)
    {
        if (cardBuilderView == null) return;
        cardBuilderView.BeginEdit(show);
        SetActivePanel(cardBuilderPanel);
    }

    private void OnCardBuilderSaved(ShowData show, string prevName, string prevDate)
    {
        if (currentPromotion == null) return;
        // Upsert show into promotion
        currentPromotion.shows ??= new List<ShowData>();
        var existing = currentPromotion.shows.FirstOrDefault(s => s == show || (s.showName == prevName && s.date == prevDate));
        if (existing == null)
        {
            // match by unique name+date if possible
            existing = currentPromotion.shows.FirstOrDefault(s => s.showName == show.showName && s.date == show.date);
        }
        if (existing == null)
        {
            currentPromotion.shows.Add(show);
        }
        // Save promotion and update histories
        DataManager.SavePromotion(currentPromotion);
        TitleHistoryManager.UpdateShowResults(currentPromotion, show, prevName, prevDate);
        // Refresh calendar
        calendarView?.Refresh();
        SetActivePanel(calendarPanel);
    }

    // ----- Virtualized List helpers -----
    private void EnsureWrestlerListView()
    {
        if (wrestlerListView != null) return;
        var parent = wrestlerListScroll != null ? wrestlerListScroll.parent : wrestlersPanel;
        wrestlerListView = new ListView
        {
            name = "wrestlerListView",
            selectionType = SelectionType.None,
            fixedItemHeight = 36f
        };
        wrestlerListView.style.flexGrow = 1;
        wrestlerListView.makeItem = () =>
        {
            var b = new Button();
            b.AddToClassList("list-entry");
            b.RegisterCallback<ClickEvent>(_ => { if (b.userData is int idx) SelectWrestler(idx); });
            return b;
        };
        wrestlerListView.bindItem = (ve, i) =>
        {
            var b = (Button)ve;
            var list = wrestlerCollection?.wrestlers;
            if (list != null && i >= 0 && i < list.Count) { b.text = list[i].name; b.userData = i; }
            else { b.text = string.Empty; b.userData = -1; }
        };
        parent?.Add(wrestlerListView);
        if (wrestlerListScroll != null) wrestlerListScroll.style.display = DisplayStyle.None;
    }

    private void RefreshWrestlerList()
    {
        if (wrestlerListView == null) return;
        var src = wrestlerCollection?.wrestlers ?? new List<WrestlerData>();
        wrestlerListView.itemsSource = src;
        wrestlerListView.Rebuild();
    }

    private void OnAddWrestler()
    {
        if (currentPromotion == null) { if (statusLabel != null) statusLabel.text = "No promotion loaded."; return; }
        var name = newWrestlerField != null ? (newWrestlerField.value ?? string.Empty).Trim() : string.Empty;
        if (string.IsNullOrEmpty(name)) { if (statusLabel != null) statusLabel.text = "Enter a wrestler name."; return; }

        wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion.promotionName) ?? new WrestlerCollection { promotionName = currentPromotion.promotionName };
        if (wrestlerCollection.promotionName != currentPromotion.promotionName)
            wrestlerCollection.promotionName = currentPromotion.promotionName;

        // Prevent duplicate names (case-insensitive)
        if (wrestlerCollection.wrestlers != null && wrestlerCollection.wrestlers.Any(w => !string.IsNullOrEmpty(w?.name) && string.Equals(w.name.Trim(), name, System.StringComparison.OrdinalIgnoreCase)))
        {
            if (statusLabel != null) statusLabel.text = "Wrestler already exists.";
            return;
        }

        var isFemale = newWrestlerIsFemaleToggle != null && newWrestlerIsFemaleToggle.value;
        var newW = new WrestlerData(name) { isFemale = isFemale, isTagTeam = false };
        wrestlerCollection.wrestlers ??= new List<WrestlerData>();
        wrestlerCollection.wrestlers.Add(newW);

        DataManager.SaveWrestlers(wrestlerCollection);
        RefreshWrestlerList();
        if (newWrestlerField != null) newWrestlerField.value = string.Empty;
        if (newWrestlerIsFemaleToggle != null) newWrestlerIsFemaleToggle.value = false;
        
        if (statusLabel != null) statusLabel.text = "Wrestler added.";
    }

    private void OnSaveWrestlers()
    {
        if (currentPromotion == null || wrestlerCollection == null) return;
        wrestlerCollection.promotionName = currentPromotion.promotionName;
        DataManager.SaveWrestlers(wrestlerCollection);
        if (statusLabel != null) statusLabel.text = "Wrestlers saved.";
    }

    private void SelectWrestler(int index)
    {
        if (wrestlerCollection?.wrestlers == null || index < 0 || index >= wrestlerCollection.wrestlers.Count) return;
        selectedWrestlerIndex = index;
        var w = wrestlerCollection.wrestlers[index];
        if (wrestlerDetails != null) wrestlerDetails.RemoveFromClassList("hidden");
        if (wrestlerAddPanel != null) wrestlerAddPanel.AddToClassList("hidden");
        if (wrestlerNameField != null) wrestlerNameField.value = w.name;
        if (wrestlerHometownField != null) wrestlerHometownField.value = w.hometown;
        if (wrestlerIsFemaleToggle != null) wrestlerIsFemaleToggle.value = w.isFemale;
        if (wrestlerHeightField != null) wrestlerHeightField.value = w.height;
        if (wrestlerWeightField != null) wrestlerWeightField.value = w.weight;
        FocusPanel(wrestlerDetails ?? wrestlersPanel);
    }

    private void OnSaveSelectedWrestler()
    {
        if (wrestlerCollection?.wrestlers == null || selectedWrestlerIndex < 0 || selectedWrestlerIndex >= wrestlerCollection.wrestlers.Count) return;
        var w = wrestlerCollection.wrestlers[selectedWrestlerIndex];
        if (wrestlerNameField != null) w.name = wrestlerNameField.value;
        if (wrestlerHometownField != null) w.hometown = wrestlerHometownField.value;
        if (wrestlerIsFemaleToggle != null) w.isFemale = wrestlerIsFemaleToggle.value;
        if (wrestlerHeightField != null) w.height = wrestlerHeightField.value;
        if (wrestlerWeightField != null) w.weight = wrestlerWeightField.value;
        DataManager.SaveWrestlers(wrestlerCollection);
        RefreshWrestlerList();
        // After saving, return to add mode: hide details, show add panel
        if (wrestlerDetails != null) wrestlerDetails.AddToClassList("hidden");
        selectedWrestlerIndex = -1;
        if (wrestlerAddPanel != null) wrestlerAddPanel.RemoveFromClassList("hidden");
        if (statusLabel != null) statusLabel.text = "Wrestler updated.";
        FocusPanel(wrestlersPanel);
    }

    private void OnDeleteSelectedWrestler()
    {
        if (wrestlerCollection?.wrestlers == null || selectedWrestlerIndex < 0 || selectedWrestlerIndex >= wrestlerCollection.wrestlers.Count) return;
        wrestlerCollection.wrestlers.RemoveAt(selectedWrestlerIndex);
        selectedWrestlerIndex = -1;
        DataManager.SaveWrestlers(wrestlerCollection);
        RefreshWrestlerList();
        if (wrestlerDetails != null) wrestlerDetails.AddToClassList("hidden");
        if (wrestlerAddPanel != null) wrestlerAddPanel.RemoveFromClassList("hidden");
        if (statusLabel != null) statusLabel.text = "Wrestler deleted.";
    }

    private void OnCancelEditWrestler()
    {
        if (wrestlerDetails != null) wrestlerDetails.AddToClassList("hidden");
        selectedWrestlerIndex = -1;
        if (wrestlerAddPanel != null) wrestlerAddPanel.RemoveFromClassList("hidden");
        FocusPanel(wrestlersPanel);
    }

    // --------- Titles (Step 4) ---------
    private void EnsureTitleListView()
    {
        if (titleListView != null) return;
        var parent = titleListScroll != null ? titleListScroll.parent : titlesPanel;
        titleListView = new ListView
        {
            name = "titleListView",
            selectionType = SelectionType.None,
            fixedItemHeight = 36f
        };
        titleListView.style.flexGrow = 1;
        titleListView.makeItem = () =>
        {
            var b = new Button(); b.AddToClassList("list-entry");
            b.RegisterCallback<ClickEvent>(_ => { if (b.userData is int idx) SelectTitle(idx); });
            return b;
        };
        titleListView.bindItem = (ve, i) =>
        {
            var b = (Button)ve;
            var list = titleCollection?.titles;
            if (list != null && i >= 0 && i < list.Count) { b.text = list[i].titleName; b.userData = i; }
            else { b.text = string.Empty; b.userData = -1; }
        };
        parent?.Add(titleListView);
        if (titleListScroll != null) titleListScroll.style.display = DisplayStyle.None;
    }

    // --------- Tournaments ---------
    private void EnsureTournamentListView()
    {
        if (tournamentListView != null) return;
        var parent = tournamentListScroll != null ? tournamentListScroll.parent : tournamentsPanel;
        tournamentListView = new ListView
        {
            name = "tournamentListView",
            selectionType = SelectionType.None,
            fixedItemHeight = 36f
        };
        tournamentListView.style.flexGrow = 1;
        tournamentListView.makeItem = () =>
        {
            var b = new Button();
            b.AddToClassList("list-entry");
            b.RegisterCallback<ClickEvent>(_ => { if (b.userData is int idx) SelectTournament(idx); });
            return b;
        };
        tournamentListView.bindItem = (ve, i) =>
        {
            var b = (Button)ve;
            var list = tournamentCollection?.tournaments;
            if (list != null && i >= 0 && i < list.Count) { b.text = list[i].name; b.userData = i; }
            else { b.text = string.Empty; b.userData = -1; }
        };
        parent?.Add(tournamentListView);
        if (tournamentListScroll != null) tournamentListScroll.style.display = DisplayStyle.None;
    }

    private void RefreshTournamentList()
    {
        if (tournamentListView == null) return;
        var src = tournamentCollection?.tournaments ?? new List<TournamentData>();
        tournamentListView.itemsSource = src;
        tournamentListView.Rebuild();
    }

    private void SelectTournament(int index)
    {
        if (tournamentCollection?.tournaments == null || index < 0 || index >= tournamentCollection.tournaments.Count) return;
        selectedTournamentIndex = index;
        var t = tournamentCollection.tournaments[index];
        if (tournamentNameField != null) tournamentNameField.value = t.name;
        EnsureTournamentTypeChoices();
        if (tournamentTypeDropdown != null) tournamentTypeDropdown.value = string.IsNullOrEmpty(t.type) ? "Singles" : t.type;
        PopulateEntrantsUI(t);
        PopulateMatchesUI(t);
        SetActivePanel(tournamentsPanel);
    }

    private void EnsureTournamentTypeChoices()
    {
        if (tournamentTypeDropdown != null)
        {
            if (tournamentTypeDropdown.choices == null || tournamentTypeDropdown.choices.Count == 0)
                tournamentTypeDropdown.choices = new List<string> { "Singles", "Tag Team", "Trios" };
            if (string.IsNullOrEmpty(tournamentTypeDropdown.value)) tournamentTypeDropdown.value = tournamentTypeDropdown.choices[0];
        }
        if (newTournamentTypeDropdown != null)
        {
            if (newTournamentTypeDropdown.choices == null || newTournamentTypeDropdown.choices.Count == 0)
                newTournamentTypeDropdown.choices = new List<string> { "Singles", "Tag Team", "Trios" };
            if (string.IsNullOrEmpty(newTournamentTypeDropdown.value)) newTournamentTypeDropdown.value = newTournamentTypeDropdown.choices[0];
        }
    }

    private void PopulateEntrantChoices(string type)
    {
        if (tournamentEntrantDropdown == null) return;
        var choices = new List<string>();
        if (string.Equals(type, "Tag Team", StringComparison.OrdinalIgnoreCase))
        {
            tagTeamCollection ??= DataManager.LoadTagTeams(currentPromotion.promotionName);
            foreach (var g in tagTeamCollection?.teams ?? new List<TagTeamData>())
                if (!string.IsNullOrEmpty(g?.teamName)) choices.Add(g.teamName);
        }
        else
        {
            wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion.promotionName);
            foreach (var w in wrestlerCollection?.wrestlers ?? new List<WrestlerData>())
                if (!string.IsNullOrEmpty(w?.name)) choices.Add(w.name);
        }
        if (choices.Count == 0) choices.Add(string.Empty);
        tournamentEntrantDropdown.choices = choices;
        tournamentEntrantDropdown.value = choices[0];
    }

    private void PopulateEntrantsUI(TournamentData t)
    {
        if (tournamentEntrantsList == null) return;
        tournamentEntrantsList.Clear();
        foreach (var e in t.entrants ?? new List<TournamentEntry>())
        {
            var label = new Label(e?.name ?? "");
            tournamentEntrantsList.Add(label);
        }
        PopulateEntrantChoices(t.type);
    }

    private void PopulateMatchesUI(TournamentData t)
    {
        if (tournamentMatchesList == null) return;
        tournamentMatchesList.Clear();
        if (t.rounds == null || t.rounds.Count == 0) return;

        var currentRound = t.rounds[^1];
        int i = 1;
        var nameById = BuildTournamentNameMap(t);
        foreach (var m in currentRound.matches ?? new List<TournamentMatch>())
        {
            var card = new VisualElement();
            card.style.flexDirection = FlexDirection.Column;
            card.style.marginBottom = 8;
            var dd = new DropdownField();
            var p1 = nameById.TryGetValue(m.participant1Id ?? string.Empty, out var n1) ? n1 : "";
            var p2 = nameById.TryGetValue(m.participant2Id ?? string.Empty, out var n2) ? n2 : "";
            var choices = new List<string>();
            if (!string.IsNullOrEmpty(p1)) choices.Add(p1);
            if (!string.IsNullOrEmpty(p2)) choices.Add(p2);
            var placeholder = "Select Winner";
            if (choices.Count == 0) choices.Add(string.Empty); else choices.Insert(0, placeholder);
            dd.choices = choices;
            if (m.winnerId == m.participant1Id) dd.value = p1;
            else if (m.winnerId == m.participant2Id) dd.value = p2;
            else dd.value = dd.choices[0];
            dd.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == p1) m.winnerId = m.participant1Id;
                else if (evt.newValue == p2) m.winnerId = m.participant2Id;
                else m.winnerId = null; // placeholder or cleared
            });
            // Participants line above the dropdown
            var matchTitle = new Label(string.IsNullOrEmpty(p1) && string.IsNullOrEmpty(p2) ? $"Match {i++}" : $"{p1} vs {p2}");
            matchTitle.style.marginBottom = 2;
            // Row with caption + dropdown
            var winRow = new VisualElement();
            winRow.style.flexDirection = FlexDirection.Row;
            winRow.style.alignItems = Align.Center;
            var winnerCaption = new Label("Match Winner:");
            winnerCaption.style.marginRight = 8;
            winRow.Add(winnerCaption);
            winRow.Add(dd);
            card.Add(matchTitle);
            card.Add(winRow);
            tournamentMatchesList.Add(card);
        }

        // If this is a finals round and a winner is selected, show champion message
        if (currentRound.matches != null && currentRound.matches.Count == 1)
        {
            var finalMatch = currentRound.matches[0];
            if (!string.IsNullOrEmpty(finalMatch?.winnerId) && nameById.TryGetValue(finalMatch.winnerId, out var champName))
            {
                var winnerNote = new Label($"{champName} has won this tournament");
                winnerNote.style.marginTop = 8;
                tournamentMatchesList.Add(winnerNote);
            }
        }
    }

    private Dictionary<string, string> BuildTournamentNameMap(TournamentData t)
    {
        var map = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        if (string.Equals(t.type, "Tag Team", StringComparison.OrdinalIgnoreCase))
        {
            tagTeamCollection ??= DataManager.LoadTagTeams(currentPromotion.promotionName);
            foreach (var g in tagTeamCollection?.teams ?? new List<TagTeamData>())
                if (!string.IsNullOrEmpty(g?.id) && !string.IsNullOrEmpty(g.teamName)) map[g.id] = g.teamName;
        }
        else
        {
            wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion.promotionName);
            foreach (var w in wrestlerCollection?.wrestlers ?? new List<WrestlerData>())
                if (!string.IsNullOrEmpty(w?.id) && !string.IsNullOrEmpty(w.name)) map[w.id] = w.name;
        }
        return map;
    }

    private void OnAddTournament()
    {
        if (currentPromotion == null) { if (statusLabel != null) statusLabel.text = "No promotion loaded."; return; }
        tournamentCollection ??= DataManager.LoadTournaments(currentPromotion.promotionName);
        tournamentCollection.promotionName = currentPromotion.promotionName;
        var t = new TournamentData { id = System.Guid.NewGuid().ToString("N"), name = (newTournamentNameField?.value ?? "New Tournament").Trim(), type = (newTournamentTypeDropdown?.value ?? "Singles") };
        t.entrants = new List<TournamentEntry>();
        t.rounds = new List<TournamentRound>();
        tournamentCollection.tournaments ??= new List<TournamentData>();
        tournamentCollection.tournaments.Add(t);
        DataManager.SaveTournaments(tournamentCollection);
        RefreshTournamentList();
        if (newTournamentNameField != null) newTournamentNameField.value = string.Empty;
        if (statusLabel != null) statusLabel.text = "Tournament added.";
    }

    private void OnSaveTournaments()
    {
        if (currentPromotion == null || tournamentCollection == null) return;
        tournamentCollection.promotionName = currentPromotion.promotionName;
        DataManager.SaveTournaments(tournamentCollection);
        if (statusLabel != null) statusLabel.text = "Tournaments saved.";
    }

    private void OnSaveSelectedTournament()
    {
        if (tournamentCollection?.tournaments == null || selectedTournamentIndex < 0 || selectedTournamentIndex >= tournamentCollection.tournaments.Count)
        {
            if (statusLabel != null) statusLabel.text = "Select a tournament first.";
            return;
        }
        var t = tournamentCollection.tournaments[selectedTournamentIndex];
        if (tournamentNameField != null) t.name = tournamentNameField.value;
        if (tournamentTypeDropdown != null) t.type = tournamentTypeDropdown.value;
        DataManager.SaveTournaments(tournamentCollection);
        RefreshTournamentList();
        if (statusLabel != null) statusLabel.text = "Tournament updated.";
        ShowTournamentAddPanel();
    }

    private void OnDeleteSelectedTournament()
    {
        if (tournamentCollection?.tournaments == null || selectedTournamentIndex < 0 || selectedTournamentIndex >= tournamentCollection.tournaments.Count) return;
        tournamentCollection.tournaments.RemoveAt(selectedTournamentIndex);
        selectedTournamentIndex = -1;
        DataManager.SaveTournaments(tournamentCollection);
        RefreshTournamentList();
        if (statusLabel != null) statusLabel.text = "Tournament deleted.";
    }

    private void OnCancelEditTournament()
    {
        selectedTournamentIndex = -1;
        if (tournamentNameField != null) tournamentNameField.value = string.Empty;
        ShowTournamentAddPanel();
    }

    private void ShowTournamentManagePanel()
    {
        if (tournamentAddPanel != null) tournamentAddPanel.AddToClassList("hidden");
        if (tournamentManagePanel != null) tournamentManagePanel.RemoveFromClassList("hidden");
        SetActivePanel(tournamentsPanel);
    }

    private void ShowTournamentAddPanel()
    {
        if (tournamentManagePanel != null) tournamentManagePanel.AddToClassList("hidden");
        if (tournamentAddPanel != null) tournamentAddPanel.RemoveFromClassList("hidden");
        SetActivePanel(tournamentsPanel);
    }

    private void OnAddEntrant()
    {
        if (tournamentCollection?.tournaments == null || selectedTournamentIndex < 0 || selectedTournamentIndex >= tournamentCollection.tournaments.Count) return;
        var t = tournamentCollection.tournaments[selectedTournamentIndex];
        EnsureTournamentTypeChoices();
        string type = tournamentTypeDropdown != null ? tournamentTypeDropdown.value : (t.type ?? "Singles");
        var name = tournamentEntrantDropdown != null ? (tournamentEntrantDropdown.value ?? string.Empty).Trim() : string.Empty;
        if (string.IsNullOrEmpty(name)) return;
        var entry = new TournamentEntry();
        if (string.Equals(type, "Tag Team", StringComparison.OrdinalIgnoreCase))
        {
            tagTeamCollection ??= DataManager.LoadTagTeams(currentPromotion.promotionName);
            var team = (tagTeamCollection?.teams ?? new List<TagTeamData>()).FirstOrDefault(x => string.Equals(x?.teamName, name, StringComparison.OrdinalIgnoreCase));
            entry.id = team?.id; entry.name = team?.teamName;
        }
        else
        {
            wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion.promotionName);
            var w = (wrestlerCollection?.wrestlers ?? new List<WrestlerData>()).FirstOrDefault(x => string.Equals(x?.name, name, StringComparison.OrdinalIgnoreCase));
            entry.id = w?.id; entry.name = w?.name;
        }
        if (string.IsNullOrEmpty(entry?.id) || string.IsNullOrEmpty(entry.name))
        {
            if (statusLabel != null) statusLabel.text = "Could not resolve entrant.";
            return;
        }
        t.entrants ??= new List<TournamentEntry>();
        if (t.entrants.Any(e => string.Equals(e?.id, entry.id, StringComparison.OrdinalIgnoreCase)))
        {
            if (statusLabel != null) statusLabel.text = "Entrant already added.";
            return; // no duplicates
        }
        t.entrants.Add(entry);
        DataManager.SaveTournaments(tournamentCollection);
        PopulateEntrantsUI(t);
        PopulateEntrantChoices(type);
        if (statusLabel != null) statusLabel.text = "Entrant added.";
    }

    private void OnGenerateBracket()
    {
        if (tournamentCollection?.tournaments == null || selectedTournamentIndex < 0 || selectedTournamentIndex >= tournamentCollection.tournaments.Count) return;
        var t = tournamentCollection.tournaments[selectedTournamentIndex];
        if (t.entrants == null || t.entrants.Count < 2) { if (statusLabel != null) statusLabel.text = "Add at least 2 entrants."; return; }
        var seeds = new List<TournamentEntry>(t.entrants);
        var round = new TournamentRound { roundNumber = (t.rounds?.Count ?? 0) + 1, matches = new List<TournamentMatch>() };
        for (int i = 0; i < seeds.Count; i += 2)
        {
            var m = new TournamentMatch { id = System.Guid.NewGuid().ToString("N") };
            m.participant1Id = seeds[i].id;
            m.participant2Id = (i + 1 < seeds.Count) ? seeds[i + 1].id : null; // bye if null
            if (m.participant2Id == null) m.winnerId = m.participant1Id; // automatic advance on bye
            round.matches.Add(m);
        }
        t.rounds ??= new List<TournamentRound>();
        t.rounds.Add(round);
        DataManager.SaveTournaments(tournamentCollection);
        PopulateMatchesUI(t);
        if (statusLabel != null) statusLabel.text = $"Round {round.roundNumber} generated.";
    }

    private void OnAdvanceRound()
    {
        if (tournamentCollection?.tournaments == null || selectedTournamentIndex < 0 || selectedTournamentIndex >= tournamentCollection.tournaments.Count) return;
        var t = tournamentCollection.tournaments[selectedTournamentIndex];
        if (t.rounds == null || t.rounds.Count == 0) return;
        var currentRound = t.rounds[^1];
        if (currentRound.matches.Any(m => string.IsNullOrEmpty(m.winnerId))) { if (statusLabel != null) statusLabel.text = "Select winners for all matches."; return; }
        var winners = currentRound.matches.Select(m => new TournamentEntry { id = m.winnerId, name = null }).ToList();
        if (winners.Count <= 1)
        {
            // Tournament winner decided
            DataManager.SaveTournaments(tournamentCollection);
            // Refresh UI so the winner note appears
            PopulateMatchesUI(t);
            if (statusLabel != null) statusLabel.text = "Tournament complete!";
            return;
        }
        var round = new TournamentRound { roundNumber = currentRound.roundNumber + 1, matches = new List<TournamentMatch>() };
        for (int i = 0; i < winners.Count; i += 2)
        {
            var m = new TournamentMatch { id = System.Guid.NewGuid().ToString("N") };
            m.participant1Id = winners[i].id;
            m.participant2Id = (i + 1 < winners.Count) ? winners[i + 1].id : null;
            if (m.participant2Id == null) m.winnerId = m.participant1Id;
            round.matches.Add(m);
        }
        t.rounds.Add(round);
        DataManager.SaveTournaments(tournamentCollection);
        PopulateMatchesUI(t);
        if (statusLabel != null) statusLabel.text = $"Advanced to Round {round.roundNumber}.";
    }

    private void OnRemoveEntrant()
    {
        if (tournamentCollection?.tournaments == null || selectedTournamentIndex < 0 || selectedTournamentIndex >= tournamentCollection.tournaments.Count) return;
        var t = tournamentCollection.tournaments[selectedTournamentIndex];
        if (tournamentEntrantDropdown == null) return;
        var name = (tournamentEntrantDropdown.value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(name) || t.entrants == null || t.entrants.Count == 0) return;
        int removed = t.entrants.RemoveAll(e => string.Equals(e?.name, name, StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
        {
            DataManager.SaveTournaments(tournamentCollection);
            PopulateEntrantsUI(t);
            if (statusLabel != null) statusLabel.text = "Entrant removed.";
        }
    }

    private void OnClearBracket()
    {
        if (tournamentCollection?.tournaments == null || selectedTournamentIndex < 0 || selectedTournamentIndex >= tournamentCollection.tournaments.Count) return;
        var t = tournamentCollection.tournaments[selectedTournamentIndex];
        t.rounds = new List<TournamentRound>();
        DataManager.SaveTournaments(tournamentCollection);
        PopulateMatchesUI(t);
        if (statusLabel != null) statusLabel.text = "Bracket cleared.";
    }

    private void EnsureTagTeamListView()
    {
        if (tagTeamListView != null) return;
        var parent = tagTeamListScroll != null ? tagTeamListScroll.parent : tagTeamsPanel;
        tagTeamListView = new ListView
        {
            name = "tagTeamListView",
            selectionType = SelectionType.None,
            fixedItemHeight = 36f
        };
        tagTeamListView.style.flexGrow = 1;
        tagTeamListView.makeItem = () =>
        {
            var b = new Button(); b.AddToClassList("list-entry");
            b.RegisterCallback<ClickEvent>(_ => { if (b.userData is int idx) SelectTeam(idx); });
            return b;
        };
        tagTeamListView.bindItem = (ve, i) =>
        {
            var b = (Button)ve;
            var list = tagTeamCollection?.teams;
            if (list != null && i >= 0 && i < list.Count) { var t = list[i]; b.text = $"{t.teamName} ({t.memberA} & {t.memberB})"; b.userData = i; }
            else { b.text = string.Empty; b.userData = -1; }
        };
        parent?.Add(tagTeamListView);
        if (tagTeamListScroll != null) tagTeamListScroll.style.display = DisplayStyle.None;
    }

    private void RefreshTagTeamList()
    {
        if (tagTeamListView == null) return;
        var src = tagTeamCollection?.teams ?? new List<TagTeamData>();
        tagTeamListView.itemsSource = src;
        tagTeamListView.Rebuild();
        RefreshTeamMemberChoices();
        if (tagTeamEmptyLabel != null)
        {
            if (src.Count == 0) tagTeamEmptyLabel.RemoveFromClassList("hidden");
            else tagTeamEmptyLabel.AddToClassList("hidden");
        }
    }

    private void RefreshTeamMemberChoices()
    {
        if (teamMemberADropdown == null || teamMemberBDropdown == null) return;
        wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion?.promotionName);
        var names = wrestlerCollection?.wrestlers?.Where(w => !string.IsNullOrEmpty(w?.name))?.Select(w => w.name).OrderBy(n => n).ToList() ?? new List<string>();
        if (names.Count == 0) names.Add(string.Empty);
        teamMemberADropdown.choices = names; teamMemberADropdown.value = names[0];
        teamMemberBDropdown.choices = names; teamMemberBDropdown.value = names[0];
    }

    private void SelectTeam(int index)
    {
        if (tagTeamCollection?.teams == null || index < 0 || index >= tagTeamCollection.teams.Count) return;
        selectedTeamIndex = index;
        var t = tagTeamCollection.teams[index];
        if (teamNameField != null) teamNameField.value = t.teamName;
        if (teamMemberADropdown != null) teamMemberADropdown.value = t.memberA;
        if (teamMemberBDropdown != null) teamMemberBDropdown.value = t.memberB;
        FocusPanel(tagTeamsPanel ?? titlesPanel);
    }

    private void OnAddTeam()
    {
        if (currentPromotion == null) { statusLabel.text = "No promotion loaded."; return; }
        tagTeamCollection ??= new TagTeamCollection { promotionName = currentPromotion.promotionName };
        var name = (teamNameField?.value ?? string.Empty).Trim();
        var a = (teamMemberADropdown?.value ?? string.Empty).Trim();
        var b = (teamMemberBDropdown?.value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) || string.Equals(a,b,System.StringComparison.OrdinalIgnoreCase))
        { statusLabel.text = "Enter a team name and two distinct members."; return; }
        if (tagTeamCollection.teams == null) tagTeamCollection.teams = new List<TagTeamData>();
        if (tagTeamCollection.teams.Any(t => string.Equals(t.teamName, name, System.StringComparison.OrdinalIgnoreCase)))
        { statusLabel.text = "Team name already exists."; return; }
        if (tagTeamCollection.teams.Any(t => SameMembers(t.memberA, t.memberB, a, b)))
        { statusLabel.text = "A team with these members already exists."; return; }
        tagTeamCollection.teams.Add(new TagTeamData { teamName = name, memberA = a, memberB = b, active = true });
        DataManager.SaveTagTeams(tagTeamCollection);
        RefreshTagTeamList();
        statusLabel.text = "Team added.";
    }

    private void OnSaveTeams()
    {
        if (currentPromotion == null || tagTeamCollection == null) return;
        tagTeamCollection.promotionName = currentPromotion.promotionName;
        DataManager.SaveTagTeams(tagTeamCollection);
        statusLabel.text = "Teams saved.";
    }

    private void OnSaveSelectedTeam()
    {
        if (tagTeamCollection?.teams == null || selectedTeamIndex < 0 || selectedTeamIndex >= tagTeamCollection.teams.Count) return;
        var t = tagTeamCollection.teams[selectedTeamIndex];
        var name = (teamNameField?.value ?? string.Empty).Trim();
        var a = (teamMemberADropdown?.value ?? string.Empty).Trim();
        var b = (teamMemberBDropdown?.value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) || string.Equals(a,b,System.StringComparison.OrdinalIgnoreCase))
        { statusLabel.text = "Enter a team name and two distinct members."; return; }
        if (tagTeamCollection.teams.Where((x, i) => i != selectedTeamIndex).Any(x => string.Equals(x.teamName, name, System.StringComparison.OrdinalIgnoreCase)))
        { statusLabel.text = "Team name already exists."; return; }
        if (tagTeamCollection.teams.Where((x, i) => i != selectedTeamIndex).Any(x => SameMembers(x.memberA, x.memberB, a, b)))
        { statusLabel.text = "A team with these members already exists."; return; }
        t.teamName = name; t.memberA = a; t.memberB = b;
        DataManager.SaveTagTeams(tagTeamCollection);
        RefreshTagTeamList();
        statusLabel.text = "Team updated.";
    }

    private void OnDeleteSelectedTeam()
    {
        if (tagTeamCollection?.teams == null || selectedTeamIndex < 0 || selectedTeamIndex >= tagTeamCollection.teams.Count) return;
        tagTeamCollection.teams.RemoveAt(selectedTeamIndex);
        selectedTeamIndex = -1;
        DataManager.SaveTagTeams(tagTeamCollection);
        RefreshTagTeamList();
        statusLabel.text = "Team deleted.";
    }

    private void OnCancelEditTeam()
    {
        selectedTeamIndex = -1;
        if (teamNameField != null) teamNameField.value = string.Empty;
        RefreshTeamMemberChoices();
        FocusPanel(tagTeamsPanel ?? titlesPanel);
    }

    private static bool SameMembers(string a1, string b1, string a2, string b2)
    {
        string x1 = (a1 ?? string.Empty).Trim();
        string y1 = (b1 ?? string.Empty).Trim();
        string x2 = (a2 ?? string.Empty).Trim();
        string y2 = (b2 ?? string.Empty).Trim();
        bool m1 = string.Equals(x1, x2, System.StringComparison.OrdinalIgnoreCase) && string.Equals(y1, y2, System.StringComparison.OrdinalIgnoreCase);
        bool m2 = string.Equals(x1, y2, System.StringComparison.OrdinalIgnoreCase) && string.Equals(y1, x2, System.StringComparison.OrdinalIgnoreCase);
        return m1 || m2;
    }

    private void RefreshTitleList()
    {
        if (titleListView == null) return;
        var src = titleCollection?.titles ?? new List<TitleData>();
        titleListView.itemsSource = src;
        titleListView.Rebuild();
    }

    private void SelectTitle(int index)
    {
        if (titleCollection == null || index < 0 || index >= (titleCollection.titles?.Count ?? 0)) return;
        selectedTitleIndex = index;
        var t = titleCollection.titles[index];
        if (titleNameField != null) titleNameField.value = t.titleName;
        if (titleDivisionField != null) titleDivisionField.value = t.division;
        if (titleChampionField != null) titleChampionField.value = t.currentChampion;
        if (titleNotesField != null) titleNotesField.value = t.notes;
        // Keep stats out of the edit panel
        titleStatsPanel?.AddToClassList("hidden");
        titleAddPanel?.AddToClassList("hidden");
        titleDetailsPanel?.RemoveFromClassList("hidden");
        titleHistoryList?.AddToClassList("hidden");
        FocusPanel(titleDetailsPanel ?? titlesPanel);
    }

    private void UpdateTitleStats()
    {
        if (currentPromotion == null || selectedTitleIndex < 0 || titleCollection == null || selectedTitleIndex >= (titleCollection.titles?.Count ?? 0))
        {
            if (titleStatsCurrentLabel != null) titleStatsCurrentLabel.text = string.Empty;
            if (titleStatsSummaryLabel != null) titleStatsSummaryLabel.text = "No stats yet.";
            if (titleStatsLongestLabel != null) titleStatsLongestLabel.text = string.Empty;
            if (titleStatsShortestLabel != null) titleStatsShortestLabel.text = string.Empty;
            if (titleStatsMostDefensesLabel != null) titleStatsMostDefensesLabel.text = string.Empty;
            return;
        }
        var t = titleCollection.titles[selectedTitleIndex];
        var summaries = TitleHistoryManager.GetTitleReignSummaries(currentPromotion.promotionName, t.titleName) ?? new List<TitleReignSummary>();
        int totalReigns = summaries.Count;
        int totalDefenses = summaries.Sum(s => s.defenses);
        var current = summaries.FirstOrDefault(s => string.IsNullOrEmpty(s.dateLost));
        if (titleStatsCurrentLabel != null)
            titleStatsCurrentLabel.text = current != null ? $"Current champion: {current.championName} â€” {current.daysHeld} days" : "Current champion: (unknown)";
        if (titleStatsSummaryLabel != null)
            titleStatsSummaryLabel.text = totalReigns > 0 ? $"Reigns: {totalReigns}   Total defenses: {totalDefenses}" : "No stats yet.";
        var longest = summaries.OrderByDescending(s => s.daysHeld).FirstOrDefault();
        var shortest = summaries.OrderBy(s => s.daysHeld).FirstOrDefault();
        if (titleStatsLongestLabel != null)
        {
            titleStatsLongestLabel.text = longest != null ?
                $"Longest: {longest.championName} â€” {longest.daysHeld} days ({SpanText(longest)})" : string.Empty;
        }
        if (titleStatsShortestLabel != null)
        {
            titleStatsShortestLabel.text = shortest != null ?
                $"Shortest: {shortest.championName} â€” {shortest.daysHeld} days ({SpanText(shortest)})" : string.Empty;
        }
        var mostDef = summaries.OrderByDescending(s => s.defenses).FirstOrDefault();
        if (titleStatsMostDefensesLabel != null)
        {
            titleStatsMostDefensesLabel.text = mostDef != null ?
                $"Most defenses in a reign: {mostDef.championName} â€” {mostDef.defenses}" : string.Empty;
        }

        string SpanText(TitleReignSummary s)
        {
            return string.IsNullOrEmpty(s.dateLost) ? $"{s.dateWon} â€“ present" : $"{s.dateWon} â€“ {s.dateLost}";
        }
    }

    private void OnAddTitle()
    {
        if (currentPromotion == null) { if (statusLabel != null) statusLabel.text = "No promotion loaded."; return; }
        var name = newTitleField != null ? (newTitleField.value ?? string.Empty).Trim() : string.Empty;
        if (string.IsNullOrEmpty(name)) { if (statusLabel != null) statusLabel.text = "Enter a title name."; return; }

        titleCollection ??= DataManager.LoadTitles(currentPromotion.promotionName) ?? new TitleCollection { promotionName = currentPromotion.promotionName };
        if (titleCollection.promotionName != currentPromotion.promotionName)
            titleCollection.promotionName = currentPromotion.promotionName;

        // Prevent duplicate title names (case-insensitive)
        if (titleCollection.titles != null && titleCollection.titles.Any(t => !string.IsNullOrEmpty(t?.titleName) && string.Equals(t.titleName.Trim(), name, System.StringComparison.OrdinalIgnoreCase)))
        {
            if (statusLabel != null) statusLabel.text = "Title already exists.";
            return;
        }

        var tNew = new TitleData { titleName = name };
        titleCollection.titles ??= new List<TitleData>();
        titleCollection.titles.Add(tNew);

        DataManager.SaveTitles(titleCollection);
        RefreshTitleList();
        if (newTitleField != null) newTitleField.value = string.Empty;
        if (statusLabel != null) statusLabel.text = "Title added.";

        // Auto-select the new title for editing
        int idx = titleCollection.titles.FindIndex(t => string.Equals(t.titleName, name, System.StringComparison.OrdinalIgnoreCase));
        if (idx >= 0) SelectTitle(idx);
    }

    private void OnSaveTitles()
    {
        if (currentPromotion == null || titleCollection == null) return;
        titleCollection.promotionName = currentPromotion.promotionName;
        DataManager.SaveTitles(titleCollection);
        if (statusLabel != null) statusLabel.text = "Titles saved.";
    }

    private void OnSaveSelectedTitle()
    {
        if (titleCollection?.titles == null || selectedTitleIndex < 0 || selectedTitleIndex >= titleCollection.titles.Count) return;
        var t = titleCollection.titles[selectedTitleIndex];
        if (titleNameField != null) t.titleName = titleNameField.value;
        if (titleDivisionField != null) t.division = titleDivisionField.value;
        if (titleChampionField != null) t.currentChampion = titleChampionField.value;
        if (titleNotesField != null) t.notes = titleNotesField.value;
        DataManager.SaveTitles(titleCollection);
        RefreshTitleList();
        if (statusLabel != null) statusLabel.text = "Title updated.";
    }

    private void OnDeleteSelectedTitle()
    {
        if (titleCollection?.titles == null || selectedTitleIndex < 0 || selectedTitleIndex >= titleCollection.titles.Count) return;
        titleCollection.titles.RemoveAt(selectedTitleIndex);
        selectedTitleIndex = -1;
        DataManager.SaveTitles(titleCollection);
        RefreshTitleList();
        titleDetailsPanel?.AddToClassList("hidden");
        titleAddPanel?.RemoveFromClassList("hidden");
        if (statusLabel != null) statusLabel.text = "Title deleted.";
    }

    private void OnCancelEditTitle()
    {
        titleDetailsPanel?.AddToClassList("hidden");
        titleAddPanel?.RemoveFromClassList("hidden");
        selectedTitleIndex = -1;
        FocusPanel(titlesPanel);
    }

    private void ShowSelectedTitleHistory()
    {
        if (titleHistoryList == null) return;
        if (currentPromotion == null)
        {
            if (statusLabel != null) statusLabel.text = "No promotion loaded!";
            return;
        }
        if (selectedTitleIndex < 0 || titleCollection == null || selectedTitleIndex >= (titleCollection.titles?.Count ?? 0))
        {
            if (statusLabel != null) statusLabel.text = "Select a title to view its history.";
            return;
        }
        var selectedTitle = titleCollection.titles[selectedTitleIndex];
        var history = TitleHistoryManager.GetHistory(currentPromotion.promotionName, selectedTitle.titleName);
        var summaries = TitleHistoryManager.GetTitleReignSummaries(currentPromotion.promotionName, selectedTitle.titleName) ?? new List<TitleReignSummary>();
        titleHistoryList.Clear();

        // Build lineage with current champion first
        var ordered = summaries
            .OrderByDescending(s => string.IsNullOrEmpty(s.dateLost)) // true first
            .ThenByDescending(s => ParseDateSafe(s.dateLost, s.dateWon));

        titleHistoryList.Add(new Label("Title Lineage:") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 6 } });
        foreach (var s in ordered)
        {
            string span = string.IsNullOrEmpty(s.dateLost) ? $"{s.dateWon} - present" : $"{s.dateWon} - {s.dateLost}";
            var row = new VisualElement(); row.style.marginBottom = 6;
            row.Add(new Label($"{s.championName} ({span})"));
            titleHistoryList.Add(row);
        }
        // Controls row: View Stats + toggle match history
        var controlsRow = new VisualElement();
        controlsRow.style.flexDirection = FlexDirection.Row;
        var statsBtn = new Button(() => ShowTitleStatsPanel(selectedTitle.titleName)) { text = "View Stats" };
        bool showHistory = GetTitleHistoryToggle(currentPromotion.promotionName, selectedTitle.titleName);
        var toggleBtn = new Button(() => { var nv = !GetTitleHistoryToggle(currentPromotion.promotionName, selectedTitle.titleName); SetTitleHistoryToggle(currentPromotion.promotionName, selectedTitle.titleName, nv); ShowSelectedTitleHistory(); }) { text = showHistory ? "Hide Match History" : "Show Match History" };
        controlsRow.Add(statsBtn);
        controlsRow.Add(new VisualElement() { style = { width = 8 } });
        controlsRow.Add(toggleBtn);
        titleHistoryList.Add(new VisualElement() { style = { height = 6 } });
        titleHistoryList.Add(controlsRow);
        titleHistoryList.Add(new VisualElement() { style = { height = 8 } });

        // Optional: Match history below lineage
        if (showHistory && history != null && history.Count > 0)
        {
            titleHistoryList.Add(new Label("Match History:") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } });
            foreach (var entry in history)
            {
                var el = new VisualElement(); el.style.marginBottom = 6;
                el.Add(new Label($"{entry.date} - {entry.matchName}"));
                if (!string.IsNullOrEmpty(entry.winner)) el.Add(new Label($"Winner: {entry.winner}"));
                titleHistoryList.Add(el);
            }
        }

        bool GetTitleHistoryToggle(string promo, string title)
        {
            string key = MakeTitleToggleKey(promo, title);
            if (titleHistoryToggleByTitle.TryGetValue(key, out var val)) return val;
            int stored = PlayerPrefs.GetInt(key, 0);
            bool b = stored == 1;
            titleHistoryToggleByTitle[key] = b;
            return b;
        }
        if (history == null || history.Count == 0)
        {
            titleHistoryList.Add(new Label("No title history recorded yet.") { style = { color = new StyleColor(new Color(0.7f,0.7f,0.7f)) } });
        }
        else
        {
            foreach (var entry in history)
            {
                var el = new VisualElement(); el.style.marginBottom = 6;
                el.Add(new Label($"{entry.date} - {entry.matchName}"));
                if (!string.IsNullOrEmpty(entry.winner)) el.Add(new Label($"Winner: {entry.winner}"));
                titleHistoryList.Add(el);
            }
        }
        titleDetailsPanel?.AddToClassList("hidden");
        titleAddPanel?.AddToClassList("hidden");
        titleStatsView?.AddToClassList("hidden");
        titleHistoryList.RemoveFromClassList("hidden");
        FocusPanel(titleHistoryList);
    }

    private static DateTime ParseDateSafe(string dateLost, string fallbackStart)
    {
        if (!string.IsNullOrEmpty(dateLost) && DateTime.TryParse(dateLost, out var d1)) return d1;
        if (!string.IsNullOrEmpty(fallbackStart) && DateTime.TryParse(fallbackStart, out var d2)) return d2;
        return DateTime.MinValue;
    }

    private void ShowTitleStatsPanel(string titleName)
    {
        if (titleStatsList == null || currentPromotion == null) return;
        var summaries = TitleHistoryManager.GetTitleReignSummaries(currentPromotion.promotionName, titleName) ?? new List<TitleReignSummary>();
        titleStatsList.Clear();
        // Aggregated stats
        int totalReigns = summaries.Count;
        int totalDefenses = summaries.Sum(s => s.defenses);
        var current = summaries.FirstOrDefault(s => string.IsNullOrEmpty(s.dateLost));
        var longest = summaries.OrderByDescending(s => s.daysHeld).FirstOrDefault();
        var shortest = summaries.OrderBy(s => s.daysHeld).FirstOrDefault();
        var mostDef = summaries.OrderByDescending(s => s.defenses).FirstOrDefault();
        titleStatsList.Add(new Label(current != null ? $"Current champion: {current.championName} â€” {current.daysHeld} days" : "Current champion: (unknown)"));
        titleStatsList.Add(new Label(totalReigns > 0 ? $"Reigns: {totalReigns}   Total defenses: {totalDefenses}" : "No stats yet."));
        if (longest != null) titleStatsList.Add(new Label($"Longest: {longest.championName} â€” {longest.daysHeld} days ({(string.IsNullOrEmpty(longest.dateLost) ? $"{longest.dateWon} â€“ present" : $"{longest.dateWon} â€“ {longest.dateLost}")})"));
        if (shortest != null) titleStatsList.Add(new Label($"Shortest: {shortest.championName} â€” {shortest.daysHeld} days ({(string.IsNullOrEmpty(shortest.dateLost) ? $"{shortest.dateWon} â€“ present" : $"{shortest.dateWon} â€“ {shortest.dateLost}")})"));
        if (mostDef != null) titleStatsList.Add(new Label($"Most defenses in a reign: {mostDef.championName} â€” {mostDef.defenses}"));
        titleStatsList.Add(new VisualElement() { style = { height = 8 } });
        // Reign summaries
        if (summaries.Count > 0)
        {
            titleStatsList.Add(new Label("Reign Summaries:") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 6 } });
            foreach (var s in summaries)
            {
                var row = new VisualElement(); row.style.marginBottom = 6;
                string span = string.IsNullOrEmpty(s.dateLost) ? $"{s.dateWon} - present" : $"{s.dateWon} - {s.dateLost}";
                row.Add(new Label($"{s.championName} ({span}) â€¢ {s.daysHeld} days, {s.defenses} defenses"));
                if (s.defenses > 0)
                {
                    row.Add(new Label($"First defense: {s.firstDefenseDate}  Last defense: {s.lastDefenseDate}"));
                }
                titleStatsList.Add(row);
            }
        }
        // Swap panels
        titleHistoryList?.AddToClassList("hidden");
        titleStatsView?.RemoveFromClassList("hidden");
        FocusPanel(titleStatsView);
    }

    private static string MakeTitleToggleKey(string promo, string title)
        => $"TitleHistoryToggle::{promo}::{title}";

    private void SetTitleHistoryToggle(string promo, string title, bool value)
    {
        string key = MakeTitleToggleKey(promo, title);
        titleHistoryToggleByTitle[key] = value;
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void EnsureShowsListView()
    {
        if (showsListView != null) return;
        var parent = showsListScroll != null ? showsListScroll.parent : showsPanel;
        showsListView = new ListView
        {
            name = "showsListView",
            selectionType = SelectionType.None,
            fixedItemHeight = 36f
        };
        showsListView.style.flexGrow = 1;
        showsListView.makeItem = () =>
        {
            var b = new Button();
            b.AddToClassList("list-entry");
            b.RegisterCallback<ClickEvent>(_ =>
            {
                if (b.userData is int idx) SelectShow(idx);
            });
            return b;
        };
        showsListView.bindItem = (ve, i) =>
        {
            var b = (Button)ve;
            var shows = currentPromotion?.shows;
            if (shows != null && i >= 0 && i < shows.Count)
            {
                var s = shows[i];
                b.text = string.IsNullOrEmpty(s?.date) ? s?.showName : $"{s?.showName} - {s?.date}";
                b.userData = i;
            }
            else { b.text = string.Empty; b.userData = -1; }
        };
        parent?.Add(showsListView);
        if (showsListScroll != null) showsListScroll.style.display = DisplayStyle.None;
    }

    private void RefreshShowList()
    {
        if (showsListView == null) return;
        var shows = currentPromotion?.shows ?? new List<ShowData>();
        showsListView.itemsSource = shows;
        showsListView.Rebuild();
    }

    private void OnAddShow()
    {
        if (currentPromotion == null)
        {
            if (statusLabel != null) statusLabel.text = "No promotion loaded.";
            return;
        }
        string name = newShowField != null ? (newShowField.value ?? string.Empty).Trim() : string.Empty;
        string date = newShowDateField != null ? (newShowDateField.value ?? string.Empty).Trim() : string.Empty;
        date = NormalizeDateString(date);
        if (string.IsNullOrEmpty(name))
        {
            if (statusLabel != null) statusLabel.text = "Enter a show name.";
            return;
        }
        currentPromotion.shows ??= new List<ShowData>();
        // prevent exact duplicate (name+date)
        if (currentPromotion.shows.Any(s => string.Equals(s?.showName ?? string.Empty, name, System.StringComparison.OrdinalIgnoreCase)
                                          && string.Equals((s?.date ?? string.Empty).Trim(), date, System.StringComparison.OrdinalIgnoreCase)))
        {
            if (statusLabel != null) statusLabel.text = "Show already exists.";
            return;
        }
        var show = new ShowData(name, date);
        if (newShowVenueField != null) show.venue = (newShowVenueField.value ?? string.Empty).Trim();
        if (newShowCityField != null) show.city = (newShowCityField.value ?? string.Empty).Trim();
        if (newShowAttendanceField != null) show.attendance = newShowAttendanceField.value;
        if (newShowRatingField != null) show.rating = newShowRatingField.value;
        currentPromotion.shows.Add(show);
        DataManager.SavePromotion(currentPromotion);
        RefreshShowList();
        if (newShowField != null) newShowField.value = string.Empty;
        if (newShowDateField != null) newShowDateField.value = string.Empty;
        if (newShowVenueField != null) newShowVenueField.value = string.Empty;
        if (newShowCityField != null) newShowCityField.value = string.Empty;
        if (newShowAttendanceField != null) newShowAttendanceField.value = 0;
        if (newShowRatingField != null) newShowRatingField.value = 0f;
        if (statusLabel != null) statusLabel.text = "Show added.";
    }

    private void OnSaveShows()
    {
        if (currentPromotion == null) return;
        DataManager.SavePromotion(currentPromotion);
        if (statusLabel != null) statusLabel.text = "Shows saved.";
    }

    private void SelectShow(int index)
    {
        if (currentPromotion?.shows == null || index < 0 || index >= currentPromotion.shows.Count) return;
        selectedShowIndex = index;
        var s = currentPromotion.shows[index];
        showDetailsPanel?.RemoveFromClassList("hidden");
        matchesView?.AddToClassList("hidden");
        showAddPanel?.AddToClassList("hidden");
        matchEditor?.AddToClassList("hidden");
        segmentEditor?.AddToClassList("hidden");
        if (showNameField != null) showNameField.value = s.showName;
        if (showDateField != null) showDateField.value = s.date;
        if (showVenueField != null) showVenueField.value = s.venue;
        if (showCityField != null) showCityField.value = s.city;
        if (showAttendanceField != null) showAttendanceField.value = s.attendance;
        if (showRatingField != null) showRatingField.value = s.rating;
        FocusPanel(showDetailsPanel ?? showsPanel);
    }

    private void OnSaveSelectedShow()
    {
        if (currentPromotion?.shows == null || selectedShowIndex < 0 || selectedShowIndex >= currentPromotion.shows.Count) return;
        var s = currentPromotion.shows[selectedShowIndex];
        string prevName = s.showName;
        string prevDate = s.date;
        if (showNameField != null) s.showName = showNameField.value;
        if (showDateField != null) { s.date = NormalizeDateString(showDateField.value); showDateField.value = s.date; }
        if (showVenueField != null) s.venue = showVenueField.value;
        if (showCityField != null) s.city = showCityField.value;
        if (showAttendanceField != null) s.attendance = showAttendanceField.value;
        if (showRatingField != null) s.rating = showRatingField.value;
        DataManager.SavePromotion(currentPromotion);
        TitleHistoryManager.UpdateShowResults(currentPromotion, s, prevName, prevDate);
        RefreshShowList();
        if (statusLabel != null) statusLabel.text = "Show updated.";
    }

    private void OnDeleteSelectedShow()
    {
        if (currentPromotion?.shows == null || selectedShowIndex < 0 || selectedShowIndex >= currentPromotion.shows.Count) return;
        var removed = currentPromotion.shows[selectedShowIndex];
        currentPromotion.shows.RemoveAt(selectedShowIndex);
        selectedShowIndex = -1;
        DataManager.SavePromotion(currentPromotion);
        TitleHistoryManager.RemoveShow(currentPromotion, removed);
        RefreshShowList();
        showDetailsPanel?.AddToClassList("hidden");
        showAddPanel?.RemoveFromClassList("hidden");
        if (statusLabel != null) statusLabel.text = "Show deleted.";
    }

    private void OnCancelEditShow()
    {
        showDetailsPanel?.AddToClassList("hidden");
        matchesView?.AddToClassList("hidden");
        selectedShowIndex = -1;
        showAddPanel?.RemoveFromClassList("hidden");
        FocusPanel(showsPanel);
    }

    private void ShowMatchEditor()
    {
        if (selectedShowIndex < 0 || currentPromotion?.shows == null || selectedShowIndex >= currentPromotion.shows.Count) return;
        EnsureDefaultMatchTypes();
        EnsureRosterAndTitlesLoaded();
        var names = wrestlerCollection?.wrestlers?.Where(w => !string.IsNullOrEmpty(w?.name))?.Select(w => w.name).OrderBy(n => n).ToList() ?? new List<string>();
        if (names.Count == 0) names.Add(string.Empty);
        SetChoices(wrestlerADropdown, names);
        SetChoices(wrestlerBDropdown, names);
        var optNames = new List<string>(names); if (!optNames.Contains(string.Empty)) optNames.Insert(0, string.Empty);
        SetChoices(wrestlerCDropdown, optNames);
        SetChoices(wrestlerDDropdown, optNames);
        var titleNames = titleCollection?.titles?.Where(t => !string.IsNullOrEmpty(t?.titleName))?.Select(t => t.titleName).OrderBy(n => n).ToList() ?? new List<string>();
        if (!titleNames.Contains(string.Empty)) titleNames.Insert(0, string.Empty);
        SetChoices(titleDropdown, titleNames);
        if (isTitleMatchToggle != null) isTitleMatchToggle.value = false;
        if (titleDropdown != null) titleDropdown.SetEnabled(false);
        UpdateWinnerChoices();
        matchEditor?.RemoveFromClassList("hidden");
        segmentEditor?.AddToClassList("hidden");
        // Hide redundant panels while editing
        showDetailsPanel?.AddToClassList("hidden");
        showsListView?.AddToClassList("hidden");
        showAddPanel?.AddToClassList("hidden");
        matchesView?.AddToClassList("hidden");
        FocusPanel(matchEditor ?? showDetailsPanel ?? showsPanel);
        RegisterWinnerAutoUpdate(wrestlerADropdown);
        RegisterWinnerAutoUpdate(wrestlerBDropdown);
        RegisterWinnerAutoUpdate(wrestlerCDropdown);
        RegisterWinnerAutoUpdate(wrestlerDDropdown);
        if (isTitleMatchToggle != null)
        {
            isTitleMatchToggle.RegisterValueChangedCallback(evt =>
            {
                titleDropdown?.SetEnabled(evt.newValue);
            });
        }
    }

    private void ShowSegmentEditor()
    {
        if (selectedShowIndex < 0 || currentPromotion?.shows == null || selectedShowIndex >= currentPromotion.shows.Count) return;
        if (segmentNameField != null) segmentNameField.value = string.Empty;
        if (segmentTextField != null) segmentTextField.value = string.Empty;
        segmentEditor?.RemoveFromClassList("hidden");
        matchEditor?.AddToClassList("hidden");
        // Hide redundant panels while editing
        showDetailsPanel?.AddToClassList("hidden");
        showsListView?.AddToClassList("hidden");
        showAddPanel?.AddToClassList("hidden");
        matchesView?.AddToClassList("hidden");
        FocusPanel(segmentEditor ?? showDetailsPanel ?? showsPanel);
    }

    private void SaveMatch()
    {
        if (selectedShowIndex < 0 || currentPromotion?.shows == null || selectedShowIndex >= currentPromotion.shows.Count) return;
        var A = wrestlerADropdown?.value?.Trim();
        var B = wrestlerBDropdown?.value?.Trim();
        var C = wrestlerCDropdown?.value?.Trim();
        var D = wrestlerDDropdown?.value?.Trim();
        var have = new List<string>();
        if (!string.IsNullOrEmpty(A)) have.Add(A);
        if (!string.IsNullOrEmpty(B)) have.Add(B);
        if (!string.IsNullOrEmpty(C)) have.Add(C);
        if (!string.IsNullOrEmpty(D)) have.Add(D);
        if (have.Count < 2) { if (statusLabel != null) statusLabel.text = "Select at least two wrestlers."; return; }
        string type = (matchTypeDropdown != null && !string.IsNullOrEmpty(matchTypeDropdown.value)) ? matchTypeDropdown.value : "Match";
        string matchName = $"{type}: {string.Join(" vs ", have)}";
        string winner = winnerDropdown != null ? (winnerDropdown.value ?? string.Empty).Trim() : string.Empty;
        // Build id maps
        EnsureRosterAndTitlesLoaded();
        var idByName = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var w in wrestlerCollection?.wrestlers ?? new List<WrestlerData>())
            if (!string.IsNullOrEmpty(w?.name) && !string.IsNullOrEmpty(w.id)) idByName[w.name.Trim()] = w.id;
        var titleIdByName = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var t in titleCollection?.titles ?? new List<TitleData>())
            if (!string.IsNullOrEmpty(t?.titleName) && !string.IsNullOrEmpty(t.id)) titleIdByName[t.titleName.Trim()] = t.id;

        string GetId(string nm) { return (!string.IsNullOrEmpty(nm) && idByName.TryGetValue(nm.Trim(), out var v)) ? v : null; }
        string GetTitleId(string nm) { return (!string.IsNullOrEmpty(nm) && titleIdByName.TryGetValue(nm.Trim(), out var v)) ? v : null; }

        var m = new MatchData
        {
            id = System.Guid.NewGuid().ToString("N"),
            matchType = (matchTypeDropdown != null && !string.IsNullOrEmpty(matchTypeDropdown.value)) ? matchTypeDropdown.value : "Match",
            matchName = matchName,
            wrestlerA = A,
            wrestlerB = B,
            wrestlerC = C,
            wrestlerD = D,
            isTitleMatch = isTitleMatchToggle != null && isTitleMatchToggle.value,
            titleName = (isTitleMatchToggle != null && isTitleMatchToggle.value && titleDropdown != null) ? titleDropdown.value : null,
            winner = winner,
            wrestlerAId = GetId(A),
            wrestlerBId = GetId(B),
            wrestlerCId = GetId(C),
            wrestlerDId = GetId(D),
            winnerId = GetId(winner),
            titleId = (isTitleMatchToggle != null && isTitleMatchToggle.value && titleDropdown != null) ? GetTitleId(titleDropdown.value) : null
        };
        var show = currentPromotion.shows[selectedShowIndex];
        show.matches ??= new List<MatchData>();
        show.matches.Add(m);
        show.entryOrder ??= new List<string>();
        show.entryOrder.Add($"M:{m.id}");
        DataManager.SavePromotion(currentPromotion);
        TitleHistoryManager.UpdateShowResults(currentPromotion, show);
        if (statusLabel != null) statusLabel.text = "Match added.";
        matchEditor?.AddToClassList("hidden");
        // Restore panels
        showDetailsPanel?.RemoveFromClassList("hidden");
        showsListView?.RemoveFromClassList("hidden");
        FocusPanel(showDetailsPanel ?? showsPanel);
        PopulateHistoryShowsList();
        RefreshShowList();
    }

    private void SaveSegment()
    {
        if (selectedShowIndex < 0 || currentPromotion?.shows == null || selectedShowIndex >= currentPromotion.shows.Count) return;
        var name = segmentNameField != null ? (segmentNameField.value ?? string.Empty).Trim() : string.Empty;
        var text = segmentTextField != null ? (segmentTextField.value ?? string.Empty).Trim() : string.Empty;
        if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(text)) { if (statusLabel != null) statusLabel.text = "Enter a segment name or text."; return; }
        var s = new SegmentData { id = System.Guid.NewGuid().ToString("N"), name = name, text = text };
        var show = currentPromotion.shows[selectedShowIndex];
        show.segments ??= new List<SegmentData>();
        show.segments.Add(s);
        show.entryOrder ??= new List<string>();
        show.entryOrder.Add($"S:{s.id}");
        DataManager.SavePromotion(currentPromotion);
        TitleHistoryManager.UpdateShowResults(currentPromotion, show);
        if (statusLabel != null) statusLabel.text = "Segment added.";
        segmentEditor?.AddToClassList("hidden");
        // Restore panels
        showDetailsPanel?.RemoveFromClassList("hidden");
        showsListView?.RemoveFromClassList("hidden");
        FocusPanel(showDetailsPanel ?? showsPanel);
        PopulateHistoryShowsList();
        RefreshShowList();
    }

    private void RegisterWinnerAutoUpdate(DropdownField field)
    {
        if (field == null) return;
        field.RegisterValueChangedCallback(_ => UpdateWinnerChoices());
    }

    private void UpdateWinnerChoices()
    {
        if (winnerDropdown == null) return;
        var names = new List<string>();
        void add(string v) { if (!string.IsNullOrEmpty(v) && !names.Contains(v)) names.Add(v); }
        add(wrestlerADropdown?.value);
        add(wrestlerBDropdown?.value);
        add(wrestlerCDropdown?.value);
        add(wrestlerDDropdown?.value);
        if (names.Count == 0) names.Add(string.Empty);
        SetChoices(winnerDropdown, names);
    }

    private void SetChoices(DropdownField dropdown, List<string> choices)
    {
        if (dropdown == null) return;
        dropdown.choices = choices ?? new List<string>();
        dropdown.value = dropdown.choices.Count > 0 ? dropdown.choices[0] : string.Empty;
    }

    private void EnsureRosterAndTitlesLoaded()
    {
        if (currentPromotion == null) return;
        wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion.promotionName);
        titleCollection ??= DataManager.LoadTitles(currentPromotion.promotionName);
    }

    // ===== Date Picker helpers =====
    private void NormalizeDateField(TextField tf)
    {
        if (tf == null) return;
        tf.value = NormalizeDateString(tf.value);
    }

    private string NormalizeDateString(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        DateTime dt;
        if (DateTime.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.None, out dt) ||
            DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt) ||
            DateTime.TryParseExact(input, new[] { "M/d/yyyy", "MM/dd/yyyy", "yyyy-MM-dd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
        {
            return dt.ToString(DateFormat, CultureInfo.InvariantCulture);
        }
        return input.Trim();
    }

    private void OpenDatePicker(TextField target)
    {
        if (target == null || root == null) return;
        activeDateField = target;
        DateTime init;
        if (!DateTime.TryParseExact(target.value ?? string.Empty, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out init))
            init = DateTime.Today;
        datePickerMonth = new DateTime(init.Year, init.Month, 1);

        if (datePickerOverlay == null)
            CreateDatePickerUI();

        BuildCalendarGrid(datePickerMonth);
        datePickerOverlay.RemoveFromClassList("hidden");
        datePickerPopup.RemoveFromClassList("hidden");
        PositionDatePickerNear(target);
        FocusPanel(datePickerPopup);
    }

    private void CreateDatePickerUI()
    {
        datePickerOverlay = new VisualElement { name = "datePickerOverlay" };
        datePickerOverlay.style.position = Position.Absolute;
        datePickerOverlay.style.left = 0; datePickerOverlay.style.top = 0;
        datePickerOverlay.style.right = 0; datePickerOverlay.style.bottom = 0;
        datePickerOverlay.style.backgroundColor = new Color(0, 0, 0, 0.3f);
        root.Add(datePickerOverlay);
        datePickerOverlay.AddToClassList("hidden");
        datePickerOverlay.RegisterCallback<MouseDownEvent>(_ => CloseDatePicker());

        datePickerPopup = new VisualElement { name = "datePickerPopup" };
        datePickerPopup.style.position = Position.Absolute;
        // Position is set dynamically near the target field when opened
        datePickerPopup.style.left = 0;
        datePickerPopup.style.top = 0;
        datePickerPopup.style.translate = new Translate(0, 0, 0);
        datePickerPopup.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.98f);
        datePickerPopup.style.paddingLeft = 10; datePickerPopup.style.paddingRight = 10;
        datePickerPopup.style.paddingTop = 8; datePickerPopup.style.paddingBottom = 10;
        datePickerPopup.style.borderTopLeftRadius = 8; datePickerPopup.style.borderTopRightRadius = 8;
        datePickerPopup.style.borderBottomLeftRadius = 8; datePickerPopup.style.borderBottomRightRadius = 8;
        datePickerOverlay.Add(datePickerPopup);
        datePickerPopup.AddToClassList("hidden");
        datePickerPopup.RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation());

        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row; header.style.alignItems = Align.Center; header.style.justifyContent = Justify.SpaceBetween; header.style.marginBottom = 6;
        datePrevButton = new Button(() => { datePickerMonth = datePickerMonth.AddMonths(-1); BuildCalendarGrid(datePickerMonth); }) { text = "<" };
        dateNextButton = new Button(() => { datePickerMonth = datePickerMonth.AddMonths(1); BuildCalendarGrid(datePickerMonth); }) { text = ">" };
        datePickerMonthLabel = new Label("");
        datePickerMonthLabel.style.unityFontStyleAndWeight = FontStyle.Bold; datePickerMonthLabel.style.fontSize = 14; datePickerMonthLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        header.Add(datePrevButton);
        header.Add(datePickerMonthLabel);
        header.Add(dateNextButton);
        datePickerPopup.Add(header);

        var dow = new VisualElement();
        dow.style.flexDirection = FlexDirection.Row; dow.style.marginBottom = 4;
        foreach (var d in new[] { "Su","Mo","Tu","We","Th","Fr","Sa" })
        {
            var l = new Label(d);
            l.style.width = 28; l.style.unityTextAlign = TextAnchor.MiddleCenter; l.style.color = new StyleColor(new Color(0.85f,0.85f,0.85f));
            dow.Add(l);
        }
        datePickerPopup.Add(dow);

        datePickerGrid = new VisualElement();
        datePickerGrid.style.flexDirection = FlexDirection.Column;
        datePickerPopup.Add(datePickerGrid);

        var closeRow = new VisualElement();
        closeRow.style.flexDirection = FlexDirection.Row; closeRow.style.justifyContent = Justify.Center; closeRow.style.marginTop = 8;
        var closeBtn = new Button(CloseDatePicker) { text = "Cancel" };
        closeRow.Add(closeBtn);
        datePickerPopup.Add(closeRow);
    }

    private void BuildCalendarGrid(DateTime month)
    {
        if (datePickerGrid == null) return;
        datePickerGrid.Clear();
        datePickerMonthLabel.text = month.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

        int daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
        int startOffset = (int)new DateTime(month.Year, month.Month, 1).DayOfWeek; // 0 = Sunday
        int day = 1;
        for (int row = 0; row < 6; row++)
        {
            var rowEl = new VisualElement();
            rowEl.style.flexDirection = FlexDirection.Row;
            for (int col = 0; col < 7; col++)
            {
                var cell = new Button();
                cell.style.width = 28; cell.style.height = 24; cell.style.marginRight = 2; cell.style.marginBottom = 2;
                if ((row == 0 && col < startOffset) || day > daysInMonth)
                {
                    cell.text = string.Empty; cell.SetEnabled(false);
                }
                else
                {
                    int d = day;
                    cell.text = d.ToString();
                    cell.clicked += () => SelectDate(new DateTime(month.Year, month.Month, d));
                    day++;
                }
                rowEl.Add(cell);
            }
            datePickerGrid.Add(rowEl);
        }
    }

    private void SelectDate(DateTime date)
    {
        if (activeDateField != null)
        {
            var s = date.ToString(DateFormat, CultureInfo.InvariantCulture);
            activeDateField.value = s;
        }
        CloseDatePicker();
    }

    private void CloseDatePicker()
    {
        datePickerPopup?.AddToClassList("hidden");
        datePickerOverlay?.AddToClassList("hidden");
        activeDateField = null;
    }

    private void PositionDatePickerNear(VisualElement field)
    {
        if (field == null || datePickerPopup == null || root == null) return;
        var wb = field.worldBound;
        var rootWB = root.worldBound;
        float desiredX = wb.xMin;
        float desiredY = wb.yMax + 4f;

        // Initial placement; refine after layout to keep within bounds
        datePickerPopup.style.left = desiredX;
        datePickerPopup.style.top = desiredY;

        datePickerPopup.schedule.Execute(() =>
        {
            var popupWB = datePickerPopup.worldBound;
            float maxX = rootWB.xMax - popupWB.width - 8f;
            float maxY = rootWB.yMax - popupWB.height - 8f;
            float minX = rootWB.xMin + 8f;
            float minY = rootWB.yMin + 8f;
            float x = desiredX, y = desiredY;
            if (x > maxX) x = maxX;
            if (y > maxY) y = maxY;
            if (x < minX) x = minX;
            if (y < minY) y = minY;
            datePickerPopup.style.left = x;
            datePickerPopup.style.top = y;
        });
    }

    private void EnsureHistoryShowsListView()
    {
        if (historyShowsListView != null) return;
        var parent = historyShowsListScroll != null ? historyShowsListScroll.parent : historyPanel;
        historyShowsListView = new ListView
        {
            name = "historyShowsListView",
            selectionType = SelectionType.None,
            fixedItemHeight = 36f
        };
        historyShowsListView.style.flexGrow = 1;
        historyShowsListView.makeItem = () =>
        {
            var b = new Button();
            b.AddToClassList("list-entry");
            b.RegisterCallback<ClickEvent>(_ =>
            {
                var list = historyShowsListView.itemsSource as List<ShowData>;
                if (b.userData is int idx && list != null && idx >= 0 && idx < list.Count)
                    ShowSelectedShowHistory(list[idx]);
            });
            return b;
        };
        historyShowsListView.bindItem = (ve, i) =>
        {
            var b = (Button)ve;
            var shows = historyShowsListView.itemsSource as List<ShowData>;
            if (shows != null && i >= 0 && i < shows.Count)
            {
                var s = shows[i];
                var loc = string.IsNullOrEmpty(s?.city) ? s?.venue : s.city;
                var datePart = string.IsNullOrEmpty(s?.date) ? string.Empty : $" - {s.date}";
                var locPart = string.IsNullOrEmpty(loc) ? string.Empty : $" ({loc})";
                b.text = $"{s?.showName}{datePart}{locPart}";
                b.userData = i;
            }
            else { b.text = string.Empty; b.userData = -1; }
        };
        parent?.Add(historyShowsListView);
        if (historyShowsListScroll != null) historyShowsListScroll.style.display = DisplayStyle.None;
    }

    private void PopulateHistoryShowsList()
    {
        if (historyShowsListView == null) return;
        var all = currentPromotion?.shows ?? new List<ShowData>();
        string filter = historyLocationFilterField != null ? (historyLocationFilterField.value ?? string.Empty).Trim() : string.Empty;
        if (!string.IsNullOrEmpty(filter))
        {
            string f = filter.ToLowerInvariant();
            all = all.Where(s =>
                (!string.IsNullOrEmpty(s?.city) && s.city.ToLowerInvariant().Contains(f)) ||
                (!string.IsNullOrEmpty(s?.venue) && s.venue.ToLowerInvariant().Contains(f))
            ).ToList();
        }
        historyShowsListView.itemsSource = all;
        historyShowsListView.Rebuild();
    }

    // ----- Drag-and-drop reordering for matches/segments (Step 3) -----
    private ListView matchesOrderView;
    private Button matchesCloseButton;

    private void EnsureMatchesOrderListView()
    {
        if (matchesView == null) return; // UXML anchor is required
        if (matchesOrderView != null) return;
        matchesOrderView = new ListView
        {
            name = "matchesOrderView",
            selectionType = SelectionType.None,
            reorderable = true,
            fixedItemHeight = 36f
        };
        matchesOrderView.style.flexGrow = 1;
        matchesOrderView.makeItem = () => { var b = new Button(); b.AddToClassList("list-entry"); return b; };
        matchesOrderView.bindItem = (ve, i) =>
        {
            var b = (Button)ve;
            var list = matchesOrderView.itemsSource as List<string>;
            b.text = GetDisplayTextForToken(currentEditingShow, (list != null && i >= 0 && i < list.Count) ? list[i] : null);
        };
        matchesOrderView.itemIndexChanged += (from, to) =>
        {
            // itemsSource is entryOrder; Unity UI Toolkit already moved the element.
            if (currentEditingShow != null)
                DataManager.SavePromotion(currentPromotion);
        };
        matchesView.Add(matchesOrderView);

        // Add a Close button to return to the main Shows panel
        if (matchesCloseButton == null)
        {
            matchesCloseButton = new Button(() => CloseMatchesView()) { name = "matchesCloseButton", text = "Close" };
            matchesView.Add(matchesCloseButton);
        }
    }

    private ShowData currentEditingShow;

    private void EditShow(ShowData show)
    {
        currentEditingShow = show;
        // Seed entryOrder if empty using current order
        if (currentEditingShow != null)
        {
            if (currentEditingShow.entryOrder == null)
                currentEditingShow.entryOrder = new List<string>();
            if (currentEditingShow.entryOrder.Count == 0)
            {
                if (currentEditingShow.matches != null)
                    foreach (var m in currentEditingShow.matches)
                        if (m != null && !string.IsNullOrEmpty(m.id)) currentEditingShow.entryOrder.Add($"M:{m.id}");
                if (currentEditingShow.segments != null)
                    foreach (var s in currentEditingShow.segments)
                        if (s != null && !string.IsNullOrEmpty(s.id)) currentEditingShow.entryOrder.Add($"S:{s.id}");
            }
        }
        RefreshMatchesOrderList();
        // Reveal the matches view panel so user can reorder
        matchesView?.RemoveFromClassList("hidden");
        // Hide other shows UI while viewing matches to declutter
        showDetailsPanel?.AddToClassList("hidden");
        showAddPanel?.AddToClassList("hidden");
        showsListView?.AddToClassList("hidden");
        FocusPanel(matchesView ?? showsPanel);
    }

    private void RefreshMatchesOrderList()
    {
        if (matchesOrderView == null) return;
        var src = currentEditingShow?.entryOrder ?? new List<string>();
        matchesOrderView.itemsSource = src;
        matchesOrderView.Rebuild();
    }

    private void CloseMatchesView()
    {
        matchesView?.AddToClassList("hidden");
        // Restore main Shows UI
        showsListView?.RemoveFromClassList("hidden");
        if (selectedShowIndex >= 0) showDetailsPanel?.RemoveFromClassList("hidden"); else showAddPanel?.RemoveFromClassList("hidden");
        FocusPanel(showsPanel);
    }

    private static string GetDisplayTextForToken(ShowData show, string token)
    {
        if (show == null || string.IsNullOrEmpty(token) || token.Length < 3 || token[1] != ':') return string.Empty;
        char kind = token[0]; string key = token.Substring(2);
        int idx;
        if (!int.TryParse(key, out idx)) { if (kind == 'M') idx = FindMatchIndexById(show, key); else if (kind == 'S') idx = FindSegmentIndexById(show, key); }
        if (idx < 0) return string.Empty;
        if (kind == 'M')
        {
            if (show.matches == null || idx < 0 || idx >= show.matches.Count) return string.Empty;
            var m = show.matches[idx];
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(m.wrestlerA)) parts.Add(m.wrestlerA);
            if (!string.IsNullOrEmpty(m.wrestlerB)) parts.Add(m.wrestlerB);
            if (!string.IsNullOrEmpty(m.wrestlerC)) parts.Add(m.wrestlerC);
            if (!string.IsNullOrEmpty(m.wrestlerD)) parts.Add(m.wrestlerD);
            string vsLine = parts.Count > 0 ? string.Join(" vs ", parts) : string.Empty;
            return string.IsNullOrEmpty(vsLine) ? m.matchName : $"{m.matchName} - {vsLine}";
        }
        else if (kind == 'S')
        {
            if (show.segments == null || idx < 0 || idx >= show.segments.Count) return string.Empty;
            var s = show.segments[idx];
            var segName = string.IsNullOrEmpty(s?.name) ? "Segment" : s.name;
            return $"Segment: {segName}";
        }
        return string.Empty;
    }

    private static bool StringEquals(string a, string b)
    {
        return string.Equals(a ?? string.Empty, b ?? string.Empty, System.StringComparison.OrdinalIgnoreCase);
    }

    private enum RankCategory { Men, Women, TagTeam, Stable }

    private void EnsureRankingsListView()
    {
        if (rankingsListView != null) return;
        var parent = rankingsListScroll != null ? rankingsListScroll.parent : rankingsPanel;
        rankingsListView = new ListView
        {
            name = "rankingsListView",
            selectionType = SelectionType.None,
            fixedItemHeight = 36f
        };
        rankingsListView.style.flexGrow = 1;
        rankingsListView.makeItem = () => { var b = new Button(); b.AddToClassList("list-entry"); return b; };
        rankingsListView.bindItem = (ve, i) =>
        {
            var b = (Button)ve;
            var items = rankingsListView.itemsSource as List<string>;
            b.text = (items != null && i >= 0 && i < items.Count) ? items[i] : string.Empty;
        };
        parent?.Add(rankingsListView);
        if (rankingsListScroll != null) rankingsListScroll.style.display = DisplayStyle.None;
    }

    private void PopulateRankings(RankCategory category)
    {
        if (rankingsListView == null || currentPromotion == null) return;

        if (category == RankCategory.TagTeam)
        {
            PopulateTagTeamRankings();
            return;
        }
        if (category == RankCategory.Stable)
        {
            PopulateStableRankings();
            return;
        }

        // Singles (Men/Women)
        var flagByName = new Dictionary<string, bool>(System.StringComparer.OrdinalIgnoreCase);
        wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion.promotionName);
        if (wrestlerCollection?.wrestlers != null)
            foreach (var w in wrestlerCollection.wrestlers)
                if (!string.IsNullOrEmpty(w.name)) flagByName[w.name] = w.isFemale;

        var singles = new Dictionary<string, (int wins, int losses)>(System.StringComparer.OrdinalIgnoreCase);
        // Build resolver for ID -> current name
        var nameById = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var w in wrestlerCollection?.wrestlers ?? new List<WrestlerData>())
            if (!string.IsNullOrEmpty(w?.id) && !string.IsNullOrEmpty(w.name)) nameById[w.id] = w.name;
        string NameOf(string id, string fallback) => (!string.IsNullOrEmpty(id) && nameById.TryGetValue(id, out var nm)) ? nm : fallback;

        if (currentPromotion.shows != null)
        {
            foreach (var show in currentPromotion.shows)
            {
                if (show?.matches == null) continue;
                foreach (var m in show.matches)
                {
                    // Consider only true singles matches for singles rankings
                    var parts = new List<string>();
                    void addPart(string id, string name) { var v = NameOf(id, name); if (!string.IsNullOrWhiteSpace(v)) parts.Add(v.Trim()); }
                    addPart(m.wrestlerAId, m.wrestlerA);
                    addPart(m.wrestlerBId, m.wrestlerB);
                    addPart(m.wrestlerCId, m.wrestlerC);
                    addPart(m.wrestlerDId, m.wrestlerD);

                    // Identify tag matches either by type or by having 4 participants
                    bool isTag = (!string.IsNullOrEmpty(m.matchType) && m.matchType.ToLowerInvariant().Contains("tag")) ||
                                 (parts.Count >= 4);

                    // Only include matches that are strictly 1v1 and not tag
                    if (isTag || parts.Count != 2) continue;
                    string winner = NameOf(m.winnerId, m.winner);
                    winner = string.IsNullOrWhiteSpace(winner) ? null : winner.Trim();
                    foreach (var p in parts)
                    {
                        flagByName.TryGetValue(p, out var isFemale);
                        bool include = category == RankCategory.Women ? isFemale : !isFemale;
                        if (!include) continue;
                        if (!singles.ContainsKey(p)) singles[p] = (0, 0);
                        var r = singles[p];
                        if (!string.IsNullOrEmpty(winner) && string.Equals(p, winner, System.StringComparison.OrdinalIgnoreCase)) r.wins++; else if (!string.IsNullOrEmpty(winner)) r.losses++;
                        singles[p] = r;
                    }
                }
            }
        }

        var itemsSingles = singles
            .OrderByDescending(e => e.Value.wins)
            .ThenBy(e => e.Value.losses)
            .ThenBy(e => e.Key)
            .Select(e =>
            {
                int total = e.Value.wins + e.Value.losses;
                string pct = total > 0 ? ((float)e.Value.wins / total).ToString("P0") : "0%";
                return $"{e.Key} - {e.Value.wins}-{e.Value.losses} ({pct})";
            })
            .ToList();
        if (itemsSingles.Count == 0) itemsSingles.Add("No results yet for this category.");
        rankingsListView.itemsSource = itemsSingles;
        rankingsListView.Rebuild();
    }

    private void PopulateTagTeamRankings()
    {
        var teams = DataManager.LoadTagTeams(currentPromotion.promotionName);
        var teamByMembers = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase); // key: A|B -> teamName
        foreach (var t in teams.teams ?? new List<TagTeamData>())
        {
            if (string.IsNullOrEmpty(t?.teamName) || string.IsNullOrEmpty(t.memberA) || string.IsNullOrEmpty(t.memberB)) continue;
            string key = MakeTeamKey(t.memberA, t.memberB);
            teamByMembers[key] = t.teamName;
        }

        var records = new Dictionary<string, (int wins, int losses)>(System.StringComparer.OrdinalIgnoreCase);
        // Name resolver for wrestler IDs
        wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion.promotionName);
        var nameById = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var w in wrestlerCollection?.wrestlers ?? new List<WrestlerData>())
            if (!string.IsNullOrEmpty(w?.id) && !string.IsNullOrEmpty(w.name)) nameById[w.id] = w.name;
        string NameOf(string id, string fallback) => (!string.IsNullOrEmpty(id) && nameById.TryGetValue(id, out var nm)) ? nm : fallback;

        foreach (var show in currentPromotion.shows ?? new List<ShowData>())
        {
            foreach (var m in show.matches ?? new List<MatchData>())
            {
                // Consider only tag matches (by type if set, else by 4 participants)
                bool isTag = (!string.IsNullOrEmpty(m.matchType) && m.matchType.ToLowerInvariant().Contains("tag")) ||
                             (!string.IsNullOrWhiteSpace(m.wrestlerA) && !string.IsNullOrWhiteSpace(m.wrestlerB) && !string.IsNullOrWhiteSpace(m.wrestlerC) && !string.IsNullOrWhiteSpace(m.wrestlerD));
                if (!isTag) continue;

                string a = NameOf(m.wrestlerAId, m.wrestlerA);
                string b = NameOf(m.wrestlerBId, m.wrestlerB);
                string c = NameOf(m.wrestlerCId, m.wrestlerC);
                string d = NameOf(m.wrestlerDId, m.wrestlerD);
                string key1 = MakeTeamKey(a, b);
                string key2 = MakeTeamKey(c, d);
                bool hasTeam1 = teamByMembers.TryGetValue(key1, out var team1);
                bool hasTeam2 = teamByMembers.TryGetValue(key2, out var team2);
                if (!hasTeam1 && !hasTeam2) continue; // skip if neither side is a defined team

                string winner = NameOf(m.winnerId, m.winner);
                winner = string.IsNullOrWhiteSpace(winner) ? null : winner.Trim();
                // Determine which side won by winner membership
                bool side1Win = !string.IsNullOrEmpty(winner) && (StringEquals(winner, a) || StringEquals(winner, b));
                bool side2Win = !string.IsNullOrEmpty(winner) && (StringEquals(winner, c) || StringEquals(winner, d));

                if (hasTeam1)
                {
                    if (!records.ContainsKey(team1)) records[team1] = (0, 0);
                    var r = records[team1];
                    if (side1Win) r.wins++; else if (side2Win) r.losses++;
                    records[team1] = r;
                }
                if (hasTeam2)
                {
                    if (!records.ContainsKey(team2)) records[team2] = (0, 0);
                    var r = records[team2];
                    if (side2Win) r.wins++; else if (side1Win) r.losses++;
                    records[team2] = r;
                }
            }
        }

        var items = records
            .OrderByDescending(e => e.Value.wins)
            .ThenBy(e => e.Value.losses)
            .ThenBy(e => e.Key)
            .Select(e =>
            {
                int total = e.Value.wins + e.Value.losses;
                string pct = total > 0 ? ((float)e.Value.wins / total).ToString("P0") : "0%";
                return $"{e.Key} - {e.Value.wins}-{e.Value.losses} ({pct})";
            })
            .ToList();
        if (items.Count == 0) items.Add("No team results yet.");
        rankingsListView.itemsSource = items;
        rankingsListView.Rebuild();

        static string MakeTeamKey(string a, string b)
        {
            string s1 = (a ?? string.Empty).Trim();
            string s2 = (b ?? string.Empty).Trim();
            if (string.Compare(s1, s2, System.StringComparison.OrdinalIgnoreCase) > 0) { var tmp = s1; s1 = s2; s2 = tmp; }
            return $"{s1}|{s2}";
        }
    }

    private void PopulateStableRankings()
    {
        // Load stables and map wrestlerId -> stableName
        var stables = DataManager.LoadStables(currentPromotion.promotionName);
        var nameById = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion.promotionName);
        foreach (var w in wrestlerCollection?.wrestlers ?? new List<WrestlerData>())
            if (!string.IsNullOrEmpty(w?.id) && !string.IsNullOrEmpty(w.name)) nameById[w.id] = w.name;

        var stableByMemberName = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var s in stables.stables ?? new List<StableData>())
        {
            foreach (var mid in s.memberIds ?? new List<string>())
            {
                if (!string.IsNullOrEmpty(mid) && nameById.TryGetValue(mid, out var nm))
                    stableByMemberName[nm] = s.stableName;
            }
        }

        string StableOf(params string[] names)
        {
            // If all provided names belong to same stable, return it; else null
            string candidate = null;
            foreach (var n in names)
            {
                if (string.IsNullOrWhiteSpace(n)) continue;
                if (!stableByMemberName.TryGetValue(n, out var sname)) return null;
                if (candidate == null) candidate = sname; else if (!StringEquals(candidate, sname)) return null;
            }
            return candidate;
        }

        // Compute records by stable for matches we can interpret as 2v2 (current data allows up to 4 participants)
        var records = new Dictionary<string, (int wins, int losses)>(System.StringComparer.OrdinalIgnoreCase);

        // Name resolver for IDs in matches
        var idToName = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var w in wrestlerCollection?.wrestlers ?? new List<WrestlerData>())
            if (!string.IsNullOrEmpty(w?.id) && !string.IsNullOrEmpty(w.name)) idToName[w.id] = w.name;
        string NameOf(string id, string fallback) => (!string.IsNullOrEmpty(id) && idToName.TryGetValue(id, out var nm)) ? nm : fallback;

        foreach (var show in currentPromotion.shows ?? new List<ShowData>())
        {
            foreach (var m in show.matches ?? new List<MatchData>())
            {
                // Treat only obvious team matches (tag/two-per-side) until participants array exists
                var parts = new List<string>();
                void addPart(string id, string name) { var v = NameOf(id, name); if (!string.IsNullOrWhiteSpace(v)) parts.Add(v.Trim()); }
                addPart(m.wrestlerAId, m.wrestlerA);
                addPart(m.wrestlerBId, m.wrestlerB);
                addPart(m.wrestlerCId, m.wrestlerC);
                addPart(m.wrestlerDId, m.wrestlerD);
                if (parts.Count != 4) continue; // 2v2 only for now
                string team1Stable = StableOf(parts[0], parts[1]);
                string team2Stable = StableOf(parts[2], parts[3]);
                if (string.IsNullOrEmpty(team1Stable) || string.IsNullOrEmpty(team2Stable)) continue; // require both sides be within a stable

                string winner = NameOf(m.winnerId, m.winner);
                winner = string.IsNullOrWhiteSpace(winner) ? null : winner.Trim();
                bool side1Win = !string.IsNullOrEmpty(winner) && (StringEquals(winner, parts[0]) || StringEquals(winner, parts[1]));
                bool side2Win = !string.IsNullOrEmpty(winner) && (StringEquals(winner, parts[2]) || StringEquals(winner, parts[3]));

                if (!records.ContainsKey(team1Stable)) records[team1Stable] = (0, 0);
                if (!records.ContainsKey(team2Stable)) records[team2Stable] = (0, 0);
                var r1 = records[team1Stable]; var r2 = records[team2Stable];
                if (side1Win) { r1.wins++; r2.losses++; }
                else if (side2Win) { r2.wins++; r1.losses++; }
                records[team1Stable] = r1; records[team2Stable] = r2;
            }
        }

        var items = records
            .OrderByDescending(e => e.Value.wins)
            .ThenBy(e => e.Value.losses)
            .ThenBy(e => e.Key)
            .Select(e =>
            {
                int total = e.Value.wins + e.Value.losses;
                string pct = total > 0 ? ((float)e.Value.wins / total).ToString("P0") : "0%";
                return $"{e.Key} - {e.Value.wins}-{e.Value.losses} ({pct})";
            })
            .ToList();
        if (items.Count == 0) items.Add("No stables results yet.");
        rankingsListView.itemsSource = items;
        rankingsListView.Rebuild();
    }

    // ----- Step 2: Stable IDs + ordered history rendering -----
    private void EnsureStableIdsAndEntryOrder()
    {
        if (currentPromotion?.shows == null) return;
        bool changed = false;
        foreach (var show in currentPromotion.shows)
        {
            if (show == null) continue;
            // Assign IDs where missing
            if (show.matches != null)
                foreach (var m in show.matches)
                    if (m != null && string.IsNullOrEmpty(m.id)) { m.id = System.Guid.NewGuid().ToString("N"); changed = true; }
            if (show.segments != null)
                foreach (var s in show.segments)
                    if (s != null && string.IsNullOrEmpty(s.id)) { s.id = System.Guid.NewGuid().ToString("N"); changed = true; }

            // Upgrade numeric entryOrder tokens to IDs
            if (show.entryOrder != null && show.entryOrder.Count > 0)
            {
                var upgraded = new List<string>(show.entryOrder.Count);
                bool localChanged = false;
                foreach (var token in show.entryOrder)
                {
                    if (string.IsNullOrEmpty(token) || token.Length < 3 || token[1] != ':') { upgraded.Add(token); continue; }
                    char kind = token[0];
                    string tail = token.Substring(2);
                    if (!int.TryParse(tail, out int idx)) { upgraded.Add(token); continue; }
                    if (kind == 'M')
                    {
                        if (show.matches != null && idx >= 0 && idx < show.matches.Count)
                        {
                            var id = show.matches[idx]?.id; if (!string.IsNullOrEmpty(id)) { upgraded.Add($"M:{id}"); localChanged = true; }
                        }
                    }
                    else if (kind == 'S')
                    {
                        if (show.segments != null && idx >= 0 && idx < show.segments.Count)
                        {
                            var id = show.segments[idx]?.id; if (!string.IsNullOrEmpty(id)) { upgraded.Add($"S:{id}"); localChanged = true; }
                        }
                    }
                }
                if (localChanged)
                {
                    show.entryOrder = upgraded;
                    changed = true;
                }
            }
        }
        if (changed)
        {
            DataManager.SavePromotion(currentPromotion);
        }
    }

    private void ShowSelectedShowHistory(ShowData show)
    {
        if (historyResultsPanel == null || historyShowMatchesList == null) return;
        historyShowsPanel?.AddToClassList("hidden");
        historyResultsPanel.RemoveFromClassList("hidden");
        if (historyResultsHeader != null)
        {
            string date = string.IsNullOrEmpty(show?.date) ? string.Empty : $" - {show.date}";
            historyResultsHeader.text = $"Results: {show?.showName}{date}";
        }
        historyShowMatchesList.Clear();
        bool any = false;
        if (show != null && show.entryOrder != null && show.entryOrder.Count > 0)
        {
            foreach (var token in show.entryOrder)
            {
                if (string.IsNullOrEmpty(token) || token.Length < 3 || token[1] != ':') continue;
                char kind = token[0]; string key = token.Substring(2);
                int idx = -1;
                if (!int.TryParse(key, out idx)) { if (kind == 'M') idx = FindMatchIndexById(show, key); else if (kind == 'S') idx = FindSegmentIndexById(show, key); }
                if (idx < 0) continue;
                if (kind == 'M')
                {
                    if (show.matches == null || idx < 0 || idx >= show.matches.Count) continue;
                    var m = show.matches[idx];
                    var entry = new VisualElement();
                    entry.style.marginBottom = 6;
                    var parts = new List<string>();
                    if (!string.IsNullOrEmpty(m.wrestlerA)) parts.Add(m.wrestlerA);
                    if (!string.IsNullOrEmpty(m.wrestlerB)) parts.Add(m.wrestlerB);
                    if (!string.IsNullOrEmpty(m.wrestlerC)) parts.Add(m.wrestlerC);
                    if (!string.IsNullOrEmpty(m.wrestlerD)) parts.Add(m.wrestlerD);
                    string vsLine = parts.Count > 0 ? string.Join(" vs ", parts) : string.Empty;
                    entry.Add(new Label(m.matchName));
                    if (!string.IsNullOrEmpty(vsLine)) entry.Add(new Label(vsLine));
                    if (!string.IsNullOrEmpty(m.winner)) entry.Add(new Label($"Winner: {m.winner}"));
                    if (m.isTitleMatch && !string.IsNullOrEmpty(m.titleName)) entry.Add(new Label($"Title: {m.titleName}"));
                    historyShowMatchesList.Add(entry);
                    any = true;
                }
                else if (kind == 'S')
                {
                    if (show.segments == null || idx < 0 || idx >= show.segments.Count) continue;
                    var s = show.segments[idx];
                    var entry = new VisualElement();
                    entry.style.marginBottom = 6;
                    var segName = string.IsNullOrEmpty(s?.name) ? "Segment" : s.name;
                    entry.Add(new Label($"Segment: {segName}"));
                    if (!string.IsNullOrEmpty(s?.text)) entry.Add(new Label(s.text));
                    historyShowMatchesList.Add(entry);
                    any = true;
                }
            }
        }
        if (!any)
        {
            // Fallback: matches then segments
            if (show?.matches != null)
            {
                foreach (var m in show.matches)
                {
                    var entry = new VisualElement(); entry.style.marginBottom = 6;
                    var parts = new List<string>();
                    if (!string.IsNullOrEmpty(m.wrestlerA)) parts.Add(m.wrestlerA);
                    if (!string.IsNullOrEmpty(m.wrestlerB)) parts.Add(m.wrestlerB);
                    if (!string.IsNullOrEmpty(m.wrestlerC)) parts.Add(m.wrestlerC);
                    if (!string.IsNullOrEmpty(m.wrestlerD)) parts.Add(m.wrestlerD);
                    string vsLine = parts.Count > 0 ? string.Join(" vs ", parts) : string.Empty;
                    entry.Add(new Label(m.matchName)); if (!string.IsNullOrEmpty(vsLine)) entry.Add(new Label(vsLine));
                    if (!string.IsNullOrEmpty(m.winner)) entry.Add(new Label($"Winner: {m.winner}"));
                    if (m.isTitleMatch && !string.IsNullOrEmpty(m.titleName)) entry.Add(new Label($"Title: {m.titleName}"));
                    historyShowMatchesList.Add(entry);
                }
            }
            if (show?.segments != null)
            {
                foreach (var s in show.segments)
                {
                    var entry = new VisualElement(); entry.style.marginBottom = 6;
                    var segName = string.IsNullOrEmpty(s?.name) ? "Segment" : s.name;
                    entry.Add(new Label($"Segment: {segName}")); if (!string.IsNullOrEmpty(s?.text)) entry.Add(new Label(s.text));
                    historyShowMatchesList.Add(entry);
                }
            }
        }
        FocusPanel(historyResultsPanel);
    }

    private static int FindMatchIndexById(ShowData show, string id)
    {
        if (show?.matches == null || string.IsNullOrEmpty(id)) return -1;
        for (int i = 0; i < show.matches.Count; i++)
            if (string.Equals(show.matches[i]?.id, id, System.StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }

    private static int FindSegmentIndexById(ShowData show, string id)
    {
        if (show?.segments == null || string.IsNullOrEmpty(id)) return -1;
        for (int i = 0; i < show.segments.Count; i++)
            if (string.Equals(show.segments[i]?.id, id, System.StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }
    // Stables UI
    private ScrollView stableListScroll;
    private ListView stableListView;
    private TextField stableNameField;
    private DropdownField stableMemberDropdown;
    private ScrollView stableMembersList;
    private VisualElement stableActionsRow;
    private Button addStableButton, saveStablesButton, saveStableButton, deleteStableButton, cancelStableButton, addStableMemberButton, removeStableMemberButton;
    private StableCollection stableCollection;
    private int selectedStableIndex = -1;
    // --------- Stables ---------
    private void EnsureStableListView()
    {
        if (stableListView != null) return;
        var parent = stableListScroll != null ? stableListScroll.parent : stablesPanel;
        stableListView = new ListView
        {
            name = "stableListView",
            selectionType = SelectionType.None,
            fixedItemHeight = 36f
        };
        stableListView.style.flexGrow = 1;
        stableListView.makeItem = () =>
        {
            var b = new Button();
            b.AddToClassList("list-entry");
            b.RegisterCallback<ClickEvent>(_ => { if (b.userData is int idx) SelectStable(idx); });
            return b;
        };
        stableListView.bindItem = (ve, i) =>
        {
            var b = (Button)ve;
            var list = stableCollection?.stables;
            if (list != null && i >= 0 && i < list.Count) { b.text = list[i].stableName; b.userData = i; }
            else { b.text = string.Empty; b.userData = -1; }
        };
        parent?.Add(stableListView);
        if (stableListScroll != null) stableListScroll.style.display = DisplayStyle.None;
    }

    private void RefreshStableList()
    {
        if (stableListView == null) return;
        var src = stableCollection?.stables ?? new List<StableData>();
        stableListView.itemsSource = src;
        stableListView.Rebuild();
    }

    private void SelectStable(int index)
    {
        if (stableCollection?.stables == null || index < 0 || index >= stableCollection.stables.Count) return;
        selectedStableIndex = index;
        var s = stableCollection.stables[index];
        if (stableNameField != null) stableNameField.value = s.stableName;
        PopulateStableMemberChoices();
        PopulateStableMembersUI(s);
        SetActivePanel(stablesPanel);
    }

    private void PopulateStableMemberChoices()
    {
        if (stableMemberDropdown == null) return;
        wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion.promotionName);
        var choices = new List<string>();
        foreach (var w in wrestlerCollection?.wrestlers ?? new List<WrestlerData>())
            if (!string.IsNullOrEmpty(w?.name)) choices.Add(w.name);
        if (choices.Count == 0) choices.Add(string.Empty);
        stableMemberDropdown.choices = choices;
        stableMemberDropdown.value = choices[0];
    }

    private void PopulateStableMembersUI(StableData s)
    {
        if (stableMembersList == null) return;
        stableMembersList.Clear();
        var map = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion.promotionName);
        foreach (var w in wrestlerCollection?.wrestlers ?? new List<WrestlerData>())
            if (!string.IsNullOrEmpty(w?.id) && !string.IsNullOrEmpty(w.name)) map[w.id] = w.name;
        foreach (var id in s.memberIds ?? new List<string>())
        {
            map.TryGetValue(id ?? string.Empty, out var nm);
            stableMembersList.Add(new Label(nm ?? id ?? string.Empty));
        }
    }

    private void OnAddStable()
    {
        if (currentPromotion == null) { if (statusLabel != null) statusLabel.text = "No promotion loaded."; return; }
        stableCollection ??= DataManager.LoadStables(currentPromotion.promotionName);
        stableCollection.promotionName = currentPromotion.promotionName;
        var s = new StableData { id = System.Guid.NewGuid().ToString("N"), stableName = (stableNameField?.value ?? "New Stable").Trim() };
        s.memberIds = new List<string>();
        stableCollection.stables ??= new List<StableData>();
        stableCollection.stables.Add(s);
        DataManager.SaveStables(stableCollection);
        RefreshStableList();
        // Auto-select the newly added stable to show members and controls
        selectedStableIndex = (stableCollection.stables?.Count ?? 1) - 1;
        SelectStable(selectedStableIndex);
        if (statusLabel != null) statusLabel.text = "Stable added.";
    }

    private void OnSaveStables()
    {
        if (currentPromotion == null || stableCollection == null) return;
        stableCollection.promotionName = currentPromotion.promotionName;
        DataManager.SaveStables(stableCollection);
        if (statusLabel != null) statusLabel.text = "Stables saved.";
    }

    private void OnSaveSelectedStable()
    {
        if (stableCollection?.stables == null || selectedStableIndex < 0 || selectedStableIndex >= stableCollection.stables.Count) return;
        var s = stableCollection.stables[selectedStableIndex];
        if (stableNameField != null) s.stableName = stableNameField.value;
        DataManager.SaveStables(stableCollection);
        RefreshStableList();
        if (statusLabel != null) statusLabel.text = "Stable updated.";
    }

    private void OnDeleteSelectedStable()
    {
        if (stableCollection?.stables == null || selectedStableIndex < 0 || selectedStableIndex >= stableCollection.stables.Count) return;
        stableCollection.stables.RemoveAt(selectedStableIndex);
        selectedStableIndex = -1;
        DataManager.SaveStables(stableCollection);
        RefreshStableList();
        if (statusLabel != null) statusLabel.text = "Stable deleted.";
    }

    private void OnCancelEditStable()
    {
        selectedStableIndex = -1;
        if (stableNameField != null) stableNameField.value = string.Empty;
        SetActivePanel(stablesPanel);
    }

    private void OnAddStableMember()
    {
        if (stableCollection?.stables == null || selectedStableIndex < 0 || selectedStableIndex >= stableCollection.stables.Count) return;
        var s = stableCollection.stables[selectedStableIndex];
        var name = stableMemberDropdown != null ? (stableMemberDropdown.value ?? string.Empty).Trim() : string.Empty;
        if (string.IsNullOrEmpty(name)) return;
        wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion.promotionName);
        var w = (wrestlerCollection?.wrestlers ?? new List<WrestlerData>()).FirstOrDefault(x => string.Equals(x?.name, name, StringComparison.OrdinalIgnoreCase));
        if (w == null || string.IsNullOrEmpty(w.id)) return;
        s.memberIds ??= new List<string>();
        if (s.memberIds.Contains(w.id)) return;
        s.memberIds.Add(w.id);
        DataManager.SaveStables(stableCollection);
        PopulateStableMembersUI(s);
        if (statusLabel != null) statusLabel.text = "Member added.";
    }

    private void OnRemoveStableMember()
    {
        if (stableCollection?.stables == null || selectedStableIndex < 0 || selectedStableIndex >= stableCollection.stables.Count) return;
        var s = stableCollection.stables[selectedStableIndex];
        var name = stableMemberDropdown != null ? (stableMemberDropdown.value ?? string.Empty).Trim() : string.Empty;
        if (string.IsNullOrEmpty(name)) return;
        wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion.promotionName);
        var w = (wrestlerCollection?.wrestlers ?? new List<WrestlerData>()).FirstOrDefault(x => string.Equals(x?.name, name, StringComparison.OrdinalIgnoreCase));
        if (w == null || string.IsNullOrEmpty(w.id)) return;
        if (s.memberIds != null && s.memberIds.Remove(w.id))
        {
            DataManager.SaveStables(stableCollection);
            PopulateStableMembersUI(s);
            if (statusLabel != null) statusLabel.text = "Member removed.";
        }
    }
}
