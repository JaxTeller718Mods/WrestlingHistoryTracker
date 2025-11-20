using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable]
public class VenueCityConfig
{
    public List<string> venues = new();
    public List<string> cities = new();
}

public static class VenueCityConfigStore
{
    private const string ConfigFileName = "VenuesAndCities.json";

    public static VenueCityConfig LoadOrCreateDefault()
    {
        var config = new VenueCityConfig();
        string path = Path.Combine(Application.persistentDataPath, ConfigFileName);

        try
        {
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                config = JsonUtility.FromJson<VenueCityConfig>(json) ?? new VenueCityConfig();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading venues/cities config: {ex.Message}");
            config = new VenueCityConfig();
        }

        // If no data present, seed with some well-known defaults
        if (config.venues == null || config.venues.Count == 0)
        {
            config.venues = new List<string>
            {
                "2300 Arena",
                "Allstate Arena",
                "Barclays Center",
                "Boston Garden",
                "Caesars Palace",
                "Cobo Arena",
                "Cow Palace",
                "Daily's Place",
                "ECW Arena",
                "Greensboro Coliseum",
                "Hammerstein Ballroom",
                "Impact Arena",
                "Korakuen Hall",
                "Madison Square Garden",
                "Manhattan Center",
                "Mid-South Coliseum",
                "Nassau Coliseum",
                "Rocket Arena",
                "Rosemont Horizon",
                "Staples Center",
                "The Sportatorium",
                "Tokyo Dome",
                "United Center"
            };
        }

        if (config.cities == null || config.cities.Count == 0)
        {
            config.cities = new List<string>
            {
                "Atlanta, GA",
                "Boston, MA",
                "Chicago, IL",
                "Cleveland, OH",
                "Dallas, TX",
                "Detroit, MI",
                "Greensboro, NC",
                "Houston, TX",
                "Jacksonville, FL",
                "Las Vegas, NV",
                "London, England",
                "Los Angeles, CA",
                "Memphis, TN",
                "Mexico City, Mexico",
                "Montreal, QC",
                "New York, NY",
                "Orlando, FL",
                "Philadelphia, PA",
                "Phoenix, AZ",
                "San Francisco, CA",
                "Tokyo, Japan",
                "Toronto, ON"
            };
        }

        // Normalize and de-duplicate
        config.venues = config.venues
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v)
            .ToList();

        config.cities = config.cities
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();

        // Persist back out so users can edit the JSON for customization
        Save(config);
        return config;
    }

    public static void Save(VenueCityConfig config)
    {
        if (config == null) return;
        try
        {
            string path = Path.Combine(Application.persistentDataPath, ConfigFileName);
            string jsonOut = JsonUtility.ToJson(config, true);
            File.WriteAllText(path, jsonOut);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saving venues/cities config: {ex.Message}");
        }
    }
}
