using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles updating title lineages when matches are saved.
/// </summary>
public static class TitleHistoryManager
{
    // Dictionary<promotionName, List<TitleHistoryEntry>>
    private static Dictionary<string, List<TitleHistoryEntry>> histories = new();

    public static void RegisterMatchResult(PromotionData promotion, MatchData match)
    {
        if (!match.isTitleMatch || string.IsNullOrEmpty(match.titleName))
            return;

        if (!histories.ContainsKey(promotion.promotionName))
            histories[promotion.promotionName] = new();

        var history = histories[promotion.promotionName];
        var entry = new TitleHistoryEntry
        {
            titleName = match.titleName,
            winner = match.winner,
            date = System.DateTime.Now.ToShortDateString(),
            matchName = match.matchName
        };

        history.Add(entry);
        Debug.Log($"🏆 Title history updated: {match.titleName} -> {match.winner}");
    }

    public static List<TitleHistoryEntry> GetHistory(string promotionName, string title)
    {
        if (histories.TryGetValue(promotionName, out var list))
            return list.FindAll(h => h.titleName == title);
        return new();
    }
}

[System.Serializable]
public class TitleHistoryEntry
{
    public string titleName;
    public string winner;
    public string date;
    public string matchName;
}
