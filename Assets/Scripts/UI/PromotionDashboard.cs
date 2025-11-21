using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Runtime.InteropServices;
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
    private bool suppressStatsRefresh;
    private bool statsDirty = true;
    private string statsPromotionKey = string.Empty;

    // Root and navigation
    private VisualElement root;
    private Label statusLabel;
    // Dashboard chrome
    private VisualElement dashboardChrome;
    private Label dashboardTitleLabel;
    private Button promotionButton, wrestlersButton, titlesButton, tournamentsButton, stablesButton, tagTeamsButton, showsButton, calendarButton, historyButton, rivalriesButton, rankingsButton, divisionsButton, awardsButton, returnButton, minimizeButton;

    // Panels
    private VisualElement promotionInfoPanel, wrestlersPanel, titlesPanel, tournamentsPanel, stablesPanel, tagTeamsPanel, showsPanel, calendarPanel, cardBuilderPanel, historyPanel, rivalriesPanel, rankingsPanel, divisionsPanel, awardsPanel;
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
    private VisualElement toastBar;
    private Label toastLabel;
    private Coroutine toastRoutine;

    // Virtualized lists (Step 1)
    private ScrollView wrestlerListScroll, showsListScroll, historyShowsListScroll, rankingsListScroll;
    private ListView wrestlerListView, showsListView, historyShowsListView, rankingsListView;
    
    // Rankings 2.0 controls
    private DropdownField rankingsTypeDropdown, rankingsGenderDropdown, rankingsDivisionDropdown, rankingsWeekDropdown;
    private TextField rankingsDateField, rankingsFromDateField, rankingsToDateField;
    private Button computeRankingsButton, saveSnapshotButton, computeRangeRankingsButton, rankingsDatePickButton, rankingsPrevWeekButton, rankingsNextWeekButton;
    private RankingStore rankingStore;
    private List<RankingEntry> currentRankingResults;

    // Divisions overview
    private DropdownField divisionsOverviewDivisionDropdown;
    private ScrollView divisionTitlesList, divisionWrestlersList, divisionTeamsList;

    // Calendar & Card Builder
    private CalendarView calendarView;
    private CardBuilderView cardBuilderView;
    private WrestlerCollection wrestlerCollection;
    private VenueCityConfig venueCityConfig;
    // Wrestler UI
    private VisualElement wrestlerDetails, wrestlerAddPanel;
    private TextField wrestlerNameField, wrestlerHometownField, newWrestlerField;
    private Toggle wrestlerIsFemaleToggle, newWrestlerIsFemaleToggle;
    private DropdownField wrestlerDivisionDropdown, newWrestlerDivisionDropdown;
    private FloatField wrestlerHeightField, wrestlerWeightField;
    private Button addWrestlerButton, saveWrestlersButton, saveWrestlerButton, deleteWrestlerButton, cancelEditButton;
    // Wrestler career UI
    private VisualElement wrestlerCareerPanel;
    private Label wrestlerCareerRecordLabel, wrestlerCareerSpanLabel, wrestlerCareerTitlesLabel;
    private ScrollView wrestlerCareerMatchesList;
    private int selectedWrestlerIndex = -1;
    // Titles (Step 4)
    private ScrollView titleListScroll, titleHistoryList;
    private ListView titleListView;
    private VisualElement titleDetailsPanel, titleAddPanel;
    private Button viewHistoryButton;
    private TextField titleNameField, titleDivisionField, titleNotesField;
    private DropdownField titleChampionDropdown, titleTagChampionADropdown, titleTagChampionBDropdown;
    private Toggle titleIsTagTeamToggle;
    private VisualElement titleTagTeamRow;
    private TextField newTitleField, newTitleDivisionField;
    private Button addTitleButton, saveTitlesButton, saveTitleButton, deleteTitleButton, cancelTitleButton;
    private TitleCollection titleCollection;
    private int selectedTitleIndex = -1;
    // Rivalries UI
    private ScrollView rivalryListScroll, rivalryEventsList;
    private ListView rivalryListView;
    private TextField rivalryNameField, rivalryNotesField;
    private Label rivalryParticipantsLabel, rivalryHeatLabel;
    private DropdownField rivalryTypeDropdown, rivalryParticipantADropdown, rivalryParticipantBDropdown;
    private TextField rivalryEventDateField, rivalryEventNotesField;
    private DropdownField rivalryEventTypeDropdown, rivalryEventOutcomeDropdown, rivalryEventShowDropdown, rivalryEventEntryDropdown;
    private FloatField rivalryEventRatingField;
    private Button addRivalryButton, saveRivalriesButton, saveRivalryButton, deleteRivalryButton, cancelRivalryButton, addRivalryEventButton, openLinkedShowButton;
    private RivalryCollection rivalryCollection;
    private int selectedRivalryIndex = -1;
    private Dictionary<string, string> rivalryEntryMap = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
    // Tournaments UI
    private ScrollView tournamentListScroll;
    private ListView tournamentListView;
    private VisualElement tournamentAddPanel, tournamentManagePanel;
    private TextField newTournamentNameField, tournamentNameField;
    private DropdownField newTournamentTypeDropdown, tournamentTypeDropdown, tournamentEntrantDropdown;
    private ScrollView tournamentEntrantsList, tournamentMatchesList;
    private Button addTournamentButton, saveTournamentsButton, viewTournamentsButton, saveTournamentButton, deleteTournamentButton, cancelTournamentButton;
    private Button addEntrantButton, removeEntrantButton, generateBracketButton, advanceRoundButton, clearBracketButton;
    // Awards UI
    private TextField awardsYearField, wotyOverrideField, motyOverrideField, feudOverrideField, tagTeamOverrideField;
    private Label wotySuggestionLabel, motySuggestionLabel, feudSuggestionLabel, tagTeamSuggestionLabel;
    private ScrollView milestonesList;
    private Button computeAwardsButton, saveAwardsButton;
    private TournamentCollection tournamentCollection;
    private int selectedTournamentIndex = -1;
    // Tag Teams UI
    private ScrollView tagTeamListScroll;
    private ListView tagTeamListView;
    private TextField teamNameField;
    private DropdownField teamMemberADropdown, teamMemberBDropdown, teamDivisionDropdown;
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
    private Label statWrestlersLabel, statTeamsLabel, statTitlesLabel, statShowsLabel, statMatchesLabel, statRivalriesLabel, statBrandsLabel, statTitleChangesLabel;
    private Button editPromotionButton, savePromotionButton, cancelPromotionButton;
    private TextField nameField, locationField, foundedField, descriptionField;
    private VisualElement editPanel;
    // Brands UI
    private ScrollView brandListScroll;
    private TextField newBrandField;
    private Button addBrandButton;

    // Shows UI (details + editors)
    private VisualElement showDetailsPanel, showAddPanel, matchEditor, segmentEditor;
    private TextField newShowField, newShowDateField, showNameField, showDateField;
    private DropdownField showVenueField, showCityField, newShowVenueField, newShowCityField;
    private ScrollView venueListScroll, cityListScroll;
    private TextField newVenueField, newCityField;
    private Button addVenueButton, addCityButton;
    private IntegerField showAttendanceField, newShowAttendanceField;
    private FloatField showRatingField, newShowRatingField;
    private DropdownField showTypeDropdown, newShowTypeDropdown, showBrandDropdown, newShowBrandDropdown, historyBrandDropdown, rankingsBrandDropdown, calendarBrandDropdown, calendarGenerationBrandDropdown, showsBrandFilterDropdown;
    private Button addShowButton, saveShowsButton, saveShowButton, deleteShowButton, cancelShowButton, viewMatchesButton;
    private Button addMatchButton, addSegmentButton, saveMatchButton, cancelMatchButton, saveSegmentButton, cancelSegmentButton;
    private DropdownField matchTypeDropdown, wrestlerADropdown, wrestlerBDropdown, wrestlerCDropdown, wrestlerDDropdown, titleDropdown, winnerDropdown;
    private Toggle isTitleMatchToggle;
    private TextField segmentNameField, segmentTextField;
    private DropdownField segmentTypeDropdown, segmentParticipantADropdown, segmentParticipantBDropdown, segmentParticipantCDropdown, segmentParticipantDDropdown;
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
        statsDirty = true;
        statsPromotionKey = string.Empty;
        root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null)
        {
            Debug.LogError("PromotionDashboard: rootVisualElement not found.");
            return;
        }

        // Dashboard chrome
        dashboardChrome = root.Q<VisualElement>("dashboardChrome");
        dashboardTitleLabel = root.Q<Label>("dashboardTitleLabel");
        statusLabel = root.Q<Label>("statusLabel");

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
        rivalriesButton = root.Q<Button>("rivalriesButton");
        rankingsButton = root.Q<Button>("rankingsButton");
        divisionsButton = root.Q<Button>("divisionsButton");
        awardsButton = root.Q<Button>("awardsButton");
        returnButton = root.Q<Button>("returnButton");
        minimizeButton = root.Q<Button>("minimizeButton");
        toastBar = root.Q<VisualElement>("toastBar");
        toastLabel = root.Q<Label>("toastLabel");

        // Apply promotion-wide theme (brand-driven, if available)
        ApplyPromotionTheme();

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
        rivalriesPanel = root.Q<VisualElement>("rivalriesPanel");
        rankingsPanel = root.Q<VisualElement>("rankingsPanel");
        divisionsPanel = root.Q<VisualElement>("divisionsPanel");
        awardsPanel = root.Q<VisualElement>("awardsPanel");

        // Divisions overview widgets
        divisionsOverviewDivisionDropdown = root.Q<DropdownField>("divisionsOverviewDivisionDropdown");
        divisionTitlesList = root.Q<ScrollView>("divisionTitlesList");
        divisionWrestlersList = root.Q<ScrollView>("divisionWrestlersList");
        divisionTeamsList = root.Q<ScrollView>("divisionTeamsList");

        if (divisionsOverviewDivisionDropdown != null)
        {
            divisionsOverviewDivisionDropdown.RegisterValueChangedCallback(_ => RefreshDivisionOverviewLists());
        }
        historyShowsPanel = root.Q<VisualElement>("historyShowsPanel");
        historyResultsPanel = root.Q<VisualElement>("historyResultsPanel");
        historyResultsHeader = root.Q<Label>("historyResultsHeader");
        historyShowMatchesList = root.Q<ScrollView>("historyShowMatchesList");
        historyLocationFilterField = root.Q<TextField>("historyLocationFilterField");
        historyBrandDropdown = root.Q<DropdownField>("historyBrandDropdown");
        calendarBrandDropdown = root.Q<DropdownField>("calendarBrandDropdown");
        calendarGenerationBrandDropdown = root.Q<DropdownField>("calendarGenerationBrandDropdown");
        rankingsPanel = root.Q<VisualElement>("rankingsPanel");
        // Rivalries queries
        rivalryListScroll = root.Q<ScrollView>("rivalryList");
        rivalryEventsList = root.Q<ScrollView>("rivalryEventsList");
        rivalryNameField = root.Q<TextField>("rivalryNameField");
        rivalryNotesField = root.Q<TextField>("rivalryNotesField");
        rivalryParticipantsLabel = root.Q<Label>("rivalryParticipantsLabel");
        rivalryHeatLabel = root.Q<Label>("rivalryHeatLabel");
        rivalryTypeDropdown = root.Q<DropdownField>("rivalryTypeDropdown");
        rivalryParticipantADropdown = root.Q<DropdownField>("rivalryParticipantADropdown");
        rivalryParticipantBDropdown = root.Q<DropdownField>("rivalryParticipantBDropdown");
        rivalryEventDateField = root.Q<TextField>("rivalryEventDateField");
        rivalryEventNotesField = root.Q<TextField>("rivalryEventNotesField");
        rivalryEventTypeDropdown = root.Q<DropdownField>("rivalryEventTypeDropdown");
        rivalryEventOutcomeDropdown = root.Q<DropdownField>("rivalryEventOutcomeDropdown");
        rivalryEventShowDropdown = root.Q<DropdownField>("rivalryEventShowDropdown");
        rivalryEventEntryDropdown = root.Q<DropdownField>("rivalryEventEntryDropdown");
        rivalryEventRatingField = root.Q<FloatField>("rivalryEventRatingField");
        addRivalryButton = root.Q<Button>("addRivalryButton");
        saveRivalriesButton = root.Q<Button>("saveRivalriesButton");
        saveRivalryButton = root.Q<Button>("saveRivalryButton");
        deleteRivalryButton = root.Q<Button>("deleteRivalryButton");
        cancelRivalryButton = root.Q<Button>("cancelRivalryButton");
        addRivalryEventButton = root.Q<Button>("addRivalryEventButton");
        openLinkedShowButton = root.Q<Button>("openLinkedShowButton");
        // Overlay behavior for dropdowns
        SetupDropdownOverlay(rivalryParticipantADropdown);
        SetupDropdownOverlay(rivalryParticipantBDropdown);
        SetupDropdownOverlay(rivalryEventTypeDropdown);
        SetupDropdownOverlay(rivalryEventOutcomeDropdown);
        SetupDropdownOverlay(rivalryEventShowDropdown);
        SetupDropdownOverlay(rivalryEventEntryDropdown);
        if (rivalryEventShowDropdown != null)
            rivalryEventShowDropdown.RegisterValueChangedCallback(_ => PopulateRivalryEventEntryChoices());

        // Query list ScrollViews/buttons used as anchors in UXML
        wrestlerListScroll = root.Q<ScrollView>("wrestlerList");
        // Wrestler details/add panel
        wrestlerDetails = root.Q<VisualElement>("wrestlerDetails");
        wrestlerAddPanel = root.Q<VisualElement>("wrestlerAddPanel");
        wrestlerCareerPanel = root.Q<VisualElement>("wrestlerCareerPanel");
        wrestlerCareerRecordLabel = root.Q<Label>("wrestlerCareerRecordLabel");
        wrestlerCareerSpanLabel = root.Q<Label>("wrestlerCareerSpanLabel");
        wrestlerCareerTitlesLabel = root.Q<Label>("wrestlerCareerTitlesLabel");
        wrestlerCareerMatchesList = root.Q<ScrollView>("wrestlerCareerMatchesList");
        wrestlerNameField = root.Q<TextField>("wrestlerNameField");
        wrestlerHometownField = root.Q<TextField>("wrestlerHometownField");
        wrestlerIsFemaleToggle = root.Q<Toggle>("wrestlerIsFemaleToggle");
        wrestlerDivisionDropdown = root.Q<DropdownField>("wrestlerDivisionDropdown");
        wrestlerHeightField = root.Q<FloatField>("wrestlerHeightField");
        wrestlerWeightField = root.Q<FloatField>("wrestlerWeightField");
        newWrestlerField = root.Q<TextField>("newWrestlerField");
        newWrestlerIsFemaleToggle = root.Q<Toggle>("newWrestlerIsFemaleToggle");
        newWrestlerDivisionDropdown = root.Q<DropdownField>("newWrestlerDivisionDropdown");
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
        
        rankingsTypeDropdown = root.Q<DropdownField>("rankingsTypeDropdown");
        rankingsGenderDropdown = root.Q<DropdownField>("rankingsGenderDropdown");
        rankingsDivisionDropdown = root.Q<DropdownField>("rankingsDivisionDropdown");
        rankingsBrandDropdown = root.Q<DropdownField>("rankingsBrandDropdown");
        rankingsWeekDropdown = root.Q<DropdownField>("rankingsWeekDropdown");
        rankingsDateField = root.Q<TextField>("rankingsDateField");
        rankingsFromDateField = root.Q<TextField>("rankingsFromDateField");
        rankingsToDateField = root.Q<TextField>("rankingsToDateField");
        computeRankingsButton = root.Q<Button>("computeRankingsButton");
        computeRangeRankingsButton = root.Q<Button>("computeRangeRankingsButton");
        saveSnapshotButton = root.Q<Button>("saveSnapshotButton");
        rankingsPrevWeekButton = root.Q<Button>("rankingsPrevWeekButton");
        rankingsNextWeekButton = root.Q<Button>("rankingsNextWeekButton");
        // Title edit/display widgets
        titleDetailsPanel = root.Q<VisualElement>("titleDetails");
        titleAddPanel = root.Q<VisualElement>("titleAddPanel");
        titleNameField = root.Q<TextField>("titleNameField");
        titleDivisionField = root.Q<TextField>("titleDivisionField");
        titleChampionDropdown = root.Q<DropdownField>("titleChampionDropdown");
        titleIsTagTeamToggle = root.Q<Toggle>("titleIsTagTeamToggle");
        titleTagTeamRow = root.Q<VisualElement>("titleTagTeamRow");
        titleTagChampionADropdown = root.Q<DropdownField>("titleTagChampionADropdown");
        titleTagChampionBDropdown = root.Q<DropdownField>("titleTagChampionBDropdown");
        titleNotesField = root.Q<TextField>("titleNotesField");
        newTitleDivisionField = root.Q<TextField>("newTitleDivisionField");
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
        newShowVenueField = root.Q<DropdownField>("newShowVenueField");
        newShowCityField = root.Q<DropdownField>("newShowCityField");
        newShowTypeDropdown = root.Q<DropdownField>("newShowTypeDropdown");
        newShowBrandDropdown = root.Q<DropdownField>("newShowBrandDropdown");
        newShowAttendanceField = root.Q<IntegerField>("newShowAttendanceField");
        newShowRatingField = root.Q<FloatField>("newShowRatingField");
        addShowButton = root.Q<Button>("addShowButton");
        saveShowsButton = root.Q<Button>("saveShowsButton");
        // Show details and editors
        showDetailsPanel = root.Q<VisualElement>("showDetails");
        showAddPanel = root.Q<VisualElement>("showAddPanel");
        showNameField = root.Q<TextField>("showNameField");
        showDateField = root.Q<TextField>("showDateField");
        showVenueField = root.Q<DropdownField>("showVenueField");
        showCityField = root.Q<DropdownField>("showCityField");
        showTypeDropdown = root.Q<DropdownField>("showTypeDropdown");
        showBrandDropdown = root.Q<DropdownField>("showBrandDropdown");
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
        segmentTypeDropdown = root.Q<DropdownField>("segmentTypeDropdown");
        segmentParticipantADropdown = root.Q<DropdownField>("segmentParticipantADropdown");
        segmentParticipantBDropdown = root.Q<DropdownField>("segmentParticipantBDropdown");
        segmentParticipantCDropdown = root.Q<DropdownField>("segmentParticipantCDropdown");
        segmentParticipantDDropdown = root.Q<DropdownField>("segmentParticipantDDropdown");
        segmentTextField = root.Q<TextField>("segmentTextField");
        saveSegmentButton = root.Q<Button>("saveSegmentButton");
        cancelSegmentButton = root.Q<Button>("cancelSegmentButton");
        // Venues & Cities management
        venueListScroll = root.Q<ScrollView>("venueList");
        cityListScroll = root.Q<ScrollView>("cityList");
        newVenueField = root.Q<TextField>("newVenueField");
        newCityField = root.Q<TextField>("newCityField");
        addVenueButton = root.Q<Button>("addVenueButton");
        addCityButton = root.Q<Button>("addCityButton");

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

        if (historyBrandDropdown != null)
            historyBrandDropdown.RegisterValueChangedCallback(_ => PopulateHistoryShowsList());
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
        statWrestlersLabel = root.Q<Label>("statWrestlersLabel");
        statTeamsLabel = root.Q<Label>("statTeamsLabel");
        statTitlesLabel = root.Q<Label>("statTitlesLabel");
        statShowsLabel = root.Q<Label>("statShowsLabel");
        statMatchesLabel = root.Q<Label>("statMatchesLabel");
        statRivalriesLabel = root.Q<Label>("statRivalriesLabel");
        statBrandsLabel = root.Q<Label>("statBrandsLabel");
        statTitleChangesLabel = root.Q<Label>("statTitleChangesLabel");
        editPromotionButton = root.Q<Button>("editPromotionButton");
        savePromotionButton = root.Q<Button>("savePromotionButton");
        cancelPromotionButton = root.Q<Button>("cancelPromotionButton");
        nameField = root.Q<TextField>("nameField");
        locationField = root.Q<TextField>("locationField");
        foundedField = root.Q<TextField>("foundedField");
        descriptionField = root.Q<TextField>("descriptionField");
        editPanel = root.Q<VisualElement>("editPanel");
        brandListScroll = root.Q<ScrollView>("brandList");
        newBrandField = root.Q<TextField>("newBrandField");
        addBrandButton = root.Q<Button>("addBrandButton");
        // Awards queries
        awardsYearField = root.Q<TextField>("awardsYearField");
        wotySuggestionLabel = root.Q<Label>("wotySuggestionLabel");
        motySuggestionLabel = root.Q<Label>("motySuggestionLabel");
        feudSuggestionLabel = root.Q<Label>("feudSuggestionLabel");
        tagTeamSuggestionLabel = root.Q<Label>("tagTeamSuggestionLabel");
        wotyOverrideField = root.Q<TextField>("wotyOverrideField");
        motyOverrideField = root.Q<TextField>("motyOverrideField");
        feudOverrideField = root.Q<TextField>("feudOverrideField");
        tagTeamOverrideField = root.Q<TextField>("tagTeamOverrideField");
        computeAwardsButton = root.Q<Button>("computeAwardsButton");
        saveAwardsButton = root.Q<Button>("saveAwardsButton");
        milestonesList = root.Q<ScrollView>("milestonesList");
        // Tag Teams queries
        teamNameField = root.Q<TextField>("teamNameField");
        teamMemberADropdown = root.Q<DropdownField>("teamMemberADropdown");
        teamMemberBDropdown = root.Q<DropdownField>("teamMemberBDropdown");
        SetupDropdownOverlay(teamMemberADropdown);
        SetupDropdownOverlay(teamMemberBDropdown);
        teamDivisionDropdown = root.Q<DropdownField>("teamDivisionDropdown");
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
        RegisterMainPanel(rivalriesPanel);
        RegisterMainPanel(rankingsPanel);
        RegisterMainPanel(divisionsPanel);
        RegisterMainPanel(awardsPanel);

        // Wire navigation
        if (promotionButton != null) promotionButton.clicked += ShowPromotionPanel;
        if (wrestlersButton != null) wrestlersButton.clicked += ShowWrestlersPanel;
        if (titlesButton != null) titlesButton.clicked += ShowTitlesPanel;
        if (tagTeamsButton != null) tagTeamsButton.clicked += ShowTagTeamsPanel;
        if (showsButton != null) showsButton.clicked += ShowShowsPanel;
        if (calendarButton != null) calendarButton.clicked += ShowCalendarPanel;
        if (historyButton != null) historyButton.clicked += ShowHistoryPanel;
        if (rivalriesButton != null) rivalriesButton.clicked += ShowRivalriesPanel;
        if (rankingsButton != null) rankingsButton.clicked += ShowRankingsPanel;
        if (divisionsButton != null) divisionsButton.clicked += ShowDivisionsPanel;
        if (awardsButton != null) awardsButton.clicked += ShowAwardsPanel;
        if (minimizeButton != null) minimizeButton.clicked += OnMinimizeClicked;
        // Rivalries handlers
        if (addRivalryButton != null) addRivalryButton.clicked += OnAddRivalry;
        if (saveRivalriesButton != null) saveRivalriesButton.clicked += OnSaveRivalries;
        if (saveRivalryButton != null) saveRivalryButton.clicked += OnSaveSelectedRivalry;
        if (deleteRivalryButton != null) deleteRivalryButton.clicked += OnDeleteSelectedRivalry;
        if (cancelRivalryButton != null) cancelRivalryButton.clicked += OnCancelEditRivalry;
        if (addRivalryEventButton != null) addRivalryEventButton.clicked += OnAddRivalryEvent;
        if (openLinkedShowButton != null) openLinkedShowButton.clicked += OnOpenLinkedShow;
        if (stablesButton != null) stablesButton.clicked += ShowStablesPanel;
        if (tournamentsButton != null) tournamentsButton.clicked += ShowTournamentsPanel;
        if (viewTournamentsButton != null) viewTournamentsButton.clicked += ShowTournamentManagePanel;
        
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
        if (titleIsTagTeamToggle != null)
        {
            titleIsTagTeamToggle.RegisterValueChangedCallback(evt =>
            {
                if (titleTagTeamRow != null)
                {
                    if (evt.newValue) titleTagTeamRow.RemoveFromClassList("hidden");
                    else titleTagTeamRow.AddToClassList("hidden");
                }
                if (titleChampionDropdown != null)
                {
                    if (evt.newValue) titleChampionDropdown.AddToClassList("hidden");
                    else titleChampionDropdown.RemoveFromClassList("hidden");
                }
            });
        }
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
        if (addBrandButton != null) addBrandButton.clicked += OnAddBrand;
        if (computeAwardsButton != null) computeAwardsButton.clicked += OnComputeAwardsClicked;
        if (saveAwardsButton != null) saveAwardsButton.clicked += OnSaveAwardsClicked;
        // Shows handlers
        if (addShowButton != null) addShowButton.clicked += OnAddShow;
        if (saveShowsButton != null) saveShowsButton.clicked += OnSaveShows;
        if (saveShowButton != null) saveShowButton.clicked += OnSaveSelectedShow;
        if (deleteShowButton != null) deleteShowButton.clicked += OnDeleteSelectedShow;
        if (cancelShowButton != null) cancelShowButton.clicked += OnCancelEditShow;
        if (addVenueButton != null) addVenueButton.clicked += OnAddVenue;
        if (addCityButton != null) addCityButton.clicked += OnAddCity;
        if (viewMatchesButton != null) viewMatchesButton.clicked += () =>
        {
            if (selectedShowIndex >= 0 && currentPromotion?.shows != null && selectedShowIndex < currentPromotion.shows.Count)
                EditShow(currentPromotion.shows[selectedShowIndex]);
        };
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
        // Sync division list (from titles) into rankings config and UI dropdowns
        suppressStatsRefresh = true;
        RefreshDivisionChoices();
        RefreshWrestlerList();
        RefreshTitleList();
        RefreshTagTeamList();
        RefreshTournamentList();
        RefreshStableList();
        RefreshShowList();
        RefreshVenueCityDropdowns();
        PopulateHistoryShowsList();
        suppressStatsRefresh = false;
        RefreshPromotionStats();
        // Rankings 2.0 setup
        InitializeRankingsControls();
        ComputeOverallRankings();

        // Brands UI
        RefreshBrandList();
        RefreshBrandDropdowns();

        // Default panel and status
        SetActivePanel(promotionInfoPanel ?? root);
        if (statusLabel != null)
            statusLabel.text = "Ready.";
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

    private void RefreshDivisionChoices()
    {
        // Build division list from titles, plus Overall/blank as needed
        var divisions = new List<string>();
        divisions.Add("Overall");

        titleCollection ??= (currentPromotion != null ? DataManager.LoadTitles(currentPromotion.promotionName) : null);
        if (titleCollection?.titles != null)
        {
            foreach (var t in titleCollection.titles)
            {
                if (t == null || string.IsNullOrWhiteSpace(t.division)) continue;
                var dv = t.division.Trim();
                if (!divisions.Contains(dv, StringComparer.OrdinalIgnoreCase))
                    divisions.Add(dv);
            }
        }

        // Persist into ranking config so it survives next loads
        rankingStore ??= new RankingStore { promotionName = currentPromotion?.promotionName ?? "Unknown" };
        rankingStore.config ??= new RankingConfig { promotionName = currentPromotion?.promotionName };
        rankingStore.config.singlesDivisions = new List<string>(divisions);
        DataManager.SaveRankings(rankingStore);

        // Apply to rankings division dropdown
        if (rankingsDivisionDropdown != null)
        {
            rankingsDivisionDropdown.choices = new List<string>(divisions);
            if (!divisions.Contains(rankingsDivisionDropdown.value))
                rankingsDivisionDropdown.value = divisions[0];
        }

        // Apply to wrestler/tag-team division dropdowns
        var wrestlerDivs = new List<string>(divisions);
        if (!wrestlerDivs.Contains(string.Empty))
            wrestlerDivs.Insert(0, string.Empty);

        if (wrestlerDivisionDropdown != null)
        {
            wrestlerDivisionDropdown.choices = wrestlerDivs;
            if (!wrestlerDivs.Contains(wrestlerDivisionDropdown.value))
                wrestlerDivisionDropdown.value = string.Empty;
        }
        if (newWrestlerDivisionDropdown != null)
        {
            newWrestlerDivisionDropdown.choices = wrestlerDivs;
            if (!newWrestlerDivisionDropdown.choices.Contains(newWrestlerDivisionDropdown.value))
                newWrestlerDivisionDropdown.value = string.Empty;
        }

        if (teamDivisionDropdown != null)
        {
            teamDivisionDropdown.choices = wrestlerDivs;
            if (!teamDivisionDropdown.choices.Contains(teamDivisionDropdown.value))
                teamDivisionDropdown.value = string.Empty;
        }

        RefreshDivisionsOverviewDivisionChoices();
    }

    private void RefreshDivisionsOverviewDivisionChoices()
    {
        if (divisionsOverviewDivisionDropdown == null) return;

        // Use the same singles division list from rankingStore, falling back to a recompute if needed
        var divisions = new List<string>();
        divisions.Add("Overall");

        if (rankingStore?.config?.singlesDivisions != null && rankingStore.config.singlesDivisions.Count > 0)
        {
            foreach (var d in rankingStore.config.singlesDivisions)
            {
                if (string.IsNullOrWhiteSpace(d)) continue;
                if (!divisions.Contains(d, StringComparer.OrdinalIgnoreCase))
                    divisions.Add(d);
            }
        }
        else
        {
            // Fallback: rebuild from titles if rankingStore is not yet populated
            titleCollection ??= (currentPromotion != null ? DataManager.LoadTitles(currentPromotion.promotionName) : null);
            if (titleCollection?.titles != null)
            {
                foreach (var t in titleCollection.titles)
                {
                    if (t == null || string.IsNullOrWhiteSpace(t.division)) continue;
                    var dv = t.division.Trim();
                    if (!divisions.Contains(dv, StringComparer.OrdinalIgnoreCase))
                        divisions.Add(dv);
                }
            }
        }

        divisionsOverviewDivisionDropdown.choices = divisions;
        if (!divisions.Contains(divisionsOverviewDivisionDropdown.value))
            divisionsOverviewDivisionDropdown.value = divisions[0];

        // Keep overview lists in sync when division list changes
        RefreshDivisionOverviewLists();
    }

    private void RefreshDivisionOverviewLists()
    {
        if (divisionsOverviewDivisionDropdown == null) return;

        var selectedDivision = divisionsOverviewDivisionDropdown.value ?? "Overall";
        bool isOverall = string.Equals(selectedDivision, "Overall", StringComparison.OrdinalIgnoreCase);

        // Ensure collections are loaded
        if (currentPromotion != null)
        {
            titleCollection ??= DataManager.LoadTitles(currentPromotion.promotionName);
            wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion.promotionName);
            tagTeamCollection ??= DataManager.LoadTagTeams(currentPromotion.promotionName);
        }

        // Helper to clear and repopulate a simple label list inside a ScrollView
        static void PopulateList(ScrollView list, IEnumerable<string> lines)
        {
            if (list == null) return;
            list.Clear();
            foreach (var line in lines)
            {
                var lbl = new Label(line);
                list.Add(lbl);
            }
        }

        // Titles in selected division
        var titleLines = new List<string>();
        foreach (var t in titleCollection?.titles ?? new List<TitleData>())
        {
            if (t == null || string.IsNullOrEmpty(t.titleName)) continue;
            var dv = t.division ?? string.Empty;
            if (!isOverall && !StringEquals(dv, selectedDivision)) continue;
            titleLines.Add(string.IsNullOrEmpty(dv)
                ? $"{t.titleName} (no division)"
                : $"{t.titleName} [{dv}]");
        }
        if (titleLines.Count == 0) titleLines.Add("No titles in this division.");
        PopulateList(divisionTitlesList, titleLines);

        // Singles wrestlers in selected division (ignore tag-only entries)
        var wrestlerLines = new List<string>();
        foreach (var w in wrestlerCollection?.wrestlers ?? new List<WrestlerData>())
        {
            if (w == null || string.IsNullOrEmpty(w.name)) continue;
            if (w.isTagTeam) continue;
            var dv = w.division ?? string.Empty;
            if (!isOverall && !StringEquals(dv, selectedDivision)) continue;
            var gender = w.isFemale ? "Women" : "Men";
            wrestlerLines.Add(string.IsNullOrEmpty(dv)
                ? $"{w.name} ({gender}, no division)"
                : $"{w.name} ({gender}) [{dv}]");
        }
        if (wrestlerLines.Count == 0) wrestlerLines.Add("No singles wrestlers in this division.");
        PopulateList(divisionWrestlersList, wrestlerLines);

        // Tag teams in selected division
        var teamLines = new List<string>();
        foreach (var t in tagTeamCollection?.teams ?? new List<TagTeamData>())
        {
            if (t == null || string.IsNullOrEmpty(t.teamName)) continue;
            var dv = t.division ?? string.Empty;
            if (!isOverall && !StringEquals(dv, selectedDivision)) continue;
            var members = $"{(string.IsNullOrEmpty(t.memberA) ? "?" : t.memberA)} & {(string.IsNullOrEmpty(t.memberB) ? "?" : t.memberB)}";
            teamLines.Add(string.IsNullOrEmpty(dv)
                ? $"{t.teamName} ({members}, no division)"
                : $"{t.teamName} ({members}) [{dv}]");
        }
        if (teamLines.Count == 0) teamLines.Add("No tag teams in this division.");
        PopulateList(divisionTeamsList, teamLines);
    }

    private void UpdatePromotionInfoUI()
    {
        if (currentPromotion == null)
        {
            if (nameLabel != null) nameLabel.text = "Name: [None]";
            if (locationLabel != null) locationLabel.text = "Location: [None]";
            if (foundedLabel != null) foundedLabel.text = "Founded: [None]";
            if (descriptionLabel != null) descriptionLabel.text = "Description: [None]";
            RefreshPromotionStats();
            return;
        }

        if (nameLabel != null) nameLabel.text = $"Name: {currentPromotion.promotionName}";
        if (locationLabel != null) locationLabel.text = $"Location: {currentPromotion.location}";
        if (foundedLabel != null) foundedLabel.text = $"Founded: {currentPromotion.foundedYear}";
        if (descriptionLabel != null) descriptionLabel.text = $"Description: {currentPromotion.description}";
        InvalidatePromotionStats();
    }

    private void InvalidatePromotionStats()
    {
        statsDirty = true;
        RefreshPromotionStats();
    }

    private void RefreshPromotionStats()
    {
        if (suppressStatsRefresh)
            return;

        var promotionKey = currentPromotion?.promotionName ?? string.Empty;
        if (!statsDirty && string.Equals(statsPromotionKey, promotionKey, StringComparison.Ordinal))
            return;

        statsPromotionKey = promotionKey;
        statsDirty = false;

        void SetStat(Label label, string value)
        {
            if (label != null) label.text = value;
        }

        if (currentPromotion == null)
        {
            SetStat(statWrestlersLabel, "--");
            SetStat(statTeamsLabel, "--");
            SetStat(statTitlesLabel, "--");
            SetStat(statShowsLabel, "--");
            SetStat(statMatchesLabel, "--");
            SetStat(statRivalriesLabel, "--");
            SetStat(statBrandsLabel, "--");
            SetStat(statTitleChangesLabel, "--");
            return;
        }

        wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion.promotionName);
        titleCollection ??= DataManager.LoadTitles(currentPromotion.promotionName);
        tagTeamCollection ??= DataManager.LoadTagTeams(currentPromotion.promotionName);
        rivalryCollection ??= DataManager.LoadRivalries(currentPromotion.promotionName);

        int wrestlerCount = wrestlerCollection?.wrestlers?.Count(w => w != null) ?? 0;
        int tagTeamCount = tagTeamCollection?.teams?.Count(t => t != null) ?? 0;
        int titleCount = titleCollection?.titles?.Count(t => t != null) ?? 0;
        int showCount = currentPromotion.shows?.Count(s => s != null) ?? 0;
        int matchCount = 0;
        if (currentPromotion.shows != null)
        {
            foreach (var show in currentPromotion.shows)
            {
                if (show?.matches == null) continue;
                matchCount += show.matches.Count(m => m != null);
            }
        }

        int recordedMatches = matchCount;
        int titleReigns = 0;
        try
        {
            var historyMatches = TitleHistoryManager.GetAllMatchResults(currentPromotion.promotionName);
            if (historyMatches != null && historyMatches.Count > 0)
                recordedMatches = historyMatches.Count;

            var lineages = TitleHistoryManager.GetTitleLineages(currentPromotion.promotionName);
            if (lineages != null)
            {
                foreach (var lineage in lineages)
                    titleReigns += lineage?.reigns?.Count ?? 0;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"PromotionDashboard: failed to compute historical stats: {ex.Message}");
        }

        int rivalryCount = rivalryCollection?.rivalries?.Count(r => r != null) ?? 0;
        int brandCount = currentPromotion.brands?.Count(b => !string.IsNullOrWhiteSpace(b)) ?? 0;

        SetStat(statWrestlersLabel, wrestlerCount.ToString("N0", CultureInfo.InvariantCulture));
        SetStat(statTeamsLabel, tagTeamCount.ToString("N0", CultureInfo.InvariantCulture));
        SetStat(statTitlesLabel, titleCount.ToString("N0", CultureInfo.InvariantCulture));
        SetStat(statShowsLabel, showCount.ToString("N0", CultureInfo.InvariantCulture));
        SetStat(statMatchesLabel, recordedMatches.ToString("N0", CultureInfo.InvariantCulture));
        SetStat(statRivalriesLabel, rivalryCount.ToString("N0", CultureInfo.InvariantCulture));
        SetStat(statBrandsLabel, brandCount.ToString("N0", CultureInfo.InvariantCulture));
        SetStat(statTitleChangesLabel, titleReigns.ToString("N0", CultureInfo.InvariantCulture));
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
    private void ShowDivisionsPanel()
    {
        SetActivePanel(divisionsPanel);
        RefreshDivisionsOverviewDivisionChoices();
        RefreshDivisionOverviewLists();
    }
    private void ShowRivalriesPanel()
    {
        SetActivePanel(rivalriesPanel);
        // Load and prepare rivalry data and lists
        rivalryCollection ??= DataManager.LoadRivalries(currentPromotion?.promotionName);
        EnsureRivalryTypeChoices();
        EnsureRivalryEventChoices();
        PopulateRivalryParticipantChoices(rivalryTypeDropdown?.value);
        EnsureRivalryListView();
        RefreshRivalryList();
        // Auto-select first rivalry if any
        if ((selectedRivalryIndex < 0) && (rivalryCollection?.rivalries != null) && rivalryCollection.rivalries.Count > 0)
        {
            SelectRivalry(0);
        }
    }

    // --------- Rivalries ---------
    private void EnsureRivalryListView()
    {
        if (rivalryListView != null) return;
        var parent = rivalryListScroll != null ? rivalryListScroll.parent : rivalriesPanel;
        rivalryListView = new ListView
        {
            name = "rivalryListView",
            selectionType = SelectionType.None,
            fixedItemHeight = 36f
        };
        rivalryListView.style.flexGrow = 1;
        rivalryListView.makeItem = () =>
        {
            var b = new Button();
            b.AddToClassList("list-entry");
            b.RegisterCallback<ClickEvent>(_ => { if (b.userData is int idx) SelectRivalry(idx); });
            return b;
        };
        rivalryListView.bindItem = (ve, i) =>
        {
            var b = (Button)ve;
            var list = rivalryCollection?.rivalries;
            if (list != null && i >= 0 && i < list.Count)
            {
                var r = list[i];
                var type = string.IsNullOrEmpty(r.type) ? "Singles" : r.type;
                var heat = r.feudScore;
                b.text = heat > 0f
                    ? $"{r.title} [{type}] • Heat: {heat:F1}"
                    : $"{r.title} [{type}]";
                b.userData = i;
            }
            else { b.text = string.Empty; b.userData = -1; }
        };
        parent?.Add(rivalryListView);
        if (rivalryListScroll != null) rivalryListScroll.style.display = DisplayStyle.None;
    }

    private void RefreshRivalryList()
    {
        if (rivalryListView == null) return;
        var src = rivalryCollection?.rivalries ?? new List<RivalryData>();
        foreach (var r in src)
            RecomputeRivalryMetrics(r);
        rivalryListView.itemsSource = src;
        rivalryListView.Rebuild();
        InvalidatePromotionStats();
    }

    private void SelectRivalry(int index)
    {
        if (rivalryCollection?.rivalries == null || index < 0 || index >= rivalryCollection.rivalries.Count) return;
        selectedRivalryIndex = index;
        var r = rivalryCollection.rivalries[index];
        if (rivalryNameField != null) rivalryNameField.value = r.title;
        EnsureRivalryTypeChoices();
        if (rivalryTypeDropdown != null) rivalryTypeDropdown.value = string.IsNullOrEmpty(r.type) ? rivalryTypeDropdown.choices.FirstOrDefault() : r.type;
        PopulateRivalryParticipantChoices(rivalryTypeDropdown?.value);
        if (rivalryParticipantADropdown != null) rivalryParticipantADropdown.value = ResolveNameFromTypedId(r.participants.ElementAtOrDefault(0));
        if (rivalryParticipantBDropdown != null) rivalryParticipantBDropdown.value = ResolveNameFromTypedId(r.participants.ElementAtOrDefault(1));
        if (rivalryNotesField != null) rivalryNotesField.value = r.notes;
        PopulateRivalryEventsUI(r);
        UpdateRivalrySummaryUI(r);
        FocusPanel(rivalriesPanel);
    }

    private void EnsureRivalryTypeChoices()
    {
        if (rivalryTypeDropdown == null) return;
        if (rivalryTypeDropdown.choices == null || rivalryTypeDropdown.choices.Count == 0)
            rivalryTypeDropdown.choices = new List<string> { "Singles", "Tag Team", "Stables" };
        if (string.IsNullOrEmpty(rivalryTypeDropdown.value)) rivalryTypeDropdown.value = rivalryTypeDropdown.choices[0];
        rivalryTypeDropdown.RegisterValueChangedCallback(_ => { PopulateRivalryParticipantChoices(rivalryTypeDropdown.value); });
    }

    private void EnsureRivalryEventChoices()
    {
        if (rivalryEventTypeDropdown != null)
        {
            if (rivalryEventTypeDropdown.choices == null || rivalryEventTypeDropdown.choices.Count == 0)
                rivalryEventTypeDropdown.choices = new List<string> { "Match", "Segment", "Promo", "Attack", "Other" };
            if (string.IsNullOrEmpty(rivalryEventTypeDropdown.value)) rivalryEventTypeDropdown.value = rivalryEventTypeDropdown.choices[0];
        }
        if (rivalryEventOutcomeDropdown != null)
        {
            if (rivalryEventOutcomeDropdown.choices == null || rivalryEventOutcomeDropdown.choices.Count == 0)
                rivalryEventOutcomeDropdown.choices = new List<string> { "A wins", "B wins", "Draw", "NA" };
            if (string.IsNullOrEmpty(rivalryEventOutcomeDropdown.value)) rivalryEventOutcomeDropdown.value = rivalryEventOutcomeDropdown.choices[0];
        }
        if (rivalryEventDateField != null && string.IsNullOrEmpty(rivalryEventDateField.value))
            rivalryEventDateField.value = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        EnsureRivalryShowChoices();
        PopulateRivalryEventEntryChoices();
    }

    private void EnsureRivalryShowChoices()
    {
        if (rivalryEventShowDropdown == null) return;
        var choices = new List<string>();
        var shows = currentPromotion?.shows ?? new List<ShowData>();
        foreach (var s in shows)
        {
            if (!string.IsNullOrEmpty(s?.date))
            {
                var label = string.IsNullOrEmpty(s.showName) ? s.date : ($"{s.date} | {s.showName}");
                choices.Add(label);
            }
        }
        if (choices.Count == 0) choices.Add(string.Empty);
        rivalryEventShowDropdown.choices = choices;
        if (string.IsNullOrEmpty(rivalryEventShowDropdown.value)) rivalryEventShowDropdown.value = choices[0];
    }

        private void PopulateRivalryEventEntryChoices()
    {
        if (rivalryEventEntryDropdown == null) return;
        rivalryEntryMap = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        var entries = new List<string>();
        var show = FindShowFromChoice(rivalryEventShowDropdown?.value);
        if (show != null)
        {
            foreach (var m in show.matches ?? new List<MatchData>())
            {
                if (!string.IsNullOrEmpty(m?.id))
                {
                    var display = BuildMatchDisplayLabel(m);
                    entries.Add(display);
                    rivalryEntryMap[display] = $"M:{m.id}";
                }
            }
            foreach (var sg in show.segments ?? new List<SegmentData>())
            {
                if (!string.IsNullOrEmpty(sg?.id))
                {
                    var segName = !string.IsNullOrEmpty(sg.name) ? sg.name : "(unnamed)";
                    var typeSuffix = string.IsNullOrEmpty(sg.segmentType) ? string.Empty : $" [{sg.segmentType}]";
                    var display = $"Segment: {segName}{typeSuffix}";
                    entries.Add(display);
                    rivalryEntryMap[display] = $"S:{sg.id}";
                }
            }
        }
        if (entries.Count == 0) entries.Add(string.Empty);
        rivalryEventEntryDropdown.choices = entries;
        if (string.IsNullOrEmpty(rivalryEventEntryDropdown.value)) rivalryEventEntryDropdown.value = entries[0];
    }

    private ShowData FindShowFromChoice(string choice)
    {
        if (string.IsNullOrEmpty(choice)) return null;
        var parts = choice.Split('|');
        var date = parts.Length > 0 ? parts[0].Trim() : choice.Trim();
        return (currentPromotion?.shows ?? new List<ShowData>()).FirstOrDefault(s => string.Equals(s?.date, date, StringComparison.OrdinalIgnoreCase));
    }

    private void PopulateRivalryParticipantChoices(string type)
    {
        string t = type ?? "Singles";
        var names = new List<string>();
        if (string.Equals(t, "Tag Team", StringComparison.OrdinalIgnoreCase))
        {
            tagTeamCollection ??= DataManager.LoadTagTeams(currentPromotion?.promotionName);
            foreach (var g in tagTeamCollection?.teams ?? new List<TagTeamData>())
                if (!string.IsNullOrEmpty(g?.teamName)) names.Add(g.teamName);
        }
        else if (string.Equals(t, "Stables", StringComparison.OrdinalIgnoreCase))
        {
            stableCollection ??= DataManager.LoadStables(currentPromotion?.promotionName);
            foreach (var s in stableCollection?.stables ?? new List<StableData>())
                if (!string.IsNullOrEmpty(s?.stableName)) names.Add(s.stableName);
        }
        else
        {
            wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion?.promotionName);
            foreach (var w in wrestlerCollection?.wrestlers ?? new List<WrestlerData>())
                if (!string.IsNullOrEmpty(w?.name)) names.Add(w.name);
        }

        names = names
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (!names.Contains(string.Empty)) names.Insert(0, string.Empty);

        SetChoices(rivalryParticipantADropdown, names);
        SetChoices(rivalryParticipantBDropdown, names);
    }

    private void PopulateRivalryEventsUI(RivalryData r)
    {
        if (rivalryEventsList == null) return;
        rivalryEventsList.Clear();
        foreach (var e in r.events ?? new List<RivalryEvent>())
        {
            var btn = new Button(() => OpenLinkedShowFromEvent(e));
            string showTag = string.IsNullOrEmpty(e.showId) ? string.Empty : $" • Show: {((currentPromotion?.shows ?? new List<ShowData>()).FirstOrDefault(s => string.Equals(s?.id, e.showId, StringComparison.OrdinalIgnoreCase))?.showName ?? e.showId)}";
            btn.text = $"{e.date} â€¢ {e.eventType} â€¢ {e.outcome}{showTag}";
            btn.AddToClassList("list-entry");
            btn.text = $"{e.date} | {e.eventType} | {e.outcome}{showTag}";
            rivalryEventsList.Add(btn);
        }
    }

    private void OpenLinkedShowFromEvent(RivalryEvent ev)
    {
        if (ev == null || string.IsNullOrEmpty(ev.showId))
        {
            if (statusLabel != null) statusLabel.text = "This event is not linked to a show.";
            return;
        }

        var shows = currentPromotion?.shows ?? new List<ShowData>();
        var show = shows.FirstOrDefault(s => string.Equals(s?.id ?? string.Empty, ev.showId, StringComparison.OrdinalIgnoreCase));
        if (show == null)
        {
            if (statusLabel != null) statusLabel.text = "Linked show not found.";
            return;
        }

        // If we have a Card Builder and a specific entry, jump directly to that card entry
        if (cardBuilderView != null && (!string.IsNullOrEmpty(ev.matchId) || !string.IsNullOrEmpty(ev.segmentId)))
        {
            SetActivePanel(cardBuilderPanel);
            if (!string.IsNullOrEmpty(ev.matchId))
                cardBuilderView.BeginEditAndSelectEntry(show, 'M', ev.matchId);
            else
                cardBuilderView.BeginEditAndSelectEntry(show, 'S', ev.segmentId);
            return;
        }

        // Fallback: open the show in the Shows panel
        SetActivePanel(showsPanel);
        EnsureShowsListView();
        RefreshShowList();
        int idx = -1;
        for (int i = 0; i < shows.Count; i++)
        {
            if (string.Equals(shows[i]?.id ?? string.Empty, ev.showId, StringComparison.OrdinalIgnoreCase)) { idx = i; break; }
        }
        if (idx >= 0) SelectShow(idx);
        else if (statusLabel != null) statusLabel.text = "Linked show not found.";
    }

    private string ResolveNameFromTypedId(string typedId)
    {
        if (string.IsNullOrEmpty(typedId) || typedId.Length < 3 || typedId[1] != ':') return string.Empty;
        char kind = typedId[0]; string id = typedId.Substring(2);
        switch (kind)
        {
            case 'W':
                wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion?.promotionName);
                return wrestlerCollection?.wrestlers?.FirstOrDefault(w => string.Equals(w?.id, id, StringComparison.OrdinalIgnoreCase))?.name ?? string.Empty;
            case 'T':
                var tags = DataManager.LoadTagTeams(currentPromotion?.promotionName);
                return tags?.teams?.FirstOrDefault(t => string.Equals(t?.id, id, StringComparison.OrdinalIgnoreCase))?.teamName ?? string.Empty;
            case 'S':
                var st = DataManager.LoadStables(currentPromotion?.promotionName);
                return st?.stables?.FirstOrDefault(s => string.Equals(s?.id, id, StringComparison.OrdinalIgnoreCase))?.stableName ?? string.Empty;
        }
        return string.Empty;
    }

    private string ResolveTypedId(string type, string name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;
        if (string.Equals(type, "Tag Team", StringComparison.OrdinalIgnoreCase))
        {
            var tags = DataManager.LoadTagTeams(currentPromotion?.promotionName);
            var t = tags?.teams?.FirstOrDefault(x => string.Equals(x?.teamName, name, StringComparison.OrdinalIgnoreCase));
            if (t != null && !string.IsNullOrEmpty(t.id)) return $"T:{t.id}";
        }
        else if (string.Equals(type, "Stables", StringComparison.OrdinalIgnoreCase))
        {
            var st = DataManager.LoadStables(currentPromotion?.promotionName);
            var s = st?.stables?.FirstOrDefault(x => string.Equals(x?.stableName, name, StringComparison.OrdinalIgnoreCase));
            if (s != null && !string.IsNullOrEmpty(s.id)) return $"S:{s.id}";
        }
        else
        {
            wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion?.promotionName);
            var w = wrestlerCollection?.wrestlers?.FirstOrDefault(x => string.Equals(x?.name, name, StringComparison.OrdinalIgnoreCase));
            if (w != null && !string.IsNullOrEmpty(w.id)) return $"W:{w.id}";
        }
        return string.Empty;
    }

    private void RecomputeRivalryMetrics(RivalryData r)
    {
        if (r == null) return;
        r.winsA = 0;
        r.winsB = 0;
        r.draws = 0;
        r.lastInteractionDate = null;
        r.feudScore = 0f;

        if (r.events == null || r.events.Count == 0) return;

        DateTime firstDate = DateTime.MaxValue;
        DateTime lastDate = DateTime.MinValue;
        float rawScore = 0f;
        // Build indexes for fast show/match lookup
        var showsById = new Dictionary<string, ShowData>(StringComparer.OrdinalIgnoreCase);
        if (currentPromotion?.shows != null)
        {
            foreach (var s in currentPromotion.shows)
            {
                if (s != null && !string.IsNullOrEmpty(s.id) && !showsById.ContainsKey(s.id))
                    showsById[s.id] = s;
            }
        }
        var matchesByShowId = new Dictionary<string, Dictionary<string, MatchData>>(StringComparer.OrdinalIgnoreCase);

        foreach (var e in r.events)
        {
            if (e == null) continue;

            // Date tracking
            if (!string.IsNullOrEmpty(e.date) && DateTime.TryParse(e.date, out var d))
            {
                if (d < firstDate) firstDate = d;
                if (d > lastDate) lastDate = d;
            }

            // Outcome tallies
            if (!string.IsNullOrEmpty(e.outcome))
            {
                if (string.Equals(e.outcome, "A wins", StringComparison.OrdinalIgnoreCase)) r.winsA++;
                else if (string.Equals(e.outcome, "B wins", StringComparison.OrdinalIgnoreCase)) r.winsB++;
                else if (string.Equals(e.outcome, "Draw", StringComparison.OrdinalIgnoreCase)) r.draws++;
            }

            // Base heat from event
            float score = 1f;
            score += Mathf.Max(0f, e.rating);

            // Event type bonus
            var t = e.eventType ?? string.Empty;
            if (t.IndexOf("Match", StringComparison.OrdinalIgnoreCase) >= 0) score += 1.5f;
            else if (t.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) >= 0) score += 1.0f;
            else if (t.IndexOf("Promo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     t.IndexOf("Segment", StringComparison.OrdinalIgnoreCase) >= 0) score += 0.5f;

            // Stip/importance based on linked match
            if (!string.IsNullOrEmpty(e.matchId) && !string.IsNullOrEmpty(e.showId) && showsById.Count > 0)
            {
                matchesByShowId.TryGetValue(e.showId, out var map);
                if (map == null)
                {
                    if (showsById.TryGetValue(e.showId, out var s) && s?.matches != null)
                    {
                        map = new Dictionary<string, MatchData>(StringComparer.OrdinalIgnoreCase);
                        foreach (var m in s.matches)
                        {
                            if (m != null && !string.IsNullOrEmpty(m.id))
                                map[m.id] = m;
                        }
                        matchesByShowId[e.showId] = map;
                    }
                }
                MatchData match = null;
                if (map != null) map.TryGetValue(e.matchId, out match);
                if (match != null)
                {
                    if (match.isTitleMatch) score += 0.5f;
                    var mt = match.matchType ?? string.Empty;
                    if (mt.IndexOf("cage", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        mt.IndexOf("cell", StringComparison.OrdinalIgnoreCase) >= 0)
                        score += 1.0f;
                    if (mt.IndexOf("ladder", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        mt.IndexOf("TLC", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        mt.IndexOf("table", StringComparison.OrdinalIgnoreCase) >= 0)
                        score += 1.0f;
                    if (mt.IndexOf("no dq", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        mt.IndexOf("no disqualification", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        mt.IndexOf("street fight", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        mt.IndexOf("hardcore", StringComparison.OrdinalIgnoreCase) >= 0)
                        score += 0.7f;
                }
            }

            rawScore += score;
        }

        // Time span and last interaction
        if (lastDate == DateTime.MinValue)
        {
            r.lastInteractionDate = null;
            r.feudScore = Mathf.Max(0f, rawScore);
            return;
        }

        r.lastInteractionDate = lastDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (firstDate == DateTime.MaxValue) firstDate = lastDate;
        double days = (lastDate - firstDate).TotalDays;
        float spanMultiplier = 1f + Mathf.Clamp01((float)(days / 365.0)); // up to 2x for very long feuds

        r.feudScore = Mathf.Max(0f, rawScore * spanMultiplier);
    }

    private void UpdateRivalrySummaryUI(RivalryData r)
    {
        if (r == null)
        {
            if (rivalryParticipantsLabel != null) rivalryParticipantsLabel.text = string.Empty;
            if (rivalryHeatLabel != null) rivalryHeatLabel.text = string.Empty;
            return;
        }

        // Participants summary
        string sideA = BuildRivalrySideLabel(r.participants.ElementAtOrDefault(0));
        string sideB = BuildRivalrySideLabel(r.participants.ElementAtOrDefault(1));
        if (rivalryParticipantsLabel != null)
        {
            if (!string.IsNullOrEmpty(sideA) || !string.IsNullOrEmpty(sideB))
                rivalryParticipantsLabel.text = $"Participants: {sideA} vs {sideB}";
            else
                rivalryParticipantsLabel.text = "Participants: (not set)";
        }

        // Heat / record summary
        if (rivalryHeatLabel != null)
        {
            var parts = new List<string>();
            parts.Add($"Heat: {r.feudScore:F1}");
            parts.Add($"Record A–B–D: {r.winsA}-{r.winsB}-{r.draws}");
            if (!string.IsNullOrEmpty(r.startDate) || !string.IsNullOrEmpty(r.lastInteractionDate))
            {
                string span;
                if (!string.IsNullOrEmpty(r.startDate) && !string.IsNullOrEmpty(r.lastInteractionDate))
                    span = $"{r.startDate} to {r.lastInteractionDate}";
                else
                    span = r.startDate ?? r.lastInteractionDate;
                parts.Add($"Span: {span}");
            }
            rivalryHeatLabel.text = string.Join("  |  ", parts);
        }
    }
    private void ShowAwardsPanel()
    {
        SetActivePanel(awardsPanel);
        // Default year to current if empty
        if (awardsYearField != null && string.IsNullOrWhiteSpace(awardsYearField.value))
            awardsYearField.value = DateTime.Today.Year.ToString(CultureInfo.InvariantCulture);
        ComputeAwardsForCurrentYear();
    }

    private string BuildRivalrySideLabel(string typedId)
    {
        if (string.IsNullOrEmpty(typedId) || typedId.Length < 3 || typedId[1] != ':')
            return string.Empty;

        char kind = typedId[0];
        string id = typedId.Substring(2);

        switch (kind)
        {
            case 'W':
                wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion?.promotionName);
                var w = wrestlerCollection?.wrestlers?.FirstOrDefault(x => string.Equals(x?.id, id, StringComparison.OrdinalIgnoreCase));
                return w?.name ?? string.Empty;
            case 'T':
                var tags = DataManager.LoadTagTeams(currentPromotion?.promotionName);
                var t = tags?.teams?.FirstOrDefault(x => string.Equals(x?.id, id, StringComparison.OrdinalIgnoreCase));
                if (t == null) return string.Empty;
                return string.IsNullOrEmpty(t.memberA) || string.IsNullOrEmpty(t.memberB)
                    ? t.teamName
                    : $"{t.teamName} ({t.memberA} & {t.memberB})";
            case 'S':
                var st = DataManager.LoadStables(currentPromotion?.promotionName);
                var s = st?.stables?.FirstOrDefault(x => string.Equals(x?.id, id, StringComparison.OrdinalIgnoreCase));
                if (s == null) return string.Empty;
                var names = new List<string>();
                wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion?.promotionName);
                foreach (var mid in s.memberIds ?? new List<string>())
                {
                    var mw = wrestlerCollection?.wrestlers?.FirstOrDefault(x => string.Equals(x?.id, mid, StringComparison.OrdinalIgnoreCase));
                    if (mw != null && !string.IsNullOrEmpty(mw.name)) names.Add(mw.name);
                }
                var members = names.Count > 0 ? $" ({string.Join(", ", names)})" : string.Empty;
                return s.stableName + members;
        }

        return string.Empty;
    }

    private void OnAddRivalry()
    {
        if (currentPromotion == null) { statusLabel.text = "No promotion loaded."; return; }
        rivalryCollection ??= new RivalryCollection { promotionName = currentPromotion.promotionName };
        if (rivalryCollection.rivalries == null) rivalryCollection.rivalries = new List<RivalryData>();
        EnsureRivalryTypeChoices();
        EnsureRivalryEventChoices();
        PopulateRivalryParticipantChoices(rivalryTypeDropdown?.value);
        var title = (rivalryNameField?.value ?? string.Empty).Trim();
        var type = rivalryTypeDropdown?.value ?? "Singles";
        var aName = (rivalryParticipantADropdown?.value ?? string.Empty).Trim();
        var bName = (rivalryParticipantBDropdown?.value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(aName) || string.IsNullOrEmpty(bName)) { statusLabel.text = "Enter title and both participants."; return; }
        if (!string.IsNullOrEmpty(aName) && string.Equals(aName, bName, StringComparison.OrdinalIgnoreCase)) { statusLabel.text = "Participants must differ."; return; }
        if (!string.IsNullOrEmpty(aName) && string.Equals(aName, bName, StringComparison.OrdinalIgnoreCase)) { statusLabel.text = "Participants must differ."; return; }
        var aId = ResolveTypedId(type, aName);
        var bId = ResolveTypedId(type, bName);
        if (string.IsNullOrEmpty(aId) || string.IsNullOrEmpty(bId) || string.Equals(aId, bId, StringComparison.OrdinalIgnoreCase)) { statusLabel.text = "Invalid participants."; return; }
        if (rivalryCollection.rivalries.Any(r => string.Equals(r.title, title, StringComparison.OrdinalIgnoreCase))) { statusLabel.text = "Rivalry title exists."; return; }
        var rNew = new RivalryData { title = title, type = type, status = "Active", startDate = DateTime.UtcNow.ToString("yyyy-MM-dd"), notes = rivalryNotesField?.value };
        rNew.participants.Add(aId); rNew.participants.Add(bId);
        rivalryCollection.rivalries.Add(rNew);
        DataManager.SaveRivalries(rivalryCollection);
        RefreshRivalryList();
        statusLabel.text = "Rivalry added.";
        ShowToast("Rivalry added.", false);
    }

    private void OnSaveRivalries()
    {
        if (currentPromotion == null || rivalryCollection == null) return;
        rivalryCollection.promotionName = currentPromotion.promotionName;
        DataManager.SaveRivalries(rivalryCollection);
        statusLabel.text = "Rivalries saved.";
        ShowToast("Rivalries saved.", false);
    }

    private void OnSaveSelectedRivalry()
    {
        if (rivalryCollection?.rivalries == null || selectedRivalryIndex < 0 || selectedRivalryIndex >= rivalryCollection.rivalries.Count) return;
        var r = rivalryCollection.rivalries[selectedRivalryIndex];
        var title = (rivalryNameField?.value ?? string.Empty).Trim();
        var type = rivalryTypeDropdown?.value ?? r.type;
        var aName = (rivalryParticipantADropdown?.value ?? string.Empty).Trim();
        var bName = (rivalryParticipantBDropdown?.value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(aName) || string.IsNullOrEmpty(bName)) { statusLabel.text = "Enter title and both participants."; return; }
        if (rivalryCollection.rivalries.Where((x, i) => i != selectedRivalryIndex).Any(x => string.Equals(x.title, title, StringComparison.OrdinalIgnoreCase))) { statusLabel.text = "Rivalry title exists."; return; }
        r.title = title; r.type = type; r.notes = rivalryNotesField?.value;
        r.participants.Clear();
        r.participants.Add(ResolveTypedId(type, aName));
        r.participants.Add(ResolveTypedId(type, bName));
        DataManager.SaveRivalries(rivalryCollection);
        RefreshRivalryList();
        statusLabel.text = "Rivalry updated.";
        ShowToast("Rivalry updated.", false);
    }

    private void OnDeleteSelectedRivalry()
    {
        if (rivalryCollection?.rivalries == null || selectedRivalryIndex < 0 || selectedRivalryIndex >= rivalryCollection.rivalries.Count) return;
        rivalryCollection.rivalries.RemoveAt(selectedRivalryIndex);
        selectedRivalryIndex = -1;
        DataManager.SaveRivalries(rivalryCollection);
        RefreshRivalryList();
        statusLabel.text = "Rivalry deleted.";
        ShowToast("Rivalry deleted.", false);
    }

    private void OnCancelEditRivalry()
    {
        selectedRivalryIndex = -1;
        if (rivalryNameField != null) rivalryNameField.value = string.Empty;
        if (rivalryNotesField != null) rivalryNotesField.value = string.Empty;
        SetActivePanel(rivalriesPanel);
    }

    private void OnAddRivalryEvent()
    {
        if (rivalryCollection?.rivalries == null || selectedRivalryIndex < 0 || selectedRivalryIndex >= rivalryCollection.rivalries.Count) { statusLabel.text = "Select a rivalry first."; return; }
        var r = rivalryCollection.rivalries[selectedRivalryIndex];
        EnsureRivalryEventChoices();
        // Gather inputs
        var sDate = (rivalryEventDateField?.value ?? string.Empty).Trim();
        if (!DateTime.TryParseExact(sDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            sDate = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
        var eType = rivalryEventTypeDropdown?.value ?? "Match";
        var outcome = rivalryEventOutcomeDropdown?.value ?? "NA";
        var rating = 0f; if (rivalryEventRatingField != null) rating = rivalryEventRatingField.value;
        var notes = rivalryEventNotesField?.value;

        // Build event
        var ev = new RivalryEvent
        {
            id = Guid.NewGuid().ToString("N"),
            date = sDate,
            eventType = eType,
            outcome = outcome,
            rating = rating,
            notes = notes,
            participants = new List<string>(r.participants)
        };
        // Link to selected show and entry if available
        var selShow = FindShowFromChoice(rivalryEventShowDropdown?.value);
        if (selShow != null) { if (string.IsNullOrEmpty(selShow.id)) selShow.id = Guid.NewGuid().ToString("N"); ev.showId = selShow.id; }
        var display = rivalryEventEntryDropdown?.value ?? string.Empty;
        string token = null;
        if (!string.IsNullOrEmpty(display) && rivalryEntryMap != null) rivalryEntryMap.TryGetValue(display, out token);
        if (!string.IsNullOrEmpty(token))
        {
            if (token.StartsWith("M:", StringComparison.OrdinalIgnoreCase)) ev.matchId = token.Substring(2);
            else if (token.StartsWith("S:", StringComparison.OrdinalIgnoreCase)) ev.segmentId = token.Substring(2);
        }
        r.events ??= new List<RivalryEvent>();
        r.events.Add(ev);

        // Recompute lightweight metrics & heat
        RecomputeRivalryMetrics(r);

        DataManager.SaveRivalries(rivalryCollection);
        PopulateRivalryEventsUI(r);
        UpdateRivalrySummaryUI(r);
        statusLabel.text = "Event added.";
    }

    private void OnOpenLinkedShow()
    {
        var selShow = FindShowFromChoice(rivalryEventShowDropdown?.value);
        if (selShow == null)
        {
            if (statusLabel != null) statusLabel.text = "Select a show to open.";
            return;
        }
        SetActivePanel(showsPanel);
        EnsureShowsListView();
        RefreshShowList();
        var shows = currentPromotion?.shows ?? new List<ShowData>();
        int idx = -1;
        for (int i = 0; i < shows.Count; i++)
        {
            if (string.Equals(shows[i]?.id ?? string.Empty, selShow.id, StringComparison.OrdinalIgnoreCase)) { idx = i; break; }
        }
        if (idx >= 0) SelectShow(idx);
        else if (statusLabel != null) statusLabel.text = "Show not found in list.";
    }
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
        // Validate references on the final show instance
        var targetShow = existing ?? show;
        string validation = ValidateShowReferences(targetShow);
        // Save promotion and update histories
        DataManager.SavePromotion(currentPromotion);
        TitleHistoryManager.UpdateShowResults(currentPromotion, show, prevName, prevDate);
        // Refresh calendar
        calendarView?.Refresh();
        if (!string.IsNullOrEmpty(validation) && statusLabel != null)
            statusLabel.text = validation;
        if (!string.IsNullOrEmpty(validation))
            ShowToast(validation, false);
        else
            ShowToast("Show saved.", false);
        SetActivePanel(calendarPanel);
    }

    private string ValidateShowReferences(ShowData show)
    {
        if (show == null || currentPromotion == null) return string.Empty;

        wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion.promotionName);
        titleCollection ??= DataManager.LoadTitles(currentPromotion.promotionName);

        var wrestlerNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in wrestlerCollection?.wrestlers ?? new List<WrestlerData>())
            if (!string.IsNullOrEmpty(w?.name)) wrestlerNames.Add(w.name.Trim());

        var titleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in titleCollection?.titles ?? new List<TitleData>())
            if (!string.IsNullOrEmpty(t?.titleName)) titleNames.Add(t.titleName.Trim());

        var brands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        currentPromotion.brands ??= new List<string>();
        foreach (var b in currentPromotion.brands)
            if (!string.IsNullOrWhiteSpace(b)) brands.Add(b.Trim());

        int missingWrestlers = 0;
        int unknownTitleFlags = 0;
        bool brandAdded = false;

        // Brand: if show uses a brand not on the promotion, auto-add it
        if (!string.IsNullOrWhiteSpace(show.brand) && !brands.Contains(show.brand.Trim()))
        {
            currentPromotion.brands.Add(show.brand.Trim());
            brandAdded = true;
        }

        void CheckWrestler(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (!wrestlerNames.Contains(name.Trim())) missingWrestlers++;
        }

        if (show.matches != null)
        {
            foreach (var m in show.matches)
            {
                if (m == null) continue;
                CheckWrestler(m.wrestlerA);
                CheckWrestler(m.wrestlerB);
                CheckWrestler(m.wrestlerC);
                CheckWrestler(m.wrestlerD);

                if (m.isTitleMatch)
                {
                    if (string.IsNullOrWhiteSpace(m.titleName) || !titleNames.Contains(m.titleName.Trim()))
                    {
                        // Clear flag but keep the display name so history remains readable
                        m.isTitleMatch = false;
                        unknownTitleFlags++;
                    }
                }
            }
        }

        if (!brandAdded && missingWrestlers == 0 && unknownTitleFlags == 0)
            return "Show saved (all references valid).";

        var parts = new List<string> { "Show saved with notes:" };
        if (brandAdded) parts.Add("brand added to promotion.");
        if (missingWrestlers > 0) parts.Add($"{missingWrestlers} wrestler name(s) not in roster.");
        if (unknownTitleFlags > 0) parts.Add($"{unknownTitleFlags} title flag(s) cleared for missing titles.");
        return string.Join(" ", parts);
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
        InvalidatePromotionStats();
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
        var division = newWrestlerDivisionDropdown != null ? (newWrestlerDivisionDropdown.value ?? string.Empty).Trim() : string.Empty;
        var newW = new WrestlerData(name) { isFemale = isFemale, isTagTeam = false, division = division };
        wrestlerCollection.wrestlers ??= new List<WrestlerData>();
        wrestlerCollection.wrestlers.Add(newW);

        DataManager.SaveWrestlers(wrestlerCollection);
        RefreshWrestlerList();
        if (newWrestlerField != null) newWrestlerField.value = string.Empty;
        if (newWrestlerIsFemaleToggle != null) newWrestlerIsFemaleToggle.value = false;
        if (newWrestlerDivisionDropdown != null) newWrestlerDivisionDropdown.value = string.Empty;
        
        if (statusLabel != null) statusLabel.text = "Wrestler added.";
    }

    private void OnSaveWrestlers()
    {
        if (currentPromotion == null || wrestlerCollection == null) return;
        wrestlerCollection.promotionName = currentPromotion.promotionName;
        DataManager.SaveWrestlers(wrestlerCollection);
        if (statusLabel != null) statusLabel.text = "Wrestlers saved.";
        ShowToast("Wrestlers saved.", false);
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
        if (wrestlerDivisionDropdown != null)
        {
            var dv = w.division ?? string.Empty;
            if (!string.IsNullOrEmpty(dv) && wrestlerDivisionDropdown.choices != null && !wrestlerDivisionDropdown.choices.Contains(dv))
            {
                var list = new System.Collections.Generic.List<string>(wrestlerDivisionDropdown.choices);
                list.Add(dv);
                wrestlerDivisionDropdown.choices = list;
            }
            wrestlerDivisionDropdown.value = dv;
        }
        UpdateWrestlerCareerSummary(w);
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
        if (wrestlerDivisionDropdown != null) w.division = (wrestlerDivisionDropdown.value ?? string.Empty).Trim();
        DataManager.SaveWrestlers(wrestlerCollection);
        RefreshWrestlerList();
        // After saving, return to add mode: hide details, show add panel
        if (wrestlerDetails != null) wrestlerDetails.AddToClassList("hidden");
        selectedWrestlerIndex = -1;
        if (wrestlerAddPanel != null) wrestlerAddPanel.RemoveFromClassList("hidden");
        if (statusLabel != null) statusLabel.text = "Wrestler updated.";
        FocusPanel(wrestlersPanel);
    }

    private void UpdateWrestlerCareerSummary(WrestlerData wrestler)
    {
        if (wrestler == null || currentPromotion == null)
            return;

        try
        {
            TitleHistoryManager.EnsureHistoryLoaded(currentPromotion);
            var allMatches = TitleHistoryManager.GetAllMatchResults(currentPromotion.promotionName) ?? new List<MatchResultData>();
            var lineages = TitleHistoryManager.GetTitleLineages(currentPromotion.promotionName) ?? new List<TitleLineageData>();

            // Filter matches involving this wrestler (by current name)
            var name = wrestler.name ?? string.Empty;
            bool Involves(MatchResultData m)
                => m != null &&
                   (!string.IsNullOrEmpty(m.wrestlerA) && StringEquals(m.wrestlerA, name) ||
                    !string.IsNullOrEmpty(m.wrestlerB) && StringEquals(m.wrestlerB, name));

            var matches = allMatches.Where(Involves).ToList();

            int wins = 0, losses = 0, draws = 0;
            DateTime first = DateTime.MinValue, last = DateTime.MinValue;

            foreach (var m in matches)
            {
                if (!string.IsNullOrEmpty(m.date) && DateTime.TryParse(m.date, out var d))
                {
                    if (first == DateTime.MinValue || d < first) first = d;
                    if (last == DateTime.MinValue || d > last) last = d;
                }

                var winner = m.winner ?? string.Empty;
                if (string.Equals(winner, "Draw", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(winner, "No Contest", StringComparison.OrdinalIgnoreCase))
                {
                    draws++;
                }
                else if (StringEquals(winner, name))
                {
                    wins++;
                }
                else if (!string.IsNullOrEmpty(winner))
                {
                    // Count loss if someone else is listed as winner
                    losses++;
                }
            }

            int total = wins + losses + draws;
            if (wrestlerCareerRecordLabel != null)
            {
                wrestlerCareerRecordLabel.text = total > 0
                    ? $"Record: {wins} W - {losses} L - {draws} D ({total} matches)"
                    : "No matches recorded yet.";
            }

            if (wrestlerCareerSpanLabel != null)
            {
                if (total == 0 || first == DateTime.MinValue)
                    wrestlerCareerSpanLabel.text = string.Empty;
                else if (last == DateTime.MinValue || last == first)
                    wrestlerCareerSpanLabel.text = $"Active: {first:MM/dd/yyyy}";
                else
                    wrestlerCareerSpanLabel.text = $"Active: {first:MM/dd/yyyy} - {last:MM/dd/yyyy}";
            }

            // Titles held: collect unique titles where this wrestler appears as champion
            var titlesHeld = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            int totalReigns = 0;
            foreach (var lineage in lineages)
            {
                if (lineage?.reigns == null) continue;
                bool anyForThis = false;
                foreach (var reign in lineage.reigns)
                {
                    if (reign != null && StringEquals(reign.championName, name))
                    {
                        anyForThis = true;
                        totalReigns++;
                    }
                }
                if (anyForThis && !string.IsNullOrEmpty(lineage.titleName))
                    titlesHeld.Add(lineage.titleName);
            }

            if (wrestlerCareerTitlesLabel != null)
            {
                if (titlesHeld.Count == 0)
                {
                    wrestlerCareerTitlesLabel.text = "Titles held: none recorded.";
                }
                else
                {
                    var titlesText = string.Join(", ", titlesHeld.OrderBy(t => t));
                    wrestlerCareerTitlesLabel.text = totalReigns > 0
                        ? $"Titles held: {titlesText} ({totalReigns} total reigns)"
                        : $"Titles held: {titlesText}";
                }
            }

            // Recent matches list (most recent first, capped)
            if (wrestlerCareerMatchesList != null)
            {
                wrestlerCareerMatchesList.Clear();
                const int maxMatches = 15;
                var ordered = matches
                    .OrderByDescending(m =>
                    {
                        if (!string.IsNullOrEmpty(m.date) && DateTime.TryParse(m.date, out var d))
                            return d;
                        return DateTime.MinValue;
                    })
                    .ThenByDescending(m => m.showName)
                    .ThenByDescending(m => m.matchName)
                    .Take(maxMatches);

                foreach (var m in ordered)
                {
                    var row = new VisualElement();
                    var dateLabel = !string.IsNullOrEmpty(m.date) ? m.date : "Unknown date";
                    var showPart = string.IsNullOrEmpty(m.showName) ? string.Empty : $"{m.showName} - ";
                    var matchName = string.IsNullOrEmpty(m.matchName) ? "(Match)" : m.matchName;
                    row.Add(new Label($"{dateLabel}  |  {showPart}{matchName}"));

                    string outcome;
                    var winner = m.winner ?? string.Empty;
                    if (string.Equals(winner, "Draw", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(winner, "No Contest", StringComparison.OrdinalIgnoreCase))
                    {
                        outcome = winner;
                    }
                    else if (StringEquals(winner, name))
                    {
                        outcome = "Result: Win";
                    }
                    else if (!string.IsNullOrEmpty(winner))
                    {
                        outcome = $"Result: Loss (winner: {winner})";
                    }
                    else
                    {
                        outcome = "Result: (unknown)";
                    }

                    if (m.isTitleMatch && !string.IsNullOrEmpty(m.titleInvolved))
                    {
                        outcome += $"  |  Title: {m.titleInvolved}";
                    }

                    row.Add(new Label(outcome));
                    row.style.marginBottom = 4;
                    wrestlerCareerMatchesList.Add(row);
                }
            }

            if (wrestlerCareerPanel != null)
                wrestlerCareerPanel.RemoveFromClassList("hidden");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to update wrestler career summary: {ex.Message}");
        }
    }

    private void RefreshBrandDropdowns()
    {
        var brands = currentPromotion?.brands ?? new List<string>();

        // Shows: existing and new
        List<string> MakeBrandChoices()
        {
            var list = new List<string> { "" };
            list.AddRange(brands.Where(b => !string.IsNullOrWhiteSpace(b)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(b => b));
            return list;
        }

        var showChoices = MakeBrandChoices();
        if (showBrandDropdown != null)
        {
            showBrandDropdown.choices = showChoices;
            if (!showChoices.Contains(showBrandDropdown.value))
                showBrandDropdown.value = "";
        }
        if (newShowBrandDropdown != null)
        {
            newShowBrandDropdown.choices = showChoices;
            if (!showChoices.Contains(newShowBrandDropdown.value))
                newShowBrandDropdown.value = "";
        }

        // History filter
        if (historyBrandDropdown != null)
        {
            var histChoices = new List<string> { "All Brands" };
            histChoices.AddRange(brands.Where(b => !string.IsNullOrWhiteSpace(b)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(b => b));
            historyBrandDropdown.choices = histChoices;
            if (!histChoices.Contains(historyBrandDropdown.value))
                historyBrandDropdown.value = "All Brands";
        }

        // Rankings filter
        if (rankingsBrandDropdown != null)
        {
            var rankChoices = new List<string> { "All Brands" };
            rankChoices.AddRange(brands.Where(b => !string.IsNullOrWhiteSpace(b)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(b => b));
            rankingsBrandDropdown.choices = rankChoices;
            if (!rankChoices.Contains(rankingsBrandDropdown.value))
                rankingsBrandDropdown.value = "All Brands";
        }

        // Calendar filter
        if (calendarBrandDropdown != null)
        {
            var calChoices = new List<string> { "All Brands" };
            calChoices.AddRange(brands.Where(b => !string.IsNullOrWhiteSpace(b)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(b => b));
            calendarBrandDropdown.choices = calChoices;
            if (!calChoices.Contains(calendarBrandDropdown.value))
                calendarBrandDropdown.value = "All Brands";
        }

        // Calendar generation brand (optional, per-template)
        if (calendarGenerationBrandDropdown != null)
        {
            var genChoices = new List<string> { "" };
            genChoices.AddRange(brands.Where(b => !string.IsNullOrWhiteSpace(b)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(b => b));
            calendarGenerationBrandDropdown.choices = genChoices;
            if (!genChoices.Contains(calendarGenerationBrandDropdown.value))
                calendarGenerationBrandDropdown.value = "";
        }
    }

    // ----- Awards & Accolades -----

    private int GetSelectedAwardsYear()
    {
        if (awardsYearField == null || string.IsNullOrWhiteSpace(awardsYearField.value))
            return DateTime.Today.Year;
        if (int.TryParse(awardsYearField.value.Trim(), out var y) && y > 1900 && y < 3000)
            return y;
        return DateTime.Today.Year;
    }

    private void OnComputeAwardsClicked()
    {
        ComputeAwardsForCurrentYear();
    }

    private void ComputeAwardsForCurrentYear()
    {
        if (currentPromotion == null) return;
        int year = GetSelectedAwardsYear();

        // Wrestler of the Year suggestion
        string woty = SuggestWrestlerOfYear(year);
        if (wotySuggestionLabel != null)
            wotySuggestionLabel.text = string.IsNullOrEmpty(woty) ? "(no suggestion)" : woty;

        // Match of the Year suggestion
        string moty = SuggestMatchOfYear(year);
        if (motySuggestionLabel != null)
            motySuggestionLabel.text = string.IsNullOrEmpty(moty) ? "(no suggestion)" : moty;

        // Feud of the Year suggestion
        string feud = SuggestFeudOfYear(year);
        if (feudSuggestionLabel != null)
            feudSuggestionLabel.text = string.IsNullOrEmpty(feud) ? "(no suggestion)" : feud;

        // Tag Team of the Year suggestion
        string tag = SuggestTagTeamOfYear(year);
        if (tagTeamSuggestionLabel != null)
            tagTeamSuggestionLabel.text = string.IsNullOrEmpty(tag) ? "(no suggestion)" : tag;

        // Milestones for this year
        PopulateMilestonesForYear(year);

        LoadAwardsOverrides(year);
    }

    private void PopulateMilestonesForYear(int year)
    {
        if (milestonesList == null)
            return;

        milestonesList.Clear();
        if (currentPromotion == null)
        {
            milestonesList.Add(new Label("No promotion loaded."));
            return;
        }

        var milestones = MilestoneEngine.ComputeYearlyMilestones(currentPromotion, year) ?? new System.Collections.Generic.List<Milestone>();
        if (milestones.Count == 0)
        {
            milestonesList.Add(new Label("No milestones detected for this year."));
            return;
        }

        foreach (var m in milestones)
        {
            string dateText = m.date;
            if (CalendarUtils.TryParseAny(m.date, out var d))
                dateText = d.ToString("MM/dd/yyyy");

            string line = dateText + " - " + (m.description ?? string.Empty);
            milestonesList.Add(new Label(line));
        }
    }

    private string MakeAwardKey(string award, int year)
    {
        var promo = currentPromotion?.promotionName ?? "UnknownPromotion";
        return $"Awards::{promo}::{year}::{award}";
    }

    private void LoadAwardsOverrides(int year)
    {
        if (wotyOverrideField != null)
            wotyOverrideField.value = PlayerPrefs.GetString(MakeAwardKey("WrestlerOfYear", year), string.Empty);
        if (motyOverrideField != null)
            motyOverrideField.value = PlayerPrefs.GetString(MakeAwardKey("MatchOfYear", year), string.Empty);
        if (feudOverrideField != null)
            feudOverrideField.value = PlayerPrefs.GetString(MakeAwardKey("FeudOfYear", year), string.Empty);
        if (tagTeamOverrideField != null)
            tagTeamOverrideField.value = PlayerPrefs.GetString(MakeAwardKey("TagTeamOfYear", year), string.Empty);
    }

    private void OnSaveAwardsClicked()
    {
        if (currentPromotion == null) return;
        int year = GetSelectedAwardsYear();

        if (wotyOverrideField != null)
            PlayerPrefs.SetString(MakeAwardKey("WrestlerOfYear", year), wotyOverrideField.value ?? string.Empty);
        if (motyOverrideField != null)
            PlayerPrefs.SetString(MakeAwardKey("MatchOfYear", year), motyOverrideField.value ?? string.Empty);
        if (feudOverrideField != null)
            PlayerPrefs.SetString(MakeAwardKey("FeudOfYear", year), feudOverrideField.value ?? string.Empty);
        if (tagTeamOverrideField != null)
            PlayerPrefs.SetString(MakeAwardKey("TagTeamOfYear", year), tagTeamOverrideField.value ?? string.Empty);

        PlayerPrefs.Save();
        if (statusLabel != null) statusLabel.text = $"Awards saved for {year}.";
    }

    private string SuggestWrestlerOfYear(int year)
    {
        try
        {
            var history = TitleHistoryManager.GetAllMatchResults(currentPromotion.promotionName) ?? new List<MatchResultData>();
            var cutoffStart = new DateTime(year, 1, 1);
            var cutoffEnd = new DateTime(year, 12, 31);
            var wins = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var matches = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var r in history)
            {
                if (!CalendarUtils.TryParseAny(r.date, out var d)) continue;
                if (d < cutoffStart || d > cutoffEnd) continue;

                void addMatch(string n)
                {
                    if (string.IsNullOrWhiteSpace(n)) return;
                    var key = n.Trim();
                    matches.TryGetValue(key, out var m); matches[key] = m + 1;
                }

                addMatch(r.wrestlerA);
                addMatch(r.wrestlerB);

                var w = r.winner ?? string.Empty;
                if (!string.IsNullOrEmpty(w) &&
                    !string.Equals(w, "Draw", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(w, "No Contest", StringComparison.OrdinalIgnoreCase))
                {
                    var key = w.Trim();
                    wins.TryGetValue(key, out var c); wins[key] = c + 1;
                }
            }

            string best = null;
            float bestScore = 0f;
            foreach (var kv in matches)
            {
                wins.TryGetValue(kv.Key, out var w);
                var m = kv.Value;
                float score = w * 3f + m * 1f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = kv.Key;
                }
            }

            return best;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"SuggestWrestlerOfYear failed: {ex.Message}");
            return string.Empty;
        }
    }

    private string SuggestMatchOfYear(int year)
    {
        try
        {
            var history = TitleHistoryManager.GetAllMatchResults(currentPromotion.promotionName) ?? new List<MatchResultData>();
            var shows = currentPromotion?.shows ?? new List<ShowData>();
            var cutoffStart = new DateTime(year, 1, 1);
            var cutoffEnd = new DateTime(year, 12, 31);

            MatchResultData bestMatch = null;
            float bestScore = float.MinValue;

            foreach (var r in history)
            {
                if (!CalendarUtils.TryParseAny(r.date, out var d)) continue;
                if (d < cutoffStart || d > cutoffEnd) continue;

                var show = shows.FirstOrDefault(s => string.Equals(s?.showName, r.showName, StringComparison.OrdinalIgnoreCase) &&
                                                     string.Equals(s?.date, r.date, StringComparison.OrdinalIgnoreCase));
                float baseRating = show?.rating ?? 0f;
                float score = baseRating;
                if (r.isTitleMatch) score += 1.0f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = r;
                }
            }

            if (bestMatch == null) return string.Empty;
            return string.IsNullOrEmpty(bestMatch.showName)
                ? $"{bestMatch.date} - {bestMatch.matchName}"
                : $"{bestMatch.showName} ({bestMatch.date}) - {bestMatch.matchName}";
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"SuggestMatchOfYear failed: {ex.Message}");
            return string.Empty;
        }
    }

    private string SuggestFeudOfYear(int year)
    {
        try
        {
            rivalryCollection ??= DataManager.LoadRivalries(currentPromotion?.promotionName);
            if (rivalryCollection?.rivalries == null || rivalryCollection.rivalries.Count == 0) return string.Empty;

            var cutoffStart = new DateTime(year, 1, 1);
            var cutoffEnd = new DateTime(year, 12, 31);
            RivalryData best = null;
            float bestScore = float.MinValue;

            foreach (var r in rivalryCollection.rivalries)
            {
                if (r == null || r.events == null || r.events.Count == 0) continue;

                // Filter events to this year and build a temporary metric
                float raw = 0f;
                foreach (var e in r.events)
                {
                    if (!DateTime.TryParse(e.date, out var d)) continue;
                    if (d < cutoffStart || d > cutoffEnd) continue;
                    float score = 1f + Mathf.Max(0f, e.rating);
                    if ((e.eventType ?? string.Empty).IndexOf("Match", StringComparison.OrdinalIgnoreCase) >= 0) score += 1.5f;
                    raw += score;
                }
                if (raw <= 0f) continue;

                if (raw > bestScore)
                {
                    bestScore = raw;
                    best = r;
                }
            }

            return best?.title ?? string.Empty;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"SuggestFeudOfYear failed: {ex.Message}");
            return string.Empty;
        }
    }

    private string SuggestTagTeamOfYear(int year)
    {
        try
        {
            var tags = DataManager.LoadTagTeams(currentPromotion?.promotionName);
            if (tags?.teams == null || tags.teams.Count == 0) return string.Empty;

            var history = TitleHistoryManager.GetAllMatchResults(currentPromotion.promotionName) ?? new List<MatchResultData>();
            var cutoffStart = new DateTime(year, 1, 1);
            var cutoffEnd = new DateTime(year, 12, 31);

            var winCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in history)
            {
                if (!CalendarUtils.TryParseAny(r.date, out var d)) continue;
                if (d < cutoffStart || d > cutoffEnd) continue;

                var w = r.winner ?? string.Empty;
                if (string.IsNullOrWhiteSpace(w) ||
                    string.Equals(w, "Draw", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(w, "No Contest", StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var t in tags.teams)
                {
                    if (t == null || string.IsNullOrEmpty(t.teamName)) continue;
                    if (string.Equals(t.teamName, w, StringComparison.OrdinalIgnoreCase))
                    {
                        winCounts.TryGetValue(t.teamName, out var c);
                        winCounts[t.teamName] = c + 1;
                        break;
                    }
                }
            }

            if (winCounts.Count == 0) return string.Empty;
            return winCounts.OrderByDescending(kv => kv.Value).First().Key;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"SuggestTagTeamOfYear failed: {ex.Message}");
            return string.Empty;
        }
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

    private void OnAddBrand()
    {
        if (currentPromotion == null) return;
        var raw = newBrandField != null ? (newBrandField.value ?? string.Empty).Trim() : string.Empty;
        if (string.IsNullOrEmpty(raw))
        {
            if (statusLabel != null) statusLabel.text = "Enter a brand name.";
            return;
        }

        currentPromotion.brands ??= new List<string>();
        if (currentPromotion.brands.Any(b => string.Equals(b, raw, StringComparison.OrdinalIgnoreCase)))
        {
            if (statusLabel != null) statusLabel.text = "Brand already exists.";
            return;
        }

        currentPromotion.brands.Add(raw);
        DataManager.SavePromotion(currentPromotion);
        if (newBrandField != null) newBrandField.value = string.Empty;
        RefreshBrandList();
        RefreshBrandDropdowns();
        if (statusLabel != null) statusLabel.text = "Brand added.";
    }

    private void OnMinimizeClicked()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        try
        {
            IntPtr handle = GetActiveWindow();
            if (handle != IntPtr.Zero)
            {
                ShowWindow(handle, 6); // SW_MINIMIZE
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to minimize window: {ex.Message}");
        }
#else
        Debug.Log("Minimize is only supported in Windows standalone builds.");
#endif
    }

    private void ShowToast(string message, bool isError = false, float duration = 2.5f)
    {
        if (toastBar == null || toastLabel == null)
        {
            if (statusLabel != null) statusLabel.text = message;
            return;
        }
        toastLabel.text = message ?? string.Empty;
        toastBar.RemoveFromClassList("hidden");
        toastBar.RemoveFromClassList("toast-success");
        toastBar.RemoveFromClassList("toast-error");
        toastBar.AddToClassList(isError ? "toast-error" : "toast-success");
        toastBar.BringToFront();

        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(HideToastAfter(duration));
    }

    private IEnumerator HideToastAfter(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        if (toastBar != null)
            toastBar.AddToClassList("hidden");
        toastRoutine = null;
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
#endif

    private void RefreshBrandList()
    {
        if (brandListScroll == null) return;
        brandListScroll.Clear();
        var brands = currentPromotion?.brands ?? new List<string>();
        var shows = currentPromotion?.shows ?? new List<ShowData>();

        if (brands.Count == 0)
        {
            brandListScroll.Add(new Label("No brands defined."));
            InvalidatePromotionStats();
            return;
        }

        foreach (var b in brands.OrderBy(x => x))
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 4;

            int count = shows.Count(s => s != null && !string.IsNullOrEmpty(s.brand) && StringEquals(s.brand, b));
            var label = new Label(count > 0 ? $"{b} ({count} shows)" : b);
            label.style.flexGrow = 1;

            var deleteBtn = new Button(() => OnDeleteBrand(b)) { text = "Delete" };

            row.Add(label);
            row.Add(deleteBtn);
            brandListScroll.Add(row);
        }
        InvalidatePromotionStats();
    }

    private void OnDeleteBrand(string brand)
    {
        if (currentPromotion == null || string.IsNullOrEmpty(brand)) return;
        if (currentPromotion.brands == null) return;
        currentPromotion.brands.RemoveAll(b => StringEquals(b, brand));

        // Clear brand from shows that used it
        if (currentPromotion.shows != null)
        {
            foreach (var s in currentPromotion.shows)
            {
                if (s != null && !string.IsNullOrEmpty(s.brand) && StringEquals(s.brand, brand))
                    s.brand = null;
            }
        }

        DataManager.SavePromotion(currentPromotion);
        RefreshBrandList();
        RefreshBrandDropdowns();
        if (statusLabel != null) statusLabel.text = "Brand deleted.";
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
        InvalidatePromotionStats();
    }

    private void RefreshTeamMemberChoices()
    {
        if (teamMemberADropdown == null || teamMemberBDropdown == null) return;
        wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion?.promotionName);
        var names = wrestlerCollection?.wrestlers?
            .Where(w => !string.IsNullOrEmpty(w?.name))
            ?.Select(w => w.name)
            .OrderBy(n => n)
            .ToList() ?? new List<string>();
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
        if (teamDivisionDropdown != null)
        {
            var dv = t.division ?? string.Empty;
            if (!string.IsNullOrEmpty(dv) && teamDivisionDropdown.choices != null && !teamDivisionDropdown.choices.Contains(dv))
            {
                var list = new System.Collections.Generic.List<string>(teamDivisionDropdown.choices);
                list.Add(dv);
                teamDivisionDropdown.choices = list;
            }
            teamDivisionDropdown.value = dv;
        }
        FocusPanel(tagTeamsPanel ?? titlesPanel);
    }

    private void OnAddTeam()
    {
        if (currentPromotion == null) { statusLabel.text = "No promotion loaded."; return; }
        tagTeamCollection ??= new TagTeamCollection { promotionName = currentPromotion.promotionName };
        var name = (teamNameField?.value ?? string.Empty).Trim();
        var a = (teamMemberADropdown?.value ?? string.Empty).Trim();
        var b = (teamMemberBDropdown?.value ?? string.Empty).Trim();
        var division = teamDivisionDropdown != null ? (teamDivisionDropdown.value ?? string.Empty).Trim() : string.Empty;
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) || string.Equals(a,b,System.StringComparison.OrdinalIgnoreCase))
        { statusLabel.text = "Enter a team name and two distinct members."; return; }
        if (tagTeamCollection.teams == null) tagTeamCollection.teams = new List<TagTeamData>();
        if (tagTeamCollection.teams.Any(t => string.Equals(t.teamName, name, System.StringComparison.OrdinalIgnoreCase)))
        { statusLabel.text = "Team name already exists."; return; }
        if (tagTeamCollection.teams.Any(t => SameMembers(t.memberA, t.memberB, a, b)))
        { statusLabel.text = "A team with these members already exists."; return; }
        tagTeamCollection.teams.Add(new TagTeamData { teamName = name, memberA = a, memberB = b, active = true, division = division });
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
        ShowToast("Teams saved.", false);
    }

    private void OnSaveSelectedTeam()
    {
        if (tagTeamCollection?.teams == null || selectedTeamIndex < 0 || selectedTeamIndex >= tagTeamCollection.teams.Count) return;
        var t = tagTeamCollection.teams[selectedTeamIndex];
        var name = (teamNameField?.value ?? string.Empty).Trim();
        var a = (teamMemberADropdown?.value ?? string.Empty).Trim();
        var b = (teamMemberBDropdown?.value ?? string.Empty).Trim();
        var division = teamDivisionDropdown != null ? (teamDivisionDropdown.value ?? string.Empty).Trim() : string.Empty;
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) || string.Equals(a,b,System.StringComparison.OrdinalIgnoreCase))
        { statusLabel.text = "Enter a team name and two distinct members."; return; }
        if (tagTeamCollection.teams.Where((x, i) => i != selectedTeamIndex).Any(x => string.Equals(x.teamName, name, System.StringComparison.OrdinalIgnoreCase)))
        { statusLabel.text = "Team name already exists."; return; }
        if (tagTeamCollection.teams.Where((x, i) => i != selectedTeamIndex).Any(x => SameMembers(x.memberA, x.memberB, a, b)))
        { statusLabel.text = "A team with these members already exists."; return; }
        t.teamName = name; t.memberA = a; t.memberB = b; t.division = division;
        DataManager.SaveTagTeams(tagTeamCollection);
        RefreshTagTeamList();
        statusLabel.text = "Team updated.";
        ShowToast("Team updated.", false);
    }

    private void OnDeleteSelectedTeam()
    {
        if (tagTeamCollection?.teams == null || selectedTeamIndex < 0 || selectedTeamIndex >= tagTeamCollection.teams.Count) return;
        tagTeamCollection.teams.RemoveAt(selectedTeamIndex);
        selectedTeamIndex = -1;
        DataManager.SaveTagTeams(tagTeamCollection);
        RefreshTagTeamList();
        statusLabel.text = "Team deleted.";
        ShowToast("Team deleted.", false);
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
        InvalidatePromotionStats();
    }

    private void SelectTitle(int index)
    {
        if (titleCollection == null || index < 0 || index >= (titleCollection.titles?.Count ?? 0)) return;
        selectedTitleIndex = index;
        var t = titleCollection.titles[index];
        if (titleNameField != null) titleNameField.value = t.titleName;
        if (titleDivisionField != null) titleDivisionField.value = t.division;
        EnsureRosterAndTitlesLoaded();
        // Populate champion dropdown choices with individual wrestlers (alphabetical)
        var champNames = wrestlerCollection?.wrestlers?
            .Where(w => w != null && !string.IsNullOrEmpty(w.name) && !w.isTagTeam)
            .Select(w => w.name)
            .OrderBy(n => n)
            .ToList() ?? new List<string>();
        if (!champNames.Contains(string.Empty)) champNames.Insert(0, string.Empty);
        SetChoices(titleChampionDropdown, champNames);
        SetChoices(titleTagChampionADropdown, champNames);
        SetChoices(titleTagChampionBDropdown, champNames);

        // Apply current champion values based on singles/tag-team mode
        string current = t.currentChampion ?? string.Empty;
        if (titleIsTagTeamToggle != null) titleIsTagTeamToggle.value = t.isTagTeamTitle;

        if (t.isTagTeamTitle)
        {
            // Parse stored "A & B" format into two names
            string aName = string.Empty, bName = string.Empty;
            if (!string.IsNullOrEmpty(current))
            {
                var parts = current.Split('&');
                if (parts.Length > 0) aName = parts[0].Trim();
                if (parts.Length > 1) bName = parts[1].Trim();
            }

            if (titleTagChampionADropdown != null && titleTagChampionADropdown.choices != null && titleTagChampionADropdown.choices.Contains(aName))
                titleTagChampionADropdown.value = aName;
            if (titleTagChampionBDropdown != null && titleTagChampionBDropdown.choices != null && titleTagChampionBDropdown.choices.Contains(bName))
                titleTagChampionBDropdown.value = bName;

            if (titleChampionDropdown != null) titleChampionDropdown.value = string.Empty;
        }
        else
        {
            if (titleChampionDropdown != null && titleChampionDropdown.choices != null && titleChampionDropdown.choices.Contains(current))
                titleChampionDropdown.value = current;
            if (titleTagChampionADropdown != null) titleTagChampionADropdown.value = string.Empty;
            if (titleTagChampionBDropdown != null) titleTagChampionBDropdown.value = string.Empty;
        }

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
            titleStatsCurrentLabel.text = current != null ? $"Current champion: {current.championName} - {current.daysHeld} days" : "Current champion: (unknown)";
        if (titleStatsSummaryLabel != null)
            titleStatsSummaryLabel.text = totalReigns > 0 ? $"Reigns: {totalReigns}   Total defenses: {totalDefenses}" : "No stats yet.";
        var longest = summaries.OrderByDescending(s => s.daysHeld).FirstOrDefault();
        var shortest = summaries.OrderBy(s => s.daysHeld).FirstOrDefault();
        if (titleStatsLongestLabel != null)
        {
            titleStatsLongestLabel.text = longest != null ?
                $"Longest: {longest.championName} - {longest.daysHeld} days ({SpanText(longest)})" : string.Empty;
        }
        if (titleStatsShortestLabel != null)
        {
            titleStatsShortestLabel.text = shortest != null ?
                $"Shortest: {shortest.championName} - {shortest.daysHeld} days ({SpanText(shortest)})" : string.Empty;
        }
        var mostDef = summaries.OrderByDescending(s => s.defenses).FirstOrDefault();
        if (titleStatsMostDefensesLabel != null)
        {
            titleStatsMostDefensesLabel.text = mostDef != null ?
                $"Most defenses in a reign: {mostDef.championName} - {mostDef.defenses}" : string.Empty;
        }

        string SpanText(TitleReignSummary s)
        {
            return string.IsNullOrEmpty(s.dateLost) ? $"{s.dateWon} - present" : $"{s.dateWon} - {s.dateLost}";
        }
    }

    private void OnAddTitle()
    {
        if (currentPromotion == null) { if (statusLabel != null) statusLabel.text = "No promotion loaded."; return; }
        var name = newTitleField != null ? (newTitleField.value ?? string.Empty).Trim() : string.Empty;
        if (string.IsNullOrEmpty(name)) { if (statusLabel != null) statusLabel.text = "Enter a title name."; return; }
        var division = newTitleDivisionField != null ? (newTitleDivisionField.value ?? string.Empty).Trim() : string.Empty;

        titleCollection ??= DataManager.LoadTitles(currentPromotion.promotionName) ?? new TitleCollection { promotionName = currentPromotion.promotionName };
        if (titleCollection.promotionName != currentPromotion.promotionName)
            titleCollection.promotionName = currentPromotion.promotionName;

        // Prevent duplicate title names (case-insensitive)
        if (titleCollection.titles != null && titleCollection.titles.Any(t => !string.IsNullOrEmpty(t?.titleName) && string.Equals(t.titleName.Trim(), name, System.StringComparison.OrdinalIgnoreCase)))
        {
            if (statusLabel != null) statusLabel.text = "Title already exists.";
            return;
        }

        var tNew = new TitleData { titleName = name, division = division };
        titleCollection.titles ??= new List<TitleData>();
        titleCollection.titles.Add(tNew);

        DataManager.SaveTitles(titleCollection);
        RefreshDivisionChoices();
        RefreshTitleList();
        if (newTitleField != null) newTitleField.value = string.Empty;
        if (newTitleDivisionField != null) newTitleDivisionField.value = string.Empty;
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
        RefreshDivisionChoices();
        if (statusLabel != null) statusLabel.text = "Titles saved.";
        ShowToast("Titles saved.", false);
    }

    private void OnSaveSelectedTitle()
    {
        if (titleCollection?.titles == null || selectedTitleIndex < 0 || selectedTitleIndex >= titleCollection.titles.Count) return;
        var t = titleCollection.titles[selectedTitleIndex];
        if (titleNameField != null) t.titleName = titleNameField.value;
        if (titleDivisionField != null) t.division = titleDivisionField.value;
        if (titleIsTagTeamToggle != null) t.isTagTeamTitle = titleIsTagTeamToggle.value;

        if (t.isTagTeamTitle)
        {
            var a = titleTagChampionADropdown != null ? (titleTagChampionADropdown.value ?? string.Empty).Trim() : string.Empty;
            var b = titleTagChampionBDropdown != null ? (titleTagChampionBDropdown.value ?? string.Empty).Trim() : string.Empty;
            if (!string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b))
                t.currentChampion = $"{a} & {b}";
            else if (!string.IsNullOrEmpty(a) || !string.IsNullOrEmpty(b))
                t.currentChampion = string.Join(" & ", new[] { a, b }.Where(s => !string.IsNullOrEmpty(s)));
            else
                t.currentChampion = string.Empty;
        }
        else
        {
            if (titleChampionDropdown != null)
                t.currentChampion = titleChampionDropdown.value;
        }
        if (titleNotesField != null) t.notes = titleNotesField.value;
        DataManager.SaveTitles(titleCollection);
        RefreshDivisionChoices();
        RefreshTitleList();
        if (statusLabel != null) statusLabel.text = "Title updated.";
        ShowToast("Title updated.", false);
    }

    private void OnDeleteSelectedTitle()
    {
        if (titleCollection?.titles == null || selectedTitleIndex < 0 || selectedTitleIndex >= titleCollection.titles.Count) return;
        titleCollection.titles.RemoveAt(selectedTitleIndex);
        selectedTitleIndex = -1;
        DataManager.SaveTitles(titleCollection);
        RefreshDivisionChoices();
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
        titleStatsList.Add(new Label(current != null ? $"Current champion: {current.championName} - {current.daysHeld} days" : "Current champion: (unknown)"));
        titleStatsList.Add(new Label(totalReigns > 0 ? $"Reigns: {totalReigns}   Total defenses: {totalDefenses}" : "No stats yet."));
        if (longest != null) titleStatsList.Add(new Label($"Longest: {longest.championName} - {longest.daysHeld} days ({(string.IsNullOrEmpty(longest.dateLost) ? $"{longest.dateWon} - present" : $"{longest.dateWon} - {longest.dateLost}")})"));
        if (shortest != null) titleStatsList.Add(new Label($"Shortest: {shortest.championName} - {shortest.daysHeld} days ({(string.IsNullOrEmpty(shortest.dateLost) ? $"{shortest.dateWon} - present" : $"{shortest.dateWon} - {shortest.dateLost}")})"));
        if (mostDef != null) titleStatsList.Add(new Label($"Most defenses in a reign: {mostDef.championName} - {mostDef.defenses}"));
        titleStatsList.Add(new VisualElement() { style = { height = 8 } });
        // Reign summaries
        if (summaries.Count > 0)
        {
            titleStatsList.Add(new Label("Reign Summaries:") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 6 } });
            foreach (var s in summaries)
            {
                var row = new VisualElement(); row.style.marginBottom = 6;
                string span = string.IsNullOrEmpty(s.dateLost) ? $"{s.dateWon} - present" : $"{s.dateWon} - {s.dateLost}";
                row.Add(new Label($"{s.championName} ({span}) - {s.daysHeld} days, {s.defenses} defenses"));
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
        InvalidatePromotionStats();
    }

    private void RefreshVenueCityDropdowns()
    {
        if (currentPromotion == null) return;

        venueCityConfig ??= VenueCityConfigStore.LoadOrCreateDefault();

        var venues = new List<string> { string.Empty };
        if (venueCityConfig.venues != null)
            venues.AddRange(venueCityConfig.venues);

        var cities = new List<string> { string.Empty };
        if (venueCityConfig.cities != null)
            cities.AddRange(venueCityConfig.cities);

        foreach (var s in currentPromotion.shows ?? new List<ShowData>())
        {
            if (!string.IsNullOrWhiteSpace(s?.venue) && !venues.Contains(s.venue, StringComparer.OrdinalIgnoreCase))
                venues.Add(s.venue);
            if (!string.IsNullOrWhiteSpace(s?.city) && !cities.Contains(s.city, StringComparer.OrdinalIgnoreCase))
                cities.Add(s.city);
        }

        venues = venues
            .Where(v => v != null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v)
            .ToList();
        cities = cities
            .Where(c => c != null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();

        if (newShowVenueField != null)
        {
            newShowVenueField.choices = venues;
            if (!venues.Contains(newShowVenueField.value))
                newShowVenueField.value = string.Empty;
        }
        if (newShowCityField != null)
        {
            newShowCityField.choices = cities;
            if (!cities.Contains(newShowCityField.value))
                newShowCityField.value = string.Empty;
        }
        if (showVenueField != null)
        {
            showVenueField.choices = venues;
            if (!venues.Contains(showVenueField.value))
                showVenueField.value = string.Empty;
        }
        if (showCityField != null)
        {
            showCityField.choices = cities;
            if (!cities.Contains(showCityField.value))
                showCityField.value = string.Empty;
        }

        // Also refresh the visible lists in the Shows panel with removable rows
        if (venueListScroll != null)
        {
            venueListScroll.Clear();
            foreach (var v in venues.Where(v => !string.IsNullOrEmpty(v)))
            {
                venueListScroll.Add(CreateVenueRow(v));
            }
        }
        if (cityListScroll != null)
        {
            cityListScroll.Clear();
            foreach (var c in cities.Where(c => !string.IsNullOrEmpty(c)))
            {
                cityListScroll.Add(CreateCityRow(c));
            }
        }
    }

    private VisualElement CreateVenueRow(string venueName)
    {
        var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        var label = new Label(venueName) { style = { flexGrow = 1 } };
        var deleteButton = new Button(() => RemoveVenue(venueName)) { text = "X" };
        deleteButton.style.width = 24;
        deleteButton.style.marginLeft = 4;
        row.Add(label);
        row.Add(deleteButton);
        return row;
    }

    private VisualElement CreateCityRow(string cityName)
    {
        var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        var label = new Label(cityName) { style = { flexGrow = 1 } };
        var deleteButton = new Button(() => RemoveCity(cityName)) { text = "X" };
        deleteButton.style.width = 24;
        deleteButton.style.marginLeft = 4;
        row.Add(label);
        row.Add(deleteButton);
        return row;
    }

    private void OnAddVenue()
    {
        var v = newVenueField != null ? (newVenueField.value ?? string.Empty).Trim() : string.Empty;
        if (string.IsNullOrEmpty(v)) return;
        venueCityConfig ??= VenueCityConfigStore.LoadOrCreateDefault();
        if (!venueCityConfig.venues.Contains(v, StringComparer.OrdinalIgnoreCase))
        {
            venueCityConfig.venues.Add(v);
            venueCityConfig.venues = venueCityConfig.venues
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();
            VenueCityConfigStore.Save(venueCityConfig);
            RefreshVenueCityDropdowns();
            if (statusLabel != null) statusLabel.text = "Venue added.";
        }
        if (newVenueField != null) newVenueField.value = string.Empty;
    }

    private void OnAddCity()
    {
        var c = newCityField != null ? (newCityField.value ?? string.Empty).Trim() : string.Empty;
        if (string.IsNullOrEmpty(c)) return;
        venueCityConfig ??= VenueCityConfigStore.LoadOrCreateDefault();
        if (!venueCityConfig.cities.Contains(c, StringComparer.OrdinalIgnoreCase))
        {
            venueCityConfig.cities.Add(c);
            venueCityConfig.cities = venueCityConfig.cities
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();
            VenueCityConfigStore.Save(venueCityConfig);
            RefreshVenueCityDropdowns();
            if (statusLabel != null) statusLabel.text = "City added.";
        }
        if (newCityField != null) newCityField.value = string.Empty;
    }

    private void RemoveVenue(string venueName)
    {
        if (string.IsNullOrWhiteSpace(venueName)) return;
        venueCityConfig ??= VenueCityConfigStore.LoadOrCreateDefault();
        int removed = venueCityConfig.venues.RemoveAll(v => string.Equals(v, venueName, StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
        {
            VenueCityConfigStore.Save(venueCityConfig);
            RefreshVenueCityDropdowns();
            if (statusLabel != null) statusLabel.text = $"Venue removed: {venueName}";
        }
    }

    private void RemoveCity(string cityName)
    {
        if (string.IsNullOrWhiteSpace(cityName)) return;
        venueCityConfig ??= VenueCityConfigStore.LoadOrCreateDefault();
        int removed = venueCityConfig.cities.RemoveAll(c => string.Equals(c, cityName, StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
        {
            VenueCityConfigStore.Save(venueCityConfig);
            RefreshVenueCityDropdowns();
            if (statusLabel != null) statusLabel.text = $"City removed: {cityName}";
        }
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
        if (newShowTypeDropdown != null)
        {
            var t = (newShowTypeDropdown.value ?? string.Empty).Trim();
            show.showType = string.IsNullOrEmpty(t) ? null : t;
        }
        if (newShowTypeDropdown != null)
        {
            var t = (newShowTypeDropdown.value ?? string.Empty).Trim();
            show.showType = string.IsNullOrEmpty(t) ? null : t;
        }
        if (newShowBrandDropdown != null)
        {
            var b = (newShowBrandDropdown.value ?? string.Empty).Trim();
            show.brand = string.IsNullOrEmpty(b) ? null : b;
        }
        currentPromotion.shows.Add(show);
        string validation = ValidateShowReferences(show);
        DataManager.SavePromotion(currentPromotion);
        RefreshShowList();
        RefreshVenueCityDropdowns();
        if (newShowField != null) newShowField.value = string.Empty;
        if (newShowDateField != null) newShowDateField.value = string.Empty;
        if (newShowVenueField != null) newShowVenueField.value = string.Empty;
        if (newShowCityField != null) newShowCityField.value = string.Empty;
        if (newShowBrandDropdown != null) newShowBrandDropdown.value = "";
        if (newShowAttendanceField != null) newShowAttendanceField.value = 0;
        if (newShowRatingField != null) newShowRatingField.value = 0f;
        string msg = string.IsNullOrEmpty(validation) ? "Show added." : validation;
        if (statusLabel != null) statusLabel.text = msg;
        ShowToast(msg, false);
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
        if (showBrandDropdown != null) showBrandDropdown.value = s.brand ?? string.Empty;
        if (showAttendanceField != null) showAttendanceField.value = s.attendance;
        if (showRatingField != null) showRatingField.value = s.rating;
        if (showTypeDropdown != null)
        {
            if (showTypeDropdown.choices == null || showTypeDropdown.choices.Count == 0)
                showTypeDropdown.choices = new List<string> { "TV", "PPV", "House" };
            var t = s.showType ?? string.Empty;
            if (string.IsNullOrEmpty(t)) t = "TV";
            if (!showTypeDropdown.choices.Contains(t))
            {
                var list = new List<string>(showTypeDropdown.choices);
                list.Add(t);
                showTypeDropdown.choices = list;
            }
            showTypeDropdown.value = t;
        }
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
        if (showVenueField != null) s.venue = (showVenueField.value ?? string.Empty).Trim();
        if (showCityField != null) s.city = (showCityField.value ?? string.Empty).Trim();
        if (showAttendanceField != null) s.attendance = showAttendanceField.value;
        if (showRatingField != null) s.rating = showRatingField.value;
        if (showBrandDropdown != null)
        {
            var b = (showBrandDropdown.value ?? string.Empty).Trim();
            s.brand = string.IsNullOrEmpty(b) ? null : b;
        }
        if (showTypeDropdown != null)
        {
            var t = (showTypeDropdown.value ?? string.Empty).Trim();
            s.showType = string.IsNullOrEmpty(t) ? null : t;
        }
        string validation = ValidateShowReferences(s);
        DataManager.SavePromotion(currentPromotion);
        TitleHistoryManager.UpdateShowResults(currentPromotion, s, prevName, prevDate);
        RefreshShowList();
        RefreshVenueCityDropdowns();
        string msg2 = string.IsNullOrEmpty(validation) ? "Show updated." : validation;
        if (statusLabel != null) statusLabel.text = msg2;
        ShowToast(msg2, false);
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
        var names = wrestlerCollection?.wrestlers?
            .Where(w => !string.IsNullOrEmpty(w?.name))
            ?.Select(w => w.name)
            .OrderBy(n => n)
            .ToList() ?? new List<string>();
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

        // For a new match, keep optional C/D blank so they don't
        // accidentally turn every match into a multi-person/tag match.
        if (wrestlerCDropdown != null) wrestlerCDropdown.value = string.Empty;
        if (wrestlerDDropdown != null) wrestlerDDropdown.value = string.Empty;

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
        if (matchTypeDropdown != null)
            matchTypeDropdown.RegisterValueChangedCallback(_ => UpdateWinnerChoices());
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
        EnsureSegmentTypeChoices();
        PopulateSegmentParticipantChoices();
        if (segmentNameField != null) segmentNameField.value = string.Empty;
        if (segmentTypeDropdown != null && string.IsNullOrEmpty(segmentTypeDropdown.value) && segmentTypeDropdown.choices != null && segmentTypeDropdown.choices.Count > 0)
            segmentTypeDropdown.value = segmentTypeDropdown.choices[0];
        if (segmentParticipantADropdown != null) segmentParticipantADropdown.value = string.Empty;
        if (segmentParticipantBDropdown != null) segmentParticipantBDropdown.value = string.Empty;
        if (segmentParticipantCDropdown != null) segmentParticipantCDropdown.value = string.Empty;
        if (segmentParticipantDDropdown != null) segmentParticipantDDropdown.value = string.Empty;
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
        string matchName; bool isTagType = !string.IsNullOrEmpty(type) && type.IndexOf("tag", System.StringComparison.OrdinalIgnoreCase) >= 0; if (isTagType && !string.IsNullOrEmpty(A) && !string.IsNullOrEmpty(B) && !string.IsNullOrEmpty(C) && !string.IsNullOrEmpty(D)) matchName = $"{type}: {A} & {B} vs {C} & {D}"; else matchName = $"{type}: {string.Join(" vs ", have)}";
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

        // Resolve potential team winners (ids)
        string winnerTeamId = null;
        tagTeamCollection ??= DataManager.LoadTagTeams(currentPromotion.promotionName);
        string ResolveTeamId(string n1, string n2)
        {
            foreach (var t in tagTeamCollection?.teams ?? new List<TagTeamData>())
            {
                if (t == null) continue;
                bool match = (StringEquals(t.memberA, n1) && StringEquals(t.memberB, n2)) || (StringEquals(t.memberA, n2) && StringEquals(t.memberB, n1));
                if (match) return t.id;
                if (!string.IsNullOrEmpty(t.teamName) && (StringEquals(t.teamName, winner))) return t.id;
            }
            return null;
        }
        if (!string.IsNullOrEmpty(winner) && isTagType && !string.IsNullOrEmpty(A) && !string.IsNullOrEmpty(B) && !string.IsNullOrEmpty(C) && !string.IsNullOrEmpty(D))
        {
            var leftId = ResolveTeamId(A, B);
            var rightId = ResolveTeamId(C, D);
            if (!string.IsNullOrEmpty(leftId) && (StringEquals(winner, A) || StringEquals(winner, B) || (!string.IsNullOrEmpty(tagTeamCollection?.teams?.FirstOrDefault(t=>t.id==leftId)?.teamName) && StringEquals(tagTeamCollection.teams.First(t=>t.id==leftId).teamName, winner))))
                winnerTeamId = leftId;
            else if (!string.IsNullOrEmpty(rightId) && (StringEquals(winner, C) || StringEquals(winner, D) || (!string.IsNullOrEmpty(tagTeamCollection?.teams?.FirstOrDefault(t=>t.id==rightId)?.teamName) && StringEquals(tagTeamCollection.teams.First(t=>t.id==rightId).teamName, winner))))
                winnerTeamId = rightId;
        }

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
            winnerTeamId = winnerTeamId,
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
        EnsureSegmentTypeChoices();
        PopulateSegmentParticipantChoices();
        var segType = segmentTypeDropdown != null ? (segmentTypeDropdown.value ?? string.Empty).Trim() : string.Empty;
        var participantNames = CollectSegmentParticipantNames();
        var participantIds = participantNames
            .Select(GetWrestlerIdByName)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList();
        var s = new SegmentData
        {
            id = System.Guid.NewGuid().ToString("N"),
            text = text,
            segmentType = string.IsNullOrEmpty(segType) ? null : segType,
            participantNames = participantNames,
            participantIds = participantIds
        };
        s.name = BuildSegmentAutoName(name, s.segmentType, s.participantNames);
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

    private void EnsureSegmentTypeChoices()
    {
        if (segmentTypeDropdown == null) return;
        if (segmentTypeDropdown.choices == null || segmentTypeDropdown.choices.Count == 0)
        {
            var types = SegmentTypeCatalog.Types != null && SegmentTypeCatalog.Types.Count > 0
                ? new List<string>(SegmentTypeCatalog.Types)
                : new List<string> { "Segment" };
            segmentTypeDropdown.choices = types;
        }
    }

    private void PopulateSegmentParticipantChoices()
    {
        if (segmentParticipantADropdown == null && segmentParticipantBDropdown == null &&
            segmentParticipantCDropdown == null && segmentParticipantDDropdown == null) return;

        wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion?.promotionName);
        var names = wrestlerCollection?.wrestlers?
            .Where(w => !string.IsNullOrEmpty(w?.name))
            .Select(w => w.name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();
        if (!names.Contains(string.Empty)) names.Insert(0, string.Empty);
        SetChoices(segmentParticipantADropdown, names);
        SetChoices(segmentParticipantBDropdown, names);
        SetChoices(segmentParticipantCDropdown, names);
        SetChoices(segmentParticipantDDropdown, names);
    }

    private List<string> CollectSegmentParticipantNames()
    {
        var result = new List<string>();
        void add(DropdownField dd)
        {
            var v = dd != null ? (dd.value ?? string.Empty).Trim() : string.Empty;
            if (string.IsNullOrEmpty(v)) return;
            if (!result.Any(x => string.Equals(x, v, StringComparison.OrdinalIgnoreCase)))
                result.Add(v);
        }
        add(segmentParticipantADropdown);
        add(segmentParticipantBDropdown);
        add(segmentParticipantCDropdown);
        add(segmentParticipantDDropdown);
        return result;
    }

    private string GetWrestlerIdByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion?.promotionName);
        var w = wrestlerCollection?.wrestlers?.FirstOrDefault(x => string.Equals(x?.name, name, StringComparison.OrdinalIgnoreCase));
        return w?.id;
    }

    private string BuildSegmentAutoName(string specifiedName, string segType, List<string> participants)
    {
        if (!string.IsNullOrWhiteSpace(specifiedName)) return specifiedName.Trim();
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(segType)) parts.Add(segType.Trim());
        if (participants != null && participants.Count > 0)
            parts.Add(string.Join(", ", participants));
        if (parts.Count > 0) return string.Join(" - ", parts);
        return "Segment";
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
        var a = wrestlerADropdown?.value;
        var b = wrestlerBDropdown?.value;
        var c = wrestlerCDropdown?.value;
        var d = wrestlerDDropdown?.value;
        add(a);
        add(b);
        add(c);
        add(d);

        // If Tag Team match type and both sides have two members, include team names as winner options
        var typeVal = matchTypeDropdown != null ? (matchTypeDropdown.value ?? string.Empty) : string.Empty;
        bool isTag = typeVal.IndexOf("tag", StringComparison.OrdinalIgnoreCase) >= 0;
        if (isTag && !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b) && !string.IsNullOrWhiteSpace(c) && !string.IsNullOrWhiteSpace(d))
        {
            var teamLeft = TeamDisplay(a, b);
            var teamRight = TeamDisplay(c, d);
            add(teamLeft);
            add(teamRight);
        }
        names.Add("Draw");
        names.Add("No Contest");
        if (names.Count == 0) names.Add(string.Empty);
        SetChoices(winnerDropdown, names);
    }

    private void SetChoices(DropdownField dropdown, List<string> choices)
    {
        if (dropdown == null) return;
        var prev = dropdown.value;
        dropdown.choices = choices ?? new List<string>();
        if (!string.IsNullOrEmpty(prev) && dropdown.choices.Contains(prev)) dropdown.value = prev;
        else dropdown.value = dropdown.choices.Count > 0 ? dropdown.choices[0] : string.Empty;
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
        // Brand filter
        string brandFilter = historyBrandDropdown != null ? (historyBrandDropdown.value ?? string.Empty).Trim() : string.Empty;
        if (!string.IsNullOrEmpty(brandFilter) && !string.Equals(brandFilter, "All Brands", StringComparison.OrdinalIgnoreCase))
        {
            all = all.Where(s => s != null && !string.IsNullOrEmpty(s.brand) && StringEquals(s.brand, brandFilter)).ToList();
        }
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
        matchesOrderView.makeItem = () =>
        {
            var b = new Button();
            b.AddToClassList("list-entry");
            b.clicked += () => OnMatchesOrderButtonClicked(b);
            return b;
        };
        matchesOrderView.bindItem = (ve, i) =>
        {
            var b = (Button)ve;
            var list = matchesOrderView.itemsSource as List<string>;
            b.text = GetDisplayTextForToken(currentEditingShow, (list != null && i >= 0 && i < list.Count) ? list[i] : null);
            b.userData = i;
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

    private void OnMatchesOrderButtonClicked(Button button)
    {
        if (button == null || matchesOrderView == null) return;
        if (cardBuilderView == null || currentEditingShow == null) return;
        if (matchesOrderView.itemsSource is not List<string> tokens) return;
        if (button.userData is not int idx || idx < 0 || idx >= tokens.Count) return;
        if (!TryParseEntryToken(tokens[idx], out var kind, out var entryId)) return;
        if (kind != 'M' || string.IsNullOrEmpty(entryId)) return;

        CloseMatchesView();
        cardBuilderView.BeginEditAndSelectEntry(currentEditingShow, 'M', entryId);
        SetActivePanel(cardBuilderPanel);
    }

    private string GetDisplayTextForToken(ShowData show, string token)
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
            string vsLine = BuildVsLine(m);
            return string.IsNullOrEmpty(vsLine) ? m.matchName : $"{m.matchName} - {vsLine}";
        }
        else if (kind == 'S')
        {
            if (show.segments == null || idx < 0 || idx >= show.segments.Count) return string.Empty;
            var s = show.segments[idx];
            var segName = string.IsNullOrEmpty(s?.name) ? "Segment" : s.name;
            var typeSuffix = string.IsNullOrEmpty(s?.segmentType) ? string.Empty : $" [{s.segmentType}]";
            return $"Segment: {segName}{typeSuffix}";
        }
        return string.Empty;
    }

    private List<string> GetSegmentParticipantDisplayNames(SegmentData segment)
    {
        var names = new List<string>();
        if (segment == null) return names;
        if (segment.participantNames != null)
        {
            foreach (var n in segment.participantNames)
            {
                if (string.IsNullOrWhiteSpace(n)) continue;
                var trimmed = n.Trim();
                if (!names.Any(x => string.Equals(x, trimmed, StringComparison.OrdinalIgnoreCase)))
                    names.Add(trimmed);
            }
        }
        if (names.Count == 0 && segment.participantIds != null)
        {
            wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion?.promotionName);
            foreach (var id in segment.participantIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                var resolved = wrestlerCollection?.wrestlers?.FirstOrDefault(w => string.Equals(w?.id, id, StringComparison.OrdinalIgnoreCase))?.name;
                if (!string.IsNullOrWhiteSpace(resolved) && !names.Any(x => string.Equals(x, resolved, StringComparison.OrdinalIgnoreCase)))
                    names.Add(resolved);
            }
        }
        return names;
    }

    private static bool TryParseEntryToken(string token, out char kind, out string id)
    {
        kind = '?';
        id = string.Empty;
        if (string.IsNullOrEmpty(token)) return false;
        int colonIndex = token.IndexOf(':');
        if (colonIndex <= 0 || colonIndex >= token.Length - 1) return false;
        kind = token[0];
        id = token.Substring(colonIndex + 1);
        return !string.IsNullOrEmpty(id);
    }

    private static bool StringEquals(string a, string b)
    {
        return string.Equals(a ?? string.Empty, b ?? string.Empty, System.StringComparison.OrdinalIgnoreCase);
    }

    // ----- Step 2: Stable IDs + ordered history rendering -----
    private void EnsureStableIdsAndEntryOrder()
    {
        if (currentPromotion?.shows == null) return;
        bool changed = false;
        foreach (var show in currentPromotion.shows)
        {
            if (show == null) continue;
            if (string.IsNullOrEmpty(show.id)) { show.id = System.Guid.NewGuid().ToString("N"); changed = true; }
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
                    string vsLine = BuildVsLine(m);
                    entry.Add(new Label(m.matchName));
                    if (!string.IsNullOrEmpty(vsLine)) entry.Add(new Label(vsLine));
                    if (!string.IsNullOrEmpty(m.winner)) entry.Add(new Label($"Winner: {m.winner}"));
                    if (m.isTitleMatch && !string.IsNullOrEmpty(m.titleName)) entry.Add(new Label($"Title: {m.titleName}"));

                    var linksRow = BuildHistoryLinksRow(show, m);
                    if (linksRow != null)
                        entry.Add(linksRow);

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
                    if (!string.IsNullOrEmpty(s?.segmentType)) entry.Add(new Label($"Type: {s.segmentType}"));
                    var segmentParticipants = GetSegmentParticipantDisplayNames(s);
                    if (segmentParticipants.Count > 0) entry.Add(new Label($"Participants: {string.Join(", ", segmentParticipants)}"));
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
                    string vsLine = BuildVsLine(m);
                    entry.Add(new Label(m.matchName)); if (!string.IsNullOrEmpty(vsLine)) entry.Add(new Label(vsLine));
                    if (!string.IsNullOrEmpty(m.winner)) entry.Add(new Label($"Winner: {m.winner}"));
                    if (m.isTitleMatch && !string.IsNullOrEmpty(m.titleName)) entry.Add(new Label($"Title: {m.titleName}"));
                    var linksRow = BuildHistoryLinksRow(show, m);
                    if (linksRow != null)
                        entry.Add(linksRow);
                    historyShowMatchesList.Add(entry);
                }
            }
            if (show?.segments != null)
            {
                foreach (var s in show.segments)
                {
                    var entry = new VisualElement(); entry.style.marginBottom = 6;
                    var segName = string.IsNullOrEmpty(s?.name) ? "Segment" : s.name;
                    entry.Add(new Label($"Segment: {segName}"));
                    if (!string.IsNullOrEmpty(s?.segmentType)) entry.Add(new Label($"Type: {s.segmentType}"));
                    var segmentParticipants = GetSegmentParticipantDisplayNames(s);
                    if (segmentParticipants.Count > 0) entry.Add(new Label($"Participants: {string.Join(", ", segmentParticipants)}"));
                    if (!string.IsNullOrEmpty(s?.text)) entry.Add(new Label(s.text));
                    historyShowMatchesList.Add(entry);
                }
            }
        }
        FocusPanel(historyResultsPanel);
    }

    private VisualElement BuildHistoryLinksRow(ShowData show, MatchData m)
    {
        if (show == null || m == null) return null;

        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.marginTop = 2;

        void AddWrestlerLink(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (string.Equals(name, "Draw", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "No Contest", StringComparison.OrdinalIgnoreCase))
                return;

            var localName = name.Trim();
            var btn = new Button(() => OpenWrestlerFromHistory(localName)) { text = localName };
            btn.style.marginRight = 4;
            btn.style.fontSize = 11;
            row.Add(btn);
        }

        AddWrestlerLink(m.wrestlerA);
        AddWrestlerLink(m.wrestlerB);
        AddWrestlerLink(m.wrestlerC);
        AddWrestlerLink(m.wrestlerD);

        if (m.isTitleMatch && !string.IsNullOrEmpty(m.titleName))
        {
            var titleLocal = m.titleName.Trim();
            var titleBtn = new Button(() => OpenTitleFromHistory(titleLocal)) { text = $"Title: {titleLocal}" };
            titleBtn.style.marginRight = 4;
            titleBtn.style.fontSize = 11;
            row.Add(titleBtn);
        }

        foreach (var rv in FindRivalriesForMatch(show, m))
        {
            var label = string.IsNullOrEmpty(rv?.title) ? "View Rivalry" : $"Rivalry: {rv.title}";
            var localRivalry = rv;
            var btn = new Button(() => OpenRivalryFromHistory(localRivalry)) { text = label };
            btn.style.marginRight = 4;
            btn.style.fontSize = 11;
            row.Add(btn);
        }

        return row.childCount > 0 ? row : null;
    }

    private void OpenWrestlerFromHistory(string name)
    {
        if (currentPromotion == null || string.IsNullOrWhiteSpace(name)) return;
        wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion.promotionName);
        var list = wrestlerCollection?.wrestlers ?? new List<WrestlerData>();
        int idx = -1;
        for (int i = 0; i < list.Count; i++)
        {
            if (StringEquals(list[i]?.name, name))
            {
                idx = i;
                break;
            }
        }
        if (idx < 0)
        {
            if (statusLabel != null) statusLabel.text = $"Wrestler not found: {name}";
            return;
        }

        EnsureWrestlerListView();
        RefreshWrestlerList();
        SetActivePanel(wrestlersPanel);
        SelectWrestler(idx);
    }

    private void OpenTitleFromHistory(string titleName)
    {
        if (currentPromotion == null || string.IsNullOrWhiteSpace(titleName)) return;
        titleCollection ??= DataManager.LoadTitles(currentPromotion.promotionName);
        var list = titleCollection?.titles ?? new List<TitleData>();
        int idx = -1;
        for (int i = 0; i < list.Count; i++)
        {
            if (StringEquals(list[i]?.titleName, titleName))
            {
                idx = i;
                break;
            }
        }
        if (idx < 0)
        {
            if (statusLabel != null) statusLabel.text = $"Title not found: {titleName}";
            return;
        }

        EnsureTitleListView();
        RefreshTitleList();
        SetActivePanel(titlesPanel);
        SelectTitle(idx);
    }

    private IEnumerable<RivalryData> FindRivalriesForMatch(ShowData show, MatchData match)
    {
        if (currentPromotion == null || show == null || match == null || string.IsNullOrEmpty(show.id) || string.IsNullOrEmpty(match.id))
            yield break;

        rivalryCollection ??= DataManager.LoadRivalries(currentPromotion.promotionName);
        foreach (var r in rivalryCollection?.rivalries ?? new List<RivalryData>())
        {
            if (r?.events == null) continue;
            foreach (var e in r.events)
            {
                if (e == null) continue;
                if (!string.IsNullOrEmpty(e.showId) && !string.IsNullOrEmpty(e.matchId) &&
                    StringEquals(e.showId, show.id) && StringEquals(e.matchId, match.id))
                {
                    yield return r;
                    break;
                }
            }
        }
    }

    private void OpenRivalryFromHistory(RivalryData rivalry)
    {
        if (rivalry == null || rivalryCollection?.rivalries == null) return;
        int idx = -1;
        for (int i = 0; i < rivalryCollection.rivalries.Count; i++)
        {
            var r = rivalryCollection.rivalries[i];
            if (r == rivalry ||
                (!string.IsNullOrEmpty(r?.id) && !string.IsNullOrEmpty(rivalry.id) && StringEquals(r.id, rivalry.id)) ||
                (!string.IsNullOrEmpty(r?.title) && !string.IsNullOrEmpty(rivalry.title) && StringEquals(r.title, rivalry.title)))
            {
                idx = i;
                break;
            }
        }
        if (idx < 0)
        {
            if (statusLabel != null) statusLabel.text = "Rivalry not found.";
            return;
        }
        ShowRivalriesPanel();
        SelectRivalry(idx);
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
        choices = choices
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
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
        rankingsListView.makeItem = () =>
        {
            var b = new Button();
            b.AddToClassList("list-entry");
            return b;
        };
        rankingsListView.bindItem = (ve, i) =>
        {
            var b = (Button)ve;
            var items = rankingsListView.itemsSource as List<string>;
            b.text = (items != null && i >= 0 && i < items.Count) ? items[i] : string.Empty;
        };
        parent?.Add(rankingsListView);
        if (rankingsListScroll != null) rankingsListScroll.style.display = DisplayStyle.None;
    }

    // ==========================
    // Rankings 2.0 (weekly snapshots)
    // ==========================

    private void InitializeRankingsControls()
    {
        if (currentPromotion == null) return;
        rankingStore = DataManager.LoadRankings(currentPromotion.promotionName) ?? new RankingStore { promotionName = currentPromotion.promotionName };
        rankingStore.promotionName = currentPromotion.promotionName;
        rankingStore.config ??= new RankingConfig { promotionName = currentPromotion.promotionName };
        if (string.IsNullOrEmpty(rankingStore.config.promotionName)) rankingStore.config.promotionName = currentPromotion.promotionName;

        // Type
        if (rankingsTypeDropdown != null)
        {
            rankingsTypeDropdown.choices = new List<string> { "Singles", "Tag Team", "Stable" };
            rankingsTypeDropdown.value = rankingsTypeDropdown.choices[0];
            rankingsTypeDropdown.RegisterValueChangedCallback(_ => { RefreshRankingControlVisibility(); RefreshWeeksDropdown(); RecomputeForCurrentWeekSelection(); });
        }

        // Gender (Singles only)
        if (rankingsGenderDropdown != null)
        {
            rankingsGenderDropdown.choices = new List<string> { "Men", "Women", "All" };
            rankingsGenderDropdown.value = "Men";
                        rankingsGenderDropdown.RegisterValueChangedCallback(_ => { RefreshWeeksDropdown(); RecomputeForCurrentWeekSelection(); });
}

        // Division (Singles only)
        if (rankingsDivisionDropdown != null)
        {
            var divs = (rankingStore.config?.singlesDivisions != null && rankingStore.config.singlesDivisions.Count > 0)
                ? new List<string>(rankingStore.config.singlesDivisions)
                : new List<string> { "Overall" };
            rankingsDivisionDropdown.choices = divs;
            rankingsDivisionDropdown.value = divs[0];
            rankingsDivisionDropdown.RegisterValueChangedCallback(_ => { RefreshWeeksDropdown(); RecomputeForCurrentWeekSelection(); });
        }

        // Weeks dropdown: Current Week + saved snapshots for the selected type/gender/division
        RefreshWeeksDropdown();

        if (computeRankingsButton != null) computeRankingsButton.clicked += RecomputeForCurrentWeekSelection;
        if (computeRangeRankingsButton != null) computeRangeRankingsButton.clicked += OnComputeRangeRankingsClicked;
        if (saveSnapshotButton != null) saveSnapshotButton.clicked += SaveCurrentWeekSnapshot;

        // Date picker for historical week selection
        if (rankingsDateField != null)
        {
            // default to today
            rankingsDateField.value = DateTime.Today.ToString(DateFormat, CultureInfo.InvariantCulture);
            // attach small calendar button
            if (rankingsDatePickButton == null)
            {
                rankingsDatePickButton = new Button(() => OpenDatePicker(rankingsDateField)) { text = "\U0001F4C5" };
                rankingsDatePickButton.style.width = 28; rankingsDatePickButton.style.height = 22; rankingsDatePickButton.style.marginLeft = 6;
                rankingsDateField.parent?.Add(rankingsDatePickButton);
            }
            if (rankingsPrevWeekButton != null) rankingsPrevWeekButton.clicked += OnPrevWeekClicked;
            if (rankingsNextWeekButton != null) rankingsNextWeekButton.clicked += OnNextWeekClicked;
        }

        if (rankingsWeekDropdown != null)
        {
            rankingsWeekDropdown.RegisterValueChangedCallback(_ => OnWeekSelectionChanged());
        }

        RefreshRankingControlVisibility();

        // Initialize formula UI from config
        if (rankingStore.config?.formula != null)
        {
            var f = rankingStore.config.formula;
            var winField = root.Q<FloatField>("rankingsWinPointsField");
            var drawField = root.Q<FloatField>("rankingsDrawPointsField");
            var lossField = root.Q<FloatField>("rankingsLossPointsField");
            var mainField = root.Q<FloatField>("rankingsMainEventBonusField");
            var titleField = root.Q<FloatField>("rankingsTitleMatchBonusField");
            if (winField != null) winField.value = f.winPoints;
            if (drawField != null) drawField.value = f.drawPoints;
            if (lossField != null) lossField.value = f.lossPoints;
            if (mainField != null) mainField.value = f.mainEventBonus;
            if (titleField != null) titleField.value = f.titleMatchBonus;

            var saveFormulaButton = root.Q<Button>("saveFormulaButton");
            if (saveFormulaButton != null)
            {
                saveFormulaButton.clicked += () =>
                {
                    try
                    {
                        if (winField != null) f.winPoints = (int)winField.value;
                        if (drawField != null) f.drawPoints = (int)drawField.value;
                        if (lossField != null) f.lossPoints = (int)lossField.value;
                        if (mainField != null) f.mainEventBonus = mainField.value;
                        if (titleField != null) f.titleMatchBonus = titleField.value;
                        DataManager.SaveRankings(rankingStore);
                        if (statusLabel != null) statusLabel.text = "Ranking formula saved.";
                        RecomputeForCurrentWeekSelection();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to save ranking formula: {ex.Message}");
                    }
                };
            }
        }
    }

    private void SetRankingQuickType(RankingType type, string gender)
    {
        if (rankingsTypeDropdown != null)
        {
            rankingsTypeDropdown.value = type == RankingType.Singles ? "Singles" : type == RankingType.TagTeam ? "Tag Team" : "Stable";
        }
        if (rankingsGenderDropdown != null && type == RankingType.Singles && !string.IsNullOrEmpty(gender))
        {
            if (rankingsGenderDropdown.choices.Contains(gender)) rankingsGenderDropdown.value = gender;
        }
        RefreshRankingControlVisibility();
        RefreshWeeksDropdown();
    }

    private void RefreshRankingControlVisibility()
    {
        string typeVal = rankingsTypeDropdown?.value ?? "Singles";
        bool singles = string.Equals(typeVal, "Singles", StringComparison.OrdinalIgnoreCase);
        if (rankingsGenderDropdown != null) rankingsGenderDropdown.style.display = singles ? DisplayStyle.Flex : DisplayStyle.None;
        if (rankingsDivisionDropdown != null) rankingsDivisionDropdown.style.display = singles ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void RefreshWeeksDropdown()
    {
        if (rankingsWeekDropdown == null) return;
        var list = new List<string> { "Current Week" };
        if (rankingStore?.snapshots != null)
        {
            var filterType = GetSelectedRankingType();
            var filterGender = GetSelectedGender();
            var filterDivision = GetSelectedDivision();
            var filterBrand = rankingsBrandDropdown != null ? (rankingsBrandDropdown.value ?? string.Empty).Trim() : string.Empty;
            foreach (var s in rankingStore.snapshots)
            {
                if (s == null) continue;
                if (s.type != filterType) continue;
                if ((filterType == RankingType.Singles) && (!StringEquals(s.gender, filterGender) || !StringEquals(s.division, filterDivision))) continue;
                if (!string.IsNullOrEmpty(filterBrand) && !string.Equals(filterBrand, "All Brands", StringComparison.OrdinalIgnoreCase))
                {
                    if (!StringEquals(s.brand, filterBrand)) continue;
                }
                string label = WeekLabel(s.weekStartIso, s.weekEndIso);
                if (!list.Contains(label)) list.Add(label);
            }
        }
        if (!list.Contains("Overall")) list.Insert(0, "Overall");
        rankingsWeekDropdown.choices = list;
        if (string.IsNullOrEmpty(rankingsWeekDropdown.value) || !list.Contains(rankingsWeekDropdown.value))
            rankingsWeekDropdown.value = "Overall";
    }

    private string WeekLabel(string startIso, string endIso)
    {
        return $"Week of {startIso} to {endIso}";
    }

    private RankingType GetSelectedRankingType()
    {
        string v = rankingsTypeDropdown?.value ?? "Singles";
        if (v.IndexOf("tag", StringComparison.OrdinalIgnoreCase) >= 0) return RankingType.TagTeam;
        if (v.IndexOf("stable", StringComparison.OrdinalIgnoreCase) >= 0) return RankingType.Stable;
        return RankingType.Singles;
    }

    private string GetSelectedGender()
    {
        return rankingsGenderDropdown?.value ?? "Men";
    }

    private string GetSelectedDivision()
    {
        return rankingsDivisionDropdown?.value ?? "Overall";
    }

    private void OnWeekSelectionChanged()
    {
        var val = rankingsWeekDropdown?.value ?? "Current Week";
        if (val.StartsWith("Overall", StringComparison.OrdinalIgnoreCase)) { ComputeOverallRankings(); return; }
        if (val.StartsWith("Current Week", StringComparison.OrdinalIgnoreCase)) { ComputeCurrentWeekRankings(); return; }
        // Find matching snapshot by label and selected filters
        var t = GetSelectedRankingType();
        var g = GetSelectedGender();
        var d = GetSelectedDivision();
        var snap = FindSnapshotByLabel(t, d, g, val);
        if (snap != null)
        {
            currentRankingResults = snap.top ?? new List<RankingEntry>();
            DisplayRankingEntries(currentRankingResults, snap.topN);
        }
        else { ComputeCurrentWeekRankings(); }
    }

    private void RecomputeForCurrentWeekSelection()
    {
        var sel = rankingsWeekDropdown?.value ?? "Overall";
        if (sel.StartsWith("Overall", StringComparison.OrdinalIgnoreCase)) { ComputeOverallRankings(); }
        else if (sel.StartsWith("Current Week", StringComparison.OrdinalIgnoreCase)) { ComputeCurrentWeekRankings(); }
        else { OnWeekSelectionChanged(); }
    }

    private RankingSnapshot FindSnapshotByLabel(RankingType t, string division, string gender, string label)
    {
        if (rankingStore?.snapshots == null) return null;
        var filterBrand = rankingsBrandDropdown != null ? (rankingsBrandDropdown.value ?? string.Empty).Trim() : string.Empty;
        foreach (var s in rankingStore.snapshots)
        {
            if (s == null) continue;
            if (s.type != t) continue;
            if (t == RankingType.Singles && (!StringEquals(s.gender, gender) || !StringEquals(s.division, division))) continue;
            if (!string.IsNullOrEmpty(filterBrand) && !string.Equals(filterBrand, "All Brands", StringComparison.OrdinalIgnoreCase))
            {
                if (!StringEquals(s.brand, filterBrand)) continue;
            }
            if (StringEquals(WeekLabel(s.weekStartIso, s.weekEndIso), label)) return s;
        }
        return null;
    }

    private List<RankingEntry> GetPreviousSnapshotTopForCurrentWeek()
    {
        if (rankingStore?.snapshots == null || rankingStore.snapshots.Count == 0) return null;
        var type = GetSelectedRankingType();
        var division = GetSelectedDivision();
        var gender = GetSelectedGender();
        var brand = rankingsBrandDropdown != null ? (rankingsBrandDropdown.value ?? string.Empty).Trim() : string.Empty;

        var asOf = GetSelectedAsOfDate();
        var (weekStart, _) = GetWeekRange(asOf);

        RankingSnapshot best = null;
        DateTime bestEnd = DateTime.MinValue;

        foreach (var s in rankingStore.snapshots)
        {
            if (s == null) continue;
            if (s.type != type) continue;
            if (type == RankingType.Singles && (!StringEquals(s.gender, gender) || !StringEquals(s.division, division))) continue;
            if (!string.IsNullOrEmpty(brand) && !string.Equals(brand, "All Brands", StringComparison.OrdinalIgnoreCase))
            {
                if (!StringEquals(s.brand, brand)) continue;
            }
            if (!CalendarUtils.TryParseAny(s.weekEndIso, out var end)) continue;
            if (end >= weekStart) continue; // only weeks strictly before current
            if (end > bestEnd)
            {
                bestEnd = end;
                best = s;
            }
        }

        return best?.top;
    }

    private void ComputeCurrentWeekRankings()
    {
        if (currentPromotion == null || rankingsListView == null) return;
        var asOf = GetSelectedAsOfDate();
        var (weekStart, weekEnd) = GetWeekRange(asOf);
        var type = GetSelectedRankingType();
        var division = GetSelectedDivision();
        var gender = GetSelectedGender();

        wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion.promotionName);
        tagTeamCollection ??= DataManager.LoadTagTeams(currentPromotion.promotionName);
        stableCollection ??= DataManager.LoadStables(currentPromotion.promotionName);

        var entries = ComputeWeekly(type, gender, division, weekStart, weekEnd);
        currentRankingResults = entries;

        var previousTop = GetPreviousSnapshotTopForCurrentWeek();
        if (previousTop != null && previousTop.Count > 0)
            DisplayRankingEntriesWithTrend(entries, previousTop, 10);
        else
            DisplayRankingEntries(entries, 10);
    }

    private void ComputeOverallRankings()
    {
        if (currentPromotion == null || rankingsListView == null) return;
        var type = GetSelectedRankingType();
        var division = GetSelectedDivision();
        var gender = GetSelectedGender();
        var entries = ComputeWeekly(type, gender, division, DateTime.MinValue, DateTime.MaxValue);
        currentRankingResults = entries;
        DisplayRankingEntries(entries, 10);
        ShowToast("Rankings computed for entire history.");
    }

    private void OnPrevWeekClicked()
    {
        var dt = GetSelectedAsOfDate().AddDays(-7);
        UpdateRankingsDateField(dt);
        SetWeekDropdownToCurrent();
        ComputeCurrentWeekRankings();
    }

    private void OnNextWeekClicked()
    {
        var dt = GetSelectedAsOfDate().AddDays(7);
        UpdateRankingsDateField(dt);
        SetWeekDropdownToCurrent();
        ComputeCurrentWeekRankings();
    }

    private void UpdateRankingsDateField(DateTime dt)
    {
        if (rankingsDateField != null)
            rankingsDateField.value = dt.ToString(DateFormat, CultureInfo.InvariantCulture);
    }

    private void SetWeekDropdownToCurrent()
    {
        if (rankingsWeekDropdown == null) return;
        if (rankingsWeekDropdown.choices == null || rankingsWeekDropdown.choices.Count == 0)
            RefreshWeeksDropdown();
        if (rankingsWeekDropdown.choices != null && rankingsWeekDropdown.choices.Count > 0)
            rankingsWeekDropdown.value = rankingsWeekDropdown.choices[0]; // "Current Week"
    }

    private DateTime GetSelectedAsOfDate()
    {
        DateTime dt;
        var s = rankingsDateField != null ? (rankingsDateField.value ?? string.Empty).Trim() : string.Empty;
        if (!string.IsNullOrEmpty(s))
        {
            if (DateTime.TryParseExact(s, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)) return dt.Date;
            if (CalendarUtils.TryParseAny(s, out dt)) return dt.Date;
        }
        return DateTime.Today;
    }

    private bool TryParseRankingDateField(TextField field, out DateTime date)
    {
        date = DateTime.MinValue;
        if (field == null) return false;
        var s = (field.value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(s)) return false;
        if (DateTime.TryParseExact(s, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) { date = d.Date; return true; }
        if (CalendarUtils.TryParseAny(s, out d)) { date = d.Date; return true; }
        return false;
    }

    private void OnComputeRangeRankingsClicked()
    {
        if (currentPromotion == null || rankingsListView == null) return;
        if (!TryParseRankingDateField(rankingsFromDateField, out var from) ||
            !TryParseRankingDateField(rankingsToDateField, out var to))
        {
            if (statusLabel != null) statusLabel.text = "Enter valid From/To dates.";
            return;
        }
        if (to < from)
        {
            var tmp = from; from = to; to = tmp;
        }

        var type = GetSelectedRankingType();
        var division = GetSelectedDivision();
        var gender = GetSelectedGender();

        wrestlerCollection ??= DataManager.LoadWrestlers(currentPromotion.promotionName);
        tagTeamCollection ??= DataManager.LoadTagTeams(currentPromotion.promotionName);
        stableCollection ??= DataManager.LoadStables(currentPromotion.promotionName);

        var entries = ComputeWeekly(type, gender, division, from, to);
        currentRankingResults = entries;
        DisplayRankingEntries(entries, 10);
        if (statusLabel != null) statusLabel.text = $"Range rankings {from:MM/dd/yyyy} - {to:MM/dd/yyyy}.";
    }

    private List<RankingEntry> ComputeWeekly(RankingType type, string gender, string division, DateTime weekStart, DateTime weekEnd)
    {
        var results = new Dictionary<string, RankingEntry>(System.StringComparer.OrdinalIgnoreCase);
        var formula = rankingStore?.config?.formula ?? new RankingConfig().formula;
        float winPts = formula.winPoints;
        float drawPts = formula.drawPoints;
        float lossPts = formula.lossPoints;
        float mainBonus = formula.mainEventBonus;
        float titleBonus = formula.titleMatchBonus;

        // Build helpers
        var wrestlerByName = new Dictionary<string, WrestlerData>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var w in wrestlerCollection?.wrestlers ?? new List<WrestlerData>())
            if (!string.IsNullOrEmpty(w?.name)) wrestlerByName[w.name] = w;

        bool IncludeWrestler(WrestlerData w)
        {
            if (w == null) return false;
            if (type == RankingType.Singles)
            {
                if (string.Equals(gender, "Men", StringComparison.OrdinalIgnoreCase) && w.isFemale) return false;
                if (string.Equals(gender, "Women", StringComparison.OrdinalIgnoreCase) && !w.isFemale) return false;
                // Division filtering: if a specific division is selected (not Overall),
                // include only wrestlers whose division matches that label.
                if (!string.IsNullOrEmpty(division) && !string.Equals(division, "Overall", StringComparison.OrdinalIgnoreCase))
                {
                    var dv = w.division ?? string.Empty;
                    if (!StringEquals(dv, division)) return false;
                }
                return true;
            }
            return true;
        }

                string TeamOfWinner(string winnerName)
        {
            if (tagTeamCollection?.teams == null) return null;
            foreach (var t in tagTeamCollection.teams)
            {
                if (t == null || string.IsNullOrEmpty(t.teamName)) continue;
                // Winner may be a team name or an individual member
                if (StringEquals(t.teamName, winnerName) || StringEquals(t.memberA, winnerName) || StringEquals(t.memberB, winnerName))
                    return t.teamName;
            }
            return null;
        }

        string StableOfWinner(string winnerName)
        {
            if (stableCollection?.stables == null) return null;
            // Find wrestler id for winner
            string id = wrestlerByName.TryGetValue(winnerName ?? string.Empty, out var ww) ? ww.id : null;
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var s in stableCollection.stables)
            {
                if (s?.memberIds != null && s.memberIds.Contains(id)) return s.stableName;
            }
            return null;
        }

        bool InWeek(DateTime d) => d.Date >= weekStart.Date && d.Date <= weekEnd.Date;
        string brandFilter = rankingsBrandDropdown != null ? (rankingsBrandDropdown.value ?? string.Empty).Trim() : string.Empty;
        bool MatchBrand(ShowData s)
        {
            if (s == null) return false;
            if (string.IsNullOrEmpty(brandFilter) || string.Equals(brandFilter, "All Brands", StringComparison.OrdinalIgnoreCase))
                return true;
            return !string.IsNullOrEmpty(s.brand) && StringEquals(s.brand, brandFilter);
        }

        foreach (var show in currentPromotion.shows ?? new List<ShowData>())
        {
            if (!MatchBrand(show)) continue;
            if (!CalendarUtils.TryParseAny(show?.date, out var sd)) continue;
            if (!InWeek(sd)) continue;
            foreach (var m in show.matches ?? new List<MatchData>())
            {
                // participants
                var parts = new List<string>();
                void add(string s) { if (!string.IsNullOrWhiteSpace(s)) parts.Add(s.Trim()); }
                add(m.wrestlerA);
                add(m.wrestlerB);
                add(m.wrestlerC);
                add(m.wrestlerD);

                bool isTag = (!string.IsNullOrEmpty(m.matchType) && m.matchType.ToLowerInvariant().Contains("tag")) || (parts.Count >= 4);
                string winner = (m?.winner ?? string.Empty).Trim();
                bool isDraw = string.Equals(winner, "Draw", StringComparison.OrdinalIgnoreCase) || string.Equals(winner, "No Contest", StringComparison.OrdinalIgnoreCase);

                bool isMainEvent = show.matches != null && show.matches.Count > 0 && show.matches[show.matches.Count - 1] == m;

                if (type == RankingType.Singles)
                {
                    if (isTag || parts.Count != 2) continue; // only true 1v1
                    // filter participants by gender
                    if (!wrestlerByName.TryGetValue(parts[0], out var a) || !IncludeWrestler(a)) continue;
                    if (!wrestlerByName.TryGetValue(parts[1], out var b) || !IncludeWrestler(b)) continue;

                    Ensure(results, parts[0]);
                    Ensure(results, parts[1]);
                    if (isDraw)
                    {
                        results[parts[0]].draws++;
                        results[parts[1]].draws++;
                    }
                    else if (StringEquals(winner, parts[0]))
                    {
                        results[parts[0]].wins++;
                        results[parts[1]].losses++;
                        if (m.isTitleMatch) results[parts[0]].score += titleBonus;
                        if (isMainEvent) results[parts[0]].score += mainBonus;
                    }
                    else if (StringEquals(winner, parts[1]))
                    {
                        results[parts[1]].wins++;
                        results[parts[0]].losses++;
                        if (m.isTitleMatch) results[parts[1]].score += titleBonus;
                        if (isMainEvent) results[parts[1]].score += mainBonus;
                    }
                }
                else if (type == RankingType.TagTeam)
                {
                    if (!isTag) continue;
                    var team = TeamOfWinner(winner);
                    if (string.IsNullOrEmpty(team)) continue;
                    Ensure(results, team);
                    results[team].wins++;
                    if (m.isTitleMatch) results[team].score += titleBonus;
                    if (isMainEvent) results[team].score += mainBonus;
                }
                else if (type == RankingType.Stable)
                {
                    var stable = StableOfWinner(winner);
                    if (string.IsNullOrEmpty(stable)) continue;
                    Ensure(results, stable);
                    results[stable].wins++;
                    if (m.isTitleMatch) results[stable].score += titleBonus;
                    if (isMainEvent) results[stable].score += mainBonus;
                }
            }
        }

        // finalize list
        var list = new List<RankingEntry>(results.Values);
        foreach (var e in list)
        {
            e.score += e.wins * winPts + e.draws * drawPts + e.losses * lossPts;
        }
        list.Sort((x, y) =>
        {
            int c = y.score.CompareTo(x.score);
            if (c != 0) return c;
            c = x.losses.CompareTo(y.losses);
            if (c != 0) return c;
            return string.Compare(x.name, y.name, StringComparison.OrdinalIgnoreCase);
        });
        return list;

        void Ensure(Dictionary<string, RankingEntry> map, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (!map.TryGetValue(name, out var e))
            {
                e = new RankingEntry { name = name, wins = 0, losses = 0, draws = 0, score = 0 };
                map[name] = e;
            }
        }
    }

    private void DisplayRankingEntries(List<RankingEntry> entries, int topN)
    {
        if (rankingsListView == null) return;
        var items = new List<string>();
        if (entries != null)
        {
            for (int i = 0; i < Math.Min(topN, entries.Count); i++)
            {
                var e = entries[i];
                // Compact format: rank, name, record (omit score to shorten)
                items.Add($"{i + 1}. {e.name}  {e.wins}-{e.losses}-{e.draws}");
            }
        }
        if (items.Count == 0)
        {
            items.Add("No results for the selected week.");
        }
        rankingsListView.itemsSource = items;
        rankingsListView.Rebuild();
    }

    private void DisplayRankingEntriesWithTrend(List<RankingEntry> current, List<RankingEntry> previous, int topN)
    {
        if (rankingsListView == null)
        {
            DisplayRankingEntries(current, topN);
            return;
        }

        var prevIndexByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (previous != null)
        {
            for (int i = 0; i < previous.Count; i++)
            {
                var e = previous[i];
                if (e != null && !string.IsNullOrWhiteSpace(e.name) && !prevIndexByName.ContainsKey(e.name))
                    prevIndexByName[e.name] = i;
            }
        }

        var items = new List<string>();
        if (current != null)
        {
            for (int i = 0; i < Math.Min(topN, current.Count); i++)
            {
                var e = current[i];
                string trend = "new";
                if (e != null && !string.IsNullOrWhiteSpace(e.name) && prevIndexByName.TryGetValue(e.name, out var prevIdx))
                {
                    int diff = prevIdx - i; // positive = moved up
                    if (diff > 0) trend = $"▲{diff}";
                    else if (diff < 0) trend = $"▼{Math.Abs(diff)}";
                    else trend = "–";
                }
                items.Add($"{i + 1}. {e.name}  {e.wins}-{e.losses}-{e.draws}  ({trend})");
            }
        }
        if (items.Count == 0)
            items.Add("No results for the selected week.");

        rankingsListView.itemsSource = items;
        rankingsListView.Rebuild();
    }

    private void ApplyPromotionTheme()
    {
        // Choose the first defined brand as the primary identity, if any
        string primaryBrand = currentPromotion?.brands != null
            ? currentPromotion.brands.FirstOrDefault(b => !string.IsNullOrWhiteSpace(b))
            : null;

        var primary = BrandColors.GetPrimary(primaryBrand);
        var accent = BrandColors.GetAccent(primaryBrand);
        var text = BrandColors.GetText(primaryBrand);

        if (dashboardChrome != null)
        {
            dashboardChrome.style.backgroundColor = new StyleColor(primary);
        }
        if (dashboardTitleLabel != null)
        {
            dashboardTitleLabel.style.color = new StyleColor(accent);
        }
        if (statusLabel != null)
        {
            statusLabel.style.color = new StyleColor(accent);
        }

        // Optionally tint sidebar buttons slightly using the accent color
        var sidebar = root != null ? root.Q<VisualElement>("sidebar") : null;
        if (sidebar != null)
        {
            foreach (var btn in sidebar.Query<Button>().ToList())
            {
                btn.style.borderTopColor = new StyleColor(accent);
                btn.style.borderBottomColor = new StyleColor(accent);
            }
        }
    }

    private void SaveCurrentWeekSnapshot()
    {
        if (currentPromotion == null || currentRankingResults == null || currentRankingResults.Count == 0) return;
        var asOf = GetSelectedAsOfDate();
        var (weekStart, weekEnd) = GetWeekRange(asOf);
        var snap = new RankingSnapshot
        {
            promotionName = currentPromotion.promotionName,
            weekStartIso = CalendarUtils.FormatIso(weekStart),
            weekEndIso = CalendarUtils.FormatIso(weekEnd),
            type = GetSelectedRankingType(),
            division = GetSelectedDivision(),
            gender = GetSelectedGender(),
            brand = rankingsBrandDropdown != null ? (rankingsBrandDropdown.value ?? string.Empty).Trim() : string.Empty,
            topN = 10,
            top = new List<RankingEntry>()
        };
        foreach (var e in currentRankingResults)
        {
            if (snap.top.Count >= snap.topN) break;
            snap.top.Add(new RankingEntry { entityId = e.entityId, name = e.name, wins = e.wins, losses = e.losses, draws = e.draws, score = e.score });
        }

        rankingStore ??= new RankingStore { promotionName = currentPromotion.promotionName };
        rankingStore.promotionName = currentPromotion.promotionName;
        rankingStore.snapshots ??= new List<RankingSnapshot>();

        // Replace any existing snapshot with same key (type + week + filters)
        rankingStore.snapshots.RemoveAll(s => s != null && s.promotionName == snap.promotionName && s.type == snap.type && StringEquals(s.weekStartIso, snap.weekStartIso) && StringEquals(s.weekEndIso, snap.weekEndIso) && StringEquals(s.division, snap.division) && StringEquals(s.gender, snap.gender));
        rankingStore.snapshots.Add(snap);
        DataManager.SaveRankings(rankingStore);
        RefreshWeeksDropdown();
        if (statusLabel != null) statusLabel.text = "Snapshot saved.";
    }

    private (System.DateTime start, System.DateTime end) GetWeekRange(System.DateTime asOf)
    {
        // Sunday to Saturday
        int diff = (int)asOf.DayOfWeek; // Sunday=0
        var start = asOf.Date.AddDays(-diff);
        var end = start.AddDays(6);
        return (start, end);
    }

    // Build a readable vs-line for matches, using tag team names when possible
    private string BuildVsLine(MatchData m)
    {
        if (m == null) return string.Empty;
        bool isTag = (!string.IsNullOrEmpty(m.matchType) && m.matchType.IndexOf("tag", System.StringComparison.OrdinalIgnoreCase) >= 0) ||
                     (!string.IsNullOrEmpty(m.wrestlerA) && !string.IsNullOrEmpty(m.wrestlerB) && !string.IsNullOrEmpty(m.wrestlerC) && !string.IsNullOrEmpty(m.wrestlerD));
        if (isTag)
        {
            if (!string.IsNullOrEmpty(m.wrestlerA) && !string.IsNullOrEmpty(m.wrestlerB) && !string.IsNullOrEmpty(m.wrestlerC) && !string.IsNullOrEmpty(m.wrestlerD))
            {
                var left = TeamDisplay(m.wrestlerA, m.wrestlerB);
                var right = TeamDisplay(m.wrestlerC, m.wrestlerD);
                return $"{left} vs {right}";
            }
        }
        var parts = new System.Collections.Generic.List<string>();
        if (!string.IsNullOrEmpty(m.wrestlerA)) parts.Add(m.wrestlerA);
        if (!string.IsNullOrEmpty(m.wrestlerB)) parts.Add(m.wrestlerB);
        if (!string.IsNullOrEmpty(m.wrestlerC)) parts.Add(m.wrestlerC);
        if (!string.IsNullOrEmpty(m.wrestlerD)) parts.Add(m.wrestlerD);
        return parts.Count > 0 ? string.Join(" vs ", parts) : string.Empty;
    }

    private string BuildMatchDisplayLabel(MatchData m)
    {
        if (m == null) return "Match: (invalid)";
        string name = !string.IsNullOrEmpty(m.matchName) ? m.matchName : string.Empty;
        string vs = BuildVsLine(m);
        string label;
        if (!string.IsNullOrEmpty(name)) label = $"Match: {name}";
        else if (!string.IsNullOrEmpty(vs)) label = $"Match: {vs}";
        else label = $"Match: {(string.IsNullOrEmpty(m.matchType) ? "Match" : m.matchType)}";

        return m.isTitleMatch ? $"★ {label}" : label;
    }

    private string TeamDisplay(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return (a ?? string.Empty) + (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) ? string.Empty : " & ") + (b ?? string.Empty);
        tagTeamCollection ??= (currentPromotion != null ? DataManager.LoadTagTeams(currentPromotion.promotionName) : null);
        foreach (var t in tagTeamCollection?.teams ?? new List<TagTeamData>())
        {
            if (t == null) continue;
            bool match = (StringEquals(t.memberA, a) && StringEquals(t.memberB, b)) || (StringEquals(t.memberA, b) && StringEquals(t.memberB, a));
            if (match && !string.IsNullOrWhiteSpace(t.teamName)) return t.teamName;
        }
        return $"{a} & {b}";
    }
}













