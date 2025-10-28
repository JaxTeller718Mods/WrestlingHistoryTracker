using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized data manager for handling save/load operations.
/// Currently handles PromotionData, WrestlerCollection, TitleCollection, and ShowCollection.
/// All methods are static so this class may be used from anywhere.
/// </summary>
public static class DataManager
{
    // Root folder for all saved data (under user’s persistent data path)
    private static readonly string baseFolder = Application.persistentDataPath;
    private static readonly string promotionFolder = Path.Combine(baseFolder, "Promotions");
    private static readonly string wrestlerFolder = Path.Combine(baseFolder, "Wrestlers");
    private static readonly string titleFolder = Path.Combine(baseFolder, "Titles");
    private static readonly string showFolder = Path.Combine(baseFolder, "Shows");

    // ------------------------
    // PROMOTION MANAGEMENT
    // ------------------------
    public static void SavePromotion(PromotionData promotion)
    {
        if (promotion == null)
        {
            Debug.LogError("Cannot save: PromotionData is null.");
            return;
        }

        if (!Directory.Exists(promotionFolder))
            Directory.CreateDirectory(promotionFolder);

        string safeName = MakeSafeFileName(promotion.promotionName);
        string filePath = Path.Combine(promotionFolder, $"{safeName}.json");

        try
        {
            string json = JsonUtility.ToJson(promotion, true);
            File.WriteAllText(filePath, json);
            Debug.Log($"✅ Promotion saved to: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Error saving promotion: {ex.Message}");
        }
    }

    public static PromotionData LoadPromotion(string promotionName)
    {
        if (string.IsNullOrEmpty(promotionName))
        {
            Debug.LogError("LoadPromotion failed: promotionName is null or empty.");
            return null;
        }

        string safeName = MakeSafeFileName(promotionName);
        string filePath = Path.Combine(promotionFolder, $"{safeName}.json");

        if (!File.Exists(filePath))
        {
            Debug.LogError($"❌ Promotion file not found: {filePath}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            PromotionData data = JsonUtility.FromJson<PromotionData>(json);
            Debug.Log($"✅ Promotion loaded: {data.promotionName}");
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Error loading promotion: {ex.Message}");
            return null;
        }
    }

    public static string[] ListSavedPromotions()
    {
        if (!Directory.Exists(promotionFolder))
            return Array.Empty<string>();

        string[] files = Directory.GetFiles(promotionFolder, "*.json");
        for (int i = 0; i < files.Length; i++)
            files[i] = Path.GetFileNameWithoutExtension(files[i]);

        return files;
    }

    public static bool DeletePromotion(string promotionName)
    {
        string safeName = MakeSafeFileName(promotionName);
        string filePath = Path.Combine(promotionFolder, $"{safeName}.json");

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"⚠️ Cannot delete promotion — file not found: {filePath}");
            return false;
        }

