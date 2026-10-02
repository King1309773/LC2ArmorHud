using System.Globalization;
using System.Reflection;
using LC2;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LC2RuntimeProbe;

internal sealed class DemonShellHudCounterReader
{
    private const int MaxPlayers = 8;
    private const int MaxCounterEntries = 220;

    private readonly Dictionary<ArmorHudKind, GameObject> _cachedEquipmentObjects = new();
    private object _cachedPowerExecutor;
    private object _cachedCounterDictionary;
    private float _nextEquipmentRefreshAt;
    private string _lastStatus = "not sampled";

    internal void Reset()
    {
        _cachedEquipmentObjects.Clear();
        _cachedPowerExecutor = null;
        _cachedCounterDictionary = null;
        _nextEquipmentRefreshAt = 0;
    }

    internal bool TryReadRawValue(out double rawValue)
    {
        return TryReadRawValue(out rawValue, out _);
    }

    internal bool TryReadRawValue(out double rawValue, out string status)
    {
        var snapshot = ReadSnapshot();
        rawValue = snapshot.GetCounterValue(ArmorHudCatalog.Get(ArmorHudKind.DemonShell).Rows[0].CounterKey);
        status = snapshot.Status;
        return snapshot.IsEquipped(ArmorHudKind.DemonShell);
    }

    internal ArmorHudCounterSnapshot ReadSnapshot(float unscaledTime = float.PositiveInfinity)
    {
        var counters = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var equipped = new Dictionary<ArmorHudKind, bool>();
        var statusParts = new List<string>();
        var refreshEquipment = ProbeUtilities.ShouldRefreshArmorHudEquipmentCache(unscaledTime, _nextEquipmentRefreshAt);
        foreach (var definition in ArmorHudCatalog.All)
        {
            equipped[definition.Kind] = IsEquipmentObjectAvailable(definition, refreshEquipment);
        }

        if (refreshEquipment)
        {
            if (!float.IsNaN(unscaledTime) && !float.IsInfinity(unscaledTime))
            {
                _nextEquipmentRefreshAt =
                    unscaledTime + ProbeUtilities.GetArmorHudEquipmentRefreshIntervalSeconds();
            }
        }

        if (_cachedPowerExecutor is not null && TryReadCounterValues(_cachedPowerExecutor, counters))
        {
            statusParts.Add("counter dictionary read via cached executor");
        }
        else
        {
            _cachedPowerExecutor = FindLocalPlayerPowerExecutor();
            if (_cachedPowerExecutor is null)
            {
                statusParts.Add("local player power executor unavailable");
            }
            else if (TryReadCounterValues(_cachedPowerExecutor, counters))
            {
                statusParts.Add("counter dictionary read via local player executor");
            }
            else
            {
                statusParts.Add("PowerCounterDict unavailable");
            }
        }

        var snapshot = new ArmorHudCounterSnapshot(
            equipped,
            counters,
            string.Join("; ", statusParts.Concat(ArmorHudCatalog.All.Select(definition =>
                $"equipped {definition.ItemId}={equipped.TryGetValue(definition.Kind, out var isEquipped) && isEquipped}"))));
        _lastStatus = snapshot.Status;
        return snapshot;
    }

    private bool IsEquipmentObjectAvailable(ArmorHudDefinition definition, bool allowSceneLookup)
    {
        if (_cachedEquipmentObjects.TryGetValue(definition.Kind, out var cachedObject))
        {
            if (IsActiveGameObject(cachedObject))
            {
                return true;
            }

            _cachedEquipmentObjects.Remove(definition.Kind);
        }

        if (!allowSceneLookup)
        {
            return false;
        }

        var foundObject = FindEquipmentObject(definition.ItemId);
        if (IsActiveGameObject(foundObject))
        {
            _cachedEquipmentObjects[definition.Kind] = foundObject;
            return true;
        }

        return false;
    }

    private static GameObject FindEquipmentObject(string itemId)
    {
        foreach (var path in ProbeUtilities.GetDemonShellHudLocalPlayerSearchPaths())
        {
            var exactPath = $"{path}/BagSystem/{itemId}";
            var foundObject = SafeGet(() => GameObject.Find(exactPath), null);
            if (IsActiveGameObject(foundObject))
            {
                return foundObject;
            }
        }

        return null;
    }

    private static bool IsActiveGameObject(GameObject gameObject)
    {
        return gameObject != null && SafeGet(() => gameObject.activeInHierarchy, false);
    }

    private bool TryReadCounterValues(object powerExecutor, Dictionary<string, double> counters)
    {
        var counterDictionary = _cachedCounterDictionary
                                ?? ProbeUtilities.TryReadNamedMember(powerExecutor, "PowerCounterDict")
                                ?? ProbeUtilities.TryReadNamedMember(powerExecutor, "_powerCounterDict");
        if (counterDictionary is null)
        {
            _cachedCounterDictionary = null;
            return false;
        }

        if (TryReadFromCounterDictionary(counterDictionary, counters))
        {
            _cachedCounterDictionary = counterDictionary;
            return true;
        }

        _cachedCounterDictionary = null;
        return false;
    }

