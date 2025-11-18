using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Computes notable milestones (match counts, first titles, streaks, attendance records)
/// for a promotion from its consolidated history.
/// </summary>
public static class MilestoneEngine
{
    public static List<Milestone> ComputeYearlyMilestones(PromotionData promotion, int year)
    {
        var list = new List<Milestone>();
        if (promotion == null || string.IsNullOrEmpty(promotion.promotionName))
            return list;

        try
        {
            var promoName = promotion.promotionName;
            var matches = TitleHistoryManager.GetAllMatchResults(promoName) ?? new List<MatchResultData>();
            var allReigns = TitleHistoryManager.GetAllTitleReignSummaries(promoName) ?? new List<TitleReignSummary>();

            list.AddRange(ComputeMatchCountMilestones(matches, year));
            list.AddRange(ComputeFirstTitleWinMilestones(allReigns, year));
            list.AddRange(ComputeWinStreakMilestones(matches, year));
            list.AddRange(ComputeAttendanceMilestones(promotion, year));
        }
        catch (Exception ex)
        {
            Debug.LogError($"MilestoneEngine: failed to compute milestones: {ex.Message}");
        }

        // Sort for display: date, type, then description
        list.Sort((a, b) =>
        {
            DateTime da = ParseAny(a.date);
            DateTime db = ParseAny(b.date);
            int c = da.CompareTo(db);
            if (c != 0) return c;
            c = a.type.CompareTo(b.type);
            if (c != 0) return c;
            return string.Compare(a.description, b.description, StringComparison.OrdinalIgnoreCase);
        });

        return list;
    }

    private static IEnumerable<Milestone> ComputeMatchCountMilestones(List<MatchResultData> matches, int year)
    {
        var result = new List<Milestone>();
        if (matches == null || matches.Count == 0) return result;

        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var milestones = new[] { 50, 100, 150, 200 };

        foreach (var r in matches.OrderBy(m => ParseAny(m.date)))
        {
            if (!CalendarUtils.TryParseAny(r.date, out var d)) continue;

            void add(string name)
            {
                if (string.IsNullOrWhiteSpace(name)) return;
                var key = name.Trim();
                if (key.Length == 0) return;
                counts.TryGetValue(key, out var c);
                c++;
                counts[key] = c;
                if (milestones.Contains(c) && d.Year == year)
                {
                    result.Add(new Milestone
                    {
                        type = MilestoneType.WrestlerMatchCount,
                        date = d.ToString("yyyy-MM-dd"),
                        wrestlerName = key,
                        value = c,
                        description = $"{key} reached {c} recorded matches."
                    });
                }
            }

            add(r.wrestlerA);
            add(r.wrestlerB);
        }

        return result;
    }

    private static IEnumerable<Milestone> ComputeFirstTitleWinMilestones(List<TitleReignSummary> allReigns, int year)
    {
        var result = new List<Milestone>();
        if (allReigns == null || allReigns.Count == 0) return result;

        var firstByChampion = new Dictionary<string, TitleReignSummary>(StringComparer.OrdinalIgnoreCase);

        foreach (var s in allReigns)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.championName)) continue;
            var name = s.championName.Trim();
            var d = ParseAny(s.dateWon);
            if (d == DateTime.MinValue) continue;

            if (!firstByChampion.TryGetValue(name, out var existing))
            {
                firstByChampion[name] = s;
            }
            else
            {
                var existingDate = ParseAny(existing.dateWon);
                if (d < existingDate)
                    firstByChampion[name] = s;
            }
        }

        foreach (var kv in firstByChampion)
        {
            var champ = kv.Key;
            var summary = kv.Value;
            var d = ParseAny(summary.dateWon);
            if (d == DateTime.MinValue || d.Year != year) continue;

            result.Add(new Milestone
            {
                type = MilestoneType.WrestlerFirstTitleWin,
                date = d.ToString("yyyy-MM-dd"),
                wrestlerName = champ,
                titleName = summary.titleName,
                description = $"{champ} won their first title: {summary.titleName}."
            });
        }

        return result;
    }

    private static IEnumerable<Milestone> ComputeWinStreakMilestones(List<MatchResultData> matches, int year)
    {
        var result = new List<Milestone>();
        if (matches == null || matches.Count == 0) return result;

        var current = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var best = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in matches.OrderBy(m => ParseAny(m.date)))
        {
            if (!CalendarUtils.TryParseAny(r.date, out var d)) continue;

            var a = NormalizeName(r.wrestlerA);
            var b = NormalizeName(r.wrestlerB);
            var participants = new List<string>();
            if (a != null) participants.Add(a);
            if (b != null && !participants.Contains(b)) participants.Add(b);

            if (participants.Count == 0) continue;

            var winner = NormalizeName(r.winner);
            bool isDraw = string.IsNullOrEmpty(winner) ||
                          string.Equals(winner, "Draw", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(winner, "No Contest", StringComparison.OrdinalIgnoreCase);

            if (isDraw)
            {
                foreach (var p in participants)
                    current[p] = 0;
                continue;
            }

            foreach (var p in participants)
            {
                if (string.Equals(p, winner, StringComparison.OrdinalIgnoreCase))
                {
                    current.TryGetValue(p, out var c);
                    c++;
                    current[p] = c;

                    best.TryGetValue(p, out var bBest);
                    if (c > bBest)
                    {
                        best[p] = c;
                        if (d.Year == year && c >= 3)
                        {
                            result.Add(new Milestone
                            {
                                type = MilestoneType.WrestlerWinStreakRecord,
                                date = d.ToString("yyyy-MM-dd"),
                                wrestlerName = p,
                                value = c,
                                description = $"{p} set a new personal win streak record of {c}."
                            });
                        }
                    }
                }
                else
                {
                    current[p] = 0;
                }
            }
        }

        return result;
    }

    private static IEnumerable<Milestone> ComputeAttendanceMilestones(PromotionData promotion, int year)
    {
        var result = new List<Milestone>();
        if (promotion?.shows == null || promotion.shows.Count == 0) return result;

        int bestAttendance = 0;

        foreach (var s in promotion.shows
                     .Where(x => x != null)
                     .OrderBy(x => ParseAny(x.date))
                     .ThenBy(x => x.showName))
        {
            if (!CalendarUtils.TryParseAny(s.date, out var d)) continue;
            int attendance = s.attendance;
            if (attendance <= 0) continue;

            if (attendance > bestAttendance)
            {
                bestAttendance = attendance;
                if (d.Year == year)
                {
                    result.Add(new Milestone
                    {
                        type = MilestoneType.AttendanceRecord,
                        date = d.ToString("yyyy-MM-dd"),
                        showName = s.showName,
                        value = attendance,
                        description = $"New attendance record: {attendance:N0} at {s.showName}."
                    });
                }
            }
        }

        return result;
    }

    private static string NormalizeName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var s = raw.Trim();
        return s.Length == 0 ? null : s;
    }

    private static DateTime ParseAny(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return DateTime.MinValue;
        if (CalendarUtils.TryParseAny(raw, out var d)) return d;
        if (DateTime.TryParse(raw, out var d2)) return d2;
        return DateTime.MinValue;
    }
}

