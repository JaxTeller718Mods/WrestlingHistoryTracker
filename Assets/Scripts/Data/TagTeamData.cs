using System;
using System.Collections.Generic;

[Serializable]
public class TagTeamData
{
    public string id; // stable identifier
    public string teamName;
    public string memberA; // wrestler name
    public string memberB; // wrestler name
    public bool active = true;
    public string division; // optional tag division label
}

[Serializable]
public class TagTeamCollection
{
    public string promotionName;
    public List<TagTeamData> teams = new();
}
