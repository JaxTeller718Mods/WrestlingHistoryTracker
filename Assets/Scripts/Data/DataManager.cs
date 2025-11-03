using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Centralized data manager for handling save/load operations.
/// Handles PromotionData (includes shows), WrestlerCollection, and TitleCollection.
/// All methods are static so this class may be used from anywhere.
/// </summary>
public static class DataManager
{
    // Root folder for all saved data (under user’s persistent data path)
    private static readonly string baseFolder      = Application.persistentDataPath;
    private static readonly string promotionFolder = Path.Combine(baseFolder, "Promotions");
    private static readonly string wrestlerFolder  = Path.Combine(baseFolder, "Wrestlers");
    private static readonly string titleFolder = Path.Combine(baseFolder, "Titles");
    private static readonly string tagTeamFolder = Path.Combine(baseFolder, "TagTeams");
    private static readonly string stableFolder  = Path.Combine(baseFolder, "Stables");
    private static readonly string tournamentsFolder = Path.Combine(baseFolder, "Tournaments");
    private static readonly string historyFolder   = Path.Combine(baseFolder, "Histories");
    private static readonly string exportFolder    = Path.Combine(baseFolder, "Exports");

    // ------------------------
    // PROMOTION MANAGEMENT (includes shows)
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

    public static string GetExportFolderPath()
    {
        try
        {
            if (!Directory.Exists(exportFolder)) Directory.CreateDirectory(exportFolder);
            return exportFolder;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to access export folder: {ex.Message}");
            return null;
        }
    }

    // ------------------------
    // EXPORT / IMPORT BUNDLE
    // ------------------------
    public static string ExportPromotionBundle(string promotionName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(promotionName)) return null;
            var safe = MakeSafeFileName(promotionName);
            var promo = LoadPromotion(promotionName);
            var wrestlers = LoadWrestlers(promotionName);
            var titles = LoadTitles(promotionName);
            var tagTeams = LoadTagTeams(promotionName);
            var stables = LoadStables(promotionName);
            var tournaments = LoadTournaments(promotionName);

            var bundle = new PromotionBundle
            {
                promotionName = promotionName,
                promotion = promo,
                wrestlers = wrestlers,
                titles = titles,
                tagTeams = tagTeams,
                stables = stables,
                tournaments = tournaments
            };

