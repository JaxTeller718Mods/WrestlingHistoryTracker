using System;
using System.IO;
using System.Collections.Generic;   // �o. Needed for List<>
using UnityEngine;

/// <summary>
/// Represents a wrestling promotion and provides JSON save/load helpers.
/// </summary>
[Serializable]
public class PromotionData
{
    public string promotionName;
    public string location;
    public string foundedYear;
    public string description;

    // �o. Added: list of shows/events for this promotion
    public List<WrestlerData> wrestlers = new();
    public List<TitleData> titles = new();
    public List<ShowData> shows = new();
    // Optional: brands within this promotion (e.g., Raw, SmackDown)
    public List<string> brands = new();

    // dY"1 Future-proof placeholders (you can fill them later)
    // public List<WrestlerData> wrestlers = new();
    // public List<TitleData> titles = new();

    /// <summary>
    /// Save this promotion's data to a JSON file.
    /// </summary>
    public void SaveToFile(string path)
    {
        string json = JsonUtility.ToJson(this, true);
        File.WriteAllText(path, json);
        Debug.Log($"Promotion data saved: {path}");
    }

    /// <summary>
    /// Load a promotion's data from a JSON file.
    /// </summary>
    public static PromotionData LoadFromFile(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"File not found: {path}");
            return null;
        }
        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<PromotionData>(json);
    }
}

