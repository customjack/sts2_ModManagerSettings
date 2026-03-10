using System;
using System.Collections.Generic;
using System.Linq;
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

        lock (LockObject)
        {
            Registrations[registration.ModPckName] = registration;
        }

        Log.Info(
            $"[ModManagerSettings] Registered settings provider for '{registration.ModPckName}' " +
            $"(toggles={registration.ToggleSettings.Count}, numbers={registration.NumberSettings.Count}, " +
            $"choices={registration.ChoiceSettings.Count}, text={registration.TextSettings.Count}, " +
            $"colors={registration.ColorSettings.Count}).");
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
}
