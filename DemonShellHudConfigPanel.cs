using System.Globalization;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace LC2RuntimeProbe;

internal sealed class DemonShellHudConfigPanel
{
    private const string RootName = "LC2RuntimeProbe_DemonShellHudConfig";
    private const float PanelWidth = 640.0f;
    private const float PanelHeight = 520.0f;
    private const float PanelMargin = 28.0f;
    private const float PanelTop = 112.0f;
    private const float ArmorListX = 16.0f;
    private const float ArmorListWidth = 196.0f;
    private const float SettingsX = 226.0f;
    private const float SettingsWidth = 390.0f;

    private readonly DemonShellHudSettingsStore _settingsStore;
    private readonly Action<string> _log;
    private readonly List<ArmorOption> _armorOptions = new();
    private readonly List<ConfigRow> _rows = new();
    private readonly List<ClickTarget> _clickTargets = new();
    private GameObject _rootObject;
    private RectTransform _panelRect;
    private Image _panelImage;
    private RectTransform _resetRect;
    private RectTransform _closeRect;
    private Text _titleText;
    private Text _statusText;
    private Text _armorHeaderText;
    private Text _settingsHeaderText;
    private Text _resetText;
    private Text _closeText;
    private Font _font;
    private bool _visible;
    private bool _layoutDirty = true;
    private int _lastScreenWidth = -1;
    private int _lastScreenHeight = -1;
    private ArmorHudKind _selectedKind = ArmorHudKind.DemonShell;
    private int _selectedIndex;
    private string _status = "\u5df2\u81ea\u52a8\u4fdd\u5b58";

    internal DemonShellHudConfigPanel(DemonShellHudSettingsStore settingsStore, Action<string> log)
    {
        _settingsStore = settingsStore;
        _log = log;
    }

    internal bool IsVisible => _visible;

    internal ArmorHudKind SelectedKind => _selectedKind;

    internal void Toggle()
    {
        if (_visible)
        {
            Hide();
            return;
        }

        EnsurePanel();
        _visible = true;
        _rootObject.SetActive(true);
        _status = "\u5df2\u81ea\u52a8\u4fdd\u5b58";
        _layoutDirty = true;
        RefreshValues();
        _log("Armor HUD config panel opened");
    }

    internal void Hide()
    {
        _visible = false;
        if (_rootObject is not null)
        {
            _rootObject.SetActive(false);
        }
    }

    internal void Dispose()
    {
        Hide();
        if (_rootObject is not null)
        {
            UnityEngine.Object.Destroy(_rootObject);
        }

        _rootObject = null;
        _panelRect = null;
        _panelImage = null;
        _resetRect = null;
        _closeRect = null;
        _titleText = null;
        _statusText = null;
        _armorHeaderText = null;
        _settingsHeaderText = null;
        _resetText = null;
        _closeText = null;
        _font = null;
        _clickTargets.Clear();
        _visible = false;
        _layoutDirty = true;
        _lastScreenWidth = -1;
        _lastScreenHeight = -1;
    }

    internal void Tick()
    {
        if (!_visible)
        {
            return;
        }

        EnsurePanel();
        RefreshLayoutIfNeeded();
        HandleKeyboard();
        HandleMouse();
        RefreshLayoutIfNeeded();
    }

    private void RefreshLayoutIfNeeded()
    {
        if (!ProbeUtilities.ShouldRefreshArmorHudConfigPanelLayout(
                _layoutDirty,
                Screen.width,
                Screen.height,
                _lastScreenWidth,
                _lastScreenHeight))
        {
            return;
        }

        UpdateLayout();
        _layoutDirty = false;
        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;
    }

