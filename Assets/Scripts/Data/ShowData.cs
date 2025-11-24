using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ShowData
{
    public string id;         // stable identifier for cross-linking (rivalries, etc.)
    public string showName;
    public string date;
    public string venue;      // arena or building
    public string city;       // city, state/province
    public int attendance;    // number of attendees
    [Obsolete("Use tvRating or ppvBuys instead.")]
    public float rating;      // legacy combined metric
    public float tvRating;    // TV rating for broadcast shows
    public int ppvBuys;       // PPV buy count for premium shows
    public string showType;   // TV, PPV, House, etc.
    public string brand;      // optional brand label (e.g., Raw, SmackDown)
    public List<MatchData> matches = new();
    public List<SegmentData> segments = new();
    // Maintains presentation order for results: tokens like "M:<id>", "S:<id>".
    // Older saves may contain index-based tokens (e.g., "M:0").
    public List<string> entryOrder = new();

    public ShowData(string name, string date)
    {
        id = Guid.NewGuid().ToString("N");
        showName = name;
        this.date = date;
        tvRating = 0f;
        ppvBuys = 0;
    }
}
