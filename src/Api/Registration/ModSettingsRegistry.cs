using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using ModManagerSettings.Core;
using ModManagerSettings.Features.Multiplayer;
using MegaCrit.Sts2.Core.Logging;

namespace ModManagerSettings.Api;

/// <summary>
/// Shared registry that other mods can use to expose settings to ModManagerSettings.
/// </summary>
public static class ModSettingsRegistry
{
    private static readonly object LockObject = new();

    private static readonly Dictionary<string, ModSettingsRegistration> Registrations =
        new(StringComparer.OrdinalIgnoreCase);

    public static void Register(ModSettingsRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentException.ThrowIfNullOrWhiteSpace(registration.ModPckName);

        var guarded = BuildGuardedRegistration(registration);

        lock (LockObject)
        {
            Registrations[registration.ModPckName] = guarded;
        }

        Log.Info(
            $"[ModManagerSettings] Registered settings provider for '{guarded.ModPckName}' " +
            $"(toggles={guarded.ToggleSettings.Count}, numbers={guarded.NumberSettings.Count}, " +
            $"choices={guarded.ChoiceSettings.Count}, text={guarded.TextSettings.Count}, " +
            $"colors={guarded.ColorSettings.Count}).");
    }

    /// <summary>
    /// Replaces one existing registration. Equivalent to Register, but explicit intent.
    /// </summary>
    public static void UpsertRegistration(ModSettingsRegistration registration) => Register(registration);

    /// <summary>
    /// Deletes one mod registration (all settings for that mod).
    /// </summary>
    public static bool RemoveRegistration(string modPckName)
    {
        if (string.IsNullOrWhiteSpace(modPckName))
        {
            return false;
        }

        lock (LockObject)
        {
            return Registrations.Remove(modPckName);
        }
    }

