using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ShowData
{
    public string showName;
    public string date;
    public List<MatchData> matches = new();

    public ShowData(string name, string date)
    {
        showName = name;
        this.date = date;
    }
}
