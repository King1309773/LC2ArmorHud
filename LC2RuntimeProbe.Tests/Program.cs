using LC2RuntimeProbe;

static void AssertEqual(string expected, string actual, string name)
{
    if (!string.Equals(expected, actual, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"{name}: expected [{expected}], actual [{actual}]");
    }
}

static void AssertTrue(bool value, string name)
{
    if (!value)
    {
        throw new InvalidOperationException($"{name}: expected true");
    }
}

static void AssertClose(double expected, double actual, string name, double tolerance = 0.0001)
{
    if (Math.Abs(expected - actual) > tolerance)
    {
        throw new InvalidOperationException($"{name}: expected [{expected}], actual [{actual}]");
    }
}

static void AssertDefaultHudSettings(ArmorHudSettings settings, string name)
{
    AssertTrue(settings.Enabled, $"{name} default HUD is enabled");
    AssertTrue(settings.ShowText, $"{name} default HUD text is enabled");
    AssertEqual("BottomLeft", settings.Anchor, $"{name} default HUD anchor");
    AssertClose(370, settings.X, $"{name} default HUD x matches requested preset");
    AssertClose(255, settings.Y, $"{name} default HUD y matches requested preset");
    AssertClose(290, settings.Width, $"{name} default HUD width matches requested preset");
    AssertClose(34, settings.Height, $"{name} default HUD height matches requested preset");
    AssertClose(0.87, settings.Opacity, $"{name} default HUD opacity matches requested preset");
}

var sourceRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
var rendererSource = File.ReadAllText(Path.Combine(sourceRoot, "DemonShellHudRenderer.cs"));
var panelSource = File.ReadAllText(Path.Combine(sourceRoot, "DemonShellHudConfigPanel.cs"));
var controllerSource = File.ReadAllText(Path.Combine(sourceRoot, "DemonShellHudController.cs"));
var pluginProjectSource = File.ReadAllText(Path.Combine(sourceRoot, "LC2ArmorHud", "LC2ArmorHud.csproj"));
AssertTrue(rendererSource.Contains("internal void Dispose()", StringComparison.Ordinal), "renderer exposes cleanup");
AssertTrue(panelSource.Contains("internal void Dispose()", StringComparison.Ordinal), "config panel exposes cleanup");
AssertTrue(controllerSource.Contains("internal void Dispose()", StringComparison.Ordinal), "controller owns cleanup");
AssertTrue(controllerSource.Contains("renderer.Dispose()", StringComparison.Ordinal), "controller disposes renderers");
AssertTrue(controllerSource.Contains("_configPanel.Dispose()", StringComparison.Ordinal), "controller disposes panel");
AssertTrue(controllerSource.Contains("_countdownTracker.Observe(", StringComparison.Ordinal), "controller observes countdowns on sample ticks");
AssertTrue(controllerSource.Contains("_countdownTracker.Clear();", StringComparison.Ordinal), "controller clears countdowns during reset");
AssertTrue(
    pluginProjectSource.Contains("<Compile Include=\"..\\ArmorHudCountdownTracker.cs\" Link=\"ArmorHudCountdownTracker.cs\" />", StringComparison.Ordinal),
    "BepInEx project links countdown tracker");
AssertTrue(rendererSource.Contains("private Image _countdownImage;", StringComparison.Ordinal), "renderer owns countdown image");
AssertTrue(rendererSource.Contains("\"Countdown\"", StringComparison.Ordinal), "renderer creates countdown child");
AssertTrue(
    panelSource.Contains("CreateRow(\"countdown\", \"\\u5012\\u8ba1\\u65f6\", 1.0, true)", StringComparison.Ordinal),
    "config panel creates Chinese countdown row");
AssertTrue(
    panelSource.Contains("ArmorHudCatalog.SupportsCountdown(_selectedKind)", StringComparison.Ordinal),
    "config panel guards countdown visibility by armor support");

AssertEqual("\"a,b\"", ProbeUtilities.EscapeCsv("a,b"), "CSV comma quoting");
AssertEqual("\"a\"\"b\"", ProbeUtilities.EscapeCsv("a\"b"), "CSV quote escaping");
AssertEqual("\"line1\nline2\"", ProbeUtilities.EscapeCsv("line1\nline2"), "CSV newline quoting");
AssertEqual("plain", ProbeUtilities.EscapeCsv("plain"), "CSV plain text");

var matches = ProbeUtilities.FindMatchedKeywords("SoulGem 灵魂石 reward");
AssertTrue(matches.Contains("Gem"), "English keyword match");
AssertTrue(matches.Contains("Soul"), "Second English keyword match");
AssertTrue(matches.Contains("灵魂石"), "Chinese keyword match");
AssertTrue(matches.Contains("Reward"), "Case-insensitive keyword match");

AssertEqual("high", ProbeUtilities.CalculateConfidence(4), "High confidence");
AssertEqual("medium", ProbeUtilities.CalculateConfidence(2), "Medium confidence");
AssertEqual("low", ProbeUtilities.CalculateConfidence(1), "Low confidence");

AssertEqual(
    "PlainChild UnityEngine.Transform name=PlainChild",
    ProbeUtilities.BuildCandidateMatchText("PlainChild", "UnityEngine.Transform", "name=PlainChild"),
    "Candidate match text excludes hierarchy paths");
AssertTrue(!ProbeUtilities.IsCandidateMemberName("hideFlags"), "hideFlags excluded from candidate members");
AssertTrue(ProbeUtilities.IsCandidateMemberName("itemId"), "Relevant ID member included");
AssertTrue(!ProbeUtilities.IsPossibleIdMemberName("hideFlags"), "hideFlags excluded from possible IDs");
AssertTrue(ProbeUtilities.IsPossibleIdMemberName("itemId"), "Relevant ID member recognized");

AssertTrue(!ProbeUtilities.FindMatchedKeywords("constrainProportionsScale").Contains("Prop"), "Prop does not match inside Proportions");
AssertTrue(!ProbeUtilities.FindMatchedKeywords("commandBufferCount").Contains("Buff"), "Buff does not match inside Buffer");
AssertTrue(ProbeUtilities.FindMatchedKeywords("SoulGem").Contains("Soul"), "Soul matches camel-case segment");
AssertTrue(ProbeUtilities.FindMatchedKeywords("SoulGem").Contains("Gem"), "Gem matches camel-case segment");

