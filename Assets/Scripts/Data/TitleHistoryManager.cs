using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// Handles recording match outcomes and maintaining title lineages for each promotion.
public static class TitleHistoryManager
{
    // Dictionary<promotionName, List<TitleHistoryEntry>>
    private static readonly Dictionary<string, MatchHistoryData> histories = new();

    public static void EnsureHistoryLoaded(PromotionData promotion)
    {
        if (promotion == null || string.IsNullOrEmpty(promotion.promotionName))
            return;

        if (histories.ContainsKey(promotion.promotionName))
            return;

        var loaded = DataManager.LoadMatchHistory(promotion.promotionName);
        if (loaded == null)
        {
            loaded = BuildHistoryFromPromotion(promotion);
            if (loaded.matchResults.Count > 0)
                DataManager.SaveMatchHistory(loaded);
        }

        histories[promotion.promotionName] = loaded;
    }

    private static MatchHistoryData GetOrLoadHistory(string promotionName)
    {
        if (string.IsNullOrEmpty(promotionName))
            return null;

        if (!histories.TryGetValue(promotionName, out var history) || history == null)
        {
            history = DataManager.LoadMatchHistory(promotionName) ?? new MatchHistoryData { promotionName = promotionName };
            histories[promotionName] = history;
        }

        history.matchResults ??= new List<MatchResultData>();
        history.titleLineages ??= new List<TitleLineageData>();
        return history;
    }

    public static void UpdateShowResults(PromotionData promotion, ShowData show, string previousShowName = null, string previousShowDate = null)
    {
        if (promotion == null || show == null)
            return;

        EnsureHistoryLoaded(promotion);

        if (!histories.TryGetValue(promotion.promotionName, out var history))
            return;

        RemoveResultsForShow(history, show, previousShowName, previousShowDate);
        AppendShowMatches(history, show);
        RebuildTitleLineages(history);

        DataManager.SaveMatchHistory(history);
        Debug.Log($"🏆 Histories updated for show '{show.showName}'.");
    }

    public static void RemoveShow(PromotionData promotion, ShowData show)
    {
        if (promotion == null || show == null)
            return;

        EnsureHistoryLoaded(promotion);

        if (!histories.TryGetValue(promotion.promotionName, out var history))
            return;

        RemoveResultsForShow(history, show, show.showName, show.date);
        RebuildTitleLineages(history);
        DataManager.SaveMatchHistory(history);
        Debug.Log($"🗑️ Removed history for show '{show.showName}'.");
    }

    public static List<MatchResultData> GetAllMatchResults(string promotionName)
    {
        var history = GetOrLoadHistory(promotionName);
        if (history == null)
            return new();

        return history.matchResults
            .OrderBy(r => ParseDate(r.date))
            .ThenBy(r => r.showName)
            .ThenBy(r => r.matchName)
            .Select(CloneMatch)
            .ToList();
    }

    public static List<TitleLineageData> GetTitleLineages(string promotionName)
    {
        var history = GetOrLoadHistory(promotionName);
        if (history == null)
            return new();

        return history.titleLineages
            .Select(CloneLineage)
            .ToList();
    }

