using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MatchHistoryData
{
    public string promotionName;
    public List<MatchResultData> matchResults = new();
    public List<TitleLineageData> titleLineages = new();
}

[Serializable]
public class MatchResultData
{
    public string showName;
    public string date;
    public string matchName;
    public string wrestlerA;
    public string wrestlerB;
    public string winner;
    public bool isTitleMatch;
    public string titleInvolved;
}

[Serializable]
public class TitleLineageData
{
    public string titleName;
    public List<TitleReign> reigns = new();
}

[Serializable]
public class TitleReign
{
    public string championName;
    public string dateWon;
    public string dateLost;   // blank = current champ
    public string eventName;
}

// Computed summary information for a single reign
[Serializable]
public class TitleReignSummary
{
    public string titleName;
    public string championName;
    public string dateWon;
    public string dateLost;         // empty if still reigning
    public int daysHeld;            // inclusive of start date
    public int defenses;            // number of successful defenses during this reign
    public string firstDefenseDate; // empty if none
    public string lastDefenseDate;  // empty if none
}