AssertEqual(
    string.Empty,
    string.Join("|", ProbeUtilities.FindCandidateKeywords("PlainChild", "UnityEngine.Transform", "constrainProportionsScale=True; name=PlainChild", true)),
    "Transform summary cannot trigger candidate");
AssertEqual(
    "Gem",
    string.Join("|", ProbeUtilities.FindCandidateKeywords("SoulGem", "UnityEngine.Transform", "name=SoulGem", true).Where(keyword => keyword == "Gem")),
    "Transform object name can trigger candidate");
AssertEqual(
    "Icon",
    string.Join("|", ProbeUtilities.FindCandidateKeywords("PlainChild", "Example.Component", "iconName=GemIcon", false).Where(keyword => keyword == "Icon")),
    "Non-transform summary can trigger high-value candidate");
AssertTrue(
    !ProbeUtilities.FindCandidateKeywords("PlainChild", "Example.Component", "propName=Prop", false).Contains("Prop"),
    "Broad summary keyword does not trigger candidate");
AssertTrue(
    ProbeUtilities.FindCandidateKeywords("PlainChild", "Example.Component", "displayName=\u7075\u9b42\u77f3", false).Contains("\u7075\u9b42\u77f3"),
    "Chinese high-value summary can trigger candidate");
AssertTrue(ProbeUtilities.IsFocusedMemberName("itemId"), "Focused probe includes useful ID member");
AssertTrue(ProbeUtilities.IsFocusedMemberName("text"), "Focused probe includes useful text member");
AssertTrue(!ProbeUtilities.IsFocusedMemberName("gameObject"), "Focused probe excludes structural gameObject property");
AssertTrue(!ProbeUtilities.IsFocusedMemberName("transform"), "Focused probe excludes structural transform property");
AssertTrue(!ProbeUtilities.IsFocusedMemberName("hideFlags"), "Focused probe excludes hideFlags noise");
AssertTrue(ProbeUtilities.CanAppendFocusedRow(299, 300), "Focused probe allows final row within budget");
AssertTrue(!ProbeUtilities.CanAppendFocusedRow(300, 300), "Focused probe enforces row budget");
AssertTrue(
    ProbeUtilities.GetFocusedComponentPriority("Il2CppLC2.PlayerBagUI")
    < ProbeUtilities.GetFocusedComponentPriority("UnityEngine.Transform"),
    "Focused probe prioritizes LC2 business components");
AssertTrue(
    ProbeUtilities.GetFocusedComponentPriority("Il2CppTMPro.TextMeshProUGUI")
    < ProbeUtilities.GetFocusedComponentPriority("UnityEngine.Canvas"),
    "Focused probe prioritizes TMPro components over generic Canvas");
AssertTrue(
    ProbeUtilities.ShouldInspectFocusedMember("Il2CppLC2.PlayerBagUI", "_selectItem"),
    "Focused probe keeps business component members");
AssertTrue(
    !ProbeUtilities.ShouldInspectFocusedMember("UnityEngine.Transform", "localPosition"),
    "Focused probe suppresses generic Transform noise");
AssertTrue(
    ProbeUtilities.ShouldInspectFocusedMember("Il2CppTMPro.TextMeshProUGUI", "text"),
    "Focused probe keeps TMPro text");
AssertTrue(
    ProbeUtilities.ShouldExpandFocusedReference("Il2CppLC2.Item"),
    "Focused probe expands one-hop LC2 references");
AssertTrue(
    ProbeUtilities.ShouldExpandFocusedReference("Il2CppTMPro.TextMeshProUGUI"),
    "Focused probe expands one-hop TMPro references");
AssertTrue(
    !ProbeUtilities.ShouldExpandFocusedReference("UnityEngine.Transform"),
    "Focused probe does not expand generic Unity references");
AssertTrue(
    ProbeUtilities.ShouldTryDirectedPlayerBagLookup("PlayerBagUI"),
    "Focused probe performs directed lookup on the PlayerBagUI object");
AssertTrue(
    !ProbeUtilities.ShouldTryDirectedPlayerBagLookup("CommonItemUnitUI"),
    "Focused probe does not perform directed lookup on PlayerBagUI descendants");
AssertTrue(
    ProbeUtilities.IsGemPanelAnchorName("InscriptionUI"),
    "F11 recognizes the inscription panel anchor");
AssertTrue(
    ProbeUtilities.IsGemPanelAnchorName("gemLibrary"),
    "F11 recognizes the gem library anchor");
AssertTrue(
    !ProbeUtilities.IsGemPanelAnchorName("PlayerBagUI"),
    "F11 does not treat the bag as a gem panel");
AssertTrue(
    ProbeUtilities.ShouldAppendGemUiCandidate("GemIcon", string.Empty, string.Empty),
    "F11 keeps gem-named nodes");
AssertTrue(
    ProbeUtilities.ShouldAppendGemUiCandidate("Unit_1234", "\u7075\u9b42\u77f3", string.Empty),
    "F11 keeps nodes with visible text");
AssertTrue(
    ProbeUtilities.ShouldAppendGemUiCandidate("Unit_1234", string.Empty, "UI_Gem_001"),
    "F11 keeps nodes with an icon");
AssertTrue(
    !ProbeUtilities.ShouldAppendGemUiCandidate("Unit_1234", string.Empty, string.Empty),
    "F11 suppresses empty generic nodes");
AssertTrue(
    ProbeUtilities.IsGemLibraryNodeName("gemLibrary"),
    "F11 recognizes gemLibrary subtree root");
AssertTrue(
    !ProbeUtilities.IsGemLibraryNodeName("InscriptionUI"),
    "F11 does not use the broad inscription panel as the preferred subtree");
AssertTrue(
    ProbeUtilities.IsGemSlotNodeName("Unit_6d22e3ec"),
    "F11 recognizes virtualized gem slot names");
AssertTrue(
    !ProbeUtilities.IsGemSlotNodeName("GemIcon"),
    "F11 does not treat child icon nodes as slots");
