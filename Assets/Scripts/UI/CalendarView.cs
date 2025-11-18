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
    private Button prevMonthButton, nextMonthButton, todayButton, createShowButton;

    private Func<PromotionData> promotionProvider;
    private DateTime currentMonth;
    private DateTime selectedDate;

    public event Action<DateTime> CreateShowRequested;
    public event Action<ShowData> EditShowRequested;

    public void Initialize(VisualElement calendarPanel, Func<PromotionData> promotionGetter)
    {
        panel = calendarPanel;
        promotionProvider = promotionGetter;
        monthLabel = panel.Q<Label>("monthLabel");
        grid = panel.Q<VisualElement>("calendarGrid");
        selectedDateLabel = panel.Q<Label>("selectedDateLabel");
        dayShowsList = panel.Q<ScrollView>("dayShowsList");
        prevMonthButton = panel.Q<Button>("prevMonthButton");
        nextMonthButton = panel.Q<Button>("nextMonthButton");
        todayButton = panel.Q<Button>("todayButton");
        createShowButton = panel.Q<Button>("createShowButton");

        currentMonth = DateTime.Today;
        selectedDate = DateTime.Today;

        if (prevMonthButton != null) prevMonthButton.clicked += () => { currentMonth = currentMonth.AddMonths(-1); Refresh(); };
        if (nextMonthButton != null) nextMonthButton.clicked += () => { currentMonth = currentMonth.AddMonths(1); Refresh(); };
        if (todayButton != null) todayButton.clicked += () => { currentMonth = DateTime.Today; selectedDate = DateTime.Today; Refresh(); };
        if (createShowButton != null) createShowButton.clicked += () => CreateShowRequested?.Invoke(selectedDate);

        Refresh();
    }

    public void Refresh()
    {
        if (panel == null) return;
        var promotion = promotionProvider?.Invoke();
        var shows = promotion?.shows ?? new List<ShowData>();

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
                        if (sd.Date != thisDate.Date) continue;
                        var badge = new Button(() => EditShowRequested?.Invoke(s)) { text = s.showName };
                        badge.AddToClassList("calendar-badge");
                        badge.AddToClassList("card-entry-button");

                        // Color-code by explicit show type if available, otherwise inferred
                        var showType = !string.IsNullOrEmpty(s.showType) ? s.showType : InferShowType(s);
                        if (!string.IsNullOrEmpty(showType))
                            badge.AddToClassList($"calendar-badge--{showType}");

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
            var todays = shows.Where(s => CalendarUtils.TryParseAny(s.date, out var sd) && sd.Date == selectedDate.Date)
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
}
