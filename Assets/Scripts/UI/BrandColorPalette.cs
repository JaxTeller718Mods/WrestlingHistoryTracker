using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BrandColorPalette", menuName = "WrestlingHistory/Brand Color Palette")]
public class BrandColorPalette : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string brandName;
        public Color primary = Color.white;
        public Color accent = Color.white;
        public Color text = Color.black;
    }

    public List<Entry> entries = new();

    private static BrandColorPalette cachedInstance;

    /// <summary>
    /// Load the shared palette from Resources/BrandColorPalette.asset, if present.
    /// </summary>
    public static BrandColorPalette Load()
    {
        if (cachedInstance != null) return cachedInstance;
        cachedInstance = Resources.Load<BrandColorPalette>("BrandColorPalette");
        return cachedInstance;
    }

    public bool TryGet(string brandName, out Entry entry)
    {
        entry = null;
        if (string.IsNullOrWhiteSpace(brandName) || entries == null) return false;
        var target = brandName.Trim();
        foreach (var e in entries)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.brandName)) continue;
            if (string.Equals(e.brandName.Trim(), target, StringComparison.OrdinalIgnoreCase))
            {
                entry = e;
                return true;
            }
        }
        return false;
    }
}

