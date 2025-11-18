using System;
using UnityEngine;

[Serializable]
public enum MilestoneType
{
    WrestlerMatchCount,
    WrestlerFirstTitleWin,
    WrestlerWinStreakRecord,
    AttendanceRecord
}

/// <summary>
/// Represents a noteworthy milestone in the promotion's history.
/// Computed on the fly from match / title / show data; not persisted.
/// </summary>
[Serializable]
public class Milestone
{
    public MilestoneType type;
    public string date;           // ISO or MM/dd/yyyy string
    public string wrestlerName;   // optional
    public string titleName;      // optional
    public string showName;       // optional
    public int value;             // e.g., match count, streak length, attendance
    public string description;    // human-readable summary
}