AssertEqual(
    "法力值<pos=80%>Lv5 冰抗性<pos=80%>Lv5",
    ProbeUtilities.StripTmProColorTags("<color=#FFEEAEFF>法力值<pos=80%>Lv5</color> <color=#FFEEAEFF>冰抗性<pos=80%>Lv5</color>"),
    "F11 strips TMPro color tags while preserving attribute text");
AssertTrue(
    ProbeUtilities.ShouldTraverseLoadedUiNode(true),
    "F11 traverses active loaded UI nodes");
AssertTrue(
    !ProbeUtilities.ShouldTraverseLoadedUiNode(false),
    "F11 skips inactive loaded UI branches");
AssertEqual(
    "InscriptionGemList/TransformRoot/Res/GemSkillPointView/gemLibrary",
    string.Join("/", ProbeUtilities.GetGemLibraryRelativePath()),
    "F11 uses the verified inscription gem library relative path");
AssertEqual(
    "法力值 Lv5 冰抗性 Lv5",
    ProbeUtilities.NormalizeGemText("<color=#FFEEAEFF>法力值<pos=80%>Lv5</color>   <color=#FFEEAEFF>冰抗性<pos=80%>Lv5</color>"),
    "Gem text normalization removes presentation tags");
AssertEqual(
    ProbeUtilities.CreateGemDefinitionFingerprint("ui_icon_gem_circle_5", "法力值 Lv5"),
    ProbeUtilities.CreateGemDefinitionFingerprint("ui_icon_gem_circle_5", "<color=#FFEEAEFF>法力值<pos=80%>Lv5</color>"),
    "Gem fingerprint ignores presentation tags");
AssertTrue(ProbeUtilities.ShouldInspectGemDataSourceMember("gemId"), "F6 includes gemId");
AssertTrue(
    ProbeUtilities.ShouldInspectGemDataSourceMember("_InscriptionGemList_k__BackingField"),
    "F6 includes gem list backing field");
AssertTrue(!ProbeUtilities.ShouldInspectGemDataSourceMember("gameObject"), "F6 excludes gameObject");
AssertTrue(!ProbeUtilities.ShouldInspectGemDataSourceMember("hideFlags"), "F6 excludes hideFlags");
AssertTrue(
    ProbeUtilities.ShouldTryDirectedInscriptionGemListLookup("InscriptionGemList"),
    "F6 performs directed typed lookup on the known inscription list node");
AssertTrue(
    !ProbeUtilities.ShouldTryDirectedInscriptionGemListLookup("gemLibrary"),
    "F6 does not perform the directed list lookup on descendants");

AssertTrue(
    ProbeUtilities.ShouldInspectDemonShellProbeMember("EquipmentCounterDic"),
    "Demon shell probe inspects equipment counter dictionaries");
AssertTrue(
    ProbeUtilities.ShouldInspectDemonShellProbeMember("currentEquipments"),
    "Demon shell probe inspects current equipment members");
AssertTrue(
    ProbeUtilities.ShouldInspectDemonShellProbeMember("MagicEnergyValue"),
    "Demon shell probe inspects magic energy members");
AssertTrue(
    !ProbeUtilities.ShouldInspectDemonShellProbeMember("transform"),
    "Demon shell probe excludes structural transform member");
AssertTrue(
    ProbeUtilities.ShouldExpandDemonShellProbeReference("Il2CppLC2.PlayerBattleData"),
    "Demon shell probe expands LC2 gameplay references");
AssertTrue(
    ProbeUtilities.ShouldExpandDemonShellProbeReference("UnityEngine.Component<LC2.PlayerBattleData>"),
    "Demon shell probe expands LC2 gameplay references wrapped by Unity component shims");
AssertTrue(
    !ProbeUtilities.ShouldExpandDemonShellProbeReference("UnityEngine.RectTransform"),
    "Demon shell probe does not expand noisy Unity references");
AssertEqual(
    "Equipment#NA_036|NA_036",
    string.Join("|", ProbeUtilities.FindDemonShellProbeMatches("counterKey=Equipment#NA_036")),
    "Demon shell probe matches the full counter key and item ID");
AssertEqual(
    "恶魔君王甲壳",
    string.Join("|", ProbeUtilities.FindDemonShellProbeMatches("已装备 恶魔君王甲壳")),
    "Demon shell probe matches the Chinese equipment name");
AssertTrue(
    ProbeUtilities.TryExtractDemonShellCounterValue("Equipment#NA_036=42", out var parsedCounter)
    && parsedCounter == 42,
    "Demon shell probe extracts integer counter values");
AssertTrue(
    ProbeUtilities.TryExtractDemonShellCounterValue("counter=12.8", out var parsedFloatCounter)
    && parsedFloatCounter == 12,
    "Demon shell probe floors decimal counter values");
AssertTrue(
    !ProbeUtilities.TryExtractDemonShellCounterValue("counter=none", out _),
    "Demon shell probe ignores non-numeric values");
AssertTrue(
    !ProbeUtilities.TryExtractDemonShellCounterValue("name=NA_036", out _),
    "Demon shell probe does not parse item IDs as counter values");
AssertTrue(
    ProbeUtilities.ShouldDeepScanDemonShellObject("NA_036", "GlobalMgr/PlayerRoot/LocalPlayer/BagSystem/NA_036"),
    "Demon shell probe deep-scans the equipped shell object path");
AssertTrue(
    ProbeUtilities.ShouldDeepScanDemonShellObject("BagSystem", "GlobalMgr/PlayerRoot/LocalPlayer/BagSystem"),
    "Demon shell probe deep-scans the player bag system");
AssertTrue(
    !ProbeUtilities.ShouldDeepScanDemonShellObject("Main Camera", "Main Camera"),
    "Demon shell probe ignores unrelated objects");
AssertTrue(
    ProbeUtilities.ShouldInspectDemonShellComponentObject(
        "NA_036",
        "GlobalMgr/PlayerRoot/LocalPlayer/BagSystem/NA_036",
        "UnityEngine.Component"),
    "Demon shell probe keeps globally discovered shell object components");
AssertTrue(
    ProbeUtilities.ShouldInspectDemonShellComponentObject(
        "ArmorProbe",
        "GlobalMgr/PlayerRoot/LocalPlayer/ArmorProbe",
        "UnityEngine.Component<LC2.Armor>"),
    "Demon shell probe recognizes LC2 component names exposed through Unity component shims");
