using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace LC2RuntimeProbe;

internal sealed class DemonShellHudRenderer
{
    private readonly string _rootName;
    private GameObject _rootObject;
    private RectTransform _panelRect;
    private RectTransform _fillRect;
    private RectTransform _shineRect;
    private RectTransform _countdownRect;
    private RectTransform _labelRect;
    private Image _backgroundImage;
    private Image _fillImage;
    private Image _shineImage;
    private Image _countdownImage;
    private Outline _outline;
    private Text _labelText;
    private bool _labelFontAvailable;
    private ArmorHudRenderState? _lastRenderState;

    internal DemonShellHudRenderer(string rootName)
    {
        _rootName = rootName;
    }

    internal void Draw(ArmorHudSettings settings, DemonShellHudProgress progress, string label = "", string suffix = "")
    {
        Draw(settings, progress, ArmorHudCountdownSnapshot.Hidden, label, suffix);
    }

    internal void Draw(
        ArmorHudSettings settings,
        DemonShellHudProgress progress,
        ArmorHudCountdownSnapshot countdown,
        string label = "",
        string suffix = "")
    {
        if (!settings.Enabled)
        {
            Hide();
            return;
        }

        EnsureHud();
        var renderState = ProbeUtilities.CreateArmorHudRenderState(
            settings,
            progress,
            label,
            suffix,
            countdown,
            Screen.width,
            Screen.height);
        if (_rootObject.activeSelf && !ProbeUtilities.HasArmorHudRenderStateChanged(_lastRenderState, renderState))
        {
            return;
        }

        UpdateLayout(settings, progress, countdown, label, suffix);
        _lastRenderState = renderState;
        if (!_rootObject.activeSelf)
        {
            _rootObject.SetActive(true);
        }
    }

    internal void Hide()
    {
        if (_rootObject is not null && _rootObject.activeSelf)
        {
            _rootObject.SetActive(false);
        }

        _lastRenderState = null;
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
        _fillRect = null;
        _shineRect = null;
        _countdownRect = null;
        _labelRect = null;
        _backgroundImage = null;
        _fillImage = null;
        _shineImage = null;
        _countdownImage = null;
        _outline = null;
        _labelText = null;
        _labelFontAvailable = false;
        _lastRenderState = null;
    }

    private void EnsureHud()
    {
        if (_rootObject is not null)
        {
            return;
        }

        _rootObject = new GameObject(
            _rootName,
            Il2CppType.Of<RectTransform>(),
            Il2CppType.Of<Canvas>(),
            Il2CppType.Of<CanvasScaler>());
        UnityEngine.Object.DontDestroyOnLoad(_rootObject);

        var canvas = _rootObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32767;

        var scaler = _rootObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1.0f;
        scaler.referencePixelsPerUnit = 100.0f;

        var panel = new GameObject(
            "Panel",
            Il2CppType.Of<RectTransform>(),
            Il2CppType.Of<Image>(),
            Il2CppType.Of<Outline>());
        panel.transform.SetParent(_rootObject.transform, false);
        _panelRect = panel.GetComponent<RectTransform>();
        _backgroundImage = panel.GetComponent<Image>();
        _backgroundImage.raycastTarget = false;
        _outline = panel.GetComponent<Outline>();
        _outline.effectDistance = new Vector2(2.0f, -2.0f);

        var fill = new GameObject(
            "Fill",
            Il2CppType.Of<RectTransform>(),
            Il2CppType.Of<Image>());
        fill.transform.SetParent(panel.transform, false);
        _fillRect = fill.GetComponent<RectTransform>();
        _fillImage = fill.GetComponent<Image>();
        _fillImage.raycastTarget = false;

        var shine = new GameObject(
            "Shine",
            Il2CppType.Of<RectTransform>(),
            Il2CppType.Of<Image>());
        shine.transform.SetParent(fill.transform, false);
        _shineRect = shine.GetComponent<RectTransform>();
        _shineImage = shine.GetComponent<Image>();
        _shineImage.raycastTarget = false;

        var countdown = new GameObject(
            "Countdown",
            Il2CppType.Of<RectTransform>(),
            Il2CppType.Of<Image>());
        countdown.transform.SetParent(panel.transform, false);
        _countdownRect = countdown.GetComponent<RectTransform>();
        _countdownImage = countdown.GetComponent<Image>();
        _countdownImage.raycastTarget = false;

        var label = new GameObject(
            "Label",
            Il2CppType.Of<RectTransform>(),
            Il2CppType.Of<Text>());
        label.transform.SetParent(panel.transform, false);
        _labelRect = label.GetComponent<RectTransform>();
        _labelText = label.GetComponent<Text>();
        _labelText.alignment = TextAnchor.MiddleCenter;
        _labelText.fontStyle = FontStyle.Bold;
        _labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _labelText.verticalOverflow = VerticalWrapMode.Truncate;
        _labelText.raycastTarget = false;
        try
        {
            _labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _labelFontAvailable = _labelText.font is not null;
        }
        catch
        {
            _labelFontAvailable = false;
        }
    }