            if (!Directory.Exists(exportFolder)) Directory.CreateDirectory(exportFolder);
            var outPath = Path.Combine(exportFolder, safe + "_bundle.json");
            var json = JsonUtility.ToJson(bundle, true);
            File.WriteAllText(outPath, json);
            return outPath;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Export failed: {ex.Message}");
            return null;
        }
    }

    public static bool ImportPromotionBundle(string bundlePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(bundlePath) || !File.Exists(bundlePath))
            {
                Debug.LogError($"Import failed: file not found {bundlePath}");
                return false;
            }
            var json = File.ReadAllText(bundlePath);
            var bundle = JsonUtility.FromJson<PromotionBundle>(json);
            if (bundle == null || string.IsNullOrWhiteSpace(bundle.promotionName))
            {
                Debug.LogError("Import failed: invalid bundle or missing promotionName.");
                return false;
            }
            if (bundle.promotion != null) SavePromotion(bundle.promotion);
            if (bundle.wrestlers != null)
            {
                bundle.wrestlers.promotionName = bundle.promotionName;
                SaveWrestlers(bundle.wrestlers);
            }
            if (bundle.titles != null)
            {
                bundle.titles.promotionName = bundle.promotionName;
                SaveTitles(bundle.titles);
            }
            if (bundle.tagTeams != null)
            {
                bundle.tagTeams.promotionName = bundle.promotionName;
                SaveTagTeams(bundle.tagTeams);
            }
            if (bundle.stables != null)
            {
                bundle.stables.promotionName = bundle.promotionName;
                SaveStables(bundle.stables);
            }
            if (bundle.tournaments != null)
            {
                bundle.tournaments.promotionName = bundle.promotionName;
                SaveTournaments(bundle.tournaments);
            }
            Debug.Log($"Imported promotion bundle for {bundle.promotionName}.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Import failed: {ex.Message}");
            return false;
        }
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
            WrestlerCollection data = JsonUtility.FromJson<WrestlerCollection>(json) ?? new WrestlerCollection { promotionName = promotionName };
            bool upgradedW = EnsureIds(data?.wrestlers);
            if (data != null) data.promotionName = promotionName;
            if (upgradedW) SaveWrestlers(data);
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
            TitleCollection data = JsonUtility.FromJson<TitleCollection>(json) ?? new TitleCollection { promotionName = promotionName };
            bool upgradedT = EnsureIds(data?.titles);
            if (data != null) data.promotionName = promotionName;
            if (upgradedT) SaveTitles(data);
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
    // TAG TEAM MANAGEMENT
    // ------------------------
    public static void SaveTagTeams(TagTeamCollection collection)
    {
        if (collection == null || string.IsNullOrEmpty(collection.promotionName))
        {
            Debug.LogError("Cannot save tag teams: collection or promotion name is null.");
            return;
        }
        if (!Directory.Exists(tagTeamFolder))
            Directory.CreateDirectory(tagTeamFolder);
        string safeName = MakeSafeFileName(collection.promotionName);
        string filePath = Path.Combine(tagTeamFolder, $"{safeName}_TagTeams.json");
        try
        {
            string json = JsonUtility.ToJson(collection, true);
            File.WriteAllText(filePath, json);
            Debug.Log($"Tag teams saved for {collection.promotionName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saving tag teams: {ex.Message}");
        }
    }

    // ------------------------
    // STABLES/TRIOS MANAGEMENT
    // ------------------------
    public static void SaveStables(StableCollection collection)
    {
        if (collection == null || string.IsNullOrEmpty(collection.promotionName))
        {
            Debug.LogError("Cannot save stables: collection or promotion name is null.");
            return;
        }
        if (!Directory.Exists(stableFolder))
            Directory.CreateDirectory(stableFolder);
        string safeName = MakeSafeFileName(collection.promotionName);
        string filePath = Path.Combine(stableFolder, $"{safeName}_Stables.json");
        try
        {
            string json = JsonUtility.ToJson(collection, true);
            File.WriteAllText(filePath, json);
            Debug.Log($"Stables saved for {collection.promotionName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saving stables: {ex.Message}");
        }
    }

    public static StableCollection LoadStables(string promotionName)
    {
        if (string.IsNullOrEmpty(promotionName))
            return new StableCollection { promotionName = promotionName };
        string safeName = MakeSafeFileName(promotionName);
        string filePath = Path.Combine(stableFolder, $"{safeName}_Stables.json");
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"No stables found for {promotionName}");
            return new StableCollection { promotionName = promotionName };
        }
        try
        {
            string json = File.ReadAllText(filePath);
            var data = JsonUtility.FromJson<StableCollection>(json) ?? new StableCollection { promotionName = promotionName };
            bool upgraded = EnsureIds(data?.stables);
            if (upgraded) SaveStables(data);
            if (data != null) data.promotionName = promotionName;
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading stables: {ex.Message}");
            return new StableCollection { promotionName = promotionName };
        }
    }

    // ------------------------
    // TOURNAMENT MANAGEMENT
    // ------------------------
    public static void SaveTournaments(TournamentCollection collection)
    {
        if (collection == null || string.IsNullOrEmpty(collection.promotionName))
        {
            Debug.LogError("Cannot save tournaments: collection or promotion name is null.");
            return;
        }
        if (!Directory.Exists(tournamentsFolder))
            Directory.CreateDirectory(tournamentsFolder);
        string safeName = MakeSafeFileName(collection.promotionName);
        string filePath = Path.Combine(tournamentsFolder, $"{safeName}_Tournaments.json");
        try
        {
            string json = JsonUtility.ToJson(collection, true);
            File.WriteAllText(filePath, json);
            Debug.Log($"Tournaments saved for {collection.promotionName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saving tournaments: {ex.Message}");
        }
    }

    public static TournamentCollection LoadTournaments(string promotionName)
    {
        if (string.IsNullOrEmpty(promotionName))
            return new TournamentCollection { promotionName = promotionName };
        string safeName = MakeSafeFileName(promotionName);
        string filePath = Path.Combine(tournamentsFolder, $"{safeName}_Tournaments.json");
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"No tournaments found for {promotionName}");
            return new TournamentCollection { promotionName = promotionName };
        }
        try
        {
            string json = File.ReadAllText(filePath);
            var data = JsonUtility.FromJson<TournamentCollection>(json) ?? new TournamentCollection { promotionName = promotionName };
            // Ensure tournaments have IDs
            bool changed = EnsureIds(data?.tournaments);
            if (changed) SaveTournaments(data);
            if (data != null) data.promotionName = promotionName;
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading tournaments: {ex.Message}");
            return new TournamentCollection { promotionName = promotionName };
        }
    }

    public static TagTeamCollection LoadTagTeams(string promotionName)
    {
        if (string.IsNullOrEmpty(promotionName))
            return new TagTeamCollection { promotionName = promotionName };
        string safeName = MakeSafeFileName(promotionName);
        string filePath = Path.Combine(tagTeamFolder, $"{safeName}_TagTeams.json");
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"No tag teams found for {promotionName}");
            return new TagTeamCollection { promotionName = promotionName };
        }
        try
        {
            string json = File.ReadAllText(filePath);
            var data = JsonUtility.FromJson<TagTeamCollection>(json) ?? new TagTeamCollection { promotionName = promotionName };
            bool upgradedG = EnsureIds(data?.teams);
            if (data != null) data.promotionName = promotionName;
            if (upgradedG) SaveTagTeams(data);
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading tag teams: {ex.Message}");
            return new TagTeamCollection { promotionName = promotionName };
        }
    }
    
    // ------------------------
    // MATCH & TITLE HISTORY MANAGEMENT
    // ------------------------
    public static void SaveMatchHistory(MatchHistoryData history)
    {
        if (history == null || string.IsNullOrEmpty(history.promotionName))
        {
            Debug.LogError("Cannot save history: data or promotion name is null.");
            return;
        }

        if (!Directory.Exists(historyFolder))
            Directory.CreateDirectory(historyFolder);

        string safeName = MakeSafeFileName(history.promotionName);
        string filePath = Path.Combine(historyFolder, $"{safeName}_History.json");

        try
        {
            string json = JsonUtility.ToJson(history, true);
            File.WriteAllText(filePath, json);
            Debug.Log($"💾 History saved for {history.promotionName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Error saving history: {ex.Message}");
        }
    }

    public static MatchHistoryData LoadMatchHistory(string promotionName)
    {
        if (string.IsNullOrEmpty(promotionName))
            return null;

        string safeName = MakeSafeFileName(promotionName);
        string filePath = Path.Combine(historyFolder, $"{safeName}_History.json");

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"⚠️ No history found for {promotionName}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            MatchHistoryData data = JsonUtility.FromJson<MatchHistoryData>(json);
            data.matchResults ??= new List<MatchResultData>();
            data.titleLineages ??= new List<TitleLineageData>();
            Debug.Log($"✅ History loaded for {promotionName}");
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Error loading history: {ex.Message}");
            return null;
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

    // ------------------------
    // ID Utilities
    // ------------------------
    private static bool EnsureIds<T>(List<T> list) where T : class
    {
        if (list == null) return false;
        bool changed = false;
        foreach (var item in list)
        {
            if (item == null) continue;
            var type = item.GetType();
            var field = type.GetField("id");
            if (field != null)
            {
                var val = field.GetValue(item) as string;
                if (string.IsNullOrEmpty(val))
                {
                    field.SetValue(item, System.Guid.NewGuid().ToString("N"));
                    changed = true;
                }
            }
        }
        return changed;
    }
}
