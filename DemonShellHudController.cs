namespace LC2RuntimeProbe;

internal sealed class DemonShellHudController
{
    private const float SampleIntervalSeconds = 0.1f;
    private const float SettingsReloadIntervalSeconds = 1.0f;

    private readonly DemonShellHudSettingsStore _settingsStore;
    private readonly DemonShellHudCounterReader _counterReader = new();
    private readonly ArmorHudCountdownTracker _countdownTracker = new();
    private readonly Dictionary<string, DemonShellHudRenderer> _renderers = new(StringComparer.Ordinal);
    private readonly DemonShellHudConfigPanel _configPanel;
    private readonly Action<string> _log;
    private ArmorHudCounterSnapshot _snapshot = ArmorHudCounterSnapshot.Empty;
    private bool _rendererFailureLogged;
    private bool _renderSuccessLogged;
    private float _nextSampleAt;
    private float _lastCountdownSampleAt;
    private float _nextSettingsReloadAt;
    private float _nextStatusLogAt;
    private string _lastLoggedStatus = string.Empty;

    internal DemonShellHudController(string dataDirectory, Action<string> log)
    {
        _settingsStore = new DemonShellHudSettingsStore(dataDirectory, log);
        _configPanel = new DemonShellHudConfigPanel(_settingsStore, log);
        _log = log;
    }

    internal string SettingsPath => _settingsStore.SettingsPath;

    internal bool IsConfigPanelVisible => _configPanel.IsVisible;

    internal void Initialize()
    {
        _settingsStore.Initialize();
        _log($"Demon shell HUD settings: {SettingsPath}");
        _log($"Demon shell HUD renderer: {ProbeUtilities.GetDemonShellHudRendererBackendLabel()}");
    }

    internal void ToggleConfigPanel()
    {
        _configPanel.Toggle();
    }

    internal void Reset()
    {
        _counterReader.Reset();
        _countdownTracker.Clear();
        _configPanel.Hide();
        HideAll();
        _rendererFailureLogged = false;
        _renderSuccessLogged = false;
        _nextSampleAt = 0;
        _lastCountdownSampleAt = 0;
        _nextSettingsReloadAt = 0;
        _nextStatusLogAt = 0;
        _lastLoggedStatus = string.Empty;
        _snapshot = ArmorHudCounterSnapshot.Empty;
    }

    internal void Dispose()
    {
        Reset();
        foreach (var renderer in _renderers.Values)
        {
            renderer.Dispose();
        }

        _renderers.Clear();
        _configPanel.Dispose();
    }

    internal void Tick(float unscaledTime)
    {
        _configPanel.Tick();

        if (unscaledTime >= _nextSettingsReloadAt)
        {
            _settingsStore.ReloadIfChanged();
            _nextSettingsReloadAt = unscaledTime + SettingsReloadIntervalSeconds;
        }

        if (unscaledTime >= _nextSampleAt)
        {
            _nextSampleAt = unscaledTime + SampleIntervalSeconds;
            _snapshot = _counterReader.ReadSnapshot(unscaledTime);
            ObserveCountdowns(unscaledTime);
            LogStatusIfUseful(_snapshot.Status, unscaledTime);
        }

        DrawCurrentState();
    }

    internal void Draw()
    {
        DrawCurrentState();
    }

    private void DrawCurrentState()
    {
        try
        {
            var drewAny = false;
            foreach (var definition in ArmorHudCatalog.All)
            {
                drewAny |= DrawArmor(definition);
            }

            if (!drewAny && _configPanel.IsVisible)
            {
                DrawPreview();
                drewAny = true;
            }

            if (!drewAny)
            {
                HideAll();
            }

            _rendererFailureLogged = false;
            if (drewAny && !_renderSuccessLogged)
            {
                _renderSuccessLogged = true;
                _log("Armor HUD Unity UI draw active");
            }
        }
        catch (Exception ex)
        {
            HideAll();
            if (!_rendererFailureLogged)
            {
                _rendererFailureLogged = true;
                _log($"Armor HUD Unity UI draw failed safely: {ex.Message}");
            }
        }
    }