    private void UpdateLayout(
        ArmorHudSettings settings,
        DemonShellHudProgress progress,
        ArmorHudCountdownSnapshot countdown,
        string label,
        string suffix)
    {
        var hudRect = settings.ResolveRect(Screen.width, Screen.height);
        var x = (float)hudRect.X;
        var y = (float)hudRect.Y;
        var width = (float)hudRect.Width;
        var height = (float)hudRect.Height;
        var opacity = (float)settings.Opacity;
        var innerWidth = Mathf.Max(0.0f, width - 6.0f);
        var innerHeight = Mathf.Max(0.0f, height - 6.0f);
        var fillWidth = innerWidth * Mathf.Clamp01((float)progress.FillRatio);
        var countdownWidth = innerWidth * Mathf.Clamp01((float)countdown.FillRatio);
        var countdownY = 3.0f + Mathf.Max(0.0f, innerHeight - 3.0f);
        var isNearFull = progress.FillRatio >= 0.9;

        SetTopLeftRect(_panelRect, x, y, width, height);
        SetTopLeftRect(_fillRect, 3.0f, 3.0f, fillWidth, innerHeight);
        SetTopLeftRect(_shineRect, 0.0f, 0.0f, fillWidth, Mathf.Max(1.0f, innerHeight * 0.24f));
        SetTopLeftRect(_countdownRect, 3.0f, countdownY, countdownWidth, 3.0f);
        SetTopLeftRect(_labelRect, 0.0f, 0.0f, width, height);

        _backgroundImage.color = new Color(0.05f, 0.025f, 0.075f, 0.72f * opacity);
        _outline.effectColor = new Color(0.95f, 0.66f, 0.22f, 0.85f * opacity);
        var fillColor = isNearFull
            ? new Color(1.0f, 0.62f, 0.16f, 0.92f * opacity)
            : new Color(0.72f, 0.16f, 0.96f, 0.90f * opacity);
        _fillImage.color = fillColor;
        _shineImage.color = new Color(1.0f, 0.9f, 1.0f, 0.22f * opacity);
        _countdownImage.enabled = settings.ShowCountdown && countdown.IsVisible;
        _countdownImage.color = new Color(fillColor.r, fillColor.g, fillColor.b, opacity);

        _labelText.enabled = settings.ShowText && _labelFontAvailable;
        if (_labelText.enabled)
        {
            _labelText.fontSize = Mathf.Clamp(Mathf.RoundToInt(height * 0.52f), 10, 28);
            var progressText = ProbeUtilities.FormatArmorHudLabel(label, progress, suffix);
            var countdownText = settings.ShowCountdown
                ? ProbeUtilities.FormatArmorHudCountdownText(countdown)
                : string.Empty;
            _labelText.text = string.IsNullOrEmpty(countdownText)
                ? progressText
                : $"{progressText} {countdownText}";
            _labelText.color = new Color(1.0f, 0.94f, 0.76f, opacity);
        }
    }

    private static void SetTopLeftRect(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0.0f, 1.0f);
        rect.anchorMax = new Vector2(0.0f, 1.0f);
        rect.pivot = new Vector2(0.0f, 1.0f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }
}
