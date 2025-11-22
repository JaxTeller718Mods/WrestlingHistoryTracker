using System;
using UnityEngine;

/// <summary>
/// Represents one wrestling match and its outcome.
/// </summary>
[Serializable]
public class MatchData
{
    public string id; // stable identifier for entryOrder
    public string matchType; // legacy combined type (kept for backward compatibility)
    public string matchStructure; // Singles, Tag Team, Triple Threat, etc.
    public string matchStipulation; // Hell in a Cell, Last Man Standing, etc.
    public string matchName;
    // Legacy name fields kept for backward compatibility/display
    public string wrestlerA;
    public string wrestlerB;
    public string wrestlerC;
    public string wrestlerD;
    public string wrestlerE;
    public string wrestlerF;
    public bool isTitleMatch;
    public string titleName;
    public string winner;
    public string winnerTeamId; // if winner is a tag team, prefer this id
    public string notes;

    // New: stable ID references (preferred for logic)
    public string wrestlerAId;
    public string wrestlerBId;
    public string wrestlerCId;
    public string wrestlerDId;
    public string wrestlerEId;
    public string wrestlerFId;
    public string winnerId;
    public string titleId;
}
