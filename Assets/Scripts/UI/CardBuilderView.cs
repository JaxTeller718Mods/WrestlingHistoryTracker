using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class CardBuilderView
{
    private VisualElement panel;
    private TextField showNameField, showDateField, showVenueField, showCityField;
    private IntegerField showAttendanceField; private FloatField showRatingField;
    private DropdownField templateDropdown; private Button applyTemplateButton;
    private ScrollView entryListScroll; // container
    private ListView entryListView;
    private Button addMatchButton, addSegmentButton, saveButton, cancelButton;

    private VisualElement noSelectionHint, matchEditor, segmentEditor;
    private TextField matchNameField, matchNotesField;
    private DropdownField matchTypeDropdown; private Toggle isTitleMatchToggle;
    private DropdownField wrestlerADropdown, wrestlerBDropdown, wrestlerCDropdown, wrestlerDDropdown, titleDropdown, winnerDropdown;
    private TextField segmentNameField, segmentTextField;

    private Func<PromotionData> promotionProvider;
    private PromotionData Promotion => promotionProvider?.Invoke();

    private ShowData workingShow;
    private string previousName; private string previousDate;

    // display list: tokens like "M:<id>" or "S:<id>"
    private readonly List<string> order = new();
    private int selectedIndex = -1;

    private List<string> wrestlerChoices = new();
    private List<string> titleChoices = new();

    public event Action<ShowData, string, string> Saved; // (show, prevName, prevDate)
    public event Action Canceled;

    public void Initialize(VisualElement cardBuilderPanel, Func<PromotionData> promotionGetter)
    {
        panel = cardBuilderPanel;
        promotionProvider = promotionGetter;

        showNameField = panel.Q<TextField>("cbShowNameField");
        showDateField = panel.Q<TextField>("cbShowDateField");
        showVenueField = panel.Q<TextField>("cbShowVenueField");
        showCityField = panel.Q<TextField>("cbShowCityField");
        showAttendanceField = panel.Q<IntegerField>("cbShowAttendanceField");
        showRatingField = panel.Q<FloatField>("cbShowRatingField");
        templateDropdown = panel.Q<DropdownField>("cbTemplateDropdown");
        applyTemplateButton = panel.Q<Button>("cbApplyTemplateButton");
        entryListScroll = panel.Q<ScrollView>("cbEntryListScroll");
        addMatchButton = panel.Q<Button>("cbAddMatchButton");
        addSegmentButton = panel.Q<Button>("cbAddSegmentButton");
        saveButton = panel.Q<Button>("cbSaveShowButton");
        cancelButton = panel.Q<Button>("cbCancelShowButton");

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
        segmentTextField = panel.Q<TextField>("cbSegmentTextField");

        // Defaults
        matchTypeDropdown.choices = new List<string> { "Singles", "Tag Team", "Triple Threat", "Four Way" };
        templateDropdown.choices = ShowTemplates.Templates.Select(t => t.name).ToList();
        if (templateDropdown.choices.Count > 0) templateDropdown.value = templateDropdown.choices[0];

        if (applyTemplateButton != null) applyTemplateButton.clicked += ApplyTemplate;
        if (addMatchButton != null) addMatchButton.clicked += () => { AddMatch(); RefreshEntryList(); };
        if (addSegmentButton != null) addSegmentButton.clicked += () => { AddSegment(); RefreshEntryList(); };
        if (saveButton != null) saveButton.clicked += Save;
        if (cancelButton != null) cancelButton.clicked += () => { Canceled?.Invoke(); };

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
    }

    private void SetupDropdownOverlay(DropdownField dd)
    {
        if (dd == null) return;
        // Avoid changing layout order; if needed, bring the containing panel forward.
        dd.RegisterCallback<PointerDownEvent>(_ => { panel?.BringToFront(); });
        dd.RegisterCallback<FocusInEvent>(_ => { panel?.BringToFront(); });
    }

    public void BeginNew(DateTime date)
    {
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

    private void ApplyWorkingShowToUI()
    {
        if (workingShow == null) return;
        showNameField.value = workingShow.showName ?? string.Empty;
        showDateField.value = CalendarUtils.NormalizeToIso(workingShow.date);
        showVenueField.value = workingShow.venue ?? string.Empty;
        showCityField.value = workingShow.city ?? string.Empty;
        showAttendanceField.value = workingShow.attendance;
        showRatingField.value = workingShow.rating;
        RefreshEntryList();
        SelectEntry(-1); // clear selection
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
            var wc = DataManager.LoadWrestlers(pn);
            if (wc?.wrestlers != null)
            {
                wrestlerChoices = wc.wrestlers
                    .Where(w => w != null && !string.IsNullOrWhiteSpace(w.name))
                    .Select(w => w.name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(n => n)
                    .ToList();
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
            UpdateWinnerChoices();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"CardBuilder: failed to load roster/titles: {ex.Message}");
        }
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
            wrestlerCDropdown.value = FindChoice(wrestlerChoices, m.wrestlerC);
            wrestlerDDropdown.value = FindChoice(wrestlerChoices, m.wrestlerD);
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
        workingShow.entryOrder = new List<string>(order);

        // Apply entry editor if modified
        if (selectedIndex >= 0 && selectedIndex < order.Count)
        {
            var (kind, id) = ParseToken(order[selectedIndex]);
            if (kind == 'M')
            {
                var m = EnsureMatch(id);
                m.matchName = matchNameField.value;
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
                s.name = segmentNameField.value;
                s.text = segmentTextField.value;
            }
        }

        Saved?.Invoke(workingShow, previousName, previousDate);
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

    private string GetMatchLabel(string id)
    {
        var m = workingShow?.matches?.FirstOrDefault(x => x != null && x.id == id);
        if (m == null) return "(Missing Match)";
        string baseName = string.IsNullOrWhiteSpace(m.matchName) ? m.matchType ?? "Match" : m.matchName;
        return $"[M] {baseName}";
    }

    private string GetSegmentLabel(string id)
    {
        var s = workingShow?.segments?.FirstOrDefault(x => x != null && x.id == id);
        if (s == null) return "(Missing Segment)";
        string baseName = string.IsNullOrWhiteSpace(s.name) ? "Segment" : s.name;
        return $"[S] {baseName}";
    }
}
