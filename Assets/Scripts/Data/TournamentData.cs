using System;
using System.Collections.Generic;

[Serializable]
public enum TournamentFormat
{
    SingleElimination,
    DoubleElimination,
    RoundRobin,
    Block
}

[Serializable]
public enum TournamentStatus
{
    Planned,
    Active,
    Completed
}

[Serializable]
public enum TournamentRoundBracket
{
    Winners,
    Losers,
    Finals
}

[Serializable]
public class TournamentData
{
    public string id;            // stable identifier
    public string name;          // tournament name
    public string type;          // "Singles", "Tag Team", "Trios"
    public string entrantType;   // Singles, Tag Team, Stable
    public string entrantDivision;
    public string entrantBrand;
    public TournamentFormat format = TournamentFormat.SingleElimination;
    public TournamentStatus status = TournamentStatus.Planned;
    public string brand;
    public int year;
    public bool seededBracket;
    public List<string> pendingLoserIds = new();
    public List<TournamentEntry> entrants = new();
    public List<TournamentRound> rounds = new(); // winners bracket (or primary round robin stage)
    public List<TournamentRound> loserRounds = new(); // secondary bracket for double elimination
    public TournamentMatch finalsMatch;
    public List<TournamentBlock> blocks = new(); // for round robin / block play
    public string championId;
    public string championName;
    public string stakes;
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
    public string label;
    public TournamentRoundBracket bracket = TournamentRoundBracket.Winners;
    public bool closed;
    public List<TournamentMatch> matches = new();
}

[Serializable]
public class TournamentMatch
{
    public string id;         // match id inside tournament
    public string participant1Id;
    public string participant2Id;
    public string winnerId;   // one of the two ids
    public bool isDraw;
    public string notes;
}

[Serializable]
public class TournamentBlock
{
    public string id;
    public string name;
    public List<TournamentEntry> entrants = new();
    public List<TournamentMatch> matches = new();
}

[Serializable]
public class TournamentCollection
{
    public string promotionName;
    public List<TournamentData> tournaments = new();
}