    public static List<TitleHistoryEntry> GetHistory(string promotionName, string title)
    {
        var history = GetOrLoadHistory(promotionName);
        if (history == null)
            return new();

        return history.matchResults
            .Where(r => r.isTitleMatch && !string.IsNullOrEmpty(r.titleInvolved) && string.Equals(r.titleInvolved, title, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => ParseDate(r.date))
            .ThenBy(r => r.showName)
            .ThenBy(r => r.matchName)
            .Select(r => new TitleHistoryEntry
            {
                titleName = r.titleInvolved,
                winner = r.winner,
                date = r.date,
                matchName = string.IsNullOrEmpty(r.showName) ? r.matchName : $"{r.showName} - {r.matchName}"
            })
            .ToList();
    }

    // Summaries per title reign with duration and defense stats
    public static List<TitleReignSummary> GetTitleReignSummaries(string promotionName, string titleName)
    {
        var history = GetOrLoadHistory(promotionName);
        var result = new List<TitleReignSummary>();
        if (history == null) return result;

        var lineage = history.titleLineages?.FirstOrDefault(t => string.Equals(t.titleName, titleName, StringComparison.OrdinalIgnoreCase));
        if (lineage == null || lineage.reigns == null) return result;

        // Determine the last known event date in the history as the cap for active reigns
        var lastDate = history.matchResults
            .Where(r => !string.IsNullOrEmpty(r?.date))
            .Select(r => ParseDate(r.date))
            .DefaultIfEmpty(DateTime.MinValue)
            .Max();
        if (lastDate == DateTime.MinValue) lastDate = DateTime.Today;

        foreach (var reign in lineage.reigns)
        {
            var start = ParseDate(reign.dateWon);
            var end = string.IsNullOrEmpty(reign.dateLost) ? lastDate : ParseDate(reign.dateLost);
            if (start == DateTime.MinValue) continue;

            // Successful defenses for this reign: title matches for same title where winner == champion and date in [start, end]
            var defenses = history.matchResults
                .Where(r => r.isTitleMatch &&
                            !string.IsNullOrEmpty(r.titleInvolved) &&
                            string.Equals(r.titleInvolved, lineage.titleName, StringComparison.OrdinalIgnoreCase) &&
                            !string.IsNullOrEmpty(r.winner) &&
                            string.Equals(r.winner, reign.championName, StringComparison.OrdinalIgnoreCase))
                .Select(r => new { r.date })
                .Select(x => ParseDate(x.date))
                .Where(d => d != DateTime.MinValue && d >= start && d <= end)
                .OrderBy(d => d)
                .ToList();

            var summary = new TitleReignSummary
            {
                titleName = lineage.titleName,
                championName = reign.championName,
                dateWon = reign.dateWon,
                dateLost = reign.dateLost,
                daysHeld = (int)Math.Max(1, (end - start).TotalDays + 1),
                defenses = defenses.Count,
                firstDefenseDate = defenses.Count > 0 ? defenses.First().ToString("MM/dd/yyyy") : string.Empty,
                lastDefenseDate = defenses.Count > 0 ? defenses.Last().ToString("MM/dd/yyyy") : string.Empty
            };
            result.Add(summary);
        }

        return result;
    }

    // Convenience: summaries for all titles in the promotion
    public static List<TitleReignSummary> GetAllTitleReignSummaries(string promotionName)
    {
        var history = GetOrLoadHistory(promotionName);
        if (history == null) return new List<TitleReignSummary>();
        var list = new List<TitleReignSummary>();
        foreach (var lineage in history.titleLineages ?? Enumerable.Empty<TitleLineageData>())
            list.AddRange(GetTitleReignSummaries(promotionName, lineage.titleName));
        return list;
    }

    private static MatchHistoryData BuildHistoryFromPromotion(PromotionData promotion)
    {
        var history = new MatchHistoryData { promotionName = promotion.promotionName };

        if (promotion.shows != null)
        {
            foreach (var show in promotion.shows)
                AppendShowMatches(history, show);
        }

        RebuildTitleLineages(history);
        return history;
    }

    private static void AppendShowMatches(MatchHistoryData history, ShowData show)
    {
        if (history == null || show == null || show.matches == null)
            return;

        foreach (var match in show.matches)
        {
            if (match == null)
                continue;

            var result = new MatchResultData
            {
                showName = show.showName,
                date = show.date,
                matchName = match.matchName,
                wrestlerA = match.wrestlerA,
                wrestlerB = match.wrestlerB,
                winner = match.winner,
                isTitleMatch = match.isTitleMatch,
                titleInvolved = match.titleName
            };

            history.matchResults.Add(result);
        }
    }

    private static void RemoveResultsForShow(MatchHistoryData history, ShowData show, string previousShowName, string previousShowDate)
    {
        if (history == null || show == null)
            return;

        bool IsSameShow(MatchResultData result, string name, string date)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            if (!string.Equals(result.showName, name, StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.IsNullOrEmpty(date) || string.IsNullOrEmpty(result.date))
                return true;

            return string.Equals(result.date, date, StringComparison.OrdinalIgnoreCase);
        }

        history.matchResults.RemoveAll(r =>
            IsSameShow(r, show.showName, show.date) ||
            (!string.IsNullOrEmpty(previousShowName) && IsSameShow(r, previousShowName, previousShowDate)));
    }

    private static void RebuildTitleLineages(MatchHistoryData history)
    {
        if (history == null)
            return;

        history.titleLineages.Clear();

        var orderedMatches = history.matchResults
            .Where(r => r.isTitleMatch && !string.IsNullOrEmpty(r.titleInvolved) && !string.IsNullOrEmpty(r.winner))
            .OrderBy(r => ParseDate(r.date))
            .ThenBy(r => r.showName)
            .ThenBy(r => r.matchName);

        foreach (var match in orderedMatches)
        {
            var lineage = history.titleLineages.FirstOrDefault(t => string.Equals(t.titleName, match.titleInvolved, StringComparison.OrdinalIgnoreCase));
            if (lineage == null)
            {
                lineage = new TitleLineageData { titleName = match.titleInvolved, reigns = new List<TitleReign>() };
                history.titleLineages.Add(lineage);
            }

            lineage.reigns ??= new List<TitleReign>();

            if (lineage.reigns.Count == 0)
            {
                lineage.reigns.Add(new TitleReign
                {
                    championName = match.winner,
                    dateWon = match.date,
                    eventName = match.showName
                });
                continue;
            }

            var currentReign = lineage.reigns[^1];
            if (string.Equals(currentReign.championName, match.winner, StringComparison.OrdinalIgnoreCase))
                continue; // successful defence, no change in champion

            if (string.IsNullOrEmpty(currentReign.dateLost))
                currentReign.dateLost = match.date;

            lineage.reigns.Add(new TitleReign
            {
                championName = match.winner,
                dateWon = match.date,
                eventName = match.showName
            });
        }
    }

    private static MatchResultData CloneMatch(MatchResultData source)
    {
        return new MatchResultData
        {
            showName = source.showName,
            date = source.date,
            matchName = source.matchName,
            wrestlerA = source.wrestlerA,
            wrestlerB = source.wrestlerB,
            winner = source.winner,
            isTitleMatch = source.isTitleMatch,
            titleInvolved = source.titleInvolved
        };
    }

    private static TitleLineageData CloneLineage(TitleLineageData source)
    {
        var clone = new TitleLineageData
        {
            titleName = source.titleName,
            reigns = new List<TitleReign>()
        };

        if (source.reigns != null)
        {
            foreach (var reign in source.reigns)
            {
                clone.reigns.Add(new TitleReign
                {
                    championName = reign.championName,
                    dateWon = reign.dateWon,
                    dateLost = reign.dateLost,
                    eventName = reign.eventName
                });
            }
        }

        return clone;
    }

    private static DateTime ParseDate(string raw)
    {
        if (DateTime.TryParse(raw, out var parsed))
            return parsed;
        return DateTime.MinValue;
    }
}

[Serializable]
public class TitleHistoryEntry
{
    public string titleName;
    public string winner;
    public string date;
    public string matchName;
}
