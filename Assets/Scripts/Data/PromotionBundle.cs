using System;
using UnityEngine;

[Serializable]
public class PromotionBundle
{
    public string promotionName;
    public PromotionData promotion;
    public WrestlerCollection wrestlers;
    public TitleCollection titles;
    public TagTeamCollection tagTeams;
    public StableCollection stables;
    public TournamentCollection tournaments;
    public RivalryCollection rivalries;
    public MatchHistoryData history;
    public RankingStore rankings;
}
