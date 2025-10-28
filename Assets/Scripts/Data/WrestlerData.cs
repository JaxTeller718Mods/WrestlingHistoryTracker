using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single wrestler.
/// </summary>
[Serializable]
public class WrestlerData
{
    public string name;
    public string nickname;
    public string hometown;
    public string debutYear;
    public float weight;   // in pounds (lbs)
    public float height;   // in centimeters

    // ✅ Parameterless constructor (needed for JSONUtility & object initializers)
    public WrestlerData() { }

    // ✅ Optional constructor for quick creation
    public WrestlerData(string name)
    {
        this.name = name;
    }
}

/// <summary>
/// Container for all wrestlers under a promotion.
/// Saved separately but referenced by promotion name.
/// </summary>
[Serializable]
public class WrestlerCollection
{
    public string promotionName;
    public List<WrestlerData> wrestlers = new();
}
