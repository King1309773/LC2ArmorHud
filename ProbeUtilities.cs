#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace LC2RuntimeProbe;

internal static class ProbeUtilities
{
    private static readonly string[] CandidateMemberTokens =
    {
        "id", "name", "title", "display", "desc", "description", "icon", "sprite",
        "text", "local", "key", "gem", "soul", "stone", "item", "prop", "reward",
        "drop", "buff", "skill", "power", "rune"
    };

    internal static readonly string[] Keywords =
    {
        "Gem", "Soul", "Stone", "Item", "Prop", "Buff", "Skill", "Inventory", "Bag",
        "Drop", "Reward", "Treasure", "Chest", "Shop", "Equip", "Power", "Rune",
        "Localization", "Localize", "Text", "Desc", "Icon", "Sprite",
        "灵魂石", "魂石", "宝石", "道具", "背包", "掉落", "奖励", "图鉴", "商店",
        "技能", "增益", "描述", "名称", "图标"
    };

    private static readonly HashSet<string> SummaryTriggerKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Gem", "Soul", "Stone", "Item", "Drop", "Reward", "Treasure", "Chest", "Shop",
        "Equip", "Power", "Rune", "Localization", "Localize", "Text", "Desc", "Icon", "Sprite",
        "\u7075\u9b42\u77f3", "\u9b42\u77f3", "\u5b9d\u77f3", "\u9053\u5177",
        "\u6389\u843d", "\u5956\u52b1", "\u56fe\u9274", "\u5546\u5e97",
        "\u63cf\u8ff0", "\u540d\u79f0", "\u56fe\u6807"
    };

    private static readonly HashSet<string> ExcludedFocusedMembers = new(StringComparer.OrdinalIgnoreCase)
    {
        "gameObject", "transform", "tag", "hideFlags"
    };

    private static readonly string[] GemDataSourceMemberTokens =
    {
        "id", "key", "name", "title", "display", "desc", "description", "icon", "sprite", "text",
        "gem", "soul", "stone", "skill", "power", "rune", "data", "list"
    };

    private static readonly string[] DemonShellProbeMemberTokens =
    {
        "id", "key", "name", "item", "equip", "equipment", "armor", "buff", "skill",
        "counter", "count", "stack", "layer", "charge", "energy", "magic", "mana",
        "value", "data", "dic", "dict", "dictionary", "map", "list", "current", "player"
    };

    private static readonly string[] DemonShellProbeTargets =
    {
        "Equipment#NA_036", "NA_036", "NA5-002", "恶魔君王甲壳"
    };

    private static readonly string[] GenericFocusedMemberTokens =
    {
        "id", "key", "name", "title", "display", "desc", "description", "icon", "sprite", "text",
        "gem", "soul", "stone", "item", "reward", "drop", "skill", "power", "rune"
    };

    internal static string EscapeCsv(string value)
    {
        value ??= string.Empty;
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\r') && !value.Contains('\n'))
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    internal static List<string> FindMatchedKeywords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        return Keywords
            .Where(keyword => ContainsKeyword(text, keyword))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal static List<string> FindCandidateKeywords(string objectName, string componentTypeName, string rawSummary, bool isTransform)
    {
        var keywords = FindMatchedKeywords(objectName)
            .Concat(FindMatchedKeywords(componentTypeName));

        if (!isTransform)
        {
            keywords = keywords.Concat(FindMatchedKeywords(rawSummary).Where(SummaryTriggerKeywords.Contains));
        }

        return keywords.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    internal static string CalculateConfidence(int evidenceCount)
    {
        return evidenceCount >= 4 ? "high" : evidenceCount >= 2 ? "medium" : "low";
    }

    internal static string BuildCandidateMatchText(string objectName, string componentTypeName, string rawSummary)
    {
        return string.Join(" ", new[] { objectName, componentTypeName, rawSummary }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    internal static bool IsCandidateMemberName(string memberName)
    {
        if (string.IsNullOrWhiteSpace(memberName) || string.Equals(memberName, "hideFlags", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var normalized = memberName.ToLowerInvariant();
        return CandidateMemberTokens.Any(normalized.Contains);
    }

    internal static bool IsPossibleIdMemberName(string memberName)
    {
        if (string.IsNullOrWhiteSpace(memberName) || string.Equals(memberName, "hideFlags", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var normalized = memberName.ToLowerInvariant();
        return normalized == "id"
            || normalized == "key"
            || normalized.EndsWith("id", StringComparison.Ordinal)
            || normalized.EndsWith("key", StringComparison.Ordinal);
    }

    internal static bool IsFocusedMemberName(string memberName)
    {
        return !string.IsNullOrWhiteSpace(memberName) && !ExcludedFocusedMembers.Contains(memberName);
    }

    internal static int GetFocusedComponentPriority(string typeName)
    {
        if (IsLc2TypeName(typeName))
        {
            return 0;
        }

        if (IsTmProTypeName(typeName))
        {
            return 1;
        }

        if (IsNoisyUnityTypeName(typeName))
        {
            return 3;
        }

        return 2;
    }

    internal static bool ShouldInspectFocusedMember(string typeName, string memberName)
    {
        if (!IsFocusedMemberName(memberName))
        {
            return false;
        }

        if (IsLc2TypeName(typeName))
        {
            return true;
        }

        var normalized = memberName.ToLowerInvariant();
        return GenericFocusedMemberTokens.Any(normalized.Contains);
    }

    internal static bool ShouldExpandFocusedReference(string typeName)
    {
        return IsLc2TypeName(typeName) || IsTmProTypeName(typeName);
    }

    internal static bool ShouldTryDirectedPlayerBagLookup(string objectName)
    {
        return string.Equals(objectName, "PlayerBagUI", StringComparison.Ordinal);
    }

    internal static bool ShouldTryDirectedInscriptionGemListLookup(string objectName)
    {
        return string.Equals(objectName, "InscriptionGemList", StringComparison.Ordinal);
    }

    internal static bool IsGemPanelAnchorName(string objectName)
    {
        return string.Equals(objectName, "InscriptionUI", StringComparison.OrdinalIgnoreCase)
            || string.Equals(objectName, "InscriptionGemList", StringComparison.OrdinalIgnoreCase)
            || string.Equals(objectName, "GemSkillPointView", StringComparison.OrdinalIgnoreCase)
            || string.Equals(objectName, "gemLibrary", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool ShouldAppendGemUiCandidate(string objectName, string displayText, string iconName)
    {
        return FindMatchedKeywords(objectName).Any(keyword =>
                   string.Equals(keyword, "Gem", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(keyword, "Soul", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(keyword, "Stone", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(keyword, "Skill", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(keyword, "Icon", StringComparison.OrdinalIgnoreCase))
            || !string.IsNullOrWhiteSpace(displayText)
            || !string.IsNullOrWhiteSpace(iconName);
    }

    internal static bool IsGemLibraryNodeName(string objectName)
    {
        return string.Equals(objectName, "gemLibrary", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsGemSlotNodeName(string objectName)
    {
        return !string.IsNullOrWhiteSpace(objectName)
            && objectName.StartsWith("Unit_", StringComparison.OrdinalIgnoreCase);
    }

    internal static string StripTmProColorTags(string value)
    {
        return Regex.Replace(value ?? string.Empty, "</?color(?:=[^>]+)?>", string.Empty,
            RegexOptions.IgnoreCase);
    }

    internal static string NormalizeGemText(string value)
    {
        var withoutColor = StripTmProColorTags(value);
        var withoutPositionTags = Regex.Replace(withoutColor, "<pos=[^>]+>", " ",
            RegexOptions.IgnoreCase);
        return Regex.Replace(withoutPositionTags, @"\s+", " ").Trim();
    }

    internal static string CreateGemDefinitionFingerprint(string iconName, string gemText)
    {
        var normalized = $"{(iconName ?? string.Empty).Trim()}|{NormalizeGemText(gemText)}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    internal static bool ShouldInspectGemDataSourceMember(string memberName)
    {
        if (string.IsNullOrWhiteSpace(memberName) || ExcludedFocusedMembers.Contains(memberName))
        {
            return false;
        }

        var normalized = memberName.ToLowerInvariant();
        return GemDataSourceMemberTokens.Any(normalized.Contains);
    }

    internal static bool ShouldInspectDemonShellProbeMember(string memberName)
    {
        if (string.IsNullOrWhiteSpace(memberName) || ExcludedFocusedMembers.Contains(memberName))
        {
            return false;
        }

        var normalized = memberName.ToLowerInvariant();
        return DemonShellProbeMemberTokens.Any(normalized.Contains);
    }

    internal static string GetDemonShellProgressProbeHotkeyLabel()
    {
        return "F8";
    }

    internal static string GetKeywordCandidateProbeHotkeyLabel()
    {
        return "F12";
    }

    internal static string GetDemonShellHudConfigPanelHotkeyLabel()
    {
        return "F5";
    }

    internal static string GetDemonShellHudRendererBackendLabel()
    {
        return "UnityUI";
    }

    internal static string GetDemonShellHudConfigLabel()
    {
        return ArmorHudCatalog.Get(ArmorHudKind.DemonShell).ConfigLabel;
    }

    internal static string GetBeastArmorHudConfigLabel()
    {
        return ArmorHudCatalog.Get(ArmorHudKind.BeastArmor).ConfigLabel;
    }

    internal static string GetBeastArmorCritHudLabel()
    {
        return "\u66b4\u51fb\u7387";
    }

    internal static string GetBeastArmorHealthHudLabel()
    {
        return "\u751f\u547d\u503c";
    }

    internal static string GetDemonSlayerHeavyArmorHudConfigLabel()
    {
        return ArmorHudCatalog.Get(ArmorHudKind.DemonSlayerHeavyArmor).ConfigLabel;
    }

    internal static string GetBlackMageRobeHudConfigLabel()
    {
        return ArmorHudCatalog.Get(ArmorHudKind.BlackMageRobe).ConfigLabel;
    }

    internal static string GetGuardianWhiteRobeHudConfigLabel()
    {
        return ArmorHudCatalog.Get(ArmorHudKind.GuardianWhiteRobe).ConfigLabel;
    }

    internal static string GetRedstoneMageRobeHudConfigLabel()
    {
        return ArmorHudCatalog.Get(ArmorHudKind.RedstoneMageRobe).ConfigLabel;
    }

    internal static string GetMagicPowerHudLabel()
    {
        return "\u6280\u80fd\u5f3a\u5ea6";
    }

    internal static string GetAttackHudLabel()
    {
        return "\u653b\u51fb\u529b";
    }

    internal static bool ShouldInspectDemonShellDirectedMember(
        string sourceTypeName,
        string memberName,
        string valueTypeName)
    {
        sourceTypeName ??= string.Empty;
        memberName ??= string.Empty;
        valueTypeName ??= string.Empty;

        if (string.IsNullOrWhiteSpace(memberName) || ExcludedFocusedMembers.Contains(memberName))
        {
            return false;
        }

        if (IsDemonShellPowerExecutorTypeName(sourceTypeName))
        {
            return IsPowerCounterDictionaryMember(memberName, valueTypeName);
        }

        var normalizedMember = memberName.ToLowerInvariant();
        if (DemonShellProbeMemberTokens.Any(normalizedMember.Contains))
        {
            return true;
        }

        if (IsDemonShellArmorTypeName(sourceTypeName)
            && (IsSimpleProbeValueTypeName(valueTypeName) || IsUsefulProbeContainerTypeName(valueTypeName)))
        {
            return true;
        }

        return IsRelevantDemonShellSourceTypeName(sourceTypeName) && IsUsefulProbeContainerTypeName(valueTypeName);
    }

    internal static bool ShouldTryDemonShellLocalPlayerLookup(string objectName, string hierarchyPath)
    {
        return string.Equals(objectName, "LocalPlayer", StringComparison.OrdinalIgnoreCase)
               || EndsWithPathSegment(hierarchyPath ?? string.Empty, "LocalPlayer");
    }

    internal static IReadOnlyList<string> GetDemonShellHudLocalPlayerSearchPaths()
    {
        return new[] { "GlobalMgr/PlayerRoot/LocalPlayer", "LocalPlayer" };
    }

    internal static bool IsDemonShellCounterKey(string key)
    {
        return string.Equals(key, "Equipment#NA_036", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsArmorHudCounterKey(string key)
    {
        return ArmorHudCatalog.IsCounterKey(key);
    }

    internal static float GetArmorHudEquipmentRefreshIntervalSeconds()
    {
        return 1.0f;
    }

    internal static bool ShouldRefreshArmorHudEquipmentCache(float unscaledTime, float nextRefreshAt)
    {
        if (float.IsNaN(unscaledTime) || float.IsInfinity(unscaledTime))
        {
            return true;
        }

        if (float.IsNaN(nextRefreshAt) || float.IsNegativeInfinity(nextRefreshAt))
        {
            return true;
        }

        return unscaledTime >= nextRefreshAt;
    }

    internal static bool ShouldRefreshArmorHudConfigPanelLayout(
        bool isDirty,
        int screenWidth,
        int screenHeight,
        int previousScreenWidth,
        int previousScreenHeight)
    {
        return isDirty || screenWidth != previousScreenWidth || screenHeight != previousScreenHeight;
    }

    internal static bool TryBuildDemonShellHudProgress(double rawValue, out DemonShellHudProgress progress)
    {
        return TryBuildArmorHudProgress(rawValue, 60, out progress);
    }

    internal static bool TryBuildArmorHudProgress(double rawValue, int maxValue, out DemonShellHudProgress progress)
    {
        return TryBuildArmorHudProgress(rawValue, maxValue, 1, out progress);
    }

    internal static bool TryBuildArmorHudProgress(
        double rawValue,
        int maxValue,
        int displayValuePerStack,
        out DemonShellHudProgress progress)
    {
        var safeMaxValue = Math.Max(1, maxValue);
        var safeDisplayValuePerStack = Math.Max(1, displayValuePerStack);
        var safeDisplayMaxValue = safeMaxValue * safeDisplayValuePerStack;
        if (double.IsNaN(rawValue) || double.IsInfinity(rawValue))
        {
            progress = new DemonShellHudProgress(0, safeDisplayMaxValue, 0);
            return false;
        }

        var clamped = Math.Clamp(rawValue, 0, safeMaxValue);
        var displayStack = (int)Math.Floor(clamped);
        progress = new DemonShellHudProgress(
            displayStack * safeDisplayValuePerStack,
            safeDisplayMaxValue,
            clamped / safeMaxValue);
        return true;
    }

    internal static string FormatDemonShellHudLabel(DemonShellHudProgress progress)
    {
        return FormatArmorHudLabel(string.Empty, progress);
    }

    internal static string FormatArmorHudLabel(string label, DemonShellHudProgress progress, string suffix = "")
    {
        var progressText = $"{progress.DisplayValue}/{progress.MaxValue}{suffix ?? string.Empty}";
        return string.IsNullOrWhiteSpace(label) ? progressText : $"{label.Trim()} {progressText}";
    }

    internal static string FormatArmorHudCountdownText(ArmorHudCountdownSnapshot countdown)
    {
        if (!countdown.IsVisible
            || float.IsNaN(countdown.RemainingSeconds)
            || float.IsInfinity(countdown.RemainingSeconds)
            || countdown.RemainingSeconds <= 0)
        {
            return string.Empty;
        }

        return $"\u6700\u65e9 {countdown.RemainingSeconds:0.0}\u79d2";
    }

    internal static ArmorHudRenderState CreateArmorHudRenderState(
        ArmorHudSettings settings,
        DemonShellHudProgress progress,
        string label,
        string suffix,
        int screenWidth,
        int screenHeight)
    {
        return CreateArmorHudRenderState(
            settings,
            progress,
            label,
            suffix,
            ArmorHudCountdownSnapshot.Hidden,
            screenWidth,
            screenHeight);
    }

    internal static ArmorHudRenderState CreateArmorHudRenderState(
        ArmorHudSettings settings,
        DemonShellHudProgress progress,
        string label,
        string suffix,
        ArmorHudCountdownSnapshot countdown,
        int screenWidth,
        int screenHeight)
    {
        var safeSettings = (settings ?? ArmorHudSettings.CreateDefault(ArmorHudKind.DemonShell)).Clone();
        var rect = safeSettings.ResolveRect(screenWidth, screenHeight);
        var fillRatio = double.IsNaN(progress.FillRatio) || double.IsInfinity(progress.FillRatio)
            ? 0
            : Math.Clamp(progress.FillRatio, 0, 1);
        var countdownVisible = safeSettings.ShowCountdown && countdown.IsVisible;
        var countdownFillRatio = !countdownVisible || float.IsNaN(countdown.FillRatio) || float.IsInfinity(countdown.FillRatio)
            ? 0
            : Math.Clamp(countdown.FillRatio, 0, 1);
        var countdownText = countdownVisible ? FormatArmorHudCountdownText(countdown) : string.Empty;
        countdownVisible = !string.IsNullOrEmpty(countdownText);

        return new ArmorHudRenderState(
            safeSettings.Enabled,
            safeSettings.ShowText,
            safeSettings.ShowCountdown,
            rect.X,
            rect.Y,
            rect.Width,
            rect.Height,
            safeSettings.Opacity,
            progress.DisplayValue,
            progress.MaxValue,
            fillRatio,
            (label ?? string.Empty).Trim(),
            suffix ?? string.Empty,
            countdownVisible,
            countdownFillRatio,
            countdownText,
            screenWidth,
            screenHeight);
    }

    internal static bool HasArmorHudRenderStateChanged(ArmorHudRenderState? previous, ArmorHudRenderState current)
    {
        return !previous.HasValue || !previous.Value.Equals(current);
    }

    internal static DemonShellHudRect GetDemonShellHudDefaultConfigRect()
    {
        return ArmorHudCatalog.Get(ArmorHudKind.DemonShell).DefaultRect;
    }

    internal static DemonShellHudRect GetBeastArmorHudDefaultConfigRect()
    {
        return ArmorHudCatalog.Get(ArmorHudKind.BeastArmor).DefaultRect;
    }

    internal static double ClampDemonShellHudConfigValue(string key, double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            value = 0;
        }

        return (key ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "x" => Math.Clamp(value, 0, 3000),
            "y" => Math.Clamp(value, 0, 3000),
            "width" => Math.Clamp(value, 80, 600),
            "height" => Math.Clamp(value, 12, 80),
            "opacity" => ClampDemonShellHudOpacity(value),
            _ => value
        };
    }

    internal static double ClampDemonShellHudOpacity(double opacity)
    {
        if (double.IsNaN(opacity) || double.IsInfinity(opacity))
        {
            return 0.82;
        }

        return Math.Clamp(opacity, 0.15, 1.0);
    }

    internal static string NormalizeDemonShellHudAnchor(string anchor)
    {
        return (anchor ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "topleft" => "TopLeft",
            "topright" => "TopRight",
            "bottomright" => "BottomRight",
            "bottomleft" => "BottomLeft",
            _ => "BottomLeft"
        };
    }

    internal static DemonShellHudRect ResolveDemonShellHudRect(
        string anchor,
        double x,
        double y,
        double width,
        double height,
        double screenWidth,
        double screenHeight)
    {
        var normalizedAnchor = NormalizeDemonShellHudAnchor(anchor);
        var safeWidth = Math.Clamp(width, 80, 600);
        var safeHeight = Math.Clamp(height, 12, 80);
        var safeX = Math.Max(0, x);
        var safeY = Math.Max(0, y);
        var resolvedX = normalizedAnchor.EndsWith("Right", StringComparison.Ordinal)
            ? screenWidth - safeX - safeWidth
            : safeX;
        var resolvedY = normalizedAnchor.StartsWith("Bottom", StringComparison.Ordinal)
            ? screenHeight - safeY - safeHeight
            : safeY;

        return new DemonShellHudRect(
            Math.Clamp(resolvedX, 0, Math.Max(0, screenWidth - safeWidth)),
            Math.Clamp(resolvedY, 0, Math.Max(0, screenHeight - safeHeight)),
            safeWidth,
            safeHeight);
    }

    internal static bool ShouldExpandDemonShellProbeReference(string typeName)
    {
        return IsLc2TypeName(typeName);
    }

    internal static IReadOnlyList<string> FindDemonShellProbeMatches(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        return DemonShellProbeTargets
            .Where(target => text.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();
    }

    internal static bool TryExtractDemonShellCounterValue(string text, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var match = Regex.Match(text, @"(?:=|:)\s*([-+]?\d+(?:\.\d+)?)");
        var valueText = match.Success
            ? match.Groups[1].Value
            : Regex.IsMatch(text.Trim(), @"^[-+]?\d+(?:\.\d+)?$")
                ? text.Trim()
                : string.Empty;
        if (string.IsNullOrWhiteSpace(valueText)
            || !double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return false;
        }

        value = (int)Math.Floor(parsed);
        return true;
    }

    internal static bool ShouldDeepScanDemonShellObject(string objectName, string hierarchyPath)
    {
        var text = $"{objectName ?? string.Empty} {hierarchyPath ?? string.Empty}";
        if (FindDemonShellProbeMatches(text).Count > 0)
        {
            return true;
        }

        return text.IndexOf("GlobalMgr/PlayerRoot/LocalPlayer/BagSystem", StringComparison.OrdinalIgnoreCase) >= 0
               || string.Equals(objectName, "BagSystem", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool ShouldRecordDemonShellHierarchyChildren(string objectName, string hierarchyPath)
    {
        var text = $"{objectName ?? string.Empty} {hierarchyPath ?? string.Empty}";
        if (FindDemonShellProbeMatches(text).Count > 0)
        {
            return true;
        }

        return string.Equals(objectName, "BagSystem", StringComparison.OrdinalIgnoreCase)
               || EndsWithPathSegment(hierarchyPath ?? string.Empty, "BagSystem");
    }

    internal static bool ShouldInspectDemonShellComponentObject(
        string objectName,
        string hierarchyPath,
        string componentTypeName)
    {
        objectName ??= string.Empty;
        hierarchyPath ??= string.Empty;
        componentTypeName ??= string.Empty;

        var text = $"{objectName} {hierarchyPath} {componentTypeName}";
        if (FindDemonShellProbeMatches(text).Count > 0 || ShouldDeepScanDemonShellObject(objectName, hierarchyPath))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(componentTypeName)
            || !IsLc2TypeName(componentTypeName))
        {
            return false;
        }

        var normalized = text.ToLowerInvariant();
        return DemonShellProbeMemberTokens.Any(normalized.Contains);
    }

    internal static bool ShouldTraverseLoadedUiNode(bool activeInHierarchy)
    {
        return activeInHierarchy;
    }

    internal static IReadOnlyList<string> GetGemLibraryRelativePath()
    {
        return new[] { "InscriptionGemList", "TransformRoot", "Res", "GemSkillPointView", "gemLibrary" };
    }

    internal static bool CanAppendFocusedRow(int currentRowCount, int maximumRowCount)
    {
        return currentRowCount < maximumRowCount;
    }

    internal static string TrimSummary(string value, int maximumLength = 512)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maximumLength)
        {
            return value ?? string.Empty;
        }

        return value.Substring(0, maximumLength) + "...";
    }

    internal static object TryReadNamedMember(object source, string memberName)
    {
        if (source is null || string.IsNullOrWhiteSpace(memberName))
        {
            return null;
        }

        var type = SafeGet(() => source.GetType(), typeof(object));
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var property = SafeGet(() => type.GetProperty(memberName, Flags), null)
                       ?? SafeGet(() => type.GetProperties(Flags)
                           .FirstOrDefault(candidate => string.Equals(candidate.Name, memberName,
                               StringComparison.OrdinalIgnoreCase)), null);
        if (property is not null && property.CanRead && property.GetIndexParameters().Length == 0)
        {
            var value = SafeGet(() => property.GetValue(source, null), null);
            if (value is not null)
            {
                return value;
            }
        }

        var field = SafeGet(() => type.GetField(memberName, Flags), null)
                    ?? SafeGet(() => type.GetFields(Flags)
                        .FirstOrDefault(candidate => string.Equals(candidate.Name, memberName,
                            StringComparison.OrdinalIgnoreCase)), null);
        return field is null ? null : SafeGet(() => field.GetValue(source), null);
    }

    private static bool ContainsKeyword(string text, string keyword)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            return false;
        }

        if (keyword.Any(character => character > 127))
        {
            return text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        var startIndex = 0;
        while (startIndex <= text.Length - keyword.Length)
        {
            var index = text.IndexOf(keyword, startIndex, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return false;
            }

            if (IsBoundaryBefore(text, index) && IsBoundaryAfter(text, index + keyword.Length))
            {
                return true;
            }

            startIndex = index + 1;
        }

        return false;
    }

    private static bool IsLc2TypeName(string typeName)
    {
        return !string.IsNullOrWhiteSpace(typeName)
            && (typeName.StartsWith("Il2CppLC2.", StringComparison.Ordinal)
                || typeName.StartsWith("LC2.", StringComparison.Ordinal)
                || typeName.IndexOf("<LC2.", StringComparison.Ordinal) >= 0);
    }

    private static bool IsTmProTypeName(string typeName)
    {
        return !string.IsNullOrWhiteSpace(typeName)
            && typeName.StartsWith("Il2CppTMPro.", StringComparison.Ordinal);
    }

    private static bool IsNoisyUnityTypeName(string typeName)
    {
        return string.Equals(typeName, "UnityEngine.Transform", StringComparison.Ordinal)
            || string.Equals(typeName, "UnityEngine.RectTransform", StringComparison.Ordinal)
            || string.Equals(typeName, "UnityEngine.Canvas", StringComparison.Ordinal)
            || string.Equals(typeName, "UnityEngine.Component", StringComparison.Ordinal);
    }

    private static bool IsDemonShellArmorTypeName(string typeName)
    {
        return !string.IsNullOrWhiteSpace(typeName)
               && (typeName.IndexOf("Il2CppLC2.Armor", StringComparison.OrdinalIgnoreCase) >= 0
                   || typeName.IndexOf("LC2.Armor", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static bool IsDemonShellPowerExecutorTypeName(string typeName)
    {
        return !string.IsNullOrWhiteSpace(typeName)
               && typeName.IndexOf("Power.PowerExecutor", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsPowerCounterDictionaryMember(string memberName, string valueTypeName)
    {
        if (string.IsNullOrWhiteSpace(memberName))
        {
            return false;
        }

        var normalizedMember = memberName.ToLowerInvariant();
        var normalizedValueType = (valueTypeName ?? string.Empty).ToLowerInvariant();
        return normalizedValueType.Contains("dictionary")
               && (string.Equals(normalizedMember, "powercounterdict", StringComparison.Ordinal)
                   || string.Equals(normalizedMember, "_powercounterdict", StringComparison.Ordinal)
                   || (normalizedMember.Contains("counter") && normalizedValueType.Contains("powercounterdata")));
    }

    private static bool IsRelevantDemonShellSourceTypeName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)
            || (typeName.IndexOf("Il2CppLC2.", StringComparison.OrdinalIgnoreCase) < 0
                && typeName.IndexOf("LC2.", StringComparison.OrdinalIgnoreCase) < 0))
        {
            return false;
        }

        var normalized = typeName.ToLowerInvariant();
        return normalized.Contains("armor")
               || normalized.Contains("bag")
               || normalized.Contains("player")
               || normalized.Contains("battle")
               || normalized.Contains("counter")
               || normalized.Contains("power")
               || normalized.Contains("buff")
               || normalized.Contains("skill")
               || normalized.Contains("creature")
               || normalized.Contains("hunter");
    }

    private static bool IsSimpleProbeValueTypeName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return false;
        }

        return typeName.EndsWith("String", StringComparison.Ordinal)
               || typeName.EndsWith("Boolean", StringComparison.Ordinal)
               || typeName.EndsWith("Byte", StringComparison.Ordinal)
               || typeName.EndsWith("SByte", StringComparison.Ordinal)
               || typeName.EndsWith("Int16", StringComparison.Ordinal)
               || typeName.EndsWith("UInt16", StringComparison.Ordinal)
               || typeName.EndsWith("Int32", StringComparison.Ordinal)
               || typeName.EndsWith("UInt32", StringComparison.Ordinal)
               || typeName.EndsWith("Int64", StringComparison.Ordinal)
               || typeName.EndsWith("UInt64", StringComparison.Ordinal)
               || typeName.EndsWith("Single", StringComparison.Ordinal)
               || typeName.EndsWith("Double", StringComparison.Ordinal)
               || typeName.EndsWith("Decimal", StringComparison.Ordinal);
    }

    private static bool IsUsefulProbeContainerTypeName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return false;
        }

        var normalized = typeName.ToLowerInvariant();
        return normalized.Contains("dictionary")
               || normalized.Contains("list")
               || normalized.Contains("hashset")
               || normalized.Contains("counter")
               || normalized.Contains("power")
               || normalized.Contains("buff")
               || normalized.Contains("skill")
               || normalized.Contains("item")
               || normalized.Contains("equip")
               || normalized.Contains("armor")
               || normalized.Contains("stack")
               || normalized.Contains("data");
    }

    private static bool EndsWithPathSegment(string path, string segment)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(segment))
        {
            return false;
        }

        return string.Equals(path, segment, StringComparison.OrdinalIgnoreCase)
               || path.EndsWith("/" + segment, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBoundaryBefore(string text, int index)
    {
        if (index == 0 || !char.IsLetterOrDigit(text[index - 1]))
        {
            return true;
        }

        return char.IsLower(text[index - 1]) && char.IsUpper(text[index]);
    }

    private static bool IsBoundaryAfter(string text, int index)
    {
        if (index >= text.Length || !char.IsLetterOrDigit(text[index]))
        {
            return true;
        }

        return char.IsLower(text[index - 1]) && char.IsUpper(text[index]);
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

internal readonly record struct DemonShellHudProgress(int DisplayValue, int MaxValue, double FillRatio);

internal readonly record struct DemonShellHudRect(double X, double Y, double Width, double Height);

internal readonly record struct ArmorHudRenderState(
    bool Enabled,
    bool ShowText,
    bool ShowCountdown,
    double X,
    double Y,
    double Width,
    double Height,
    double Opacity,
    int DisplayValue,
    int MaxValue,
    double FillRatio,
    string Label,
    string Suffix,
    bool CountdownVisible,
    double CountdownFillRatio,
    string CountdownText,
    int ScreenWidth,
    int ScreenHeight);
