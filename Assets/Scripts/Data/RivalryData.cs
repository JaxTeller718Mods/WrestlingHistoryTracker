using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RivalryEvent
{
    public string id;                 // identifier for the event
    public string date;               // ISO date string (yyyy-MM-dd)
    public string eventType;          // Match, Segment, Promo, Attack, Other
    public string showId;             // optional link to a show
    public string matchId;            // optional link to a match
    public string segmentId;          // optional link to a segment
    public List<string> participants = new(); // typed IDs (e.g., "W:<id>", "T:<id>", "S:<id>")
    public string outcome;            // A wins, B wins, Draw, NA
    public float rating;              // optional rating/importance
    public string notes;              // freeform notes
}

[Serializable]
public class RivalryData
{
    public string id;                 // identifier
    public string title;              // display name
    public List<string> participants = new(); // typed IDs (e.g., W:<id>, T:<id>, S:<id>)
    public string type;               // Singles, Teams, Stables, Mixed
    public string status;             // Active, Dormant, Concluded
    public string startDate;          // ISO date
    public string endDate;            // ISO date (optional)
    public string notes;              // freeform notes

    public List<RivalryEvent> events = new();

    // Lightweight metrics (can be recomputed)
    public int winsA;
    public int winsB;
    public int draws;
    public string lastInteractionDate;
    public float feudScore;
}

[Serializable]
public class RivalryCollection
{
    public string promotionName;
    public List<RivalryData> rivalries = new();
}

