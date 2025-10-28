using System;
using System.Collections.Generic;
using UnityEngine;

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
