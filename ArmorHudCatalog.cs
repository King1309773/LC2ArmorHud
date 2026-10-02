using System;

namespace LC2RuntimeProbe;

internal sealed class ArmorHudDefinition
{
    internal ArmorHudDefinition(
        ArmorHudKind kind,
        string itemId,
        string configLabel,
        DemonShellHudRect defaultRect,
        params ArmorHudRowDefinition[] rows)
    {
        Kind = kind;
        ItemId = itemId;
        ConfigLabel = configLabel;
        DefaultRect = defaultRect;
        Rows = Array.AsReadOnly(rows);
    }

    internal ArmorHudKind Kind { get; }

    internal string ItemId { get; }

    internal string ConfigLabel { get; }

    internal DemonShellHudRect DefaultRect { get; }

    internal IReadOnlyList<ArmorHudRowDefinition> Rows { get; }
}

internal readonly record struct ArmorHudRowDefinition(
    string CounterKey,
    string Label,
    int MaxValue,
    int DisplayValuePerStack = 1,
    string ValueSuffix = "");

internal static class ArmorHudCatalog
{
    private static readonly DemonShellHudRect RequestedDefaultRect = new(370, 255, 290, 34);

    private static readonly ArmorHudDefinition[] Definitions =
    {
        new(
            ArmorHudKind.DemonShell,
            "NA_036",
            "\u7532\u58f3",
            RequestedDefaultRect,
            new ArmorHudRowDefinition("Equipment#NA_036", string.Empty, 60)),
        new(
            ArmorHudKind.BeastArmor,
            "HA_019",
            "\u517d\u9762\u94e0",
            RequestedDefaultRect,
            new ArmorHudRowDefinition("Equipment#HA_019_Default_0", "\u751f\u547d\u503c", 20, 1, "%"),
            new ArmorHudRowDefinition("Equipment#HA_019", "\u66b4\u51fb\u7387", 20, 1, "%")),
        new(
            ArmorHudKind.DemonSlayerHeavyArmor,
            "HA_008",
            "\u5c60\u9b54\u8005\u91cd\u7532",
            RequestedDefaultRect,
            new ArmorHudRowDefinition("Equipment#HA_008", "\u653b\u51fb\u529b", 12, 3, "%")),
        new(
            ArmorHudKind.BlackMageRobe,
            "CA_026",
            "\u9ed1\u9b54\u6cd5\u5e08\u957f\u888d",
            RequestedDefaultRect,
            new ArmorHudRowDefinition("Equipment#CA_026", "\u6280\u80fd\u5f3a\u5ea6", 35, 1, "%")),
        new(
            ArmorHudKind.GuardianWhiteRobe,
            "CA_016",
            "\u5b88\u62a4\u8005\u767d\u888d",
            RequestedDefaultRect,
            new ArmorHudRowDefinition("Equipment#CA_016", "\u6280\u80fd\u5f3a\u5ea6", 8, 10, "%")),
        new(
            ArmorHudKind.RedstoneMageRobe,
            "CA_017",
            "\u7ea2\u77f3\u6cd5\u5e08\u957f\u888d",
            RequestedDefaultRect,
            new ArmorHudRowDefinition("Equipment#CA_017", "\u6280\u80fd\u5f3a\u5ea6", 12, 5, "%"))
    };

    private static readonly IReadOnlyList<ArmorHudDefinition> ReadOnlyDefinitions = Array.AsReadOnly(Definitions);

    internal static IReadOnlyList<ArmorHudDefinition> All => ReadOnlyDefinitions;

    internal static ArmorHudDefinition Get(ArmorHudKind kind)
    {
        return Definitions.First(definition => definition.Kind == kind);
    }

    internal static ArmorHudCountdownRule GetCountdownRule(ArmorHudKind kind)
    {
        return kind switch
        {
            ArmorHudKind.BeastArmor => new ArmorHudCountdownRule(ArmorHudCountdownMode.RefreshAll, 15, 20),
            ArmorHudKind.BlackMageRobe => new ArmorHudCountdownRule(ArmorHudCountdownMode.Independent, 10, 35),
            ArmorHudKind.GuardianWhiteRobe => new ArmorHudCountdownRule(ArmorHudCountdownMode.Independent, 15, 8),
            ArmorHudKind.RedstoneMageRobe => new ArmorHudCountdownRule(ArmorHudCountdownMode.Independent, 10, 12),
            _ => ArmorHudCountdownRule.None
        };
    }

    internal static bool SupportsCountdown(ArmorHudKind kind)
    {
        return GetCountdownRule(kind).Mode != ArmorHudCountdownMode.None;
    }

    internal static ArmorHudKind Next(ArmorHudKind kind)
    {
        var index = Array.FindIndex(Definitions, definition => definition.Kind == kind);
        return Definitions[(index + 1 + Definitions.Length) % Definitions.Length].Kind;
    }

    internal static bool IsCounterKey(string key)
    {
        return Definitions.Any(definition => definition.Rows.Any(row =>
            string.Equals(row.CounterKey, key, StringComparison.OrdinalIgnoreCase)));
    }
}
