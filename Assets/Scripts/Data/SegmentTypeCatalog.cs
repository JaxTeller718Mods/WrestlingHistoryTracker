using System.Collections.Generic;

/// <summary>
/// Central catalog of default segment/angle types so UI panels stay in sync.
/// </summary>
public static class SegmentTypeCatalog
{
    public static readonly List<string> Types = new()
    {
        "In-Ring Promo",
        "Backstage Promo",
        "Open Challenge",
        "Contract Signing",
        "Pre-Match Attack",
        "Post-Match Attack",
        "Interference",
        "Debut/Return",
        "Announcement",
        "Confrontation",
        "Celebration",
        "Brawl"
    };
}
