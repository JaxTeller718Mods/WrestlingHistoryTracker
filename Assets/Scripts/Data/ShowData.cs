using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ShowData
{
    public string showName;
    public string date;
    public string venue;      // arena or building
    public string city;       // city, state/province
    public int attendance;    // number of attendees
    public float rating;      // buyrate/TV rating
    public List<MatchData> matches = new();
    public List<SegmentData> segments = new();
    // Maintains presentation order for results: tokens like "M:<id>", "S:<id>".
    // Older saves may contain index-based tokens (e.g., "M:0").
    public List<string> entryOrder = new();

    public ShowData(string name, string date)
    {
        showName = name;
        this.date = date;
    }
}
