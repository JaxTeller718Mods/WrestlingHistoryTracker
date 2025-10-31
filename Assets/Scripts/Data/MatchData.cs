using System;
using UnityEngine;

/// <summary>
/// Represents one wrestling match and its outcome.
/// </summary>
[Serializable]
public class MatchData
{
    public string id; // stable identifier for entryOrder
    public string matchType; // e.g., Singles, Tag Team, etc.
    public string matchName;
    public string wrestlerA;
    public string wrestlerB;
    public string wrestlerC;
    public string wrestlerD;
    public bool isTitleMatch;
    public string titleName;
    public string winner;
    public string notes;
}