    private bool DrawArmor(ArmorHudDefinition definition)
    {
        var settings = _settingsStore.GetArmorSettings(definition.Kind);
        if (!settings.Enabled || !_snapshot.IsEquipped(definition.Kind))
        {
            HideArmor(definition);
            return false;
        }

        var countdownRule = ArmorHudCatalog.GetCountdownRule(definition.Kind);
        DrawRows(definition, settings, row =>
        {
            ProbeUtilities.TryBuildArmorHudProgress(
                _snapshot.GetCounterValue(row.CounterKey),
                row.MaxValue,
                row.DisplayValuePerStack,
                out var progress);
            return progress;
        }, row => _countdownTracker.Get(row.CounterKey, countdownRule, _lastCountdownSampleAt));
        return true;
    }

    private void DrawPreview()
    {
        HideAll();
        var definition = ArmorHudCatalog.Get(_configPanel.SelectedKind);
        var settings = _settingsStore.GetArmorSettings(definition.Kind);
        var countdownRule = ArmorHudCatalog.GetCountdownRule(definition.Kind);
        DrawRows(definition, settings, row =>
        {
            var previewValue = Math.Max(1, row.MaxValue / 2);
            ProbeUtilities.TryBuildArmorHudProgress(previewValue, row.MaxValue, row.DisplayValuePerStack, out var progress);
            return progress;
        }, _ => countdownRule.Mode == ArmorHudCountdownMode.None
            ? ArmorHudCountdownSnapshot.Hidden
            : new ArmorHudCountdownSnapshot(true, countdownRule.DurationSeconds / 2.0f, 0.5f));
    }

    private void DrawRows(
        ArmorHudDefinition definition,
        ArmorHudSettings settings,
        Func<ArmorHudRowDefinition, DemonShellHudProgress> buildProgress,
        Func<ArmorHudRowDefinition, ArmorHudCountdownSnapshot> buildCountdown)
    {
        for (var index = 0; index < definition.Rows.Count; index++)
        {
            var row = definition.Rows[index];
            GetRenderer(definition.Kind, index).Draw(
                OffsetSettings(settings, index),
                buildProgress(row),
                buildCountdown(row),
                row.Label,
                row.ValueSuffix);
        }
    }

    private void ObserveCountdowns(float unscaledTime)
    {
        _lastCountdownSampleAt = unscaledTime;
        foreach (var definition in ArmorHudCatalog.All)
        {
            var countdownRule = ArmorHudCatalog.GetCountdownRule(definition.Kind);
            if (countdownRule.Mode == ArmorHudCountdownMode.None)
            {
                continue;
            }

            var equipped = _snapshot.IsEquipped(definition.Kind);
            for (var index = 0; index < definition.Rows.Count; index++)
            {
                var row = definition.Rows[index];
                if (!equipped)
                {
                    _countdownTracker.Clear(row.CounterKey);
                    continue;
                }

                var stackCount = Math.Clamp(
                    (int)Math.Floor(_snapshot.GetCounterValue(row.CounterKey)),
                    0,
                    row.MaxValue);
                _countdownTracker.Observe(row.CounterKey, countdownRule, stackCount, unscaledTime);
            }
        }
    }

    private static ArmorHudSettings OffsetSettings(ArmorHudSettings settings, int rowsAboveBottom)
    {
        var clone = settings.Clone();
        clone.Y += rowsAboveBottom * (clone.Height + 4.0);
        return clone.Normalize();
    }

    private void HideAll()
    {
        foreach (var renderer in _renderers.Values)
        {
            renderer.Hide();
        }
    }

    private void HideArmor(ArmorHudDefinition definition)
    {
        for (var index = 0; index < definition.Rows.Count; index++)
        {
            GetRenderer(definition.Kind, index).Hide();
        }
    }

    private DemonShellHudRenderer GetRenderer(ArmorHudKind kind, int rowIndex)
    {
        var key = $"{kind}:{rowIndex}";
        if (_renderers.TryGetValue(key, out var renderer))
        {
            return renderer;
        }

        renderer = new DemonShellHudRenderer($"LC2RuntimeProbe_{kind}Hud_{rowIndex}");
        _renderers[key] = renderer;
        return renderer;
    }

    private void LogStatusIfUseful(string status, float unscaledTime)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return;
        }

        if (string.Equals(status, _lastLoggedStatus, StringComparison.Ordinal)
            && unscaledTime < _nextStatusLogAt)
        {
            return;
        }

        _lastLoggedStatus = status;
        _nextStatusLogAt = unscaledTime + 5.0f;
        _log($"Armor HUD status: {status}");
    }
}