AssertTrue(
    !ProbeUtilities.ShouldInspectDemonShellComponentObject(
        "Canvas",
        "UIRoot/HUD/Canvas",
        "UnityEngine.Canvas"),
    "Demon shell probe ignores unrelated global components");
AssertTrue(
    ProbeUtilities.ShouldRecordDemonShellHierarchyChildren(
        "NA_036",
        "GlobalMgr/PlayerRoot/LocalPlayer/BagSystem/NA_036"),
    "Demon shell probe records children of the equipped shell object");
AssertTrue(
    ProbeUtilities.ShouldRecordDemonShellHierarchyChildren(
        "BagSystem",
        "GlobalMgr/PlayerRoot/LocalPlayer/BagSystem"),
    "Demon shell probe records children of the player bag system");
AssertTrue(
    !ProbeUtilities.ShouldRecordDemonShellHierarchyChildren("Canvas", "UIRoot/HUD/Canvas"),
    "Demon shell probe does not record children for unrelated objects");
AssertEqual(
    "F8",
    ProbeUtilities.GetDemonShellProgressProbeHotkeyLabel(),
    "Demon shell probe uses F8");
AssertEqual(
    "F12",
    ProbeUtilities.GetKeywordCandidateProbeHotkeyLabel(),
    "Keyword candidate probe moves to F12");
AssertEqual(
    "F5",
    ProbeUtilities.GetDemonShellHudConfigPanelHotkeyLabel(),
    "Demon shell HUD config panel uses F5");
AssertEqual(
    "UnityUI",
    ProbeUtilities.GetDemonShellHudRendererBackendLabel(),
    "Demon shell HUD uses Unity UI renderer instead of IMGUI");
AssertEqual(
    "\u7532\u58f3",
    ProbeUtilities.GetDemonShellHudConfigLabel(),
    "Demon shell HUD config label is Chinese");
AssertEqual(
    "\u517d\u9762\u94e0",
    ProbeUtilities.GetBeastArmorHudConfigLabel(),
    "Beast armor HUD config label is Chinese");
AssertEqual(
    "\u66b4\u51fb\u7387",
    ProbeUtilities.GetBeastArmorCritHudLabel(),
    "Beast armor crit HUD label is Chinese");
AssertEqual(
    "\u751f\u547d\u503c",
    ProbeUtilities.GetBeastArmorHealthHudLabel(),
    "Beast armor health HUD label is Chinese");
AssertEqual(
    "\u9ed1\u9b54\u6cd5\u5e08\u957f\u888d",
    ProbeUtilities.GetBlackMageRobeHudConfigLabel(),
    "Black mage robe HUD config label is Chinese");
AssertEqual(
    "\u5b88\u62a4\u8005\u767d\u888d",
    ProbeUtilities.GetGuardianWhiteRobeHudConfigLabel(),
    "Guardian white robe HUD config label is Chinese");
AssertEqual(
    "\u7ea2\u77f3\u6cd5\u5e08\u957f\u888d",
    ProbeUtilities.GetRedstoneMageRobeHudConfigLabel(),
    "Redstone mage robe HUD config label is Chinese");
AssertEqual(
    "\u5c60\u9b54\u8005\u91cd\u7532",
    ProbeUtilities.GetDemonSlayerHeavyArmorHudConfigLabel(),
    "Demon slayer heavy armor HUD config label is Chinese");
AssertEqual(
    "\u6280\u80fd\u5f3a\u5ea6",
    ProbeUtilities.GetMagicPowerHudLabel(),
    "Magic power armor HUD row label is Chinese");
AssertEqual(
    "\u653b\u51fb\u529b",
    ProbeUtilities.GetAttackHudLabel(),
    "Attack armor HUD row label is Chinese");
AssertTrue(
    ProbeUtilities.IsArmorHudCounterKey("Equipment#HA_019"),
    "Armor HUD recognizes beast armor crit counter");
AssertTrue(
    ProbeUtilities.IsArmorHudCounterKey("Equipment#HA_019_Default_0"),
    "Armor HUD recognizes beast armor max-health counter");
AssertTrue(
    ProbeUtilities.IsArmorHudCounterKey("Equipment#CA_026"),
    "Armor HUD recognizes black mage robe counter");
AssertTrue(
    ProbeUtilities.IsArmorHudCounterKey("Equipment#CA_016"),
    "Armor HUD recognizes guardian white robe counter");
AssertTrue(
    ProbeUtilities.IsArmorHudCounterKey("Equipment#CA_017"),
    "Armor HUD recognizes redstone mage robe counter");
AssertTrue(
    ProbeUtilities.IsArmorHudCounterKey("Equipment#HA_008"),
    "Armor HUD recognizes demon slayer heavy armor counter");
AssertTrue(
    ProbeUtilities.TryBuildArmorHudProgress(21.6, 20, out var beastFullProgress)
    && beastFullProgress.DisplayValue == 20
    && Math.Abs(beastFullProgress.FillRatio - 1.0) < 0.0001,
    "Beast armor HUD clamps progress to 20");
AssertEqual(
    "\u66b4\u51fb\u7387 7/20%",
    ProbeUtilities.FormatArmorHudLabel(ProbeUtilities.GetBeastArmorCritHudLabel(), new DemonShellHudProgress(7, 20, 0.35), "%"),
    "Beast armor crit HUD label shows percentage bonus");
AssertEqual(
    "\u751f\u547d\u503c 20/20%",
    ProbeUtilities.FormatArmorHudLabel(ProbeUtilities.GetBeastArmorHealthHudLabel(), new DemonShellHudProgress(20, 20, 1.0), "%"),
    "Beast armor health HUD label shows percentage bonus");
AssertEqual(
    "\u6280\u80fd\u5f3a\u5ea6 35/35%",
    ProbeUtilities.FormatArmorHudLabel(ProbeUtilities.GetMagicPowerHudLabel(), new DemonShellHudProgress(35, 35, 1.0), "%"),
    "Magic robe HUD label shows percentage bonus");
