using System;
using System.Collections.Generic;
using System.Globalization;

public static class CalendarUtils
{
    public const string IsoDateFormat = "yyyy-MM-dd";
    private static readonly string[] LegacyFormats = new[] { "MM/dd/yyyy", "M/d/yyyy", "M/dd/yyyy", "MM/d/yyyy" };

    public static string FormatIso(DateTime date)
    {
        return date.ToString(IsoDateFormat, CultureInfo.InvariantCulture);
    }

    public static bool TryParseAny(string input, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(input)) return false;

        // Try ISO first
        if (DateTime.TryParseExact(input.Trim(), IsoDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return true;

        // Try legacy formats
        foreach (var fmt in LegacyFormats)
        {
            if (DateTime.TryParseExact(input.Trim(), fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                return true;
        }

        // Fallback to culture-agnostic parse
        return DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    public static string NormalizeToIso(string input)
    {
        if (TryParseAny(input, out var dt))
            return FormatIso(dt);
        return input;
    }

    public static List<DateTime> BuildMonthGrid(DateTime anyDayInMonth)
    {
        // Returns 42 days (6 weeks) starting on Sunday of the first week showing the month
        var firstOfMonth = new DateTime(anyDayInMonth.Year, anyDayInMonth.Month, 1);
        int diffToSunday = ((int)firstOfMonth.DayOfWeek + 7 - (int)DayOfWeek.Sunday) % 7;
        var start = firstOfMonth.AddDays(-diffToSunday);
        var days = new List<DateTime>(42);
        for (int i = 0; i < 42; i++) days.Add(start.AddDays(i));
        return days;
    }
}

