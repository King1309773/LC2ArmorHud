using System.Text.Json;

namespace LC2RuntimeProbe;

internal enum ArmorHudKind
{
    DemonShell,
    BeastArmor,
    DemonSlayerHeavyArmor,
    BlackMageRobe,
    GuardianWhiteRobe,
    RedstoneMageRobe
}

internal sealed class ArmorHudSettings
{
    public bool Enabled { get; set; } = true;

    public string Anchor { get; set; } = "BottomLeft";

    public double X { get; set; }

    public double Y { get; set; }

    public double Width { get; set; }

    public double Height { get; set; }

    public double Opacity { get; set; } = 0.87;

    public bool ShowText { get; set; } = true;

    public bool ShowCountdown { get; set; } = true;

    internal static ArmorHudSettings CreateDefault(ArmorHudKind kind)
    {
        var rect = ArmorHudCatalog.Get(kind).DefaultRect;
        return new ArmorHudSettings
        {
            X = rect.X,
            Y = rect.Y,
            Width = rect.Width,
            Height = rect.Height
        }.Normalize();
    }

    internal ArmorHudSettings Normalize()
    {
        Anchor = ProbeUtilities.NormalizeDemonShellHudAnchor(Anchor);
        X = ProbeUtilities.ClampDemonShellHudConfigValue("x", X);
        Y = ProbeUtilities.ClampDemonShellHudConfigValue("y", Y);
        Width = ProbeUtilities.ClampDemonShellHudConfigValue("width", Width);
        Height = ProbeUtilities.ClampDemonShellHudConfigValue("height", Height);
        Opacity = ProbeUtilities.ClampDemonShellHudConfigValue("opacity", Opacity);
        return this;
    }

    internal ArmorHudSettings Clone()
    {
        return new ArmorHudSettings
        {
            Enabled = Enabled,
            Anchor = Anchor,
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            Opacity = Opacity,
            ShowText = ShowText,
            ShowCountdown = ShowCountdown
        }.Normalize();
    }

    internal DemonShellHudRect ResolveRect(double screenWidth, double screenHeight)
    {
        Normalize();
        return ProbeUtilities.ResolveDemonShellHudRect(Anchor, X, Y, Width, Height, screenWidth, screenHeight);
    }
}

internal sealed class DemonShellHudSettings
{
    internal const string FileName = "demon-shell-hud.json";

    public ArmorHudSettings DemonShell { get; set; } = ArmorHudSettings.CreateDefault(ArmorHudKind.DemonShell);

    public ArmorHudSettings BeastArmor { get; set; } = ArmorHudSettings.CreateDefault(ArmorHudKind.BeastArmor);

    public ArmorHudSettings DemonSlayerHeavyArmor { get; set; } =
        ArmorHudSettings.CreateDefault(ArmorHudKind.DemonSlayerHeavyArmor);

    public ArmorHudSettings BlackMageRobe { get; set; } = ArmorHudSettings.CreateDefault(ArmorHudKind.BlackMageRobe);

    public ArmorHudSettings GuardianWhiteRobe { get; set; } = ArmorHudSettings.CreateDefault(ArmorHudKind.GuardianWhiteRobe);

    public ArmorHudSettings RedstoneMageRobe { get; set; } = ArmorHudSettings.CreateDefault(ArmorHudKind.RedstoneMageRobe);

    internal static DemonShellHudSettings CreateDefault()
    {
        return new DemonShellHudSettings().Normalize();
    }

    internal DemonShellHudSettings Normalize()
    {
        DemonShell = (DemonShell ?? ArmorHudSettings.CreateDefault(ArmorHudKind.DemonShell)).Normalize();
        BeastArmor = (BeastArmor ?? ArmorHudSettings.CreateDefault(ArmorHudKind.BeastArmor)).Normalize();
        DemonSlayerHeavyArmor = (DemonSlayerHeavyArmor ??
                                 ArmorHudSettings.CreateDefault(ArmorHudKind.DemonSlayerHeavyArmor)).Normalize();
        BlackMageRobe = (BlackMageRobe ?? ArmorHudSettings.CreateDefault(ArmorHudKind.BlackMageRobe)).Normalize();
        GuardianWhiteRobe = (GuardianWhiteRobe ?? ArmorHudSettings.CreateDefault(ArmorHudKind.GuardianWhiteRobe)).Normalize();
        RedstoneMageRobe = (RedstoneMageRobe ?? ArmorHudSettings.CreateDefault(ArmorHudKind.RedstoneMageRobe)).Normalize();
        return this;
    }

    internal DemonShellHudSettings Clone()
    {
        return new DemonShellHudSettings
        {
            DemonShell = DemonShell.Clone(),
            BeastArmor = BeastArmor.Clone(),
            DemonSlayerHeavyArmor = DemonSlayerHeavyArmor.Clone(),
            BlackMageRobe = BlackMageRobe.Clone(),
            GuardianWhiteRobe = GuardianWhiteRobe.Clone(),
            RedstoneMageRobe = RedstoneMageRobe.Clone()
        }.Normalize();
    }

    internal ArmorHudSettings GetArmorSettings(ArmorHudKind kind)
    {
        Normalize();
        return kind switch
        {
            ArmorHudKind.BeastArmor => BeastArmor,
            ArmorHudKind.DemonSlayerHeavyArmor => DemonSlayerHeavyArmor,
            ArmorHudKind.BlackMageRobe => BlackMageRobe,
            ArmorHudKind.GuardianWhiteRobe => GuardianWhiteRobe,
            ArmorHudKind.RedstoneMageRobe => RedstoneMageRobe,
            _ => DemonShell
        };
    }

