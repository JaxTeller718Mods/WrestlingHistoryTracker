using System;
using System.Collections.Generic;

[Serializable]
public class StableData
{
    public string id;           // stable identifier
    public string stableName;   // display name
    public List<string> memberIds = new(); // wrestler IDs
    public bool active = true;
}

[Serializable]
public class StableCollection
{
    public string promotionName;
    public List<StableData> stables = new();
}

