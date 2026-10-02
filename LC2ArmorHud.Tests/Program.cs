using LC2ArmorHud;

var failures = new List<string>();

void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        failures.Add($"{name}: expected '{expected}', got '{actual}'");
    }
}

void AssertTrue(bool value, string name)
{
    if (!value)
    {
        failures.Add($"{name}: expected true");
    }
}

AssertEqual("com.localofflineprobe.lc2armorhud", PluginMetadata.Guid, "plugin GUID");
AssertEqual("LostCastle2.exe", PluginMetadata.ProcessName, "process restriction");
AssertEqual("LC2ArmorHud", PluginMetadata.ConfigDirectoryName, "config directory");

using (var sandbox = new TemporaryDirectory())
{
    var activePath = Path.Combine(sandbox.Path, "BepInEx", "config", "LC2ArmorHud", "demon-shell-hud.json");
    var legacyPath = Path.Combine(sandbox.Path, "UserData", "LC2RuntimeProbe", "demon-shell-hud.json");
    Directory.CreateDirectory(Path.GetDirectoryName(legacyPath)!);
    File.WriteAllText(legacyPath, "{\"demonShell\":{\"x\":370}}");

    AssertEqual(
        HudConfigMigrationOutcome.LegacyFileCopied,
        HudConfigMigration.EnsureActiveFile(activePath, legacyPath, _ => { }),
        "legacy settings copy");
    AssertEqual(File.ReadAllText(legacyPath), File.ReadAllText(activePath), "copied content");

    File.WriteAllText(activePath, "{\"demonShell\":{\"x\":999}}");
    AssertEqual(
        HudConfigMigrationOutcome.ActiveFileAlreadyPresent,
        HudConfigMigration.EnsureActiveFile(activePath, legacyPath, _ => { }),
        "active settings are never overwritten");
    AssertEqual("{\"demonShell\":{\"x\":999}}", File.ReadAllText(activePath), "active content retained");
}

var sourceRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
var pluginRoot = Path.Combine(sourceRoot, "LC2ArmorHud");
var pluginSource = File.ReadAllText(Path.Combine(pluginRoot, "Plugin.cs"));
var behaviourSource = File.ReadAllText(Path.Combine(pluginRoot, "PluginBehaviour.cs"));
AssertTrue(pluginSource.Contains("[BepInPlugin(PluginMetadata.Guid", StringComparison.Ordinal), "BepInEx identity");
AssertTrue(pluginSource.Contains("[BepInProcess(PluginMetadata.ProcessName)]", StringComparison.Ordinal), "game process restriction");
AssertTrue(pluginSource.Contains("Paths.ConfigPath", StringComparison.Ordinal), "BepInEx config path");
AssertTrue(pluginSource.Contains("HudConfigMigration.EnsureActiveFile", StringComparison.Ordinal), "legacy migration");
AssertTrue(pluginSource.Contains("AddComponent<PluginBehaviour>()", StringComparison.Ordinal), "IL2CPP host");
AssertTrue(behaviourSource.Contains("SceneManager.sceneLoaded", StringComparison.Ordinal), "scene reset subscription");
AssertTrue(pluginSource.Contains("Time.unscaledTime", StringComparison.Ordinal), "unscaled HUD time");

if (failures.Count > 0)
{
    throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
}

Console.WriteLine("LC2ArmorHud migration tests passed.");

internal sealed class TemporaryDirectory : IDisposable
{
    internal TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "LC2ArmorHud.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    internal string Path { get; }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