    private void EnsurePanel()
    {
        if (_rootObject is not null)
        {
            return;
        }

        LoadFont();

        _rootObject = new GameObject(
            RootName,
            Il2CppType.Of<RectTransform>(),
            Il2CppType.Of<Canvas>(),
            Il2CppType.Of<CanvasScaler>());
        UnityEngine.Object.DontDestroyOnLoad(_rootObject);

        var canvas = _rootObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32766;

        var scaler = _rootObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1.0f;
        scaler.referencePixelsPerUnit = 100.0f;

        var panel = CreateBox("Panel", _rootObject.transform, new Color(0.025f, 0.020f, 0.040f, 0.90f));
        _panelRect = panel.Rect;
        _panelImage = panel.Image;

        _titleText = CreateText("Title", _panelRect, "\u88c5\u5907HUD\u914d\u7f6e", 22, TextAnchor.MiddleLeft);
        _statusText = CreateText("Status", _panelRect, _status, 13, TextAnchor.MiddleLeft);
        _armorHeaderText = CreateText("ArmorHeader", _panelRect, "\u88c5\u5907", 15, TextAnchor.MiddleLeft);
        _settingsHeaderText = CreateText("SettingsHeader", _panelRect, "\u53c2\u6570", 15, TextAnchor.MiddleLeft);

        foreach (var definition in ArmorHudCatalog.All)
        {
            _armorOptions.Add(CreateArmorOption(definition.Kind));
        }

        _rows.Add(CreateRow("enabled", "\u542f\u7528", 1.0, true));
        _rows.Add(CreateRow("text", "\u6587\u5b57", 1.0, true));
        _rows.Add(CreateRow("x", "X", 10.0, false));
        _rows.Add(CreateRow("y", "Y", 10.0, false));
        _rows.Add(CreateRow("width", "\u5bbd\u5ea6", 10.0, false));
        _rows.Add(CreateRow("height", "\u9ad8\u5ea6", 2.0, false));
        _rows.Add(CreateRow("opacity", "\u900f\u660e\u5ea6", 0.05, false));
        _rows.Add(CreateRow("countdown", "\u5012\u8ba1\u65f6", 1.0, true));

        var reset = CreateBox("Reset", _panelRect, new Color(0.22f, 0.13f, 0.08f, 0.95f));
        _resetRect = reset.Rect;
        _resetText = CreateText("ResetText", _resetRect, "\u6062\u590d\u9ed8\u8ba4", 14, TextAnchor.MiddleCenter);

        var close = CreateBox("Close", _panelRect, new Color(0.12f, 0.11f, 0.15f, 0.95f));
        _closeRect = close.Rect;
        _closeText = CreateText("CloseText", _closeRect, "\u5173\u95ed", 14, TextAnchor.MiddleCenter);

        _rootObject.SetActive(false);
    }

    private ArmorOption CreateArmorOption(ArmorHudKind kind)
    {
        var background = CreateBox($"{kind}_option", _panelRect, new Color(0.09f, 0.065f, 0.13f, 0.70f));
        var text = CreateText($"{kind}_option_text", background.Rect, ArmorHudCatalog.Get(kind).ConfigLabel, 14, TextAnchor.MiddleLeft);
        return new ArmorOption(kind, background.Rect, background.Image, text);
    }

    private ConfigRow CreateRow(string key, string label, double step, bool isBoolean)
    {
        var background = CreateBox($"{key}_row", _panelRect, new Color(0.08f, 0.06f, 0.11f, 0.72f));
        var labelText = CreateText($"{key}_label", background.Rect, label, 14, TextAnchor.MiddleLeft);
        var valueText = CreateText($"{key}_value", background.Rect, string.Empty, 14, TextAnchor.MiddleCenter);
        var minus = CreateBox($"{key}_minus", background.Rect, new Color(0.16f, 0.10f, 0.20f, 0.95f));
        var plus = CreateBox($"{key}_plus", background.Rect, new Color(0.18f, 0.10f, 0.24f, 0.95f));
        var minusText = CreateText($"{key}_minus_text", minus.Rect, isBoolean ? "\u5173" : "-", 14, TextAnchor.MiddleCenter);
        var plusText = CreateText($"{key}_plus_text", plus.Rect, isBoolean ? "\u5f00" : "+", 14, TextAnchor.MiddleCenter);

        return new ConfigRow(
            key,
            label,
            step,
            isBoolean,
            background.Rect,
            background.Image,
            labelText,
            valueText,
            minus.Rect,
            plus.Rect,
            minus.Image,
            plus.Image,
            minusText,
            plusText);
    }