    /// <summary>
    /// Applies a transform function to one registration and stores the result.
    /// Useful for "modify in place" style edits.
    /// </summary>
    public static bool ModifyRegistration(string modPckName, Func<ModSettingsRegistration, ModSettingsRegistration> transform)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modPckName);
        ArgumentNullException.ThrowIfNull(transform);

        lock (LockObject)
        {
            if (!Registrations.TryGetValue(modPckName, out var existing))
            {
                return false;
            }

            var updated = transform(existing);
            ArgumentNullException.ThrowIfNull(updated);

            var normalized = updated with
            {
                ModPckName = string.IsNullOrWhiteSpace(updated.ModPckName) ? modPckName : updated.ModPckName
            };

            var guarded = BuildGuardedRegistration(normalized);
            Registrations[guarded.ModPckName] = guarded;
            if (!guarded.ModPckName.Equals(modPckName, StringComparison.OrdinalIgnoreCase))
            {
                Registrations.Remove(modPckName);
            }

            return true;
        }
    }

    public static bool TryGet(string modPckName, out ModSettingsRegistration registration)
    {
        lock (LockObject)
        {
            return Registrations.TryGetValue(modPckName, out registration!);
        }
    }

    public static IReadOnlyList<ModSettingsRegistration> GetAll()
    {
        lock (LockObject)
        {
            return Registrations.Values.ToList();
        }
    }

    /// <summary>
    /// True once SaveManager can resolve profile-scoped storage paths.
    /// </summary>
    public static bool IsPersistenceReady()
    {
        return ProfileSettingsStore.IsProfileScopedPathReady();
    }

    /// <summary>
    /// Reads raw persisted values for a mod from profile storage.
    /// </summary>
    public static IReadOnlyDictionary<string, string> GetPersistedSettingValues(string modPckName)
    {
        if (!IsPersistenceReady())
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return ProfileSettingsStore.GetPersistedSettingsForMod(modPckName);
    }

    /// <summary>
    /// Persists current registration values (using GetCurrentValue/defaults) for one mod.
    /// Useful when settings are changed outside ModManagerSettings UI.
    /// </summary>
    public static bool PersistCurrentRegistrationValues(string modPckName)
    {
        if (!IsPersistenceReady())
        {
            Log.Info($"[ModManagerSettings] Skipping registration persistence for '{modPckName}' because profile-scoped path is not ready.");
            return false;
        }

        if (!TryGet(modPckName, out var registration))
        {
            return false;
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var def in registration.ToggleSettings)
        {
            values[def.Key] = (def.GetCurrentValue?.Invoke() ?? def.DefaultValue).ToString();
        }

        foreach (var def in registration.NumberSettings)
        {
            values[def.Key] = (def.GetCurrentValue?.Invoke() ?? def.DefaultValue)
                .ToString("R", CultureInfo.InvariantCulture);
        }

        foreach (var def in registration.ChoiceSettings)
        {
            values[def.Key] = def.GetCurrentValue?.Invoke() ?? def.DefaultValue;
        }

        foreach (var def in registration.TextSettings)
        {
            values[def.Key] = def.GetCurrentValue?.Invoke() ?? def.DefaultValue;
        }

        foreach (var def in registration.ColorSettings)
        {
            values[def.Key] = def.GetCurrentValue?.Invoke() ?? def.DefaultValue;
        }

        ProfileSettingsStore.SavePersistedSettingsForMod(modPckName, values);
        Log.Info($"[ModManagerSettings] Persisted registration values for '{modPckName}'. count={values.Count}.");
        return true;
    }


    /// <summary>
    /// Returns all setting definitions for one mod.
    /// </summary>
    public static IReadOnlyList<ModSettingDefinitionBase> GetAllSettings(string modPckName)
    {
        if (!TryGet(modPckName, out var registration))
        {
            return Array.Empty<ModSettingDefinitionBase>();
        }

        return GetAllSettings(registration);
    }

    /// <summary>
    /// Finds one setting definition by key, regardless of value type.
    /// </summary>
    public static bool TryGetSetting(string modPckName, string key, out ModSettingDefinitionBase definition)
    {
        definition = null!;
        if (!TryGet(modPckName, out var registration))
        {
            return false;
        }

        definition = GetAllSettings(registration).FirstOrDefault(s => s.Key.Equals(key, StringComparison.OrdinalIgnoreCase))!;
        return definition != null;
    }

    /// <summary>
    /// Adds or replaces one setting definition on a mod registration.
    /// Existing key match is removed from all type buckets before insert.
    /// </summary>
    public static bool UpsertSetting(string modPckName, ModSettingDefinitionBase definition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modPckName);
        ArgumentNullException.ThrowIfNull(definition);

        return ModifyRegistration(modPckName, existing => UpsertSettingInternal(existing, definition));
    }

    /// <summary>
    /// Removes one setting by key from a mod registration.
    /// </summary>
    public static bool RemoveSetting(string modPckName, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modPckName);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var changed = false;
        var modified = ModifyRegistration(modPckName, existing =>
        {
            var toggles = existing.ToggleSettings.Where(s => !s.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).ToList();
            var numbers = existing.NumberSettings.Where(s => !s.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).ToList();
            var choices = existing.ChoiceSettings.Where(s => !s.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).ToList();
            var text = existing.TextSettings.Where(s => !s.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).ToList();
            var colors = existing.ColorSettings.Where(s => !s.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).ToList();

            changed =
                toggles.Count != existing.ToggleSettings.Count ||
                numbers.Count != existing.NumberSettings.Count ||
                choices.Count != existing.ChoiceSettings.Count ||
                text.Count != existing.TextSettings.Count ||
                colors.Count != existing.ColorSettings.Count;

            return existing with
            {
                ToggleSettings = toggles,
                NumberSettings = numbers,
                ChoiceSettings = choices,
                TextSettings = text,
                ColorSettings = colors
            };
        });

        return modified && changed;
    }

    /// <summary>
    /// Applies a setting callback by key using an already-typed value.
    /// </summary>
    public static bool TryApplySetting<T>(string modPckName, string key, T value)
    {
        if (!TryGetSetting(modPckName, key, out var definition))
        {
            return false;
        }

        switch (definition)
        {
            case ModSettingToggleDefinition toggle when value is bool b:
                toggle.OnApply?.Invoke(b);
                return toggle.OnApply != null;
            case ModSettingNumberDefinition number when value is double d:
                number.OnApply?.Invoke(d);
                return number.OnApply != null;
            case ModSettingChoiceDefinition choice when value is string s:
                choice.OnApply?.Invoke(s);
                return choice.OnApply != null;
            case ModSettingTextDefinition text when value is string s:
                text.OnApply?.Invoke(s);
                return text.OnApply != null;
            case ModSettingColorDefinition color when value is string s:
                color.OnApply?.Invoke(s);
                return color.OnApply != null;
            default:
                return false;
        }
    }

    /// <summary>
    /// Applies a setting callback by key from string payload.
    /// Useful when driving from JSON/network/console tooling.
    /// </summary>
    public static bool TryApplySetting(string modPckName, string key, string rawValue)
    {
        if (!TryGetSetting(modPckName, key, out var definition))
        {
            return false;
        }

        switch (definition)
        {
            case ModSettingToggleDefinition:
                return bool.TryParse(rawValue, out var parsedBool) && TryApplySetting(modPckName, key, parsedBool);
            case ModSettingNumberDefinition:
                return double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedDouble) &&
                       TryApplySetting(modPckName, key, parsedDouble);
            case ModSettingChoiceDefinition:
            case ModSettingTextDefinition:
            case ModSettingColorDefinition:
                return TryApplySetting(modPckName, key, rawValue);
            default:
                return false;
        }
    }

    private static ModSettingsRegistration BuildGuardedRegistration(ModSettingsRegistration registration)
    {
        return registration with
        {
            ToggleSettings = registration.ToggleSettings.Select(def => new ModSettingToggleDefinition
            {
                Key = def.Key,
                Label = def.Label,
                Description = def.Description,
                Path = def.Path,
                DefaultValue = def.DefaultValue,
                GetCurrentValue = def.GetCurrentValue,
                OnApply = WrapSettingApply(registration.ModPckName, def.Key, def.OnApply)
            }).ToList(),
            NumberSettings = registration.NumberSettings.Select(def => new ModSettingNumberDefinition
            {
                Key = def.Key,
                Label = def.Label,
                Description = def.Description,
                Path = def.Path,
                DefaultValue = def.DefaultValue,
                GetCurrentValue = def.GetCurrentValue,
                MinValue = def.MinValue,
                MaxValue = def.MaxValue,
                Step = def.Step,
                OnApply = WrapSettingApply(registration.ModPckName, def.Key, def.OnApply)
            }).ToList(),
            ChoiceSettings = registration.ChoiceSettings.Select(def => new ModSettingChoiceDefinition
            {
                Key = def.Key,
                Label = def.Label,
                Description = def.Description,
                Path = def.Path,
                DefaultValue = def.DefaultValue,
                GetCurrentValue = def.GetCurrentValue,
                Options = def.Options,
                OnApply = WrapSettingApply(registration.ModPckName, def.Key, def.OnApply)
            }).ToList(),
            TextSettings = registration.TextSettings.Select(def => new ModSettingTextDefinition
            {
                Key = def.Key,
                Label = def.Label,
                Description = def.Description,
                Path = def.Path,
                DefaultValue = def.DefaultValue,
                GetCurrentValue = def.GetCurrentValue,
                PlaceholderText = def.PlaceholderText,
                OnApply = WrapSettingApply(registration.ModPckName, def.Key, def.OnApply)
            }).ToList(),
            ColorSettings = registration.ColorSettings.Select(def => new ModSettingColorDefinition
            {
                Key = def.Key,
                Label = def.Label,
                Description = def.Description,
                Path = def.Path,
                DefaultValue = def.DefaultValue,
                GetCurrentValue = def.GetCurrentValue,
                PlaceholderText = def.PlaceholderText,
                OnApply = WrapSettingApply(registration.ModPckName, def.Key, def.OnApply)
            }).ToList(),
            OnApply = WrapRegistrationApply(registration.ModPckName, registration.OnApply),
            OnRestoreDefaults = registration.OnRestoreDefaults
        };
    }

    private static ModSettingsRegistration UpsertSettingInternal(ModSettingsRegistration existing, ModSettingDefinitionBase definition)
    {
        var key = definition.Key;
        var toggles = existing.ToggleSettings.Where(s => !s.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).ToList();
        var numbers = existing.NumberSettings.Where(s => !s.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).ToList();
        var choices = existing.ChoiceSettings.Where(s => !s.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).ToList();
        var text = existing.TextSettings.Where(s => !s.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).ToList();
        var colors = existing.ColorSettings.Where(s => !s.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).ToList();

        switch (definition)
        {
            case ModSettingToggleDefinition toggle:
                toggles.Add(toggle);
                break;
            case ModSettingNumberDefinition number:
                numbers.Add(number);
                break;
            case ModSettingChoiceDefinition choice:
                choices.Add(choice);
                break;
            case ModSettingTextDefinition textSetting:
                text.Add(textSetting);
                break;
            case ModSettingColorDefinition color:
                colors.Add(color);
                break;
            default:
                throw new NotSupportedException($"Unsupported setting definition type: {definition.GetType().FullName}");
        }

        return existing with
        {
            ToggleSettings = toggles,
            NumberSettings = numbers,
            ChoiceSettings = choices,
            TextSettings = text,
            ColorSettings = colors
        };
    }

    private static List<ModSettingDefinitionBase> GetAllSettings(ModSettingsRegistration registration)
    {
        var all = new List<ModSettingDefinitionBase>(
            registration.ToggleSettings.Count +
            registration.NumberSettings.Count +
            registration.ChoiceSettings.Count +
            registration.TextSettings.Count +
            registration.ColorSettings.Count);

        all.AddRange(registration.ToggleSettings);
        all.AddRange(registration.NumberSettings);
        all.AddRange(registration.ChoiceSettings);
        all.AddRange(registration.TextSettings);
        all.AddRange(registration.ColorSettings);
        return all;
    }

    private static void RestorePersistedValues(string modPckName)
    {
        try
        {
            var persisted = ProfileSettingsStore.GetPersistedSettingsForMod(modPckName);
            if (persisted.Count == 0)
            {
                return;
            }

            var applied = 0;
            foreach (var pair in persisted)
            {
                if (TryGetSetting(modPckName, pair.Key, out var definition) &&
                    definition is ModSettingTextDefinition)
                {
                    // Text settings can execute complex callbacks (e.g. dynamic registry mutation).
                    // Keep them persisted for UI load, but skip startup-time auto-apply for safety.
                    continue;
                }

                if (TryApplySetting(modPckName, pair.Key, pair.Value))
                {
                    applied++;
                }
            }

            if (applied > 0 && TryGet(modPckName, out var registration) && registration.OnApply != null)
            {
                registration.OnApply.Invoke();
            }

            Log.Info($"[ModManagerSettings] Restored persisted settings for '{modPckName}'. applied={applied}, stored={persisted.Count}.");
        }
        catch (Exception ex)
        {
            Log.Warn($"[ModManagerSettings] Failed restoring persisted settings for '{modPckName}'. {ex.Message}");
        }
    }

    // Startup auto-apply is intentionally disabled for now to avoid lifecycle-related instability.

    private static Action? WrapRegistrationApply(string modPckName, Action? original)
    {
        if (original == null)
        {
            return null;
        }

        return () =>
        {
            if (ModSettingsSyncService.IsLocalMutationLocked)
            {
                Log.Warn($"[ModManagerSettings] Blocked local registration apply for '{modPckName}' during active multiplayer session.");
                return;
            }

            original.Invoke();
        };
    }

    private static Action<T>? WrapSettingApply<T>(string modPckName, string key, Action<T>? original)
    {
        if (original == null)
        {
            return null;
        }

        return value =>
        {
            if (ModSettingsSyncService.IsLocalMutationLocked)
            {
                Log.Warn($"[ModManagerSettings] Blocked local setting apply for mod='{modPckName}' key='{key}' during active multiplayer session.");
                return;
            }

            original.Invoke(value);
        };
    }
}
