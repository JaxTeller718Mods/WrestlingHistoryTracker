using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class CalendarView
{
    private VisualElement panel;
    private Label monthLabel;
    private VisualElement grid;
    private Label selectedDateLabel;
    private ScrollView dayShowsList;
    private Button prevMonthButton, nextMonthButton, todayButton, createShowButton, generateYearButton;
    private DropdownField recurrenceDropdown;
    private TextField yearField;

    private Func<PromotionData> promotionProvider;
    private Func<string> brandFilterProvider;
    private DateTime currentMonth;
    private DateTime selectedDate;

    public event Action<DateTime> CreateShowRequested;
    public event Action<ShowData> EditShowRequested;

    public void Initialize(VisualElement calendarPanel, Func<PromotionData> promotionGetter, Func<string> brandFilterGetter = null)
    {
        panel = calendarPanel;
        promotionProvider = promotionGetter;
        brandFilterProvider = brandFilterGetter;
        monthLabel = panel.Q<Label>("monthLabel");
        grid = panel.Q<VisualElement>("calendarGrid");
        selectedDateLabel = panel.Q<Label>("selectedDateLabel");
        dayShowsList = panel.Q<ScrollView>("dayShowsList");
        prevMonthButton = panel.Q<Button>("prevMonthButton");
        nextMonthButton = panel.Q<Button>("nextMonthButton");
        todayButton = panel.Q<Button>("todayButton");
        createShowButton = panel.Q<Button>("createShowButton");
        recurrenceDropdown = panel.Q<DropdownField>("calendarRecurrenceDropdown");
        yearField = panel.Q<TextField>("calendarYearField");
        generateYearButton = panel.Q<Button>("calendarGenerateYearButton");

        currentMonth = DateTime.Today;
        selectedDate = DateTime.Today;

        if (prevMonthButton != null) prevMonthButton.clicked += () => { currentMonth = currentMonth.AddMonths(-1); Refresh(); };
        if (nextMonthButton != null) nextMonthButton.clicked += () => { currentMonth = currentMonth.AddMonths(1); Refresh(); };
        if (todayButton != null) todayButton.clicked += () => { currentMonth = DateTime.Today; selectedDate = DateTime.Today; Refresh(); };
        if (createShowButton != null) createShowButton.clicked += () => CreateShowRequested?.Invoke(selectedDate);

        if (recurrenceDropdown != null)
        {
            var choices = new List<string>();
            foreach (var r in ShowTemplates.Recurring)
            {
                if (r != null && !string.IsNullOrWhiteSpace(r.name))
                    choices.Add(r.name);
            }
            recurrenceDropdown.choices = choices;
            if (choices.Count > 0) recurrenceDropdown.value = choices[0];
        }

        if (yearField != null)
        {
            yearField.value = DateTime.Today.Year.ToString();
        }

        if (generateYearButton != null)
        {
            generateYearButton.clicked += OnGenerateYearClicked;
        }

        Refresh();
    }

    public void Refresh()
    {
        if (panel == null) return;
        var promotion = promotionProvider?.Invoke();
        var shows = promotion?.shows ?? new List<ShowData>();
        string brandFilter = brandFilterProvider != null ? (brandFilterProvider() ?? string.Empty).Trim() : string.Empty;
        bool MatchBrand(ShowData s)
        {
            if (s == null) return false;
            if (string.IsNullOrEmpty(brandFilter) || string.Equals(brandFilter, "All Brands", StringComparison.OrdinalIgnoreCase))
                return true;
            return !string.IsNullOrEmpty(s.brand) && string.Equals(s.brand, brandFilter, StringComparison.OrdinalIgnoreCase);
        }

        if (monthLabel != null)
        {
            monthLabel.text = currentMonth.ToString("MMMM yyyy");
        }

        if (grid != null)
        {
            // Ensure selected date is within current month
            if (selectedDate.Year != currentMonth.Year || selectedDate.Month != currentMonth.Month)
                selectedDate = new DateTime(currentMonth.Year, currentMonth.Month, 1);

            grid.Clear();

            // Add day-of-week header row
            var dowRow = new VisualElement();
            dowRow.AddToClassList("calendar-dow-row");
            string[] dows = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            foreach (var d in dows)
            {
                var h = new Label(d);
                h.AddToClassList("calendar-dow-cell");
                dowRow.Add(h);
            }
            grid.Add(dowRow);

            int daysInMonth = DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);
            int day = 1;
            while (day <= daysInMonth)
            {
                var row = new VisualElement();
                row.AddToClassList("calendar-row");

                // Padding cells to align first day of month to correct weekday
                int startCol = 0;
                if (day == 1)
                {
                    var first = new DateTime(currentMonth.Year, currentMonth.Month, 1);
                    startCol = (int)first.DayOfWeek; // Sunday=0
                    for (int pad = 0; pad < startCol; pad++)
                    {
                        var padCell = new VisualElement();
                        padCell.AddToClassList("calendar-cell");
                        padCell.AddToClassList("calendar-cell--pad");
                        row.Add(padCell);
                    }
                }

                for (int col = startCol; col < 7 && day <= daysInMonth; col++)
                {
                    var thisDate = new DateTime(currentMonth.Year, currentMonth.Month, day);
                    var cell = new VisualElement();
                    cell.AddToClassList("calendar-cell");
                    if (thisDate.Date == DateTime.Today.Date) cell.AddToClassList("calendar-cell--today");

                    var dateLabel = new Label(day.ToString());
                    dateLabel.AddToClassList("calendar-cell__date");
                    cell.Add(dateLabel);

                    foreach (var s in shows)
                    {
                        if (!CalendarUtils.TryParseAny(s.date, out var sd)) continue;
                        if (!MatchBrand(s)) continue;
                        if (sd.Date != thisDate.Date) continue;
                        var badge = new Button(() => EditShowRequested?.Invoke(s)) { text = s.showName };
                        badge.AddToClassList("calendar-badge");
                        badge.AddToClassList("card-entry-button");

                        // Color-code by explicit show type if available, otherwise inferred
                        var showType = !string.IsNullOrEmpty(s.showType) ? s.showType : InferShowType(s);
                        if (!string.IsNullOrEmpty(showType))
                            badge.AddToClassList($"calendar-badge--{showType}");

                        // Brand-driven background/text colors
                        var brandPrimary = BrandColors.GetPrimary(s.brand);
                        var brandText = BrandColors.GetText(s.brand);
                        badge.style.backgroundColor = new StyleColor(brandPrimary);
                        badge.style.color = new StyleColor(brandText);

                        // Highlight shows that include any title match
                        if (ShowHasTitleMatch(s))
                        {
                            badge.AddToClassList("calendar-badge--title");
                            if (!string.IsNullOrEmpty(badge.text))
                                badge.text = $"★ {badge.text}";
                        }

                        cell.Add(badge);
                    }

                    cell.RegisterCallback<ClickEvent>(_ => { selectedDate = thisDate; RefreshDayDetails(); });
                    row.Add(cell);
                    day++;
                }
                grid.Add(row);
            }
        }

        RefreshDayDetails();
    }

    private string InferShowType(ShowData show)
    {
        if (show == null) return "tv";
        var name = (show.showName ?? string.Empty).ToLowerInvariant();

        if (name.Contains("ppv") || name.Contains("pay-per-view") || name.Contains("pay per view"))
            return "ppv";
        if (name.Contains("house") || name.Contains("live event") || name.Contains("live show"))
            return "house";

        return "tv";
    }

    private bool ShowHasTitleMatch(ShowData show)
    {
        if (show?.matches == null) return false;
        foreach (var m in show.matches)
        {
            if (m != null && m.isTitleMatch)
                return true;
        }
        return false;
    }

    private void RefreshDayDetails()
    {
        if (selectedDateLabel != null)
        {
            selectedDateLabel.text = selectedDate.ToString("D");
        }
        if (dayShowsList != null)
        {
            dayShowsList.Clear();
            var promotion = promotionProvider?.Invoke();
            var shows = promotion?.shows ?? new List<ShowData>();
            string brandFilter = brandFilterProvider != null ? (brandFilterProvider() ?? string.Empty).Trim() : string.Empty;
            bool MatchBrand(ShowData s)
            {
                if (s == null) return false;
                if (string.IsNullOrEmpty(brandFilter) || string.Equals(brandFilter, "All Brands", StringComparison.OrdinalIgnoreCase))
                    return true;
                return !string.IsNullOrEmpty(s.brand) && string.Equals(s.brand, brandFilter, StringComparison.OrdinalIgnoreCase);
            }
            var todays = shows.Where(s => MatchBrand(s) && CalendarUtils.TryParseAny(s.date, out var sd) && sd.Date == selectedDate.Date)
                              .OrderBy(s => s.showName)
                              .ToList();
            if (todays.Count == 0)
            {
                dayShowsList.Add(new Label("No shows on this date."));
            }
            else
            {
                foreach (var s in todays)
                {
                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.marginBottom = 4;
                    var lbl = new Label(s.showName);
                    lbl.style.flexGrow = 1;
                    var edit = new Button(() => EditShowRequested?.Invoke(s)) { text = "Edit" };
                    row.Add(lbl);
                    row.Add(edit);
                    dayShowsList.Add(row);
                }
            }
        }
    }

    private void OnGenerateYearClicked()
    {
        var promotion = promotionProvider?.Invoke();
        if (promotion == null)
        {
            Debug.LogWarning("Calendar: no promotion loaded for year generation.");
            return;
        }

        if (recurrenceDropdown == null || recurrenceDropdown.choices == null || recurrenceDropdown.choices.Count == 0)
        {
            Debug.LogWarning("Calendar: no recurrence template selected.");
            return;
        }

        if (yearField == null || string.IsNullOrWhiteSpace(yearField.value) || !int.TryParse(yearField.value.Trim(), out var year) || year < 1900 || year > 3000)
        {
            year = DateTime.Today.Year;
            if (yearField != null) yearField.value = year.ToString();
        }

        var templateName = recurrenceDropdown.value;
        var recurring = ShowTemplates.Recurring.FirstOrDefault(r => r != null && string.Equals(r.name, templateName, StringComparison.OrdinalIgnoreCase));
        if (recurring == null)
        {
            Debug.LogWarning($"Calendar: recurrence template '{templateName}' not found.");
            return;
        }

        var dates = GenerateDatesForYear(recurring, year);
        if (dates.Count == 0)
            return;

        promotion.shows ??= new List<ShowData>();

        foreach (var date in dates)
        {
            string iso = CalendarUtils.FormatIso(date);
            bool exists = promotion.shows.Any(s => s != null && CalendarUtils.TryParseAny(s.date, out var d) && d.Date == date.Date && string.Equals(s.showName, recurring.showName, StringComparison.OrdinalIgnoreCase));
            if (exists) continue;

            var show = new ShowData(recurring.showName, iso)
            {
                showType = recurring.showType,
                brand = recurring.brand ?? string.Empty,
                venue = string.Empty,
                city = string.Empty,
                attendance = 0,
                rating = 0f
            };
            promotion.shows.Add(show);
        }

        DataManager.SavePromotion(promotion);
        Refresh();
    }

    private List<DateTime> GenerateDatesForYear(ShowTemplates.RecurringTemplate recurring, int year)
    {
        var list = new List<DateTime>();
        if (recurring == null) return list;

        if (recurring.kind == ShowTemplates.RecurrenceKind.Weekly)
        {
            // From Jan 1 to Dec 31, pick all given weekday
            var start = new DateTime(year, 1, 1);
            var end = new DateTime(year, 12, 31);
            int offset = ((int)recurring.weeklyDay - (int)start.DayOfWeek + 7) % 7;
            var first = start.AddDays(offset);
            for (var d = first; d <= end; d = d.AddDays(7))
                list.Add(d);
        }
        else if (recurring.kind == ShowTemplates.RecurrenceKind.Monthly)
        {
            for (int month = 1; month <= 12; month++)
            {
                var date = GetMonthlyDate(year, month, recurring.monthlyWeekIndex, recurring.monthlyDay);
                if (date != DateTime.MinValue)
                    list.Add(date);
            }
        }

        return list;
    }

    private DateTime GetMonthlyDate(int year, int month, int weekIndex, DayOfWeek dayOfWeek)
    {
        if (weekIndex <= 0) return DateTime.MinValue;

        var first = new DateTime(year, month, 1);
        int offset = ((int)dayOfWeek - (int)first.DayOfWeek + 7) % 7;
        var firstDesired = first.AddDays(offset);

        if (weekIndex >= 1 && weekIndex <= 4)
        {
            var date = firstDesired.AddDays(7 * (weekIndex - 1));
            return date.Month == month ? date : DateTime.MinValue;
        }

        if (weekIndex == 5) // last occurrence in the month
        {
            var date = firstDesired;
            DateTime lastValid = DateTime.MinValue;
            while (date.Month == month)
            {
                lastValid = date;
                date = date.AddDays(7);
            }
            return lastValid;
        }

        return DateTime.MinValue;
    }
}