    private void UpdateLayout()
    {
        var panelX = Mathf.Max(18.0f, Screen.width - PanelWidth - PanelMargin);
        var panelY = PanelTop;
        SetTopLeftRect(_panelRect, panelX, panelY, PanelWidth, PanelHeight);
        SetTopLeftRect(_titleText.rectTransform, 20.0f, 12.0f, 580.0f, 34.0f);
        SetTopLeftRect(_armorHeaderText.rectTransform, ArmorListX + 2.0f, 58.0f, ArmorListWidth, 24.0f);
        SetTopLeftRect(_settingsHeaderText.rectTransform, SettingsX + 2.0f, 58.0f, SettingsWidth, 24.0f);
        SetTopLeftRect(_statusText.rectTransform, 20.0f, 426.0f, 580.0f, 24.0f);
        SetTopLeftRect(_resetRect, SettingsX, 464.0f, 128.0f, 34.0f);
        SetTopLeftRect(_closeRect, 488.0f, 464.0f, 128.0f, 34.0f);
        SetTopLeftRect(_resetText.rectTransform, 0.0f, 0.0f, 128.0f, 34.0f);
        SetTopLeftRect(_closeText.rectTransform, 0.0f, 0.0f, 128.0f, 34.0f);

        _clickTargets.Clear();
        for (var index = 0; index < _armorOptions.Count; index++)
        {
            var option = _armorOptions[index];
            var optionY = 92.0f + index * 44.0f;
            var selected = option.Kind == _selectedKind;
            SetTopLeftRect(option.RootRect, ArmorListX, optionY, ArmorListWidth, 36.0f);
            SetTopLeftRect(option.Text.rectTransform, 12.0f, 0.0f, ArmorListWidth - 20.0f, 36.0f);
            option.RootImage.color = selected
                ? new Color(0.24f, 0.13f, 0.32f, 0.92f)
                : new Color(0.09f, 0.065f, 0.13f, 0.70f);
            option.Text.color = selected
                ? new Color(1.0f, 0.82f, 0.32f, 1.0f)
                : new Color(0.92f, 0.88f, 1.0f, 0.96f);

            var optionScreenRect = new Rect(panelX + ArmorListX, panelY + optionY, ArmorListWidth, 36.0f);
            _clickTargets.Add(new ClickTarget(optionScreenRect, () => SelectArmor(option.Kind)));
        }

        var visibleRowIndex = 0;
        for (var index = 0; index < _rows.Count; index++)
        {
            var row = _rows[index];
            var rowVisible = IsRowVisible(row);
            row.RootRect.gameObject.SetActive(rowVisible);
            if (!rowVisible)
            {
                continue;
            }

            var rowY = 92.0f + visibleRowIndex * 42.0f;
            var selected = visibleRowIndex == _selectedIndex;
            SetTopLeftRect(row.RootRect, SettingsX, rowY, SettingsWidth, 36.0f);
            SetTopLeftRect(row.LabelText.rectTransform, 14.0f, 0.0f, 96.0f, 36.0f);
            SetTopLeftRect(row.ValueText.rectTransform, 118.0f, 0.0f, 104.0f, 36.0f);
            SetTopLeftRect(row.MinusRect, 244.0f, 5.0f, 50.0f, 26.0f);
            SetTopLeftRect(row.PlusRect, 318.0f, 5.0f, 50.0f, 26.0f);
            SetTopLeftRect(row.MinusText.rectTransform, 0.0f, 0.0f, 50.0f, 26.0f);
            SetTopLeftRect(row.PlusText.rectTransform, 0.0f, 0.0f, 50.0f, 26.0f);

            row.RootImage.color = selected
                ? new Color(0.18f, 0.10f, 0.25f, 0.86f)
                : new Color(0.08f, 0.06f, 0.11f, 0.72f);
            row.MinusImage.color = new Color(0.16f, 0.10f, 0.20f, 0.95f);
            row.PlusImage.color = new Color(0.18f, 0.10f, 0.24f, 0.95f);

            var minusScreenRect = new Rect(panelX + SettingsX + 244.0f, panelY + rowY + 5.0f, 50.0f, 26.0f);
            var plusScreenRect = new Rect(panelX + SettingsX + 318.0f, panelY + rowY + 5.0f, 50.0f, 26.0f);
            _clickTargets.Add(new ClickTarget(minusScreenRect, () => AdjustRow(row, -1.0)));
            _clickTargets.Add(new ClickTarget(plusScreenRect, () => AdjustRow(row, 1.0)));
            visibleRowIndex++;
        }

        _clickTargets.Add(new ClickTarget(new Rect(panelX + SettingsX, panelY + 464.0f, 128.0f, 34.0f), ResetDefaults));
        _clickTargets.Add(new ClickTarget(new Rect(panelX + 488.0f, panelY + 464.0f, 128.0f, 34.0f), Hide));

        _panelImage.color = new Color(0.025f, 0.020f, 0.040f, 0.90f);
        _titleText.text = $"\u88c5\u5907HUD\u914d\u7f6e - {GetArmorLabel(_selectedKind)}";
        _titleText.color = new Color(1.0f, 0.88f, 0.46f, 1.0f);
        _armorHeaderText.color = new Color(0.78f, 0.72f, 0.92f, 0.96f);
        _settingsHeaderText.color = new Color(0.78f, 0.72f, 0.92f, 0.96f);
        _statusText.text = _status;
        _statusText.color = new Color(0.86f, 0.82f, 0.94f, 0.96f);
        RefreshValues();
    }