    private object FindLocalPlayerPowerExecutor()
    {
        foreach (var path in ProbeUtilities.GetDemonShellHudLocalPlayerSearchPaths())
        {
            var playerObject = SafeGet(() => GameObject.Find(path), null);
            var powerExecutor = TryReadPowerExecutorFromPlayerObject(playerObject);
            if (powerExecutor is not null)
            {
                return powerExecutor;
            }
        }

        var players = SafeGet(() => Object.FindObjectsOfType<Player>(), Array.Empty<Player>());
        foreach (var player in players.Where(candidate => candidate is not null).Take(MaxPlayers))
        {
            var gameObject = SafeGet(() => player.gameObject, null);
            var objectName = SafeGet(() => gameObject is null ? string.Empty : gameObject.name, string.Empty);
            if (!string.IsNullOrWhiteSpace(objectName)
                && !string.Equals(objectName, "LocalPlayer", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var ownerCreature = SafeGet(() => player.OwnerCreature, null);
            var powerExecutor = ProbeUtilities.TryReadNamedMember(ownerCreature, "PowerExecutor");
            if (powerExecutor is not null)
            {
                return powerExecutor;
            }
        }

        foreach (var player in players.Where(candidate => candidate is not null).Take(MaxPlayers))
        {
            var ownerCreature = SafeGet(() => player.OwnerCreature, null);
            var powerExecutor = ProbeUtilities.TryReadNamedMember(ownerCreature, "PowerExecutor");
            if (powerExecutor is not null)
            {
                return powerExecutor;
            }
        }

        return null;
    }

    private static object TryReadPowerExecutorFromPlayerObject(GameObject playerObject)
    {
        if (playerObject is null)
        {
            return null;
        }

        var player = SafeGet(() => playerObject.GetComponent<Player>(), null);
        if (player is null)
        {
            return null;
        }

        var ownerCreature = SafeGet(() => player.OwnerCreature, null);
        return ProbeUtilities.TryReadNamedMember(ownerCreature, "PowerExecutor");
    }

    private static bool TryReadFromCounterDictionary(object counterDictionary, Dictionary<string, double> counters)
    {
        var enumerator = TryGetEnumerator(counterDictionary);
        if (enumerator is null)
        {
            return false;
        }

        var enumeratorType = SafeGet(() => enumerator.GetType(), typeof(object));
        var moveNext = SafeGet(
            () => enumeratorType.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.Public),
            null);
        var currentProperty = SafeGet(
            () => enumeratorType.GetProperty("Current", BindingFlags.Instance | BindingFlags.Public),
            null);
        if (moveNext is null || currentProperty is null)
        {
            return false;
        }

        for (var index = 0; index < MaxCounterEntries; index++)
        {
            var hasNext = SafeGet(() => moveNext.Invoke(enumerator, null) is true, false);
            if (!hasNext)
            {
                break;
            }

            var current = SafeGet(() => currentProperty.GetValue(enumerator, null), null);
            var key = Convert.ToString(ProbeUtilities.TryReadNamedMember(current, "Key"), CultureInfo.InvariantCulture)
                      ?? string.Empty;
            if (!ProbeUtilities.IsArmorHudCounterKey(key))
            {
                continue;
            }

            var value = ProbeUtilities.TryReadNamedMember(current, "Value");
            if (TryReadNumericMember(value, "mCurValue", out var rawValue))
            {
                counters[key] = rawValue;
            }
        }

        return true;
    }

    private static bool TryReadNumericMember(object source, string memberName, out double value)
    {
        value = 0;
        var rawValue = ProbeUtilities.TryReadNamedMember(source, memberName);
        return rawValue is not null
               && double.TryParse(
                   Convert.ToString(rawValue, CultureInfo.InvariantCulture),
                   NumberStyles.Float,
                   CultureInfo.InvariantCulture,
                   out value);
    }

    private static object TryGetEnumerator(object value)
    {
        var type = SafeGet(() => value.GetType(), typeof(object));
        var getEnumerator = SafeGet(
            () => type.GetMethod("GetEnumerator", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null),
            null);
        return SafeGet(() => getEnumerator is null ? null : getEnumerator.Invoke(value, null), null);
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

internal sealed class ArmorHudCounterSnapshot
{
    private readonly Dictionary<ArmorHudKind, bool> _equipped;
    private readonly Dictionary<string, double> _counters;

    internal ArmorHudCounterSnapshot(
        IReadOnlyDictionary<ArmorHudKind, bool> equipped,
        IReadOnlyDictionary<string, double> counters,
        string status)
    {
        _equipped = new Dictionary<ArmorHudKind, bool>(equipped ?? new Dictionary<ArmorHudKind, bool>());
        _counters = new Dictionary<string, double>(counters ?? new Dictionary<string, double>(), StringComparer.OrdinalIgnoreCase);
        Status = status ?? string.Empty;
    }

    internal static ArmorHudCounterSnapshot Empty { get; } = new(
        new Dictionary<ArmorHudKind, bool>(),
        new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase),
        "not sampled");

    internal string Status { get; }

    internal bool IsEquipped(ArmorHudKind kind)
    {
        return _equipped.TryGetValue(kind, out var equipped) && equipped;
    }

    internal double GetCounterValue(string key)
    {
        return !string.IsNullOrWhiteSpace(key) && _counters.TryGetValue(key, out var value) ? value : 0;
    }
}
