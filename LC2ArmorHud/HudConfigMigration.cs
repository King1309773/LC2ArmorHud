namespace LC2ArmorHud;

internal enum HudConfigMigrationOutcome
{
    ActiveFileAlreadyPresent,
    LegacyFileCopied,
    LegacyFileNotFound,
    Failed
}

internal static class HudConfigMigration
{
    internal static HudConfigMigrationOutcome EnsureActiveFile(string activePath, string legacyPath, Action<string> log)
    {
        if (File.Exists(activePath))
        {
            return HudConfigMigrationOutcome.ActiveFileAlreadyPresent;
        }

        if (!File.Exists(legacyPath))
        {
            return HudConfigMigrationOutcome.LegacyFileNotFound;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(activePath)!);
            File.Copy(legacyPath, activePath, overwrite: false);
            return HudConfigMigrationOutcome.LegacyFileCopied;
        }
        catch (Exception ex)
        {
            log($"HUD settings migration skipped safely: {ex.Message}");
            return HudConfigMigrationOutcome.Failed;
        }
    }
}