    private void HandleKeyboard()
    {
        ClampSelectedIndex();
        var previousSelectedIndex = _selectedIndex;
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            _selectedIndex = Math.Max(0, _selectedIndex - 1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            _selectedIndex = Math.Min(GetVisibleRowCount() - 1, _selectedIndex + 1);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            AdjustRow(GetVisibleRowAt(_selectedIndex), -1.0);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            AdjustRow(GetVisibleRowAt(_selectedIndex), 1.0);
        }
        else if (Input.GetKeyDown(KeyCode.Tab))
        {
            SelectArmor(ArmorHudCatalog.Next(_selectedKind));
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            ResetDefaults();
        }

        if (_selectedIndex != previousSelectedIndex)
        {
            _layoutDirty = true;
        }
    }

    private void HandleMouse()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        var mouse = Input.mousePosition;
        var point = new Vector2(mouse.x, Screen.height - mouse.y);
        for (var index = _clickTargets.Count - 1; index >= 0; index--)
        {
            var target = _clickTargets[index];
            if (!target.Rect.Contains(point))
            {
                continue;
            }

            target.Click();
            break;
        }
    }

    private void SelectArmor(ArmorHudKind kind)
    {
        _selectedKind = kind;
        ClampSelectedIndex();
        _status = $"\u6b63\u5728\u8c03\u6574 {GetArmorLabel(_selectedKind)}";
        _layoutDirty = true;
        RefreshValues();
    }

    private void AdjustRow(ConfigRow row, double direction)
    {
        _selectedIndex = GetVisibleRowIndex(row);
        var settings = _settingsStore.GetArmorSettings(_selectedKind).Clone();
        if (row.IsBoolean)
        {
            if (string.Equals(row.Key, "enabled", StringComparison.Ordinal))
            {
                settings.Enabled = direction > 0;
            }
            else if (string.Equals(row.Key, "text", StringComparison.Ordinal))
            {
                settings.ShowText = direction > 0;
            }
            else if (string.Equals(row.Key, "countdown", StringComparison.Ordinal))
            {
                settings.ShowCountdown = direction > 0;
            }
        }
        else
        {
            var delta = row.Step * direction;
            switch (row.Key)
            {
                case "x":
                    settings.X = ProbeUtilities.ClampDemonShellHudConfigValue(row.Key, settings.X + delta);
                    break;
                case "y":
                    settings.Y = ProbeUtilities.ClampDemonShellHudConfigValue(row.Key, settings.Y + delta);
                    break;
                case "width":
                    settings.Width = ProbeUtilities.ClampDemonShellHudConfigValue(row.Key, settings.Width + delta);
                    break;
                case "height":
                    settings.Height = ProbeUtilities.ClampDemonShellHudConfigValue(row.Key, settings.Height + delta);
                    break;
                case "opacity":
                    settings.Opacity = ProbeUtilities.ClampDemonShellHudConfigValue(row.Key, settings.Opacity + delta);
                    break;
            }
        }

        _settingsStore.SaveArmorSettings(_selectedKind, settings);
        _status = $"\u5df2\u4fdd\u5b58 {GetArmorLabel(_selectedKind)} {row.Label} {FormatValue(_settingsStore.GetArmorSettings(_selectedKind), row.Key)}";
        _layoutDirty = true;
        RefreshValues();
    }

    private void ResetDefaults()
    {
        _settingsStore.ResetArmorSettings(_selectedKind);
        _status = $"{GetArmorLabel(_selectedKind)} \u5df2\u6062\u590d\u9ed8\u8ba4";
        _layoutDirty = true;
        RefreshValues();
    }

    private void RefreshValues()
    {
        var settings = _settingsStore.GetArmorSettings(_selectedKind);
        foreach (var row in _rows)
        {
            row.ValueText.text = FormatValue(settings, row.Key);
        }
    }

    private string FormatValue(ArmorHudSettings settings, string key)
    {
        return key switch
        {
            "x" => settings.X.ToString("0", CultureInfo.InvariantCulture),
            "y" => settings.Y.ToString("0", CultureInfo.InvariantCulture),
            "width" => settings.Width.ToString("0", CultureInfo.InvariantCulture),
            "height" => settings.Height.ToString("0", CultureInfo.InvariantCulture),
            "opacity" => $"{Math.Round(settings.Opacity * 100.0):0}%",
            "enabled" => settings.Enabled ? "\u5f00" : "\u5173",
            "text" => settings.ShowText ? "\u5f00" : "\u5173",
            "countdown" => settings.ShowCountdown ? "\u5f00" : "\u5173",
            _ => string.Empty
        };
    }

    private bool IsRowVisible(ConfigRow row)
    {
        return !string.Equals(row.Key, "countdown", StringComparison.Ordinal)
               || ArmorHudCatalog.SupportsCountdown(_selectedKind);
    }

    private int GetVisibleRowCount()
    {
        var count = 0;
        for (var index = 0; index < _rows.Count; index++)
        {
            if (IsRowVisible(_rows[index]))
            {
                count++;
            }
        }

        return count;
    }

    private ConfigRow GetVisibleRowAt(int visibleIndex)
    {
        for (var index = 0; index < _rows.Count; index++)
        {
            var row = _rows[index];
            if (!IsRowVisible(row))
            {
                continue;
            }

            if (visibleIndex == 0)
            {
                return row;
            }

            visibleIndex--;
        }

        return _rows[0];
    }

    private int GetVisibleRowIndex(ConfigRow target)
    {
        var visibleIndex = 0;
        for (var index = 0; index < _rows.Count; index++)
        {
            var row = _rows[index];
            if (!IsRowVisible(row))
            {
                continue;
            }

            if (ReferenceEquals(row, target))
            {
                return visibleIndex;
            }

            visibleIndex++;
        }

        return 0;
    }

    private void ClampSelectedIndex()
    {
        var visibleRowCount = GetVisibleRowCount();
        _selectedIndex = visibleRowCount > 0
            ? Math.Min(_selectedIndex, visibleRowCount - 1)
            : 0;
    }

    private static string GetArmorLabel(ArmorHudKind kind)
    {
        return ArmorHudCatalog.Get(kind).ConfigLabel;
    }

    private void LoadFont()
    {
        try
        {
            _font = Font.CreateDynamicFontFromOSFont(
                new[] { "Source Han Sans CN", "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial" },
                18);
            if (_font is not null)
            {
                return;
            }
        }
        catch
        {
            _font = null;
        }

        try
        {
            _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        catch
        {
            _font = null;
        }
    }

    private UiBox CreateBox(string name, Transform parent, Color color)
    {
        var box = new GameObject(
            name,
            Il2CppType.Of<RectTransform>(),
            Il2CppType.Of<Image>());
        box.transform.SetParent(parent, false);
        var rect = box.GetComponent<RectTransform>();
        var image = box.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = color;
        return new UiBox(rect, image);
    }

    private Text CreateText(string name, Transform parent, string textValue, int fontSize, TextAnchor alignment)
    {
        var textObject = new GameObject(
            name,
            Il2CppType.Of<RectTransform>(),
            Il2CppType.Of<Text>());
        textObject.transform.SetParent(parent, false);
        var text = textObject.GetComponent<Text>();
        text.text = textValue;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.fontStyle = FontStyle.Bold;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        if (_font is not null)
        {
            text.font = _font;
        }

        text.color = new Color(0.96f, 0.92f, 1.0f, 1.0f);
        return text;
    }

    private static void SetTopLeftRect(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0.0f, 1.0f);
        rect.anchorMax = new Vector2(0.0f, 1.0f);
        rect.pivot = new Vector2(0.0f, 1.0f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private sealed class ArmorOption
    {
        internal ArmorOption(ArmorHudKind kind, RectTransform rootRect, Image rootImage, Text text)
        {
            Kind = kind;
            RootRect = rootRect;
            RootImage = rootImage;
            Text = text;
        }

        internal ArmorHudKind Kind { get; }

        internal RectTransform RootRect { get; }

        internal Image RootImage { get; }

        internal Text Text { get; }
    }

    private sealed class ConfigRow
    {
        internal ConfigRow(
            string key,
            string label,
            double step,
            bool isBoolean,
            RectTransform rootRect,
            Image rootImage,
            Text labelText,
            Text valueText,
            RectTransform minusRect,
            RectTransform plusRect,
            Image minusImage,
            Image plusImage,
            Text minusText,
            Text plusText)
        {
            Key = key;
            Label = label;
            Step = step;
            IsBoolean = isBoolean;
            RootRect = rootRect;
            RootImage = rootImage;
            LabelText = labelText;
            ValueText = valueText;
            MinusRect = minusRect;
            PlusRect = plusRect;
            MinusImage = minusImage;
            PlusImage = plusImage;
            MinusText = minusText;
            PlusText = plusText;
        }

        internal string Key { get; }

        internal string Label { get; }

        internal double Step { get; }

        internal bool IsBoolean { get; }

        internal RectTransform RootRect { get; }

        internal Image RootImage { get; }

        internal Text LabelText { get; }

        internal Text ValueText { get; }

        internal RectTransform MinusRect { get; }

        internal RectTransform PlusRect { get; }

        internal Image MinusImage { get; }

        internal Image PlusImage { get; }

        internal Text MinusText { get; }

        internal Text PlusText { get; }
    }

    private readonly struct UiBox
    {
        internal UiBox(RectTransform rect, Image image)
        {
            Rect = rect;
            Image = image;
        }

        internal RectTransform Rect { get; }

        internal Image Image { get; }
    }

    private readonly struct ClickTarget
    {
        internal ClickTarget(Rect rect, Action click)
        {
            Rect = rect;
            Click = click;
        }

        internal Rect Rect { get; }

        internal Action Click { get; }
    }
}
