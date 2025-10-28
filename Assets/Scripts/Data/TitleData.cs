using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TitleData
{
    public string titleName;
    public string division;
    public string establishedYear;
    public string currentChampion;
    public string notes;
}

[Serializable]
public class TitleCollection
{
    public string promotionName;
    public List<TitleData> titles = new();
}
