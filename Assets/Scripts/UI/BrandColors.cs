using System;
using UnityEngine;

/// <summary>
/// Central helper for resolving brand colors.
/// First consults a BrandColorPalette ScriptableObject (if present),
/// then falls back to a deterministic color based on the brand name.
/// </summary>
public static class BrandColors
{
    private static readonly Color DefaultPrimary = new Color32(40, 40, 40, 255);
    private static readonly Color DefaultAccent  = new Color32(255, 213, 74, 255);
    private static readonly Color DefaultText    = Color.white;

    private static bool TryGetFromPalette(string brand, out Color primary, out Color accent, out Color text)
    {
        primary = default;
        accent = default;
        text = default;

        var palette = BrandColorPalette.Load();
        if (palette == null) return false;

        if (!palette.TryGet(brand, out var entry) || entry == null) return false;

        primary = entry.primary;
        accent = entry.accent;
        text = entry.text;
        return true;
    }

    public static Color GetPrimary(string brand)
    {
        if (TryGetFromPalette(brand, out var primary, out _, out _))
            return primary;

        if (string.IsNullOrWhiteSpace(brand))
            return DefaultPrimary;

        return HashToColor(brand);
    }

    public static Color GetAccent(string brand)
    {
        if (TryGetFromPalette(brand, out _, out var accent, out _))
            return accent;

        var baseColor = GetPrimary(brand);
        Color.RGBToHSV(baseColor, out float h, out float s, out float v);
        v = Mathf.Clamp01(v + 0.15f);
        s = Mathf.Clamp01(s + 0.1f);
        return Color.HSVToRGB(h, s, v);
    }

    public static Color GetText(string brand)
    {
        if (TryGetFromPalette(brand, out _, out _, out var text))
            return text;

        var bg = GetPrimary(brand);
        float luminance = 0.299f * bg.r + 0.587f * bg.g + 0.114f * bg.b;
        return luminance > 0.55f ? Color.black : Color.white;
    }

    private static Color HashToColor(string brand)
    {
        int hash = brand.Trim().ToLowerInvariant().GetHashCode();
        uint u = unchecked((uint)hash);

        float hue = (u & 0xFFFF) / 65535f;
        float sat = 0.55f + ((u >> 16) & 0xFF) / 255f * 0.25f;
        float val = 0.7f;

        return Color.HSVToRGB(hue, sat, val);
    }
}

