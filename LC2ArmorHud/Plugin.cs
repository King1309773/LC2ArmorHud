using BepInEx;
using BepInEx.Unity.IL2CPP;
using LC2RuntimeProbe;
using UnityEngine;

namespace LC2ArmorHud;

[BepInPlugin(PluginMetadata.Guid, PluginMetadata.Name, PluginMetadata.Version)]
[BepInProcess(PluginMetadata.ProcessName)]
public sealed class Plugin : BasePlugin
{
    private DemonShellHudController _controller;
    private PluginBehaviour _behaviour;
    private bool _cleaningUp;

    public override void Load()
    {
        try
        {
            var configDirectory = Path.Combine(Paths.ConfigPath, PluginMetadata.ConfigDirectoryName);
            var activePath = Path.Combine(configDirectory, DemonShellHudSettings.FileName);
            var legacyPath = Path.Combine(
                Paths.GameRootPath,
                "UserData",
                PluginMetadata.LegacyDirectoryName,
                DemonShellHudSettings.FileName);
            var migration = HudConfigMigration.EnsureActiveFile(
                activePath,
                legacyPath,
                message => Log.LogWarning(message));

            _controller = new DemonShellHudController(configDirectory, message => Log.LogInfo(message));
            _controller.Initialize();
            _behaviour = AddComponent<PluginBehaviour>();
            _behaviour.Initialize(Tick, Reset);
            Log.LogInfo($"{PluginMetadata.Name} loaded. Settings: {activePath}. Migration: {migration}.");
        }
        catch (Exception ex)
        {
            Cleanup();
            Log.LogError($"{PluginMetadata.Name} initialization failed safely: {ex}");
        }
    }

    public override bool Unload()
    {
        Cleanup();
        return true;
    }

    private void Tick()
    {
        _controller?.Tick(Time.unscaledTime);
        if (Input.GetKeyDown(KeyCode.F5))
        {
            _controller?.ToggleConfigPanel();
        }
    }

    private void Reset()
    {
        _controller?.Reset();
    }

    private void Cleanup()
    {
        if (_cleaningUp)
        {
            return;
        }

        _cleaningUp = true;
        try
        {
            _behaviour?.Shutdown();
            _controller?.Dispose();
            if (_behaviour is not null)
            {
                UnityEngine.Object.Destroy(_behaviour);
            }

            _behaviour = null;
            _controller = null;
        }
        finally
        {
            _cleaningUp = false;
        }
    }
}