    internal void SetArmorSettings(ArmorHudKind kind, ArmorHudSettings settings)
    {
        var normalized = (settings ?? ArmorHudSettings.CreateDefault(kind)).Normalize();
        switch (kind)
        {
            case ArmorHudKind.BeastArmor:
                BeastArmor = normalized;
                break;
            case ArmorHudKind.DemonSlayerHeavyArmor:
                DemonSlayerHeavyArmor = normalized;
                break;
            case ArmorHudKind.BlackMageRobe:
                BlackMageRobe = normalized;
                break;
            case ArmorHudKind.GuardianWhiteRobe:
                GuardianWhiteRobe = normalized;
                break;
            case ArmorHudKind.RedstoneMageRobe:
                RedstoneMageRobe = normalized;
                break;
            default:
                DemonShell = normalized;
                break;
        }
    }
}

internal sealed class DemonShellHudSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly Action<string> _log;
    private DateTime _lastWriteUtc;

    internal DemonShellHudSettingsStore(string dataDirectory, Action<string> log)
    {
        SettingsPath = Path.Combine(dataDirectory, DemonShellHudSettings.FileName);
        _log = log;
        Current = DemonShellHudSettings.CreateDefault();
    }

    internal string SettingsPath { get; }

    internal DemonShellHudSettings Current { get; private set; }

    internal DemonShellHudSettings Initialize()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath) ?? string.Empty);
        if (!File.Exists(SettingsPath))
        {
            WriteDefaultSettings();
        }

        Current = LoadSettings();
        Save(Current);
        return Current;
    }

    internal ArmorHudSettings GetArmorSettings(ArmorHudKind kind)
    {
        return Current.GetArmorSettings(kind);
    }

    internal ArmorHudSettings SaveArmorSettings(ArmorHudKind kind, ArmorHudSettings settings)
    {
        var next = Current.Clone();
        next.SetArmorSettings(kind, settings);
        Save(next);
        return Current.GetArmorSettings(kind);
    }

    internal ArmorHudSettings ResetArmorSettings(ArmorHudKind kind)
    {
        return SaveArmorSettings(kind, ArmorHudSettings.CreateDefault(kind));
    }

    internal DemonShellHudSettings Save(DemonShellHudSettings settings)
    {
        Current = (settings ?? DemonShellHudSettings.CreateDefault()).Normalize();
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath) ?? string.Empty);
        var json = JsonSerializer.Serialize(Current, JsonOptions);
        File.WriteAllText(SettingsPath, json);
        _lastWriteUtc = SafeGet(() => File.GetLastWriteTimeUtc(SettingsPath), DateTime.MinValue);
        return Current;
    }

    internal DemonShellHudSettings ResetToDefaults()
    {
        return Save(DemonShellHudSettings.CreateDefault());
    }

    internal DemonShellHudSettings ReloadIfChanged()
    {
        var lastWriteUtc = SafeGet(() => File.GetLastWriteTimeUtc(SettingsPath), DateTime.MinValue);
        if (lastWriteUtc == DateTime.MinValue || lastWriteUtc == _lastWriteUtc)
        {
            return Current;
        }

        Current = LoadSettings();
        return Current;
    }

    private DemonShellHudSettings LoadSettings()
    {
        try
        {
            var json = File.ReadAllText(SettingsPath);
            var settings = LoadCurrentOrLegacySettings(json);
            _lastWriteUtc = SafeGet(() => File.GetLastWriteTimeUtc(SettingsPath), DateTime.MinValue);
            return settings.Normalize();
        }
        catch (Exception ex)
        {
            _log($"Demon shell HUD settings load failed safely: {ex.Message}");
            return DemonShellHudSettings.CreateDefault();
        }
    }

    private static DemonShellHudSettings LoadCurrentOrLegacySettings(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.TryGetProperty("demonShell", out _)
            || root.TryGetProperty("beastArmor", out _)
            || root.TryGetProperty("demonSlayerHeavyArmor", out _)
            || root.TryGetProperty("blackMageRobe", out _)
            || root.TryGetProperty("guardianWhiteRobe", out _)
            || root.TryGetProperty("redstoneMageRobe", out _))
        {
            return JsonSerializer.Deserialize<DemonShellHudSettings>(json, JsonOptions)
                   ?? DemonShellHudSettings.CreateDefault();
        }

        var legacy = JsonSerializer.Deserialize<ArmorHudSettings>(json, JsonOptions)
                     ?? ArmorHudSettings.CreateDefault(ArmorHudKind.DemonShell);
        return new DemonShellHudSettings
        {
            DemonShell = legacy.Normalize(),
            BeastArmor = ArmorHudSettings.CreateDefault(ArmorHudKind.BeastArmor),
            DemonSlayerHeavyArmor = ArmorHudSettings.CreateDefault(ArmorHudKind.DemonSlayerHeavyArmor),
            BlackMageRobe = ArmorHudSettings.CreateDefault(ArmorHudKind.BlackMageRobe),
            GuardianWhiteRobe = ArmorHudSettings.CreateDefault(ArmorHudKind.GuardianWhiteRobe),
            RedstoneMageRobe = ArmorHudSettings.CreateDefault(ArmorHudKind.RedstoneMageRobe)
        }.Normalize();
    }

    private void WriteDefaultSettings()
    {
        Save(DemonShellHudSettings.CreateDefault());
    }

    private static T SafeGet<T>(Func<T> getter, T fallback)
    {
        try
        {
            return getter();
        }
        catch
        {
            return fallback;
        }
    }
}
