using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ShowData
{
    public string showName;
    public string date;
    public List<MatchData> matches = new();
    public List<SegmentData> segments = new();
    // Maintains presentation order for results: tokens like "M:0", "S:1"
    public List<string> entryOrder = new();

    public ShowData(string name, string date)
    {
        showName = name;
        this.date = date;
    }
}
