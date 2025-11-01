using System;
using System.Collections.Generic;

[Serializable]
public class TournamentData
{
    public string id;            // stable identifier
    public string name;          // tournament name
    public string type;          // "Singles" or "Tag Team"
    public List<TournamentEntry> entrants = new();
    public List<TournamentRound> rounds = new();
}

[Serializable]
public class TournamentEntry
{
    public string id;    // wrestlerId or tagTeamId depending on type
    public string name;  // snapshot for display
}

[Serializable]
public class TournamentRound
{
    public int roundNumber; // 1-based
    public List<TournamentMatch> matches = new();
}

[Serializable]
public class TournamentMatch
{
    public string id;         // match id inside tournament
    public string participant1Id;
    public string participant2Id;
    public string winnerId;   // one of the two ids
}

[Serializable]
public class TournamentCollection
{
    public string promotionName;
    public List<TournamentData> tournaments = new();
}