AssertTrue(
    ProbeUtilities.TryBuildArmorHudProgress(12, 12, 3, out var demonSlayerAttackProgress)
    && demonSlayerAttackProgress.DisplayValue == 36
    && demonSlayerAttackProgress.MaxValue == 36
    && Math.Abs(demonSlayerAttackProgress.FillRatio - 1.0) < 0.0001,
    "Demon slayer heavy armor HUD converts stacks to attack percent");
AssertEqual(
    "\u653b\u51fb\u529b 36/36%",
    ProbeUtilities.FormatArmorHudLabel(ProbeUtilities.GetAttackHudLabel(), demonSlayerAttackProgress, "%"),
    "Attack armor HUD label shows percentage bonus");
AssertTrue(
    ProbeUtilities.TryBuildArmorHudProgress(4, 8, 10, out var guardianMagicProgress)
    && guardianMagicProgress.DisplayValue == 40
    && guardianMagicProgress.MaxValue == 80
    && Math.Abs(guardianMagicProgress.FillRatio - 0.5) < 0.0001,
    "Guardian white robe HUD converts stacks to skill strength percent");
AssertTrue(
    ProbeUtilities.TryBuildArmorHudProgress(12, 12, 5, out var redstoneMagicProgress)
    && redstoneMagicProgress.DisplayValue == 60
    && redstoneMagicProgress.MaxValue == 60
    && Math.Abs(redstoneMagicProgress.FillRatio - 1.0) < 0.0001,
    "Redstone mage robe HUD converts stacks to skill strength percent");
AssertTrue(
    !ProbeUtilities.ShouldRefreshArmorHudEquipmentCache(1.2f, 2.0f),
    "Armor HUD equipment cache avoids scene lookup before refresh time");
AssertTrue(
    ProbeUtilities.ShouldRefreshArmorHudEquipmentCache(2.0f, 2.0f),
    "Armor HUD equipment cache refreshes when interval expires");
AssertClose(
    1.0,
    ProbeUtilities.GetArmorHudEquipmentRefreshIntervalSeconds(),
    "Armor HUD equipment cache uses a low-frequency refresh interval");
AssertTrue(
    ProbeUtilities.ShouldRefreshArmorHudConfigPanelLayout(true, 1920, 1080, 1920, 1080),
    "Armor HUD config panel refreshes dirty layouts");
AssertTrue(
    !ProbeUtilities.ShouldRefreshArmorHudConfigPanelLayout(false, 1920, 1080, 1920, 1080),
    "Armor HUD config panel skips unchanged layouts");
AssertTrue(
    ProbeUtilities.ShouldRefreshArmorHudConfigPanelLayout(false, 2560, 1440, 1920, 1080),
    "Armor HUD config panel refreshes when screen size changes");
var renderStateSettings = ArmorHudSettings.CreateDefault(ArmorHudKind.DemonSlayerHeavyArmor);
var renderState = ProbeUtilities.CreateArmorHudRenderState(
    renderStateSettings,
    demonSlayerAttackProgress,
    ProbeUtilities.GetAttackHudLabel(),
    "%",
    1920,
    1080);
AssertTrue(
    ProbeUtilities.HasArmorHudRenderStateChanged(null, renderState),
    "Armor HUD renderer updates on first draw");
AssertTrue(
    !ProbeUtilities.HasArmorHudRenderStateChanged(renderState, renderState),
    "Armor HUD renderer skips duplicate draw inputs");
var changedRenderState = ProbeUtilities.CreateArmorHudRenderState(
    renderStateSettings,
    new DemonShellHudProgress(33, 36, 33.0 / 36.0),
    ProbeUtilities.GetAttackHudLabel(),
    "%",
    1920,
    1080);
AssertTrue(
    ProbeUtilities.HasArmorHudRenderStateChanged(renderState, changedRenderState),
    "Armor HUD renderer updates when progress changes");
var resizedRenderState = ProbeUtilities.CreateArmorHudRenderState(
    renderStateSettings,
    demonSlayerAttackProgress,
    ProbeUtilities.GetAttackHudLabel(),
    "%",
    2560,
    1440);
AssertTrue(
    ProbeUtilities.HasArmorHudRenderStateChanged(renderState, resizedRenderState),
    "Armor HUD renderer updates when screen size changes");
var visibleCountdown = new ArmorHudCountdownSnapshot(true, 8.6f, 8.6f / 15.0f);
AssertEqual(
    "\u6700\u65e9 8.6\u79d2",
    ProbeUtilities.FormatArmorHudCountdownText(visibleCountdown),
    "Armor HUD countdown text is compact Chinese");
AssertEqual(
    string.Empty,
    ProbeUtilities.FormatArmorHudCountdownText(ArmorHudCountdownSnapshot.Hidden),
    "Hidden armor HUD countdown has no text");
var countdownRenderState = ProbeUtilities.CreateArmorHudRenderState(
    renderStateSettings,
    demonSlayerAttackProgress,
    ProbeUtilities.GetAttackHudLabel(),
    "%",
    visibleCountdown,
    1920,
    1080);
AssertTrue(
    ProbeUtilities.HasArmorHudRenderStateChanged(renderState, countdownRenderState),
    "Armor HUD renderer updates when countdown state appears");
AssertTrue(
    !ProbeUtilities.HasArmorHudRenderStateChanged(countdownRenderState, countdownRenderState),
    "Armor HUD renderer skips duplicate countdown state");
AssertTrue(
    ProbeUtilities.ShouldInspectDemonShellDirectedMember(
        "Il2CppLC2.Armor",
        "field_Private_Int32_0",
        "System.Int32"),
    "Demon shell directed probe records simple private armor values even when names are obfuscated");
AssertTrue(
    ProbeUtilities.ShouldInspectDemonShellDirectedMember(
        "Il2CppLC2.PlayerBattleData",
        "EquipmentCounterDic",
        "Il2CppSystem.Collections.Generic.Dictionary`2"),
    "Demon shell directed probe records player equipment counter dictionaries");
AssertTrue(
    ProbeUtilities.ShouldInspectDemonShellDirectedMember(
        "Il2CppLC2.Power.PowerExecutor",
        "PowerCounterDict",
        "Il2CppSystem.Collections.Generic.Dictionary`2"),
    "Demon shell directed probe records power executor counter dictionaries");