        try
        {
            File.Delete(filePath);
            Debug.Log($"🗑️ Promotion deleted: {promotionName}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Error deleting promotion: {ex.Message}");
            return false;
        }
    }

    // ------------------------
    // WRESTLER MANAGEMENT
    // ------------------------
    public static void SaveWrestlers(WrestlerCollection collection)
    {
        if (collection == null || string.IsNullOrEmpty(collection.promotionName))
        {
            Debug.LogError("Cannot save wrestlers: collection or promotion name is null.");
            return;
        }

        if (!Directory.Exists(wrestlerFolder))
            Directory.CreateDirectory(wrestlerFolder);

        string safeName = MakeSafeFileName(collection.promotionName);
        string filePath = Path.Combine(wrestlerFolder, $"{safeName}_Wrestlers.json");

        try
        {
            string json = JsonUtility.ToJson(collection, true);
            File.WriteAllText(filePath, json);
            Debug.Log($"💾 Wrestlers saved for {collection.promotionName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Error saving wrestlers: {ex.Message}");
        }
    }

    public static WrestlerCollection LoadWrestlers(string promotionName)
    {
        if (string.IsNullOrEmpty(promotionName))
            return new WrestlerCollection { promotionName = promotionName };

        string safeName = MakeSafeFileName(promotionName);
        string filePath = Path.Combine(wrestlerFolder, $"{safeName}_Wrestlers.json");

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"⚠️ No wrestlers found for {promotionName}");
            return new WrestlerCollection { promotionName = promotionName };
        }

        try
        {
            string json = File.ReadAllText(filePath);
            WrestlerCollection data = JsonUtility.FromJson<WrestlerCollection>(json);
            Debug.Log($"✅ Wrestlers loaded for {promotionName}");
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Error loading wrestlers: {ex.Message}");
            return new WrestlerCollection { promotionName = promotionName };
        }
    }

    // ------------------------
    // TITLE MANAGEMENT
    // ------------------------
    public static void SaveTitles(TitleCollection collection)
    {
        if (collection == null || string.IsNullOrEmpty(collection.promotionName))
        {
            Debug.LogError("Cannot save titles: collection or promotion name is null.");
            return;
        }

        if (!Directory.Exists(titleFolder))
            Directory.CreateDirectory(titleFolder);

        string safeName = MakeSafeFileName(collection.promotionName);
        string filePath = Path.Combine(titleFolder, $"{safeName}_Titles.json");

        try
        {
            string json = JsonUtility.ToJson(collection, true);
            File.WriteAllText(filePath, json);
            Debug.Log($"💾 Titles saved for {collection.promotionName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Error saving titles: {ex.Message}");
        }
    }

    public static TitleCollection LoadTitles(string promotionName)
    {
        if (string.IsNullOrEmpty(promotionName))
            return new TitleCollection { promotionName = promotionName };

        string safeName = MakeSafeFileName(promotionName);
        string filePath = Path.Combine(titleFolder, $"{safeName}_Titles.json");

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"⚠️ No titles found for {promotionName}");
            return new TitleCollection { promotionName = promotionName };
        }

        try
        {
            string json = File.ReadAllText(filePath);
            TitleCollection data = JsonUtility.FromJson<TitleCollection>(json);
            Debug.Log($"✅ Titles loaded for {promotionName}");
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Error loading titles: {ex.Message}");
            return new TitleCollection { promotionName = promotionName };
        }
    }

    // ------------------------
    // SHOWS / EVENTS MANAGEMENT
    // ------------------------
    public static void SaveShows(ShowCollection collection)
    {
        if (collection == null || string.IsNullOrEmpty(collection.promotionName))
        {
            Debug.LogError("Cannot save shows: collection or promotion name is null.");
            return;
        }

        if (!Directory.Exists(showFolder))
            Directory.CreateDirectory(showFolder);

        string safeName = MakeSafeFileName(collection.promotionName);
        string filePath = Path.Combine(showFolder, $"{safeName}_Shows.json");

        try
        {
            string json = JsonUtility.ToJson(collection, true);
            File.WriteAllText(filePath, json);
            Debug.Log($"💾 Shows saved for {collection.promotionName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Error saving shows: {ex.Message}");
        }
    }

    public static ShowCollection LoadShows(string promotionName)
    {
        if (string.IsNullOrEmpty(promotionName))
            return new ShowCollection { promotionName = promotionName };

        string safeName = MakeSafeFileName(promotionName);
        string filePath = Path.Combine(showFolder, $"{safeName}_Shows.json");

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"⚠️ No shows found for {promotionName}");
            return new ShowCollection { promotionName = promotionName };
        }

        try
        {
            string json = File.ReadAllText(filePath);
            ShowCollection data = JsonUtility.FromJson<ShowCollection>(json);
            Debug.Log($"✅ Shows loaded for {promotionName}");
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Error loading shows: {ex.Message}");
            return new ShowCollection { promotionName = promotionName };
        }
    }

    // ------------------------
    // Utilities
    // ------------------------
    private static string MakeSafeFileName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "unnamed";

        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
