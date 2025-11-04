using System;
using System.Collections.Generic;

[Serializable]
public enum RankingType
{
    Singles,
    TagTeam,
    Stable
}

[Serializable]
public class RankingFormula
{
    public int winPoints = 1;   // MVP: simple wins
    public int drawPoints = 0;
    public int lossPoints = 0;
}

[Serializable]
public class RankingConfig
{
    public string promotionName;
    public List<string> singlesDivisions = new() { "Overall" };
    public RankingFormula formula = new RankingFormula();
}

[Serializable]
public class RankingEntry
{
    public string entityId; // wrestlerId, teamId, or stableId (may be blank if name-only)
    public string name;
    public int wins;
    public int losses;
    public int draws;
    public float score; // MVP: equals wins
}

[Serializable]
public class RankingSnapshot
{
    public string promotionName;
    public string weekStartIso;  // yyyy-MM-dd
    public string weekEndIso;    // yyyy-MM-dd
    public RankingType type;
    public string division;      // for Singles (e.g., Overall)
    public string gender;        // Men, Women, All (Singles only)
    public int topN = 10;
    public List<RankingEntry> top = new();
}

[Serializable]
public class RankingStore
{
    public string promotionName;
    public RankingConfig config = new RankingConfig();
    public List<RankingSnapshot> snapshots = new();
}