AssertTrue(
    ProbeUtilities.ShouldInspectDemonShellDirectedMember(
        "Il2CppLC2.Power.PowerExecutor",
        "_powerCounterDict",
        "Il2CppSystem.Collections.Generic.Dictionary`2<System.String,LC2.Power.PowerCounterData>"),
    "Demon shell directed probe records private power executor counter dictionaries");
AssertTrue(
    !ProbeUtilities.ShouldInspectDemonShellDirectedMember(
        "Il2CppLC2.Power.PowerExecutor",
        "_allPowerDict",
        "Il2CppSystem.Collections.Generic.Dictionary`2<System.Int32,LC2.Power.PowerBase>"),
    "Demon shell directed probe skips broad power executor power dictionaries");
AssertTrue(
    !ProbeUtilities.ShouldInspectDemonShellDirectedMember(
        "Il2CppLC2.Power.PowerExecutor",
        "_SortedPowerList",
        "Il2CppSystem.Collections.Generic.List`1<LC2.Power.PowerBase>"),
    "Demon shell directed probe skips broad power executor power lists");
AssertTrue(
    ProbeUtilities.ShouldTryDemonShellLocalPlayerLookup(
        "LocalPlayer",
        "GlobalMgr/PlayerRoot/LocalPlayer"),
    "Demon shell directed probe follows the local player owner path");
AssertEqual(
    "GlobalMgr/PlayerRoot/LocalPlayer|LocalPlayer",
    string.Join("|", ProbeUtilities.GetDemonShellHudLocalPlayerSearchPaths()),
    "Demon shell HUD tries the verified scene path before broad player scans");
AssertTrue(
    !ProbeUtilities.ShouldTryDemonShellLocalPlayerLookup(
        "BagSystem",
        "GlobalMgr/PlayerRoot/LocalPlayer/BagSystem"),
    "Demon shell directed probe does not treat BagSystem as the player component anchor");
AssertTrue(
    ProbeUtilities.IsDemonShellCounterKey("Equipment#NA_036"),
    "Demon shell directed probe recognizes the exact equipment counter key");
AssertTrue(
    !ProbeUtilities.IsDemonShellCounterKey("Equipment#CA_001"),
    "Demon shell directed probe ignores unrelated equipment counter keys");
AssertTrue(
    !ProbeUtilities.ShouldInspectDemonShellDirectedMember(
        "UnityEngine.Transform",
        "localPosition",
        "UnityEngine.Vector3"),
    "Demon shell directed probe ignores unrelated Unity transform members");
AssertTrue(
    ProbeUtilities.TryBuildDemonShellHudProgress(12.6, out var earlyHudProgress)
    && earlyHudProgress.DisplayValue == 12
    && earlyHudProgress.MaxValue == 60,
    "Demon shell HUD floors partial progress for x/60 text");
AssertClose(
    0.21,
    earlyHudProgress.FillRatio,
    "Demon shell HUD computes fill ratio against fixed 60-point threshold");
AssertTrue(
    ProbeUtilities.TryBuildDemonShellHudProgress(61.2, out var fullHudProgress)
    && fullHudProgress.DisplayValue == 60
    && Math.Abs(fullHudProgress.FillRatio - 1.0) < 0.0001,
    "Demon shell HUD clamps overfilled progress to the 60-point threshold");
AssertTrue(
    ProbeUtilities.TryBuildDemonShellHudProgress(-3, out var emptyHudProgress)
    && emptyHudProgress.DisplayValue == 0
    && Math.Abs(emptyHudProgress.FillRatio) < 0.0001,
    "Demon shell HUD clamps negative progress to zero");
AssertEqual(
    "30/60",
    ProbeUtilities.FormatDemonShellHudLabel(new DemonShellHudProgress(30, 60, 0.5)),
    "Demon shell HUD label shows only numeric progress");
AssertClose(0.15, ProbeUtilities.ClampDemonShellHudOpacity(-1), "HUD opacity has a readable minimum");
AssertClose(1.0, ProbeUtilities.ClampDemonShellHudOpacity(4), "HUD opacity has an upper bound");
AssertEqual("BottomLeft", ProbeUtilities.NormalizeDemonShellHudAnchor("bottomleft"), "HUD anchor normalizes casing");
AssertEqual("BottomLeft", ProbeUtilities.NormalizeDemonShellHudAnchor("center"), "HUD anchor falls back to bottom-left");
var defaultHudRect = ProbeUtilities.GetDemonShellHudDefaultConfigRect();
AssertClose(370, defaultHudRect.X, "HUD default x matches requested preset");
AssertClose(255, defaultHudRect.Y, "HUD default y matches requested preset");
AssertClose(290, defaultHudRect.Width, "HUD default width matches requested preset");
AssertClose(34, defaultHudRect.Height, "HUD default height matches requested preset");
var beastHudRect = ProbeUtilities.GetBeastArmorHudDefaultConfigRect();
AssertClose(370, beastHudRect.X, "Beast armor HUD default x matches requested preset");
AssertClose(255, beastHudRect.Y, "Beast armor HUD default y matches requested preset");
AssertClose(290, beastHudRect.Width, "Beast armor HUD default width matches requested preset");
AssertClose(34, beastHudRect.Height, "Beast armor HUD default height matches requested preset");
AssertDefaultHudSettings(
    ArmorHudSettings.CreateDefault(ArmorHudKind.DemonSlayerHeavyArmor),
    ProbeUtilities.GetDemonSlayerHeavyArmorHudConfigLabel());
AssertTrue(
    ArmorHudSettings.CreateDefault(ArmorHudKind.GuardianWhiteRobe).ShowCountdown,
    "Guardian white robe default countdown is enabled");
var countdownDisabledSettings = ArmorHudSettings.CreateDefault(ArmorHudKind.GuardianWhiteRobe);
countdownDisabledSettings.ShowCountdown = false;
AssertTrue(!countdownDisabledSettings.Clone().ShowCountdown, "Armor HUD clone preserves disabled countdown");
AssertTrue(
    System.Text.Json.JsonSerializer.Deserialize<ArmorHudSettings>("{}")!.Normalize().ShowCountdown,
    "Armor HUD JSON without countdown defaults it to enabled");
