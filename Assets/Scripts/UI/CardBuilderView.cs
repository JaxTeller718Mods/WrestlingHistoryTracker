using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class CardBuilderView
{
    private VisualElement panel;
    private VisualElement brandStripe;
    private TextField showNameField, showDateField;
    private DropdownField showVenueField, showCityField;
    private IntegerField showAttendanceField; private FloatField showRatingField;
    private DropdownField showTypeDropdown, showBrandDropdown;
    private DropdownField templateDropdown; private Button applyTemplateButton;
    private ScrollView entryListScroll; // container
    private ListView entryListView;
    private Button addMatchButton, addSegmentButton, saveButton, cancelButton, deleteEntryButton;

    private VisualElement noSelectionHint, matchEditor, segmentEditor;
    private TextField matchNameField, matchNotesField;
    private DropdownField matchTypeDropdown; private Toggle isTitleMatchToggle;
    private DropdownField wrestlerADropdown, wrestlerBDropdown, wrestlerCDropdown, wrestlerDDropdown, titleDropdown, winnerDropdown;
    private TextField segmentNameField, segmentTextField;
    private DropdownField segmentTypeDropdown, segmentParticipantADropdown, segmentParticipantBDropdown, segmentParticipantCDropdown, segmentParticipantDDropdown;

    private Func<PromotionData> promotionProvider;
    private PromotionData Promotion => promotionProvider?.Invoke();

    private ShowData workingShow;
    private string previousName; private string previousDate;

    // display list: tokens like "M:<id>" or "S:<id>"
    private readonly List<string> order = new();
    private int selectedIndex = -1;

    private List<string> wrestlerChoices = new();
    private readonly Dictionary<string, string> wrestlerIdByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> wrestlerNameById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private List<string> titleChoices = new();
    private VenueCityConfig venueCityConfig;

    public event Action<ShowData, string, string> Saved; // (show, prevName, prevDate)
    public event Action Canceled;

    public void Initialize(VisualElement cardBuilderPanel, Func<PromotionData> promotionGetter)
    {
        panel = cardBuilderPanel;
        promotionProvider = promotionGetter;

        brandStripe = panel.Q<VisualElement>("cbBrandStripe");
        showNameField = panel.Q<TextField>("cbShowNameField");
        showDateField = panel.Q<TextField>("cbShowDateField");
        showVenueField = panel.Q<DropdownField>("cbShowVenueField");
        showCityField = panel.Q<DropdownField>("cbShowCityField");
        showTypeDropdown = panel.Q<DropdownField>("cbShowTypeDropdown");
        showBrandDropdown = panel.Q<DropdownField>("cbShowBrandDropdown");
        showAttendanceField = panel.Q<IntegerField>("cbShowAttendanceField");
        showRatingField = panel.Q<FloatField>("cbShowRatingField");
        templateDropdown = panel.Q<DropdownField>("cbTemplateDropdown");
        applyTemplateButton = panel.Q<Button>("cbApplyTemplateButton");
        entryListScroll = panel.Q<ScrollView>("cbEntryListScroll");
        addMatchButton = panel.Q<Button>("cbAddMatchButton");
        addSegmentButton = panel.Q<Button>("cbAddSegmentButton");
        saveButton = panel.Q<Button>("cbSaveShowButton");
        cancelButton = panel.Q<Button>("cbCancelShowButton");
        deleteEntryButton = panel.Q<Button>("cbDeleteEntryButton");

        noSelectionHint = panel.Q<VisualElement>("cbNoSelectionHint");
        matchEditor = panel.Q<VisualElement>("cbMatchEditor");
        segmentEditor = panel.Q<VisualElement>("cbSegmentEditor");
        matchNameField = panel.Q<TextField>("cbMatchNameField");
        matchTypeDropdown = panel.Q<DropdownField>("cbMatchTypeDropdown");
        wrestlerADropdown = panel.Q<DropdownField>("cbWrestlerADropdown");
        wrestlerBDropdown = panel.Q<DropdownField>("cbWrestlerBDropdown");
        wrestlerCDropdown = panel.Q<DropdownField>("cbWrestlerCDropdown");
        wrestlerDDropdown = panel.Q<DropdownField>("cbWrestlerDDropdown");
        isTitleMatchToggle = panel.Q<Toggle>("cbIsTitleMatchToggle");
        titleDropdown = panel.Q<DropdownField>("cbTitleDropdown");
        winnerDropdown = panel.Q<DropdownField>("cbWinnerDropdown");
        matchNotesField = panel.Q<TextField>("cbMatchNotesField");
        segmentNameField = panel.Q<TextField>("cbSegmentNameField");
        segmentTypeDropdown = panel.Q<DropdownField>("cbSegmentTypeDropdown");
        segmentParticipantADropdown = panel.Q<DropdownField>("cbSegmentParticipantADropdown");
        segmentParticipantBDropdown = panel.Q<DropdownField>("cbSegmentParticipantBDropdown");
        segmentParticipantCDropdown = panel.Q<DropdownField>("cbSegmentParticipantCDropdown");
        segmentParticipantDDropdown = panel.Q<DropdownField>("cbSegmentParticipantDDropdown");
        segmentTextField = panel.Q<TextField>("cbSegmentTextField");

        // Defaults
        matchTypeDropdown.choices = new List<string> { "Singles", "Tag Team", "Triple Threat", "Four Way" };
        templateDropdown.choices = ShowTemplates.Templates.Select(t => t.name).ToList();
        if (templateDropdown.choices.Count > 0) templateDropdown.value = templateDropdown.choices[0];
        if (showBrandDropdown != null)
        {
            var brands = Promotion?.brands ?? new List<string>();
            var brandChoices = new List<string> { "" };
            brandChoices.AddRange(brands.Where(b => !string.IsNullOrWhiteSpace(b)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(b => b));
            showBrandDropdown.choices = brandChoices;
            showBrandDropdown.value = "";

            showBrandDropdown.RegisterValueChangedCallback(e =>
            {
                if (workingShow != null) workingShow.brand = e.newValue;
                UpdateBrandStripe(e.newValue);
            });
        }
        if (showTypeDropdown != null)
        {
            showTypeDropdown.choices = new List<string> { "TV", "PPV", "House" };
            showTypeDropdown.value = "TV";
        }

        if (segmentTypeDropdown != null)
        {
            var types = SegmentTypeCatalog.Types != null && SegmentTypeCatalog.Types.Count > 0
                ? new List<string>(SegmentTypeCatalog.Types)
                : new List<string> { "Segment" };
            segmentTypeDropdown.choices = types;
            if (string.IsNullOrEmpty(segmentTypeDropdown.value)) segmentTypeDropdown.value = types[0];
        }

        if (applyTemplateButton != null) applyTemplateButton.clicked += ApplyTemplate;
        if (addMatchButton != null) addMatchButton.clicked += () => { AddMatch(); RefreshEntryList(); };
        if (addSegmentButton != null) addSegmentButton.clicked += () => { AddSegment(); RefreshEntryList(); };
        if (saveButton != null) saveButton.clicked += Save;
        if (cancelButton != null) cancelButton.clicked += () => { Canceled?.Invoke(); };
        if (deleteEntryButton != null) deleteEntryButton.clicked += DeleteSelectedEntry;

        // Build ListView for entries inside the scroll container
        if (entryListScroll != null && entryListView == null)
        {
            entryListView = new ListView(order, 28, MakeEntryItem, BindEntryItem)
            {
                selectionType = SelectionType.Single,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                name = "cbEntryListView"
            };
            entryListView.style.flexGrow = 1;
            entryListView.selectionChanged += objs =>
            {
                var first = objs?.Cast<string>().FirstOrDefault();
                int idx = first != null ? order.IndexOf(first) : -1;
                SelectEntry(idx);
            };
            entryListScroll.Add(entryListView);
        }

        // React to dropdown changes
        wrestlerADropdown?.RegisterValueChangedCallback(e => { UpdateWinnerChoices(); });
        wrestlerBDropdown?.RegisterValueChangedCallback(e => { UpdateWinnerChoices(); });
        wrestlerCDropdown?.RegisterValueChangedCallback(e => { UpdateWinnerChoices(); });
        wrestlerDDropdown?.RegisterValueChangedCallback(e => { UpdateWinnerChoices(); });

        LoadRosterAndTitles();

        // Ensure dropdown menus overlay cleanly without reordering rows
        SetupDropdownOverlay(templateDropdown);
        SetupDropdownOverlay(matchTypeDropdown);
        SetupDropdownOverlay(wrestlerADropdown);
        SetupDropdownOverlay(wrestlerBDropdown);
        SetupDropdownOverlay(wrestlerCDropdown);
        SetupDropdownOverlay(wrestlerDDropdown);
        SetupDropdownOverlay(titleDropdown);
        SetupDropdownOverlay(winnerDropdown);

        // Load venue/city config once
        venueCityConfig = VenueCityConfigStore.LoadOrCreateDefault();
        RefreshVenueCityChoices();
    }

    private void SetupDropdownOverlay(DropdownField dd)
    {
        if (dd == null) return;
        // Avoid changing layout order; if needed, bring the containing panel forward.
        dd.RegisterCallback<PointerDownEvent>(_ => { panel?.BringToFront(); });
        dd.RegisterCallback<FocusInEvent>(_ => { panel?.BringToFront(); });
    }

    private void RefreshVenueCityChoices()
    {
        if (venueCityConfig == null)
            venueCityConfig = VenueCityConfigStore.LoadOrCreateDefault();

        var venueChoices = new List<string> { string.Empty };
        if (venueCityConfig.venues != null)
            venueChoices.AddRange(venueCityConfig.venues);

        var cityChoices = new List<string> { string.Empty };
        if (venueCityConfig.cities != null)
            cityChoices.AddRange(venueCityConfig.cities);

        if (showVenueField != null)
        {
            showVenueField.choices = venueChoices;
            if (!venueChoices.Contains(showVenueField.value))
                showVenueField.value = string.Empty;
        }
        if (showCityField != null)
        {
            showCityField.choices = cityChoices;
            if (!cityChoices.Contains(showCityField.value))
                showCityField.value = string.Empty;
        }
    }

    public void BeginNew(DateTime date)
    {
        LoadRosterAndTitles();
        workingShow = new ShowData("New Show", CalendarUtils.FormatIso(date))
        {
            venue = string.Empty,
            city = string.Empty,
            attendance = 0,
            rating = 0f,
            matches = new List<MatchData>(),
            segments = new List<SegmentData>(),
            entryOrder = new List<string>()
        };
        previousName = null; previousDate = null;
        order.Clear();
        ApplyWorkingShowToUI();
    }

    public void BeginEdit(ShowData existing)
    {
        LoadRosterAndTitles();
        workingShow = existing;
        previousName = existing.showName;
        previousDate = existing.date;
        // Build order list from existing entryOrder or infer
        order.Clear();
        if (existing.entryOrder != null && existing.entryOrder.Count > 0)
        {
            foreach (var tok in existing.entryOrder)
            {
                order.Add(tok);
            }
        }
        else
        {
            for (int i = 0; i < (existing.matches?.Count ?? 0); i++)
            {
                var id = existing.matches[i]?.id;
                if (string.IsNullOrEmpty(id)) { id = Guid.NewGuid().ToString("N"); existing.matches[i].id = id; }
                order.Add($"M:{id}");
            }
            for (int i = 0; i < (existing.segments?.Count ?? 0); i++)
            {
                var id = existing.segments[i]?.id;
                if (string.IsNullOrEmpty(id)) { id = Guid.NewGuid().ToString("N"); existing.segments[i].id = id; }
                order.Add($"S:{id}");
            }
        }
        ApplyWorkingShowToUI();
    }

    /// <summary>
    /// Open an existing show in edit mode and, if possible, select
    /// a specific match or segment entry by its ID.
    /// </summary>
    /// <param name="existing">Show to edit.</param>
    /// <param name="kind">Entry kind: 'M' for match, 'S' for segment.</param>
    /// <param name="entryId">MatchData.id or SegmentData.id to focus.</param>
    public void BeginEditAndSelectEntry(ShowData existing, char kind, string entryId)
    {
        BeginEdit(existing);
        if (string.IsNullOrEmpty(entryId))
            return;

        int idx = -1;
        for (int i = 0; i < order.Count; i++)
        {
            var (k, id) = ParseToken(order[i]);
            if (k == kind && string.Equals(id, entryId, StringComparison.OrdinalIgnoreCase))
            {
                idx = i;
                break;
            }
        }
        if (idx >= 0)
        {
            SelectEntry(idx);
        }
    }

    private void ApplyWorkingShowToUI()
    {
        if (workingShow == null) return;
        showNameField.value = workingShow.showName ?? string.Empty;
        showDateField.value = CalendarUtils.NormalizeToIso(workingShow.date);
        RefreshVenueCityChoices();
        if (showVenueField != null)
        {
            var v = workingShow.venue ?? string.Empty;
            if (!string.IsNullOrEmpty(v) && showVenueField.choices != null && !showVenueField.choices.Contains(v))
            {
                var list = new List<string>(showVenueField.choices);
                list.Add(v);
                showVenueField.choices = list;
            }
            showVenueField.value = v;
        }
        if (showCityField != null)
        {
            var c = workingShow.city ?? string.Empty;
            if (!string.IsNullOrEmpty(c) && showCityField.choices != null && !showCityField.choices.Contains(c))
            {
                var list = new List<string>(showCityField.choices);
                list.Add(c);
                showCityField.choices = list;
            }
            showCityField.value = c;
        }
        showAttendanceField.value = workingShow.attendance;
        showRatingField.value = workingShow.rating;
        if (showTypeDropdown != null)
        {
            var t = workingShow.showType ?? string.Empty;
            if (string.IsNullOrEmpty(t)) t = "TV";
            if (showTypeDropdown.choices == null || showTypeDropdown.choices.Count == 0)
                showTypeDropdown.choices = new List<string> { "TV", "PPV", "House" };
            if (!showTypeDropdown.choices.Contains(t))
            {
                var list = new List<string>(showTypeDropdown.choices);
                list.Add(t);
                showTypeDropdown.choices = list;
            }
            showTypeDropdown.value = t;
        }
        if (showBrandDropdown != null)
        {
            var b = workingShow.brand ?? string.Empty;
            if (!string.IsNullOrEmpty(b) && !showBrandDropdown.choices.Contains(b))
            {
                var list = new List<string>(showBrandDropdown.choices ?? new List<string>());
                list.Add(b);
                showBrandDropdown.choices = list;
            }
            showBrandDropdown.value = b;
            UpdateBrandStripe(b);
        }
        RefreshEntryList();
        SelectEntry(-1); // clear selection
    }

    private void UpdateBrandStripe(string brand)
    {
        if (brandStripe == null) return;
        var primary = BrandColors.GetPrimary(brand);
        brandStripe.style.backgroundColor = new StyleColor(primary);
    }

    private void RefreshEntryList()
    {
        if (entryListView != null)
        {
            entryListView.itemsSource = null; // force rebind
            entryListView.itemsSource = order;
            entryListView.RefreshItems();
        }
    }

    private VisualElement MakeEntryItem()
    {
        var b = new Button();
        b.AddToClassList("card-entry-button");
        b.style.unityTextAlign = TextAnchor.MiddleLeft;
        return b;
    }

    private void BindEntryItem(VisualElement ve, int index)
    {
        if (index < 0 || index >= order.Count) return;
        var tok = order[index];
        var (kind, id) = ParseToken(tok);
        string label = kind == 'M' ? GetMatchLabel(id) : GetSegmentLabel(id);
        if (ve is Button b)
        {
            b.text = label;
            b.clicked -= null; // clear
            b.clicked += () => SelectEntry(index);
        }
    }

    private void LoadRosterAndTitles()
    {
        try
        {
            var pn = Promotion?.promotionName;
            // Wrestlers
            wrestlerChoices = new List<string>();
            wrestlerIdByName.Clear();
            wrestlerNameById.Clear();
            var wc = DataManager.LoadWrestlers(pn);
            if (wc?.wrestlers != null)
            {
                wrestlerChoices = wc.wrestlers
                    .Where(w => w != null && !string.IsNullOrWhiteSpace(w.name))
                    .Select(w => w.name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(n => n)
                    .ToList();
                foreach (var w in wc.wrestlers)
                {
                    if (w == null || string.IsNullOrEmpty(w.name) || string.IsNullOrEmpty(w.id)) continue;
                    wrestlerIdByName[w.name] = w.id;
                    if (!wrestlerNameById.ContainsKey(w.id))
                        wrestlerNameById[w.id] = w.name;
                }
            }
            // Titles
            titleChoices = new List<string>();
            var tc = DataManager.LoadTitles(pn);
            if (tc?.titles != null)
            {
                titleChoices = tc.titles
                    .Where(t => t != null && !string.IsNullOrWhiteSpace(t.titleName))
                    .Select(t => t.titleName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(n => n)
                    .ToList();
            }

            // Apply to dropdowns
            SetDropdownChoices(wrestlerADropdown, wrestlerChoices);
            SetDropdownChoices(wrestlerBDropdown, wrestlerChoices);
            SetDropdownChoices(wrestlerCDropdown, wrestlerChoices, allowEmpty: true);
            SetDropdownChoices(wrestlerDDropdown, wrestlerChoices, allowEmpty: true);
            SetDropdownChoices(titleDropdown, titleChoices, allowEmpty: true);
            SetSegmentParticipantChoices();
            UpdateWinnerChoices();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"CardBuilder: failed to load roster/titles: {ex.Message}");
        }
    }

    private void SetSegmentParticipantChoices()
    {
        SetDropdownChoices(segmentParticipantADropdown, wrestlerChoices, allowEmpty: true);
        SetDropdownChoices(segmentParticipantBDropdown, wrestlerChoices, allowEmpty: true);
        SetDropdownChoices(segmentParticipantCDropdown, wrestlerChoices, allowEmpty: true);
        SetDropdownChoices(segmentParticipantDDropdown, wrestlerChoices, allowEmpty: true);
    }

    private void SetDropdownChoices(DropdownField dd, List<string> source, bool allowEmpty = false)
    {
        if (dd == null) return;
        var list = new List<string>();
        if (allowEmpty) list.Add("");
        if (source != null && source.Count > 0) list.AddRange(source);
        dd.choices = list;
        if (allowEmpty) dd.value = list.FirstOrDefault() ?? "";
        else dd.value = list.FirstOrDefault();
    }

    private void UpdateWinnerChoices(string preferred = null)
    {
        if (winnerDropdown == null) return;
        var opts = new List<string>();
        void add(string s) { if (!string.IsNullOrWhiteSpace(s) && !opts.Contains(s, StringComparer.OrdinalIgnoreCase)) opts.Add(s); }
        add(wrestlerADropdown?.value);
        add(wrestlerBDropdown?.value);
        add(wrestlerCDropdown?.value);
        add(wrestlerDDropdown?.value);
        // common outcomes
        opts.Add("Draw");
        opts.Add("No Contest");
        winnerDropdown.choices = opts;
        // Try keep previous or preferred winner
        var prev = preferred ?? winnerDropdown.value;
        winnerDropdown.value = (!string.IsNullOrWhiteSpace(prev) && opts.Contains(prev)) ? prev : opts.FirstOrDefault();
    }

    // No text-to-dropdown sync needed; wrestler inputs are dropdown-only now.

    private string FindChoice(List<string> list, string value)
    {
        if (string.IsNullOrWhiteSpace(value) || list == null) return list?.FirstOrDefault();
        var found = list.FirstOrDefault(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase));
        return found ?? list.FirstOrDefault();
    }

    private void SelectEntry(int idx)
    {
        selectedIndex = idx;
        if (idx < 0 || idx >= order.Count)
        {
            noSelectionHint?.RemoveFromClassList("hidden");
            matchEditor?.AddToClassList("hidden");
            segmentEditor?.AddToClassList("hidden");
            return;
        }
        noSelectionHint?.AddToClassList("hidden");
        var (kind, id) = ParseToken(order[idx]);
        if (kind == 'M')
        {
            var m = EnsureMatch(id);
            matchEditor?.RemoveFromClassList("hidden");
            segmentEditor?.AddToClassList("hidden");
            matchNameField.value = m.matchName;
            matchTypeDropdown.value = string.IsNullOrEmpty(m.matchType) ? matchTypeDropdown.choices.FirstOrDefault() : m.matchType;
            wrestlerADropdown.value = FindChoice(wrestlerChoices, m.wrestlerA);
            wrestlerBDropdown.value = FindChoice(wrestlerChoices, m.wrestlerB);
            wrestlerCDropdown.value = string.IsNullOrWhiteSpace(m.wrestlerC)
                ? ""
                : FindChoice(wrestlerChoices, m.wrestlerC);
            wrestlerDDropdown.value = string.IsNullOrWhiteSpace(m.wrestlerD)
                ? ""
                : FindChoice(wrestlerChoices, m.wrestlerD);
            isTitleMatchToggle.value = m.isTitleMatch;
            if (titleDropdown != null) titleDropdown.value = FindChoice(titleChoices, m.titleName);
            UpdateWinnerChoices(m.winner);
            matchNotesField.value = m.notes;
        }
        else
        {
            var s = EnsureSegment(id);
            segmentEditor?.RemoveFromClassList("hidden");
            matchEditor?.AddToClassList("hidden");
            segmentNameField.value = s.name;
            if (segmentTypeDropdown != null)
            {
                if (segmentTypeDropdown.choices == null || segmentTypeDropdown.choices.Count == 0)
                {
                    var types = SegmentTypeCatalog.Types != null && SegmentTypeCatalog.Types.Count > 0
                        ? new List<string>(SegmentTypeCatalog.Types)
                        : new List<string> { "Segment" };
                    segmentTypeDropdown.choices = types;
                }
                segmentTypeDropdown.value = string.IsNullOrEmpty(s.segmentType)
                    ? (segmentTypeDropdown.choices?.FirstOrDefault() ?? string.Empty)
                    : s.segmentType;
            }
            segmentParticipantADropdown.value = ResolveSegmentParticipantName(s, 0);
            segmentParticipantBDropdown.value = ResolveSegmentParticipantName(s, 1);
            segmentParticipantCDropdown.value = ResolveSegmentParticipantName(s, 2);
            segmentParticipantDDropdown.value = ResolveSegmentParticipantName(s, 3);
            segmentTextField.value = s.text;
        }
    }

    private void ApplyTemplate()
    {
        var t = ShowTemplates.Templates.FirstOrDefault(x => x.name == templateDropdown.value);
        if (t == null) return;
        if (workingShow == null) workingShow = new ShowData("New Show", CalendarUtils.FormatIso(DateTime.Today));
        workingShow.matches ??= new List<MatchData>();
        workingShow.segments ??= new List<SegmentData>();
        order.Clear();
        if (t.openerPromo)
        {
            var seg = new SegmentData { id = Guid.NewGuid().ToString("N"), name = "Opening Promo", text = string.Empty };
            workingShow.segments.Add(seg);
            order.Add($"S:{seg.id}");
        }
        for (int i = 0; i < t.matches; i++)
        {
            var m = new MatchData { id = Guid.NewGuid().ToString("N"), matchType = "Singles", matchName = $"Match {i+1}" };
            workingShow.matches.Add(m);
            order.Add($"M:{m.id}");
        }
        if (t.midPromo)
        {
            var seg2 = new SegmentData { id = Guid.NewGuid().ToString("N"), name = "Backstage Promo", text = string.Empty };
            workingShow.segments.Add(seg2);
            order.Add($"S:{seg2.id}");
        }
        RefreshEntryList();
        SelectEntry(order.Count > 0 ? 0 : -1);
    }

    private void AddMatch()
    {
        workingShow.matches ??= new List<MatchData>();
        var m = new MatchData { id = Guid.NewGuid().ToString("N"), matchType = "Singles", matchName = "New Match" };
        workingShow.matches.Add(m);
        order.Add($"M:{m.id}");
    }

    private void AddSegment()
    {
        workingShow.segments ??= new List<SegmentData>();
        var s = new SegmentData { id = Guid.NewGuid().ToString("N"), name = "Segment", text = string.Empty };
        workingShow.segments.Add(s);
        order.Add($"S:{s.id}");
    }

    private void DeleteSelectedEntry()
    {
        if (selectedIndex < 0 || selectedIndex >= order.Count) return;

        var (kind, id) = ParseToken(order[selectedIndex]);

        if (kind == 'M' && workingShow?.matches != null)
        {
            workingShow.matches.RemoveAll(m => m != null && m.id == id);
        }
        else if (kind == 'S' && workingShow?.segments != null)
        {
            workingShow.segments.RemoveAll(s => s != null && s.id == id);
        }

        order.RemoveAt(selectedIndex);
        selectedIndex = -1;

        matchEditor?.AddToClassList("hidden");
        segmentEditor?.AddToClassList("hidden");
        noSelectionHint?.RemoveFromClassList("hidden");

        RefreshEntryList();
    }

    private void Save()
    {
        if (workingShow == null) return;
        // Apply meta
        workingShow.showName = showNameField.value?.Trim();
        workingShow.date = CalendarUtils.NormalizeToIso(showDateField.value);
        workingShow.venue = showVenueField.value?.Trim();
        workingShow.city = showCityField.value?.Trim();
        workingShow.attendance = showAttendanceField.value;
        workingShow.rating = showRatingField.value;
        if (showTypeDropdown != null)
            workingShow.showType = string.IsNullOrWhiteSpace(showTypeDropdown.value) ? null : showTypeDropdown.value.Trim();
        if (showBrandDropdown != null)
            workingShow.brand = string.IsNullOrWhiteSpace(showBrandDropdown.value) ? null : showBrandDropdown.value.Trim();
        workingShow.entryOrder = new List<string>(order);

        // Apply entry editor if modified
        if (selectedIndex >= 0 && selectedIndex < order.Count)
        {
            var (kind, id) = ParseToken(order[selectedIndex]);
            if (kind == 'M')
            {
                var m = EnsureMatch(id);
                // Auto-generate match name if user left it blank
                var name = matchNameField != null ? (matchNameField.value ?? string.Empty).Trim() : string.Empty;
                if (string.IsNullOrEmpty(name))
                {
                    string type = matchTypeDropdown != null ? (matchTypeDropdown.value ?? string.Empty).Trim() : string.Empty;
                    string title = titleDropdown != null ? (titleDropdown.value ?? string.Empty).Trim() : string.Empty;
                    bool isTitleMatch = isTitleMatchToggle != null && isTitleMatchToggle.value;

                    string a = wrestlerADropdown != null ? (wrestlerADropdown.value ?? string.Empty).Trim() : string.Empty;
                    string b = wrestlerBDropdown != null ? (wrestlerBDropdown.value ?? string.Empty).Trim() : string.Empty;
                    string c = wrestlerCDropdown != null ? (wrestlerCDropdown.value ?? string.Empty).Trim() : string.Empty;
                    string d = wrestlerDDropdown != null ? (wrestlerDDropdown.value ?? string.Empty).Trim() : string.Empty;

                    var participants = new List<string>();
                    if (!string.IsNullOrEmpty(a)) participants.Add(a);
                    if (!string.IsNullOrEmpty(b)) participants.Add(b);
                    if (!string.IsNullOrEmpty(c)) participants.Add(c);
                    if (!string.IsNullOrEmpty(d)) participants.Add(d);

                    string vsPart = string.Empty;
                    bool isTagType = !string.IsNullOrEmpty(type) && type.IndexOf("tag", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (participants.Count == 2)
                        vsPart = $"{participants[0]} vs {participants[1]}";
                    else if (participants.Count == 3)
                        vsPart = $"{participants[0]} vs {participants[1]} vs {participants[2]}";
                    else if (participants.Count == 4)
                    {
                        if (isTagType)
                            vsPart = $"{participants[0]} & {participants[1]} vs {participants[2]} & {participants[3]}";
                        else
                            vsPart = $"{participants[0]} vs {participants[1]} vs {participants[2]} vs {participants[3]}";
                    }
                    else if (participants.Count > 1)
                        vsPart = string.Join(" vs ", participants);
                    else if (participants.Count == 1)
                        vsPart = participants[0];

                    string prefix = string.Empty;
                    if (isTitleMatch && !string.IsNullOrEmpty(title))
                        prefix = $"{title} - ";
                    else if (!string.IsNullOrEmpty(type))
                        prefix = $"{type} - ";

                    if (!string.IsNullOrEmpty(vsPart))
                        name = prefix + vsPart;
                    else if (!string.IsNullOrEmpty(title))
                        name = title;
                    else if (!string.IsNullOrEmpty(type))
                        name = type;
                    else
                        name = "Match";
                }

                m.matchName = name;
                m.matchType = matchTypeDropdown.value;
                m.wrestlerA = wrestlerADropdown?.value;
                m.wrestlerB = wrestlerBDropdown?.value;
                m.wrestlerC = wrestlerCDropdown?.value;
                m.wrestlerD = wrestlerDDropdown?.value;
                m.isTitleMatch = isTitleMatchToggle.value;
                m.titleName = titleDropdown?.value;
                m.winner = winnerDropdown?.value;
                m.notes = matchNotesField.value;
            }
            else
            {
                var s = EnsureSegment(id);
                var rawName = segmentNameField != null ? segmentNameField.value : null;
                var segType = segmentTypeDropdown != null ? (segmentTypeDropdown.value ?? string.Empty).Trim() : string.Empty;
                s.segmentType = string.IsNullOrEmpty(segType) ? null : segType;
                s.text = segmentTextField.value;
                var participantNames = new List<string>();
                void add(DropdownField dd)
                {
                    var v = dd != null ? (dd.value ?? string.Empty).Trim() : string.Empty;
                    if (string.IsNullOrEmpty(v)) return;
                    if (!participantNames.Any(x => string.Equals(x, v, StringComparison.OrdinalIgnoreCase)))
                        participantNames.Add(v);
                }
                add(segmentParticipantADropdown);
                add(segmentParticipantBDropdown);
                add(segmentParticipantCDropdown);
                add(segmentParticipantDDropdown);
                s.participantNames = participantNames;
                s.participantIds = participantNames
                    .Select(GetWrestlerId)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .ToList();
                s.name = BuildSegmentAutoName(rawName, s.segmentType, s.participantNames);
            }
        }

        Saved?.Invoke(workingShow, previousName, previousDate);
    }

    private string GetWrestlerId(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return wrestlerIdByName.TryGetValue(name, out var id) ? id : null;
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

    private (char kind, string id) ParseToken(string token)
    {
        if (string.IsNullOrEmpty(token)) return ('?', string.Empty);
        var parts = token.Split(':');
        if (parts.Length == 2) return (parts[0].Length > 0 ? parts[0][0] : '?', parts[1]);
        // legacy index tokens "M:0" etc. we cannot map reliably here
        return ('?', token);
    }

    private MatchData EnsureMatch(string id)
    {
        workingShow.matches ??= new List<MatchData>();
        var m = workingShow.matches.FirstOrDefault(x => x != null && x.id == id);
        if (m == null)
        {
            m = new MatchData { id = id };
            workingShow.matches.Add(m);
        }
        return m;
    }

    private SegmentData EnsureSegment(string id)
    {
        workingShow.segments ??= new List<SegmentData>();
        var s = workingShow.segments.FirstOrDefault(x => x != null && x.id == id);
        if (s == null)
        {
                s = new SegmentData { id = id };
                workingShow.segments.Add(s);
            }
            return s;
        }

    private string ResolveSegmentParticipantName(SegmentData segment, int index)
    {
        if (segment == null || index < 0) return string.Empty;
        var names = segment.participantNames ?? new List<string>();
        if (index < names.Count && !string.IsNullOrEmpty(names[index])) return FindChoice(wrestlerChoices, names[index]);

        var ids = segment.participantIds ?? new List<string>();
        if (index < ids.Count)
        {
            var id = ids[index];
            if (!string.IsNullOrEmpty(id) && wrestlerNameById.TryGetValue(id, out var resolved))
                return FindChoice(wrestlerChoices, resolved);
        }
        return string.Empty;
    }

    private string GetMatchLabel(string id)
    {
        var m = workingShow?.matches?.FirstOrDefault(x => x != null && x.id == id);
        if (m == null) return "(Missing Match)";
        string baseName = string.IsNullOrWhiteSpace(m.matchName) ? m.matchType ?? "Match" : m.matchName;
        return m.isTitleMatch ? $"★ [M] {baseName}" : $"[M] {baseName}";
    }

    private string GetSegmentLabel(string id)
    {
        var s = workingShow?.segments?.FirstOrDefault(x => x != null && x.id == id);
        if (s == null) return "(Missing Segment)";
        string baseName = string.IsNullOrWhiteSpace(s.name) ? "Segment" : s.name;
        return $"[S] {baseName}";
    }
}
