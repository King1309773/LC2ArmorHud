using System;
using System.Collections.Generic;

namespace LC2RuntimeProbe;

internal enum ArmorHudCountdownMode
{
    None,
    RefreshAll,
    Independent
}

internal readonly record struct ArmorHudCountdownRule(
    ArmorHudCountdownMode Mode,
    float DurationSeconds,
    int MaxStackCount)
{
    internal static readonly ArmorHudCountdownRule None = new(ArmorHudCountdownMode.None, 0, 0);
}

internal readonly record struct ArmorHudCountdownSnapshot(bool IsVisible, float RemainingSeconds, float FillRatio)
{
    internal static readonly ArmorHudCountdownSnapshot Hidden = new(false, 0, 0);
}

internal sealed class ArmorHudCountdownTracker
{
    private readonly Dictionary<string, CounterState> _states = new(StringComparer.OrdinalIgnoreCase);

    internal ArmorHudCountdownSnapshot Observe(
        string counterKey,
        ArmorHudCountdownRule rule,
        int currentStackCount,
        float sampleTime)
    {
        if (currentStackCount < 0)
        {
            currentStackCount = 0;
        }
        else if (currentStackCount > rule.MaxStackCount)
        {
            currentStackCount = rule.MaxStackCount;
        }

        var hasState = _states.TryGetValue(counterKey, out var state);
        if (!hasState || state.Rule != rule)
        {
            state = new CounterState
            {
                Rule = rule,
                IsSynchronized = currentStackCount == 0,
                StackCount = currentStackCount
            };

            if (hasState)
            {
                _states[counterKey] = state;
            }
            else
            {
                _states.Add(counterKey, state);
            }

            return ArmorHudCountdownSnapshot.Hidden;
        }

        if (!IsActive(rule))
        {
            return ArmorHudCountdownSnapshot.Hidden;
        }

        if (currentStackCount == 0)
        {
            state.IsSynchronized = true;
            state.StackCount = 0;
            state.RefreshExpiry = 0;
            state.Expiries?.Clear();
            _states[counterKey] = state;
            return ArmorHudCountdownSnapshot.Hidden;
        }

        if (!state.IsSynchronized)
        {
            state.StackCount = currentStackCount;
            _states[counterKey] = state;
            return ArmorHudCountdownSnapshot.Hidden;
        }

        if (rule.Mode == ArmorHudCountdownMode.RefreshAll)
        {
            if (currentStackCount > state.StackCount)
            {
                state.RefreshExpiry = sampleTime + rule.DurationSeconds;
            }

            state.StackCount = currentStackCount;
            _states[counterKey] = state;
            return CreateSnapshot(state.RefreshExpiry, rule.DurationSeconds, sampleTime);
        }

        var stackDelta = currentStackCount - state.StackCount;
        if (stackDelta > 0)
        {
            state.Expiries ??= new List<float>(rule.MaxStackCount);
            var expiry = sampleTime + rule.DurationSeconds;
            for (var index = 0; index < stackDelta; index++)
            {
                state.Expiries.Add(expiry);
            }
        }

        if (state.Expiries is not null)
        {
            var expiredCount = 0;
            while (expiredCount < state.Expiries.Count && state.Expiries[expiredCount] <= sampleTime)
            {
                expiredCount++;
            }

            if (expiredCount > 0)
            {
                state.Expiries.RemoveRange(0, expiredCount);
            }

            var excessCount = state.Expiries.Count - currentStackCount;
            if (excessCount > 0)
            {
                state.Expiries.RemoveRange(0, excessCount);
            }
        }

        state.StackCount = currentStackCount;
        _states[counterKey] = state;

        return state.Expiries is null || state.Expiries.Count == 0
            ? ArmorHudCountdownSnapshot.Hidden
            : CreateSnapshot(state.Expiries[0], rule.DurationSeconds, sampleTime);
    }

    internal ArmorHudCountdownSnapshot Get(string counterKey, ArmorHudCountdownRule rule, float sampleTime)
    {
        if (!IsActive(rule) || !_states.TryGetValue(counterKey, out var state) || state.Rule != rule || !state.IsSynchronized || state.StackCount == 0)
        {
            return ArmorHudCountdownSnapshot.Hidden;
        }

        if (rule.Mode == ArmorHudCountdownMode.RefreshAll)
        {
            return CreateSnapshot(state.RefreshExpiry, rule.DurationSeconds, sampleTime);
        }

        return state.Expiries is null || state.Expiries.Count == 0
            ? ArmorHudCountdownSnapshot.Hidden
            : CreateSnapshot(state.Expiries[0], rule.DurationSeconds, sampleTime);
    }

    internal void Clear()
    {
        _states.Clear();
    }

    internal void Clear(string counterKey)
    {
        _states.Remove(counterKey);
    }

    private static bool IsActive(ArmorHudCountdownRule rule)
    {
        return rule.Mode != ArmorHudCountdownMode.None && rule.DurationSeconds > 0;
    }

    private static ArmorHudCountdownSnapshot CreateSnapshot(float expiry, float durationSeconds, float sampleTime)
    {
        if (expiry <= sampleTime || durationSeconds <= 0)
        {
            return ArmorHudCountdownSnapshot.Hidden;
        }

        var remainingSeconds = expiry - sampleTime;
        var fillRatio = remainingSeconds / durationSeconds;
        if (fillRatio > 1)
        {
            fillRatio = 1;
        }

        return new ArmorHudCountdownSnapshot(true, remainingSeconds, fillRatio);
    }

    private struct CounterState
    {
        internal ArmorHudCountdownRule Rule;
        internal bool IsSynchronized;
        internal int StackCount;
        internal float RefreshExpiry;
        internal List<float> Expiries;
    }
}