foreach (var definition in ArmorHudCatalog.All)
{
    AssertDefaultHudSettings(ArmorHudSettings.CreateDefault(definition.Kind), definition.ConfigLabel);
}
AssertClose(0, ProbeUtilities.ClampDemonShellHudConfigValue("x", -12), "HUD config x clamps at zero");
AssertClose(600, ProbeUtilities.ClampDemonShellHudConfigValue("width", 999), "HUD config width clamps at maximum");
AssertClose(12, ProbeUtilities.ClampDemonShellHudConfigValue("height", 2), "HUD config height clamps at minimum");
AssertClose(1, ProbeUtilities.ClampDemonShellHudConfigValue("opacity", 4), "HUD config opacity clamps at maximum");
var hudRect = ProbeUtilities.ResolveDemonShellHudRect("BottomLeft", 420, 120, 240, 24, 1920, 1080);
AssertClose(420, hudRect.X, "HUD bottom-left rect x");
AssertClose(936, hudRect.Y, "HUD bottom-left rect y");
AssertClose(240, hudRect.Width, "HUD rect width");
AssertClose(24, hudRect.Height, "HUD rect height");
var reflectedMemberSource = new ReflectedMemberSource();
AssertEqual("public-property", Convert.ToString(ProbeUtilities.TryReadNamedMember(reflectedMemberSource, "PublicProperty")) ?? string.Empty, "Shared member reader reads public properties");
AssertEqual("private-field", Convert.ToString(ProbeUtilities.TryReadNamedMember(reflectedMemberSource, "_privateField")) ?? string.Empty, "Shared member reader reads private fields");

AssertTrue(ArmorHudCatalog.All is not ArmorHudDefinition[], "Armor HUD catalog does not expose its definition array");
AssertTrue(
    ArmorHudCatalog.Get(ArmorHudKind.BeastArmor).Rows is not ArmorHudRowDefinition[],
    "Armor HUD definition does not expose its row array");

var beastCountdownRule = ArmorHudCatalog.GetCountdownRule(ArmorHudKind.BeastArmor);
AssertTrue(
    beastCountdownRule.Mode == ArmorHudCountdownMode.RefreshAll,
    "Beast armor countdown refreshes all stacks");
AssertClose(15, beastCountdownRule.DurationSeconds, "Beast armor countdown lasts fifteen seconds");
AssertClose(20, beastCountdownRule.MaxStackCount, "Beast armor countdown caps at twenty stacks");
var blackMageCountdownRule = ArmorHudCatalog.GetCountdownRule(ArmorHudKind.BlackMageRobe);
AssertTrue(
    blackMageCountdownRule.Mode == ArmorHudCountdownMode.Independent,
    "Black mage robe uses independent countdowns");
AssertClose(10, blackMageCountdownRule.DurationSeconds, "Black mage robe countdown lasts ten seconds");
AssertClose(35, blackMageCountdownRule.MaxStackCount, "Black mage robe countdown caps at thirty-five stacks");
var guardianWhiteCountdownRule = ArmorHudCatalog.GetCountdownRule(ArmorHudKind.GuardianWhiteRobe);
AssertTrue(
    guardianWhiteCountdownRule.Mode == ArmorHudCountdownMode.Independent,
    "Guardian white robe uses independent countdowns");
AssertClose(15, guardianWhiteCountdownRule.DurationSeconds, "Guardian white robe countdown lasts fifteen seconds");
AssertClose(8, guardianWhiteCountdownRule.MaxStackCount, "Guardian white robe countdown caps at eight stacks");
AssertTrue(
    ArmorHudCatalog.GetCountdownRule(ArmorHudKind.DemonShell) == ArmorHudCountdownRule.None,
    "Demon shell resolves to the none countdown rule");
AssertTrue(
    ArmorHudCatalog.GetCountdownRule(ArmorHudKind.DemonSlayerHeavyArmor) == ArmorHudCountdownRule.None,
    "Demon slayer heavy armor resolves to the none countdown rule");
AssertClose(0, ArmorHudCountdownRule.None.MaxStackCount, "None countdown rule has no stack capacity");
AssertTrue(ArmorHudCatalog.SupportsCountdown(ArmorHudKind.BlackMageRobe), "Black mage robe supports countdowns");
AssertTrue(!ArmorHudCatalog.SupportsCountdown(ArmorHudKind.DemonShell), "Demon shell has no countdown");
AssertTrue(
    !ArmorHudCatalog.SupportsCountdown(ArmorHudKind.DemonSlayerHeavyArmor),
    "Demon slayer heavy armor has no countdown");

var beastCountdownTracker = new ArmorHudCountdownTracker();
var beastUnsynchronized = beastCountdownTracker.Observe("Equipment#HA_019", beastCountdownRule, 1, 10);
AssertTrue(!beastUnsynchronized.IsVisible, "Initial nonzero beast armor sample stays hidden");
var beastStillUnsynchronized = beastCountdownTracker.Observe("Equipment#HA_019", beastCountdownRule, 2, 11);
AssertTrue(!beastStillUnsynchronized.IsVisible, "Unsynchronized beast armor increase stays hidden");
beastCountdownTracker.Observe("Equipment#HA_019", beastCountdownRule, 0, 12);
var beastStarted = beastCountdownTracker.Observe("Equipment#HA_019", beastCountdownRule, 1, 13);
AssertTrue(beastStarted.IsVisible, "Beast armor starts after a synchronized increase");
AssertClose(15, beastStarted.RemainingSeconds, "Beast armor starts at its full duration");
var beastRefreshed = beastCountdownTracker.Observe("Equipment#HA_019", beastCountdownRule, 2, 15);
AssertClose(15, beastRefreshed.RemainingSeconds, "Beast armor increase refreshes the full duration");
var beastFiveSecondsLater = beastCountdownTracker.Get("equipment#ha_019", beastCountdownRule, 20);
AssertTrue(beastFiveSecondsLater.IsVisible, "Countdown lookup uses case-insensitive counter keys");
AssertClose(10, beastFiveSecondsLater.RemainingSeconds, "Beast armor has ten seconds after five seconds elapse");
var beastClearedByZero = beastCountdownTracker.Observe("Equipment#HA_019", beastCountdownRule, 0, 21);
AssertTrue(!beastClearedByZero.IsVisible, "Beast armor zero observation hides an active countdown");
AssertTrue(
    !beastCountdownTracker.Get("Equipment#HA_019", beastCountdownRule, 25).IsVisible,
    "Beast armor remains hidden after a zero observation");

