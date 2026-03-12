using HarmonyLib;
using ModManagerSettings.Features.Examples;
using MegaCrit.Sts2.Core.Logging;

namespace ModManagerSettings.Core;

/// <summary>
/// Central startup for the mod.
/// - Applies all Harmony patches in this assembly.
/// </summary>
public static class ModBootstrap
{
    private const string HarmonyId = "modmanagersettings.harmony";
    private const string BuildMarker = "2026-03-12-mp-sync-applyonly-a";

    private static bool _initialized;
    private static Harmony? _harmony;

    /// <summary>
    /// Initializes mod runtime once per process.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized)
        {
            Log.Info("[ModManagerSettings] ModBootstrap.Initialize skipped (already initialized).");
            return;
        }

        _initialized = true;
        Log.Info($"[ModManagerSettings] Mod bootstrap starting. build={BuildMarker}");
        BuiltInExampleSettingsRegistration.Register();
        Log.Info("[ModManagerSettings] Built-in example settings registered.");

        _harmony = new Harmony(HarmonyId);
        _harmony.PatchAll();
        Log.Info($"[ModManagerSettings] Harmony patches applied with id '{HarmonyId}'.");
    }
}
