using System;
using System.Collections.Generic;

public static class ShowTemplates
{
    public class Template
    {
        public string name;
        public int matches;
        public bool openerPromo;
        public bool midPromo;
    }

    public static readonly List<Template> Templates = new()
    {
        new Template { name = "Weekly TV", matches = 5, openerPromo = true, midPromo = true },
        new Template { name = "PPV", matches = 8, openerPromo = true, midPromo = true },
        new Template { name = "House Show", matches = 6, openerPromo = false, midPromo = false }
    };

    public enum RecurrenceKind
    {
        Weekly,
        Monthly
    }

    public class RecurringTemplate
    {
        public string name;
        public string showName;
        public string showType;    // e.g. "TV", "PPV", "House"
        public string brand;       // optional brand label
        public RecurrenceKind kind;
        public DayOfWeek weeklyDay;        // for Weekly
        public int monthlyWeekIndex;       // 1 = first, 2 = second, 3 = third, 4 = fourth, 5 = last
        public DayOfWeek monthlyDay;       // e.g. Sunday for PPV
    }

    // Some sensible defaults; users can still override generated shows.
    public static readonly List<RecurringTemplate> Recurring = new()
    {
        new RecurringTemplate
        {
            name = "Weekly TV (Monday)",
            showName = "Weekly TV",
            showType = "TV",
            brand = "",
            kind = RecurrenceKind.Weekly,
            weeklyDay = DayOfWeek.Monday
        },
        new RecurringTemplate
        {
            name = "PPV (First Sunday)",
            showName = "Monthly PPV",
            showType = "PPV",
            brand = "",
            kind = RecurrenceKind.Monthly,
            monthlyWeekIndex = 1,
            monthlyDay = DayOfWeek.Sunday
        }
    };
}