var redstoneCountdownRule = ArmorHudCatalog.GetCountdownRule(ArmorHudKind.RedstoneMageRobe);
AssertTrue(
    redstoneCountdownRule.Mode == ArmorHudCountdownMode.Independent,
    "Redstone mage robe uses independent countdowns");
AssertClose(10, redstoneCountdownRule.DurationSeconds, "Redstone mage robe countdown lasts ten seconds");
AssertClose(12, redstoneCountdownRule.MaxStackCount, "Redstone mage robe countdown caps at twelve stacks");
var differentRedstoneGetRule = new ArmorHudCountdownRule(ArmorHudCountdownMode.Independent, 15, 12);
var mismatchedGetTracker = new ArmorHudCountdownTracker();
mismatchedGetTracker.Observe("Equipment#GetRuleMismatch", redstoneCountdownRule, 0, 0);
AssertTrue(
    mismatchedGetTracker.Observe("Equipment#GetRuleMismatch", redstoneCountdownRule, 1, 1).IsVisible,
    "Redstone countdown starts before a mismatched rule lookup");
AssertTrue(
    !mismatchedGetTracker.Get("Equipment#GetRuleMismatch", differentRedstoneGetRule, 2).IsVisible,
    "Countdown lookup hides state created by a different rule");
var ruleResetTracker = new ArmorHudCountdownTracker();
ruleResetTracker.Observe("Equipment#RuleReset", redstoneCountdownRule, 0, 0);
AssertTrue(
    ruleResetTracker.Observe("Equipment#RuleReset", redstoneCountdownRule, 1, 1).IsVisible,
    "Redstone countdown starts before a rule change");
AssertTrue(
    !ruleResetTracker.Observe("Equipment#RuleReset", beastCountdownRule, 1, 2).IsVisible,
    "Rule change to beast armor hides the prior redstone countdown");
AssertTrue(
    !ruleResetTracker.Observe("Equipment#RuleReset", redstoneCountdownRule, 1, 3).IsVisible,
    "Rule change back to redstone stays hidden without synchronization");
ruleResetTracker.Observe("Equipment#RuleReset", redstoneCountdownRule, 0, 4);
var ruleResetRestarted = ruleResetTracker.Observe("Equipment#RuleReset", redstoneCountdownRule, 2, 5);
AssertTrue(ruleResetRestarted.IsVisible, "Redstone countdown restarts after zero synchronization and an increase");
AssertClose(10, ruleResetRestarted.RemainingSeconds, "Redstone restart uses a fresh full duration");
var redstoneCountdownTracker = new ArmorHudCountdownTracker();
redstoneCountdownTracker.Observe("Equipment#CA_017", redstoneCountdownRule, 0, 0);
var redstoneTwoStacks = redstoneCountdownTracker.Observe("Equipment#CA_017", redstoneCountdownRule, 2, 1);
AssertClose(10, redstoneTwoStacks.RemainingSeconds, "Redstone two stacks start at ten seconds");
var redstoneThirdStack = redstoneCountdownTracker.Observe("Equipment#CA_017", redstoneCountdownRule, 3, 4);
AssertClose(7, redstoneThirdStack.RemainingSeconds, "Redstone keeps the earliest independent countdown");
var redstoneAfterExpiry = redstoneCountdownTracker.Observe("Equipment#CA_017", redstoneCountdownRule, 1, 12);
AssertClose(2, redstoneAfterExpiry.RemainingSeconds, "Redstone keeps its remaining third-stack countdown");
redstoneCountdownTracker.Clear();
AssertTrue(
    !redstoneCountdownTracker.Get("Equipment#CA_017", redstoneCountdownRule, 12).IsVisible,
    "Clearing the tracker hides redstone countdowns");

var redstoneBoundedTracker = new ArmorHudCountdownTracker();
redstoneBoundedTracker.Observe("Equipment#CA_017_Bounded", redstoneCountdownRule, 0, 0);
redstoneBoundedTracker.Observe("Equipment#CA_017_Bounded", redstoneCountdownRule, 11, 1);
var redstoneOversized = redstoneBoundedTracker.Observe("Equipment#CA_017_Bounded", redstoneCountdownRule, 1000, 4);
AssertTrue(redstoneOversized.IsVisible, "Oversized redstone counter observation remains usable");
AssertTrue(redstoneOversized.FillRatio <= 1, "Oversized redstone counter never exceeds full fill");
var redstoneAtVerifiedMaximum = redstoneBoundedTracker.Observe("Equipment#CA_017_Bounded", redstoneCountdownRule, 12, 5);
AssertClose(
    6,
    redstoneAtVerifiedMaximum.RemainingSeconds,
    "Redstone counter is bounded to its verified twelve-stack maximum");

var independentTrimTracker = new ArmorHudCountdownTracker();
independentTrimTracker.Observe("Equipment#CA_017_Trim", redstoneCountdownRule, 0, 0);
independentTrimTracker.Observe("Equipment#CA_017_Trim", redstoneCountdownRule, 1, 1);
independentTrimTracker.Observe("Equipment#CA_017_Trim", redstoneCountdownRule, 2, 4);
var independentAfterCounterDecrease = independentTrimTracker.Observe("Equipment#CA_017_Trim", redstoneCountdownRule, 1, 5);
AssertTrue(independentAfterCounterDecrease.IsVisible, "Independent countdown remains visible after a stack decrease");
AssertClose(
    9,
    independentAfterCounterDecrease.RemainingSeconds,
    "Independent countdown removes the oldest valid expiry after a stack decrease");

Console.WriteLine("LC2RuntimeProbe utility tests passed.");

internal sealed class ReflectedMemberSource
{
#pragma warning disable CS0414
    private readonly string _privateField = "private-field";
#pragma warning restore CS0414

    public string PublicProperty { get; } = "public-property";
}
